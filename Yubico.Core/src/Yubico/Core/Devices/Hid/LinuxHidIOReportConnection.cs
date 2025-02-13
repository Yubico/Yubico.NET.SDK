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
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Yubico.Core.Buffers;
using Yubico.Core.Logging;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.Hid
{
    internal class LinuxHidIOReportConnection : IHidConnection
    {
        private const int YubiKeyIOReportSize = 64;

        private readonly LinuxFileSafeHandle _handle;
        private bool _isDisposed;

        private readonly ILogger _log = Logging.Log.GetLogger<LinuxHidIOReportConnection>();
        private readonly LinuxHidDevice _device;

        public int InputReportSize { get; private set; }
        public int OutputReportSize { get; private set; }

        public LinuxHidIOReportConnection(LinuxHidDevice device, string devnode)
        {
            _log.LogInformation("Creating new IO report connection for device [{DevNode}]", devnode);

            InputReportSize = YubiKeyIOReportSize;
            OutputReportSize = YubiKeyIOReportSize;

            _device = device;
            _handle = NativeMethods.open(devnode, NativeMethods.OpenFlags.O_RDWR);

            if (_handle.IsInvalid)
            {
                _log.LogError("Could not open device for IO reports: {Error}", LibcHelpers.GetErrnoString());

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
            _log.SensitiveLogInformation("Sending IO report> {report}, Length = {length}", Hex.BytesToHex(report), report.Length);
            if (report.Length != YubiKeyIOReportSize)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidReportBufferLength));
            }

            // HIDRAW expects the first byte to be the frame number - or in cases where a frame number is not used,
            // like with the YubiKey, the first byte should be zero.
            byte[] paddedBuffer = new byte[YubiKeyIOReportSize + 1];
            report.CopyTo(paddedBuffer.AsSpan(1)); // Leave the first byte as 00

            int bytesWritten = NativeMethods.write(_handle.DangerousGetHandle().ToInt32(), paddedBuffer, paddedBuffer.Length);

            _device.LogDeviceAccessTime();

            CryptographicOperations.ZeroMemory(paddedBuffer);

            if (bytesWritten >= 0)
            {
                return;
            }

            _log.LogError("Write failed with: {Error}", LibcHelpers.GetErrnoString());

            throw new PlatformApiException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.HidrawFailed));
        }

        // Get the response that is waiting on the device. It will be 64 bytes.
        public byte[] GetReport()
        {
            // The return value is either < 0 for error, or the number of
            // bytes placed into the provided buffer.
            byte[] outputBuffer = new byte[YubiKeyIOReportSize];
            int bytesRead = NativeMethods.read(_handle, outputBuffer, YubiKeyIOReportSize);

            _device.LogDeviceAccessTime();

            if (bytesRead >= 0)
            {
                _log.SensitiveLogInformation("Receiving IO report< {report}", Hex.BytesToHex(outputBuffer));
                return outputBuffer;
            }

            _log.LogError("Read failed with: {Error}", LibcHelpers.GetErrnoString());
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
