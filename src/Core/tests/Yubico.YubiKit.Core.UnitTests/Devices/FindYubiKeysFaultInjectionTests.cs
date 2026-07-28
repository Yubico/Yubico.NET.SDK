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
using Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Apdu.Fakes;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
///     Phase 1 fault-injection harness for <see cref="FindYubiKeys" /> (composite-merge remediation plan,
///     docs/plans/composite-merge-remediation/PLAN.md): scripted identity-read outcomes per interface via
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
///         The identity-cache pins use a two-key 0x0405 (OTP+CCID) rig — FOUR concurrent identity reads —
///         because the process-wide discovery admission has exactly four workers and skips (rather than
///         queues) the excess: a six-read 0x0407 rig nondeterministically orphans up to two interfaces even
///         with instantly-succeeding fakes. That saturation behavior is itself the Phase-0 finding-1/5
///         defect mechanism and is pinned deterministically by
///         <see cref="FindAllAsync_SixIdentityReadsAgainstFourWorkerAdmission_TwoSkipWithoutConnecting" />.
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
    public async Task FindAllAsync_ScriptedIdentityFailureOrphans_SameInstanceHealsWhenRetrySucceeds_Pin()
    {
        // Phase 0 finding 2 (convergence PIN): a failed identity read orphans its interface on scan 1;
        // scan 2 on the SAME FindYubiKeys instance retries the failed read (only successful reads are
        // cached) and heals to complete grouping, without re-reading the already-cached interfaces.
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        var (find, factory, _, _) = CreateTwoDualKeyRig();
        factory.FailReads(OtpB);

        var scan1 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Equal(3, scan1.Count);
        var keyA1 = Assert.IsType<CompositeYubiKey>(Assert.Single(scan1, d => d is CompositeYubiKey));
        Assert.Equal("ykphysical:111", keyA1.DeviceId);
        Assert.Equal(Dual, keyA1.AvailableConnections);
        Assert.Single(scan1, d => d.AvailableConnections == ConnectionType.SmartCard); // key B's CCID fragment
        Assert.Single(scan1, d => d.AvailableConnections == ConnectionType.HidOtp); // orphaned failed read

        var otpAConnectsAfterScan1 = factory.ConnectCalls(OtpA);
        var otpBConnectsAfterScan1 = factory.ConnectCalls(OtpB);
        factory.SucceedReads(OtpB, serial: 222);

        var scan2 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Equal(2, scan2.Count);
        Assert.All(scan2, d => Assert.Equal(Dual, d.AvailableConnections));
        Assert.Contains(scan2, d => d.DeviceId == "ykphysical:111");
        Assert.Contains(scan2, d => d.DeviceId == "ykphysical:222");

        // Cache behavior: the healed interface was re-read; a previously cached interface was not.
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
        var keyA = Assert.IsType<CompositeYubiKey>(Assert.Single(scan3, d => d is CompositeYubiKey));
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
    public async Task FindAllAsync_PcscReaderRenameWithFailingReread_OrphansConservativelyWithoutStaleServe_Pin()
    {
        // Phase 0 finding 4, failure arm (PIN): when the re-read under the renamed reader FAILS, the
        // renamed CCID must be orphaned conservatively — the scan-1 serial cached under the OLD reader
        // name must NOT be served for the new name (that would be stale-identity misattribution: the
        // rename is indistinguishable from new hardware). Investigated per plan directive: the cache
        // behaves correctly under rename (miss + re-read + conservative split) — pinned, not a defect.
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        var (find, factory, pcsc, _) = CreateTwoDualKeyRig();

        var scan1 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
        Assert.Equal(2, scan1.Count);

        pcsc.Devices = [new FakePcscDevice(ReaderARenamed), new FakePcscDevice(ReaderB)];
        factory.FailReads(ReaderARenamed);

        var scan2 = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        // Had the stale scan-1 serial been served for the renamed reader, key A would still assemble as
        // ykphysical:111 with two members. Instead: key B intact, key A conservatively split.
        Assert.Equal(3, scan2.Count);
        var keyB = Assert.IsType<CompositeYubiKey>(Assert.Single(scan2, d => d is CompositeYubiKey));
        Assert.Equal("ykphysical:222", keyB.DeviceId);
        Assert.Equal(Dual, keyB.AvailableConnections);
        Assert.Single(scan2, d => d.AvailableConnections == ConnectionType.SmartCard); // renamed, unread CCID
        Assert.Single(scan2, d => d.AvailableConnections == ConnectionType.HidOtp); // key A's cached OTP fragment
        Assert.True(
            factory.ConnectCalls(ReaderARenamed) > 0,
            "The renamed reader must be re-read (cache keyed by DeviceId cannot carry identity across renames).");
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task FindAllAsync_SixIdentityReadsAgainstFourWorkerAdmission_TwoSkipWithoutConnecting()
    {
        // Phase 0 findings 1 & 5, deterministic reproduction (item 3 of the Phase-1 PRD): on a two-key
        // same-PID 0x0407 rig, one scan issues SIX identity reads against the FOUR-worker discovery
        // admission (DiscoveryWorkerAdmission). While four reads occupy the workers, the remaining two
        // fail the nonblocking TryAcquire and throw DiscoveryReadSkippedException
        // (ProtocolDeviceInfo.StartSharedRead), which DiscoveryIdentityReader logs as "aborted: interface
        // gained a live connection" and degrades to null — orphaning those interfaces. The two scan-1
        // aborts in the Phase-0 shared-mode diagnostics (6 interfaces, 4 workers) match this exactly:
        // the "aborted" failures are discovery SELF-contention on the worker-admission gate — the log
        // message's "gained a live connection" wording misattributes the cause — not sessions and not
        // PC/SC sharing violations. Deterministic here because the four admitted connects block until
        // released, so the fifth and sixth reads always find the gate saturated, whatever the order.
        // PIN for Phase 2 (scheduling tune: exempt/serialize identity reads): the assertion pins the
        // CURRENT contract — exactly four interfaces reach a connect, and the scan degrades to a
        // conservative six-way split rather than stalling or crashing.
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);

        var (find, factory) = CreateTwoTripleKeyRig();
        foreach (var name in new[] { TripleReaderA, TripleReaderB, TripleFidoA, TripleOtpA, TripleFidoB, TripleOtpB })
            factory.BlockReads(name);

        IReadOnlyList<IYubiKey> result;
        try
        {
            result = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
        }
        finally
        {
            factory.FailAllBlockedReads();
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(6, result.Count);
        Assert.DoesNotContain(result, d => d is CompositeYubiKey);
        Assert.All(result, d => Assert.NotEqual(Triple, d.AvailableConnections));
        Assert.Equal(4, factory.TotalConnectCalls);
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

        return (new FindYubiKeys(pcsc, hid, factory), factory, pcsc, hid);
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

        return (new FindYubiKeys(pcsc, hid, factory), factory);
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

    private sealed class FakePcscDevice(string readerName) : IPcscDevice
    {
        public string ReaderName { get; } = readerName;
        public AnswerToReset? Atr => null;
        public PscsConnectionKind Kind => PscsConnectionKind.Usb;
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
        Block
    }

    /// <summary>
    ///     Creates <see cref="ScriptedYubiKey" /> per-interface devices whose discovery connects follow a
    ///     per-interface script keyed by the underlying device name. DeviceIds carry a per-factory prefix so
    ///     the process-wide registries (DeviceConnectionRegistry, ProtocolDeviceInfo single-flight) never
    ///     collide across tests, while staying stable across scans within one test.
    /// </summary>
    private sealed class ScriptedIdentityFactory : IYubiKeyFactory
    {
        private readonly string _prefix = $"test-fi:{Guid.NewGuid():N}";
        private readonly ConcurrentDictionary<string, (ReadOutcome Outcome, int Serial)> _scripts = new();
        private readonly ConcurrentDictionary<string, int> _connectCalls = new();
        private readonly ConcurrentBag<TaskCompletionSource<IConnection>> _blocked = [];

        public int TotalConnectCalls => _connectCalls.Values.Sum();

        public int ConnectCalls(string name) => _connectCalls.GetValueOrDefault(name);

        public void SucceedReads(string name, int serial) => _scripts[name] = (ReadOutcome.Succeed, serial);

        public void FailReads(string name) => _scripts[name] = (ReadOutcome.Fail, 0);

        public void BlockReads(string name) => _scripts[name] = (ReadOutcome.Block, 0);

        public void FailAllBlockedReads()
        {
            foreach (var blocked in _blocked)
                blocked.TrySetException(new InvalidOperationException("Expected blocked-read cleanup failure."));
        }

        public IYubiKey Create(IDevice device) => device switch
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
                default:
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
        ScriptedIdentityFactory owner) : IYubiKey, IDiscoveryConnectionProvider
    {
        public string DeviceId { get; } = deviceId;

        public ConnectionType AvailableConnections { get; } = connection;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            Task.FromException<TConnection>(
                new InvalidOperationException("Public connect must not be used by discovery."));

        Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
            ConnectionType connectionType,
            CancellationToken cancellationToken) =>
            owner.ConnectFor(name);
    }
}