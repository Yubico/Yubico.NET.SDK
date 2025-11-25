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
using System.Linq;
using Yubico.Core.Devices.SmartCard;
using Yubico.YubiKey.Pipelines;
using Yubico.YubiKey.Scp;
using Yubico.YubiKey.Scp03;

namespace Yubico.YubiKey
{
    [Obsolete("Obsolete")]
    internal class Scp03Connection : SmartCardConnection, IScp03YubiKeyConnection
    {
        private bool _disposed;

        // If an Scp03ApduTransform is used, keep this copy so it can be disposed.
        private readonly Scp03ApduTransform _scp03ApduTransform;

        public Scp03Connection(
            ISmartCardDevice smartCardDevice,
            YubiKeyApplication yubiKeyApplication,
            Scp03.StaticKeys scp03Keys)
            : base(smartCardDevice, yubiKeyApplication, null)
        {
            _scp03ApduTransform = SetObject(yubiKeyApplication, scp03Keys);
        }

        public Scp03Connection(ISmartCardDevice smartCardDevice, byte[] applicationId, Scp03.StaticKeys scp03Keys)
            : base(smartCardDevice, YubiKeyApplication.Unknown, applicationId)
        {
            var setError = YubiKeyApplication.Unknown;
            if (applicationId.SequenceEqual(YubiKeyApplication.Fido2.GetIso7816ApplicationId()))
            {
                setError = YubiKeyApplication.Fido2;
            }
            else if (applicationId.SequenceEqual(YubiKeyApplication.Otp.GetIso7816ApplicationId()))
            {
                setError = YubiKeyApplication.Otp;
            }

            _scp03ApduTransform = SetObject(setError, scp03Keys);
        }

        private Scp03ApduTransform SetObject(
            YubiKeyApplication setError,
            Yubico.YubiKey.Scp03.StaticKeys scp03Keys)
        {
            var scp03ApduTransform = new Scp03ApduTransform(GetPipeline(), scp03Keys);
            IApduTransform apduPipeline = scp03ApduTransform;

            if (setError == YubiKeyApplication.Fido2)
            {
                apduPipeline = new FidoErrorTransform(apduPipeline);
            }
            else if (setError == YubiKeyApplication.Otp)
            {
                apduPipeline = new OtpErrorTransform(apduPipeline);
            }

            SetPipeline(apduPipeline);

            apduPipeline.Setup();

            _disposed = false;

            return scp03ApduTransform;
        }

        public Scp03.StaticKeys GetScp03Keys() => _scp03ApduTransform.Scp03Keys;

        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _scp03ApduTransform.Dispose();
                _disposed = true;
            }
            
            base.Dispose(disposing);
        }
    }
}
