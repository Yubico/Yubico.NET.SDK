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
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.Hid
{
    internal class WindowsHidIOReportConnection : IHidConnection
    {
        private readonly WindowsHidDevice _owningDevice;

        private IHidDDevice Device { get; set; }

        public int InputReportSize { get; private set; }
        public int OutputReportSize { get; private set; }

        internal WindowsHidIOReportConnection(WindowsHidDevice device, string path)
        {
            _owningDevice = device;
            Device = new HidDDevice(path);
            SetupConnection();
        }

        private void SetupConnection()
        {
            Device.OpenIOConnection();
            InputReportSize = Device.InputReportByteLength;
            OutputReportSize = Device.OutputReportByteLength;
        }

        public byte[] GetReport()
        {
            _owningDevice.AccessDevice();
            return Device.GetInputReport();
        }

        public void SetReport(byte[] report)
        {
            _owningDevice.AccessDevice();
            Device.SetOutputReport(report);
        }

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Device.Dispose();
                }

                disposedValue = true;
            }
        }

        ~WindowsHidIOReportConnection()
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
