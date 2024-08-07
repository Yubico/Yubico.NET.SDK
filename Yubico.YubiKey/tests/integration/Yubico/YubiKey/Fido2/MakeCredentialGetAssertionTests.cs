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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    public class MakeCredentialGetAssertionTests
    {
        static readonly byte[] _clientDataHash = {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
        };

        static readonly RelyingParty _rp = new RelyingParty("relyingparty1");

        // This test requires user to touch the device.
        [Fact, Trait(TraitTypes.Category, TestCategories.RequiresTouch)]
        public void MakeCredential_NonDiscoverable_GetAssertion_Succeeds()
        {
            IYubiKeyDevice yubiKeyDevice = YubiKeyDevice.FindByTransport(Transport.HidFido).First();

            bool isValid = Fido2ResetForTest.DoReset(yubiKeyDevice.SerialNumber);
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

                MakeCredentialData mcData = fido2.MakeCredential(mcParams);

                Assert.True(mcData.VerifyAttestation(_clientDataHash));

                // Call GetAssertion
                var gaParams = new GetAssertionParameters(_rp, _clientDataHash);

                gaParams.AllowCredential(mcData.AuthenticatorData.CredentialId!);

                IReadOnlyList<GetAssertionData> assertions = fido2.GetAssertions(gaParams);

                GetAssertionData assertion = Assert.Single(assertions);
                Assert.Equal(1, assertion.NumberOfCredentials);
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
        [Fact, Trait(TraitTypes.Category, TestCategories.RequiresTouch)]
        public void MakeCredential_NoName_GetAssertion_Succeeds()
        {
            IYubiKeyDevice yubiKeyDevice = YubiKeyDevice.FindByTransport(Transport.HidFido).First();

            bool isValid = Fido2ResetForTest.DoReset(yubiKeyDevice.SerialNumber);
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
                mcParams.AddOption(AuthenticatorOptions.rk, true);

                MakeCredentialData mcData = fido2.MakeCredential(mcParams);

                Assert.True(mcData.VerifyAttestation(_clientDataHash));

                // Call GetAssertion
                var gaParams = new GetAssertionParameters(_rp, _clientDataHash);

                IReadOnlyList<GetAssertionData> assertions = fido2.GetAssertions(gaParams);

                _ = Assert.Single(assertions);
            }
        }

        // This test requires user to touch the device.
        [Fact, Trait(TraitTypes.Category, TestCategories.RequiresTouch)]
        public void MakeCredential_MultipleCredentials_GetAssertion_ReturnsMultipleAssertions()
        {
            IYubiKeyDevice yubiKeyDevice = YubiKeyDevice.FindByTransport(Transport.HidFido).First();

            bool isValid = Fido2ResetForTest.DoReset(yubiKeyDevice.SerialNumber);
            Assert.True(isValid);

            using (var fido2 = new Fido2Session(yubiKeyDevice))
            {
                // Set up a key collector
                fido2.KeyCollector = KeyCollector;
                int startCount = (int)fido2.AuthenticatorInfo.RemainingDiscoverableCredentials!; //RemainingDiscoverableCredentials is NULL on my two keys I tried with (USBA 5.4.3 Keychain and Nano)

                // Fido app was reset above, so set and confirm a pin (hardcoded in KeyCollector)
                fido2.SetPin();
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
                mcParams1.AddOption(AuthenticatorOptions.rk, true);
                mcParams1.AddCredProtectExtension(
                    CredProtectPolicy.UserVerificationRequired,
                    fido2.AuthenticatorInfo);

                var mcParams2 = new MakeCredentialParameters(_rp, user2)
                {
                    ClientDataHash = _clientDataHash
                };
                mcParams2.AddOption(AuthenticatorOptions.rk, true);
                mcParams2.AddCredProtectExtension(
                    CredProtectPolicy.UserVerificationOptionalWithCredentialIDList,
                    fido2.AuthenticatorInfo);

                MakeCredentialData mcData = fido2.MakeCredential(mcParams1);
                Assert.True(mcData.VerifyAttestation(_clientDataHash));
                CredentialId cred1 = mcData.AuthenticatorData.CredentialId!;
                CredProtectPolicy cpPolicy = mcData.AuthenticatorData.GetCredProtectExtension();
                Assert.Equal(CredProtectPolicy.UserVerificationRequired, cpPolicy);

                int midCount = (int)fido2.AuthenticatorInfo.RemainingDiscoverableCredentials!;
                Assert.True(startCount - midCount == 1);

                mcData = fido2.MakeCredential(mcParams2);
                Assert.True(mcData.VerifyAttestation(_clientDataHash));
                CredentialId cred2 = mcData.AuthenticatorData.CredentialId!;
                cpPolicy = mcData.AuthenticatorData.GetCredProtectExtension();
                Assert.Equal(CredProtectPolicy.UserVerificationOptionalWithCredentialIDList, cpPolicy);

                int endCount = (int)fido2.AuthenticatorInfo.RemainingDiscoverableCredentials!;
                Assert.True(startCount - endCount == 2);

                // Call GetAssertion
                var gaParams = new GetAssertionParameters(_rp, _clientDataHash);

                IReadOnlyList<GetAssertionData> assertions = fido2.GetAssertions(gaParams);

                Assert.Equal(2, assertions.Count);
                Assert.Equal(2, assertions[0].NumberOfCredentials);
            }
        }


        // This test requires user to touch the device.
        [Theory, Trait("Category", "RequiresTouch")]
        [InlineData(CredProtectPolicy.UserVerificationOptional, true)]
        [InlineData(CredProtectPolicy.UserVerificationOptional, false)]
        [InlineData(CredProtectPolicy.UserVerificationRequired, true)]
        [InlineData(CredProtectPolicy.UserVerificationRequired, false)]
        public void AddCredProtectExtension_KeySupportsCredProtectExtension(
            CredProtectPolicy credProtectPolicy,
            bool enforceCredProtectPolicy)
        {
            // Could have combined expectExtensionSupported cases under a single Theory, but
            // expectExtensionSupported true vs false requires using YubiKeys with different
            // firmware, and using separate Theory methods makes it somewhat easier to run them
            // as separate, grouped sets
            TestAddCredProtectExtension(expectExtensionSupported: true, credProtectPolicy, enforceCredProtectPolicy);
        }

        [Theory, Trait("Category", "RequiresTouch")]
        [InlineData(CredProtectPolicy.UserVerificationOptional, true)]
        [InlineData(CredProtectPolicy.UserVerificationOptional, false)]
        [InlineData(CredProtectPolicy.UserVerificationRequired, true)]
        [InlineData(CredProtectPolicy.UserVerificationRequired, false)]
        [InlineData(CredProtectPolicy.UserVerificationOptionalWithCredentialIDList, true)]
        public void AddCredProtectExtension_KeyDoesNotSupportCredProtectExtension(
            CredProtectPolicy credProtectPolicy,
            bool enforceCredProtectPolicy)
        {
            // Could have combined expectExtensionSupported cases under a single Theory, but
            // expectExtensionSupported true vs false requires using YubiKeys with different
            // firmware, and using separate Theory methods makes it somewhat easier to run them
            // as distinct, grouped sets
            TestAddCredProtectExtension(expectExtensionSupported: false, credProtectPolicy, enforceCredProtectPolicy);
        }

        private void TestAddCredProtectExtension(
            bool expectExtensionSupported,
            CredProtectPolicy credProtectPolicy,
            bool enforceCredProtectPolicy)
        {
            IYubiKeyDevice yubiKeyDevice = YubiKeyDevice.FindByTransport(Transport.HidFido).First();

            bool isValid = Fido2ResetForTest.DoReset(yubiKeyDevice.SerialNumber);
            Assert.True(isValid);

            using (var fido2Session = new Fido2Session(yubiKeyDevice))
            {
                // Set up a key collector
                fido2Session.KeyCollector = KeyCollector;

                // Fido app was reset above, so set and confirm a pin (hardcoded in KeyCollector)
                fido2Session.SetPin();
                fido2Session.VerifyPin();

                // Note that Name is a required value
                var user = new UserEntity(new byte[] { 1, 2, 3, 4 })
                {
                    Name = "Name",
                    DisplayName = "DisplayName",
                };

                var mcParams = new MakeCredentialParameters(_rp, user)
                {
                    ClientDataHash = _clientDataHash
                };

                var isExtensionSupported = fido2Session.AuthenticatorInfo.Extensions?.Contains("credProtect") ?? false;

                // If this fails, the yubikey used doesn't have the proper support level expected for the test
                // For expectExtensionSupported==true, the key must have FW 5.2.0 or later
                // For expectExtensionSupported==false, the key must have FW before 5.2.0
                Assert.Equal(expectExtensionSupported, isExtensionSupported);

                // Act
                try
                {
                    mcParams.AddCredProtectExtension(
                        credProtectPolicy,
                        enforceCredProtectPolicy,
                        fido2Session.AuthenticatorInfo!);
                }
                catch (NotSupportedException)
                {
                    // This shouldn't fail for Optional, even if the key doesn't support the
                    // extension, so ensure that's not what was set
                    Assert.NotEqual(CredProtectPolicy.UserVerificationOptional, credProtectPolicy);
                    Assert.False(isExtensionSupported);
                    return;
                }

                // Verify
                // The call to set the extension should always succeed under any of these conditions
                Assert.True(
                    !enforceCredProtectPolicy
                    || isExtensionSupported
                    || credProtectPolicy == CredProtectPolicy.UserVerificationOptional);

                Assert.Equal(expectExtensionSupported, mcParams.Extensions?.ContainsKey("credProtect") ?? false);

                MakeCredentialData mcData = fido2Session.MakeCredential(mcParams);

                CredProtectPolicy cpPolicy = mcData.AuthenticatorData.GetCredProtectExtension();
                Assert.Equal(expectExtensionSupported ? credProtectPolicy : CredProtectPolicy.None, cpPolicy);
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
                case KeyEntryRequest.SetFido2Pin:
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
