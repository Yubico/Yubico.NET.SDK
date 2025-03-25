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
using System.Linq;
using Microsoft.Extensions.Logging;
using Yubico.Core.Buffers;
using Yubico.Core.Devices.SmartCard;
using Yubico.Core.Iso7816;
using Yubico.Core.Logging;
using Yubico.YubiKey.InterIndustry.Commands;
using Yubico.YubiKey.Pipelines;

namespace Yubico.YubiKey
{
    internal class SmartCardConnection : IYubiKeyConnection
    {
        private readonly ILogger _log = Log.GetLogger<SmartCardConnection>();

        // The application can be set either by using the YubikeyApplication enum or the Iso7816ApplicationId
        private readonly YubiKeyApplication _yubiKeyApplication;
        private readonly byte[]? _applicationId;
        private readonly ISmartCardConnection _smartCardConnection;
        private IApduTransform _apduPipeline;
        private bool _disposedValue;
        private bool IsOath => GetIsOauth();
        public ISelectApplicationData? SelectApplicationData { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartCardConnection"/> class with the specified
        /// smart card device and YubiKey application.
        /// </summary>
        /// <param name="smartCardDevice">The smart card device to connect to.</param>
        /// <param name="yubiKeyApplication">The YubiKey application to be used for the connection.</param>
        public SmartCardConnection(
            ISmartCardDevice smartCardDevice,
            YubiKeyApplication yubiKeyApplication)
            : this(smartCardDevice, yubiKeyApplication, null)
        {
            if (yubiKeyApplication == YubiKeyApplication.Fido2)
            {
                _apduPipeline = new FidoErrorTransform(_apduPipeline);
            }

            // CCID has the concept of multiple applications. Since we cannot guarantee the
            // state of the smart card when connecting, we should always send down a connection
            // request.
            SelectApplication();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartCardConnection"/> class with the specified
        /// smart card device and application id.
        /// </summary>
        /// <param name="smartCardDevice">The smart card device to connect to.</param>
        /// <param name="applicationId">The application id of the YubiKey application to be used for the connection.</param>
        public SmartCardConnection(
            ISmartCardDevice smartCardDevice,
            byte[] applicationId)
            : this(smartCardDevice, YubiKeyApplication.Unknown, applicationId)
        {
            if (applicationId.SequenceEqual(YubiKeyApplication.Fido2.GetIso7816ApplicationId()))
            {
                _apduPipeline = new FidoErrorTransform(_apduPipeline);
            }
            else if (applicationId.SequenceEqual(YubiKeyApplication.Otp.GetIso7816ApplicationId()))
            {
                _apduPipeline = new OtpErrorTransform(_apduPipeline);
            }

            // CCID has the concept of multiple applications. Since we cannot guarantee the
            // state of the smart card when connecting, we should always send down a connection
            // request.
            SelectApplication();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartCardConnection"/> class with the specified
        /// smart card device, YubiKey application, and application id.
        /// </summary>
        /// <param name="smartCardDevice">The smart card device to connect to.</param>
        /// <param name="application">The YubiKey application to be used for the connection.</param>
        /// <param name="applicationId">The application id of the YubiKey application to be used for the connection.</param>
        protected SmartCardConnection(
            ISmartCardDevice smartCardDevice,
            YubiKeyApplication application,
            byte[]? applicationId)
        {
            if (applicationId is null && application == YubiKeyApplication.Unknown)
            {
                throw new NotSupportedException();
            }

            _yubiKeyApplication = application;
            _applicationId = applicationId;

            _smartCardConnection = smartCardDevice.Connect();

            // Set up the pipeline
            _apduPipeline = new SmartCardTransform(_smartCardConnection);
            _apduPipeline = AddResponseChainingTransform(_apduPipeline);
            _apduPipeline = new CommandChainingTransform(_apduPipeline);
        }

        public virtual TResponse SendCommand<TResponse>(IYubiKeyCommand<TResponse> yubiKeyCommand)
            where TResponse : IYubiKeyResponse
        {
            using var _ = _smartCardConnection.BeginTransaction(out bool cardWasReset);
            if (cardWasReset)
            {
                SelectApplication();
            }

            var responseApdu = _apduPipeline.Invoke(
                yubiKeyCommand.CreateCommandApdu(),
                yubiKeyCommand.GetType(),
                typeof(TResponse));

            return yubiKeyCommand.CreateResponseForApdu(responseApdu);
        }
        
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        // Allow subclasses to build a different pipeline, which means they need
        // to get the current one.
        protected IApduTransform GetPipeline() => _apduPipeline;

        /// <summary>
        /// This method is protected thus allows subclasses to build a different pipeline and set it here in the
        /// base class.
        /// It also issues a call to SelectApplication() 
        /// </summary>
        /// <param name="apduPipeline"></param>
        protected void SetPipeline(IApduTransform apduPipeline)
        {
            _apduPipeline = apduPipeline;

            SelectApplication();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _apduPipeline.Cleanup();
                    _smartCardConnection.Dispose();
                }

                _disposedValue = true;
            }
        }

        private IApduTransform AddResponseChainingTransform(IApduTransform pipeline) =>
            IsOath
                ? new OathResponseChainingTransform(pipeline)
                : new ResponseChainingTransform(pipeline);

        private void SelectApplication()
        {
            // Gets the correct select application command.
            // Note that Oath is special and has a different command than the generic SelectApplication command
            IYubiKeyCommand<ISelectApplicationResponse<ISelectApplicationData>> selectApplicationCommand =
                _yubiKeyApplication switch
                {
                    YubiKeyApplication.Oath => new Oath.Commands.SelectOathCommand(),
                    YubiKeyApplication.Unknown => new SelectApplicationCommand(_applicationId!),
                    _ => new SelectApplicationCommand(_yubiKeyApplication),
                };

            _log.LogInformation(
                "Selecting smart card application [{AID}]",
                Base16.EncodeBytes(_applicationId ?? _yubiKeyApplication.GetIso7816ApplicationId()));

            // Transmit command
            var responseApdu = _smartCardConnection.Transmit(selectApplicationCommand.CreateCommandApdu());
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

            // Set the instance property SelectApplicationData
            var response = selectApplicationCommand.CreateResponseForApdu(responseApdu);
            SelectApplicationData = response.GetData();
        }

        private bool GetIsOauth() =>
            _yubiKeyApplication == YubiKeyApplication.Oath ||
            (_applicationId != null &&
            _applicationId.SequenceEqual(
                YubiKeyApplication.Oath.GetIso7816ApplicationId()));
    }
}
