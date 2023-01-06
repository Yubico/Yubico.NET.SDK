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
using System.Collections.Generic;
using Moq;
using Xunit;
using Yubico.Core.Devices;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Devices.SmartCard;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey
{
    internal class TestSmartCardDevice : ISmartCardDevice
    {
        public readonly static ISmartCardDevice AnyInstance = new TestSmartCardDevice()
            { Kind = SmartCardConnectionKind.Any };

        public readonly static ISmartCardDevice NfcInstance = new TestSmartCardDevice()
            { Kind = SmartCardConnectionKind.Nfc };

        public DateTime LastAccessed { get; } = DateTime.Now;
        public string Path { get; } = string.Empty;
        public string? ParentDeviceId { get; } = null;
        public AnswerToReset? Atr { get; }
        public SmartCardConnectionKind Kind { get; private set; }
        public ISmartCardConnection Connect()
        {
            throw new System.NotImplementedException();
        }
    }

    internal class TestHidDevice : IHidDevice
    {
        public readonly static IHidDevice FidoInstance = new TestHidDevice() { UsagePage = HidUsagePage.Fido };
        public readonly static IHidDevice KeyboardInstance = new TestHidDevice() { UsagePage = HidUsagePage.Keyboard };

        public DateTime LastAccessed { get; } = DateTime.Now;
        public string Path { get; } = string.Empty;
        public string? ParentDeviceId { get; } = null;
        public short VendorId { get; }
        public short ProductId { get; }
        public short Usage { get; }
        public HidUsagePage UsagePage { get; private set; }
        public IHidConnection ConnectToFeatureReports()
        {
            throw new System.NotImplementedException();
        }

        public IHidConnection ConnectToIOReports()
        {
            throw new System.NotImplementedException();
        }
    }

    public class ConnectionManagerTests
    {
        private readonly Mock<IYubiKeyDevice> _yubiKeyDeviceMock = new Mock<IYubiKeyDevice>();
        private readonly Mock<ISmartCardDevice> _smartCardDeviceMock = new Mock<ISmartCardDevice>();
        private readonly Mock<ISmartCardConnection> _smartCardConnectionMock = new Mock<ISmartCardConnection>();

        public static IEnumerable<object[]> SupportedApplicationTuples =>
            new List<object[]>()
            {
                new object[] { TestHidDevice.FidoInstance, YubiKeyApplication.FidoU2f },
                new object[] { TestHidDevice.FidoInstance, YubiKeyApplication.Fido2 },
                new object[] { TestHidDevice.KeyboardInstance, YubiKeyApplication.Otp },
                new object[] { TestSmartCardDevice.AnyInstance, YubiKeyApplication.Otp },
                new object[] { TestSmartCardDevice.AnyInstance, YubiKeyApplication.Oath },
                new object[] { TestSmartCardDevice.AnyInstance, YubiKeyApplication.Piv },
                new object[] { TestSmartCardDevice.AnyInstance, YubiKeyApplication.OpenPgp },
                new object[] { TestSmartCardDevice.AnyInstance, YubiKeyApplication.FidoU2f },
                new object[] { TestSmartCardDevice.AnyInstance, YubiKeyApplication.Fido2 },
                new object[] { TestSmartCardDevice.AnyInstance, YubiKeyApplication.Management },
                new object[] { TestSmartCardDevice.AnyInstance, YubiKeyApplication.YubiHsmAuth },
                new object[] { TestSmartCardDevice.NfcInstance, YubiKeyApplication.OtpNdef }
            };

        [Theory]
        [MemberData(nameof(SupportedApplicationTuples))]
        public void DeviceSupportsApplication_GivenSupportedTuple_ReturnsTrue(IDevice device, YubiKeyApplication application)
        {
            Assert.True(ConnectionManager.DeviceSupportsApplication(device, application));
        }

        public static IEnumerable<object[]> UnsupportedApplicationTuples =>
            new List<object[]>()
            {
                new object[] { TestHidDevice.FidoInstance, YubiKeyApplication.Otp },
                new object[] { TestHidDevice.FidoInstance, YubiKeyApplication.OtpNdef },
                new object[] { TestHidDevice.FidoInstance, YubiKeyApplication.Oath },
                new object[] { TestHidDevice.FidoInstance, YubiKeyApplication.Piv },
                new object[] { TestHidDevice.FidoInstance, YubiKeyApplication.OpenPgp },
                new object[] { TestHidDevice.FidoInstance, YubiKeyApplication.Management },
                new object[] { TestHidDevice.FidoInstance, YubiKeyApplication.YubiHsmAuth },
                new object[] { TestHidDevice.KeyboardInstance, YubiKeyApplication.OtpNdef },
                new object[] { TestHidDevice.KeyboardInstance, YubiKeyApplication.Oath },
                new object[] { TestHidDevice.KeyboardInstance, YubiKeyApplication.Piv },
                new object[] { TestHidDevice.KeyboardInstance, YubiKeyApplication.OpenPgp },
                new object[] { TestHidDevice.KeyboardInstance, YubiKeyApplication.Management },
                new object[] { TestHidDevice.KeyboardInstance, YubiKeyApplication.YubiHsmAuth },
            };

        [Theory]
        [MemberData(nameof(UnsupportedApplicationTuples))]
        public void DeviceSupportsApplication_GivenUnsupportedTuple_ReturnsFalse(IDevice device, YubiKeyApplication application)
        {
            Assert.False(ConnectionManager.DeviceSupportsApplication(device, application));
        }

        [Fact]
        public void Instance_ReturnsSameInstanceOfConnectionManager()
        {
            ConnectionManager? connectionManager1 = ConnectionManager.Instance;
            Assert.NotNull(connectionManager1);

            ConnectionManager? connectionManager2 = ConnectionManager.Instance;
            Assert.Same(connectionManager1, connectionManager2);
        }

        [Fact]
        public void TryCreateConnection_NoOpenConnections_ReturnsTrueAndConnection()
        {
            var cm = new ConnectionManager();

            _ = _smartCardDeviceMock
                .Setup(x => x.Connect()).Returns(_smartCardConnectionMock.Object);
            _ = _smartCardConnectionMock
                .Setup(x => x.Transmit(It.IsAny<CommandApdu>()))
                .Returns(new ResponseApdu(Array.Empty<byte>(), SWConstants.Success));

            bool result = cm.TryCreateConnection(
                _yubiKeyDeviceMock.Object,
                _smartCardDeviceMock.Object,
                YubiKeyApplication.Piv,
                out IYubiKeyConnection? connection);

            Assert.True(result);
            Assert.NotNull(connection);
        }

        [Fact]
        public void TryCreateConnection_OpenConnectionToSameYubiKey_ReturnsFalseAndNull()
        {
            var cm = new ConnectionManager();

            _ = _yubiKeyDeviceMock
                .Setup(x => x.Equals(It.IsAny<IYubiKeyDevice>()))
                .Returns(true);
            _ = _smartCardDeviceMock
                .Setup(x => x.Connect()).Returns(_smartCardConnectionMock.Object);
            _ = _smartCardConnectionMock
                .Setup(x => x.Transmit(It.IsAny<CommandApdu>()))
                .Returns(new ResponseApdu(Array.Empty<byte>(), SWConstants.Success));

            _ = cm.TryCreateConnection(
                _yubiKeyDeviceMock.Object,
                _smartCardDeviceMock.Object,
                YubiKeyApplication.Piv,
                out _);

            bool result = cm.TryCreateConnection(
                _yubiKeyDeviceMock.Object,
                _smartCardDeviceMock.Object,
                YubiKeyApplication.Piv,
                out IYubiKeyConnection? connection);

            Assert.False(result);
            Assert.Null(connection);
        }

        [Fact]
        public void TryCreateConnection_OpenConnectionToDifferentYubiKey_ReturnsTrueAndConnection()
        {
            var cm = new ConnectionManager();

            _ = _yubiKeyDeviceMock
                .Setup(x => x.Equals(It.IsAny<IYubiKeyDevice>()))
                .Returns(false);
            _ = _smartCardDeviceMock
                .Setup(x => x.Connect()).Returns(_smartCardConnectionMock.Object);
            _ = _smartCardConnectionMock
                .Setup(x => x.Transmit(It.IsAny<CommandApdu>()))
                .Returns(new ResponseApdu(Array.Empty<byte>(), SWConstants.Success));

            bool result = cm.TryCreateConnection(
                _yubiKeyDeviceMock.Object,
                _smartCardDeviceMock.Object,
                YubiKeyApplication.Piv,
                out IYubiKeyConnection? connection1);

            Assert.True(result);
            Assert.NotNull(connection1);

            result = cm.TryCreateConnection(
                _yubiKeyDeviceMock.Object,
                _smartCardDeviceMock.Object,
                YubiKeyApplication.Piv,
                out IYubiKeyConnection? connection2);

            Assert.True(result);
            Assert.NotNull(connection2);
        }

        [Fact]
        public void TryCreateConnectionOverload_NoOpenConnections_ReturnsTrueAndConnection()
        {
            var cm = new ConnectionManager();

            _ = _smartCardDeviceMock
                .Setup(x => x.Connect()).Returns(_smartCardConnectionMock.Object);
            _ = _smartCardConnectionMock
                .Setup(x => x.Transmit(It.IsAny<CommandApdu>()))
                .Returns(new ResponseApdu(Array.Empty<byte>(), SWConstants.Success));

            bool result = cm.TryCreateConnection(
                _yubiKeyDeviceMock.Object,
                _smartCardDeviceMock.Object,
                new byte[] { 1, 2, 3, 4 },
                out IYubiKeyConnection? connection);

            Assert.True(result);
            Assert.NotNull(connection);
        }

        [Fact]
        public void TryCreateConnectionOverload_OpenConnectionToSameYubiKey_ReturnsFalseAndNull()
        {
            var cm = new ConnectionManager();

            _ = _yubiKeyDeviceMock
                .Setup(x => x.Equals(It.IsAny<IYubiKeyDevice>()))
                .Returns(true);
            _ = _smartCardDeviceMock
                .Setup(x => x.Connect()).Returns(_smartCardConnectionMock.Object);
            _ = _smartCardConnectionMock
                .Setup(x => x.Transmit(It.IsAny<CommandApdu>()))
                .Returns(new ResponseApdu(Array.Empty<byte>(), SWConstants.Success));

            _ = cm.TryCreateConnection(
                _yubiKeyDeviceMock.Object,
                _smartCardDeviceMock.Object,
                new byte[] { 1, 2, 3, 4 },
                out _);

            bool result = cm.TryCreateConnection(
                _yubiKeyDeviceMock.Object,
                _smartCardDeviceMock.Object,
                new byte[] { 1, 2, 3, 4 },
                out IYubiKeyConnection? connection);

            Assert.False(result);
            Assert.Null(connection);
        }

        [Fact]
        public void TryCreateConnectionOverload_OpenConnectionToDifferentYubiKey_ReturnsTrueAndConnection()
        {
            var cm = new ConnectionManager();

            _ = _yubiKeyDeviceMock
                .Setup(x => x.Equals(It.IsAny<IYubiKeyDevice>()))
                .Returns(false);
            _ = _smartCardDeviceMock
                .Setup(x => x.Connect()).Returns(_smartCardConnectionMock.Object);
            _ = _smartCardConnectionMock
                .Setup(x => x.Transmit(It.IsAny<CommandApdu>()))
                .Returns(new ResponseApdu(Array.Empty<byte>(), SWConstants.Success));

            bool result = cm.TryCreateConnection(
                _yubiKeyDeviceMock.Object,
                _smartCardDeviceMock.Object,
                new byte[] { 1, 2, 3, 4 },
                out IYubiKeyConnection? connection1);

            Assert.True(result);
            Assert.NotNull(connection1);

            result = cm.TryCreateConnection(
                _yubiKeyDeviceMock.Object,
                _smartCardDeviceMock.Object,
                new byte[] { 1, 2, 3, 4 },
                out IYubiKeyConnection? connection2);

            Assert.True(result);
            Assert.NotNull(connection2);
        }

        [Fact]
        public void EndConnection_NoOpenConnections_ThrowsNotFoundException()
        {
            var cm = new ConnectionManager();

            void Action() => cm.EndConnection(_yubiKeyDeviceMock.Object);

            _ = Assert.Throws<KeyNotFoundException>(Action);
        }

        [Fact]
        public void EndConnection_OpenConnectionToSameYubiKey_AllowsNewConnection()
        {
            var cm = new ConnectionManager();

            _ = _yubiKeyDeviceMock
                .Setup(x => x.Equals(It.IsAny<IYubiKeyDevice>()))
                .Returns(true);
            _ = _smartCardDeviceMock
                .Setup(x => x.Connect()).Returns(_smartCardConnectionMock.Object);
            _ = _smartCardConnectionMock
                .Setup(x => x.Transmit(It.IsAny<CommandApdu>()))
                .Returns(new ResponseApdu(Array.Empty<byte>(), SWConstants.Success));

            _ = cm.TryCreateConnection(
                _yubiKeyDeviceMock.Object,
                _smartCardDeviceMock.Object,
                YubiKeyApplication.Piv,
                out _);

            cm.EndConnection(_yubiKeyDeviceMock.Object);

            bool result = cm.TryCreateConnection(
                _yubiKeyDeviceMock.Object,
                _smartCardDeviceMock.Object,
                YubiKeyApplication.Piv,
                out IYubiKeyConnection? connection);

            Assert.True(result);
            Assert.NotNull(connection);
        }
    }
}
