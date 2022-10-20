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
using System.Linq;
using System.Text;
using Xunit;

namespace Yubico.YubiKey.Fido2
{
    public class MakeCredentialGetAssertionTests
    {
        [Fact]
        public void MakeCredential_NonDiscoverable_GetAssertion_Succeeds()
        {
            byte[] clientDataHash = {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };

            var rp = new RelyingParty("relyingparty1");

            // Test assumptions: PIN is already set to 123456 (UTF-8 chars, not the number `123456`)
            IYubiKeyDevice yubiKeyDevice = YubiKeyDevice.FindByTransport(Transport.HidFido).First();

            using (var fido2 = new Fido2Session(yubiKeyDevice))
            {
                // Set up a key collector
                fido2.KeyCollector = KeyCollector;

                // Verify the PIN
                fido2.VerifyPin();

                // Call MakeCredential
                var userId = new UserEntity(new byte[] { 1, 2, 3, 4 })
                {
                    Name = "TestUser1",
                    DisplayName = "Test User"
                };

                var mcParams = new MakeCredentialParameters(rp, userId)
                {
                    ClientDataHash = clientDataHash
                };

                MakeCredentialData mcData = fido2.MakeCredential(mcParams);

                Assert.True(mcData.VerifyAttestation(clientDataHash));

                // Call GetAssertion
                var gaParams = new GetAssertionParameters(rp, clientDataHash);

                var credentialId = new CredentialId()
                {
                    Id = mcData.AuthenticatorData.CredentialId!.Value
                };

                gaParams.AllowCredential(credentialId);

                ICollection<GetAssertionData> assertions = fido2.GetAssertion(gaParams);

                Assert.Equal(1, assertions.Count);
                Assert.Equal(1, assertions.First().NumberOfCredentials ?? 1);
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
