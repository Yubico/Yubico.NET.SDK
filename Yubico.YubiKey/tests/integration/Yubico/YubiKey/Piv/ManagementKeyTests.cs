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

        [Fact]
        public void HasFeature_ReturnsCorrect()
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5);

            var expectedResult = testDevice.FirmwareVersion >= new FirmwareVersion(major: 5, minor: 4, patch: 2);

            var hasFeature = testDevice.HasFeature(YubiKeyFeature.PivAesManagementKey);

            Assert.Equal(hasFeature, expectedResult);
        }

        [Fact]
        public void GetManagementAlgorithm_WhenReset_ReturnsCorrectType()
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5);
            var shouldBeTripleDes = testDevice.FirmwareVersion < FirmwareVersion.V5_7_0;
            var defaultManagementKeyType = shouldBeTripleDes
                ? PivAlgorithm.TripleDes
                : PivAlgorithm.Aes192;

            var alternativeManagementKeyType = !shouldBeTripleDes
                ? PivAlgorithm.TripleDes
                : PivAlgorithm.Aes192;

            using var session = new PivSession(testDevice);
            session.KeyCollector = TestKeyCollectorDelegate;

            session.ResetApplication();
            session.ChangeManagementKey(PivTouchPolicy.None, alternativeManagementKeyType);
            var firstCheckKeyType = session.ManagementKeyAlgorithm;

            Assert.Equal(alternativeManagementKeyType, firstCheckKeyType);

            session.AuthenticateManagementKey();
            session.ResetApplication();

            var secondCheckKeyType = session.ManagementKeyAlgorithm;
            Assert.Equal(defaultManagementKeyType, secondCheckKeyType);
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void RandomKey_Authenticates(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            var isValid = false;
            var count = 10;
            for (var index = 0; index < count; index++)
            {
                GetRandomMgmtKey();
                isValid = ChangeMgmtKey(testDevice, PivAlgorithm.TripleDes);
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
