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

using System.Buffers.Binary;
using System.Collections.Concurrent;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.UnitTests.Infrastructure;
using Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Apdu.Fakes;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
///     Fault-injection harness for <see cref="FindYubiKeys" /> (see
///     docs/architecture/device-discovery-guarantees.md): scripted identity-read outcomes per interface via
///     constructor fakes, exercising the identity cache (convergence, eviction, reader-rename behavior) and
///     deterministically reproducing the Phase-0 scan-1 "aborted" identity-read failures.
/// </summary>
/// <remarks>
///     <para>
///         All vectors here are PINs (expected-behavior contracts; they may pass against current code and
///         are never fix evidence). Successful identity reads are scripted by returning a
///         <see cref="FakeSmartCardConnection" /> preloaded with a SELECT response and a Management
///         device-info page carrying the scripted serial — <c>ProtocolDeviceInfo.ReadAsync</c> switches on
///         the CONNECTION object type, so HID interface fakes can use the same smart-card scripting seam.
///     </para>
///     <para>
///         The identity-cache pins use a two-key 0x0405 (OTP+CCID) rig — four concurrent identity reads
///         against the four-worker process-wide discovery admission — so they never depend on admission
///         behavior. Oversubscription itself (six reads, four workers) is covered separately: identity
///         reads now WAIT for a worker slot, bounded by their read budget, instead of skipping (see
///         <see cref="FindAllAsync_SixIdentityReadsAgainstFourWorkerAdmission_WaitForSlotsInsteadOfSkipping" />
///         and the bound-preservation vector
///         <see cref="FindAllAsync_SaturatedWorkersBeyondBudget_IdentityDegradesToNull_BoundPreserved" />).
///     </para>
/// </remarks>
[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class FindYubiKeysFaultInjectionTests
{
    private const ConnectionType Triple =
        ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp;

    private const ConnectionType Dual = ConnectionType.SmartCard | ConnectionType.HidOtp;

    // Two-key 0x0405 rig (identity-cache pins; 4 concurrent identity reads ≤ 4 admission workers).
    private const string ReaderA = "Yubico YubiKey OTP+CCID";
    private const string ReaderB = "Yubico YubiKey OTP+CCID 01";
    private const string ReaderARenamed = "Yubico YubiKey OTP+CCID 02";
    private const string OtpA = "hidA-otp";
    private const string OtpB = "hidB-otp";

    // Two-key 0x0407 rig (admission-saturation reproduction; 6 concurrent identity reads).
    private const string TripleReaderA = "Yubico YubiKey OTP+FIDO+CCID";
    private const string TripleReaderB = "Yubico YubiKey OTP+FIDO+CCID 01";
    private const string TripleFidoA = "hidA-fido";
    private const string TripleOtpA = "hidA-otp3";
    private const string TripleFidoB = "hidB-fido";
    private const string TripleOtpB = "hidB-otp3";

    [Fact]
    public async Task FindAllAsync_ScriptedIdentityFailure_DeducedIntoAnchoredKey_AndRereadOnNextScan_Pin()
    {
        // Phase-2 updated pin (was: "failed read orphans on scan 1" pre-deduction). With the tier-4
        // pigeonhole deduction, a failed identity read whose interface uniquely fills the single missing
        // slot of exactly one serial-anchored key is attributed there ALREADY on scan 1 — the orphan
        // window closes without waiting for the cache. The cache contract is unchanged and still pinned:
        // failures are not cached, so the failed interface is re-read on the next scan (now with serial
        // evidence), while already-cached interfaces are not re-read.
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        var (find, factory, _, _) = CreateTwoDualKeyRig();
        factory.FailReads(OtpB);

        var scan1 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Equal(2, scan1.Count);
        Assert.All(scan1, d => Assert.Equal(Dual, d.AvailableConnections));
        var keyB1 = Assert.Single(scan1, d => d.DeviceId == "ykphysical:222");
        Assert.Contains(
            Assert.IsType<YubiKeyDevice>(keyB1).InterfaceIds,
            id => id.EndsWith(OtpB, StringComparison.Ordinal));

        var otpAConnectsAfterScan1 = factory.ConnectCalls(OtpA);
        var otpBConnectsAfterScan1 = factory.ConnectCalls(OtpB);
        factory.SucceedReads(OtpB, serial: 222);

        var scan2 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Equal(2, scan2.Count);
        Assert.All(scan2, d => Assert.Equal(Dual, d.AvailableConnections));
        Assert.Contains(scan2, d => d.DeviceId == "ykphysical:111");
        Assert.Contains(scan2, d => d.DeviceId == "ykphysical:222");

        // Cache behavior: the failed interface was re-read; a previously cached interface was not.
        Assert.True(
            factory.ConnectCalls(OtpB) > otpBConnectsAfterScan1,
            "The failed identity read must be retried on the next scan (failures are not cached).");
        Assert.Equal(otpAConnectsAfterScan1, factory.ConnectCalls(OtpA));
    }
    [Fact]
    public async Task FindAllAsync_InterfaceDisappearance_EvictsIdentityCacheEntries_Pin()
    {
        // Verified premise 2 (eviction PIN): identity-cache entries are evicted when their interface
        // disappears. Scan 3 proves it: key B returns with failing reads, and if its scan-1 identities
        // had survived the absence, key B would still group as ykphysical:222 — instead it splits.
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        var (find, factory, pcsc, hid) = CreateTwoDualKeyRig();

        var scan1 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
        Assert.Equal(2, scan1.Count);

        // Key B unplugged: only key A's interfaces remain (PID count drops to 1 → PID merge, no reads).
        pcsc.Devices = [new FakePcscDevice(ReaderA)];
        hid.Devices = [new FakeHidDevice(0x0405, HidInterfaceType.Otp, OtpA)];

        var scan2 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
        var lonely = Assert.Single(scan2);
        Assert.Equal(Dual, lonely.AvailableConnections);

        // Key B replugged, but every identity read on it now fails.
        pcsc.Devices = [new FakePcscDevice(ReaderA), new FakePcscDevice(ReaderB)];
        hid.Devices =
        [
            new FakeHidDevice(0x0405, HidInterfaceType.Otp, OtpA),
            new FakeHidDevice(0x0405, HidInterfaceType.Otp, OtpB)
        ];
        factory.FailReads(ReaderB);
        factory.FailReads(OtpB);
        var readerBConnectsBeforeScan3 = factory.ConnectCalls(ReaderB);

        var scan3 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Equal(3, scan3.Count);
        var keyA = Assert.Single(scan3, d => Assert.IsType<YubiKeyDevice>(d).InterfaceIds.Count > 1);
        Assert.Equal("ykphysical:111", keyA.DeviceId);
        Assert.Equal(Dual, keyA.AvailableConnections);
        Assert.DoesNotContain(scan3, d => d.DeviceId == "ykphysical:222");
        Assert.True(
            factory.ConnectCalls(ReaderB) > readerBConnectsBeforeScan3,
            "Key B's identity must be re-read after replug: its cache entries were evicted while absent.");
    }

    [Fact]
    public async Task FindAllAsync_PcscReaderRenameBetweenScans_OldEntryMissesAndSuccessfulRereadHeals_Pin()
    {
        // Phase 0 finding 4 (rename PIN): pcsc reader-name suffixes are unstable as the reader set
        // changes. The identity cache is keyed by per-interface DeviceId (reader name), so a rename is a
        // cache MISS for the new name (the old entry self-evicts as absent). With a successful re-read
        // under the new name, grouping stays complete — rename costs one re-read, never correctness.
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        var (find, factory, pcsc, _) = CreateTwoDualKeyRig();

        var scan1 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
        Assert.Equal(2, scan1.Count);

        pcsc.Devices = [new FakePcscDevice(ReaderARenamed), new FakePcscDevice(ReaderB)];
        factory.SucceedReads(ReaderARenamed, serial: 111);

        var scan2 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Equal(2, scan2.Count);
        Assert.All(scan2, d => Assert.Equal(Dual, d.AvailableConnections));
        Assert.Contains(scan2, d => d.DeviceId == "ykphysical:111");
        Assert.Contains(scan2, d => d.DeviceId == "ykphysical:222");
        Assert.Equal(1, factory.ConnectCalls(ReaderARenamed));
    }

    [Fact]
    public async Task FindAllAsync_PcscReaderRenameWithFailingReread_RereadsAndDeducesWithoutStaleServe_Pin()
    {
        // Phase 0 finding 4, failure arm (Phase-2 updated pin — was: "orphans conservatively"
        // pre-deduction). When the re-read under the renamed reader FAILS, the scan-1 serial cached
        // under the OLD reader name must NOT be served for the new name: the rename is a cache MISS and
        // the re-read is attempted (proven by the connect-count assertion below). With the tier-4
        // deduction the null-serial renamed CCID then uniquely fills key A's only missing slot and is
        // attributed there — reached via deduction over the current scan's evidence, never via a stale
        // cache entry keyed to a vanished DeviceId.
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        var (find, factory, pcsc, _) = CreateTwoDualKeyRig();

        var scan1 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
        Assert.Equal(2, scan1.Count);

        pcsc.Devices = [new FakePcscDevice(ReaderARenamed), new FakePcscDevice(ReaderB)];
        factory.FailReads(ReaderARenamed);

        var scan2 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Equal(2, scan2.Count);
        Assert.All(scan2, d => Assert.Equal(Dual, d.AvailableConnections));
        var keyA = Assert.Single(scan2, d => d.DeviceId == "ykphysical:111");
        Assert.Contains(
            Assert.IsType<YubiKeyDevice>(keyA).InterfaceIds,
            id => id.EndsWith(ReaderARenamed, StringComparison.Ordinal));
        Assert.True(
            factory.ConnectCalls(ReaderARenamed) > 0,
            "The renamed reader must be re-read (cache keyed by DeviceId cannot carry identity across renames).");
    }

    /// <summary>
    ///     A same-slot swap between scans must not attribute the old key's cached serial to the new key.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The identity cache is keyed by per-interface DeviceId and evicted only when that interface
    ///         is observed absent at scan time. A swap that completes <em>between</em> scans — key B
    ///         unplugged, a same-model key plugged into the same slot — reuses the same PC/SC reader name
    ///         (slot-derived) and the same HID path, so the interface is never observed absent and
    ///         scan-time eviction never fires. Without hotplug-driven invalidation, the new key inherits
    ///         the old key's serial: key substitution, found independently by two consultation runs.
    ///     </para>
    ///     <para>
    ///         The physical swap cannot happen without the OS observing removal and arrival — the same
    ///         events that trigger rescans — so those events are the invalidation signal.
    ///         <see cref="IFindYubiKeys.NotifyTransportActivity" /> is what the device monitor calls at
    ///         event ingress.
    ///     </para>
    /// </remarks>
    [Fact]
    public async Task FindAllAsync_SameSlotSwapWithTransportActivity_RereadsInsteadOfServingTheOldKeysSerial()
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        var (find, factory, _, _) = CreateTwoDualKeyRig();

        var scan1 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
        Assert.Equal(2, scan1.Count);
        Assert.Contains(scan1, d => d.DeviceId == "ykphysical:222");

        // Key B swapped for a same-model key C in the same slot: identical reader name, identical HID
        // path, different physical hardware. Only the firmware answers differently.
        factory.SucceedReads(ReaderB, serial: 333);
        factory.SucceedReads(OtpB, serial: 333);
        var readerBConnectsBeforeScan2 = factory.ConnectCalls(ReaderB);

        // The swap is physically impossible without removal+arrival events; the monitor forwards them.
        find.NotifyTransportActivity(ConnectionType.SmartCard);
        find.NotifyTransportActivity(ConnectionType.Hid);

        var scan2 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Contains(scan2, d => d.DeviceId == "ykphysical:333");
        Assert.DoesNotContain(scan2, d => d.DeviceId == "ykphysical:222");
        Assert.True(
            factory.ConnectCalls(ReaderB) > readerBConnectsBeforeScan2,
            "Transport activity must invalidate cached identity: serving the old key's serial for the " +
            "swapped-in key is key substitution, not a stale cache entry.");
    }

    /// <summary>
    ///     The metadata cache is exposed to the same same-slot swap as the identity cache: it is keyed by
    ///     PhysicalIdentityKey (built from reusable interface ids), so without hotplug-driven invalidation
    ///     the swapped-in key would be published with the departed key's serial and capabilities.
    /// </summary>
    [Fact]
    public async Task FindAllAsync_SameSlotSwapWithTransportActivity_DoesNotServeStaleMetadata()
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        const string fidoPath = "swap-slot-fido";
        var factory = new ScriptedIdentityFactory();
        factory.SucceedReads(fidoPath, serial: 333);
        var find = new FindYubiKeys(
            new MutableFindPcscDevices(),
            new MutableFindHidDevices
            {
                Devices = [new FakeHidDevice(0x0120, HidInterfaceType.Fido, fidoPath)]
            },
            factory.Create);

        var scan1 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
        Assert.Equal(333, Assert.IsType<YubiKeyDevice>(Assert.Single(scan1)).DeviceInfo?.SerialNumber);
        var connectsAfterScan1 = factory.ConnectCalls(fidoPath);

        // Same-model key swapped into the same slot between scans: identical interface id, different
        // hardware. Only the firmware answers differently.
        factory.SucceedReads(fidoPath, serial: 999);
        find.NotifyTransportActivity(ConnectionType.Hid);

        var scan2 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Equal(999, Assert.IsType<YubiKeyDevice>(Assert.Single(scan2)).DeviceInfo?.SerialNumber);
        Assert.True(
            factory.ConnectCalls(fidoPath) > connectsAfterScan1,
            "Transport activity must invalidate cached metadata: serving the old key's DeviceInfo for the " +
            "swapped-in key is key substitution, not a stale cache entry.");
    }

    /// <summary>
    ///     Invalidation is deliberately NOT scoped per transport: activity on one transport evicts all
    ///     cached evidence, including the other transport's.
    /// </summary>
    /// <remarks>
    ///     A composite key's swap surfaces removal/arrival events on multiple transports, and nothing
    ///     guarantees which arrives first. If eviction were scoped to the event's transport, the first
    ///     event would refresh that transport's evidence while the sibling transport still named the
    ///     departed key — splitting one replacement key into phantom devices built from two keys'
    ///     evidence. Re-reading budgeted identities on the next scan is the price of never mixing them.
    /// </remarks>
    [Fact]
    public async Task NotifyTransportActivity_HidOnly_EvictsPcscIdentityEvidenceToo()
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        var (find, factory, _, _) = CreateTwoDualKeyRig();

        var scan1 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
        Assert.Equal(2, scan1.Count);
        var readerAConnectsAfterScan1 = factory.ConnectCalls(ReaderA);
        var readerBConnectsAfterScan1 = factory.ConnectCalls(ReaderB);
        var otpAConnectsAfterScan1 = factory.ConnectCalls(OtpA);

        find.NotifyTransportActivity(ConnectionType.Hid);

        var scan2 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Equal(2, scan2.Count);
        Assert.True(
            factory.ConnectCalls(OtpA) > otpAConnectsAfterScan1,
            "HID activity must invalidate HID-read identity entries.");
        Assert.True(
            factory.ConnectCalls(ReaderA) > readerAConnectsAfterScan1,
            "HID activity must also invalidate PC/SC identity entries: a composite swap's events can " +
            "arrive on one transport first, and retained sibling evidence would mix two keys.");
        Assert.True(
            factory.ConnectCalls(ReaderB) > readerBConnectsAfterScan1,
            "HID activity must also invalidate PC/SC identity entries: a composite swap's events can " +
            "arrive on one transport first, and retained sibling evidence would mix two keys.");
    }

    /// <summary>
    ///     A read that STARTED before hotplug activity and COMPLETED after it must not repopulate the
    ///     just-cleared identity cache: later scans would trust the departed key's serial indefinitely.
    /// </summary>
    [Fact]
    public async Task FindAllAsync_TransportActivityWhileIdentityReadsInFlight_DiscardsTheirCacheWrites()
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        var (find, factory, _, _) = CreateTwoDualKeyRig();
        factory.GateReads(ReaderA, serial: 111);
        factory.GateReads(OtpA, serial: 111);
        factory.GateReads(ReaderB, serial: 222);
        factory.GateReads(OtpB, serial: 222);

        IReadOnlyList<IYubiKey> scan1;
        try
        {
            var scanTask = find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
            Assert.True(
                await AsyncWait.TryWaitUntilAsync(() => factory.TotalConnectCalls >= 4),
                "The identity reads never reached a connect; cannot stage the in-flight interleaving.");

            // Hotplug lands while all four identity reads are in flight; they complete afterwards.
            find.NotifyTransportActivity(ConnectionType.SmartCard);
            factory.ReleaseAllGatedReads();

            scan1 = await scanTask;
        }
        finally
        {
            factory.ReleaseAllGatedReads();
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        }

        // The scan itself publishes what it read; only the cached copies must have been discarded.
        Assert.Equal(2, scan1.Count);
        var connectsAfterScan1 = factory.TotalConnectCalls;
        foreach (var name in new[] { ReaderA, OtpA, ReaderB, OtpB })
            factory.SucceedReads(name, serial: name is ReaderA or OtpA ? 111 : 222);

        var scan2 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Equal(2, scan2.Count);
        Assert.True(
            factory.TotalConnectCalls > connectsAfterScan1,
            "Identity reads completing after transport activity must not repopulate the cache: scan 2 " +
            "served the departed hardware's cached identity instead of re-reading.");
    }

    /// <summary>
    ///     Same interleaving for the metadata cache: a best-effort metadata read completing after hotplug
    ///     activity must not repopulate the cache with the departed key's DeviceInfo.
    /// </summary>
    [Fact]
    public async Task FindAllAsync_TransportActivityWhileMetadataReadInFlight_DiscardsItsCacheWrite()
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        const string fidoPath = "race-slot-fido";
        var factory = new ScriptedIdentityFactory();
        factory.GateReads(fidoPath, serial: 333);
        var find = new FindYubiKeys(
            new MutableFindPcscDevices(),
            new MutableFindHidDevices
            {
                Devices = [new FakeHidDevice(0x0120, HidInterfaceType.Fido, fidoPath)]
            },
            factory.Create);

        try
        {
            var scanTask = find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
            Assert.True(
                await AsyncWait.TryWaitUntilAsync(() => factory.TotalConnectCalls >= 1),
                "The metadata read never reached a connect; cannot stage the in-flight interleaving.");

            find.NotifyTransportActivity(ConnectionType.Hid);
            factory.ReleaseAllGatedReads();

            _ = await scanTask;
        }
        finally
        {
            factory.ReleaseAllGatedReads();
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        }

        // The swapped-in hardware answers with a different serial; a retained stale cache entry would
        // publish 333 without re-reading.
        factory.SucceedReads(fidoPath, serial: 999);

        var scan2 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Equal(
            999,
            Assert.IsType<YubiKeyDevice>(Assert.Single(scan2)).DeviceInfo?.SerialNumber);
    }

    /// <summary>
    ///     A cached identity is valid only for the configuration it was read under: a PID change on the
    ///     same interface id is a cache miss, not a hit.
    /// </summary>
    /// <remarks>
    ///     On the supported platforms a reconfiguration normally changes the per-interface id too (PC/SC
    ///     reader names and HID paths embed the interface set), which self-evicts. This pins the invariant
    ///     for any id scheme where that does not hold: the PID is part of the evidence's validity, not
    ///     incidental metadata.
    /// </remarks>
    [Fact]
    public async Task FindAllAsync_PidChangeOnSameInterfaceId_IsACacheMissNotAHit()
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        var (find, factory, _, hid) = CreateTwoDualKeyRig();

        var scan1 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
        Assert.Equal(2, scan1.Count);
        var otpAConnectsAfterScan1 = factory.ConnectCalls(OtpA);
        var otpBConnectsAfterScan1 = factory.ConnectCalls(OtpB);

        // Both keys reconfigured: the OTP interfaces re-enumerate under PID 0x0407 with unchanged ids.
        // Two 0x0407 OTP instances force the serial path, so the cache is consulted under the new PID.
        hid.Devices =
        [
            new FakeHidDevice(0x0407, HidInterfaceType.Otp, OtpA),
            new FakeHidDevice(0x0407, HidInterfaceType.Otp, OtpB)
        ];

        _ = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.True(
            factory.ConnectCalls(OtpA) > otpAConnectsAfterScan1,
            "An identity cached under PID 0x0405 must not be served for the same interface under 0x0407.");
        Assert.True(
            factory.ConnectCalls(OtpB) > otpBConnectsAfterScan1,
            "An identity cached under PID 0x0405 must not be served for the same interface under 0x0407.");
    }

    [Fact]
    public async Task FindAllAsync_OneSlotUsbDevice_PopulatesBestEffortMetadata()
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        const string fidoPath = "single-slot-fido";
        var factory = new ScriptedIdentityFactory();
        factory.SucceedReads(fidoPath, serial: 333);
        var find = new FindYubiKeys(
            new MutableFindPcscDevices(),
            new MutableFindHidDevices
            {
                Devices = [new FakeHidDevice(0x0120, HidInterfaceType.Fido, fidoPath)]
            },
            factory.Create);

        var result = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        var published = Assert.Single(result);
        Assert.Equal(ConnectionType.HidFido, published.AvailableConnections);
        Assert.Equal(333, Assert.IsType<YubiKeyDevice>(published).DeviceInfo?.SerialNumber);
        Assert.Equal(1, factory.ConnectCalls(fidoPath));
    }

    [Fact]
    public async Task FindAllAsync_NfcDevice_PopulatesBestEffortMetadata()
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        const string readerName = "single-slot-nfc";
        var factory = new ScriptedIdentityFactory();
        factory.SucceedReads(readerName, serial: 444);
        var find = new FindYubiKeys(
            new MutableFindPcscDevices
            {
                Devices = [new FakePcscDevice(readerName, PscsConnectionKind.Nfc)]
            },
            new MutableFindHidDevices(),
            factory.Create);

        var result = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        var published = Assert.Single(result);
        Assert.Equal(ConnectionType.SmartCard, published.AvailableConnections);
        Assert.Equal(444, Assert.IsType<YubiKeyDevice>(published).DeviceInfo?.SerialNumber);
        Assert.Equal(1, factory.ConnectCalls(readerName));
    }

    [Fact]
    public async Task FindAllAsync_ConsumedIdentityBudget_DoesNotAddMetadataOpenAndRetriesLater()
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        var (find, factory, _, _) = CreateTwoDualKeyRig();
        foreach (var name in new[] { ReaderA, ReaderB, OtpA, OtpB })
            factory.FailReads(name);

        var scan1 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Equal(4, scan1.Count);
        Assert.Equal(12, factory.TotalConnectCalls); // three identity attempts per interface; no metadata open

        var scan2 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Equal(4, scan2.Count);
        Assert.Equal(24, factory.TotalConnectCalls);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task FindAllAsync_SixIdentityReadsAgainstFourWorkerAdmission_WaitForSlotsInsteadOfSkipping()
    {
        // Phase-2 RED→GREEN vector for the admission fix (Phase 0 findings 1 & 5; Phase-1 ledger hook a).
        // On a two-key same-PID 0x0407 rig, one scan issues SIX identity reads against the FOUR-worker
        // discovery admission. Pre-change code skipped the two excess reads via the nonblocking
        // TryAcquire (DiscoveryReadSkippedException, logged as "aborted") — orphaning their interfaces.
        // DESIRED: identity reads WAIT for a worker slot (bounded by their 2s read budget), so once the
        // first four reads finish, the remaining two run and the scan groups both keys completely.
        // PREDICTED RED REASON (pre-change): only 4 of 6 interfaces ever reach a connect; the scan
        // returns orphaned fragments instead of two complete composites.
        // Choreography keeps it deterministic: the first four connects are gated open (holding all four
        // workers) until the test observes them, then released — pre-change the fifth/sixth reads have
        // deterministically skipped by then; post-change they are waiting and connect next.
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);

        var (find, factory) = CreateTwoTripleKeyRig();
        factory.GateReads(TripleReaderA, serial: 111);
        factory.GateReads(TripleFidoA, serial: 111);
        factory.GateReads(TripleOtpA, serial: 111);
        factory.GateReads(TripleReaderB, serial: 222);
        factory.GateReads(TripleFidoB, serial: 222);
        factory.GateReads(TripleOtpB, serial: 222);

        IReadOnlyList<IYubiKey> result;
        bool allSixConnected;
        try
        {
            var scanTask = find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

            Assert.True(
                await AsyncWait.TryWaitUntilAsync(() => factory.TotalConnectCalls >= 4, TimeSpan.FromSeconds(5)),
                "The first four identity reads never reached a connect; cannot stage admission saturation.");
            factory.ReleaseAllGatedReads();

            allSixConnected = await AsyncWait.TryWaitUntilAsync(() => factory.TotalConnectCalls >= 6, TimeSpan.FromSeconds(3));
            factory.ReleaseAllGatedReads();

            result = await scanTask;
        }
        finally
        {
            factory.ReleaseAllGatedReads();
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        }

        Assert.True(
            allSixConnected,
            $"Identity reads must WAIT for a worker slot, not skip: only {factory.TotalConnectCalls} of 6 " +
            "interfaces ever reached a connect (admission saturation skipped the rest).");
        Assert.Equal(2, result.Count);
        Assert.All(result, d => Assert.Equal(Triple, d.AvailableConnections));
        Assert.Contains(result, d => d.DeviceId == "ykphysical:111");
        Assert.Contains(result, d => d.DeviceId == "ykphysical:222");
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task FindAllAsync_SaturatedWorkersBeyondBudget_IdentityDegradesToNull_BoundPreserved()
    {
        // Phase-2 bound-preservation vector: the admission bound must survive the waiting change. Six
        // identity reads, four workers, and every admitted connect hangs (a hung native call). The two
        // waiting reads must NOT connect while the bound is saturated (at most four concurrent native
        // reads — hung calls cannot multiply workers), and when their 2s budget expires while still
        // waiting, they degrade to null exactly like any failed best-effort read: the scan completes
        // with a conservative six-way split rather than stalling. Once the hung connects fail and free
        // the workers, the two waiting reads run in the background (waiting, never skipped).
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);

        var (find, factory) = CreateTwoTripleKeyRig();
        foreach (var name in new[] { TripleReaderA, TripleReaderB, TripleFidoA, TripleOtpA, TripleFidoB, TripleOtpB })
            factory.BlockReads(name);

        IReadOnlyList<IYubiKey> result;
        int connectsAtScanEnd;
        bool allSixEventuallyConnected;
        try
        {
            result = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
            connectsAtScanEnd = factory.TotalConnectCalls;
        }
        finally
        {
            factory.FailAllBlockedReads();
            allSixEventuallyConnected = await AsyncWait.TryWaitUntilAsync(
                () => factory.TotalConnectCalls >= 6,
                TimeSpan.FromSeconds(5));
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(6, result.Count);
        Assert.All(result, d => Assert.Single(Assert.IsType<YubiKeyDevice>(d).InterfaceIds));
        Assert.Equal(4, connectsAtScanEnd); // bound preserved: hung native calls never multiply workers
        Assert.True(
            allSixEventuallyConnected,
            "The excess identity reads must wait for a slot (not skip): they never connected after workers freed.");
    }

    // ---------------------------------------------------------------------------------------------
    // Rig construction
    // ---------------------------------------------------------------------------------------------

    private static (FindYubiKeys Find, ScriptedIdentityFactory Factory, MutableFindPcscDevices Pcsc, MutableFindHidDevices Hid)
        CreateTwoDualKeyRig()
    {
        var factory = new ScriptedIdentityFactory();
        factory.SucceedReads(ReaderA, serial: 111);
        factory.SucceedReads(OtpA, serial: 111);
        factory.SucceedReads(ReaderB, serial: 222);
        factory.SucceedReads(OtpB, serial: 222);

        var pcsc = new MutableFindPcscDevices
        {
            Devices = [new FakePcscDevice(ReaderA), new FakePcscDevice(ReaderB)]
        };
        var hid = new MutableFindHidDevices
        {
            Devices =
            [
                new FakeHidDevice(0x0405, HidInterfaceType.Otp, OtpA),
                new FakeHidDevice(0x0405, HidInterfaceType.Otp, OtpB)
            ]
        };

        return (new FindYubiKeys(pcsc, hid, factory.Create), factory, pcsc, hid);
    }

    private static (FindYubiKeys Find, ScriptedIdentityFactory Factory) CreateTwoTripleKeyRig()
    {
        var factory = new ScriptedIdentityFactory();
        var pcsc = new MutableFindPcscDevices
        {
            Devices = [new FakePcscDevice(TripleReaderA), new FakePcscDevice(TripleReaderB)]
        };
        var hid = new MutableFindHidDevices
        {
            Devices =
            [
                new FakeHidDevice(0x0407, HidInterfaceType.Fido, TripleFidoA),
                new FakeHidDevice(0x0407, HidInterfaceType.Otp, TripleOtpA),
                new FakeHidDevice(0x0407, HidInterfaceType.Fido, TripleFidoB),
                new FakeHidDevice(0x0407, HidInterfaceType.Otp, TripleOtpB)
            ]
        };

        return (new FindYubiKeys(pcsc, hid, factory.Create), factory);
    }

    // ---------------------------------------------------------------------------------------------
    // Fakes
    // ---------------------------------------------------------------------------------------------

    private sealed class MutableFindPcscDevices : IFindPcscDevices
    {
        public IReadOnlyList<IPcscDevice> Devices { get; set; } = [];

        public Task<IReadOnlyList<IPcscDevice>> FindAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Devices);
    }

    private sealed class MutableFindHidDevices : IFindHidDevices
    {
        public IReadOnlyList<IHidDevice> Devices { get; set; } = [];

        public Task<IReadOnlyList<IHidDevice>> FindAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Devices);
    }

    private sealed class FakePcscDevice(
        string readerName,
        PscsConnectionKind kind = PscsConnectionKind.Usb) : IPcscDevice
    {
        public string ReaderName { get; } = readerName;
        public AnswerToReset? Atr => null;
        public PscsConnectionKind Kind => kind;
    }

    private sealed class FakeHidDevice(short productId, HidInterfaceType interfaceType, string name) : IHidDevice
    {
        public string ReaderName { get; } = name;
        public HidDescriptorInfo DescriptorInfo { get; } = new() { VendorId = 0x1050, ProductId = productId };
        public HidInterfaceType InterfaceType { get; } = interfaceType;
        public IHidConnection ConnectToFeatureReports() => throw new NotSupportedException();
        public IHidConnection ConnectToIOReports() => throw new NotSupportedException();
    }

    private enum ReadOutcome
    {
        Succeed,
        Fail,
        Block,
        Gated
    }

    /// <summary>
    ///     Creates scripted per-interface slots whose discovery connects follow a
    ///     per-interface script keyed by the underlying device name. DeviceIds carry a per-factory prefix so
    ///     the process-wide registries (DeviceConnectionRegistry, ProtocolDeviceInfo single-flight) never
    ///     collide across tests, while staying stable across scans within one test.
    /// </summary>
    private sealed class ScriptedIdentityFactory
    {
        private readonly string _prefix = $"test-fi:{Guid.NewGuid():N}";
        private readonly ConcurrentDictionary<string, (ReadOutcome Outcome, int Serial)> _scripts = new();
        private readonly ConcurrentDictionary<string, int> _connectCalls = new();
        private readonly ConcurrentBag<TaskCompletionSource<IConnection>> _blocked = [];
        private readonly ConcurrentQueue<(int Serial, TaskCompletionSource<IConnection> Pending)> _gated = new();
        private volatile bool _failAllBlocked;

        public int TotalConnectCalls => _connectCalls.Values.Sum();

        public int ConnectCalls(string name) => _connectCalls.GetValueOrDefault(name);

        public void SucceedReads(string name, int serial) => _scripts[name] = (ReadOutcome.Succeed, serial);

        public void FailReads(string name) => _scripts[name] = (ReadOutcome.Fail, 0);

        public void BlockReads(string name) => _scripts[name] = (ReadOutcome.Block, 0);

        public void GateReads(string name, int serial) => _scripts[name] = (ReadOutcome.Gated, serial);

        /// <summary>Completes every currently pending gated connect with its scripted identity connection.</summary>
        public void ReleaseAllGatedReads()
        {
            while (_gated.TryDequeue(out var gated))
                gated.Pending.TrySetResult(CreateIdentityConnection(gated.Serial));
        }

        /// <summary>Fails all pending blocked connects; later blocked connects fail immediately (sticky).</summary>
        public void FailAllBlockedReads()
        {
            _failAllBlocked = true;
            foreach (var blocked in _blocked)
                blocked.TrySetException(new InvalidOperationException("Expected blocked-read cleanup failure."));
        }

        public IYubiKeyConnectionSlot Create(IDevice device) => device switch
        {
            IPcscDevice p => new ScriptedYubiKey($"{_prefix}:pcsc:{p.ReaderName}", p.ReaderName, ConnectionType.SmartCard, this),
            IHidDevice h => new ScriptedYubiKey(
                $"{_prefix}:hid:{h.ReaderName}",
                h.ReaderName,
                ConnectionTypeMapper.ToConnectionType(h.InterfaceType),
                this),
            _ => throw new NotSupportedException()
        };

        public Task<IConnection> ConnectFor(string name)
        {
            _ = _connectCalls.AddOrUpdate(name, 1, static (_, count) => count + 1);

            if (!_scripts.TryGetValue(name, out var script))
                throw new InvalidOperationException($"No scripted identity-read outcome for '{name}'.");

            switch (script.Outcome)
            {
                case ReadOutcome.Succeed:
                    return Task.FromResult<IConnection>(CreateIdentityConnection(script.Serial));
                case ReadOutcome.Fail:
                    return Task.FromException<IConnection>(
                        new InvalidOperationException($"Scripted identity-read failure for '{name}'."));
                case ReadOutcome.Gated:
                    var pending = new TaskCompletionSource<IConnection>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _gated.Enqueue((script.Serial, pending));
                    return pending.Task;
                default:
                    if (_failAllBlocked)
                    {
                        return Task.FromException<IConnection>(
                            new InvalidOperationException("Expected blocked-read cleanup failure."));
                    }

                    var blocked = new TaskCompletionSource<IConnection>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _blocked.Add(blocked);
                    return blocked.Task;
            }
        }

        // ProtocolDeviceInfo.ReadAsync dispatches on the connection object type, so the smart-card fake
        // scripts a successful device-info read for HID interfaces too: SELECT Management → 0x9000, then
        // one device-info page ([length][TLVs]) carrying the scripted serial → 0x9000.
        private static FakeSmartCardConnection CreateIdentityConnection(int serial)
        {
            var connection = new FakeSmartCardConnection();
            connection.EnqueueResponse(new byte[] { 0x90, 0x00 });
            connection.EnqueueResponse(BuildDeviceInfoResponse(serial));
            return connection;
        }

        private static byte[] BuildDeviceInfoResponse(int serial)
        {
            var serialBytes = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(serialBytes, serial);

            // Required device-info TLV set (mirrors DeviceInfoReaderTests) with a parametrized serial (0x02).
            var encoded = TlvHelper.EncodeAndDisposeList(
            [
                new Tlv(0x0A, [0x00]),
                new Tlv(0x04, [(byte)FormFactor.UsbAKeychain]),
                new Tlv(0x18, [0x00]),
                new Tlv(0x03, [0x00, 0x01]),
                new Tlv(0x01, [0x00, 0x01]),
                new Tlv(0x0E, [0x00]),
                new Tlv(0x0D, [0x00]),
                new Tlv(0x14, [0x00]),
                new Tlv(0x15, [0x00]),
                new Tlv(0x06, [0x00, 0x00]),
                new Tlv(0x07, [0x2A]),
                new Tlv(0x08, [0x00]),
                new Tlv(0x05, [0x05, 0x07, 0x02]),
                new Tlv(0x02, serialBytes)
            ]);

            var response = new byte[encoded.Length + 3];
            response[0] = (byte)encoded.Length;
            encoded.Span.CopyTo(response.AsSpan(1));
            response[^2] = 0x90;
            response[^1] = 0x00;
            return response;
        }
    }

    private sealed class ScriptedYubiKey(
        string deviceId,
        string name,
        ConnectionType connection,
        ScriptedIdentityFactory owner) : IYubiKeyConnectionSlot, IDiscoveryConnectionProvider
    {
        public string InterfaceId { get; } = deviceId;

        public ConnectionType ConnectionType { get; } = connection;

        public Task<IConnection> OpenRawConnectionAsync(
            ConnectionType connectionType,
            CancellationToken cancellationToken) =>
            owner.ConnectFor(name);

        Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
            ConnectionType connectionType,
            CancellationToken cancellationToken) =>
            OpenRawConnectionAsync(connectionType, cancellationToken);
    }
}
