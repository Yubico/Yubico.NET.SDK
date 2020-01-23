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

using Yubico.YubiKey.Pipelines;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey
{
    internal class FidoConnection : IYubiKeyConnection
    {
        private readonly IApduTransform _apduPipeline;
        private readonly IHidConnection _fidoConnection;
        private bool _disposedValue;

        public FidoConnection(IHidDevice hidDevice)
        {
            _fidoConnection = hidDevice.ConnectToIOReports();

            _apduPipeline = new FidoTransform(_fidoConnection);

            _apduPipeline.Setup();
        }

        public TResponse SendCommand<TResponse>(IYubiKeyCommand<TResponse> yubiKeyCommand)
            where TResponse : IYubiKeyResponse
        {
            CommandApdu commandApdu = yubiKeyCommand.CreateCommandApdu();

            ResponseApdu responseApdu = _apduPipeline.Invoke(commandApdu, yubiKeyCommand.GetType(), typeof(TResponse));

            return yubiKeyCommand.CreateResponseForApdu(responseApdu);
        }

        public InterIndustry.Commands.ISelectApplicationData? SelectApplicationData { get; set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _fidoConnection.Dispose();
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
