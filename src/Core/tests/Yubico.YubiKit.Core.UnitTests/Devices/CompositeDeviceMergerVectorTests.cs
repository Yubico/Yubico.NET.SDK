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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
///     Unit-vector harness for <see cref="CompositeDeviceMerger.Merge" />. The merge tiers these vectors
///     exercise are specified in docs/architecture/device-discovery-guarantees.md.
/// </summary>
/// <remarks>
///     <para>
///         Vectors are classified per the plan's evidence rule (Owner decisions, item 3):
///     </para>
///     <list type="bullet">
///         <item>
///             <b>REGRESSION vectors</b> (names containing <c>Regression</c>) were written RED against the
///             pre-Phase-2 merger and went GREEN when the generalized guard and pigeonhole deduction
///             landed. They are now regression pins: a failure here is a real defect, not the expected
///             state. Each records the behavior it used to exhibit under "WAS", for history.
///         </item>
///         <item>
///             <b>PIN vectors</b> (names containing <c>Pin</c>) assert current expected-conservative or
///             documented-bound behavior. They MAY pass today; they pin contracts and are never fix evidence.
///         </item>
///     </list>
/// </remarks>
public class CompositeDeviceMergerVectorTests
{
    private const ConnectionType Triple =
        ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp;

    // ---------------------------------------------------------------------------------------------
    // 1a. PID-class coverage: full-visibility single key merges by PID with no serial (PINs).
    // ---------------------------------------------------------------------------------------------

    [Theory]
    [InlineData((ushort)0x0407)]
    [InlineData((ushort)0x0116)]
    public void Merge_SingleTripleKeyFullVisibilityNoSerials_MergesByPid_Pin(ushort pid)
    {
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("ccid-a", ConnectionType.SmartCard, pid),
            Descriptor("fido-a", ConnectionType.HidFido, pid),
            Descriptor("otp-a", ConnectionType.HidOtp, pid)
        ]);

        var device = Assert.Single(result);
        var composite = Assert.IsType<CompositeYubiKey>(device);
        Assert.Equal($"ykphysical:pid:{pid:X4}", composite.DeviceId);
        Assert.Equal(Triple, composite.AvailableConnections);
    }

    [Theory]
    [InlineData((ushort)0x0403, ConnectionType.HidOtp, ConnectionType.HidFido)]
    [InlineData((ushort)0x0405, ConnectionType.HidOtp, ConnectionType.SmartCard)]
    [InlineData((ushort)0x0406, ConnectionType.HidFido, ConnectionType.SmartCard)]
    public void Merge_SingleDualInterfaceKeyFullVisibilityNoSerials_MergesByPid_Pin(
        ushort pid,
        ConnectionType first,
        ConnectionType second)
    {
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("iface-1", first, pid),
            Descriptor("iface-2", second, pid)
        ]);

        var device = Assert.Single(result);
        var composite = Assert.IsType<CompositeYubiKey>(device);
        Assert.Equal($"ykphysical:pid:{pid:X4}", composite.DeviceId);
        Assert.Equal(first | second, composite.AvailableConnections);
    }

    [Theory]
    [InlineData((ushort)0x0401, ConnectionType.HidOtp)]
    [InlineData((ushort)0x0402, ConnectionType.HidFido)]
    [InlineData((ushort)0x0404, ConnectionType.SmartCard)]
    [InlineData((ushort)0x0120, ConnectionType.HidFido)] // SKY
    public void Merge_SingleInterfacePid_StandsAloneWithoutCompositeWrapper_Pin(
        ushort pid,
        ConnectionType connection)
    {
        var result = CompositeDeviceMerger.Merge([Descriptor("only", connection, pid)]);

        var device = Assert.Single(result);
        Assert.IsNotType<CompositeYubiKey>(device);
        Assert.Equal("only", device.DeviceId);
        Assert.Equal(connection, device.AvailableConnections);
    }

    // ---------------------------------------------------------------------------------------------
    // 1a. Two same-PID keys with all serials present group correctly (PINs).
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Merge_TwoSamePidTripleKeysAllSerialsKnown_GroupsBySerial_Pin()
    {
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("ccid-a", ConnectionType.SmartCard, 0x0407, serial: 111),
            Descriptor("fido-a", ConnectionType.HidFido, 0x0407, serial: 111),
            Descriptor("otp-a", ConnectionType.HidOtp, 0x0407, serial: 111),
            Descriptor("ccid-b", ConnectionType.SmartCard, 0x0407, serial: 222),
            Descriptor("fido-b", ConnectionType.HidFido, 0x0407, serial: 222),
            Descriptor("otp-b", ConnectionType.HidOtp, 0x0407, serial: 222)
        ]);

        Assert.Equal(2, result.Count);
        var keyA = Assert.IsType<CompositeYubiKey>(Assert.Single(result, d => d.DeviceId == "ykphysical:111"));
        var keyB = Assert.IsType<CompositeYubiKey>(Assert.Single(result, d => d.DeviceId == "ykphysical:222"));
        Assert.Equal(Triple, keyA.AvailableConnections);
        Assert.Equal(Triple, keyB.AvailableConnections);
        Assert.Equal(["ccid-a", "fido-a", "otp-a"], keyA.MemberDeviceIds);
        Assert.Equal(["ccid-b", "fido-b", "otp-b"], keyB.MemberDeviceIds);
    }

    [Fact]
    public void Merge_TwoSamePidDualKeysAllSerialsKnown_GroupsBySerial_Pin()
    {
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("otp-a", ConnectionType.HidOtp, 0x0403, serial: 111),
            Descriptor("fido-a", ConnectionType.HidFido, 0x0403, serial: 111),
            Descriptor("otp-b", ConnectionType.HidOtp, 0x0403, serial: 222),
            Descriptor("fido-b", ConnectionType.HidFido, 0x0403, serial: 222)
        ]);

        Assert.Equal(2, result.Count);
        Assert.All(result, d => Assert.IsType<CompositeYubiKey>(d));
        Assert.Contains(result, d => d.DeviceId == "ykphysical:111");
        Assert.Contains(result, d => d.DeviceId == "ykphysical:222");
        Assert.All(result, d => Assert.Equal(ConnectionType.HidOtp | ConnectionType.HidFido, d.AvailableConnections));
    }

    // ---------------------------------------------------------------------------------------------
    // 1b. REGRESSION vectors — RED before Phase 2, GREEN since. A failure here is a real regression.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Merge_Regression_CrossKeyShapeB_TwoTripleKeysDisjointHidNoCcidNoSerials_MustStayStandalone()
    {
        // Verified premise 4(b) of PLAN.md: two 0x0407 keys, key A's FIDO + key B's OTP enumerated, no
        // CCID, no serials. observed (FIDO|OTP) != expected (triple), but hasSmartCard == false bypasses
        // the bespoke triple guard in CanMergeByPidWithoutSerial — the merger fuses the two keys.
        // REQUIRED (Phase 2 generalized guard, now implemented): PID-unique alone is insufficient when
        // observed != expected; the descriptors fall back to the serial path and, with null serials, stay
        // standalone.
        // WAS: a composite spanning both keys existed. A failure here means that has returned.
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("fido-keyA", ConnectionType.HidFido, 0x0407),
            Descriptor("otp-keyB", ConnectionType.HidOtp, 0x0407)
        ]);

        var composites = result.OfType<CompositeYubiKey>().ToList();
        Assert.True(
            composites.Count == 0,
            "Cross-key transient shape B (premise 4b): the merger fused key A's FIDO and key B's OTP " +
            $"(both 0x0407, no CCID, no serials) into {composites.Count} composite(s): {Describe(result)}. " +
            "observed != expected must route to the serial path; null serials must stay standalone.");
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Merge_Regression_TwoTripleKeysFiveOfSixSerialsKnown_OrphanIsAttributedByPigeonhole()
    {
        // Pigeonhole deduction (Solution design tier 4): both 0x0407 keys fully enumerated; serials known
        // for 5 of 6 interfaces. The null-serial OTP orphan's connection type exactly fills the single
        // missing slot of exactly ONE incomplete same-PID composite (key B), and type-count closure holds
        // (2 OTP interfaces visible, 2 candidate keys).
        // REQUIRED (Phase 2, now implemented): the orphan is attributed to key B; two complete physical keys.
        // WAS: the merger left the orphan standalone (3 devices, not 2). A failure here means that has returned.
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("ccid-a", ConnectionType.SmartCard, 0x0407, serial: 111),
            Descriptor("fido-a", ConnectionType.HidFido, 0x0407, serial: 111),
            Descriptor("otp-a", ConnectionType.HidOtp, 0x0407, serial: 111),
            Descriptor("ccid-b", ConnectionType.SmartCard, 0x0407, serial: 222),
            Descriptor("fido-b", ConnectionType.HidFido, 0x0407, serial: 222),
            Descriptor("otp-b", ConnectionType.HidOtp, 0x0407) // identity read failed: null serial
        ]);

        Assert.True(
            result.Count == 2,
            "Pigeonhole deduction (0x0407 pair, 5/6 serials): the null-serial OTP orphan uniquely fills " +
            $"key B's only missing slot but was left standalone; got {result.Count} devices: {Describe(result)}.");
        Assert.All(result, d => Assert.Equal(Triple, d.AvailableConnections));
    }

    [Fact]
    public void Merge_Regression_TwoDualKeysThreeOfFourSerialsKnown_OrphanIsAttributedByPigeonhole()
    {
        // Pigeonhole deduction, 0x0403 pair: both keys fully enumerated (4 interfaces), serials known for
        // 3 of 4. Key A is complete; key B is anchored by its serial-bearing OTP. The null-serial FIDO
        // orphan's type exactly fills key B's only missing slot; type-count closure holds (2 FIDO visible,
        // 2 candidate keys).
        // REQUIRED (Phase 2, now implemented): orphan attributed to key B; two complete OTP+FIDO keys.
        // WAS: the merger left the orphan standalone (3 devices, not 2). A failure here means that has returned.
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("otp-a", ConnectionType.HidOtp, 0x0403, serial: 111),
            Descriptor("fido-a", ConnectionType.HidFido, 0x0403, serial: 111),
            Descriptor("otp-b", ConnectionType.HidOtp, 0x0403, serial: 222),
            Descriptor("fido-b", ConnectionType.HidFido, 0x0403) // identity read failed: null serial
        ]);

        Assert.True(
            result.Count == 2,
            "Pigeonhole deduction (0x0403 pair, 3/4 serials): the null-serial FIDO orphan uniquely fills " +
            $"key B's only missing slot but was left standalone; got {result.Count} devices: {Describe(result)}.");
        Assert.All(
            result,
            d => Assert.Equal(ConnectionType.HidOtp | ConnectionType.HidFido, d.AvailableConnections));
    }

    [Fact]
    public void Merge_TwoTripleKeysBothMissingSameInterfaceTypeSerial_StaysConservativelySplit_Pin()
    {
        // Deduction ambiguity (PIN): both 0x0407 keys anchored (CCID+FIDO by serial) but BOTH OTP
        // identity reads failed — two orphans of one type, two candidate composites. Any attribution
        // would be a guess; the plan's tier-4 deduction requires exactly ONE candidate. Conservative
        // split is the CURRENT behavior and must REMAIN the behavior after Phase 2.
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("ccid-a", ConnectionType.SmartCard, 0x0407, serial: 111),
            Descriptor("fido-a", ConnectionType.HidFido, 0x0407, serial: 111),
            Descriptor("ccid-b", ConnectionType.SmartCard, 0x0407, serial: 222),
            Descriptor("fido-b", ConnectionType.HidFido, 0x0407, serial: 222),
            Descriptor("otp-a", ConnectionType.HidOtp, 0x0407), // null serial — ambiguous orphan
            Descriptor("otp-b", ConnectionType.HidOtp, 0x0407) // null serial — ambiguous orphan
        ]);

        Assert.Equal(4, result.Count);
        var composites = result.OfType<CompositeYubiKey>().ToList();
        Assert.Equal(2, composites.Count);
        Assert.All(
            composites,
            c => Assert.Equal(ConnectionType.SmartCard | ConnectionType.HidFido, c.AvailableConnections));
        var orphans = result.Where(d => d is not CompositeYubiKey).ToList();
        Assert.Equal(2, orphans.Count);
        Assert.All(orphans, o => Assert.Equal(ConnectionType.HidOtp, o.AvailableConnections));
    }

    // ---------------------------------------------------------------------------------------------
    // 1c. Epistemic-bound PINS (PLAN.md "Epistemic bound", Solution design).
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Merge_EpistemicBound_ComplementaryPartials_TwoDualKeysOneInterfaceEach_MergeIsRepresentable_Pin()
    {
        // Epistemic bound (PIN — see PLAN.md, Solution design, "Epistemic bound"): two 0x0403 keys, each
        // with only ONE complementary interface enumerated (key A's OTP, key B's FIDO), no serials. The
        // two descriptors are byte-indistinguishable from ONE fully-visible 0x0403 key: PID count is 1
        // and observed == expected, so the merger fuses them — and, because pidCount == 1, no serial
        // reads even fire to contradict it. No merge logic can resolve this shape; only serial or
        // topology evidence can, and neither exists in this window. This pin asserts the CURRENT (and
        // plan-sanctioned) behavior: a cross-key composite is representable while complementary partial
        // visibility persists, bounded to that window, and heals conditionally — on the first subsequent
        // scan with complete same-PID visibility, serial evidence, or topology evidence.
        // Disposition note: the Phase-1 PRD originally listed this shape as a defect vector; the audited
        // PLAN's epistemic bound governs, and the orchestrator reclassified it to this pin (recorded in
        // the Phase-1 evidence ledger). Not fix evidence.
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("otp-keyA", ConnectionType.HidOtp, 0x0403),
            Descriptor("fido-keyB", ConnectionType.HidFido, 0x0403)
        ]);

        var device = Assert.Single(result);
        var composite = Assert.IsType<CompositeYubiKey>(device);
        Assert.Equal("ykphysical:pid:0403", composite.DeviceId);
        Assert.Equal(ConnectionType.HidOtp | ConnectionType.HidFido, composite.AvailableConnections);
        Assert.Equal(["fido-keyB", "otp-keyA"], composite.MemberDeviceIds);
    }

    [Fact]
    public void Merge_ComplementaryPartialMasquerade_MisattributionIsRepresentableAndBounded_Pin()
    {
        // Epistemic bound (PIN — see PLAN.md, Solution design, "Epistemic bound"): key A is anchored as
        // {OTP, FIDO} (serial 111) while key B is visible ONLY as an unread CCID (null serial). The three
        // descriptors are byte-indistinguishable from ONE fully-visible 0x0407 key: PID count is 1 and
        // observed == expected, so the merger fuses them — attributing key B's CCID to key A. NO merge
        // logic can resolve this shape; only serial or topology evidence can, and neither exists for the
        // unread CCID. This pin asserts the CURRENT documented-bound behavior so the contract is pinned:
        // the misattribution is representable, bounded to partial-visibility windows, and heals
        // conditionally (first subsequent scan with complete same-PID visibility, serial, or topology
        // evidence). This vector is NOT a defect vector and never counts as fix evidence.
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("otp-keyA", ConnectionType.HidOtp, 0x0407, serial: 111),
            Descriptor("fido-keyA", ConnectionType.HidFido, 0x0407, serial: 111),
            Descriptor("ccid-keyB", ConnectionType.SmartCard, 0x0407) // physically key B; unreadable
        ]);

        var device = Assert.Single(result);
        var composite = Assert.IsType<CompositeYubiKey>(device);
        Assert.Equal("ykphysical:pid:0407", composite.DeviceId);
        Assert.Equal(Triple, composite.AvailableConnections);
        Assert.Equal(["ccid-keyB", "fido-keyA", "otp-keyA"], composite.MemberDeviceIds);
    }

    // ---------------------------------------------------------------------------------------------
    // 1d. Serial-less same-PID pair (PINs) + Phase-3 topology TODO.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Merge_TwoSamePidTripleKeysNoSerialsFullVisibility_ConservativeSplit_Pin()
    {
        // Serial-less pair (PIN): two 0x0407 keys fully visible, no serial evidence. PID count is 2, so
        // the merger routes to the serial path; null serials never collapse. Conservative split (6
        // standalone) is the CURRENT and the target macOS/Linux behavior (platform bound, guarantee
        // matrix row "Serial-less multi-interface pair").
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("ccid-a", ConnectionType.SmartCard, 0x0407),
            Descriptor("fido-a", ConnectionType.HidFido, 0x0407),
            Descriptor("otp-a", ConnectionType.HidOtp, 0x0407),
            Descriptor("ccid-b", ConnectionType.SmartCard, 0x0407),
            Descriptor("fido-b", ConnectionType.HidFido, 0x0407),
            Descriptor("otp-b", ConnectionType.HidOtp, 0x0407)
        ]);

        Assert.Equal(6, result.Count);
        Assert.DoesNotContain(result, d => d is CompositeYubiKey);

        // TODO(Phase 3 — A′ Windows topology): when the merger accepts optional topology evidence
        // (Windows Container ID per interface), add the RED→GREEN vector:
        //   Merge_TwoSamePidTripleKeysNoSerials_WithTopologyEvidence_GroupsByContainerId
        //   Input: the same six descriptors, each carrying a topology key — container-1 for
        //          {ccid-a, fido-a, otp-a}, container-2 for {ccid-b, fido-b, otp-b}.
        //   Expected: exactly two composites grouped by container id (topology is evidence tier 1,
        //             above serial and PID). RED against the Phase-2 (B-only) merger — it has no
        //             topology input and conservatively splits — GREEN once the topology tier lands.
        //   Per repo rule, no skipped placeholder test is added here; this pin asserts the current
        //   conservative behavior, which remains the correct topology-ABSENT behavior on all platforms.
    }

    [Fact]
    public void Merge_TwoSamePidDualKeysNoSerialsFullVisibility_ConservativeSplit_Pin()
    {
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("otp-a", ConnectionType.HidOtp, 0x0403),
            Descriptor("fido-a", ConnectionType.HidFido, 0x0403),
            Descriptor("otp-b", ConnectionType.HidOtp, 0x0403),
            Descriptor("fido-b", ConnectionType.HidFido, 0x0403)
        ]);

        Assert.Equal(4, result.Count);
        Assert.DoesNotContain(result, d => d is CompositeYubiKey);
    }

    // ---------------------------------------------------------------------------------------------
    // 1e. Reconfiguration transitions (PINs).
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Merge_ReconfiguredKeyReenumeratedUnderNewPid_GroupsByCurrentPidTruth_Pin()
    {
        // Transport reconfiguration (PLAN.md Scenario analyses): a 0x0407 key had CCID disabled via
        // Management and re-enumerated as 0x0403. The merger sees only the new descriptor set (old
        // interfaces absent — verified premise 2: reconfiguration changes reader names and HID paths,
        // stale identities self-evict). Grouping follows the CURRENT PID truth: one 0x0403 composite.
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("otp-a-new", ConnectionType.HidOtp, 0x0403),
            Descriptor("fido-a-new", ConnectionType.HidFido, 0x0403)
        ]);

        var device = Assert.Single(result);
        var composite = Assert.IsType<CompositeYubiKey>(device);
        Assert.Equal("ykphysical:pid:0403", composite.DeviceId);
        Assert.Equal(ConnectionType.HidOtp | ConnectionType.HidFido, composite.AvailableConnections);
    }

    [Fact]
    public void Merge_OneOfTwoKeysReconfigured_DifferentPidsNoSerials_TriviallyDistinguishable_Pin()
    {
        // One key of a formerly same-PID pair reconfigured (0x0407 → 0x0403): PID counts drop to 1 per
        // PID, so both keys group by PID alone with no serial reads — the rig becomes trivially
        // distinguishable (PLAN.md Scenario analyses, transport reconfiguration).
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("ccid-a", ConnectionType.SmartCard, 0x0407),
            Descriptor("fido-a", ConnectionType.HidFido, 0x0407),
            Descriptor("otp-a", ConnectionType.HidOtp, 0x0407),
            Descriptor("otp-b-new", ConnectionType.HidOtp, 0x0403),
            Descriptor("fido-b-new", ConnectionType.HidFido, 0x0403)
        ]);

        Assert.Equal(2, result.Count);
        var keyA = Assert.IsType<CompositeYubiKey>(Assert.Single(result, d => d.DeviceId == "ykphysical:pid:0407"));
        var keyB = Assert.IsType<CompositeYubiKey>(Assert.Single(result, d => d.DeviceId == "ykphysical:pid:0403"));
        Assert.Equal(Triple, keyA.AvailableConnections);
        Assert.Equal(ConnectionType.HidOtp | ConnectionType.HidFido, keyB.AvailableConnections);
    }

    // ---------------------------------------------------------------------------------------------
    // Phase 3 — Tier 1 topology evidence (PLAN.md "Solution design", evidence hierarchy tier 1).
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Merge_SeriallessPairWithDistinctTopologyKeys_GroupsIntoTwoCompleteKeys()
    {
        // Phase-3 headline RED→GREEN (PLAN.md guarantee matrix, "Serial-less multi-interface pair"):
        // two same-PID 0x0407 keys, NO serials, full visibility, each interface carrying its physical
        // device's topology key (Windows Container ID). Tier 1 groups them into two complete keys with no
        // serial read at all — the only complete answer for serial-less hardware.
        // WAS (tier-2..5 only, i.e. no topology input): conservative six-way split. Tier 1 now resolves it;
        // a six-way split here means topology attribution has regressed.
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("ccid-a", ConnectionType.SmartCard, 0x0407, topologyKey: "container-A"),
            Descriptor("fido-a", ConnectionType.HidFido, 0x0407, topologyKey: "container-A"),
            Descriptor("otp-a", ConnectionType.HidOtp, 0x0407, topologyKey: "container-A"),
            Descriptor("ccid-b", ConnectionType.SmartCard, 0x0407, topologyKey: "container-B"),
            Descriptor("fido-b", ConnectionType.HidFido, 0x0407, topologyKey: "container-B"),
            Descriptor("otp-b", ConnectionType.HidOtp, 0x0407, topologyKey: "container-B")
        ]);

        Assert.True(
            result.Count == 2,
            $"Topology tier must group the serial-less pair into two complete keys; got {result.Count} " +
            $"device(s): {Describe(result)}.");
        var keyA = Assert.IsType<CompositeYubiKey>(
            Assert.Single(result, d => d.DeviceId == "ykphysical:topology:container-A"));
        var keyB = Assert.IsType<CompositeYubiKey>(
            Assert.Single(result, d => d.DeviceId == "ykphysical:topology:container-B"));
        Assert.Equal(Triple, keyA.AvailableConnections);
        Assert.Equal(Triple, keyB.AvailableConnections);
        Assert.Equal(["ccid-a", "fido-a", "otp-a"], keyA.MemberDeviceIds);
        Assert.Equal(["ccid-b", "fido-b", "otp-b"], keyB.MemberDeviceIds);
    }

    [Fact]
    public void Merge_ComplementaryPartialsWithTopologyKeys_SplitByTopology_NotMergedByPid()
    {
        // Topology CLOSES the epistemic bound (PLAN.md: "on Windows this holds under partial visibility
        // too whenever topology evidence is readable"). This is byte-identical to the shape-A
        // epistemic-bound pin — two 0x0403 keys, one complementary interface each, no serials, observed
        // == expected — which tiers 2..5 must merge. With topology evidence the two interfaces are known
        // to belong to different physical devices and MUST stay split.
        // WAS (tier-2..5 only): one cross-key composite ykphysical:pid:0403. Tier 1 now separates them;
        // a cross-key composite here means topology attribution has regressed.
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("otp-keyA", ConnectionType.HidOtp, 0x0403, topologyKey: "container-A"),
            Descriptor("fido-keyB", ConnectionType.HidFido, 0x0403, topologyKey: "container-B")
        ]);

        Assert.True(
            result.Count == 2,
            "Topology evidence must split complementary partials from two physical keys; got " +
            $"{result.Count} device(s): {Describe(result)}.");
        Assert.DoesNotContain(result, d => d is CompositeYubiKey);
        Assert.Equal(["fido-keyB", "otp-keyA"], result.Select(d => d.DeviceId).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Merge_TopologyAbsentForAllInterfaces_IsByteIdenticalToPreTopologyBehavior_Pin()
    {
        // Degradation pin: with no topology evidence anywhere (macOS/Linux always; Windows on topology
        // read failure), results must be byte-identical to the tier-2..5 behavior. Same inputs as the
        // serial-less pair pin and the shape-A epistemic-bound pin, asserted against the same
        // expectations. This is the pinned "degrades to exactly the macOS/Linux semantics" contract.
        var seriallessPair = CompositeDeviceMerger.Merge(
        [
            Descriptor("ccid-a", ConnectionType.SmartCard, 0x0407),
            Descriptor("fido-a", ConnectionType.HidFido, 0x0407),
            Descriptor("otp-a", ConnectionType.HidOtp, 0x0407),
            Descriptor("ccid-b", ConnectionType.SmartCard, 0x0407),
            Descriptor("fido-b", ConnectionType.HidFido, 0x0407),
            Descriptor("otp-b", ConnectionType.HidOtp, 0x0407)
        ]);

        Assert.Equal(6, seriallessPair.Count);
        Assert.DoesNotContain(seriallessPair, d => d is CompositeYubiKey);

        var complementaryPartials = CompositeDeviceMerger.Merge(
        [
            Descriptor("otp-keyA", ConnectionType.HidOtp, 0x0403),
            Descriptor("fido-keyB", ConnectionType.HidFido, 0x0403)
        ]);

        var bounded = Assert.IsType<CompositeYubiKey>(Assert.Single(complementaryPartials));
        Assert.Equal("ykphysical:pid:0403", bounded.DeviceId);
    }

    [Fact]
    public void Merge_PartialTopology_KeyedInterfacesGroup_UnkeyedFallThroughUnguessed_Pin()
    {
        // Mixed/partial topology (the Windows "CCID resolves, HID doesn't" case): interfaces WITH keys
        // group by topology; interfaces WITHOUT keys must never be guessed into a topology group — they
        // fall through to the unchanged tiers. Here key A's CCID+FIDO carry a container id while its OTP
        // does not; the OTP is the only 0x0403-PID... it is a 0x0407 interface whose PID group is now
        // topology-free and observed(OTP) != expected(triple), so it stands alone conservatively.
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("ccid-a", ConnectionType.SmartCard, 0x0407, topologyKey: "container-A"),
            Descriptor("fido-a", ConnectionType.HidFido, 0x0407, topologyKey: "container-A"),
            Descriptor("otp-unresolved", ConnectionType.HidOtp, 0x0407)
        ]);

        Assert.Equal(2, result.Count);
        var keyA = Assert.IsType<CompositeYubiKey>(
            Assert.Single(result, d => d.DeviceId == "ykphysical:topology:container-A"));
        Assert.Equal(ConnectionType.SmartCard | ConnectionType.HidFido, keyA.AvailableConnections);
        Assert.DoesNotContain("otp-unresolved", keyA.MemberDeviceIds);
        var unkeyed = Assert.Single(result, d => d is not CompositeYubiKey);
        Assert.Equal("otp-unresolved", unkeyed.DeviceId);
    }

    [Fact]
    public void Merge_MixedTopologyAndSerialEvidence_IsDeterministicAndConserving_Pin()
    {
        // Mixed evidence: key A resolved by topology (tier 1), keys B and C are a same-PID 0x0403 pair
        // that only serial evidence can separate (tier 2 — PID count is 2, so no PID merge), plus an NFC
        // interface standing alone. Conservation: every input interface appears exactly once across the
        // returned devices, and no interface is attributed twice.
        var descriptors = new[]
        {
            Descriptor("ccid-a", ConnectionType.SmartCard, 0x0407, topologyKey: "container-A"),
            Descriptor("fido-a", ConnectionType.HidFido, 0x0407, topologyKey: "container-A"),
            Descriptor("otp-a", ConnectionType.HidOtp, 0x0407, topologyKey: "container-A"),
            Descriptor("otp-b", ConnectionType.HidOtp, 0x0403, serial: 222),
            Descriptor("fido-b", ConnectionType.HidFido, 0x0403, serial: 222),
            Descriptor("otp-c", ConnectionType.HidOtp, 0x0403, serial: 333),
            Descriptor("fido-c", ConnectionType.HidFido, 0x0403, serial: 333),
            Descriptor("nfc-reader", ConnectionType.SmartCard, pid: null, isUsb: false)
        };

        var result = CompositeDeviceMerger.Merge(descriptors);

        Assert.Equal(4, result.Count);
        var keyA = Assert.IsType<CompositeYubiKey>(
            Assert.Single(result, d => d.DeviceId == "ykphysical:topology:container-A"));
        Assert.Equal(Triple, keyA.AvailableConnections);
        var keyB = Assert.IsType<CompositeYubiKey>(Assert.Single(result, d => d.DeviceId == "ykphysical:222"));
        Assert.Equal(ConnectionType.HidOtp | ConnectionType.HidFido, keyB.AvailableConnections);
        var keyC = Assert.IsType<CompositeYubiKey>(Assert.Single(result, d => d.DeviceId == "ykphysical:333"));
        Assert.Equal(ConnectionType.HidOtp | ConnectionType.HidFido, keyC.AvailableConnections);
        Assert.Single(result, d => d.DeviceId == "nfc-reader");

        // Conservation: each input interface id appears exactly once across all returned devices.
        var attributed = result
            .SelectMany(d => d is CompositeYubiKey composite ? composite.MemberDeviceIds : [d.DeviceId])
            .ToList();
        Assert.Equal(descriptors.Length, attributed.Count);
        Assert.Equal(
            descriptors.Select(d => d.Device.DeviceId).Order(StringComparer.Ordinal),
            attributed.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Merge_TwoSameTypeOrphansExceedAnchoredKeys_StayStandaloneInsteadOfDoubleAttribution_Pin()
    {
        // Type-count closure (tier 4 precondition, NoInterfaceTypeOutnumbersCandidateKeys). One anchored
        // 0x0407 key (CCID, serial 111) and TWO null-serial OTP orphans: key A shows CCID+OTP, key B shows
        // only OTP, and neither OTP interface yielded a serial. Both orphans "uniquely fill" the single
        // anchored key's missing OTP slot, so unique-candidate deduction alone would attribute BOTH to key
        // 111 — fusing two physical keys' OTP interfaces into one device. Closure refuses: 2 OTP interfaces
        // are visible but only 1 anchored key can own one, so no orphan is attributable and both stay
        // standalone (three devices, conservative).
        //
        // Why this needs member-level assertions: the bad composite would report AvailableConnections as
        // SmartCard|HidOtp — the duplicate OTP interface is INVISIBLE in the flags. Only member inspection
        // distinguishes the correct result from the wrong one.
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("ccid-a", ConnectionType.SmartCard, 0x0407, serial: 111),
            Descriptor("otp-a", ConnectionType.HidOtp, 0x0407), // identity read failed: null serial
            Descriptor("otp-b", ConnectionType.HidOtp, 0x0407) // different physical key, also null serial
        ]);

        var duplicated = result
            .OfType<CompositeYubiKey>()
            .Where(c => c.Members.GroupBy(m => m.AvailableConnections).Any(g => g.Count() > 1))
            .ToList();
        Assert.True(
            duplicated.Count == 0,
            "Type-count closure: two null-serial OTP orphans were both attributed to the single anchored " +
            $"key, producing a composite holding two OTP interfaces from different physical keys: {Describe(result)}.");
        Assert.Equal(3, result.Count);
        Assert.All(result, d => Assert.IsNotType<CompositeYubiKey>(d));
    }

    // ---------------------------------------------------------------------------------------------
    // 5. Cross-PID contested serials — the winner rule.
    //
    // A serial observed under more than one PID in a single scan is one physical key caught
    // mid-reconfiguration (serial is globally unique per key; PID changes with configuration). The
    // groups must NOT be merged into one composite: the members would then contain two interfaces of
    // the same connection type, AvailableConnections' flags-union would hide that, and
    // TryResolveMember's first-match would route sessions to an arbitrary (possibly dying) member.
    // Instead, at most ONE group may carry the durable ykphysical:{serial} id — the group whose
    // anchored census exactly matches its PID's expected interface set — and every other contested
    // group's interfaces publish standalone. Zero or multiple complete groups is ambiguity, and
    // ambiguity fragments conservatively rather than guessing.
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    ///     One complete group wins the durable id; the stale group's interfaces stand alone.
    /// </summary>
    /// <remarks>
    ///     The 0x0403 group (otp+fido) exactly matches its PID's expected set, so it is the live
    ///     enumeration; the 0x0407 group (ccid+otp, missing fido against the triple) is the stale one. The
    ///     physical key keeps its durable <c>ykphysical:{serial}</c> identity across the transition, and no
    ///     composite ever holds two members of one connection type.
    /// </remarks>
    [Fact]
    public void Merge_ContestedSerial_CompleteGroupWinsTheSerialId_StaleGroupStandsAlone()
    {
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("ccid-old", ConnectionType.SmartCard, 0x0407, serial: 500),
            Descriptor("otp-old", ConnectionType.HidOtp, 0x0407, serial: 500),
            Descriptor("otp-new", ConnectionType.HidOtp, 0x0403, serial: 500),
            Descriptor("fido-new", ConnectionType.HidFido, 0x0403, serial: 500)
        ]);

        var winner = Assert.IsType<CompositeYubiKey>(
            Assert.Single(result, d => d.DeviceId == "ykphysical:500"));
        Assert.Equal(new[] { "fido-new", "otp-new" }, winner.MemberDeviceIds);
        Assert.Single(result, d => d.DeviceId == "ccid-old");
        Assert.Single(result, d => d.DeviceId == "otp-old");
        Assert.Equal(3, result.Count);
    }

    /// <summary>
    ///     No complete group means no winner: every contested interface stands alone.
    /// </summary>
    /// <remarks>
    ///     Neither census matches its PID's expected set (ccid+otp is missing fido against 0x0407's
    ///     triple, and contains an unexpected ccid against 0x0403's otp+fido). Picking either group would
    ///     be a guess, so neither gets the durable id and the next scan converges.
    /// </remarks>
    [Fact]
    public void Merge_ContestedSerial_NoCompleteGroup_AllInterfacesStandAlone()
    {
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("ccid-first", ConnectionType.SmartCard, 0x0407, serial: 500),
            Descriptor("otp-first", ConnectionType.HidOtp, 0x0407, serial: 500),
            Descriptor("ccid-second", ConnectionType.SmartCard, 0x0403, serial: 500),
            Descriptor("otp-second", ConnectionType.HidOtp, 0x0403, serial: 500)
        ]);

        Assert.Equal(4, result.Count);
        Assert.All(result, d => Assert.IsNotType<CompositeYubiKey>(d));
        Assert.DoesNotContain(result, d => d.DeviceId.StartsWith("ykphysical:", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Two complete groups is ambiguity, not a tie to break: everything stands alone.
    /// </summary>
    /// <remarks>
    ///     Both censuses are complete for their PIDs, so completeness cannot say which enumeration is
    ///     live. Any tie-break (newest PID, enumeration order) would be guessing with a different name.
    /// </remarks>
    [Fact]
    public void Merge_ContestedSerial_MultipleCompleteGroups_AllInterfacesStandAlone()
    {
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("ccid-a", ConnectionType.SmartCard, 0x0407, serial: 500),
            Descriptor("otp-a", ConnectionType.HidOtp, 0x0407, serial: 500),
            Descriptor("fido-a", ConnectionType.HidFido, 0x0407, serial: 500),
            Descriptor("otp-b", ConnectionType.HidOtp, 0x0403, serial: 500),
            Descriptor("fido-b", ConnectionType.HidFido, 0x0403, serial: 500)
        ]);

        Assert.Equal(5, result.Count);
        Assert.All(result, d => Assert.IsNotType<CompositeYubiKey>(d));
    }

    /// <summary>
    ///     A contested serial is not a pigeonhole candidate: a null-serial orphan cannot rescue a losing
    ///     group into completeness, and is published standalone alongside it.
    /// </summary>
    /// <remarks>
    ///     Winner determination uses anchored (serial-bearing) members only. If attributed orphans could
    ///     complete a contested census, orphan attribution — a deduction — would decide which enumeration
    ///     gets the durable identity, inverting the evidence hierarchy during the one window where the
    ///     evidence is least trustworthy.
    /// </remarks>
    [Fact]
    public void Merge_ContestedSerial_OrphanCannotRescueALosingGroup()
    {
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("ccid-old", ConnectionType.SmartCard, 0x0407, serial: 500),
            Descriptor("otp-old", ConnectionType.HidOtp, 0x0407, serial: 500),
            Descriptor("fido-orphan", ConnectionType.HidFido, 0x0407), // identity read failed: null serial
            Descriptor("otp-new", ConnectionType.HidOtp, 0x0403, serial: 500),
            Descriptor("fido-new", ConnectionType.HidFido, 0x0403, serial: 500)
        ]);

        var winner = Assert.IsType<CompositeYubiKey>(
            Assert.Single(result, d => d.DeviceId == "ykphysical:500"));
        Assert.Equal(new[] { "fido-new", "otp-new" }, winner.MemberDeviceIds);
        Assert.Single(result, d => d.DeviceId == "ccid-old");
        Assert.Single(result, d => d.DeviceId == "otp-old");
        Assert.Single(result, d => d.DeviceId == "fido-orphan");
        Assert.Equal(4, result.Count);
    }

    /// <summary>
    ///     A contested loser in the group suppresses orphan attribution to clean keys sharing that PID:
    ///     the loser's interfaces are physically present and unaccounted-for, so an orphan could belong to
    ///     the mid-transition key rather than the clean candidate.
    /// </summary>
    /// <remarks>
    ///     Deliberate conservatism, not a lost optimization. The type-count closure counts all of the
    ///     group's descriptors while candidacy is restricted to eligible keys, so deduction goes standalone
    ///     during a contest and the next scan converges. Without the contested loser present, the same
    ///     orphan attributes normally (covered by the pigeonhole vectors above).
    /// </remarks>
    [Fact]
    public void Merge_ContestedSerial_LoserSuppressesOrphanAttributionToCleanKeys()
    {
        var result = CompositeDeviceMerger.Merge(
        [
            // Clean key 111 under 0x0407, missing its OTP; the orphan would attribute to it if alone.
            Descriptor("ccid-clean", ConnectionType.SmartCard, 0x0407, serial: 111),
            Descriptor("fido-clean", ConnectionType.HidFido, 0x0407, serial: 111),
            Descriptor("otp-orphan", ConnectionType.HidOtp, 0x0407), // identity read failed: null serial
            // Contested serial 500: loses here (incomplete), wins nowhere (0x0403 side incomplete too).
            Descriptor("otp-contested", ConnectionType.HidOtp, 0x0407, serial: 500),
            Descriptor("ccid-contested", ConnectionType.SmartCard, 0x0403, serial: 500)
        ]);

        // The orphan stays standalone: with the contested key's OTP unaccounted-for, attributing it to
        // key 111 would be a guess between two plausible owners.
        Assert.Single(result, d => d.DeviceId == "otp-orphan");
        var clean = Assert.IsType<CompositeYubiKey>(Assert.Single(result, d => d.DeviceId == "ykphysical:111"));
        Assert.Equal(new[] { "ccid-clean", "fido-clean" }, clean.MemberDeviceIds);
        Assert.Single(result, d => d.DeviceId == "otp-contested");
        Assert.Single(result, d => d.DeviceId == "ccid-contested");
        Assert.Equal(4, result.Count);
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    ///     Output <c>DeviceId</c>s must be pairwise distinct. Two devices sharing an id would make the id
    ///     useless as the durable key the discovery contract tells consumers to rely on.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This invariant was originally prose only, and asserting it found a real defect: per-PID
    ///         minting produced two composites both named <c>ykphysical:500</c> when one serial appeared
    ///         under two PIDs. The winner rule (<c>ResolveContestedSerials</c>) now enforces it. This theory
    ///         is deliberately implementation-blind — it asserts only pairwise distinctness, so it keeps
    ///         guarding the invariant through any future change to how contested serials are resolved.
    ///     </para>
    ///     <para>
    ///         The theory feeds the same serial through two PID classes — the one shape that reaches the
    ///         duplicate-minting path.
    ///     </para>
    /// </remarks>
    [Theory]
    [InlineData(0x0407, 0x0403)]
    [InlineData(0x0407, 0x0407)]
    public void Merge_AnyVector_ProducesPairwiseDistinctDeviceIds(ushort firstPid, ushort secondPid)
    {
        const int sharedSerial = 500;

        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("ccid-first", ConnectionType.SmartCard, firstPid, sharedSerial),
            Descriptor("otp-first", ConnectionType.HidOtp, firstPid, sharedSerial),
            Descriptor("ccid-second", ConnectionType.SmartCard, secondPid, sharedSerial),
            Descriptor("otp-second", ConnectionType.HidOtp, secondPid, sharedSerial)
        ]);

        var ids = result.Select(d => d.DeviceId).ToList();

        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
    }

    private static DeviceInterfaceDescriptor Descriptor(
        string deviceId,
        ConnectionType connection,
        ushort? pid,
        int? serial = null,
        bool isUsb = true,
        string? topologyKey = null) =>
        new(new StubYubiKey(deviceId, connection), connection, isUsb, pid, serial, DeviceInfo: null, topologyKey);

    private static string Describe(IReadOnlyList<IYubiKey> result) =>
        string.Join("; ", result.Select(d => d is CompositeYubiKey c
            ? $"{c.DeviceId}=[{string.Join("|", c.MemberDeviceIds)}]"
            : $"{d.DeviceId}({d.AvailableConnections})"));

    private sealed class StubYubiKey(string deviceId, ConnectionType connection) : IYubiKey
    {
        public string DeviceId { get; } = deviceId;

        public ConnectionType AvailableConnections { get; } = connection;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            throw new InvalidOperationException("Merger vectors never open connections.");
    }
}