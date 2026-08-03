// Copyright 2026 Yubico AB
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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

/// <summary>
///     The session-vs-session boundary on one physical key: an ordinary public API call must not
///     silently destroy an open applet session by taking the CCID interface out from under it.
/// </summary>
/// <remarks>
///     <para>
///         This is the motivating defect of the session-contention effort
///         (<c>docs/plans/session-contention/ISA.md</c>, ISC-1). Four lines of ordinary public API,
///         default settings, one process:
///     </para>
///     <code>
///     await using var piv = await key.CreatePivSessionAsync();  // CCID handle #1, SELECT PIV
///     await piv.VerifyPinAsync(pin);                            // security state established
///     var info = await key.GetDeviceInfoAsync();                // CCID handle #2, SELECT Management
///     await piv.SignAsync(...);                                 // pre-fix: SW=0x6D00
///     </code>
///     <para>
///         <see cref="PivDiscoveryContentionTests" /> covers the same hazard reached through
///         <em>discovery</em>. These tests cover it reached through the <em>public applet API</em>,
///         which is the path a consumer actually writes. Both must hold.
///     </para>
///     <para>
///         Human-coordinated: these mutate PIV state (reset, key generation) on the key they use.
///     </para>
/// </remarks>
public class PivSessionContentionTests
{
    private static readonly byte[] DefaultTripleDesManagementKey =
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    ];

    private static readonly byte[] DefaultAesManagementKey =
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    ];

    private static readonly byte[] DefaultPin = "123456"u8.ToArray();

    private static byte[] GetDefaultManagementKey(FirmwareVersion version) =>
        version >= new FirmwareVersion(5, 7, 0) ? DefaultAesManagementKey : DefaultTripleDesManagementKey;

    /// <summary>
    ///     Provisions a PIV slot with an EccP256 key under PIN policy Once and leaves the PIN verified,
    ///     so that a subsequent sign is gated on the security state this test suite is protecting.
    /// </summary>
    private static async Task ArmPinGatedSigningAsync(PivSession session, FirmwareVersion firmware)
    {
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(firmware));
        _ = await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256, PivPinPolicy.Once);
        await session.VerifyPinAsync(DefaultPin);
    }

    /// <summary>
    ///     ISC-1, the whole point. <see cref="IYubiKeyExtensions.GetDeviceInfoAsync" /> is a plain read
    ///     whose default transport order puts SmartCard first. Pre-fix it opened a second CCID handle and
    ///     issued SELECT Management, which on the card's basic logical channel deselected PIV and destroyed
    ///     the verified-PIN state — so the next PIN-gated sign failed with SW=0x6D00.
    ///     Post-fix the call must route around the held CCID interface and leave the victim session intact.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task GetDeviceInfoAsync_WhilePivSessionHasVerifiedPin_DoesNotClobberSessionState(
        YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await ArmPinGatedSigningAsync(session, state.FirmwareVersion);

        var digest = SHA256.HashData("session contention"u8);

        // Baseline: the PIN-gated sign works while this session is the only holder.
        var before = await session.SignOrDecryptAsync(PivSlot.Authentication, PivAlgorithm.EccP256, digest);
        Assert.NotEqual(0, before.Length);

        // The third line of the motivating sequence. Ordinary public API, no exotic conditions.
        var info = await state.Device.GetDeviceInfoAsync();
        Assert.Equal(state.SerialNumber, info.SerialNumber);

        // The fourth line. Pre-fix this throws: PIV was deselected (SW 6D00/6E00) or its PIN state
        // was reset (SW 6982).
        var after = await session.SignOrDecryptAsync(PivSlot.Authentication, PivAlgorithm.EccP256, digest);
        Assert.NotEqual(0, after.Length);
    }

    /// <summary>
    ///     The mechanism behind the test above: Management prefers SmartCard, but when a PIV session holds
    ///     the CCID interface it must fall back to a transport that does not conflict rather than take CCID
    ///     or throw. Asserting <see cref="ManagementSession.Transport" /> — what was actually opened, not
    ///     what was requested — is what stops this from silently passing over SmartCard.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task CreateManagementSessionAsync_WhilePivHoldsCcid_OpensOverANonSmartCardTransport(
        YubiKeyTestState state)
    {
        Skip.If(state.Device.AvailableConnections == ConnectionType.SmartCard,
            "Requires a key exposing a HID transport alongside SmartCard; a SmartCard-only device " +
            "has no route around the held CCID interface and is expected to throw instead.");

        await using var session = await state.Device.CreatePivSessionAsync();
        await ArmPinGatedSigningAsync(session, state.FirmwareVersion);

        await using var management = await state.Device.CreateManagementSessionAsync();

        Assert.NotEqual(ConnectionType.SmartCard, management.Transport);

        var info = await management.GetDeviceInfoAsync();
        Assert.Equal(state.SerialNumber, info.SerialNumber);

        // Routing around the lease is only correct if the lease holder is still healthy afterwards.
        var digest = SHA256.HashData("management fallback"u8);
        var signature = await session.SignOrDecryptAsync(PivSlot.Authentication, PivAlgorithm.EccP256, digest);
        Assert.NotEqual(0, signature.Length);
    }

    /// <summary>
    ///     ISC-1's other half: where no safe route exists the SDK must fail loudly and name the holder,
    ///     never silently deselect. A direct second CCID connection has nowhere to go, so it is refused at
    ///     acquisition — before anything reaches the wire.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task ConnectAsync_SecondSmartCardConnection_WhilePivSessionOpen_IsRefused(
        YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await ArmPinGatedSigningAsync(session, state.FirmwareVersion);

        var exception = await Assert.ThrowsAsync<ConnectionInUseException>(async () =>
        {
            await using var second = await state.Device.ConnectAsync<ISmartCardConnection>();
        });

        // "Fails loudly with an error naming the current holder" is the criterion, so the message is
        // part of the contract, not incidental.
        Assert.Contains("SmartCard", exception.Message, StringComparison.Ordinal);

        // The refusal must be non-destructive: the victim keeps its verified-PIN state.
        var digest = SHA256.HashData("refused acquisition"u8);
        var signature = await session.SignOrDecryptAsync(PivSlot.Authentication, PivAlgorithm.EccP256, digest);
        Assert.NotEqual(0, signature.Length);
    }

    /// <summary>
    ///     The documented migration path for the ownership change. "One session at a time per interface" is
    ///     only acceptable because a caller who owns the connection can run successive applet sessions over
    ///     it — dispose session A, construct session B on the same handle, no reconnect and no
    ///     re-enumeration. Disposing a session must therefore NOT dispose a caller-created connection.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task SuccessiveSessions_OverOneCallerOwnedConnection_BothReachTheCard(
        YubiKeyTestState state)
    {
        await using var connection = await state.Device.ConnectAsync<ISmartCardConnection>();

        int pivSerial;
        await using (var piv = await PivSession.CreateAsync(connection))
        {
            pivSerial = await piv.GetSerialNumberAsync();
            Assert.Equal(state.SerialNumber, pivSerial);
        }

        // The PIV session is gone; the connection it borrowed is not. A second session over the same
        // handle must both be permitted and actually reach the card.
        await using (var management = await ManagementSession.CreateAsync(connection))
        {
            Assert.Equal(ConnectionType.SmartCard, management.Transport);

            var info = await management.GetDeviceInfoAsync();
            Assert.Equal(state.SerialNumber, info.SerialNumber);
        }

        // And a third, back on the original applet: ownership transfer is not a one-shot.
        await using (var piv = await PivSession.CreateAsync(connection))
        {
            Assert.Equal(pivSerial, await piv.GetSerialNumberAsync());
        }
    }

    /// <summary>
    ///     The guard is per-connection, not per-process: two live sessions over ONE connection are refused
    ///     even when they are the same applet, because they would share the card's security state.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task SecondSession_OnOneLiveConnection_IsRefused(YubiKeyTestState state)
    {
        await using var connection = await state.Device.ConnectAsync<ISmartCardConnection>();
        await using var first = await PivSession.CreateAsync(connection);

        await Assert.ThrowsAsync<ConnectionInUseException>(async () =>
        {
            await using var second = await ManagementSession.CreateAsync(connection);
        });

        // The refused construction must not have damaged the incumbent.
        Assert.Equal(state.SerialNumber, await first.GetSerialNumberAsync());
    }
}