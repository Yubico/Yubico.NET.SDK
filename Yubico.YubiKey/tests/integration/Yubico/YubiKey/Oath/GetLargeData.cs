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
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Oath
{
    // This set of tests checks that we can successfully
    // retrieve large amounts of data that spans more
    // than one APDU. For more information, see
    // Pipelines.ResponseChainingTransform.
    [Trait("Category", "Simple")]
    public sealed class GetLargeData
    {
        private static readonly Random random = new Random();

        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private IEnumerable<Credential> FillWithRandCreds(IYubiKeyDevice testDevice)
        {
            List<Credential> creds = new List<Credential>();

            using (var oathSession = new OathSession(testDevice))
            {
                // As documented, the OATH application can hold 32 creds
                for (int i = 0; i < 32; i++)
                {
                    creds.Add(
                        oathSession.AddCredential(
                        "",
                        RandomString(63)));
                }

                return creds;
            }
        }

        private IYubiKeyDevice GetCleanDevice(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            return DeviceReset.ResetOath(testDevice);
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void GetLotsOfCredentials(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = GetCleanDevice(testDeviceType);
            IEnumerable<Credential>? expectedCredsOnDevice = FillWithRandCreds(testDevice);
            IEnumerable<Credential> actualCredsOnDevice;

            using (var oathSession = new OathSession(testDevice))
            {
                actualCredsOnDevice = oathSession.GetCredentials();
            }

            Assert.Equal(expectedCredsOnDevice.Count(), actualCredsOnDevice.Count());
        }
    }
}
