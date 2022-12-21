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
using System.Linq;
using Xunit;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    public class MakeCredentialBlobTests
    {
        [Fact]
        public void CredBlobExtension_Correct()
        {
            IYubiKeyDevice yubiKeyDevice = IntegrationTestDeviceEnumeration.GetTestDevices()[0];
            IYubiKeyConnection connection = yubiKeyDevice.Connect(YubiKeyApplication.Fido2);

            var getInfoCmd = new GetInfoCommand();
            GetInfoResponse getInfoRsp = connection.SendCommand(getInfoCmd);
            Assert.Equal(ResponseStatus.Success, getInfoRsp.Status);
            AuthenticatorInfo authInfo = getInfoRsp.GetData();
            Assert.Equal(32, authInfo.MaximumCredentialBlobLength);

            int maxCredBlobLength = authInfo.MaximumCredentialBlobLength ?? 0;
            Assert.NotNull(authInfo.Extensions);
            if (!(authInfo.Extensions is null))
            {
                bool isValid = authInfo.Extensions.Contains<string>("credBlob") && (maxCredBlobLength > 0);
                Assert.True(isValid);
            }
        }

        [Fact]
        public void MakeCredentialCommand_Succeeds()
        {
            var pin = new ReadOnlyMemory<byte>(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 });

            IYubiKeyDevice yubiKey = YubiKeyDevice.FindAll().First();

            using (var fido2Session = new Fido2Session(yubiKey))
            {
                _ = fido2Session.TrySetPin(pin);
                bool isValid = fido2Session.TryVerifyPin(pin, null, null, out int? retriesRemaining, out bool? reboot);
                Assert.True(isValid);

                isValid = SupportsLargeBlobs(fido2Session.AuthenticatorInfo);
                Assert.True(isValid);

                isValid = GetParams(fido2Session, out MakeCredentialParameters makeParams);
                Assert.True(isValid);
            }

//            var cmd = new MakeCredentialCommand(makeParams);
//            MakeCredentialResponse rsp = connection.SendCommand(cmd);
//            Assert.Equal(ResponseStatus.Success, rsp.Status);
//            MakeCredentialData cData = rsp.GetData();
//            isValid = cData.VerifyAttestation(makeParams.ClientDataHash);
//            Assert.True(isValid);
        }

        private bool SupportsLargeBlobs(AuthenticatorInfo authenticatorInfo)
        {
            if (!(authenticatorInfo.Extensions is null))
            {
                if (authenticatorInfo.Extensions.Contains<string>("largeBlobs"))
                {
                    return true;
                }
            }

            return false;
        }

        private bool GetParams(
            Fido2Session fido2Session,
            out MakeCredentialParameters makeParams)
        {
            byte[] clientDataHash = new byte[] {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };
            byte[] arbitraryData = new byte[] {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A
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

            if (fido2Session.AuthToken is null)
            {
                return false;
            }
            ReadOnlyMemory<byte> token = (ReadOnlyMemory<byte>)fido2Session.AuthToken;
            byte[] pinUvAuthParam = fido2Session.AuthProtocol.AuthenticateUsingPinToken(
                token.ToArray(), clientDataHash);

            makeParams.ClientDataHash = clientDataHash;
            makeParams.Protocol = fido2Session.AuthProtocol.Protocol;
            makeParams.PinUvAuthParam = pinUvAuthParam;

            makeParams.AddOption(AuthenticatorOptions.rk, true);
            makeParams.AddExtension("largeBlob", arbitraryData);

            return true;
        }
    }
}
