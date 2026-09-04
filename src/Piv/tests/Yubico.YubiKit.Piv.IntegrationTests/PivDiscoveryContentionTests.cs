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

using System.Diagnostics;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

/// <summary>
///     Proves contention bugs between device discovery/enumeration and already-open applet sessions.
///     Human-coordinated: these tests mutate PIV state (reset, key generation) and hold the card busy
///     for the duration of an RSA-4096 on-card key generation.
/// </summary>
public class PivDiscoveryContentionTests
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
    ///     Bug A: a discovery scan whose metadata/identity reads land on a card that is busy inside a
    ///     long-running operation must not stall until the card operation finishes. The reads are
    ///     best-effort with a bounded (3s) budget by design; today the in-flight PC/SC transmit ignores
    ///     that budget, so the scan queues behind the RSA-4096 keygen for its full duration — backing up
    ///     the monitor pipeline behind FindYubiKeys' scan lock.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task FindAllAsync_WhileCardBusyWithRsa4096Keygen_CompletesWithoutWaitingForKeygen(
        YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        // Occupy the card with a long on-card operation; deliberately not awaited yet.
        var keygenTask = session.GenerateKeyAsync(PivSlot.Retired1, PivAlgorithm.Rsa4096);

        var scanElapsed = TimeSpan.Zero;
        try
        {
            // Let the GENERATE ASYMMETRIC APDU reach the wire.
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            Assert.False(keygenTask.IsCompleted,
                "RSA-4096 keygen finished before the scan started; card was never busy, test proves nothing.");

            // Fresh FindYubiKeys instance = cold identity/metadata caches, exactly what a first
            // enumeration or post-replug rescan does. Its composite metadata read opens a second
            // shared-mode CCID handle to the busy card.
            var finder = FindYubiKeys.Create();
            var stopwatch = Stopwatch.StartNew();
            var devices = await finder.FindAllAsync(ConnectionType.All);
            stopwatch.Stop();
            scanElapsed = stopwatch.Elapsed;

            Assert.NotEmpty(devices);
        }
        finally
        {
            // Always drain the keygen before the session disposes: abandoning an in-flight native
            // transmit would leave PC/SC in an undefined state. Outcome is observed below.
            await ((Task)keygenTask).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
        }

        var publicKey = await keygenTask;
        Assert.NotNull(publicKey);

        // Metadata reads budget 3s per read; 4s gives margin. Pre-fix this stalls for the full
        // keygen duration (tens of seconds).
        Assert.True(scanElapsed < TimeSpan.FromSeconds(4),
            $"Discovery scan stalled for {scanElapsed} behind the in-flight RSA-4096 keygen; " +
            "best-effort discovery reads must be time-bounded.");
    }

    /// <summary>
    ///     Bug B: a cold-cache discovery scan's best-effort metadata read opens a SECOND shared-mode CCID
    ///     handle to the card and issues SELECT Management on it. PC/SC shared handles share the card's
    ///     basic logical channel, so that SELECT deselects the PIV applet and destroys the open session's
    ///     security state (verified PIN). Enumerating devices must not break an already-open, authenticated
    ///     applet session: a PIN-gated sign that succeeded before the scan must still succeed after it.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task FindAllAsync_WhilePivSessionHasVerifiedPin_DoesNotClobberSessionState(
        YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        _ = await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256, new PivKeyCreationOptions { PinPolicy = PivPinPolicy.Once });
        await session.VerifyPinAsync(DefaultPin);

        var digest = SHA256.HashData("discovery contention"u8);

        // Baseline: the PIN-gated sign works on the open session.
        var before = await session.SignOrDecryptAsync(PivSlot.Authentication, PivAlgorithm.EccP256, digest);
        Assert.NotEqual(0, before.Length);

        // Fresh FindYubiKeys instance = cold caches, exactly what a first enumeration or
        // post-replug rescan does. Its metadata read SELECTs Management on a second handle.
        var devices = await FindYubiKeys.Create().FindAllAsync(ConnectionType.All);
        Assert.NotEmpty(devices);

        // The already-open session must be unaffected by passive enumeration. Pre-fix this throws:
        // the PIV applet was deselected (SW 6D00/6E00) or its PIN state reset (SW 6982).
        var after = await session.SignOrDecryptAsync(PivSlot.Authentication, PivAlgorithm.EccP256, digest);
        Assert.NotEqual(0, after.Length);
    }
}