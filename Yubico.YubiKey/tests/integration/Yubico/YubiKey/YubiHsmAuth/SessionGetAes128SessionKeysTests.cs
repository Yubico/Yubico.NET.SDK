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
using System.Security;
using Xunit;

namespace Yubico.YubiKey.YubiHsmAuth
{
    public class SessionGetAes128SessionKeysTests
    {
        #region NonKeyCollector
        #region password
        [Fact]
        public void GetAes128SessionKeys_TouchNotRequired_ReturnsTrueAndSessionKeys()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            // "default" credential does not require touch
            YhaTestUtilities.AddDefaultAes128Credential(testDevice);

            SessionKeys keys;

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Test
                keys = yubiHsmAuthSession.GetAes128SessionKeys(
                    YhaTestUtilities.DefaultCredLabel,
                    YhaTestUtilities.DefaultCredPassword,
                    YhaTestUtilities.DefaultHostChallenge,
                    YhaTestUtilities.DefaultHsmDeviceChallenge);
            }

            // Postconditions
            Assert.NotNull(keys);
        }

        [Fact]
        public void GetAes128SessionKeys_WrongCredPassword_ThrowsSecurityException()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();
            YhaTestUtilities.AddDefaultAes128Credential(testDevice);

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Test & postcondition
                void getSessionKeys() => yubiHsmAuthSession.GetAes128SessionKeys(
                    YhaTestUtilities.DefaultCredLabel,
                    YhaTestUtilities.AlternateCredPassword,
                    YhaTestUtilities.DefaultHostChallenge,
                    YhaTestUtilities.DefaultHsmDeviceChallenge);

                _ = Assert.Throws<SecurityException>(getSessionKeys);
            }
        }
        #endregion

        #region touch
        // When touch is required, the user should touch the YubiKey.
        //
        // It's recommended to use a debug break point in either the
        // key collector or GetAes128SessionKeys(...) so that you're
        // aware of when touch is about to be expected.
        [Fact(Skip = "Requires user interaction")]
        public void GetAes128SessionKeys_TouchRequired_ReturnsSessionKeys()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            // "alternate" credential does not require touch
            YhaTestUtilities.AddAlternateAes128Credential(testDevice);

            SessionKeys keys;

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Test
                keys = yubiHsmAuthSession.GetAes128SessionKeys(
                    YhaTestUtilities.AlternateCredLabel,
                    YhaTestUtilities.AlternateCredPassword,
                    YhaTestUtilities.AlternateHostChallenge,
                    YhaTestUtilities.AlternateHsmDeviceChallenge);
            }

            // Postconditions
            Assert.NotNull(keys);
        }

        [Fact]
        public void GetAes128SessionKeys_TouchTimeout_ThrowsTimeoutException()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            // "alternate" credential does not require touch
            YhaTestUtilities.AddAlternateAes128Credential(testDevice);

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Test & postcondition
                void getSessionKeys() => yubiHsmAuthSession.GetAes128SessionKeys(
                    YhaTestUtilities.AlternateCredLabel,
                    YhaTestUtilities.AlternateCredPassword,
                    YhaTestUtilities.AlternateHostChallenge,
                    YhaTestUtilities.AlternateHsmDeviceChallenge);

                _ = Assert.Throws<TimeoutException>(getSessionKeys);
            }
        }
        #endregion

        [Fact]
        public void GetAes128SessionKeys_CredNotFound_ThrowsInvalidOpEx()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();
            YhaTestUtilities.AddDefaultAes128Credential(testDevice);

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Use incorrect management key, exhausting retries
                yubiHsmAuthSession.KeyCollector = SimpleKeyCollector.DefaultValueCollectorDelegate;

                // Test & postcondition
                void getSessionKeys() => yubiHsmAuthSession.GetAes128SessionKeys(
                    YhaTestUtilities.AlternateCredLabel,
                    YhaTestUtilities.DefaultCredPassword,
                    YhaTestUtilities.DefaultHostChallenge,
                    YhaTestUtilities.DefaultHsmDeviceChallenge);

                _ = Assert.Throws<InvalidOperationException>(getSessionKeys);
            }
        }
        #endregion

        #region KeyCollector
        #region password
        [Fact]
        public void TryGetAes128SessionKeys_TouchNotRequired_ReturnsTrueAndSessionKeys()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            // "default" credential does not require touch
            YhaTestUtilities.AddDefaultAes128Credential(testDevice);

            bool result;
            SessionKeys? keys;

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                yubiHsmAuthSession.KeyCollector = SimpleKeyCollector.DefaultValueCollectorDelegate;

                // Test
                result = yubiHsmAuthSession.TryGetAes128SessionKeys(
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
        public void TryGetAes128SessionKeys_CredPasswordRetry_ReturnsTrueAndSessionKeys()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            // "default" credential does not require touch
            YhaTestUtilities.AddDefaultAes128Credential(testDevice);

            bool result;
            SessionKeys? keys;

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                SimpleKeyCollector keyCollector = new SimpleKeyCollector
                {
                    // Start with the incorrect cred password, forcing a retry
                    UseDefaultValue = false
                };
                yubiHsmAuthSession.KeyCollector = keyCollector.FlipFlopCollectorDelegate;

                // Test
                result = yubiHsmAuthSession.TryGetAes128SessionKeys(
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
        public void TryGetAes128SessionKeys_UserCancelsCredPassword_ReturnsFalseAndNoSessionKeys()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            // "default" credential does not require touch
            YhaTestUtilities.AddDefaultAes128Credential(testDevice);

            bool result;
            SessionKeys? keys;

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                yubiHsmAuthSession.KeyCollector = SimpleKeyCollector.ReturnsFalseCollectorDelegate;

                // Test
                result = yubiHsmAuthSession.TryGetAes128SessionKeys(
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
        public void TryGetAes128SessionKeys_WrongCredPasswordNoRetries_ThrowsSecurityException()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            // "default" credential does not require touch
            YhaTestUtilities.AddDefaultAes128Credential(testDevice);

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Use incorrect cred password, exhausting retries
                yubiHsmAuthSession.KeyCollector = SimpleKeyCollector.AlternateValueCollectorDelegate;

                // Test & postcondition
                void tryGetSessionKeys() => yubiHsmAuthSession.TryGetAes128SessionKeys(
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
        // key collector or TryGetAes128SessionKeys(...) so that you're
        // aware of when touch is about to be expected.
        [Fact(Skip = "Requires user interaction")]
        public void TryGetAes128SessionKeys_Touch_ReturnsTrueAndSessionKeys()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            // "alternate" credential requires touch
            YhaTestUtilities.AddAlternateAes128Credential(testDevice);

            bool result;
            SessionKeys? keys;

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                yubiHsmAuthSession.KeyCollector = SimpleKeyCollector.AlternateValueCollectorDelegate;

                // Test
                result = yubiHsmAuthSession.TryGetAes128SessionKeys(
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
        public void TryGetAes128SessionKeys_TouchTimeout_ThrowsTimeoutException()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();
            YhaTestUtilities.AddAlternateAes128Credential(testDevice);

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Use incorrect management key, exhausting retries
                yubiHsmAuthSession.KeyCollector = SimpleKeyCollector.AlternateValueCollectorDelegate;

                // Test & postcondition
                void tryGetSessionKeys() => yubiHsmAuthSession.TryGetAes128SessionKeys(
                    YhaTestUtilities.AlternateCredLabel,
                    YhaTestUtilities.AlternateHostChallenge,
                    YhaTestUtilities.AlternateHsmDeviceChallenge,
                    out _);

                _ = Assert.Throws<TimeoutException>(tryGetSessionKeys);
            }
        }
        #endregion

        [Fact]
        public void TryGetAes128SessionKeys_CredNotFound_ThrowsInvalidOpEx()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();
            YhaTestUtilities.AddDefaultAes128Credential(testDevice);

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Use incorrect management key, exhausting retries
                yubiHsmAuthSession.KeyCollector = SimpleKeyCollector.DefaultValueCollectorDelegate;

                // Test & postcondition
                void tryGetSessionKeys() => yubiHsmAuthSession.TryGetAes128SessionKeys(
                    YhaTestUtilities.AlternateCredLabel,
                    YhaTestUtilities.DefaultHostChallenge,
                    YhaTestUtilities.DefaultHsmDeviceChallenge,
                    out _);

                _ = Assert.Throws<InvalidOperationException>(tryGetSessionKeys);
            }
        }

        [Fact]
        public void TryGetAes128SessionKeys_NoKeyCollector_ThrowsInvalidOpEx()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();
            YhaTestUtilities.AddDefaultAes128Credential(testDevice);

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Leave yubiHsmAuthSession.KeyCollector set to its default value of `null`

                // Test & postcondition
                void tryGetSessionKeys() => yubiHsmAuthSession.TryGetAes128SessionKeys(
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
