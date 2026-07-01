// Copyright 2025 Yubico AB
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

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Yubico.YubiKit.Core.Native.Windows.HidD;

internal sealed class HidDDevice : IHidDDevice
{
    private const int ErrorAccessDenied = 5;
    private const string WindowsHidAccessDeniedGuidance =
        "Windows denied access to the HID interface. The interface may be held exclusively by another process, " +
        "or this environment may require running the process elevated as Administrator to open YubiKey HID reports.";
    private SafeFileHandle _handle;
    private bool _disposed;

    public HidDDevice(string devicePath)
    {
        DevicePath = devicePath;

        _handle = OpenHandleForMetadata(out var capabilities);

        Usage = capabilities.Usage;
        UsagePage = capabilities.UsagePage;
        InputReportByteLength = capabilities.InputReportByteLength;
        OutputReportByteLength = capabilities.OutputReportByteLength;
        FeatureReportByteLength = capabilities.FeatureReportByteLength;
    }


    public string DevicePath { get; }
    public short Usage { get; }
    public short UsagePage { get; }
    public short InputReportByteLength { get; }
    public short OutputReportByteLength { get; }
    public short FeatureReportByteLength { get; }

    public void OpenIOConnection()
        => OpenReportConnection();

    public void OpenFeatureConnection()
        => OpenReportConnection();

    public byte[] GetFeatureReport()
    {
        EnsureOpenHandle();

        var buffer = new byte[FeatureReportByteLength];

        if (!NativeMethods.HidD_GetFeature(_handle, buffer, buffer.Length))
        {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        // Windows includes the report ID byte; the SDK exposes only report payload bytes.
        var returnBuf = new byte[FeatureReportByteLength - 1];
        Array.Copy(buffer, 1, returnBuf, 0, returnBuf.Length);

        return returnBuf;
    }

    public void SetFeatureReport(byte[] buffer)
    {
        EnsureOpenHandle();

        if (buffer.Length != FeatureReportByteLength - 1)
        {
            throw new ArgumentException(
                $"The HID feature report buffer length is invalid. Expected {FeatureReportByteLength - 1} bytes, but got {buffer.Length}.",
                nameof(buffer));
        }

        // Windows expects the report ID byte before the report payload.
        var sendBuf = new byte[buffer.Length + 1];
        Array.Copy(buffer, 0, sendBuf, 1, buffer.Length);

        if (!NativeMethods.HidD_SetFeature(_handle, sendBuf, sendBuf.Length))
        {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
    }

    public byte[] GetInputReport()
    {
        EnsureOpenHandle();

        var buffer = new byte[InputReportByteLength];
        if (!Kernel32.NativeMethods.ReadFile(_handle, buffer, buffer.Length, out var bytesRead, IntPtr.Zero)
            || bytesRead != buffer.Length)
        {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        // Windows includes the report ID byte; the SDK exposes only report payload bytes.
        var returnBuf = new byte[InputReportByteLength - 1];
        Array.Copy(buffer, 1, returnBuf, 0, returnBuf.Length);

        return returnBuf;
    }

    public void SetOutputReport(byte[] buffer)
    {
        EnsureOpenHandle();

        if (buffer.Length != OutputReportByteLength - 1)
        {
            throw new ArgumentException(
                $"The HID output report buffer length is invalid. Expected {OutputReportByteLength - 1} bytes, but got {buffer.Length}.",
                nameof(buffer));
        }

        // Windows expects the report ID byte before the report payload.
        var sendBuf = new byte[buffer.Length + 1];
        Array.Copy(buffer, 0, sendBuf, 1, buffer.Length);

        if (!Kernel32.NativeMethods.WriteFile(_handle, sendBuf, sendBuf.Length, out var bytesWritten, IntPtr.Zero)
            || bytesWritten != sendBuf.Length)
        {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
    }


    private static NativeMethods.HIDP_CAPS GetCapabilities(SafeFileHandle safeHandle)
    {
        NativeMethods.HIDP_CAPS capabilities = new();

        if (!NativeMethods.HidD_GetPreparsedData(safeHandle, out var preparsedData))
        {
            ThrowHidDWin32Failure(nameof(NativeMethods.HidD_GetPreparsedData), "Failed to get HID preparsed data.");
        }

        try
        {
            var result = NativeMethods.HidP_GetCaps(preparsedData, ref capabilities);
            return result == NativeMethods.HidpStatusSuccess
                ? capabilities
                : throw new PlatformApiException(nameof(NativeMethods.HidP_GetCaps), result,
                    "Failed to get HID capabilities.");
        }
        finally
        {
            _ = NativeMethods.HidD_FreePreparsedData(preparsedData);
        }
    }

    private SafeFileHandle OpenHandleWithAccess(Kernel32.NativeMethods.DESIRED_ACCESS desiredAccess)
    {
        var handle = Kernel32.NativeMethods.CreateFile(
            DevicePath,
            desiredAccess,
            Kernel32.NativeMethods.FILE_SHARE.ALL,
            IntPtr.Zero,
            Kernel32.NativeMethods.CREATION_DISPOSITION.OPEN_EXISTING,
            Kernel32.NativeMethods.FILE_FLAG.NORMAL,
            IntPtr.Zero
        );

        if (handle.IsInvalid)
        {
            var error = Marshal.GetLastWin32Error();
            if (error == ErrorAccessDenied)
            {
                throw new UnauthorizedAccessException(
                    $"Access denied opening HID device '{DevicePath}'. {WindowsHidAccessDeniedGuidance}");
            }

            throw new PlatformApiException(nameof(Kernel32.NativeMethods.CreateFile), error,
                $"Failed to open HID device '{DevicePath}'.");
        }

        return handle;
    }

    private SafeFileHandle OpenHandleForMetadata(out NativeMethods.HIDP_CAPS capabilities)
    {
        try
        {
            var handle = OpenHandleWithAccess(Kernel32.NativeMethods.DESIRED_ACCESS.NONE);
            try
            {
                capabilities = GetCapabilities(handle);
                return handle;
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }
        catch (Exception ex) when (RequiresReadWriteMetadataHandle(ex))
        {
            var handle = OpenReadWriteHandle();
            try
            {
                capabilities = GetCapabilities(handle);
                return handle;
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }
    }

    private void OpenReportConnection()
    {
        var handle = OpenReadWriteHandle();
        _handle.Dispose();
        _handle = handle;
    }

    private SafeFileHandle OpenReadWriteHandle()
        => OpenHandleWithAccess(Kernel32.NativeMethods.DESIRED_ACCESS.GENERIC_READ |
                                Kernel32.NativeMethods.DESIRED_ACCESS.GENERIC_WRITE);

    private static bool RequiresReadWriteMetadataHandle(Exception exception)
        => exception is UnauthorizedAccessException;

    private static void ThrowHidDWin32Failure(string source, string message)
    {
        var error = Marshal.GetLastWin32Error();
        if (error == ErrorAccessDenied)
        {
            throw new UnauthorizedAccessException($"{message} {WindowsHidAccessDeniedGuidance}");
        }

        throw new PlatformApiException(source, error, message);
    }

    private void EnsureOpenHandle()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_handle.IsInvalid || _handle.IsClosed)
        {
            throw new InvalidOperationException($"The HID device handle for '{DevicePath}' is not open.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _handle.Dispose();
        _disposed = true;
    }

}