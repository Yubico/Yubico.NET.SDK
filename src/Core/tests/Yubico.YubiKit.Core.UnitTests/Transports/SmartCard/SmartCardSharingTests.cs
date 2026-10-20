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
using Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.Transports.SmartCard;

/// <summary>
///     How a PC/SC handle is opened and released. The PIV PIN security status is card-wide
///     (NIST SP 800-73-5 Part 2 §2.4.2), so a shared handle opened by another local process can use a PIN=ONCE
///     slot during and after an SDK session unless the SDK holds the card exclusively and powers it off on
///     release. Default: exclusive with shared fallback, SCARD_UNPOWER_CARD on disconnect.
/// </summary>
[Trait("Audit", "YESDK-1630")]
public class SmartCardSharingTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<ISmartCardConnection> ConnectAsync(ControlledSCardConnectionApi api, SmartCardSharing? sharing = null)
    {
        var factory = sharing is { } s ? new SmartCardConnectionFactory(api, s) : new SmartCardConnectionFactory(api);
        return await PcscTestDevices.Create(factory, out _).ConnectAsync<ISmartCardConnection>(Ct);
    }

    [Fact]
    public async Task Default_OpensExclusively()
    {
        var api = new ControlledSCardConnectionApi();

        await using var connection = await ConnectAsync(api);

        Assert.Equal([SCARD_SHARE.EXCLUSIVE], api.ConnectShareModes);
        Assert.True(connection.IsExclusive);
    }

    /// <summary>Another client already holds the card: the exclusive attempt fails and a shared one follows.</summary>
    [Fact]
    public async Task Default_FallsBackToSharedWhenAnotherClientHoldsTheCard()
    {
        var api = new ControlledSCardConnectionApi { RefuseExclusive = true };

        await using var connection = await ConnectAsync(api);

        Assert.Equal([SCARD_SHARE.EXCLUSIVE, SCARD_SHARE.SHARED], api.ConnectShareModes);
        Assert.False(connection.IsExclusive);
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

    [Fact]
    public async Task DefaultSharing_IsUsedByFactoriesWithoutAnExplicitMode()
    {
        var previous = SmartCardConnectionFactory.DefaultSharing;
        try
        {
            SmartCardConnectionFactory.DefaultSharing = SmartCardSharing.Shared;
            var api = new ControlledSCardConnectionApi();

            await using var connection = await ConnectAsync(api);

            Assert.Equal([SCARD_SHARE.SHARED], api.ConnectShareModes);
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
    [Fact]
    public async Task Release_PowersTheCardOff()
    {
        var api = new ControlledSCardConnectionApi();
        var connection = await ConnectAsync(api);

        await connection.DisposeAsync();

        Assert.Equal([SCARD_DISPOSITION.UNPOWER_CARD], api.DisconnectDispositions);
    }
}