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
using Yubico.Core.Buffers;
using Yubico.YubiKey.InterIndustry.Commands;
using Yubico.YubiKey.Pipelines;
using Yubico.Core.Devices.SmartCard;
using Yubico.Core.Iso7816;
using Yubico.Core.Logging;
using System.Linq;

namespace Yubico.YubiKey
{
    internal class CcidConnection : IYubiKeyConnection
    {
        private readonly Logger _log = Log.GetLogger();

        private readonly byte[]? _applicationId;
        private readonly IApduTransform _apduPipeline;
        private readonly ISmartCardConnection _smartCardConnection;
        private readonly YubiKeyApplication _yubiKeyApplication;
        private bool _disposedValue;

        private bool IsOath => _yubiKeyApplication == YubiKeyApplication.Oath
            || (_applicationId != null && _applicationId.SequenceEqual(YubiKeyApplicationExtensions.GetIso7816ApplicationId(YubiKeyApplication.Oath)));

        private IApduTransform AddResponseChainingTransform(IApduTransform pipeline) =>
            IsOath
            ? new OathResponseChainingTransform(pipeline)
            : new ResponseChainingTransform(pipeline);

        public ISelectApplicationData? SelectApplicationData { get; set; }

        public CcidConnection(ISmartCardDevice smartCardDevice, YubiKeyApplication yubiKeyApplication)
        {
            if (yubiKeyApplication == YubiKeyApplication.Unknown)
            {
                throw new NotSupportedException();
            }

            _yubiKeyApplication = yubiKeyApplication;

            _smartCardConnection = smartCardDevice.Connect();

            _apduPipeline = new SmartCardTransform(_smartCardConnection);
            _apduPipeline = AddResponseChainingTransform(_apduPipeline);
            _apduPipeline = new CommandChainingTransform(_apduPipeline);

            // CCID has the concept of multiple applications. Since we cannot guarantee the
            // state of the smart card when connecting, we should always send down a connection
            // request.
            SelectApplication();
        }

        public CcidConnection(ISmartCardDevice smartCardDevice, byte[] applicationId)
        {
            _applicationId = applicationId;
            _yubiKeyApplication = YubiKeyApplication.Unknown;
            
            _smartCardConnection = smartCardDevice.Connect();

            _apduPipeline = new SmartCardTransform(_smartCardConnection);
            _apduPipeline = AddResponseChainingTransform(_apduPipeline);
            _apduPipeline = new CommandChainingTransform(_apduPipeline);

            // CCID has the concept of multiple applications. Since we cannot guarantee the
            // state of the smart card when connecting, we should always send down a connection
            // request.
            SelectApplication();
        }

        public CcidConnection(ISmartCardDevice smartCardDevice, YubiKeyApplication yubiKeyApplication, Scp03.StaticKeys staticKeys)
        {
            if (yubiKeyApplication == YubiKeyApplication.Unknown)
            {
                throw new NotSupportedException();
            }

            _yubiKeyApplication = yubiKeyApplication;

            _smartCardConnection = smartCardDevice.Connect();

            _apduPipeline = new SmartCardTransform(_smartCardConnection);
            _apduPipeline = new Scp03ApduTransform(_apduPipeline, staticKeys);
            _apduPipeline = AddResponseChainingTransform(_apduPipeline);
            _apduPipeline = new CommandChainingTransform(_apduPipeline);
            _apduPipeline = new OtpErrorTransform(_apduPipeline);

            // CCID has the concept of multiple applications. Since we cannot guarantee the
            // state of the smart card when connecting, we should always send down a connection
            // request.
            SelectApplication();

            _apduPipeline.Setup();
        }

        public CcidConnection(ISmartCardDevice smartCardDevice, byte[] applicationId, Scp03.StaticKeys staticKeys)
        {
            _applicationId = applicationId;
            _yubiKeyApplication = YubiKeyApplication.Unknown;

            _smartCardConnection = smartCardDevice.Connect();

            _apduPipeline = new SmartCardTransform(_smartCardConnection);
            _apduPipeline = new Scp03ApduTransform(_apduPipeline, staticKeys);
            _apduPipeline = AddResponseChainingTransform(_apduPipeline);
            _apduPipeline = new CommandChainingTransform(_apduPipeline);
            _apduPipeline = new OtpErrorTransform(_apduPipeline);

            // CCID has the concept of multiple applications. Since we cannot guarantee the
            // state of the smart card when connecting, we should always send down a connection
            // request.
            SelectApplication();

            _apduPipeline.Setup();
        }

        public TResponse SendCommand<TResponse>(IYubiKeyCommand<TResponse> yubiKeyCommand) where TResponse : IYubiKeyResponse
        {
            using (IDisposable transaction = _smartCardConnection.BeginTransaction(out bool cardWasReset))
            {
                if (cardWasReset)
                {
                    SelectApplication();
                }

                ResponseApdu responseApdu = _apduPipeline.Invoke(
                    yubiKeyCommand.CreateCommandApdu(),
                    yubiKeyCommand.GetType(),
                    typeof(TResponse));

                return yubiKeyCommand.CreateResponseForApdu(responseApdu);
            }
        }

        private void SelectApplication()
        {
            IYubiKeyCommand<ISelectApplicationResponse<ISelectApplicationData>> selectApplicationCommand = _yubiKeyApplication switch
            {
                YubiKeyApplication.Oath => new Oath.Commands.SelectOathCommand(),
                YubiKeyApplication.Unknown => new SelectApplicationCommand(_applicationId!),
                _ => new SelectApplicationCommand(_yubiKeyApplication),
            };

            _log.LogInformation("Selecting smart card application [{AID}]", Hex.BytesToHex(_applicationId ?? _yubiKeyApplication.GetIso7816ApplicationId()));
            ResponseApdu responseApdu = _smartCardConnection.Transmit(selectApplicationCommand.CreateCommandApdu());

            if (responseApdu.SW != SWConstants.Success)
            {
                throw new ApduException(
                      string.Format(
                          CultureInfo.CurrentCulture,
                          ExceptionMessages.SmartCardPipelineSetupFailed,
                          responseApdu.SW))
                {
                    SW = responseApdu.SW
                };
            }

            ISelectApplicationResponse<ISelectApplicationData>? response = selectApplicationCommand.CreateResponseForApdu(responseApdu);
            SelectApplicationData = response.GetData();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _smartCardConnection.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
