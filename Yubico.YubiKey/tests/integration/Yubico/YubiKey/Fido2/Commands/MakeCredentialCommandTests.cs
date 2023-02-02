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
using Yubico.YubiKey.TestUtilities;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2
{
    public class MakeCredentialCommandTests : SimpleIntegrationTestConnection
    {
        public MakeCredentialCommandTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Bio)
        {
        }

        [Fact]
        public void MakeCredentialCommand_Succeeds()
        {
            byte[] pin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

            var protocol = new PinUvAuthProtocolTwo();

            bool isValid = GetParams(protocol, pin, out MakeCredentialParameters makeParams);
            Assert.True(isValid);

            var cmd = new MakeCredentialCommand(makeParams);
            MakeCredentialResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
            MakeCredentialData cData = rsp.GetData();
            isValid = cData.VerifyAttestation(makeParams.ClientDataHash);
            Assert.True(isValid);
        }

        private bool GetParams(
            PinUvAuthProtocolBase protocol,
            byte[] pin,
            out MakeCredentialParameters makeParams)
        {
            byte[] clientDataHash = new byte[] {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };

            var rp = new RelyingParty("SomeRpId")
            {
                Name = "SomeRpName",
            };
            byte[] userId = new byte[] { 0x11, 0x22, 0x33, 0x44 };
            var user = new UserEntity(new ReadOnlyMemory<byte>(userId))
            {
                Name = "SomeUserName",
                DisplayName = "User",
            };

            makeParams = new MakeCredentialParameters(rp, user);

            if (!GetPinToken(protocol, pin, out byte[] pinToken))
            {
                return false;
            }

            byte[] pinUvAuthParam = protocol.AuthenticateUsingPinToken(pinToken, clientDataHash);

            makeParams.ClientDataHash = clientDataHash;
            makeParams.Protocol = protocol.Protocol;
            makeParams.PinUvAuthParam = pinUvAuthParam;

            makeParams.AddOption(AuthenticatorOptions.rk, true);
            //makeParams.AddOption("up", true);
            //makeParams.AddOption("uv", false);

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
            PinUvAuthProtocolBase protocol,
            byte[] pin,
            out byte[] pinToken)
        {
            pinToken = Array.Empty<byte>();
            if (protocol.AuthenticatorPublicKey is null)
            {
                var getKeyCmd = new GetKeyAgreementCommand(protocol.Protocol);
                GetKeyAgreementResponse getKeyRsp = Connection.SendCommand(getKeyCmd);
                if (getKeyRsp.Status != ResponseStatus.Success)
                {
                    return false;
                }

                protocol.Encapsulate(getKeyRsp.GetData());

                var getTokenCmd = new GetPinTokenCommand(protocol, pin);
                GetPinUvAuthTokenResponse getTokenRsp = Connection.SendCommand(getTokenCmd);
                if (getTokenRsp.Status == ResponseStatus.Success)
                {
                    pinToken = getTokenRsp.GetData().ToArray();
                    return true;
                }

                if (getTokenRsp.StatusWord == 0x6F31)
                {
                    return false;
                }

                var setPinCmd = new SetPinCommand(protocol, pin);
                SetPinResponse setPinRsp = Connection.SendCommand(setPinCmd);
                if (setPinRsp.Status != ResponseStatus.Success)
                {
                    return false;
                }
            }

            var cmd = new GetPinTokenCommand(protocol, pin);
            GetPinUvAuthTokenResponse rsp = Connection.SendCommand(cmd);
            if (rsp.Status == ResponseStatus.Success)
            {
                pinToken = rsp.GetData().ToArray();
                return true;
            }

            return false;
        }
    }
}
