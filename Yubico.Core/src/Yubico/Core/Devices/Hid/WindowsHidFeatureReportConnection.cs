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

using System;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.Hid
{
    internal sealed class WindowsHidFeatureReportConnection : IHidConnection
    {
        // The SDK device instance that created this connection instance.
        private readonly WindowsHidDevice _device;
        
        // The underlying Windows HID device used for communication.
        private readonly HidDDevice _hidDDevice;

        public int InputReportSize { get; private set; }
        public int OutputReportSize { get; private set; }

        internal WindowsHidFeatureReportConnection(WindowsHidDevice device, string path)
        {
            _device = device;
            _hidDDevice = new HidDDevice(path);
            SetupConnection();
        }

        private void SetupConnection()
        {
            _hidDDevice.OpenFeatureConnection();
            InputReportSize = _hidDDevice.FeatureReportByteLength;
            OutputReportSize = _hidDDevice.FeatureReportByteLength;
        }

        public byte[] GetReport()
        {
            byte[] data = _hidDDevice.GetFeatureReport();

            _device.LogDeviceAccessTime();

            return data;
        }

        public void SetReport(byte[] report)
        {
            _hidDDevice.SetFeatureReport(report);
            _device.LogDeviceAccessTime();
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _hidDDevice.Dispose();
                }

                _disposedValue = true;
            }
        }

        ~WindowsHidFeatureReportConnection()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
