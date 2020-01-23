// Copyright 2021 Yubico AB
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
using System;
using System.Runtime.InteropServices;
using Yubico.Core;
using static Yubico.PlatformInterop.NativeMethods;

namespace Yubico.PlatformInterop
{
    internal class HidDDevice : IHidDDevice
    {
        public string DevicePath { get; private set; }
        public short Usage { get; private set; }
        public short UsagePage { get; private set; }
        public short InputReportByteLength { get; private set; }
        public short OutputReportByteLength { get; private set; }
        public short FeatureReportByteLength { get; private set; }

        private SafeFileHandle _handle;

        public HidDDevice(string devicePath)
        {
            DevicePath = devicePath;

            _handle = OpenHandleWithAccess(DESIRED_ACCESS.NONE);
            HIDP_CAPS capabilities = GetCapabilities(_handle);

            Usage = capabilities.Usage;
            UsagePage = capabilities.UsagePage;
            InputReportByteLength = capabilities.InputReportByteLength;
            OutputReportByteLength = capabilities.OutputReportByteLength;
            FeatureReportByteLength = capabilities.FeatureReportByteLength;
        }

        public void OpenIOConnection()
        {
            _handle.Dispose();
            _handle = OpenHandleWithAccess(DESIRED_ACCESS.GENERIC_READ | DESIRED_ACCESS.GENERIC_WRITE);
        }
        public void OpenFeatureConnection()
        {
            _handle.Dispose();
            _handle = OpenHandleWithAccess(DESIRED_ACCESS.GENERIC_WRITE);
        }

        public byte[] GetFeatureReport()
        {
            if (_handle is null)
            {
                throw new InvalidOperationException(ExceptionMessages.InvalidSafeFileHandle);
            }

            byte[] buffer = new byte[FeatureReportByteLength];

            if (!HidD_GetFeature(_handle, buffer, buffer.Length))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            byte[] returnBuf = new byte[FeatureReportByteLength - 1];
            Array.Copy(buffer, 1, returnBuf, 0, returnBuf.Length);

            return returnBuf;
        }

        public void SetFeatureReport(byte[] buffer)
        {
            if (_handle is null)
            {
                throw new InvalidOperationException(ExceptionMessages.InvalidSafeFileHandle);
            }

            if (buffer.Length != FeatureReportByteLength - 1)
            {
                throw new InvalidOperationException(ExceptionMessages.InvalidReportBufferLength);
            }

            byte[] sendBuf = new byte[buffer.Length + 1];
            Array.Copy(buffer, 0, sendBuf, 1, buffer.Length);

            if (!HidD_SetFeature(_handle, sendBuf, sendBuf.Length))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }

        public byte[] GetInputReport()
        {
            if (_handle is null)
            {
                throw new InvalidOperationException(ExceptionMessages.InvalidSafeFileHandle);
            }

            byte[] buffer = new byte[InputReportByteLength];

            if (!ReadFile(_handle, buffer, buffer.Length, out int bytesRead, IntPtr.Zero)
                || bytesRead != buffer.Length)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            byte[] returnBuf = new byte[InputReportByteLength - 1];
            Array.Copy(buffer, 1, returnBuf, 0, returnBuf.Length);

            return returnBuf;
        }

        public void SetOutputReport(byte[] buffer)
        {
            if (_handle is null)
            {
                throw new InvalidOperationException(ExceptionMessages.InvalidSafeFileHandle);
            }

            if (buffer.Length != OutputReportByteLength - 1)
            {
                throw new InvalidOperationException(ExceptionMessages.InvalidReportBufferLength);
            }

            byte[] sendBuf = new byte[buffer.Length + 1];
            Array.Copy(buffer, 0, sendBuf, 1, buffer.Length);

            if (!WriteFile(_handle, sendBuf, sendBuf.Length, out int bytesWritten, IntPtr.Zero)
                || bytesWritten != sendBuf.Length)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }

        private static HIDP_CAPS GetCapabilities(SafeFileHandle safeHandle)
        {
            var capabilities = new HIDP_CAPS();

            if (!HidD_GetPreparsedData(safeHandle, out IntPtr preparsedData))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            try
            {
                if (!HidP_GetCaps(preparsedData, ref capabilities))
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }

                return capabilities;
            }
            finally
            {
                _ = HidD_FreePreparsedData(preparsedData);
            }
        }

        private SafeFileHandle OpenHandleWithAccess(DESIRED_ACCESS desiredAccess)
        {
            SafeFileHandle handle = CreateFile(
                DevicePath,
                desiredAccess,
                FILE_SHARE.READWRITE,
                IntPtr.Zero,
                CREATION_DISPOSITION.OPEN_EXISTING,
                FILE_FLAG.NORMAL,
                IntPtr.Zero
                );

            if (handle.IsInvalid)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            return handle;
        }

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _handle.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() =>
            Dispose(true);
        #endregion

    }
}
