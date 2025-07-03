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
using Xunit;
using Xunit.Abstractions;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class ManagementKeyTests : PivSessionIntegrationTestBase
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

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void HasFeature_ReturnsCorrect(StandardTestDevice device)
        {
            TestDeviceType = device;

            Skip.If(Device.FirmwareVersion < FirmwareVersion.V5_4_2);
            var hasFeature = Device.HasFeature(YubiKeyFeature.PivAesManagementKey);
            Assert.True(hasFeature);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void GetManagementAlgorithm_WhenReset_ReturnsCorrectType(StandardTestDevice device)
        {
            TestDeviceType = device;

            var shouldBeAes = Device.FirmwareVersion >= FirmwareVersion.V5_7_0;
            var mustBeAes = shouldBeAes && Device.IsFipsSeries;
            var defaultManagementKeyType = shouldBeAes
                ? KeyType.AES192
                : KeyType.TripleDES;

            var alternativeManagementKeyType = !shouldBeAes
                ? KeyType.AES192
                : KeyType.TripleDES;

            Session.KeyCollector = TestKeyCollectorDelegate;

            // This must throw for FIPS devices.
            if (mustBeAes)
            {
                Assert.Throws<InvalidOperationException>(
                    () => Session.ChangeManagementKey(PivTouchPolicy.None, KeyType.TripleDES.GetPivAlgorithm()));
            }
            else
            {
                Session.ChangeManagementKey(PivTouchPolicy.None, alternativeManagementKeyType.GetPivAlgorithm());
                Assert.Equal(alternativeManagementKeyType.GetPivAlgorithm(), Session.ManagementKeyAlgorithm);

                Session.AuthenticateManagementKey();
                Session.ResetApplication();

                Assert.Equal(defaultManagementKeyType.GetPivAlgorithm(), Session.ManagementKeyAlgorithm);
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void ChangeManagementKey_WithDefaultParameters_UsesCorrectTypeForRespectiveVersion(StandardTestDevice device)
        {
            var Device = IntegrationTestDeviceEnumeration.GetTestDevice(device);

            var shouldBeAes = Device.FirmwareVersion >= FirmwareVersion.V5_7_0;
            var mustBeAes = shouldBeAes && Device.IsFipsSeries;
            var defaultManagementKeyType = shouldBeAes || mustBeAes
                ? KeyType.AES192
                : KeyType.TripleDES;

            using var Session = new PivSession(Device);
            Session.KeyCollector = TestKeyCollectorDelegate;

            // This must not throw. 5.7 FIPS requires management key to be AES192.
            Session.ChangeManagementKey();
            Assert.Equal(defaultManagementKeyType.GetPivAlgorithm(), Session.ManagementKeyAlgorithm);

            // This must throw for FIPS devices.
            if (mustBeAes)
            {
                Assert.Throws<InvalidOperationException>(
                    () => Session.ChangeManagementKey(PivTouchPolicy.None, KeyType.TripleDES.GetPivAlgorithm()));
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void RandomKey_Authenticates(StandardTestDevice DeviceType)
        {

            TestDeviceType = DeviceType;
            var shouldBeAes = Device.FirmwareVersion >= FirmwareVersion.V5_7_0;
            var mustBeAes = shouldBeAes && Device.IsFipsSeries;
            var defaultManagementKeyType = shouldBeAes || mustBeAes
                ? KeyType.AES192
                : KeyType.TripleDES;

            var isValid = false;
            var count = 10;
            for (var index = 0; index < count; index++)
            {
                GetRandomMgmtKey();
                isValid = ChangeMgmtKey(defaultManagementKeyType);
                if (!isValid)
                {
                    break;
                }

                isValid = VerifyMgmtKey(isMutual: false);
                if (!isValid)
                {
                    break;
                }

                isValid = VerifyMgmtKey(isMutual: true);
                if (!isValid)
                {
                    break;
                }
            }

            Assert.True(isValid);
        }

        private bool VerifyMgmtKey(bool isMutual)
        {
            Session.KeyCollector = TestKeyCollectorDelegate;
            return Session.TryAuthenticateManagementKey(isMutual);
        }

        private bool ChangeMgmtKey(KeyType managementKeyType)
        {
            Session.KeyCollector = TestKeyCollectorDelegate;
            var isChanged = Session.TryChangeManagementKey(PivTouchPolicy.Default, managementKeyType.GetPivAlgorithm());
            if (isChanged)
            {
                Array.Copy(_newKey, _currentKey, length: 24);
            }

            return isChanged;
        }

        private static void ResetPiv(IYubiKeyDevice Device)
        {
            using var Session = new PivSession(Device);
            Session.ResetApplication();
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
