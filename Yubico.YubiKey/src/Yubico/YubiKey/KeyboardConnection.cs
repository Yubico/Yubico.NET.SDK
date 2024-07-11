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
using Yubico.Core.Devices.Hid;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Pipelines;

namespace Yubico.YubiKey
{
    internal class KeyboardConnection : IYubiKeyConnection
    {
        private readonly IApduTransform _apduPipeline;
        private readonly IHidConnection _hidConnection;
        private readonly KeyboardTransform _kb;
        private bool _disposedValue;

        public KeyboardConnection(IHidDevice hidDevice)
        {
            _hidConnection = hidDevice.ConnectToFeatureReports();

            _kb = new KeyboardTransform(_hidConnection);
            _apduPipeline = new OtpErrorTransform(_kb);

            _apduPipeline.Setup();
        }

        public TResponse SendCommand<TResponse>(IYubiKeyCommand<TResponse> yubiKeyCommand)
            where TResponse : IYubiKeyResponse
        {
            CommandApdu apdu = yubiKeyCommand.CreateCommandApdu();

            ResponseApdu responseApdu = _apduPipeline.Invoke(apdu, yubiKeyCommand.GetType(), typeof(TResponse));

            return yubiKeyCommand.CreateResponseForApdu(responseApdu);
        }

        public event EventHandler<EventArgs> TouchEvent
        {
            add => _kb.TouchPending += value;
            remove => _kb.TouchPending -= value;
        }

        public InterIndustry.Commands.ISelectApplicationData? SelectApplicationData { get; set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _apduPipeline.Cleanup();
                    _hidConnection.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }
    }
}
