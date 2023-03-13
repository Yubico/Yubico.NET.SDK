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
        static readonly byte[] _clientDataHash = {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
        };

        static readonly RelyingParty _rp = new RelyingParty("relyingparty1");

        [Fact]
        public void MakeCredential_NonDiscoverable_GetAssertion_Succeeds()
        {
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

                var mcParams = new MakeCredentialParameters(_rp, userId)
                {
                    ClientDataHash = _clientDataHash
                };

                MakeCredentialData mcData = fido2.MakeCredential(mcParams);

                Assert.True(mcData.VerifyAttestation(_clientDataHash));

                // Call GetAssertion
                var gaParams = new GetAssertionParameters(_rp, _clientDataHash);

                gaParams.AllowCredential(mcData.AuthenticatorData.CredentialId!);

                IReadOnlyList<GetAssertionData> assertions = fido2.GetAssertions(gaParams);

                Assert.Equal(1, assertions.Count);
                Assert.Equal(1, assertions[0].NumberOfCredentials);
            }
        }

        [Fact]
        public void MakeCredential_MultipleCredentials_GetAssertion_ReturnsMultipleAssertions()
        {
            // Test assumptions: PIN is already set to 123456 (UTF-8 chars, not the number `123456`)
            IYubiKeyDevice yubiKeyDevice = YubiKeyDevice.FindByTransport(Transport.HidFido).First();

            using (var fido2 = new Fido2Session(yubiKeyDevice))
            {
                // Set up a key collector
                fido2.KeyCollector = KeyCollector;

                // Verify the PIN
                fido2.VerifyPin();

                // Call MakeCredential
                var user1 = new UserEntity(new byte[] { 1, 2, 3, 4 })
                {
                    Name = "TestUser1",
                    DisplayName = "Test User"
                };

                var user2 = new UserEntity(new byte[] { 5, 6, 7, 8 })
                {
                    Name = "TestUser2",
                    DisplayName = "Test User 2"
                };

                var mcParams1 = new MakeCredentialParameters(_rp, user1)
                {
                    ClientDataHash = _clientDataHash
                };

                var mcParams2 = new MakeCredentialParameters(_rp, user2)
                {
                    ClientDataHash = _clientDataHash
                };

                MakeCredentialData mcData = fido2.MakeCredential(mcParams1);
                Assert.True(mcData.VerifyAttestation(_clientDataHash));
                CredentialId cred1 = mcData.AuthenticatorData.CredentialId!;

                mcData = fido2.MakeCredential(mcParams2);
                Assert.True(mcData.VerifyAttestation(_clientDataHash));
                CredentialId cred2 = mcData.AuthenticatorData.CredentialId!;

                // Call GetAssertion
                var gaParams = new GetAssertionParameters(_rp, _clientDataHash);

                gaParams.AllowCredential(cred1);
                gaParams.AllowCredential(cred2);

                IReadOnlyList<GetAssertionData> assertions = fido2.GetAssertions(gaParams);

                Assert.Equal(2, assertions.Count);
                Assert.Equal(2, assertions[0].NumberOfCredentials);
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
