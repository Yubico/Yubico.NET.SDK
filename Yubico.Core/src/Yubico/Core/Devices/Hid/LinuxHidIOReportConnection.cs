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
using System.Globalization;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.Hid
{
    internal class LinuxHidIOReportConnection : IHidConnection
    {
        private const int YubiKeyIOReportSize = 64;

        private readonly LinuxFileSafeHandle _handle;
        private bool _isDisposed;

        public int InputReportSize { get; private set; }
        public int OutputReportSize { get; private set; }

        public LinuxHidIOReportConnection(string devnode)
        {
            InputReportSize = YubiKeyIOReportSize;
            OutputReportSize = YubiKeyIOReportSize;

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

        // Send the given report to the FIDO device. All FIDO messages are
        // exactly 64 bytes long.
        public void SetReport(byte[] report)
        {
            if (report.Length != YubiKeyIOReportSize)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidReportBufferLength));
            }

            int bytesWritten = NativeMethods.write(_handle, report, report.Length);
            if (bytesWritten >= 0)
            {
                return;
            }

            throw new PlatformApiException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.HidrawFailed));
        }

        // Get the response that is waiting on the device. It will be 64 bytes.
        public byte[] GetReport()
        {
            var fds = new NativeMethods.PollFd[1]
            {
                new NativeMethods.PollFd
                {
                    fd = _handle.DangerousGetHandle().ToInt32(),
                    events = 1
                }
            };

            _ = NativeMethods.poll(fds, fds.Length, -1);

            // The return value is either < 0 for error, or the number of
            // bytes placed into the provided buffer.
            byte[] outputBuffer = new byte[YubiKeyIOReportSize];
            int bytesRead = NativeMethods.read(_handle, outputBuffer, YubiKeyIOReportSize);
            if (bytesRead >= 0)
            {
                return outputBuffer;
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
