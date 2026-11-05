// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.UnitTests.Devices;
using Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.Transports.SmartCard;

[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class SmartCardConnectionFactoryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ExternalFactory_CreateAsync_IsDispatched()
    {
        var factory = new SuccessfulSmartCardConnectionFactory();
        var device = PcscTestDevices.Create(factory, out _);

        await using var connection = await device.ConnectAsync<ISmartCardConnection>(Ct);

        Assert.Equal(1, factory.CreateCalls);
    }

    [Fact]
    public async Task ExternalFactory_SuccessfulConnectionDispose_ReleasesClaimAndAllowsReconnect()
    {
        var factory = new SuccessfulSmartCardConnectionFactory();
        var device = PcscTestDevices.Create(factory, out var slot);

        var connection = await device.ConnectAsync<ISmartCardConnection>(Ct);
        Assert.True(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));

        await connection.DisposeAsync();

        Assert.False(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
        await using var reopened = await device.ConnectAsync<ISmartCardConnection>(Ct);

        Assert.Equal(2, factory.CreateCalls);
    }

    [Fact]
    public async Task CreateDefault_UsesConfiguredYubiKitLoggerBeforeRootingNativeOwner()
    {
        var factory = SmartCardConnectionFactory.CreateDefault();
        var device = PcscTestDevices.Create(factory, out var slot);
        var failure = new InvalidOperationException("configured default logger failure");
        var loggerFactory = new ThrowingLoggerFactory(failure);
        using var logging = YubiKitLogging.UseTemporary(loggerFactory);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => device.ConnectAsync<ISmartCardConnection>(Ct));

        Assert.Same(failure, actual);
        Assert.Equal(1, loggerFactory.CreateCalls);
        Assert.False(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
    }

    [Fact]
    public void DirectConnection_DefaultLogger_UsesConfiguredYubiKitLoggerBeforeRootingNativeOwner()
    {
        var failure = new InvalidOperationException("configured direct logger failure");
        var loggerFactory = new ThrowingLoggerFactory(failure);
        using var logging = YubiKitLogging.UseTemporary(loggerFactory);
        var pcscDevice = new PcscDevice
        {
            ReaderName = $"async-boundary-direct-logger-{Guid.NewGuid():N}",
            Atr = null
        };

        var actual = Assert.Throws<InvalidOperationException>(() => new UsbSmartCardConnection(pcscDevice));

        Assert.Same(failure, actual);
        Assert.Equal(1, loggerFactory.CreateCalls);
    }

    [Fact]
    public async Task PublicFactory_UsesYubiKitLoggingConfiguredAfterConstruction()
    {
        var factory = new SmartCardConnectionFactory();
        var device = PcscTestDevices.Create(factory, out var slot);
        var failure = new InvalidOperationException("configured public factory logger failure");
        var loggerFactory = new ThrowingLoggerFactory(failure);
        using var logging = YubiKitLogging.UseTemporary(loggerFactory);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => device.ConnectAsync<ISmartCardConnection>(Ct));

        Assert.Same(failure, actual);
        Assert.Equal(1, loggerFactory.CreateCalls);
        Assert.False(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
    }

    [Fact]
    public async Task BuiltInPcscConnection_PreOwnerLoggerFailure_ReleasesPhysicalClaim()
    {
        var pcscDevice = new PcscDevice
        {
            ReaderName = $"async-boundary-logger-{Guid.NewGuid():N}",
            Atr = null
        };
        var factory = new SmartCardConnectionFactory();
        var slot = new PcscConnectionSlot(pcscDevice, factory);
        var device = new YubiKeyDevice(slot.InterfaceId, slot, hidFido: null, hidOtp: null, deviceInfo: null);
        using var logging = YubiKitLogging.UseTemporary(new ThrowingLoggerFactory());

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => device.ConnectAsync<ISmartCardConnection>(Ct));

        Assert.False(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
        using var claim = await DeviceConnectionRegistry.AcquireConnectionAsync([slot.InterfaceId], Ct);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CustomFactory_PreOpenFailure_ReleasesClaimAndAllowsRetry(bool cancellation)
    {
        Exception failure = cancellation
            ? new OperationCanceledException("custom factory canceled before returning a connection")
            : new SCardException("custom factory found no card", ErrorCode.SCARD_E_NO_SMARTCARD);
        var factory = new FailOnceSmartCardConnectionFactory(failure);
        var device = PcscTestDevices.Create(factory, out var slot);

        _ = await Assert.ThrowsAnyAsync<Exception>(() => device.ConnectAsync<ISmartCardConnection>(Ct));

        Assert.False(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
        await using var connection = await device.ConnectAsync<ISmartCardConnection>(Ct);
        Assert.Equal(2, factory.CreateCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CustomFactory_ExplicitUnrecoveredOpenFailureRetainsClaim(bool discovery)
    {
        var cause = new InvalidOperationException("custom factory could not prove native cleanup");
        var factory = new AlwaysFailingSmartCardConnectionFactory(
            new UnrecoveredConnectionException("custom factory retained native ownership", cause));
        var device = PcscTestDevices.Create(factory, out _);

        if (discovery)
        {
            _ = await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => ProtocolDeviceInfo.ReadBoundedAsync(
                device,
                ConnectionType.SmartCard,
                TimeSpan.FromSeconds(5),
                NullLogger.Instance,
                Ct));
        }
        else
        {
            _ = await Assert.ThrowsAsync<UnrecoveredConnectionException>(
                () => device.ConnectAsync<ISmartCardConnection>(Ct));
        }

        _ = await Assert.ThrowsAsync<UnrecoveredConnectionException>(
            () => device.ConnectAsync<ISmartCardConnection>(Ct));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task BuiltInPcscConnection_InitializeLoggerFailureReleasesNativeResourcesAndClaim(int throwingLogCall)
    {
        var api = new ControlledSCardConnectionApi();
        var factory = new SmartCardConnectionFactory(api);
        var device = PcscTestDevices.Create(factory, out var slot);
        using var logging = YubiKitLogging.UseTemporary(new ThrowingLogLoggerFactory(throwingLogCall));

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => device.ConnectAsync<ISmartCardConnection>(Ct));

        Assert.Equal(throwingLogCall == 1 ? 0 : 1, api.ConnectCalls);
        Assert.Equal(throwingLogCall == 1 ? 0 : 1, api.DisconnectCalls);
        Assert.Equal(throwingLogCall == 1 ? 0 : 1, api.ReleaseContextCalls);
        Assert.False(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
        using var claim = await DeviceConnectionRegistry.AcquireConnectionAsync([slot.InterfaceId], Ct);
    }

    [Fact]
    public Task DelegatingFactory_BuiltInPartialOpenWithUnprovenCleanupRetainsClaim() =>
        PcscIsolatedProbe.RunAsync(
            "delegating-partial-open",
            typeof(SmartCardConnectionFactoryTests),
            nameof(DelegatingFactory_BuiltInPartialOpenWithUnprovenCleanupRetainsClaim),
            () => PcscCleanupFailureProbe.RunAsync("delegating-partial-open"));
}