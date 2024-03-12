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

namespace Yubico.YubiKey.Piv.Commands
{
    // All these tests will reset the PIV application, run, then reset the PIV
    // application again.
    public class GetMetadataCmdTests : IDisposable
    {
        private readonly IYubiKeyDevice yubiKey;

        public GetMetadataCmdTests()
        {
            yubiKey = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5);

            if (yubiKey.HasFeature(YubiKeyFeature.PivAesManagementKey))
            {
                ResetPiv(yubiKey);
            }
        }

        public void Dispose()
        {
            if (yubiKey.HasFeature(YubiKeyFeature.PivAesManagementKey))
            {
                ResetPiv(yubiKey);
            }
        }

        [Theory]
        [InlineData(PivAlgorithm.Aes128)]
        [InlineData(PivAlgorithm.Aes192)]
        [InlineData(PivAlgorithm.Aes256)]
        public void AesKey_GetMetadata_CorrectAlgorithm(PivAlgorithm algorithm)
        {
            if (!yubiKey.HasFeature(YubiKeyFeature.PivAesManagementKey))
            {
                return;
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                bool isValid = pivSession.TryAuthenticateManagementKey();
                Assert.True(isValid);

                byte[] keyData = new byte[] {
                    0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                    0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                    0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
                    0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68
                };

                int keyLength = algorithm switch
                {
                    PivAlgorithm.Aes128 => 16,
                    PivAlgorithm.Aes192 => 24,
                    _ => 32,
                };

                var mgmtKey = new ReadOnlyMemory<byte>(keyData, 0, keyLength);

                var setCmd = new SetManagementKeyCommand(mgmtKey, PivTouchPolicy.Never, algorithm);

                SetManagementKeyResponse setRsp = pivSession.Connection.SendCommand(setCmd);
                Assert.Equal(ResponseStatus.Success, setRsp.Status);

                var getCmd = new GetMetadataCommand(PivSlot.Management);
                GetMetadataResponse getRsp = pivSession.Connection.SendCommand(getCmd);
                Assert.Equal(ResponseStatus.Success, getRsp.Status);

                PivMetadata metadata = getRsp.GetData();
                Assert.Equal(algorithm, metadata.Algorithm);
            }
        }

        private static void ResetPiv(IYubiKeyDevice yubiKey)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();
            }
        }
    }
}
