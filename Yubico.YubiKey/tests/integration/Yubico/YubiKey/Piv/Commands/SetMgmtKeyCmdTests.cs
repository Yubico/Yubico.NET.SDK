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
    public class SetMgmtKeyCmdTests : IDisposable
    {
        private readonly IYubiKeyDevice yubiKey;

        public SetMgmtKeyCmdTests()
        {
            yubiKey = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5);
            ResetPiv(yubiKey);
        }

        public void Dispose()
        {
            ResetPiv(yubiKey);
        }

        [Fact]
        public void SetKey_ValidAes_Succeeds()
        {
            if (yubiKey.FirmwareVersion < new FirmwareVersion(5, 4, 2))
            {
                return;
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                byte[] keyData = {
                    0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                    0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
                };
                var setCmd = new SetManagementKeyCommand(keyData)
                {
                    Algorithm = PivAlgorithm.Aes128,
                };

                SetManagementKeyResponse setRsp = pivSession.Connection.SendCommand(setCmd);
                Assert.Equal(ResponseStatus.AuthenticationRequired, setRsp.Status);

                bool isValid = pivSession.TryAuthenticateManagementKey();
                Assert.True(isValid);

                setRsp = pivSession.Connection.SendCommand(setCmd);
                Assert.Equal(ResponseStatus.Success, setRsp.Status);
            }
        }

        [Fact]
        public void SetKey_Aes256_Succeeds()
        {
            if (yubiKey.FirmwareVersion < new FirmwareVersion(5, 4, 2))
            {
                return;
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                bool isValid = pivSession.TryAuthenticateManagementKey();
                Assert.True(isValid);

                byte[] keyData = {
                    0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                    0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                    0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                    0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
                };
                var setCmd = new SetManagementKeyCommand(keyData, PivTouchPolicy.Never, PivAlgorithm.Aes256);

                SetManagementKeyResponse setRsp = pivSession.Connection.SendCommand(setCmd);
                Assert.Equal(ResponseStatus.Success, setRsp.Status);
            }
        }

        [Fact]
        public void SetKey_TDes_Succeeds()
        {
            if (yubiKey.FirmwareVersion < new FirmwareVersion(5, 4, 2))
            {
                return;
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                bool isValid = pivSession.TryAuthenticateManagementKey();
                Assert.True(isValid);

                byte[] keyData = {
                    0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                    0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                    0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58
                };
                var setCmd = new SetManagementKeyCommand(keyData);

                SetManagementKeyResponse setRsp = pivSession.Connection.SendCommand(setCmd);
                Assert.Equal(ResponseStatus.Success, setRsp.Status);
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
