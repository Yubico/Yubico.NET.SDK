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
using System.Text;
using Xunit;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2
{
    public class PinCollectionTests
    {
        [Fact]
        [Trait("Category", "RequiresSetup")]
        public void PinOperations_Succeed()
        {
            // Assumption - the YubiKey returned has a new or reset FIDO2 application with no PIN set.
            IYubiKeyDevice yubiKey = YubiKeyDevice.FindAll().First();

            using (var fido2 = new Fido2Session(yubiKey))
            {
                byte[] pin1 = Encoding.UTF8.GetBytes("12345");
                byte[] pin2 = Encoding.UTF8.GetBytes("abcde");

                fido2.KeyCollector = req =>
                {
                    switch (req.Request)
                    {
                        case KeyEntryRequest.SetFido2Pin:
                            req.SubmitValue(pin1);
                            break;
                        case KeyEntryRequest.ChangeFido2Pin:
                            if (req.IsRetry)
                            {
                                req.SubmitValues(pin1, pin2);
                            }
                            else
                            {
                                req.SubmitValues(pin2, pin1);
                            }
                            break;
                        case KeyEntryRequest.VerifyFido2Pin:
                            if (req.IsRetry)
                            {
                                req.SubmitValue(pin2);
                            }
                            else
                            {
                                req.SubmitValue(pin1);
                            }
                            break;
                    }

                    return true;
                };

                fido2.SetPin();
                fido2.ChangePin();
                fido2.VerifyPin();

                bool isValid = fido2.TryChangePin(pin1, pin2);
                Assert.False(isValid);
                isValid = fido2.TryChangePin(pin2, pin1);
                Assert.True(isValid);

                isValid = fido2.TryVerifyPin(pin2, null, null, out _, out _);
                Assert.False(isValid);
                isValid = fido2.TryVerifyPin(pin1, null, null, out _, out _);
                Assert.True(isValid);
            }
        }

        [Fact]
        [Trait("Category", "RequiresSetup")]
        public void UvOperations_Succeed()
        {
            // Test assumptions: PIN is already set to 123456 (UTF-8 chars, not the number `123456`)
            // Test assumptions: A fingerprint is registered on the key.

            IYubiKeyDevice yubiKey = YubiKeyDevice.FindAll().First();

            using (var fido2 = new Fido2Session(yubiKey))
            {
                fido2.KeyCollector = KeyCollector;
                fido2.VerifyUv(PinUvAuthTokenPermissions.MakeCredential | PinUvAuthTokenPermissions.GetAssertion, "relyingParty1");
            }
        }

        [Fact]
        [Trait("Category", "RequiresSetup")]
        public void InvalidPinFollowedByValidPin_Succeeds()
        {
            // Test assumption: PIN is already set to 123456 (UTF-8 chars, not the number `123456`)
            IYubiKeyDevice yubiKey = YubiKeyDevice.FindAll().First();

            byte[] invalidPin = Encoding.UTF8.GetBytes("44444");
            byte[] validPin = Encoding.UTF8.GetBytes("123456");

            using (var fido2 = new Fido2Session(yubiKey))
            {
                bool success = fido2.TryVerifyPin(
                    invalidPin,
                    PinUvAuthTokenPermissions.CredentialManagement,
                    "",
                    out _, out _);

                Assert.False(success);

                success = fido2.TryVerifyPin(
                    validPin,
                    PinUvAuthTokenPermissions.CredentialManagement,
                    "",
                    out _, out _);

                Assert.True(success);
            }
        }

        private bool KeyCollector(KeyEntryData arg)
        {
            switch (arg.Request)
            {
                case KeyEntryRequest.TouchRequest:
                    Console.WriteLine("YubiKey requires touch");
                    break;
                case KeyEntryRequest.VerifyFido2Pin:
                    arg.SubmitValue(Encoding.UTF8.GetBytes("123456"));
                    break;
                case KeyEntryRequest.VerifyFido2Uv:
                    Console.WriteLine("Bio touch needed.");
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
