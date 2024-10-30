// Copyright 2023 Yubico AB
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

using Yubico.Core.Devices.SmartCard;
using Yubico.YubiKey.Pipelines;

namespace Yubico.YubiKey.Scp
{
    internal class ScpConnection : SmartCardConnection, IScpYubiKeyConnection
    {
        private bool _disposed;
        private readonly ScpApduTransform _scpApduTransform;

        public ScpConnection(
            ISmartCardDevice smartCardDevice,
            YubiKeyApplication yubiKeyApplication,
            ScpKeyParameters keyParameters)
            : base(smartCardDevice, yubiKeyApplication, null) // TODO Consider this, dont use this constructor
        {
            // _scpApduTransform = SetObject(yubiKeyApplication, scpKeys); TODO Is this method really needed?
            
            var previousPipeline = GetPipeline();
            var nextPipeline = new ScpApduTransform(previousPipeline, keyParameters);

            // Set parent pipeline
            SetPipeline(nextPipeline);
            nextPipeline.Setup();

            _scpApduTransform = nextPipeline;
        }
        
        // public ScpConnection(
        //     ISmartCardDevice smartCardDevice, 
        //     ReadOnlyMemory<byte> applicationId, 
        //     ScpKeyParameters scpKeys)
        //     : base(smartCardDevice, YubiKeyApplication.Unknown, applicationId.ToArray()) //TODO Consider using the Span
        // {
        //     var application = YubiKeyApplication.Unknown;
        //     if (applicationId.Span.SequenceEqual(YubiKeyApplication.Fido2.GetIso7816ApplicationId()))
        //     {
        //         application = YubiKeyApplication.Fido2;
        //     }
        //     else if (applicationId.Span.SequenceEqual(YubiKeyApplication.Otp.GetIso7816ApplicationId()))
        //     {
        //         application = YubiKeyApplication.Otp;
        //     }
        //
        //     _scpApduTransform = SetObject(application, scpKeys);
        // }
        
        public ScpKeyParameters KeyParameters => _scpApduTransform.KeyParameters;

        DataEncryptor? IScpYubiKeyConnection.DataEncryptor => _scpApduTransform.DataEncryptor;

        // private ScpApduTransform SetObject( TODO Is this needed? I dont why
        //     YubiKeyApplication application,
        //     ScpKeyParameters keyParameters)
        // {
        //     var previousPipeline = GetPipeline();
        //     var appendedPipeline = new ScpApduTransform(previousPipeline, keyParameters);
        //     
        //     // Is it even possible to connect to Fido2 and Otp with SCP?
        //     // IApduTransform apduPipeline = application switch
        //     // {
        //     //     YubiKeyApplication.Fido2 => new FidoErrorTransform(appendedPipeline),
        //     //     YubiKeyApplication.Otp => new OtpErrorTransform(appendedPipeline),
        //     //     _ => appendedPipeline
        //     // }; 
        //     
        //     // Set parent pipeline
        //     // SetPipeline(apduPipeline);
        //     // apduPipeline.Setup();
        //
        //     // Set parent pipeline
        //     SetPipeline(appendedPipeline);
        //     appendedPipeline.Setup();
        //     
        //     return appendedPipeline;
        // }
        
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _scpApduTransform.Dispose();
                    _disposed = true;
                }
            }

            base.Dispose(disposing);
        }
    }
}
