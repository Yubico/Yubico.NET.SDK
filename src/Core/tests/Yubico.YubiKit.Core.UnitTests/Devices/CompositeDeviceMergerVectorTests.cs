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
using Yubico.YubiKit.Core.UnitTests.Infrastructure;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
///     Unit-vector harness for <see cref="CompositeDeviceMerger.Merge" />. The merge tiers these vectors
///     exercise are specified in docs/architecture/device-discovery-guarantees.md.
/// </summary>
/// <remarks>
///     <para>
///         Method names distinguish two kinds of source-defined expectations:
///     </para>
///     <list type="bullet">
///         <item>
///             <b>Regression vectors</b> (names containing <c>Regression</c>) guard the generalized
///             completeness check and pigeonhole deduction against cross-key merges or avoidable fragments.
///         </item>
///         <item>
///             <b>Pin vectors</b> (names containing <c>Pin</c>) assert conservative outcomes or the accepted
///             partial-evidence bound. A passing pin alone establishes no defect repair or hardware result.
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
        Assert.Equal($"ykphysical:pid:{pid:X4}", device.DeviceId);
        Assert.Equal(Triple, device.AvailableConnections);
        Assert.Equal(3, InterfaceIdsOf(device).Count);
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
        Assert.Equal($"ykphysical:pid:{pid:X4}", device.DeviceId);
        Assert.Equal(first | second, device.AvailableConnections);
        Assert.Equal(2, InterfaceIdsOf(device).Count);
    }
    [Theory]
    [InlineData((ushort)0x0401, ConnectionType.HidOtp)]
    [InlineData((ushort)0x0402, ConnectionType.HidFido)]
    [InlineData((ushort)0x0404, ConnectionType.SmartCard)]
    [InlineData((ushort)0x0120, ConnectionType.HidFido)] // SKY
    public void Merge_SingleInterfacePid_PublishesOneSlotWithTransportShapedDeviceId_Pin(
        ushort pid,
        ConnectionType connection)
    {
        var result = CompositeDeviceMerger.Merge([Descriptor("only", connection, pid)]);

        var device = Assert.Single(result);
        Assert.Equal("only", device.DeviceId);
        Assert.Equal(connection, device.AvailableConnections);
        Assert.Single(InterfaceIdsOf(device));
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
        var keyA = Assert.Single(result, d => d.DeviceId == "ykphysical:111");
        var keyB = Assert.Single(result, d => d.DeviceId == "ykphysical:222");
        Assert.Equal(Triple, keyA.AvailableConnections);
        Assert.Equal(Triple, keyB.AvailableConnections);
        Assert.Equal(["ccid-a", "fido-a", "otp-a"], InterfaceIdsOf(keyA));
        Assert.Equal(["ccid-b", "fido-b", "otp-b"], InterfaceIdsOf(keyB));
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
        Assert.All(result, d => Assert.Equal(2, InterfaceIdsOf(d).Count));
        Assert.Contains(result, d => d.DeviceId == "ykphysical:111");
        Assert.Contains(result, d => d.DeviceId == "ykphysical:222");
        Assert.All(result, d => Assert.Equal(ConnectionType.HidOtp | ConnectionType.HidFido, d.AvailableConnections));
    }

    // ---------------------------------------------------------------------------------------------
    // 1b. Regression vectors for completeness checks and orphan attribution.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Merge_Regression_CrossKeyShapeB_TwoTripleKeysDisjointHidNoCcidNoSerials_MustStayStandalone()
    {
        // Key A's FIDO and key B's OTP share PID 0x0407, with no CCID or serial evidence. Their observed
        // set differs from the expected triple, so the completeness guard refuses PID grouping and the
        // serial path leaves both standalone.
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("fido-keyA", ConnectionType.HidFido, 0x0407),
            Descriptor("otp-keyB", ConnectionType.HidOtp, 0x0407)
        ]);

        var composites = result.Where(d => InterfaceIdsOf(d).Count > 1).ToList();
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
        // Pigeonhole deduction: both 0x0407 keys fully enumerated; serials known
        // for 5 of 6 interfaces. The null-serial OTP orphan's connection type exactly fills the single
        // missing slot of exactly ONE incomplete same-PID composite (key B), and type-count closure holds
        // (2 OTP interfaces visible, 2 candidate keys).
        // Attribution to key B yields two complete connection sets instead of a separate orphan.
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
        // Attribution to key B yields two complete OTP+FIDO connection sets.
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
        // would be a guess; deduction requires exactly one candidate, so both OTP interfaces stay standalone.
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
        var composites = result.Where(d => InterfaceIdsOf(d).Count > 1).ToList();
        Assert.Equal(2, composites.Count);
        Assert.All(
            composites,
            c => Assert.Equal(ConnectionType.SmartCard | ConnectionType.HidFido, c.AvailableConnections));
        var orphans = result.Where(d => InterfaceIdsOf(d).Count == 1).ToList();
        Assert.Equal(2, orphans.Count);
        Assert.All(orphans, o => Assert.Equal(ConnectionType.HidOtp, o.AvailableConnections));
    }

    // ---------------------------------------------------------------------------------------------
    // 1c. Accepted partial-evidence bound.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Merge_EpistemicBound_ComplementaryPartials_TwoDualKeysOneInterfaceEach_MergeIsRepresentable_Pin()
    {
        // Partial-evidence bound: two 0x0403 keys, each
        // with only ONE complementary interface enumerated (key A's OTP, key B's FIDO), no serials. The
        // two descriptors present the same grouping evidence as one fully visible 0x0403 key: PID count is 1
        // and observed == expected, so the merger takes the PID shortcut and groups across keys.
        // That result can persist while the implemented tiers make the same grouping decision. Later
        // regrouping requires evidence those tiers use to distinguish the members. Adding different
        // serials alone can leave a complete PID-count-one group on the same shortcut, which checks
        // connection completeness and cross-PID contested serials, not same-PID serial disagreement.
        // The assertions pin that cross-key member set; this single-scan vector does not test convergence.
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("otp-keyA", ConnectionType.HidOtp, 0x0403),
            Descriptor("fido-keyB", ConnectionType.HidFido, 0x0403)
        ]);

        var device = Assert.Single(result);
        Assert.Equal("ykphysical:pid:0403", device.DeviceId);
        Assert.Equal(ConnectionType.HidOtp | ConnectionType.HidFido, device.AvailableConnections);
        Assert.Equal(["fido-keyB", "otp-keyA"], InterfaceIdsOf(device));
    }

    [Fact]
    public void Merge_ComplementaryPartialMasquerade_MisattributionIsRepresentableAndBounded_Pin()
    {
        // Partial-evidence bound: key A is anchored as
        // {OTP, FIDO} (serial 111) while key B is visible ONLY as an unread CCID (null serial). The three
        // descriptors present the same grouping evidence as one fully visible 0x0407 key: PID count is 1 and
        // observed == expected, so the merger takes the PID shortcut — attributing key B's CCID to key A.
        // The misattribution can persist while the implemented tiers make the same grouping decision.
        // Later regrouping requires evidence those tiers use to distinguish the members; a different
        // serial on this same-PID CCID alone need not bypass the complete PID-count-one shortcut.
        // The assertions pin the misattributed members in one scan, not a later healing scan.
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("otp-keyA", ConnectionType.HidOtp, 0x0407, serial: 111),
            Descriptor("fido-keyA", ConnectionType.HidFido, 0x0407, serial: 111),
            Descriptor("ccid-keyB", ConnectionType.SmartCard, 0x0407) // physically key B; unreadable
        ]);

        var device = Assert.Single(result);
        Assert.Equal("ykphysical:pid:0407", device.DeviceId);
        Assert.Equal(Triple, device.AvailableConnections);
        Assert.Equal(["ccid-keyB", "fido-keyA", "otp-keyA"], InterfaceIdsOf(device));
    }

    // ---------------------------------------------------------------------------------------------
    // 1d. Serial-less same-PID pairs without topology evidence.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Merge_TwoSamePidTripleKeysNoSerialsFullVisibility_ConservativeSplit_Pin()
    {
        // Serial-less pair (PIN): two 0x0407 keys fully visible, no serial evidence. PID count is 2, so
        // the merger routes to the serial path; null serials never collapse. Conservative split (6
        // standalone) is the current macOS/Linux no-topology behavior (guarantee
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
        Assert.All(result, d => Assert.Single(InterfaceIdsOf(d)));

        // Merge_SeriallessPairWithDistinctTopologyKeys_GroupsIntoTwoCompleteKeys covers the keyed case.
        // This vector retains the six-way split when those keys are absent on any platform.
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
        Assert.All(result, d => Assert.Single(InterfaceIdsOf(d)));
    }

    // ---------------------------------------------------------------------------------------------
    // 1e. Reconfiguration transitions (PINs).
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Merge_ReconfiguredKeyReenumeratedUnderNewPid_GroupsByCurrentPidTruth_Pin()
    {
        // Model reconfiguration from 0x0407 to 0x0403 by supplying only the new descriptors. This vector
        // checks grouping of that set; it does not exercise OS re-enumeration or finder cache eviction.
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("otp-a-new", ConnectionType.HidOtp, 0x0403),
            Descriptor("fido-a-new", ConnectionType.HidFido, 0x0403)
        ]);

        var device = Assert.Single(result);
        Assert.Equal("ykphysical:pid:0403", device.DeviceId);
        Assert.Equal(ConnectionType.HidOtp | ConnectionType.HidFido, device.AvailableConnections);
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
        var keyA = Assert.Single(result, d => d.DeviceId == "ykphysical:pid:0407");
        var keyB = Assert.Single(result, d => d.DeviceId == "ykphysical:pid:0403");
        Assert.Equal(Triple, keyA.AvailableConnections);
        Assert.Equal(ConnectionType.HidOtp | ConnectionType.HidFido, keyB.AvailableConnections);
    }

    // ---------------------------------------------------------------------------------------------
    // Tier 1 topology evidence.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Merge_SeriallessPairWithDistinctTopologyKeys_GroupsIntoTwoCompleteKeys()
    {
        // Two same-PID 0x0407 keys have no serials, but each interface carries a scripted container key.
        // Topology groups them into two complete keys. This checks supplied-key grouping, not Windows
        // Container ID acquisition or native interoperability.
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
        var keyA = Assert.Single(result, d => d.DeviceId == "ykphysical:topology:container-A");
        var keyB = Assert.Single(result, d => d.DeviceId == "ykphysical:topology:container-B");
        Assert.Equal(Triple, keyA.AvailableConnections);
        Assert.Equal(Triple, keyB.AvailableConnections);
        Assert.Equal(["ccid-a", "fido-a", "otp-a"], InterfaceIdsOf(keyA));
        Assert.Equal(["ccid-b", "fido-b", "otp-b"], InterfaceIdsOf(keyB));
    }

    [Fact]
    public void Merge_ComplementaryPartialsWithTopologyKeys_SplitByTopology_NotMergedByPid()
    {
        // Complementary partials have the same PID and no serials, but distinct supplied topology keys.
        // Those keys distinguish the members that the no-topology vector groups across keys.
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("otp-keyA", ConnectionType.HidOtp, 0x0403, topologyKey: "container-A"),
            Descriptor("fido-keyB", ConnectionType.HidFido, 0x0403, topologyKey: "container-B")
        ]);

        Assert.True(
            result.Count == 2,
            "Topology evidence must split complementary partials from two physical keys; got " +
            $"{result.Count} device(s): {Describe(result)}.");
        Assert.All(result, d => Assert.Single(InterfaceIdsOf(d)));
        Assert.Equal(["fido-keyB", "otp-keyA"], result.Select(d => d.DeviceId).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Merge_TopologyAbsentForAllInterfaces_IsByteIdenticalToPreTopologyBehavior_Pin()
    {
        // With no topology keys, assert the six standalone members of a serial-less pair and the single
        // PID-shaped result for complementary partials. These expected shapes check fallback behavior;
        // the test does not serialize or compare a second merger implementation.
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
        Assert.All(seriallessPair, d => Assert.Single(InterfaceIdsOf(d)));

        var complementaryPartials = CompositeDeviceMerger.Merge(
        [
            Descriptor("otp-keyA", ConnectionType.HidOtp, 0x0403),
            Descriptor("fido-keyB", ConnectionType.HidFido, 0x0403)
        ]);

        var bounded = Assert.Single(complementaryPartials);
        Assert.Equal("ykphysical:pid:0403", bounded.DeviceId);
    }

    [Fact]
    public void Merge_PartialTopology_KeyedInterfacesGroup_UnkeyedFallThroughUnguessed_Pin()
    {
        // Mixed/partial topology (the Windows "CCID resolves, HID doesn't" case): interfaces WITH keys
        // group by topology; interfaces WITHOUT keys must never be guessed into a topology group — they
        // fall through to the unchanged tiers. Here key A's CCID+FIDO carry a container id while its OTP
        // does not; the OTP is a 0x0407 interface whose PID group is now
        // topology-free and observed(OTP) != expected(triple), so it stands alone conservatively.
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("ccid-a", ConnectionType.SmartCard, 0x0407, topologyKey: "container-A"),
            Descriptor("fido-a", ConnectionType.HidFido, 0x0407, topologyKey: "container-A"),
            Descriptor("otp-unresolved", ConnectionType.HidOtp, 0x0407)
        ]);

        Assert.Equal(2, result.Count);
        var keyA = Assert.Single(result, d => d.DeviceId == "ykphysical:topology:container-A");
        Assert.Equal(ConnectionType.SmartCard | ConnectionType.HidFido, keyA.AvailableConnections);
        Assert.DoesNotContain("otp-unresolved", InterfaceIdsOf(keyA));
        var unkeyed = Assert.Single(result, d => InterfaceIdsOf(d).Count == 1);
        Assert.Equal("otp-unresolved", unkeyed.DeviceId);
    }

    [Fact]
    public void Merge_MixedTopologyAndSerialEvidence_IsDeterministicAndConserving_Pin()
    {
        // Mixed evidence: key A resolved by topology (tier 1), keys B and C are a same-PID 0x0403 pair
        // that only serial evidence can separate (tier 2 — PID count is 2, so no PID merge), plus an NFC
        // interface standing alone. Conservation: every input interface appears exactly once across the
        // returned devices, and no interface is attributed twice.
        // Only this input order is exercised; permutation invariance is outside these assertions.
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
        var keyA = Assert.Single(result, d => d.DeviceId == "ykphysical:topology:container-A");
        Assert.Equal(Triple, keyA.AvailableConnections);
        var keyB = Assert.Single(result, d => d.DeviceId == "ykphysical:222");
        Assert.Equal(ConnectionType.HidOtp | ConnectionType.HidFido, keyB.AvailableConnections);
        var keyC = Assert.Single(result, d => d.DeviceId == "ykphysical:333");
        Assert.Equal(ConnectionType.HidOtp | ConnectionType.HidFido, keyC.AvailableConnections);
        Assert.Single(result, d => d.DeviceId == "nfc-reader");

        // Conservation: each input interface id appears exactly once across all returned devices.
        var attributed = result.SelectMany(InterfaceIdsOf).ToList();
        Assert.Equal(descriptors.Length, attributed.Count);
        Assert.Equal(
            descriptors.Select(d => d.Device.InterfaceId).Order(StringComparer.Ordinal),
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

        Assert.Equal(3, result.Count);
        Assert.All(result, d => Assert.Single(InterfaceIdsOf(d)));
    }

    [Fact]
    public void Merge_SerialFallbackWithDuplicateSlots_PreservesBothAsStandalone()
    {
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("ccid-old", ConnectionType.SmartCard, 0x0407, serial: 500),
            Descriptor("ccid-new", ConnectionType.SmartCard, 0x0407, serial: 500)
        ], pidCorrelationUntrusted: true);

        Assert.Equal(2, result.Count);
        Assert.All(result, device => Assert.Single(InterfaceIdsOf(device)));
        Assert.Equal(2, result.SelectMany(InterfaceIdsOf).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Merge_TopologyGroupWithDuplicateSlots_PreservesBothAsStandalone()
    {
        var result = CompositeDeviceMerger.Merge(
        [
            Descriptor("ccid-old", ConnectionType.SmartCard, 0x0407, topologyKey: "container-A"),
            Descriptor("ccid-new", ConnectionType.SmartCard, 0x0407, topologyKey: "container-A")
        ]);

        Assert.Equal(2, result.Count);
        Assert.All(result, device => Assert.Single(InterfaceIdsOf(device)));
        Assert.Equal(2, result.SelectMany(InterfaceIdsOf).Distinct(StringComparer.Ordinal).Count());
    }

    // ---------------------------------------------------------------------------------------------
    // 5. Cross-PID contested serials — the winner rule.
    //
    // A serial observed under more than one PID in a single scan is one physical key caught
    // mid-reconfiguration (serial is globally unique per key; PID changes with configuration). The
    // groups must NOT be merged into one published device: the flat shape has one slot per connection
    // type, and AvailableConnections' flags-union would hide a duplicate candidate.
    // Instead, at most one group may carry the scan's ykphysical:{serial} id — the group whose
    // anchored census exactly matches its PID's expected interface set — and every other contested
    // group's interfaces publish standalone. Zero or multiple complete groups is ambiguity, and
    // ambiguity fragments conservatively rather than guessing.
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    ///     One complete group wins the serial-shaped id; the other group's interfaces stand alone.
    /// </summary>
    /// <remarks>
    ///     The 0x0403 group's anchored census matches its expected set; the 0x0407 group lacks FIDO.
    ///     Completeness selects 0x0403 here as a heuristic, not proof of which enumeration is live.
    ///     The assertions check the winner's members and the other group's standalone results.
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

        var winner = Assert.Single(result, d => d.DeviceId == "ykphysical:500");
        Assert.Equal(new[] { "fido-new", "otp-new" }, InterfaceIdsOf(winner));
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
    ///     be a guess, so neither gets the serial-shaped id. Later convergence requires changed evidence.
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
        Assert.All(result, d => Assert.Single(InterfaceIdsOf(d)));
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
        Assert.All(result, d => Assert.Single(InterfaceIdsOf(d)));
    }

    /// <summary>
    ///     A contested serial is not a pigeonhole candidate: a null-serial orphan cannot rescue a losing
    ///     group into completeness, and is published standalone alongside it.
    /// </summary>
    /// <remarks>
    ///     Winner determination uses anchored (serial-bearing) members only. If attributed orphans could
    ///     complete a contested census, orphan attribution — a deduction — would decide which enumeration
    ///     gets the serial-shaped identifier, inverting the evidence hierarchy during the one window where the
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

        var winner = Assert.Single(result, d => d.DeviceId == "ykphysical:500");
        Assert.Equal(new[] { "fido-new", "otp-new" }, InterfaceIdsOf(winner));
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
    ///     during a contest. Later convergence requires changed evidence. Without the contested loser present, the same
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
        var clean = Assert.Single(result, d => d.DeviceId == "ykphysical:111");
        Assert.Equal(new[] { "ccid-clean", "fido-clean" }, InterfaceIdsOf(clean));
        Assert.Single(result, d => d.DeviceId == "otp-contested");
        Assert.Single(result, d => d.DeviceId == "ccid-contested");
        Assert.Equal(4, result.Count);
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    ///     Output <c>DeviceId</c>s must be pairwise distinct. Two devices sharing an id would make the id
    ///     ambiguous within that scan. DeviceId is discovery-scoped; the serial supplies durable correlation.
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
        new(new FakeSlot(deviceId, connection), connection, isUsb, pid, serial, DeviceInfo: null, topologyKey);

    private static IReadOnlyList<string> InterfaceIdsOf(IYubiKey device) =>
        Assert.IsType<YubiKeyDevice>(device).InterfaceIds;

    private static string Describe(IReadOnlyList<IYubiKey> result) =>
        string.Join("; ", result.Select(d =>
            $"{d.DeviceId}=[{string.Join("|", InterfaceIdsOf(d))}]"));

    private sealed class FakeSlot(string deviceId, ConnectionType connection) : IYubiKeyConnectionSlot
    {
        public string InterfaceId { get; } = deviceId;

        public ConnectionType ConnectionType { get; } = connection;
    }
}