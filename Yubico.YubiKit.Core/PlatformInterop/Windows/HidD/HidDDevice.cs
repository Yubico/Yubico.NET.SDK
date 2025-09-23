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

namespace Yubico.YubiKit.Core.PlatformInterop.Windows.HidD;

internal class HidDDevice : IHidDDevice
{
    private SafeFileHandle _handle;

    public HidDDevice(string devicePath)
    {
        DevicePath = devicePath;

        _handle = OpenHandleWithAccess(Kernel32.NativeMethods.DESIRED_ACCESS.NONE);
        NativeMethods.HIDP_CAPS capabilities = GetCapabilities(_handle);

        Usage = capabilities.Usage;
        UsagePage = capabilities.UsagePage;
        InputReportByteLength = capabilities.InputReportByteLength;
        OutputReportByteLength = capabilities.OutputReportByteLength;
        FeatureReportByteLength = capabilities.FeatureReportByteLength;
    }

    #region IHidDDevice Members

    public string DevicePath { get; }
    public short Usage { get; }
    public short UsagePage { get; }
    public short InputReportByteLength { get; }
    public short OutputReportByteLength { get; }
    public short FeatureReportByteLength { get; }

    public void OpenIOConnection()
    {
        _handle.Dispose();
        _handle = OpenHandleWithAccess(Kernel32.NativeMethods.DESIRED_ACCESS.GENERIC_READ |
                                       Kernel32.NativeMethods.DESIRED_ACCESS.GENERIC_WRITE);
    }

    public void OpenFeatureConnection()
    {
        _handle.Dispose();
        _handle = OpenHandleWithAccess(Kernel32.NativeMethods.DESIRED_ACCESS.GENERIC_WRITE);
    }

    public byte[] GetFeatureReport()
    {
        if (_handle is null) throw new InvalidOperationException("ExceptionMessages.InvalidSafeFileHandle");

        byte[] buffer = new byte[FeatureReportByteLength];

        if (!NativeMethods.HidD_GetFeature(_handle, buffer, buffer.Length))
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

        byte[] returnBuf = new byte[FeatureReportByteLength - 1];
        Array.Copy(buffer, 1, returnBuf, 0, returnBuf.Length);

        return returnBuf;
    }

    public void SetFeatureReport(byte[] buffer)
    {
        if (_handle is null) throw new InvalidOperationException("ExceptionMessages.InvalidSafeFileHandle");

        if (buffer.Length != FeatureReportByteLength - 1)
            throw new InvalidOperationException("ExceptionMessages.InvalidReportBufferLength");

        byte[] sendBuf = new byte[buffer.Length + 1];
        Array.Copy(buffer, 0, sendBuf, 1, buffer.Length);

        if (!NativeMethods.HidD_SetFeature(_handle, sendBuf, sendBuf.Length))
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
    }

    public byte[] GetInputReport()
    {
        if (_handle is null) throw new InvalidOperationException("ExceptionMessages.InvalidSafeFileHandle");

        byte[] buffer = new byte[InputReportByteLength];

        if (!Kernel32.NativeMethods.ReadFile(_handle, buffer, buffer.Length, out int bytesRead, IntPtr.Zero)
            || bytesRead != buffer.Length)
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

        byte[] returnBuf = new byte[InputReportByteLength - 1];
        Array.Copy(buffer, 1, returnBuf, 0, returnBuf.Length);

        return returnBuf;
    }

    public void SetOutputReport(byte[] buffer)
    {
        if (_handle is null) throw new InvalidOperationException("ExceptionMessages.InvalidSafeFileHandle");

        if (buffer.Length != OutputReportByteLength - 1)
            throw new InvalidOperationException("ExceptionMessages.InvalidReportBufferLength");

        byte[] sendBuf = new byte[buffer.Length + 1];
        Array.Copy(buffer, 0, sendBuf, 1, buffer.Length);

        if (!Kernel32.NativeMethods.WriteFile(_handle, sendBuf, sendBuf.Length, out int bytesWritten, IntPtr.Zero)
            || bytesWritten != sendBuf.Length)
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
    }

    #endregion

    private static NativeMethods.HIDP_CAPS GetCapabilities(SafeFileHandle safeHandle)
    {
        NativeMethods.HIDP_CAPS capabilities = new();

        if (!NativeMethods.HidD_GetPreparsedData(safeHandle, out IntPtr preparsedData))
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

        try
        {
            if (!NativeMethods.HidP_GetCaps(preparsedData, ref capabilities))
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            return capabilities;
        }
        finally
        {
            _ = NativeMethods.HidD_FreePreparsedData(preparsedData);
        }
    }

    private SafeFileHandle OpenHandleWithAccess(Kernel32.NativeMethods.DESIRED_ACCESS desiredAccess)
    {
        SafeFileHandle handle = Kernel32.NativeMethods.CreateFile(
            DevicePath,
            desiredAccess,
            Kernel32.NativeMethods.FILE_SHARE.READWRITE,
            IntPtr.Zero,
            Kernel32.NativeMethods.CREATION_DISPOSITION.OPEN_EXISTING,
            Kernel32.NativeMethods.FILE_FLAG.NORMAL,
            IntPtr.Zero
        );

        if (handle.IsInvalid) Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

        return handle;
    }

    #region IDisposable Support

    private bool disposedValue; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing) _handle.Dispose();

            disposedValue = true;
        }
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose() => Dispose(true);

    #endregion
}