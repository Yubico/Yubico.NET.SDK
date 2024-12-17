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

using System;
using Xunit;
using Yubico.YubiKey.Fido2.PinProtocols;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2.Commands
{
    [Trait(TraitTypes.Category, TestCategories.RequiresBio)]
    public class HmacSecretTests : SimpleIntegrationTestConnection
    {
        private readonly byte[] _pin;
        private readonly byte[] _clientDataHash;
        private readonly RelyingParty _rp;
        private readonly byte[] _userId;
        private readonly UserEntity _user;
        private readonly PinUvAuthProtocolTwo _protocol;
        private readonly AuthenticatorInfo _deviceInfo;

        public HmacSecretTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Fw5Bio)
        {
            _pin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

            _clientDataHash = new byte[] {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };

            _rp = new RelyingParty("SomeRpId")
            {
                Name = "SomeRpName",
            };

            _userId = new byte[] { 0x11, 0x22, 0x33, 0x44 };
            _user = new UserEntity(new ReadOnlyMemory<byte>(_userId))
            {
                Name = "SomeUserName",
                DisplayName = "User",
            };

            _protocol = new PinUvAuthProtocolTwo();
            var getKeyCmd = new GetKeyAgreementCommand(_protocol.Protocol);
            GetKeyAgreementResponse getKeyRsp = Connection.SendCommand(getKeyCmd);
            Assert.True(getKeyRsp.Status == ResponseStatus.Success);
            _protocol.Encapsulate(getKeyRsp.GetData());

            var infoCmd = new GetInfoCommand();
            GetInfoResponse infoRsp = Connection.SendCommand(infoCmd);
            _deviceInfo = infoRsp.GetData();
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void MakeCredentialWithHmacSecret_Succeeds()
        {
            bool isValid = GetMakeCredentialParams(out MakeCredentialParameters makeParams);
            Assert.True(isValid);

            makeParams.AddHmacSecretExtension(_deviceInfo);

            var cmd = new MakeCredentialCommand(makeParams);
            MakeCredentialResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
            MakeCredentialData cData = rsp.GetData();
            isValid = cData.VerifyAttestation(makeParams.ClientDataHash);
            Assert.True(isValid);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void GetAssertionWithHmacSecret_Succeeds()
        {
            byte[] salt1 = {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };

            bool isValid = GetGetAssertionParams(out GetAssertionParameters assertionParams);
            Assert.True(isValid);

            assertionParams.RequestHmacSecretExtension(salt1);
            assertionParams.EncodeHmacSecretExtension(_protocol);

            var cmd = new GetAssertionCommand(assertionParams);
            GetAssertionResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
            GetAssertionData cData = rsp.GetData();
            byte[] hmacSecret = cData.AuthenticatorData.GetHmacSecretExtension(_protocol);
            Assert.True(hmacSecret.Length == 32);
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

            bool isValid = GetGetAssertionParams(out GetAssertionParameters assertionParams);
            Assert.True(isValid);

            assertionParams.RequestHmacSecretExtension(salt1, salt2);
            assertionParams.EncodeHmacSecretExtension(_protocol);

            var cmd = new GetAssertionCommand(assertionParams);
            GetAssertionResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
            GetAssertionData cData = rsp.GetData();
            byte[] hmacSecret = cData.AuthenticatorData.GetHmacSecretExtension(_protocol);
            Assert.True(hmacSecret.Length == 64);
        }

        private bool GetMakeCredentialParams(
            out MakeCredentialParameters makeParams)
        {
            makeParams = new MakeCredentialParameters(_rp, _user);

            if (!GetPinToken(PinUvAuthTokenPermissions.MakeCredential, out byte[] pinToken))
            {
                return false;
            }

            byte[] pinUvAuthParam = _protocol.AuthenticateUsingPinToken(pinToken, _clientDataHash);

            makeParams.ClientDataHash = _clientDataHash;
            makeParams.Protocol = _protocol.Protocol;
            makeParams.PinUvAuthParam = pinUvAuthParam;

            makeParams.AddOption(AuthenticatorOptions.rk, true);

            return true;
        }

        private bool GetGetAssertionParams(
            out GetAssertionParameters assertionParams)
        {
            assertionParams = new GetAssertionParameters(_rp, _clientDataHash);

            PinUvAuthTokenPermissions permissions =
                PinUvAuthTokenPermissions.GetAssertion | PinUvAuthTokenPermissions.CredentialManagement;

            if (!GetPinToken(permissions, out byte[] pinToken))
            {
                return false;
            }
            byte[] pinUvAuthParam = _protocol.AuthenticateUsingPinToken(pinToken, _clientDataHash);

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
            if (permissions.HasFlag(PinUvAuthTokenPermissions.MakeCredential)
                || permissions.HasFlag(PinUvAuthTokenPermissions.GetAssertion))
            {
                rpId = _rp.Id;
            }

            ResponseStatus status;
            do
            {
                var getTokenCmd = new GetPinUvAuthTokenUsingPinCommand(_protocol, _pin, permissions, rpId);
                GetPinUvAuthTokenResponse getTokenRsp = Connection.SendCommand(getTokenCmd);
                if (getTokenRsp.Status == ResponseStatus.Success)
                {
                    pinToken = getTokenRsp.GetData().ToArray();
                    return true;
                }

                if (getTokenRsp.StatusWord != 0x6F35)
                {
                    return false;
                }

                var setPinCmd = new SetPinCommand(_protocol, _pin);
                SetPinResponse setPinRsp = Connection.SendCommand(setPinCmd);
                status = setPinRsp.Status;

            } while (status == ResponseStatus.Success);

            return false;
        }
    }
}
