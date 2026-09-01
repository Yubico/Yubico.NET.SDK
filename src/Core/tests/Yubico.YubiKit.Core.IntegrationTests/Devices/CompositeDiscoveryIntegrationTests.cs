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
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

/// <summary>
///     Key-count-agnostic composite-discovery invariants; the guarantees under test are specified in
///     docs/architecture/device-discovery-guarantees.md. Requires one or more allow-listed composite USB
///     YubiKeys; no touch / user-presence required. The invariants hold for any number of keys, so
///     single-key rigs keep passing while multi-key rigs exercise the same contracts.
/// </summary>
/// <remarks>
///     Expectations are computed from an independent raw USB enumeration (PC/SC reader names + HID
///     descriptors) taken by the test itself, so no assertion hard-codes a key count. The rig is assumed
///     USB-only (no NFC reader attached) and idle (no other process holding interfaces exclusively).
///     Per Phase 0, zero-orphans / completeness / stability are EXPECTED RED on multi-same-PID-key rigs in
///     fresh-process single-scan conditions until Phase 2 lands; conservation (interface count) is expected
///     to hold today.
/// </remarks>
public class CompositeDiscoveryIntegrationTests : IAsyncLifetime
{
    private static readonly ConnectionType[] ConcreteTypes =
    [
        ConnectionType.SmartCard,
        ConnectionType.HidFido,
        ConnectionType.HidOtp
    ];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await YubiKeyManager.ShutdownAsync();

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task FindAllAsync_Conservation_EveryEnumeratedUsbInterfaceAppearsExactlyOnce()
    {
        // Conservation: for every concrete interface type, the number of independently enumerated USB
        // YubiKey interfaces equals the number of returned physical devices exposing that type — no
        // interface is lost and none is double-attributed, regardless of how grouping went.
        var raw = await EnumerateRawUsbInterfacesAsync();
        Assert.NotEmpty(raw);

        var devices = await TransientScanRetry.ScanAsync(
            () => YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: true));

        foreach (var type in ConcreteTypes)
        {
            var enumerated = raw.Count(i => i.Type == type);
            var exposed = devices.Count(d => Supports(d, type));
            Assert.True(
                enumerated == exposed,
                $"Conservation violated for {type}: {enumerated} interface(s) enumerated at the USB layer " +
                $"but {exposed} returned device(s) expose the type. Devices: {Describe(devices)}");
        }
        // Per-connection filters must return exactly the devices from the same snapshot exposing the type.
        foreach (var type in ConcreteTypes)
        {
            var filtered = await TransientScanRetry.ScanAsync(() => YubiKeyManager.FindAllAsync(type));
            Assert.Equal(
                devices.Where(d => Supports(d, type)).Select(d => d.DeviceId).Order(StringComparer.Ordinal),
                filtered.Select(d => d.DeviceId).Order(StringComparer.Ordinal));
        }
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task FindAllAsync_ZeroOrphansWhenIdle_DeviceCountEqualsPhysicalKeyCount()
    {
        // Zero orphans when idle: on an idle rig every interface groups into its physical key, so the
        // number of returned devices equals the number of physical keys (derived per PID as the maximum
        // per-interface-type count, mirroring the USB truth that one key appears once per transport).
        var raw = await EnumerateRawUsbInterfacesAsync();
        Assert.NotEmpty(raw);
        var expectedKeyCount = KeyCountPerPid(raw).Values.Sum();

        var devices = await TransientScanRetry.ScanAsync(
            () => YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: true));

        Assert.True(
            devices.Count == expectedKeyCount,
            $"Orphaned interfaces: expected {expectedKeyCount} physical device(s) from the enumerated USB " +
            $"interface set but discovery returned {devices.Count}. Devices: {Describe(devices)}");
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task FindAllAsync_CompletenessPerPid_EveryDeviceExposesItsFullExpectedInterfaceSet()
    {
        // Completeness per PID: each returned physical device must expose exactly the interface set its
        // USB Product ID advertises (ExpectedConnectionsForPid) — compared as multisets so any number of
        // keys per PID class is supported.
        var raw = await EnumerateRawUsbInterfacesAsync();
        Assert.NotEmpty(raw);
        var expectedShapes = KeyCountPerPid(raw)
            .SelectMany(kvp => Enumerable.Repeat(ExpectedConnectionsByPid[kvp.Key], kvp.Value))
            .OrderBy(c => (int)c)
            .ToList();

        var devices = await TransientScanRetry.ScanAsync(
            () => YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: true));
        var actualShapes = devices.Select(d => d.AvailableConnections).OrderBy(c => (int)c).ToList();

        Assert.True(
            expectedShapes.SequenceEqual(actualShapes),
            "Incomplete grouping: expected interface-set multiset " +
            $"[{string.Join(", ", expectedShapes)}] but discovery returned [{string.Join(", ", actualShapes)}]. " +
            $"Devices: {Describe(devices)}");
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task FindAllAsync_TwoConsecutiveScansOnOneManager_ReturnStableGrouping()
    {
        // Stability: two consecutive scans on one manager (identity cache shared) must produce the same
        // grouping — same device count, same interface-set multiset, same device identities.
        var scan1 = await TransientScanRetry.ScanAsync(
            () => YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: true));
        var scan2 = await TransientScanRetry.ScanAsync(
            () => YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: true));

        Assert.True(
            scan1.Count == scan2.Count,
            $"Grouping unstable: scan 1 returned {scan1.Count} device(s) [{Describe(scan1)}] but scan 2 " +
            $"returned {scan2.Count} [{Describe(scan2)}].");
        Assert.Equal(
            scan1.Select(d => d.AvailableConnections).OrderBy(c => (int)c),
            scan2.Select(d => d.AvailableConnections).OrderBy(c => (int)c));
        Assert.Equal(
            scan1.Select(d => d.DeviceId).Order(StringComparer.Ordinal),
            scan2.Select(d => d.DeviceId).Order(StringComparer.Ordinal));
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task FindAllAsync_RepeatedScans_EventuallyPopulateMetadataOnEveryRetainedDevice()
    {
        // Best-effort metadata reads use four process-wide worker slots. A rig with more keys can leave a
        // first-scan object without metadata, but later scans must retry and propagate successful metadata
        // onto the object retained by the repository without requiring a hot-plug.
        IReadOnlyList<IYubiKey> devices = [];
        for (var attempt = 0; attempt < 3; attempt++)
        {
            devices = await YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: true);
            if (devices.All(device => Assert.IsType<YubiKeyDevice>(device).DeviceInfo is not null))
                break;
        }

        Assert.NotEmpty(devices);
        Assert.All(devices, device => Assert.NotNull(Assert.IsType<YubiKeyDevice>(device).DeviceInfo));
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task ConnectAsync_TypedTransports_OnEveryReturnedDevice_Succeed()
    {
        // Every returned device must honor its advertised interface set with a working typed connect
        // (no touch required for opening and closing a connection).
        var devices = await TransientScanRetry.ScanAsync(
            () => YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: true));
        Assert.NotEmpty(devices);

        foreach (var device in devices)
        {
            if (Supports(device, ConnectionType.SmartCard))
            {
                await using var smartCard = await device.ConnectAsync<ISmartCardConnection>();
                Assert.NotNull(smartCard);
            }

            if (Supports(device, ConnectionType.HidFido))
            {
                await using var fido = await device.ConnectAsync<IFidoHidConnection>();
                Assert.NotNull(fido);
            }

            if (Supports(device, ConnectionType.HidOtp))
            {
                await using var otp = await device.ConnectAsync<IOtpHidConnection>();
                Assert.NotNull(otp);
            }
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Independent raw USB enumeration (the oracle the invariants are computed from)
    // ---------------------------------------------------------------------------------------------

    private const short YubicoVendorId = 0x1050;

    // Mirror of ReaderNamePidParser.ExpectedConnectionsForPid (internal to Core): the USB Product ID's
    // advertised interface set. Kept locally so the integration oracle stays independent of the
    // implementation under test.
    private static readonly IReadOnlyDictionary<ushort, ConnectionType> ExpectedConnectionsByPid =
        new Dictionary<ushort, ConnectionType>
        {
            [0x0110] = ConnectionType.HidOtp,
            [0x0111] = ConnectionType.HidOtp | ConnectionType.SmartCard,
            [0x0112] = ConnectionType.SmartCard,
            [0x0113] = ConnectionType.HidFido,
            [0x0114] = ConnectionType.HidOtp | ConnectionType.HidFido,
            [0x0115] = ConnectionType.HidFido | ConnectionType.SmartCard,
            [0x0116] = ConnectionType.HidOtp | ConnectionType.HidFido | ConnectionType.SmartCard,
            [0x0120] = ConnectionType.HidFido, // Security Key (SKY)
            [0x0401] = ConnectionType.HidOtp,
            [0x0402] = ConnectionType.HidFido,
            [0x0403] = ConnectionType.HidOtp | ConnectionType.HidFido,
            [0x0404] = ConnectionType.SmartCard,
            [0x0405] = ConnectionType.HidOtp | ConnectionType.SmartCard,
            [0x0406] = ConnectionType.HidFido | ConnectionType.SmartCard,
            [0x0407] = ConnectionType.HidOtp | ConnectionType.HidFido | ConnectionType.SmartCard
        };

    private sealed record RawUsbInterface(ConnectionType Type, ushort Pid);

    private static async Task<IReadOnlyList<RawUsbInterface>> EnumerateRawUsbInterfacesAsync()
    {
        var pcscDevices = await TransientScanRetry.ScanAsync(() => FindPcscDevices.Create().FindAllAsync());

        var hidDevices = await FindHidDevices.Create().FindAllAsync();

        var interfaces = new List<RawUsbInterface>();

        foreach (var reader in pcscDevices)
        {
            if (reader.Kind != PscsConnectionKind.Usb)
                continue;

            var pid = PidFromReaderName(reader.ReaderName);
            Assert.True(
                pid is not null,
                $"USB PC/SC reader '{reader.ReaderName}' did not parse to a known YubiKey PID; the rig is " +
                "in an unexpected state for these invariants.");
            interfaces.Add(new RawUsbInterface(ConnectionType.SmartCard, pid!.Value));
        }

        foreach (var device in hidDevices)
        {
            if (device.DescriptorInfo.VendorId != YubicoVendorId)
                continue;

            var pid = (ushort)device.DescriptorInfo.ProductId;
            if (!ExpectedConnectionsByPid.ContainsKey(pid))
                continue;

            var type = device.InterfaceType switch
            {
                HidInterfaceType.Fido => ConnectionType.HidFido,
                HidInterfaceType.Otp => ConnectionType.HidOtp,
                _ => ConnectionType.Unknown
            };
            if (type != ConnectionType.Unknown)
                interfaces.Add(new RawUsbInterface(type, pid));
        }

        return interfaces;
    }

    // Mirror of the Rust reference / ReaderNamePidParser: the USB YubiKey PC/SC reader name encodes the
    // enabled interface set, which maps deterministically to a PID.
    private static ushort? PidFromReaderName(string readerName)
    {
        var lower = readerName.ToLowerInvariant();
        if (!lower.Contains("yubico yubikey"))
            return null;

        var otp = lower.Contains("otp");
        var fido = lower.Contains("fido") || lower.Contains("u2f");
        var ccid = lower.Contains("ccid");
        var isNeo = lower.Contains("neo");

        return (isNeo, otp, fido, ccid) switch
        {
            (true, true, false, false) => 0x0110,
            (true, true, false, true) => 0x0111,
            (true, false, false, true) => 0x0112,
            (true, false, true, false) => 0x0113,
            (true, true, true, false) => 0x0114,
            (true, false, true, true) => 0x0115,
            (true, true, true, true) => 0x0116,
            (false, true, false, false) => 0x0401,
            (false, false, true, false) => 0x0402,
            (false, true, true, false) => 0x0403,
            (false, false, false, true) => 0x0404,
            (false, true, false, true) => 0x0405,
            (false, false, true, true) => 0x0406,
            (false, true, true, true) => 0x0407,
            _ => null
        };
    }

    /// <summary>
    ///     Physical key count per PID: the maximum per-interface-type count within the PID class (one key
    ///     contributes exactly one interface per transport it exposes).
    /// </summary>
    private static IReadOnlyDictionary<ushort, int> KeyCountPerPid(IReadOnlyList<RawUsbInterface> raw) =>
        raw.GroupBy(i => i.Pid)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(i => i.Type).Max(byType => byType.Count()));

    private static bool Supports(IYubiKey device, ConnectionType type) =>
        (device.AvailableConnections & type) == type;

    private static string Describe(IReadOnlyList<IYubiKey> devices) =>
        string.Join("; ", devices.Select(d => $"{d.DeviceId}({d.AvailableConnections})"));
}