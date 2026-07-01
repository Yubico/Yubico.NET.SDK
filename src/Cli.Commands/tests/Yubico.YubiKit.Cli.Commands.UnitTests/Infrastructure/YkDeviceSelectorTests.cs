// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Cli.Commands.UnitTests.Infrastructure;

public sealed class YkDeviceSelectorTests
{
    [Fact]
    public async Task FindDevicesWithRetryAsync_WithSerial_FiltersToMatchingDevice()
    {
        var first = new FakeYubiKey("first", ConnectionType.SmartCard);
        var second = new FakeYubiKey("second", ConnectionType.SmartCard);
        var selector = new TestYkDeviceSelector(
            [ConnectionType.SmartCard],
            serial: 222,
            requestedTransport: null,
            devices: [first, second],
            serialsByDeviceId: new Dictionary<string, int>
            {
                ["first"] = 111,
                ["second"] = 222
            });

        var result = await selector.FindDevicesWithRetryAsync(TestContext.Current.CancellationToken);

        var device = Assert.Single(result);
        Assert.Equal("second", device.DeviceId);
    }

    [Fact]
    public async Task FindDevicesWithRetryAsync_WithRequestedTransport_FiltersToRequestedTransport()
    {
        var smartCard = new FakeYubiKey("smartcard", ConnectionType.SmartCard);
        var fido = new FakeYubiKey("fido", ConnectionType.HidFido);
        var selector = new TestYkDeviceSelector(
            [ConnectionType.SmartCard, ConnectionType.HidFido],
            serial: null,
            requestedTransport: ConnectionType.HidFido,
            devices: [smartCard, fido],
            serialsByDeviceId: new Dictionary<string, int>());

        var result = await selector.FindDevicesWithRetryAsync(TestContext.Current.CancellationToken);

        var device = Assert.Single(result);
        Assert.Equal("fido", device.DeviceId);
    }

    [Fact]
    public void Constructor_WithUnsupportedRequestedTransport_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new YkDeviceSelector(
                [ConnectionType.SmartCard],
                serial: null,
                requestedTransport: ConnectionType.HidOtp));

        Assert.Contains("not supported", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TestYkDeviceSelector : YkDeviceSelector
    {
        private readonly IReadOnlyList<IYubiKey> _devices;
        private readonly IReadOnlyDictionary<string, int> _serialsByDeviceId;

        public TestYkDeviceSelector(
            ConnectionType[] supportedTypes,
            int? serial,
            ConnectionType? requestedTransport,
            IReadOnlyList<IYubiKey> devices,
            IReadOnlyDictionary<string, int> serialsByDeviceId)
            : base(supportedTypes, serial, requestedTransport)
        {
            _devices = devices;
            _serialsByDeviceId = serialsByDeviceId;
        }

        protected override Task<IReadOnlyList<IYubiKey>> FindAllDevicesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_devices);

        protected override Task<DeviceInfo?> GetDeviceInfoAsync(IYubiKey device, CancellationToken cancellationToken) =>
            Task.FromResult<DeviceInfo?>(CreateDeviceInfo(_serialsByDeviceId.GetValueOrDefault(device.DeviceId)));

        protected override bool HandleNonInteractiveNoDevices() => true;
    }

    private sealed class FakeYubiKey : IYubiKey
    {
        public FakeYubiKey(string deviceId, ConnectionType availableConnections)
        {
            DeviceId = deviceId;
            AvailableConnections = availableConnections;
        }

        public string DeviceId { get; }

        public ConnectionType AvailableConnections { get; }

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            throw new NotSupportedException("This fake does not create hardware connections.");
    }

    private static DeviceInfo CreateDeviceInfo(int serialNumber) =>
        new()
        {
            IsSky = false,
            IsFips = false,
            FormFactor = FormFactor.Unknown,
            SerialNumber = serialNumber,
            IsLocked = false,
            UsbEnabled = DeviceCapabilities.None,
            UsbSupported = DeviceCapabilities.None,
            NfcEnabled = DeviceCapabilities.None,
            NfcSupported = DeviceCapabilities.None,
            ResetBlocked = DeviceCapabilities.None,
            FipsCapabilities = DeviceCapabilities.None,
            FipsApproved = DeviceCapabilities.None,
            HasPinComplexity = false,
            PartNumber = null,
            IsNfcRestricted = false,
            AutoEjectTimeout = 0,
            ChallengeResponseTimeout = ReadOnlyMemory<byte>.Empty,
            DeviceFlags = DeviceFlags.None,
            FirmwareVersion = new FirmwareVersion(5, 8, 0),
            VersionQualifier = new VersionQualifier(new FirmwareVersion(5, 8, 0), VersionQualifierType.Final, 0)
        };
}
