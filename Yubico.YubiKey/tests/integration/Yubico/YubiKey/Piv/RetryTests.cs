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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait("Category", "Simple")]
    public class RetryTests
    {
        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ChangeRetry_Succeeds(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            var isOld = testDevice.FirmwareVersion < FirmwareVersion.V5_3_0;

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ChangePinAndPukRetryCounts(newRetryCountPin: 7, newRetryCountPuk: 8);

                if (isOld)
                {
                    return;
                }

                var metadata = pivSession.GetMetadata(PivSlot.Pin);
                Assert.Equal(expected: 7, metadata.RetryCount);

                metadata = pivSession.GetMetadata(PivSlot.Puk);
                Assert.Equal(expected: 8, metadata.RetryCount);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ChangeRetry_SetsToDefault(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            var isOld = testDevice.FirmwareVersion < FirmwareVersion.V5_3_0;

            using (var pivSession = new PivSession(testDevice))
            {
                try
                {
                    var collectorObj = new Simple39KeyCollector();
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ChangePin();
                    pivSession.ChangePuk();
                    pivSession.ChangeManagementKey();

                    collectorObj.KeyFlag = 1;
                    pivSession.ChangePinAndPukRetryCounts(newRetryCountPin: 9, newRetryCountPuk: 10);

                    collectorObj.KeyFlag = 0;
                    pivSession.VerifyPin();

                    if (isOld)
                    {
                        return;
                    }

                    var metadata = pivSession.GetMetadata(PivSlot.Pin);
                    Assert.Equal(expected: 9, metadata.RetryCount);
                    Assert.Equal(PivKeyStatus.Default, metadata.KeyStatus);

                    metadata = pivSession.GetMetadata(PivSlot.Puk);
                    Assert.Equal(expected: 10, metadata.RetryCount);
                    Assert.Equal(PivKeyStatus.Default, metadata.KeyStatus);
                }
                finally
                {
                    pivSession.ResetApplication();
                }
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void Metadata_OldYubiKey_ThrowsException(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            // If the YubiKey is 5.3 or later, don't bother with this test.
            if (testDevice.HasFeature(YubiKeyFeature.PivMetadata))
            {
                return;
            }

            using (var pivSession = new PivSession(testDevice))
            {
                _ = Assert.Throws<InvalidOperationException>(() => pivSession.GetMetadata(PivSlot.Authentication));
            }
        }
    }
}
