// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Native.Windows.Kernel32;
using HidNative = Yubico.YubiKit.Core.Native.Windows.HidD.NativeMethods;
using Kernel = Yubico.YubiKit.Core.Native.Windows.Kernel32.NativeMethods;

namespace Yubico.YubiKit.Core.Transports.Hid.Windows;

/// <summary>Worker-directed resources for a single overlapped HID report handle.</summary>
/// <remarks>
/// The owner supplies all scheduling and lifetime barriers. Windows JIT/AOT callback-tail and HID
/// driver validation are required; hardware-free result/handle tests do not attest native execution.
/// </remarks>
internal sealed class WindowsHidReportApi : IWindowsHidReportApi
{
    private const string AccessDeniedGuidance =
        "Windows denied access to the HID interface. The interface may be held exclusively by another process, " +
        "or this environment may require running the process elevated as Administrator to open YubiKey HID reports.";
    private readonly WindowsHidSafeHandle _handle = new();
    private ThreadPoolBoundHandle? _binding;
    private WindowsHidOverlappedOperation? _activeOperation;
    private nint _preparsedData;
    private bool _opened;
    private bool _handleAcquired;
    private bool _handleReference;
    private bool _closeStarted;
    private WindowsHidRelease _release;

    public WindowsHidReportMetadata Open(string devicePath)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        if (_opened || _closeStarted)
        {
            throw new InvalidOperationException("The HID native resources cannot be reopened.");
        }

        _opened = true;
        nint value = Kernel.CreateHidFile(devicePath,
            Kernel.DESIRED_ACCESS.GENERIC_READ | Kernel.DESIRED_ACCESS.GENERIC_WRITE,
            Kernel.FILE_SHARE.ALL, 0, Kernel.CREATION_DISPOSITION.OPEN_EXISTING,
            Kernel.FILE_FLAG.NORMAL | Kernel.FILE_FLAG.OVERLAPPED, 0);
        int error = Marshal.GetLastPInvokeError();
        _handle.Initialize(value);
        _handleAcquired = !_handle.IsInvalid;
        if (!_handleAcquired)
        {
            ThrowWin32Failure("CreateFile", error, $"Failed to open HID device '{devicePath}'.");
        }

        _handle.DangerousAddRef(ref _handleReference);
        byte result = HidNative.GetHidPreparsedData(value, out _preparsedData);
        error = Marshal.GetLastPInvokeError();
        if (result == 0)
        {
            ThrowWin32Failure("HidD_GetPreparsedData", error, "Failed to get HID preparsed data.");
        }

        HidNative.HIDP_CAPS capabilities = new();
        int status = HidNative.HidP_GetCaps(_preparsedData, ref capabilities);
        if (status != HidNative.HidpStatusSuccess)
        {
            // HIDP returns NTSTATUS directly, not a Win32 last-error value.
            throw new PlatformApiException("HidP_GetCaps", status, "Failed to get HID capabilities.");
        }

        _binding = ThreadPoolBoundHandle.BindHandle(_handle);
        return new WindowsHidReportMetadata(unchecked((ushort)capabilities.UsagePage),
            unchecked((ushort)capabilities.Usage), unchecked((ushort)capabilities.InputReportByteLength),
            unchecked((ushort)capabilities.OutputReportByteLength));
    }

    public unsafe WindowsHidSubmission Submit(WindowsHidOverlappedOperation operation)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        ThreadPoolBoundHandle binding = _binding ?? throw new InvalidOperationException("HID handle is not bound.");
        if (_closeStarted || _activeOperation is not null || operation.RetirementStarted)
        {
            throw new InvalidOperationException("HID submission does not own an available native slot.");
        }

        _activeOperation = operation;
        operation.NativeOverlapped = binding.UnsafeAllocateNativeOverlapped(OnCompleted, operation, operation.NativeReport);
        operation.NativeOverlapped->EventHandle = 0;
        // Default completion modes: TRUE also queues a callback. Never synthesize one or enable
        // FILE_SKIP_COMPLETION_PORT_ON_SUCCESS. BCL's allocation starts Windows thread-pool tracking.
        int result;
        fixed (byte* buffer = operation.NativeReport)
        {
            result = operation.Direction == WindowsHidReportDirection.Input
                ? Kernel.ReadFile(_handle.DangerousGetHandle(), buffer, (uint)operation.NativeReport.Length,
                    null, operation.NativeOverlapped)
                : Kernel.WriteFile(_handle.DangerousGetHandle(), buffer, (uint)operation.NativeReport.Length,
                    null, operation.NativeOverlapped);
        }

        uint error = result == 0 ? unchecked((uint)Marshal.GetLastPInvokeError()) : 0;
        // Nothing fallible after native acceptance. The owner joins submit-return with last use.
        return InterpretSubmission(result, error);
    }

    internal static WindowsHidSubmission InterpretSubmission(int result, uint error)
        // ERROR_GEN_FAILURE preserves rejection if a broken native call returns FALSE with no error.
        => new(result != 0 || error == 997 ? 0 : error == 0 ? 31u : error);

    public unsafe uint Cancel(WindowsHidOverlappedOperation operation)
    {
        if (!ReferenceEquals(_activeOperation, operation) || operation.NativeOverlapped is null || operation.RetirementStarted)
        {
            throw new InvalidOperationException("Cancellation requires the exact live HID request.");
        }

        int result = Kernel.CancelIoEx(_handle.DangerousGetHandle(), operation.NativeOverlapped);
        uint error = result == 0 ? unchecked((uint)Marshal.GetLastPInvokeError()) : 0;
        return result != 0 ? 0 : error == 0 ? 31u : error;
    }

    public unsafe void Retire(WindowsHidOverlappedOperation operation)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        if (operation.RetirementStarted)
        {
            return;
        }

        if (operation.NativeOverlapped is not null && !ReferenceEquals(_activeOperation, operation))
        {
            throw new InvalidOperationException("Retirement requires this API's native request.");
        }

        operation.RetirementStarted = true;
        if (operation.NativeOverlapped is not null)
        {
            ThreadPoolBoundHandle binding = _binding ?? throw new InvalidOperationException("HID binding was lost.");
            // Owner guarantees submit-return plus rejection or callback last-use. Mark first:
            // unexpected free failure is quarantine, not permission to retry a possibly freed pointer.
            binding.FreeNativeOverlapped(operation.NativeOverlapped);
            operation.NativeOverlapped = null;
        }

        if (ReferenceEquals(_activeOperation, operation))
        {
            _activeOperation = null;
        }
    }

    public WindowsHidRelease CloseOnce()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        if (_closeStarted)
        {
            return _release;
        }

        _closeStarted = true;
        if (_activeOperation is not null)
        {
            return _release; // No native release attempt while request storage is live.
        }

        WindowsHidRelease metadataRelease = ReleasePreparsedData();
        _binding?.Dispose();
        _binding = null;
        if (_handleReference)
        {
            _handleReference = false;
            _handle.DangerousRelease();
        }

        WindowsHidRelease handleRelease = _handle.CloseOnce();
        _release = new WindowsHidRelease(metadataRelease.NativeReleaseProven && (!_handleAcquired || handleRelease.NativeReleaseProven),
            metadataRelease.ErrorCode != 0 ? metadataRelease.ErrorCode : handleRelease.ErrorCode);
        return _release;
    }

    private WindowsHidRelease ReleasePreparsedData()
    {
        if (_preparsedData == 0)
        {
            return new WindowsHidRelease(true, 0);
        }

        byte result = HidNative.FreeHidPreparsedData(_preparsedData);
        uint error = result == 0 ? unchecked((uint)Marshal.GetLastPInvokeError()) : 0;
        if (result != 0)
        {
            _preparsedData = 0;
        }

        return new WindowsHidRelease(result != 0, error);
    }

    [SupportedOSPlatform("windows")]
    private static unsafe void OnCompleted(uint error, uint bytesTransferred, NativeOverlapped* overlapped)
    {
        var operation = (WindowsHidOverlappedOperation?)ThreadPoolBoundHandle.GetNativeOverlappedState(overlapped)
            ?? throw new InvalidOperationException("HID completion state was lost.");
        operation.Complete(new WindowsHidCompletion(error, bytesTransferred));
        // No operation, pointer, buffer or handle access after Complete's last-use publication.
    }

    private static void ThrowWin32Failure(string source, int error, string message)
    {
        if (error == 5)
        {
            throw new UnauthorizedAccessException($"{message} {AccessDeniedGuidance}");
        }

        throw new PlatformApiException(source, error, message);
    }
}