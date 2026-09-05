// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Cli.Commands.UnitTests.Infrastructure;

public sealed class YkDeviceContextTests
{
    [Fact]
    public void SessionOptions_ReturnsFreshOptionsWithPreferredConnection()
    {
        var context = new YkDeviceContext
        {
            Device = new FakeYubiKey(),
            Selection = new DeviceSelection(
                new FakeYubiKey(),
                SerialNumber: null,
                FormFactor.Unknown,
                string.Empty,
                ConnectionType.HidFido),
            PreferredConnection = ConnectionType.SmartCard
        };

        var first = context.SessionOptions;
        var second = context.SessionOptions;

        Assert.Equal(ConnectionType.SmartCard, first.PreferredConnectionType);
        Assert.Equal(ConnectionType.SmartCard, second.PreferredConnectionType);
        Assert.NotSame(first, second);
    }

    private sealed class FakeYubiKey : IYubiKey
    {
        public string DeviceId => "fake";

        public ConnectionType AvailableConnections => ConnectionType.HidFido | ConnectionType.SmartCard;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            throw new NotSupportedException();
    }
}
