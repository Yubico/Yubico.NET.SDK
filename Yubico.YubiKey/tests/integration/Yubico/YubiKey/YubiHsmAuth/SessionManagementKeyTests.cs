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

namespace Yubico.YubiKey.YubiHsmAuth
{
    public class SessionManagementKeyTests
    {
        #region GetRetries
        [Fact]
        public void GetMgmtKeyRetries_NoFailedAttempts_Returns8()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            int retriesRemaining;

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Test
                retriesRemaining = yubiHsmAuthSession.GetManagementKeyRetries();
            }

            // Postconditions
            Assert.Equal(8, retriesRemaining);
        }

        [Fact]
        public void GetMgmtKeyRetries_FailAllAttempts_Returns0()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            int retriesRemaining;

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Exhaust all retries
                int availableRetries = yubiHsmAuthSession.GetManagementKeyRetries();

                for (int i = 0; i < availableRetries; i++)
                {
                    _ = yubiHsmAuthSession.TryChangeManagementKey(
                        YhaTestUtilities.AlternateMgmtKey,
                        YhaTestUtilities.DefaultMgmtKey,
                        out int? _);
                }

                // Test
                retriesRemaining = yubiHsmAuthSession.GetManagementKeyRetries();
            }

            // Postconditions
            Assert.Equal(0, retriesRemaining);
        }
        #endregion

        #region TryChangeMgmtKey
        [Fact]
        public void TryChangeManagementKey_ValidKeys_ReturnsTrue()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            bool mgmtKeyChanged = false;

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Test
                mgmtKeyChanged = yubiHsmAuthSession.TryChangeManagementKey(
                    YhaTestUtilities.DefaultMgmtKey,
                    YhaTestUtilities.AlternateMgmtKey,
                    out int? _);
            }

            // Postcondition
            Assert.True(mgmtKeyChanged);
        }

        [Fact]
        public void TryChangeManagementKey_ValidKeys_ReturnsNullRetries()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            int? retriesRemaining;

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Test
                _ = yubiHsmAuthSession.TryChangeManagementKey(
                    YhaTestUtilities.DefaultMgmtKey,
                    YhaTestUtilities.AlternateMgmtKey,
                    out retriesRemaining);
            }

            // Postcondition
            Assert.False(retriesRemaining.HasValue);
        }

        [Fact]
        public void TryChangeMgmtKey_WrongCurrentKey_ReturnsFalse()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            bool mgmtKeyChanged = false;

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Test
                mgmtKeyChanged = yubiHsmAuthSession.TryChangeManagementKey(
                    YhaTestUtilities.AlternateMgmtKey,
                    YhaTestUtilities.DefaultMgmtKey,
                    out int? _);
            }

            // Postcondition
            Assert.False(mgmtKeyChanged);
        }

        [Fact]
        public void TryChangeMgmtKey_WrongCurrentKey_Returns1FewerRetries()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            int expectedRetriesRemaining;
            int? actualRetriesRemaining;

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                expectedRetriesRemaining = yubiHsmAuthSession.GetManagementKeyRetries() - 1;

                // Test
                _ = yubiHsmAuthSession.TryChangeManagementKey(
                    YhaTestUtilities.AlternateMgmtKey,
                    YhaTestUtilities.DefaultMgmtKey,
                    out actualRetriesRemaining);
            }

            // Postcondition
            Assert.Equal(expectedRetriesRemaining, actualRetriesRemaining);
        }
        #endregion

        #region ChangeMgmtKey
        [Fact]
        public void ChangeManagementKey_ValidKeys_MgmtKeyChanged()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Test by changing from current to new, and then back
                yubiHsmAuthSession.ChangeManagementKey(
                    YhaTestUtilities.DefaultMgmtKey,
                    YhaTestUtilities.AlternateMgmtKey);

                yubiHsmAuthSession.ChangeManagementKey(
                    YhaTestUtilities.AlternateMgmtKey,
                    YhaTestUtilities.DefaultMgmtKey);
            }
        }

        [Fact]
        public void ChangeMgmtKey_WrongCurrentKey_ThrowsSecurityEx()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Test & postcondition
                void changeMgmtKey() => yubiHsmAuthSession.ChangeManagementKey(
                    YhaTestUtilities.AlternateMgmtKey,
                    YhaTestUtilities.DefaultMgmtKey);

                _ = Assert.Throws<SecurityException>(changeMgmtKey);
            }
        }

        [Fact]
        public void ChangeMgmtKey_NoRetries_ThrowsSecurityEx()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Exhaust all retries
                int availableRetries = yubiHsmAuthSession.GetManagementKeyRetries();

                for (int i = 0; i < availableRetries; i++)
                {
                    _ = yubiHsmAuthSession.TryChangeManagementKey(
                        YhaTestUtilities.AlternateMgmtKey,
                        YhaTestUtilities.DefaultMgmtKey,
                        out int? _);
                }

                // Test
                void changeMgmtKey() => yubiHsmAuthSession.ChangeManagementKey(
                    YhaTestUtilities.DefaultMgmtKey,
                    YhaTestUtilities.AlternateMgmtKey);

                _ = Assert.Throws<SecurityException>(changeMgmtKey);
            }
        }
        #endregion

        #region KeyCollector
        [Fact]
        public void TryChangeMgmtKeyKeyCollector_NoKeyCollector_ThrowsInvalidOpEx()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                // Leave yubiHsmAuthSession.KeyCollector set to its default value of `null`

                // Test & postcondition
                void tryChangeMgmtKey() => yubiHsmAuthSession.TryChangeManagementKey();

                _ = Assert.Throws<InvalidOperationException>(tryChangeMgmtKey);
            }
        }

        [Fact]
        public void TryChangeMgmtKeyKeyCollector_KeyCollectorReturnsFalse_ReturnsFalse()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                yubiHsmAuthSession.KeyCollector = SimpleKeyCollector.ReturnsFalseCollectorDelegate;

                // Test
                bool managmentKeyChanged = yubiHsmAuthSession.TryChangeManagementKey();

                // Postconditions
                Assert.False(managmentKeyChanged);
            }
        }

        [Fact]
        public void TryChangeManagementKeyKeyCollector_ValidKeys_ManagementKeyChanged()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();
            var keyCollector = new SimpleKeyCollector();

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                yubiHsmAuthSession.KeyCollector = keyCollector.FlipFlopCollectorDelegate;

                // Test by changing from current to new, and then back
                bool managmentKeyChanged = yubiHsmAuthSession.TryChangeManagementKey();
                Assert.True(managmentKeyChanged);

                managmentKeyChanged = yubiHsmAuthSession.TryChangeManagementKey();
                Assert.True(managmentKeyChanged);
            }
        }

        [Fact]
        public void TryChangeMgmtKeyKeyCollector_WrongCurrentKey_RetrySuccess()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();
            var keyCollector = new SimpleKeyCollector
            {
                UseDefaultValue = false,
            };

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                yubiHsmAuthSession.KeyCollector = keyCollector.FlipFlopCollectorDelegate;

                // Test
                bool managmentKeyChanged = yubiHsmAuthSession.TryChangeManagementKey();

                // Postconditions
                Assert.True(managmentKeyChanged);
            }
        }

        [Fact]
        public void TryChangeMgmtKeyKeyCollector_FailedAuthenticationWithNoRetries_ThrowsSecurityEx()
        {
            // Preconditions
            IYubiKeyDevice testDevice = YhaTestUtilities.GetCleanDevice();

            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                yubiHsmAuthSession.KeyCollector = SimpleKeyCollector.AlternateValueCollectorDelegate;

                // Test & postcondition
                void tryChangeMgmtKey() => yubiHsmAuthSession.TryChangeManagementKey();
                _ = Assert.Throws<SecurityException>(tryChangeMgmtKey);
            }
        }
        #endregion KeyCollector
    }
}
