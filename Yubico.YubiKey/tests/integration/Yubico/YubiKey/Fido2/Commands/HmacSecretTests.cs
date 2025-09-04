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
using Xunit;
using Yubico.YubiKey.Fido2.PinProtocols;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2.Commands
{
    public class HmacSecretTests : SimpleIntegrationTestConnection
    {

        private readonly PinUvAuthProtocolTwo _protocol;
        private readonly AuthenticatorInfo _deviceInfo;

        public HmacSecretTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Fw5)
        {
            _protocol = new PinUvAuthProtocolTwo();
            var getKeyRsp = Connection.SendCommand(new GetKeyAgreementCommand(_protocol.Protocol));
            Assert.Equal(ResponseStatus.Success, getKeyRsp.Status);
            
            _protocol.Encapsulate(getKeyRsp.GetData());

            var infoRsp = Connection.SendCommand(new GetInfoCommand());
            _deviceInfo = infoRsp.GetData();
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void MakeCredentialWithHmacSecret_Succeeds()
        {
            var isValid = GetMakeCredentialParams(out var mcParams);
            Assert.True(isValid);

            mcParams.AddHmacSecretExtension(_deviceInfo);

            var cmd = new MakeCredentialCommand(mcParams);
            var rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
            var cData = rsp.GetData();
            isValid = cData.VerifyAttestation(mcParams.ClientDataHash);
            Assert.True(isValid);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void GetAssertion_WithHmacSecret_Succeeds()
        {
            byte[] salt1 = {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };

            var isValid = GetGetAssertionParams(out var assertionParams);
            Assert.True(isValid);

            assertionParams.RequestHmacSecretExtension(salt1);
            assertionParams.EncodeHmacSecretExtension(_protocol);

            var cmd = new GetAssertionCommand(assertionParams);
            var rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
            
            var cData = rsp.GetData();
            var hmacSecret = cData.AuthenticatorData.GetHmacSecretExtension(_protocol);
            Assert.Equal(32, hmacSecret.Length);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void GetAssertionWithTwoHmacSecrets_Succeeds()
        {
            byte[] salt1 = {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };
            byte[] salt2 = {
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18
            };

            var isValid = GetGetAssertionParams(out var assertionParams);
            Assert.True(isValid);

            assertionParams.RequestHmacSecretExtension(salt1, salt2);
            assertionParams.EncodeHmacSecretExtension(_protocol);

            var cmd = new GetAssertionCommand(assertionParams);
            var rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
            var cData = rsp.GetData();
            var hmacSecret = cData.AuthenticatorData.GetHmacSecretExtension(_protocol);
            Assert.Equal(64, hmacSecret.Length);
        }

        private bool GetMakeCredentialParams(out MakeCredentialParameters makeParams)
        {
            makeParams = new MakeCredentialParameters(FidoSessionIntegrationTestBase.Rp, FidoSessionIntegrationTestBase.UserEntity);

            if (!GetPinToken(PinUvAuthTokenPermissions.MakeCredential, out var pinToken))
            {
                return false;
            }

            var pinUvAuthParam = _protocol.AuthenticateUsingPinToken(pinToken, FidoSessionIntegrationTestBase.ClientDataHash);

            makeParams.ClientDataHash = FidoSessionIntegrationTestBase.ClientDataHash;
            makeParams.Protocol = _protocol.Protocol;
            makeParams.PinUvAuthParam = pinUvAuthParam;

            makeParams.AddOption(AuthenticatorOptions.rk, true);

            return true;
        }

        private bool GetGetAssertionParams(
            out GetAssertionParameters assertionParams)
        {
            assertionParams = new GetAssertionParameters(FidoSessionIntegrationTestBase.Rp, FidoSessionIntegrationTestBase.ClientDataHash);

            var permissions =
                PinUvAuthTokenPermissions.GetAssertion | PinUvAuthTokenPermissions.CredentialManagement;

            if (!GetPinToken(permissions, out var pinToken))
            {
                return false;
            }
            var pinUvAuthParam = _protocol.AuthenticateUsingPinToken(pinToken, FidoSessionIntegrationTestBase.ClientDataHash);

            assertionParams.Protocol = _protocol.Protocol;
            assertionParams.PinUvAuthParam = pinUvAuthParam;

            return true;
        }

        // This will get a PIN token.
        // To do so, it will check the PinProtocol object. If it is not yet in a
        // post-Encapsulate state, it will get the YubiKey's public key, then
        // call Encapsulate (the input object will be updated).
        // Next, it will get a PIN token using the given PIN.
        // If that works, return the PIN token (the out arg).
        // If it doesn't work because there is no PIN set, set the PIN and then
        // get the PIN token.
        // If it doesn't work because the PIN was wron, return false.
        private bool GetPinToken(
            PinUvAuthTokenPermissions permissions,
            out byte[] pinToken)
        {
            pinToken = Array.Empty<byte>();

            string? rpId = null;
            if (permissions.HasFlag(PinUvAuthTokenPermissions.MakeCredential) ||
                permissions.HasFlag(PinUvAuthTokenPermissions.GetAssertion))
            {
                rpId = FidoSessionIntegrationTestBase.Rp.Id;
            }

            ResponseStatus status;
            do
            {
                var getTokenCmd = new GetPinUvAuthTokenUsingPinCommand(_protocol, FidoSessionIntegrationTestBase.ComplexPin, permissions, rpId);
                var getTokenRsp = Connection.SendCommand(getTokenCmd);
                if (getTokenRsp.Status == ResponseStatus.Success)
                {
                    pinToken = getTokenRsp.GetData().ToArray();
                    return true;
                }

                if (getTokenRsp.StatusWord != 0x6F35)
                {
                    return false;
                }

                var setPinCmd = new SetPinCommand(_protocol, FidoSessionIntegrationTestBase.ComplexPin);
                var setPinRsp = Connection.SendCommand(setPinCmd);
                status = setPinRsp.Status;

            } while (status == ResponseStatus.Success);

            return false;
        }
    }
}
