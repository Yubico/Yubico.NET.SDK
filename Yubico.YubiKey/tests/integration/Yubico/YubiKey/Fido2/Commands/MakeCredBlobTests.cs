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

namespace Yubico.YubiKey.Fido2.Commands
{
    public class MakeCredBlobTests : NeedPinToken
    {
        private readonly byte[] _clientDataHash = new byte[] {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
        };

        private readonly byte[] _credBlobValue = new byte[] {
            0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
            0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
            0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
            0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50
        };

        private readonly RelyingParty _rp = new RelyingParty("SomeRpId")
        {
            Name = "SomeRpName",
        };

        private readonly UserEntity _user = new UserEntity(new ReadOnlyMemory<byte>(new byte[] { 0x11, 0x22, 0x33, 0x44 }))
        {
            Name = "SomeUserName",
            DisplayName = "User",
        };

        public MakeCredBlobTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Bio, null)
        {
        }

        [Fact]
        public void MakeCredentialBlob_Succeeds()
        {
            var protocol = new PinUvAuthProtocolTwo();

            var getInfoCmd = new GetInfoCommand();
            GetInfoResponse getInfoRsp = Connection.SendCommand(getInfoCmd);
            Assert.Equal(ResponseStatus.Success, getInfoRsp.Status);
            AuthenticatorInfo authInfo = getInfoRsp.GetData();

            bool isValid = GetParamsMake(authInfo, protocol, out MakeCredentialParameters makeParams);
            Assert.True(isValid);

            var cmd = new MakeCredentialCommand(makeParams);
            MakeCredentialResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
            MakeCredentialData cData = rsp.GetData();
            isValid = cData.VerifyAttestation(makeParams.ClientDataHash);
            Assert.True(isValid);

            isValid = GetParamsAssert(protocol, out GetAssertionParameters assertionParams);
            Assert.True(isValid);
            var getAssertionCmd = new GetAssertionCommand(assertionParams);
            GetAssertionResponse getAssertionRsp = Connection.SendCommand(getAssertionCmd);
            Assert.Equal(ResponseStatus.Success, getAssertionRsp.Status);
            GetAssertionData aData = getAssertionRsp.GetData();
            Assert.NotNull(cData.AuthenticatorData.CredentialPublicKey);
            if (!(cData.AuthenticatorData.CredentialPublicKey is null))
            {
                isValid = aData.VerifyAssertion(cData.AuthenticatorData.CredentialPublicKey, _clientDataHash);
                Assert.True(isValid);
            }
            isValid = CheckCredBlob(aData);
            Assert.True(isValid);
        }

        private bool GetParamsMake(
            AuthenticatorInfo authInfo,
            PinUvAuthProtocolBase protocol,
            out MakeCredentialParameters makeParams)
        {
            makeParams = new MakeCredentialParameters(_rp, _user);

            if (!GetPinToken(protocol, PinUvAuthTokenPermissions.None, out byte[] pinToken))
            {
                return false;
            }

            byte[] pinUvAuthParam = protocol.AuthenticateUsingPinToken(pinToken, _clientDataHash);

            makeParams.ClientDataHash = _clientDataHash;
            makeParams.Protocol = protocol.Protocol;
            makeParams.PinUvAuthParam = pinUvAuthParam;

            makeParams.AddOption(AuthenticatorOptions.rk, true);
            makeParams.AddCredBlobExtension(_credBlobValue, authInfo);

            return true;
        }

        private bool GetParamsAssert(
            PinUvAuthProtocolBase protocol,
            out GetAssertionParameters assertionParams)
        {
            assertionParams = new GetAssertionParameters(_rp, _clientDataHash);

            if (!GetPinToken(protocol, PinUvAuthTokenPermissions.None, out byte[] pinToken))
            {
                return false;
            }

            byte[] pinUvAuthParam = protocol.AuthenticateUsingPinToken(pinToken, _clientDataHash);

            assertionParams.Protocol = protocol.Protocol;
            assertionParams.PinUvAuthParam = pinUvAuthParam;

            assertionParams.AddOption("up", true);
            assertionParams.AddExtension("credBlob", new byte[] { 0xF5 });

            return true;
        }

        private bool CheckCredBlob(GetAssertionData aData)
        {
            byte[] credBlobData = aData.AuthenticatorData.GetCredBlobExtension();
            if (MemoryExtensions.SequenceEqual<byte>(credBlobData, _credBlobValue))
            {
                return true;
            }

            return false;
        }
    }
}
