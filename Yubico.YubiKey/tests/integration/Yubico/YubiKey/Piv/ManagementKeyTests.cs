// Copyright 2021 Yubico AB
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
using Xunit;
using Xunit.Abstractions;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class ManagementKeyTests
    {
        private readonly byte[] _currentKey;
        private readonly byte[] _newKey;
        private readonly ITestOutputHelper _output;

        public ManagementKeyTests(ITestOutputHelper output)
        {
            _output = output;
            _currentKey = new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };

            _newKey = new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void HasFeature_ReturnsCorrect(StandardTestDevice device)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(device);

            var expectedResult = testDevice.FirmwareVersion >= new FirmwareVersion(major: 5, minor: 4, patch: 2);

            var hasFeature = testDevice.HasFeature(YubiKeyFeature.PivAesManagementKey);

            Assert.Equal(hasFeature, expectedResult);
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void GetManagementAlgorithm_WhenReset_ReturnsCorrectType(StandardTestDevice device)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(device);
            var shouldBeAes = testDevice.FirmwareVersion >= FirmwareVersion.V5_7_0;
            var mustBeAes = shouldBeAes && testDevice.IsFipsSeries;
            var defaultManagementKeyType = shouldBeAes
                ? PivAlgorithm.Aes192
                : PivAlgorithm.TripleDes;

            var alternativeManagementKeyType = !shouldBeAes
                ? PivAlgorithm.Aes192
                : PivAlgorithm.TripleDes;

            using var session = new PivSession(testDevice);
            session.KeyCollector = TestKeyCollectorDelegate;

            session.ResetApplication();

            // This must throw for FIPS devices.
            if (mustBeAes)
            {
                Assert.Throws<InvalidOperationException>(
                    () => session.ChangeManagementKey(PivTouchPolicy.None, PivAlgorithm.TripleDes));
            }
            else
            {
                session.ChangeManagementKey(PivTouchPolicy.None, alternativeManagementKeyType);
                Assert.Equal(alternativeManagementKeyType, session.ManagementKeyAlgorithm);

                session.AuthenticateManagementKey();
                session.ResetApplication();

                Assert.Equal(defaultManagementKeyType, session.ManagementKeyAlgorithm);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void ChangeManagementKey_WithDefaultParameters_UsesCorrectTypeForRespectiveVersion(StandardTestDevice device)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(device);

            var shouldBeAes = testDevice.FirmwareVersion >= FirmwareVersion.V5_7_0;
            var mustBeAes = shouldBeAes && testDevice.IsFipsSeries;
            var defaultManagementKeyType = shouldBeAes || mustBeAes
                ? PivAlgorithm.Aes192
                : PivAlgorithm.TripleDes;

            using var session = new PivSession(testDevice);
            session.KeyCollector = TestKeyCollectorDelegate;
            session.ResetApplication();

            // This must not throw. 5.7 FIPS requires management key to be AES192.
            session.ChangeManagementKey();
            Assert.Equal(defaultManagementKeyType, session.ManagementKeyAlgorithm);

            // This must throw for FIPS devices.
            if (mustBeAes)
            {
                Assert.Throws<InvalidOperationException>(
                    () => session.ChangeManagementKey(PivTouchPolicy.None, PivAlgorithm.TripleDes));
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void RandomKey_Authenticates(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            var shouldBeAes = testDevice.FirmwareVersion >= FirmwareVersion.V5_7_0;
            var mustBeAes = shouldBeAes && testDevice.IsFipsSeries;
            var defaultManagementKeyType = shouldBeAes || mustBeAes
                ? PivAlgorithm.Aes192
                : PivAlgorithm.TripleDes;

            var isValid = false;
            var count = 10;
            for (var index = 0; index < count; index++)
            {
                GetRandomMgmtKey();
                isValid = ChangeMgmtKey(testDevice, defaultManagementKeyType);
                if (!isValid)
                {
                    break;
                }

                isValid = VerifyMgmtKey(isMutual: false, testDevice);
                if (!isValid)
                {
                    break;
                }

                isValid = VerifyMgmtKey(isMutual: true, testDevice);
                if (!isValid)
                {
                    break;
                }
            }

            ResetPiv(testDevice);

            Assert.True(isValid);
        }

        private bool VerifyMgmtKey(bool isMutual, IYubiKeyDevice testDevice)
        {
            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.KeyCollector = TestKeyCollectorDelegate;
                return pivSession.TryAuthenticateManagementKey(isMutual);
            }
        }

        private bool ChangeMgmtKey(IYubiKeyDevice testDevice, PivAlgorithm managementKeyType)
        {
            using var pivSession = new PivSession(testDevice);
            pivSession.KeyCollector = TestKeyCollectorDelegate;
            var isChanged = pivSession.TryChangeManagementKey(PivTouchPolicy.Default, managementKeyType);
            if (isChanged)
            {
                Array.Copy(_newKey, _currentKey, length: 24);
            }

            return isChanged;
        }

        private static void ResetPiv(IYubiKeyDevice testDevice)
        {
            using var pivSession = new PivSession(testDevice);
            pivSession.ResetApplication();
        }

        private void GetRandomMgmtKey()
        {
            using var random = RandomObjectUtility.GetRandomObject(fixedBytes: null);
            random.GetBytes(_newKey);
        }

        private bool TestKeyCollectorDelegate(KeyEntryData? keyEntryData)
        {
            if (keyEntryData is null)
            {
                return false;
            }

            if (keyEntryData.IsRetry)
            {
                _output.WriteLine("Retry");
                return false;
            }

            switch (keyEntryData.Request)
            {
                default:
                    return false;

                case KeyEntryRequest.Release:
                    break;

                case KeyEntryRequest.AuthenticatePivManagementKey:
                    keyEntryData.SubmitValue(_currentKey);
                    break;

                case KeyEntryRequest.ChangePivManagementKey:
                    keyEntryData.SubmitValues(_currentKey, _newKey);
                    break;
            }

            return true;
        }
    }
}
