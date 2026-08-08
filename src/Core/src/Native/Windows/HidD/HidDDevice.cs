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

    // The two report paths below deliberately request DIFFERENT access, and are deliberately SEPARATE
    // methods. Read the note on OpenFeatureConnection before changing either: the split is load-bearing,
    // and it is what makes hidapi's retry-on-failure pattern unnecessary here.

    /// <summary>
    ///     Opens the handle used for input/output reports (FIDO).
    /// </summary>
    /// <remarks>
    ///     Requires GENERIC_READ | GENERIC_WRITE because this path really does use ReadFile/WriteFile
    ///     (see <see cref="GetInputReport" /> and <see cref="SetOutputReport" />), unlike the feature path.
    ///     Do NOT add a fall back to a lesser access level here: a zero-access handle would open
    ///     successfully and then fail on every subsequent ReadFile/WriteFile, turning one clear
    ///     UnauthorizedAccessException at open time — which carries the elevation guidance a non-elevated
    ///     Windows caller needs — into cryptic failures at each I/O call.
    /// </remarks>
    public void OpenIOConnection()
        => OpenReportConnection(Kernel32.NativeMethods.DESIRED_ACCESS.GENERIC_READ |
                                Kernel32.NativeMethods.DESIRED_ACCESS.GENERIC_WRITE);

    /// <summary>
    ///     Opens the handle used for feature reports (OTP), with no desired access.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         OTP feature-report I/O uses HidD_GetFeature/HidD_SetFeature, which are IOCTLs that succeed on
    ///         a zero-access handle. The OTP interface is a keyboard top-level collection, and Windows
    ///         refuses GENERIC_READ/GENERIC_WRITE on the system keyboard even for an elevated process
    ///         (anti-keylogger restriction). Opening with no access sidesteps that while still permitting
    ///         feature-report IOCTLs.
    ///     </para>
    ///     <para>
    ///         Scope of the evidence, stated precisely: the constructor's metadata probe shows only that this
    ///         same path OPENS with zero access (CreateFile + HidD_GetPreparsedData/HidP_GetCaps). It does not
    ///         exercise HidD_GetFeature/HidD_SetFeature, so it is supporting evidence, not proof that feature
    ///         I/O succeeds. Sufficiency for feature I/O rests on the Win32 contract for these IOCTLs plus the
    ///         Windows hardware run recorded in docs/plans/session-contention/ISA.md (YubiOtp integration
    ///         10/10, fw 5.8.0).
    ///     </para>
    ///     <para>
    ///         <b>Why there is no "try read/write, fall back to none" retry, as hidapi does.</b> hidapi's
    ///         Windows backend (<c>windows/hid.c</c>, <c>hid_open_path</c>) opens with
    ///         GENERIC_READ | GENERIC_WRITE and, on failure, retries with zero access, commenting that system
    ///         devices such as keyboards cannot be opened read/write because the system takes exclusive
    ///         control to prevent keyloggers, but that feature reports still work. It needs that retry because
    ///         it returns ONE handle serving both <c>hid_read</c>/<c>hid_write</c> (ReadFile/WriteFile) and
    ///         <c>hid_get_feature_report</c> (IOCTL), so it must request the maximal access and degrade.
    ///     </para>
    ///     <para>
    ///         This type splits those into two handles instead — <see cref="OpenIOConnection" /> for
    ///         ReadFile/WriteFile, this method for feature IOCTLs — so each already requests exactly what its
    ///         callers use. Adding the retry here would buy nothing and cost a syscall: read/write access
    ///         grants no capability this path uses; on the keyboard-class OTP collection the first attempt
    ///         always fails, so it would always fall through to zero access anyway; and zero access already
    ///         succeeds in a superset of the cases read/write does, being the least restrictive request and
    ///         not subject to share-mode conflicts. There is no case a read/write-first retry could rescue
    ///         that this call does not already handle.
    ///     </para>
    /// </remarks>
    public void OpenFeatureConnection()
        => OpenReportConnection(Kernel32.NativeMethods.DESIRED_ACCESS.NONE);

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

    private void OpenReportConnection(Kernel32.NativeMethods.DESIRED_ACCESS desiredAccess)
    {
        var handle = OpenHandleWithAccess(desiredAccess);
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