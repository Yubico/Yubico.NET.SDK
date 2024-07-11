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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    public class MakeCredentialGetAssertionTests
    {
        private static readonly byte[] _clientDataHash =
        {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
        };

        private static readonly RelyingParty _rp = new RelyingParty("relyingparty1");

        // This test requires user to touch the device.
        [Fact]
        [Trait("Category", "RequiresTouch")]
        public void MakeCredential_NonDiscoverable_GetAssertion_Succeeds()
        {
            var yubiKeyDevice = YubiKeyDevice.FindByTransport(Transport.HidFido).First();

            var isValid = Fido2ResetForTest.DoReset(yubiKeyDevice.SerialNumber);
            Assert.True(isValid);

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

                var mcData = fido2.MakeCredential(mcParams);

                Assert.True(mcData.VerifyAttestation(_clientDataHash));

                // Call GetAssertion
                var gaParams = new GetAssertionParameters(_rp, _clientDataHash);

                gaParams.AllowCredential(mcData.AuthenticatorData.CredentialId!);

                var assertions = fido2.GetAssertions(gaParams);

                var assertion = Assert.Single(assertions);
                Assert.Equal(expected: 1, assertion.NumberOfCredentials);
                // Assert.Equal() Failure: Values differ
                // Expected: 1
                // Actual:   null

                // assertion.NumberOfCredentials
                // The total number of credentials found on the YubiKey for the relying
                // party. This is optional and can be null. If null, then there is only
                // one credential.
                //
            }
        }

        // This test requires user to touch the device.
        [Fact]
        [Trait("Category", "RequiresTouch")]
        public void MakeCredential_NoName_GetAssertion_Succeeds()
        {
            var yubiKeyDevice = YubiKeyDevice.FindByTransport(Transport.HidFido).First();

            var isValid = Fido2ResetForTest.DoReset(yubiKeyDevice.SerialNumber);
            Assert.True(isValid);

            using (var fido2 = new Fido2Session(yubiKeyDevice))
            {
                // Set up a key collector
                fido2.KeyCollector = KeyCollector;

                // Verify the PIN
                fido2.VerifyPin();

                // Call MakeCredential
                var userId = new UserEntity(new byte[] { 1, 2, 3, 4 });

                var mcParams = new MakeCredentialParameters(_rp, userId)
                {
                    ClientDataHash = _clientDataHash
                };
                mcParams.AddOption(AuthenticatorOptions.rk, optionValue: true);

                var mcData = fido2.MakeCredential(mcParams);

                Assert.True(mcData.VerifyAttestation(_clientDataHash));

                // Call GetAssertion
                var gaParams = new GetAssertionParameters(_rp, _clientDataHash);

                var assertions = fido2.GetAssertions(gaParams);

                _ = Assert.Single(assertions);
            }
        }

        // This test requires user to touch the device.
        [Fact]
        [Trait("Category", "RequiresTouch")]
        public void MakeCredential_MultipleCredentials_GetAssertion_ReturnsMultipleAssertions()
        {
            var yubiKeyDevice = YubiKeyDevice.FindByTransport(Transport.HidFido).First();

            var isValid = Fido2ResetForTest.DoReset(yubiKeyDevice.SerialNumber);
            Assert.True(isValid);

            using (var fido2 = new Fido2Session(yubiKeyDevice))
            {
                // Set up a key collector
                fido2.KeyCollector = KeyCollector;
                var startCount =
                    (int)fido2.AuthenticatorInfo
                        .RemainingDiscoverableCredentials
                    !; //RemainingDiscoverableCredentials is NULL on my two keys I tried with (USBA 5.4.3 Keychain and Nano)

                // Verify the PIN
                fido2.VerifyPin(); //Never completes on my 5.7 

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
                mcParams1.AddOption(AuthenticatorOptions.rk, optionValue: true);
                mcParams1.AddCredProtectExtension(
                    CredProtectPolicy.UserVerificationRequired,
                    fido2.AuthenticatorInfo);

                var mcParams2 = new MakeCredentialParameters(_rp, user2)
                {
                    ClientDataHash = _clientDataHash
                };
                mcParams2.AddOption(AuthenticatorOptions.rk, optionValue: true);
                mcParams2.AddCredProtectExtension(
                    CredProtectPolicy.UserVerificationOptionalWithCredentialIDList,
                    fido2.AuthenticatorInfo);

                var mcData = fido2.MakeCredential(mcParams1);
                Assert.True(mcData.VerifyAttestation(_clientDataHash));
                var cred1 = mcData.AuthenticatorData.CredentialId!;
                var cpPolicy = mcData.AuthenticatorData.GetCredProtectExtension();
                Assert.Equal(CredProtectPolicy.UserVerificationRequired, cpPolicy);

                var midCount = (int)fido2.AuthenticatorInfo.RemainingDiscoverableCredentials!;
                Assert.True(startCount - midCount == 1);

                mcData = fido2.MakeCredential(mcParams2);
                Assert.True(mcData.VerifyAttestation(_clientDataHash));
                var cred2 = mcData.AuthenticatorData.CredentialId!;
                cpPolicy = mcData.AuthenticatorData.GetCredProtectExtension();
                Assert.Equal(CredProtectPolicy.UserVerificationOptionalWithCredentialIDList, cpPolicy);

                var endCount = (int)fido2.AuthenticatorInfo.RemainingDiscoverableCredentials!;
                Assert.True(startCount - endCount == 2);

                // Call GetAssertion
                var gaParams = new GetAssertionParameters(_rp, _clientDataHash);

                var assertions = fido2.GetAssertions(gaParams);

                Assert.Equal(expected: 2, assertions.Count);
                Assert.Equal(expected: 2, assertions[index: 0].NumberOfCredentials);
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
