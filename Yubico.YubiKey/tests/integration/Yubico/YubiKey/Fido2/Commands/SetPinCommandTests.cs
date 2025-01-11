// Copyright 2022 Yubico AB
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
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Fido2.PinProtocols;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2.Commands
{
    [Trait(TraitTypes.Category, TestCategories.RequiresBio)]
    public class SetPinCommandTests : SimpleIntegrationTestConnection
    {
        private const int Fido2AuthPin = 1;
        private const int Fido2AuthUv = 2;

        public SetPinCommandTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Fw5Bio)
        {
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void SetPinCommand_Succeeds()
        {
            byte[] newPin = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

            var resetCmd = new ResetCommand();
            ResetResponse resetRsp = Connection.SendCommand(resetCmd);
            Assert.Equal(ResponseStatus.ConditionsNotSatisfied, resetRsp.Status);

            var protocol = new PinUvAuthProtocolOne();

            var cmd = new GetKeyAgreementCommand(protocol.Protocol);
            GetKeyAgreementResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            CoseEcPublicKey authenticatorPubKey = rsp.GetData();
            Assert.Equal(CoseEcCurve.P256, authenticatorPubKey.Curve);

            protocol.Encapsulate(authenticatorPubKey);

            var setPinCmd = new SetPinCommand(protocol, newPin);
            SetPinResponse setPinRsp = Connection.SendCommand(setPinCmd);
            Assert.Equal(ResponseStatus.Success, setPinRsp.Status);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void ChangePinCommand_Succeeds()
        {
            byte[] currentPin = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };
            byte[] newPin = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38 };

            var protocol = new PinUvAuthProtocolOne();

            var cmd = new GetKeyAgreementCommand(protocol.Protocol);
            GetKeyAgreementResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            CoseEcPublicKey authenticatorPubKey = rsp.GetData();
            Assert.Equal(CoseEcCurve.P256, authenticatorPubKey.Curve);

            protocol.Encapsulate(authenticatorPubKey);

            var changePinCmd = new ChangePinCommand(protocol, currentPin, newPin);
            ChangePinResponse changePinRsp = Connection.SendCommand(changePinCmd);
            Assert.Equal(ResponseStatus.Success, changePinRsp.Status);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void GetPinTokenCommand_Succeeds()
        {
            byte[] currentPin = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

            var protocol = new PinUvAuthProtocolOne();

            var cmd = new GetKeyAgreementCommand(protocol.Protocol);
            GetKeyAgreementResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            CoseEcPublicKey authenticatorPubKey = rsp.GetData();
            Assert.Equal(CoseEcCurve.P256, authenticatorPubKey.Curve);

            protocol.Encapsulate(authenticatorPubKey);

            var getTokenCmd = new GetPinTokenCommand(protocol, currentPin);
            GetPinUvAuthTokenResponse getTokenRsp = Connection.SendCommand(getTokenCmd);
            Assert.Equal(ResponseStatus.Success, getTokenRsp.Status);

            int expectedLength = protocol.Protocol == PinUvAuthProtocol.ProtocolOne ? 32 : 48;
            ReadOnlyMemory<byte> encryptedToken = getTokenRsp.GetData();
            Assert.Equal(expectedLength, encryptedToken.Length);

            byte[] token = protocol.Decrypt(encryptedToken.ToArray(), 0, encryptedToken.Length);
            Assert.Equal(32, token.Length);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void GetPinUvAuthTokenUsingPinCommand_Correct()
        {
            byte[] currentPin = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

            bool isSupported = IsSupportedWithPermissions(Fido2AuthPin);

            var protocol = new PinUvAuthProtocolTwo();

            var cmd = new GetKeyAgreementCommand(protocol.Protocol);
            GetKeyAgreementResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            CoseEcPublicKey authenticatorPubKey = rsp.GetData();
            Assert.Equal(CoseEcCurve.P256, authenticatorPubKey.Curve);

            protocol.Encapsulate(authenticatorPubKey);

            var getTokenCmd = new GetPinUvAuthTokenUsingPinCommand(
                protocol, currentPin, PinUvAuthTokenPermissions.BioEnrollment, null);
            GetPinUvAuthTokenResponse getTokenRsp = Connection.SendCommand(getTokenCmd);

            if (!isSupported)
            {
                Assert.Equal(ResponseStatus.Failed, getTokenRsp.Status);
                Assert.Equal(SWConstants.CommandNotAllowed, getTokenRsp.StatusWord);
                return;
            }

            Assert.Equal(ResponseStatus.Success, getTokenRsp.Status);

            int expectedLength = protocol.Protocol == PinUvAuthProtocol.ProtocolOne ? 32 : 48;
            ReadOnlyMemory<byte> encryptedToken = getTokenRsp.GetData();
            Assert.Equal(expectedLength, encryptedToken.Length);

            byte[] token = protocol.Decrypt(encryptedToken.ToArray(), 0, encryptedToken.Length);
            Assert.Equal(32, token.Length);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void GetPinUvAuthTokenUsingUvCommand_Correct()
        {
            bool isSupported = IsSupportedWithPermissions(Fido2AuthUv);

            var protocol = new PinUvAuthProtocolOne();

            var cmd = new GetKeyAgreementCommand(protocol.Protocol);
            GetKeyAgreementResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            CoseEcPublicKey authenticatorPubKey = rsp.GetData();
            Assert.Equal(CoseEcCurve.P256, authenticatorPubKey.Curve);

            protocol.Encapsulate(authenticatorPubKey);

            var getTokenCmd = new GetPinUvAuthTokenUsingUvCommand(
                protocol, PinUvAuthTokenPermissions.BioEnrollment, null);
            GetPinUvAuthTokenResponse getTokenRsp = Connection.SendCommand(getTokenCmd);

            if (!isSupported)
            {
                Assert.Equal(ResponseStatus.Failed, getTokenRsp.Status);
                Assert.Equal(SWConstants.CommandNotAllowed, getTokenRsp.StatusWord);
                return;
            }

            Assert.Equal(ResponseStatus.Success, getTokenRsp.Status);

            int expectedLength = protocol.Protocol == PinUvAuthProtocol.ProtocolOne ? 32 : 48;
            ReadOnlyMemory<byte> encryptedToken = getTokenRsp.GetData();
            Assert.Equal(expectedLength, encryptedToken.Length);

            byte[] token = protocol.Decrypt(encryptedToken.ToArray(), 0, encryptedToken.Length);
            Assert.Equal(32, token.Length);
        }

        // If auth is Fido2AuthPin (int = 1), then see if
        // GetPinUvAuthTokenUsingPin is supported.
        // Otherwise see if GetPinUvAuthTokenUsingUv is supported.
        private bool IsSupportedWithPermissions(int auth)
        {
            string keyToken = "pinUvAuthToken";
            string keyAuth = auth == Fido2AuthPin ? "clientPin" : "uv";
            var cmd = new GetInfoCommand();
            GetInfoResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            AuthenticatorInfo deviceInfo = rsp.GetData();
            if (deviceInfo.Options is null)
            {
                return false;
            }

            if (!deviceInfo.Options!.ContainsKey(keyToken) || !deviceInfo.Options!.ContainsKey(keyAuth))
            {
                return false;
            }

            if (!deviceInfo.Options![keyToken] || !deviceInfo.Options![keyAuth])
            {
                return false;
            }

            return true;
        }
    }
}
