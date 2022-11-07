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
using System.Collections.Generic;
using Xunit;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2
{
    public class SetPinCommandTests
    {
        private const int Fido2AuthPin = 1;
        private const int Fido2AuthUv = 2;

        [Fact]
        public void SetPinCommand_Succeeds()
        {
            byte[] newPin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

            IEnumerable<HidDevice> devices = HidDevice.GetHidDevices();
            Assert.NotNull(devices);

            HidDevice? deviceToUse = GetKeyAgreeCommandTests.GetFidoHid(devices);
            Assert.NotNull(deviceToUse);
            if (deviceToUse is null)
            {
                return;
            }

            var connection = new FidoConnection(deviceToUse);
            Assert.NotNull(connection);

            var resetCmd = new ResetCommand();
            ResetResponse resetRsp = connection.SendCommand(resetCmd);
            Assert.Equal(ResponseStatus.ConditionsNotSatisfied, resetRsp.Status);

            var protocol = new PinUvAuthProtocolOne();

            var cmd = new GetKeyAgreementCommand(protocol.Protocol);
            GetKeyAgreementResponse rsp = connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            CoseEcPublicKey authenticatorPubKey = rsp.GetData();
            Assert.Equal(CoseEcCurve.P256, authenticatorPubKey.Curve);

            protocol.Encapsulate(authenticatorPubKey);

            var setPinCmd = new SetPinCommand(protocol, newPin);
            SetPinResponse setPinRsp = connection.SendCommand(setPinCmd);
            Assert.Equal(ResponseStatus.Success, setPinRsp.Status);
        }

        [Fact]
        public void ChangePinCommand_Succeeds()
        {
            byte[] currentPin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };
            byte[] newPin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38 };

            IEnumerable<HidDevice> devices = HidDevice.GetHidDevices();
            Assert.NotNull(devices);

            HidDevice? deviceToUse = GetKeyAgreeCommandTests.GetFidoHid(devices);
            Assert.NotNull(deviceToUse);
            if (deviceToUse is null)
            {
                return;
            }

            var connection = new FidoConnection(deviceToUse);
            Assert.NotNull(connection);

            var protocol = new PinUvAuthProtocolOne();

            var cmd = new GetKeyAgreementCommand(protocol.Protocol);
            GetKeyAgreementResponse rsp = connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            CoseEcPublicKey authenticatorPubKey = rsp.GetData();
            Assert.Equal(CoseEcCurve.P256, authenticatorPubKey.Curve);

            protocol.Encapsulate(authenticatorPubKey);

            var changePinCmd = new ChangePinCommand(protocol, currentPin, newPin);
            ChangePinResponse changePinRsp = connection.SendCommand(changePinCmd);
            Assert.Equal(ResponseStatus.Success, changePinRsp.Status);
        }

        [Fact]
        public void GetPinTokenCommand_Succeeds()
        {
            byte[] currentPin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

            IEnumerable<HidDevice> devices = HidDevice.GetHidDevices();
            Assert.NotNull(devices);

            HidDevice? deviceToUse = GetKeyAgreeCommandTests.GetFidoHid(devices);
            Assert.NotNull(deviceToUse);
            if (deviceToUse is null)
            {
                return;
            }

            var connection = new FidoConnection(deviceToUse);
            Assert.NotNull(connection);

            var protocol = new PinUvAuthProtocolOne();

            var cmd = new GetKeyAgreementCommand(protocol.Protocol);
            GetKeyAgreementResponse rsp = connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            CoseEcPublicKey authenticatorPubKey = rsp.GetData();
            Assert.Equal(CoseEcCurve.P256, authenticatorPubKey.Curve);

            protocol.Encapsulate(authenticatorPubKey);

            var getTokenCmd = new GetPinTokenCommand(protocol, currentPin);
            GetPinUvAuthTokenResponse getTokenRsp = connection.SendCommand(getTokenCmd);
            Assert.Equal(ResponseStatus.Success, getTokenRsp.Status);

            int expectedLength = (protocol.Protocol == PinUvAuthProtocol.ProtocolOne) ? 32 : 48;
            ReadOnlyMemory<byte> encryptedToken = getTokenRsp.GetData();
            Assert.Equal(expectedLength, encryptedToken.Length);

            byte[] token = protocol.Decrypt(encryptedToken.ToArray(), 0, encryptedToken.Length);
            Assert.Equal(32, token.Length);
        }

        [Fact]
        public void GetPinUvAuthTokenUsingPinCommand_Correct()
        {
            byte[] currentPin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

            IEnumerable<HidDevice> devices = HidDevice.GetHidDevices();
            Assert.NotNull(devices);

            HidDevice? deviceToUse = GetKeyAgreeCommandTests.GetFidoHid(devices);
            Assert.NotNull(deviceToUse);
            if (deviceToUse is null)
            {
                return;
            }

            var connection = new FidoConnection(deviceToUse);
            Assert.NotNull(connection);

            bool isSupported = IsSupportedWithPermissions(connection, Fido2AuthPin);

            var protocol = new PinUvAuthProtocolTwo();

            var cmd = new GetKeyAgreementCommand(protocol.Protocol);
            GetKeyAgreementResponse rsp = connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            CoseEcPublicKey authenticatorPubKey = rsp.GetData();
            Assert.Equal(CoseEcCurve.P256, authenticatorPubKey.Curve);

            protocol.Encapsulate(authenticatorPubKey);

            var getTokenCmd = new GetPinUvAuthTokenUsingPinCommand(
                protocol, currentPin, PinUvAuthTokenPermissions.BioEnrollment, null);
            GetPinUvAuthTokenResponse getTokenRsp = connection.SendCommand(getTokenCmd);

            if (!isSupported)
            {
                Assert.Equal(ResponseStatus.Failed, getTokenRsp.Status);
                Assert.Equal(SWConstants.CommandNotAllowed, getTokenRsp.StatusWord);
                return;
            }

            Assert.Equal(ResponseStatus.Success, getTokenRsp.Status);

            int expectedLength = (protocol.Protocol == PinUvAuthProtocol.ProtocolOne) ? 32 : 48;
            ReadOnlyMemory<byte> encryptedToken = getTokenRsp.GetData();
            Assert.Equal(expectedLength, encryptedToken.Length);

            byte[] token = protocol.Decrypt(encryptedToken.ToArray(), 0, encryptedToken.Length);
            Assert.Equal(32, token.Length);
        }

        [Fact]
        public void GetPinUvAuthTokenUsingUvCommand_Correct()
        {
            IEnumerable<HidDevice> devices = HidDevice.GetHidDevices();
            Assert.NotNull(devices);

            HidDevice? deviceToUse = GetKeyAgreeCommandTests.GetFidoHid(devices);
            Assert.NotNull(deviceToUse);
            if (deviceToUse is null)
            {
                return;
            }

            var connection = new FidoConnection(deviceToUse);
            Assert.NotNull(connection);

            bool isSupported = IsSupportedWithPermissions(connection, Fido2AuthUv);

            var protocol = new PinUvAuthProtocolOne();

            var cmd = new GetKeyAgreementCommand(protocol.Protocol);
            GetKeyAgreementResponse rsp = connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            CoseEcPublicKey authenticatorPubKey = rsp.GetData();
            Assert.Equal(CoseEcCurve.P256, authenticatorPubKey.Curve);

            protocol.Encapsulate(authenticatorPubKey);

            var getTokenCmd = new GetPinUvAuthTokenUsingUvCommand(
                protocol, PinUvAuthTokenPermissions.BioEnrollment, null);
            GetPinUvAuthTokenResponse getTokenRsp = connection.SendCommand(getTokenCmd);

            if (!isSupported)
            {
                Assert.Equal(ResponseStatus.Failed, getTokenRsp.Status);
                Assert.Equal(SWConstants.CommandNotAllowed, getTokenRsp.StatusWord);
                return;
            }

            Assert.Equal(ResponseStatus.Success, getTokenRsp.Status);

            int expectedLength = (protocol.Protocol == PinUvAuthProtocol.ProtocolOne) ? 32 : 48;
            ReadOnlyMemory<byte> encryptedToken = getTokenRsp.GetData();
            Assert.Equal(expectedLength, encryptedToken.Length);

            byte[] token = protocol.Decrypt(encryptedToken.ToArray(), 0, encryptedToken.Length);
            Assert.Equal(32, token.Length);
        }

        // If auth is Fido2AuthPin (int = 1), then see if
        // GetPinUvAuthTokenUsingPin is supported.
        // Otherwise see if GetPinUvAuthTokenUsingUv is supported.
        private static bool IsSupportedWithPermissions(FidoConnection connection, int auth)
        {
            string keyToken = "pinUvAuthToken";
            string keyAuth = (auth == Fido2AuthPin) ? "clientPin" : "uv";
            var cmd = new GetInfoCommand();
            GetInfoResponse rsp = connection.SendCommand(cmd);
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
