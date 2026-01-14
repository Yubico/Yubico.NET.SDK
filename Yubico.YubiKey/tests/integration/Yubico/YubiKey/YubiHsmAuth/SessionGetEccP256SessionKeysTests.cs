// Copyright 2025 Yubico AB
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
using System.Security;
using Xunit;
using Yubico.YubiKey.TestUtilities;


namespace Yubico.YubiKey.YubiHsmAuth
{
    public class SessionGetEccP256SessionKeysTests
    {
        #region NonKeyCollector

        #region password
        [SkippableFact(typeof(DeviceNotFoundException))]
        [Trait(TraitTypes.Category, TestCategories.Simple)]
        public void GetEccP256SessionKeys_TouchNotRequired_ReturnsTrueAndSessionKeys()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            // "default" credential does not require touch
            YhaTestUtilities.AddDefaultEccP256Credential(testDevice);

            SessionKeys keys;

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Test
                // host challenge (65 bytes?), public key (65 bytes), card cryptogram (16 bytes) 
                //Host challenge = context
                keys = yubiHsmAuthSession.GetEccP256SessionKeys(
                    YhaTestUtilities.DefaultCredLabel,
                    YhaTestUtilities.DefaultCredPassword,
                    YhaTestUtilities.DefaultHostChallenge,
                    YhaTestUtilities.DefaultHsmDeviceChallenge,
                    YhaTestUtilities.DefaultEccP256PublicKey,
                    YhaTestUtilities.cardCryptoDefault);
            }

            // Postconditions
            Assert.NotNull(keys);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        [Trait(TraitTypes.Category, TestCategories.Simple)]
        public void GetEccP256SessionKeys_WrongCredPassword_ThrowsSecurityException()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();
            YhaTestUtilities.AddDefaultEccP256Credential(testDevice);

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Test & postcondition
                void getSessionKeys() => yubiHsmAuthSession.GetEccP256SessionKeys(
                    YhaTestUtilities.DefaultCredLabel,
                    YhaTestUtilities.AlternateCredPassword,
                    YhaTestUtilities.DefaultHostChallenge,
                    YhaTestUtilities.DefaultHsmDeviceChallenge,
                    YhaTestUtilities.DefaultEccP256PublicKey,
                    YhaTestUtilities.cardCryptoDefault);

                _ = Assert.Throws<SecurityException>(getSessionKeys);
            }
        }

        #endregion

        #region touch

        // When touch is required, the user should touch the YubiKey.
        //
        // It's recommended to use a debug break point in either the
        // key collector or GetEccP256SessionKeys(...) so that you're
        // aware of when touch is about to be expected.
        [Trait(TraitTypes.Category, TestCategories.RequiresTouch)]
        [Fact(Skip = "Requires user interaction")]
        public void GetEccP256SessionKeys_TouchRequired_ReturnsSessionKeys()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            // "alternate" credential requires touch
            YhaTestUtilities.AddAlternateEccP256Credential(testDevice);

            SessionKeys keys;

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Test
                keys = yubiHsmAuthSession.GetEccP256SessionKeys(
                    YhaTestUtilities.AlternateCredLabel,
                    YhaTestUtilities.AlternateCredPassword,
                    YhaTestUtilities.AlternateHostChallenge,
                    YhaTestUtilities.AlternateHsmDeviceChallenge,
                    YhaTestUtilities.DefaultEccP256PublicKey,
                    YhaTestUtilities.cardCryptoDefault);
            }

            // Postconditions
            Assert.NotNull(keys);
        }

        [Fact]
        public void GetEccP256SessionKeys_TouchTimeout_ThrowsTimeoutException()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            // "alternate" credential requires touch
            YhaTestUtilities.AddAlternateEccP256Credential(testDevice);

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Test & postcondition
                void getSessionKeys() => yubiHsmAuthSession.GetEccP256SessionKeys(
                    YhaTestUtilities.AlternateCredLabel,
                    YhaTestUtilities.AlternateCredPassword,
                    YhaTestUtilities.AlternateHostChallenge,
                    YhaTestUtilities.AlternateHsmDeviceChallenge,
                    YhaTestUtilities.DefaultEccP256PublicKey,
                    YhaTestUtilities.cardCryptoDefault);

                _ = Assert.Throws<TimeoutException>(getSessionKeys);
            }
        }

        #endregion

        [Fact]
        public void GetEccP256SessionKeys_CredNotFound_ThrowsInvalidOpEx()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();
            YhaTestUtilities.AddDefaultEccP256Credential(testDevice);

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Use incorrect management key, exhausting retries
                yubiHsmAuthSession.KeyCollector = SimpleKeyCollector.DefaultValueCollectorDelegate;

                // Test & postcondition
                void getSessionKeys() => yubiHsmAuthSession.GetEccP256SessionKeys(
                    YhaTestUtilities.AlternateCredLabel,
                    YhaTestUtilities.DefaultCredPassword,
                    YhaTestUtilities.DefaultHostChallenge,
                    YhaTestUtilities.DefaultHsmDeviceChallenge,
                    YhaTestUtilities.DefaultEccP256PublicKey,
                    YhaTestUtilities.cardCryptoDefault);

                _ = Assert.Throws<InvalidOperationException>(getSessionKeys);
            }
        }

        #endregion

        #region KeyCollector

        #region password

        [Fact]
        public void TryGetEccP256SessionKeys_TouchNotRequired_ReturnsTrueAndSessionKeys()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            // "default" credential does not require touch
            YhaTestUtilities.AddDefaultEccP256Credential(testDevice);

            bool result;
            SessionKeys? keys;

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                yubiHsmAuthSession.KeyCollector = SimpleKeyCollector.DefaultValueCollectorDelegate;

                // Test
                result = yubiHsmAuthSession.TryGetEccP256SessionKeys(
                    YhaTestUtilities.DefaultCredLabel,
                    YhaTestUtilities.DefaultHostChallenge,
                    YhaTestUtilities.DefaultHsmDeviceChallenge,
                    out keys);
            }

            // Postconditions
            Assert.True(result);
            Assert.NotNull(keys);
        }

        [Fact]
        public void TryGetEccP256SessionKeys_CredPasswordRetry_ReturnsTrueAndSessionKeys()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            // "default" credential does not require touch
            YhaTestUtilities.AddDefaultEccP256Credential(testDevice);

            bool result;
            SessionKeys? keys;

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                var keyCollector = new SimpleKeyCollector
                {
                    // Start with the incorrect cred password, forcing a retry
                    UseDefaultValue = false
                };
                yubiHsmAuthSession.KeyCollector = keyCollector.FlipFlopCollectorDelegate;

                // Test
                result = yubiHsmAuthSession.TryGetEccP256SessionKeys(
                    YhaTestUtilities.DefaultCredLabel,
                    YhaTestUtilities.DefaultHostChallenge,
                    YhaTestUtilities.DefaultHsmDeviceChallenge,
                    out keys);
            }

            // Postconditions
            Assert.True(result);
            Assert.NotNull(keys);
        }

        [Fact]
        public void TryGetEccP256SessionKeys_UserCancelsCredPassword_ReturnsFalseAndNoSessionKeys()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            // "default" credential does not require touch
            YhaTestUtilities.AddDefaultEccP256Credential(testDevice);

            bool result;
            SessionKeys? keys;

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                yubiHsmAuthSession.KeyCollector = SimpleKeyCollector.ReturnsFalseCollectorDelegate;

                // Test
                result = yubiHsmAuthSession.TryGetEccP256SessionKeys(
                    YhaTestUtilities.DefaultCredLabel,
                    YhaTestUtilities.DefaultHostChallenge,
                    YhaTestUtilities.DefaultHsmDeviceChallenge,
                    out keys);
            }

            // Postconditions
            Assert.False(result);
            Assert.Null(keys);
        }

        [Fact]
        public void TryGetEccP256SessionKeys_WrongCredPasswordNoRetries_ThrowsSecurityException()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            // "default" credential does not require touch
            YhaTestUtilities.AddDefaultEccP256Credential(testDevice);

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Use incorrect cred password, exhausting retries
                yubiHsmAuthSession.KeyCollector = SimpleKeyCollector.AlternateValueCollectorDelegate;

                // Test & postcondition
                void tryGetSessionKeys() => yubiHsmAuthSession.TryGetEccP256SessionKeys(
                    YhaTestUtilities.DefaultCredLabel,
                    YhaTestUtilities.DefaultHostChallenge,
                    YhaTestUtilities.DefaultHsmDeviceChallenge,
                    out _);

                _ = Assert.Throws<SecurityException>(tryGetSessionKeys);
            }
        }

        #endregion

        #region touch

        // When touch is requested, the user should touch the YubiKey.
        //
        // It's recommended to use a debug break point in either the
        // key collector or TryGetEccP256SessionKeys(...) so that you're
        // aware of when touch is about to be expected.
        [Fact(Skip = "Requires user interaction")]
        public void TryGetEccP256SessionKeys_Touch_ReturnsTrueAndSessionKeys()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            // "alternate" credential requires touch
            YhaTestUtilities.AddAlternateEccP256Credential(testDevice);

            bool result;
            SessionKeys? keys;

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                yubiHsmAuthSession.KeyCollector = SimpleKeyCollector.AlternateValueCollectorDelegate;

                // Test
                result = yubiHsmAuthSession.TryGetEccP256SessionKeys(
                    YhaTestUtilities.AlternateCredLabel,
                    YhaTestUtilities.AlternateHostChallenge,
                    YhaTestUtilities.AlternateHsmDeviceChallenge,
                    out keys);
            }

            // Postconditions
            Assert.True(result);
            Assert.NotNull(keys);
        }

        [Fact]
        public void TryGetEccP256SessionKeys_TouchTimeout_ThrowsTimeoutException()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();
            YhaTestUtilities.AddAlternateEccP256Credential(testDevice);

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Use incorrect management key, exhausting retries
                yubiHsmAuthSession.KeyCollector = SimpleKeyCollector.AlternateValueCollectorDelegate;

                // Test & postcondition
                void tryGetSessionKeys() => yubiHsmAuthSession.TryGetEccP256SessionKeys(
                    YhaTestUtilities.AlternateCredLabel,
                    YhaTestUtilities.AlternateHostChallenge,
                    YhaTestUtilities.AlternateHsmDeviceChallenge,
                    out _);

                _ = Assert.Throws<TimeoutException>(tryGetSessionKeys);
            }
        }

        #endregion

        [Fact]
        public void TryGetEccP256SessionKeys_CredNotFound_ThrowsInvalidOpEx()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();
            YhaTestUtilities.AddDefaultEccP256Credential(testDevice);

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Use incorrect management key, exhausting retries
                yubiHsmAuthSession.KeyCollector = SimpleKeyCollector.DefaultValueCollectorDelegate;

                // Test & postcondition
                void tryGetSessionKeys() => yubiHsmAuthSession.TryGetEccP256SessionKeys(
                    YhaTestUtilities.AlternateCredLabel,
                    YhaTestUtilities.DefaultHostChallenge,
                    YhaTestUtilities.DefaultHsmDeviceChallenge,
                    out _);

                _ = Assert.Throws<InvalidOperationException>(tryGetSessionKeys);
            }
        }

        [Fact]
        public void TryGetEccP256SessionKeys_NoKeyCollector_ThrowsInvalidOpEx()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();
            YhaTestUtilities.AddDefaultEccP256Credential(testDevice);

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Leave yubiHsmAuthSession.KeyCollector set to its default value of `null`

                // Test & postcondition
                void tryGetSessionKeys() => yubiHsmAuthSession.TryGetEccP256SessionKeys(
                    YhaTestUtilities.DefaultCredLabel,
                    YhaTestUtilities.DefaultHostChallenge,
                    YhaTestUtilities.DefaultHsmDeviceChallenge,
                    out _);

                _ = Assert.Throws<InvalidOperationException>(tryGetSessionKeys);
            }
        }

        #endregion KeyCollector
    }
}
