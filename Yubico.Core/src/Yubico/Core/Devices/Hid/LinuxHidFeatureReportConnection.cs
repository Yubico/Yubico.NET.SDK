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

using System;
using System.Linq;
using System.Globalization;
using System.Runtime.InteropServices;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.Hid
{
    internal class LinuxHidFeatureReportConnection : IHidConnection
    {
        private const int YubiKeyFeatureReportSize = 8;

        private readonly LinuxFileSafeHandle _handle;
        private bool _isDisposed;
        private readonly LinuxHidDevice _device;

        public int InputReportSize { get; private set; }
        public int OutputReportSize { get; private set; }

        public LinuxHidFeatureReportConnection(LinuxHidDevice device, string devnode)
        {
            InputReportSize = YubiKeyFeatureReportSize;
            OutputReportSize = YubiKeyFeatureReportSize;

            _device = device;
            _handle = NativeMethods.open(
                devnode, NativeMethods.OpenFlags.O_RDWR | NativeMethods.OpenFlags.O_NONBLOCK);

            if (_handle.IsInvalid)
            {
                throw new PlatformApiException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.LinuxHidOpenFailed));
            }
        }

        // Send the given report as a HID feature report.
        // We expect to get a report that is FeatureReportSize bytes long. Then
        // we prepend a 00 byte for the actual data passed into the YubiKey.
        public void SetReport(byte[] report)
        {
            if (report.Length != YubiKeyFeatureReportSize)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidReportBufferLength));
            }

            byte[] reportToSend = new byte[report.Length + 1];
            Array.Copy(report, 0, reportToSend, 1, report.Length);

            long ioctlFlag = NativeMethods.HIDIOCSFEATURE | ((long)(report.Length + 1) << 16);
            IntPtr setReportData = Marshal.AllocHGlobal(report.Length + 1);

            try
            {
                Marshal.Copy(reportToSend, 0, setReportData, reportToSend.Length);
                int bytesSent = NativeMethods.ioctl(_handle, ioctlFlag, setReportData);

                _device.LogDeviceAccessTime();

                if (bytesSent >= 0)
                {
                    return;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(setReportData);
            }

            throw new PlatformApiException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.HidrawFailed));
        }

        // Get the feature report that is waiting on the device.
        public byte[] GetReport()
        {
            long ioctlFlag = NativeMethods.HIDIOCGFEATURE | ((long)NativeMethods.MaxFeatureBufferSize << 16);
            IntPtr getReportData = Marshal.AllocHGlobal(NativeMethods.MaxFeatureBufferSize);

            try
            {
                // The return value is either < 0 for error, or the number of
                // bytes placed into the provided buffer.
                int bytesReturned = NativeMethods.ioctl(_handle, ioctlFlag, getReportData);

                _device.LogDeviceAccessTime();

                if (bytesReturned >= 0)
                {
                    // A YubiKey "has a usable payload of 8 bytes". Hence,
                    // if we receive something longer than 8, just return the
                    // last 8.
                    byte[] outputBuffer = new byte[bytesReturned];
                    Marshal.Copy(getReportData, outputBuffer, 0, bytesReturned);
                    if (bytesReturned > YubiKeyFeatureReportSize)
                    {
                        outputBuffer = outputBuffer.Skip(bytesReturned - YubiKeyFeatureReportSize).ToArray();
                    }
                    return outputBuffer;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(getReportData);
            }

            throw new PlatformApiException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.HidrawFailed));
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                _handle.Dispose();
            }

            _isDisposed = true;
        }
    }
}
