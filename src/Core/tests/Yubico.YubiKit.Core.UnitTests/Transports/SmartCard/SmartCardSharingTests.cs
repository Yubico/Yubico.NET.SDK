// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.UnitTests.Devices;
using Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.Transports.SmartCard;

/// <summary>
///     How a PC/SC handle is opened and released. The PIV PIN security status is card-wide
///     (NIST SP 800-73-5 Part 2 §2.4.2), so a shared handle opened by another local process can use a PIN=ONCE
///     slot during and after an SDK session unless the SDK holds the card exclusively and powers it off on
///     release. These no-hardware tests prove strict exclusive mode selection, no shared retry on
///     contention, and SCARD_UNPOWER_CARD on disconnect, not physical card isolation.
/// </summary>
[Trait("Audit", "YESDK-1630")]
[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class SmartCardSharingTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<ISmartCardConnection> ConnectAsync(ControlledSCardConnectionApi api, SmartCardSharing? sharing = null)
    {
        var factory = sharing is { } s ? new SmartCardConnectionFactory(api, s) : new SmartCardConnectionFactory(api);
        return await PcscTestDevices.Create(factory, out _).ConnectAsync<ISmartCardConnection>(Ct);
    }

    [Fact]
    public void DefaultSharing_InitiallyRequiresStrictExclusiveAccess() =>
        Assert.Equal(SmartCardSharing.Exclusive, SmartCardConnectionFactory.DefaultSharing);

    [Fact]
    public async Task Default_OpensExclusively()
    {
        var api = new ControlledSCardConnectionApi();

        await using var connection = await ConnectAsync(api);

        Assert.Equal([SCARD_SHARE.EXCLUSIVE], api.ConnectShareModes);
        Assert.True(connection.IsExclusive);
    }

    [Fact]
    public async Task Default_ContentionFailsWithoutSharedRetryAndReleasesClaim()
    {
        var api = new ControlledSCardConnectionApi { RefuseExclusive = true };
        var device = PcscTestDevices.Create(new SmartCardConnectionFactory(api), out var slot);

        await Assert.ThrowsAsync<ConnectionInUseException>(async () =>
        {
            await using var unexpected = await device.ConnectAsync<ISmartCardConnection>(Ct);
        });

        Assert.Equal([SCARD_SHARE.EXCLUSIVE], api.ConnectShareModes);
        Assert.Equal(1, api.ReleaseContextCalls);
        Assert.Equal(0, api.DisconnectCalls);
        Assert.False(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));

        api.RefuseExclusive = false;
        await using var reopened = await device.ConnectAsync<ISmartCardConnection>(Ct);
        Assert.True(reopened.IsExclusive);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnregisteredFactory_DefaultRequiresExclusiveAccess(bool contention)
    {
        var api = new ControlledSCardConnectionApi { RefuseExclusive = contention };
        var factory = new SmartCardConnectionFactory(api);
        var reader = new PcscDevice { ReaderName = "default-factory", Atr = null };

        if (contention)
        {
            await Assert.ThrowsAsync<ConnectionInUseException>(async () =>
            {
                await using var unexpected = await factory.CreateAsync(reader, Ct);
            });
        }
        else
        {
            await using var connection = await factory.CreateAsync(reader, Ct);
            Assert.True(connection.IsExclusive);
        }

        Assert.Equal([SCARD_SHARE.EXCLUSIVE], api.ConnectShareModes);
        Assert.Equal(1, api.ReleaseContextCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirectConnection_DefaultRequiresExclusiveAccess(bool contention)
    {
        var api = new ControlledSCardConnectionApi { RefuseExclusive = contention };
        var reader = new PcscDevice { ReaderName = "default-connection", Atr = null };
        await using var connection = new UsbSmartCardConnection(reader, api);

        if (contention)
            await Assert.ThrowsAsync<ConnectionInUseException>(() => connection.InitializeAsync(Ct).AsTask());
        else
        {
            await connection.InitializeAsync(Ct);
            Assert.True(connection.IsExclusive);
        }

        Assert.Equal([SCARD_SHARE.EXCLUSIVE], api.ConnectShareModes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NativeState_DefaultRequiresExclusiveAccess(bool contention)
    {
        var api = new ControlledSCardConnectionApi { RefuseExclusive = contention };
        var state = new PcscConnectionNativeState("default-state", api);
        try
        {
            if (contention)
                await Assert.ThrowsAsync<ConnectionInUseException>(() => state.OpenAsync(Ct));
            else
            {
                await state.OpenAsync(Ct);
                Assert.True(state.IsExclusive);
            }

            Assert.Equal([SCARD_SHARE.EXCLUSIVE], api.ConnectShareModes);
        }
        finally
        {
            await state.RequestShutdown();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeResources_DefaultRequiresExclusiveAccess(bool contention)
    {
        var api = new ControlledSCardConnectionApi { RefuseExclusive = contention };
        var resources = new PcscNativeResources("default-resources", api);
        try
        {
            if (contention)
                Assert.Throws<ConnectionInUseException>(resources.Open);
            else
            {
                resources.Open();
                Assert.True(resources.IsExclusive);
            }

            Assert.Equal([SCARD_SHARE.EXCLUSIVE], api.ConnectShareModes);
        }
        finally
        {
            Assert.True(resources.TryRelease(out var cleanupError));
            Assert.Null(cleanupError);
        }
    }

    [Fact]
    public async Task ExplicitFallback_ContentionRetriesShared()
    {
        var api = new ControlledSCardConnectionApi { RefuseExclusive = true };

        await using var connection = await ConnectAsync(api, SmartCardSharing.ExclusiveWithSharedFallback);

        Assert.Equal([SCARD_SHARE.EXCLUSIVE, SCARD_SHARE.SHARED], api.ConnectShareModes);
        Assert.False(connection.IsExclusive);
    }

    [Fact]
    public async Task ExplicitFallback_WithoutContentionOpensExclusively()
    {
        var api = new ControlledSCardConnectionApi();

        await using var connection = await ConnectAsync(api, SmartCardSharing.ExclusiveWithSharedFallback);

        Assert.Equal([SCARD_SHARE.EXCLUSIVE], api.ConnectShareModes);
        Assert.True(connection.IsExclusive);
    }

    [Fact]
    public async Task ExplicitFallback_OtherConnectFailureDoesNotRetryShared()
    {
        var api = new ControlledSCardConnectionApi { FailNextConnect = true };

        var failure = await Assert.ThrowsAsync<SCardException>(
            () => ConnectAsync(api, SmartCardSharing.ExclusiveWithSharedFallback));

        Assert.Equal(unchecked((int)ErrorCode.SCARD_E_NO_SMARTCARD), failure.HResult);
        Assert.Equal([SCARD_SHARE.EXCLUSIVE], api.ConnectShareModes);
        Assert.Equal(1, api.ReleaseContextCalls);
    }

    [Fact]
    public async Task Exclusive_WithoutFallback_ThrowsConnectionInUse()
    {
        var api = new ControlledSCardConnectionApi { RefuseExclusive = true };

        await Assert.ThrowsAsync<ConnectionInUseException>(() => ConnectAsync(api, SmartCardSharing.Exclusive));

        Assert.Equal([SCARD_SHARE.EXCLUSIVE], api.ConnectShareModes);
    }

    [Fact]
    public async Task Shared_NeverTriesExclusive()
    {
        var api = new ControlledSCardConnectionApi();

        await using var connection = await ConnectAsync(api, SmartCardSharing.Shared);

        Assert.Equal([SCARD_SHARE.SHARED], api.ConnectShareModes);
        Assert.False(connection.IsExclusive);
    }

    [Theory]
    [InlineData(SmartCardSharing.Shared)]
    [InlineData(SmartCardSharing.ExclusiveWithSharedFallback)]
    public async Task DefaultSharing_ExplicitOptInIsUsedByNewFactories(SmartCardSharing sharing)
    {
        var previous = SmartCardConnectionFactory.DefaultSharing;
        try
        {
            SmartCardConnectionFactory.DefaultSharing = sharing;
            var api = new ControlledSCardConnectionApi { RefuseExclusive = true };
            var factory = new SmartCardConnectionFactory(api);
            SmartCardConnectionFactory.DefaultSharing = SmartCardSharing.Exclusive;

            await using var connection = await PcscTestDevices.Create(factory, out _).ConnectAsync<ISmartCardConnection>(Ct);

            Assert.Equal(sharing == SmartCardSharing.Shared
                ? [SCARD_SHARE.SHARED]
                : [SCARD_SHARE.EXCLUSIVE, SCARD_SHARE.SHARED], api.ConnectShareModes);
            Assert.False(connection.IsExclusive);
        }
        finally
        {
            SmartCardConnectionFactory.DefaultSharing = previous;
        }
    }

    [Fact]
    public async Task ExplicitSharing_OverridesDefaultSharing()
    {
        var previous = SmartCardConnectionFactory.DefaultSharing;
        try
        {
            SmartCardConnectionFactory.DefaultSharing = SmartCardSharing.Shared;
            var api = new ControlledSCardConnectionApi();

            await using var connection = await ConnectAsync(api, SmartCardSharing.Exclusive);

            Assert.Equal([SCARD_SHARE.EXCLUSIVE], api.ConnectShareModes);
        }
        finally
        {
            SmartCardConnectionFactory.DefaultSharing = previous;
        }
    }

    /// <summary>Power the card off on release so PIN=ONCE state cannot outlive the session.</summary>
    [Theory]
    [InlineData(SmartCardSharing.Exclusive)]
    [InlineData(SmartCardSharing.Shared)]
    [InlineData(SmartCardSharing.ExclusiveWithSharedFallback)]
    public async Task Release_AllModesPowerTheCardOff(SmartCardSharing sharing)
    {
        var api = new ControlledSCardConnectionApi { RefuseExclusive = sharing != SmartCardSharing.Exclusive };
        var connection = await ConnectAsync(api, sharing);

        await connection.DisposeAsync();

        Assert.Equal([SCARD_DISPOSITION.UNPOWER_CARD], api.DisconnectDispositions);
    }
}