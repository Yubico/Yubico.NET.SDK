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
        public void PinOperations_Succeed()
        {
            // Assumption - the YubiKey returned has a new or reset FIDO2 application with no PIN set.
            IYubiKeyDevice yubiKey = YubiKeyDevice.FindAll().First();

            using (var fido2 = new Fido2Session(yubiKey))
            {
                var pin1 = Encoding.UTF8.GetBytes("12345");
                var pin2 = Encoding.UTF8.GetBytes("abcde");

                fido2.KeyCollector = req =>
                {
                    switch (req.Request)
                    {
                        case KeyEntryRequest.SetFido2Pin:
                            req.SubmitValue(pin1);
                            break;
                        case KeyEntryRequest.ChangeFido2Pin:
                            req.SubmitValues(pin1, pin2);
                            break;
                        case KeyEntryRequest.VerifyFido2Pin:
                            req.SubmitValue(pin2);
                            break;
                    }

                    return true;
                };

                fido2.SetPin();
                fido2.ChangePin();
                fido2.VerifyPin();
            }
        }

        [Fact]
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
