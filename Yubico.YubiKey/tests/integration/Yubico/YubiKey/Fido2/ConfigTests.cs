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
using System.Text;
using System.Collections.Generic;
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    public class ConfigTests : SimpleIntegrationTestConnection
    {
        public ConfigTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Fw5)
        {
        }

        [Fact]
        public void EnableEnterpriseAttestation_Succeeds()
        {
            using (var fido2Session = new Fido2Session(Device))
            {
                fido2Session.KeyCollector = LocalKeyCollector;

                OptionValue optionValue = fido2Session.AuthenticatorInfo.GetOptionValue("ep");

                bool expectedResult = false;
                if ((optionValue == OptionValue.True) || (optionValue == OptionValue.False))
                {
                    expectedResult = true;
                }

                bool isSet = fido2Session.TryEnableEnterpriseAttestation();

                Assert.Equal(expectedResult, isSet);
            }
        }

        [Fact]
        public void ToggleAlwaysUv_Succeeds()
        {
            using (var fido2Session = new Fido2Session(Device))
            {
                fido2Session.KeyCollector = LocalKeyCollector;

                OptionValue optionValue = fido2Session.AuthenticatorInfo.GetOptionValue("alwaysUv");

                bool expectedResult = false;
                OptionValue expectedValue = optionValue switch
                {
                    OptionValue.True => OptionValue.False,
                    OptionValue.False => OptionValue.True,
                    _ => OptionValue.NotSupported,
                };

                if (expectedValue != OptionValue.NotSupported)
                {
                    expectedResult = true;
                }

                bool isSet = fido2Session.TryToggleAlwaysUv();

                optionValue = fido2Session.AuthenticatorInfo.GetOptionValue("alwaysUv");
                Assert.Equal(expectedResult, isSet);
                Assert.Equal(expectedValue, optionValue);
            }
        }

        [Fact]
        public void SetMinPinLen_Succeeds()
        {
            using (var fido2Session = new Fido2Session(Device))
            {
                fido2Session.KeyCollector = LocalKeyCollector;

                OptionValue optionValue = fido2Session.AuthenticatorInfo.GetOptionValue("setMinPINLength");

                bool expectedResult = optionValue == OptionValue.True;

                bool isSet = fido2Session.TrySetPinConfig(6, null, null);
                Assert.Equal(expectedResult, isSet);
                if (isSet)
                {
                    Assert.NotNull(fido2Session.AuthenticatorInfo.ForcePinChange);
                    Assert.True(fido2Session.AuthenticatorInfo.ForcePinChange!);
                }
            }
        }

        [Fact]
        public void ForceChangePin_Succeeds()
        {
            using (var fido2Session = new Fido2Session(Device))
            {
                fido2Session.KeyCollector = LocalKeyCollector;

                Assert.NotNull(fido2Session.AuthenticatorInfo.ForcePinChange);
                Assert.False(fido2Session.AuthenticatorInfo.ForcePinChange!);

                OptionValue optionValue = fido2Session.AuthenticatorInfo.GetOptionValue("setMinPINLength");

                bool expectedResult = optionValue == OptionValue.True;

                bool isSet = fido2Session.TrySetPinConfig(null, null, true);
                Assert.Equal(expectedResult, isSet);
                if (isSet)
                {
                    Assert.NotNull(fido2Session.AuthenticatorInfo.ForcePinChange);
                    Assert.True(fido2Session.AuthenticatorInfo.ForcePinChange!);
                }
            }
        }

        [Fact]
        public void SetRpId_Succeeds()
        {
            using (var fido2Session = new Fido2Session(Device))
            {
                fido2Session.KeyCollector = LocalKeyCollector;

                OptionValue optionValue = fido2Session.AuthenticatorInfo.GetOptionValue("setMinPINLength");
                bool isSupported = fido2Session.AuthenticatorInfo.IsExtensionSupported("minPinLength");

                bool expectedResult = (optionValue == OptionValue.True) && isSupported;

                var rpList = new List<string>(1)
                {
                    "rpidOne"
                };
                bool isSet = fido2Session.TrySetPinConfig(null, rpList, null);
                Assert.Equal(expectedResult, isSet);

                if (isSet)
                {
                    isSet = VerifyExtension(fido2Session);
                    Assert.True(isSet);
                }
            }
        }

        private bool VerifyExtension(Fido2Session fido2Session)
        {
            byte[] clientDataHash = {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };
            var rp = new RelyingParty("rpidOne");
            var user1 = new UserEntity(new byte[] { 1, 2, 3, 4 })
            {
                Name = "TestUser",
                DisplayName = "Test User"
            };

            var mcParams = new MakeCredentialParameters(rp, user1)
            {
                ClientDataHash = clientDataHash
            };
            mcParams.AddOption(AuthenticatorOptions.rk, true);
            mcParams.AddExtension("minPinLength", new byte[] { 0xF5 });

            MakeCredentialData mcData = fido2Session.MakeCredential(mcParams);

            if (mcData.AuthenticatorData.Extensions is null)
            {
                return false;
            }

            bool isValid = mcData.AuthenticatorData.Extensions!.TryGetValue("minPinLength", out byte[]? eValue);
            if (isValid)
            {
                isValid = eValue![0] == 4;
            }

            return isValid;
       }

        private bool LocalKeyCollector(KeyEntryData arg)
        {
            switch (arg.Request)
            {
                case KeyEntryRequest.VerifyFido2Pin:
                    arg.SubmitValue(Encoding.UTF8.GetBytes("123456"));
                    break;
                case KeyEntryRequest.VerifyFido2Uv:
                    Console.WriteLine("Fingerprint requested.");
                    break;
                case KeyEntryRequest.TouchRequest:
                    Console.WriteLine("Touch requested.");
                    break;
                case KeyEntryRequest.Release:
                    break;
                default:
                    throw new NotSupportedException("Not supported by this test");
            }

            return true;
        }
    }
}
