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
using Yubico.YubiKit.Core.Native;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
///     Phase-3 seam vectors for the Windows topology tier (composite-merge remediation PLAN.md, "A′-Windows
///     interop"). All native operations are scripted through <see cref="IWindowsTopologyNativeOps" />, so the
///     full resolver decision tree runs keyless on ANY OS — mirroring the LinuxUdevHidEventSourceTests
///     pattern. Windows HARDWARE validation remains deferred to Phase 4 Tier 2; these vectors prove the
///     logic and every documented failure-mode degradation, not the P/Invoke marshalling itself.
/// </summary>
public class WindowsDeviceTopologyResolverTests
{
    private static readonly Guid ContainerA = new("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void TryGetTopologyKey_SmartCardHappyPath_ResolvesReaderThenContainerId()
    {
        var ops = new ScriptedNativeOps
        {
            ReaderInstanceIds = { ["Yubico YubiKey OTP+FIDO+CCID 0"] = "USB\\VID_1050&PID_0407\\6&ABC" },
            ContainerIdsByInstanceId = { ["USB\\VID_1050&PID_0407\\6&ABC"] = ContainerA }
        };
        var resolver = new WindowsDeviceTopologyResolver(ops);

        var resolved = resolver.TryGetTopologyKey(
            new FakeDevice("Yubico YubiKey OTP+FIDO+CCID 0"),
            ConnectionType.SmartCard,
            out var topologyKey);

        Assert.True(resolved);
        Assert.Equal(ContainerA.ToString("D"), topologyKey);
    }

    [Theory]
    [InlineData(ConnectionType.HidFido)]
    [InlineData(ConnectionType.HidOtp)]
    public void TryGetTopologyKey_HidHappyPath_ResolvesContainerIdFromDevicePath(ConnectionType connection)
    {
        var devicePath = "\\\\?\\hid#vid_1050&pid_0407&mi_01#7&XYZ";
        var ops = new ScriptedNativeOps { ContainerIdsByDevicePath = { [devicePath] = ContainerA } };
        var resolver = new WindowsDeviceTopologyResolver(ops);

        var resolved = resolver.TryGetTopologyKey(new FakeDevice(devicePath), connection, out var topologyKey);

        Assert.True(resolved);
        Assert.Equal(ContainerA.ToString("D"), topologyKey);
        Assert.Empty(ops.ReaderLookups); // HID must never go through the WinSCard path
    }

    [Fact]
    public void TryGetTopologyKey_CcidAndHidOfOneKey_ShareOneTopologyKey()
    {
        // The contract the merger depends on: the Container ID is identical across all interfaces of one
        // composite USB device, including its HID interfaces.
        const string readerName = "Yubico YubiKey OTP+FIDO+CCID 0";
        const string instanceId = "USB\\VID_1050&PID_0407\\6&ABC";
        const string hidPath = "\\\\?\\hid#vid_1050&pid_0407&mi_01#7&XYZ";
        var ops = new ScriptedNativeOps
        {
            ReaderInstanceIds = { [readerName] = instanceId },
            ContainerIdsByInstanceId = { [instanceId] = ContainerA },
            ContainerIdsByDevicePath = { [hidPath] = ContainerA }
        };
        var resolver = new WindowsDeviceTopologyResolver(ops);

        Assert.True(resolver.TryGetTopologyKey(new FakeDevice(readerName), ConnectionType.SmartCard, out var ccidKey));
        Assert.True(resolver.TryGetTopologyKey(new FakeDevice(hidPath), ConnectionType.HidFido, out var hidKey));

        Assert.Equal(ccidKey, hidKey);
    }

    [Fact]
    public void TryGetTopologyKey_ReaderInstanceIdLookupFails_DegradesToUnknown()
    {
        // SCardGetReaderDeviceInstanceId unavailable (pre-Win8) or unknown/invalid reader.
        var resolver = new WindowsDeviceTopologyResolver(new ScriptedNativeOps());

        var resolved = resolver.TryGetTopologyKey(
            new FakeDevice("Yubico YubiKey OTP+FIDO+CCID 0"),
            ConnectionType.SmartCard,
            out var topologyKey);

        Assert.False(resolved);
        Assert.Null(topologyKey);
    }

    [Fact]
    public void TryGetTopologyKey_ReaderApiThrows_DegradesToUnknownWithoutPropagating()
    {
        // An SCardException / EntryPointNotFoundException from the P/Invoke must never escape a scan.
        var ops = new ScriptedNativeOps
        {
            ReaderLookupException = () => new EntryPointNotFoundException("SCardGetReaderDeviceInstanceIdW")
        };
        var resolver = new WindowsDeviceTopologyResolver(ops);

        var resolved = resolver.TryGetTopologyKey(
            new FakeDevice("Yubico YubiKey OTP+FIDO+CCID 0"),
            ConnectionType.SmartCard,
            out var topologyKey);

        Assert.False(resolved);
        Assert.Null(topologyKey);
    }

    [Fact]
    public void TryGetTopologyKey_LocateDevNodeNoSuchDevNode_DegradesToUnknown()
    {
        // Stale device instance id mid-hotplug: CM_Locate_DevNode returns CR_NO_SUCH_DEVNODE, which
        // WindowsTopologyNativeOps surfaces as a failed lookup. Never inferred, never guessed.
        const string readerName = "Yubico YubiKey OTP+FIDO+CCID 0";
        var ops = new ScriptedNativeOps
        {
            ReaderInstanceIds = { [readerName] = "USB\\VID_1050&PID_0407\\STALE" },
            ContainerIdLookupException = () => new PlatformApiException(
                "CONFIG_RET", 13, "CR_NO_SUCH_DEVNODE")
        };
        var resolver = new WindowsDeviceTopologyResolver(ops);

        var resolved = resolver.TryGetTopologyKey(new FakeDevice(readerName), ConnectionType.SmartCard, out var key);

        Assert.False(resolved);
        Assert.Null(key);
    }

    [Fact]
    public void TryGetTopologyKey_ContainerIdPropertyMissing_DegradesToUnknown()
    {
        const string readerName = "Yubico YubiKey OTP+FIDO+CCID 0";
        var ops = new ScriptedNativeOps
        {
            ReaderInstanceIds = { [readerName] = "USB\\VID_1050&PID_0407\\6&ABC" }
            // No ContainerId entry: the DEVPKEY_Device_ContainerId property read failed.
        };
        var resolver = new WindowsDeviceTopologyResolver(ops);

        Assert.False(resolver.TryGetTopologyKey(new FakeDevice(readerName), ConnectionType.SmartCard, out var key));
        Assert.Null(key);
    }

    [Fact]
    public void TryGetTopologyKey_EmptyContainerId_IsTreatedAsUnknown()
    {
        // Windows reports an all-zero ContainerId for devices with no container. Using it as a group key
        // would fuse unrelated interfaces, so it must be rejected.
        const string readerName = "Yubico YubiKey OTP+FIDO+CCID 0";
        const string instanceId = "USB\\VID_1050&PID_0407\\6&ABC";
        var ops = new ScriptedNativeOps
        {
            ReaderInstanceIds = { [readerName] = instanceId },
            ContainerIdsByInstanceId = { [instanceId] = Guid.Empty }
        };
        var resolver = new WindowsDeviceTopologyResolver(ops);

        Assert.False(resolver.TryGetTopologyKey(new FakeDevice(readerName), ConnectionType.SmartCard, out var key));
        Assert.Null(key);
    }

    [Fact]
    public void TryGetTopologyKey_MixedCcidResolvesHidDoesNot_YieldsPartialTopologySafely()
    {
        // The documented partial case: CCID resolves, HID does not. Each interface is answered
        // independently — the HID interface gets NO key rather than inheriting the CCID's.
        const string readerName = "Yubico YubiKey OTP+FIDO+CCID 0";
        const string instanceId = "USB\\VID_1050&PID_0407\\6&ABC";
        var ops = new ScriptedNativeOps
        {
            ReaderInstanceIds = { [readerName] = instanceId },
            ContainerIdsByInstanceId = { [instanceId] = ContainerA }
        };
        var resolver = new WindowsDeviceTopologyResolver(ops);

        Assert.True(resolver.TryGetTopologyKey(new FakeDevice(readerName), ConnectionType.SmartCard, out var ccidKey));
        Assert.False(resolver.TryGetTopologyKey(
            new FakeDevice("\\\\?\\hid#vid_1050&pid_0407&mi_01#7&UNKNOWN"),
            ConnectionType.HidFido,
            out var hidKey));

        Assert.Equal(ContainerA.ToString("D"), ccidKey);
        Assert.Null(hidKey);
    }

    [Fact]
    public void TryGetTopologyKey_EmptyReaderName_DegradesWithoutCallingNative()
    {
        var ops = new ScriptedNativeOps();
        var resolver = new WindowsDeviceTopologyResolver(ops);

        Assert.False(resolver.TryGetTopologyKey(new FakeDevice(string.Empty), ConnectionType.SmartCard, out var key));
        Assert.Null(key);
        Assert.Empty(ops.ReaderLookups);
    }

    [Fact]
    public void NullResolver_AlwaysReportsUnknown_PinnedPlatformBound()
    {
        // macOS/Linux platform bound: no reader/interface → USB device mapping exists, so topology
        // evidence is never available and tiers 2..5 always decide.
        var resolver = NullDeviceTopologyResolver.Instance;

        Assert.False(resolver.TryGetTopologyKey(new FakeDevice("any"), ConnectionType.SmartCard, out var ccid));
        Assert.False(resolver.TryGetTopologyKey(new FakeDevice("any"), ConnectionType.HidFido, out var hid));
        Assert.Null(ccid);
        Assert.Null(hid);
    }

    [Fact]
    public void Create_OnNonWindows_ReturnsNullResolver()
    {
        // Guards the load-safety contract: on macOS/Linux the factory must never construct the Windows
        // implementation (which would bind Windows-only native entry points).
        var resolver = DeviceTopologyResolver.Create();

        if (SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows)
            Assert.IsType<WindowsDeviceTopologyResolver>(resolver);
        else
            Assert.Same(NullDeviceTopologyResolver.Instance, resolver);
    }

    private sealed class ScriptedNativeOps : IWindowsTopologyNativeOps
    {
        public Dictionary<string, string> ReaderInstanceIds { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, Guid> ContainerIdsByInstanceId { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, Guid> ContainerIdsByDevicePath { get; } = new(StringComparer.Ordinal);
        public List<string> ReaderLookups { get; } = [];
        public Func<Exception>? ReaderLookupException { get; init; }
        public Func<Exception>? ContainerIdLookupException { get; init; }

        public bool TryGetReaderDeviceInstanceId(string readerName, out string? deviceInstanceId)
        {
            ReaderLookups.Add(readerName);
            if (ReaderLookupException is not null)
                throw ReaderLookupException();

            return ReaderInstanceIds.TryGetValue(readerName, out deviceInstanceId);
        }

        public bool TryGetContainerIdByInstanceId(string deviceInstanceId, out Guid containerId)
        {
            if (ContainerIdLookupException is not null)
                throw ContainerIdLookupException();

            return ContainerIdsByInstanceId.TryGetValue(deviceInstanceId, out containerId);
        }

        public bool TryGetContainerIdByDevicePath(string devicePath, out Guid containerId)
        {
            if (ContainerIdLookupException is not null)
                throw ContainerIdLookupException();

            return ContainerIdsByDevicePath.TryGetValue(devicePath, out containerId);
        }
    }

    private sealed class FakeDevice(string readerName) : IDevice
    {
        public string ReaderName { get; } = readerName;
    }
}