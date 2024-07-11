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
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Scp03
{
    public class SimpleSessionTests
    {
        private readonly byte[] _pin = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void SessionSetupAndUse_Succeeds(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice device = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            Assert.True(device.FirmwareVersion >= FirmwareVersion.V5_3_0);
            Assert.True(device.HasFeature(YubiKeyFeature.Scp03));

#pragma warning disable CS0618 // Specifically testing this soon-to-be-deprecated feature
            IYubiKeyDevice scp03Device = (device as YubiKeyDevice)!.WithScp03(new StaticKeys());
#pragma warning restore CS0618

            using var piv = new PivSession(scp03Device);
            bool result = piv.TryVerifyPin(new ReadOnlyMemory<byte>(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 }),
                out _);
            Assert.True(result);

            PivMetadata metadata = piv.GetMetadata(PivSlot.Pin)!;
            Assert.Equal(3, metadata.RetryCount);
        }

        [Fact]
        public void ConnectScp03_Application_Succeeds()
        {
            IYubiKeyDevice device = IntegrationTestDeviceEnumeration.GetTestDevice(
                Transport.SmartCard, FirmwareVersion.V5_3_0);

            using var scp03Keys = new StaticKeys();

            using IYubiKeyConnection connection = device.ConnectScp03(YubiKeyApplication.Piv, scp03Keys);
            Assert.NotNull(connection);

            var cmd = new VerifyPinCommand(_pin);
            VerifyPinResponse rsp = connection!.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
        }

        [Fact]
        public void ConnectScp03_AlgorithmId_Succeeds()
        {
            IYubiKeyDevice device = IntegrationTestDeviceEnumeration.GetTestDevice(
                Transport.SmartCard, FirmwareVersion.V5_3_0);

            using var scp03Keys = new StaticKeys();

            using IYubiKeyConnection connection = device.ConnectScp03(
                YubiKeyApplication.Piv.GetIso7816ApplicationId(), scp03Keys);
            Assert.NotNull(connection);

            var cmd = new VerifyPinCommand(_pin);
            VerifyPinResponse rsp = connection!.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
        }

        [Fact]
        public void TryConnectScp03_Application_Succeeds()
        {
            IYubiKeyDevice device = IntegrationTestDeviceEnumeration.GetTestDevice(
                Transport.SmartCard, FirmwareVersion.V5_3_0);

            using var scp03Keys = new StaticKeys();

            bool isValid = device.TryConnectScp03(YubiKeyApplication.Piv, scp03Keys,
                out IScp03YubiKeyConnection? connection);
            using (connection)
            {
                Assert.NotNull(connection);
                Assert.True(isValid);
                var cmd = new VerifyPinCommand(_pin);
                VerifyPinResponse rsp = connection!.SendCommand(cmd);
                Assert.Equal(ResponseStatus.Success, rsp.Status);
            }
        }

        [Fact]
        public void TryConnectScp03_AlgorithmId_Succeeds()
        {
            IYubiKeyDevice device = IntegrationTestDeviceEnumeration.GetTestDevice(
                Transport.SmartCard, FirmwareVersion.V5_3_0);

            using var scp03Keys = new StaticKeys();

            bool isValid = device.TryConnectScp03(
                YubiKeyApplication.Piv.GetIso7816ApplicationId(), scp03Keys, out IScp03YubiKeyConnection? connection);
            using (connection)
            {
                Assert.NotNull(connection);
                Assert.True(isValid);
                var cmd = new VerifyPinCommand(_pin);
                VerifyPinResponse rsp = connection!.SendCommand(cmd);
                Assert.Equal(ResponseStatus.Success, rsp.Status);
            }
        }
    }
}
