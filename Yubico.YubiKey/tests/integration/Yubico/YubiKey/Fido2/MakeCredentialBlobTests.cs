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
            var yubiKeyDevice = IntegrationTestDeviceEnumeration.GetTestDevices()[index: 0];
            var connection = yubiKeyDevice.Connect(YubiKeyApplication.Fido2);

            var getInfoCmd = new GetInfoCommand();
            var getInfoRsp = connection.SendCommand(getInfoCmd);
            Assert.Equal(ResponseStatus.Success, getInfoRsp.Status);
            var authInfo = getInfoRsp.GetData();
            Assert.Equal(expected: 32,
                authInfo
                    .MaximumCredentialBlobLength); /* Assert.Equal() Failure: Values differExpected: 32 Actual: null */

            var maxCredBlobLength = authInfo.MaximumCredentialBlobLength ?? 0;
            Assert.NotNull(authInfo.Extensions);
            if (!(authInfo.Extensions is null))
            {
                var isValid = authInfo.Extensions.Contains<string>("credBlob") && maxCredBlobLength > 0;
                Assert.True(isValid);
            }
        }

        [Fact]
        public void MakeCredentialCommand_Succeeds()
        {
            var pin = new ReadOnlyMemory<byte>(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 });

            var yubiKey = YubiKeyDevice.FindAll().First();

            using (var fido2Session = new Fido2Session(yubiKey))
            {
                _ = fido2Session.TrySetPin(pin);
                var isValid = fido2Session.TryVerifyPin(pin, permissions: null, relyingPartyId: null,
                    out var retriesRemaining, out var reboot);
                Assert.True(isValid);

                isValid = SupportsLargeBlobs(fido2Session.AuthenticatorInfo);
                Assert.True(isValid); /*Xunit.Sdk.TrueException
Assert.True() Failure
Expected: True
Actual:   False*/

                isValid = GetParams(fido2Session, out var makeParams);
                Assert.True(isValid);
            }
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
            byte[] clientDataHash =
            {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };
            byte[] arbitraryData =
            {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A
            };

            var rp = new RelyingParty("SomeRpId")
            {
                Name = "SomeRpName"
            };
            byte[] userId = { 0x11, 0x22, 0x33, 0x44 };
            var user = new UserEntity(new ReadOnlyMemory<byte>(userId))
            {
                Name = "SomeUserName",
                DisplayName = "User"
            };

            makeParams = new MakeCredentialParameters(rp, user);

            if (fido2Session.AuthToken is null)
            {
                return false;
            }

            var token = (ReadOnlyMemory<byte>)fido2Session.AuthToken;
            var pinUvAuthParam = fido2Session.AuthProtocol.AuthenticateUsingPinToken(
                token.ToArray(), clientDataHash);

            makeParams.ClientDataHash = clientDataHash;
            makeParams.Protocol = fido2Session.AuthProtocol.Protocol;
            makeParams.PinUvAuthParam = pinUvAuthParam;

            makeParams.AddOption(AuthenticatorOptions.rk, optionValue: true);
            makeParams.AddExtension("largeBlob", arbitraryData);

            return true;
        }
    }
}
