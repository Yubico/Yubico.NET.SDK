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

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

/// <summary>
///     Key-count-agnostic composite-discovery invariants; the guarantees under test are specified in
///     docs/architecture/device-discovery-guarantees.md. Requires one or more dedicated composite USB
///     YubiKeys; no touch / user-presence required. The checks adapt to the enumerated key count, so
///     single-key rigs keep passing while multi-key rigs exercise the same contracts.
/// </summary>
/// <remarks>
///     Expectations use production PC/SC and HID enumerators with a local copy of PID/name policy. This
///     oracle is independent of the merger, not an independent hardware census. The rig must be USB-only
///     (no NFC reader attached), fully enumerated, idle, and free of competing interface owners. Several
///     tests scan every returned key, and the typed-connect test opens every advertised interface directly;
///     those paths do not apply the shared fixture allow list. Attach only dedicated authorized test keys.
///     Serial-correlation cases select expected serials from AuthorizedDevices, after discovery has run.
/// </remarks>
[Trait(TestCategories.Category, TestCategories.Discovery)]
public class CompositeDiscoveryIntegrationTests : IAsyncLifetime
{
    private static readonly ConnectionType[] ConcreteTypes =
    [
        ConnectionType.SmartCard,
        ConnectionType.HidFido,
        ConnectionType.HidOtp
    ];

    public Task InitializeAsync()
    {
        YubiKitLogging.LoggerFactory = LoggerFactory.Create(
            builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task FindAllAsync_Conservation_EveryEnumeratedUsbInterfaceAppearsExactlyOnce()
    {
        // Compare per-type counts from raw enumeration and merged results. Equal counts can still hide
        // an equal-count substitution; member-level attribution is covered by managed merger vectors.
        var raw = await EnumerateRawUsbInterfacesAsync();
        Assert.NotEmpty(raw);

        var devices = await TransientScanRetry.ScanAsync(
            () => YubiKeyManager.FindAllAsync());

        foreach (var type in ConcreteTypes)
        {
            var enumerated = raw.Count(i => i.Type == type);
            var exposed = devices.Count(d => Supports(d, type));
            Assert.True(
                enumerated == exposed,
                $"Conservation violated for {type}: {enumerated} interface(s) enumerated at the USB layer " +
                $"but {exposed} returned device(s) expose the type. Devices: {Describe(devices)}");
        }
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task FindAllAsync_ZeroOrphansWhenIdle_DeviceCountEqualsPhysicalKeyCount()
    {
        // On this fully enumerated idle rig, compare result count with the per-PID maximum interface-type
        // count estimate. The estimate alone cannot identify physical membership under partial visibility.
        var raw = await EnumerateRawUsbInterfacesAsync();
        Assert.NotEmpty(raw);
        var expectedKeyCount = KeyCountPerPid(raw).Values.Sum();

        var devices = await TransientScanRetry.ScanAsync(
            () => YubiKeyManager.FindAllAsync());

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
            () => YubiKeyManager.FindAllAsync());
        var actualShapes = devices.Select(d => d.AvailableConnections).OrderBy(c => (int)c).ToList();

        Assert.True(
            expectedShapes.SequenceEqual(actualShapes),
            "Incomplete grouping: expected interface-set multiset " +
            $"[{string.Join(", ", expectedShapes)}] but discovery returned [{string.Join(", ", actualShapes)}]. " +
            $"Devices: {Describe(devices)}");
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task FindAllAsync_TwoIndependentScans_ReturnStableGroupingWhenIdle()
    {
        // Stability: two independent observations on an idle serial-bearing rig must produce the same
        // grouping — same device count, same interface-set multiset, same device identities.
        var scan1 = await TransientScanRetry.ScanAsync(
            () => YubiKeyManager.FindAllAsync());
        var scan2 = await TransientScanRetry.ScanAsync(
            () => YubiKeyManager.FindAllAsync());

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
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task Monitor_EventuallyPopulatesMetadata_AndRetainsUnchangedGroups()
    {
        // Later identity reads may regroup standalone interfaces. Only unchanged groups must retain
        // their published objects, so observe one owned monitor rather than independent Find views.
        await using var monitor = await YubiKeyManager.CreateMonitorAsync();
        var initial = await MonitorSnapshot.ReadAsync(monitor, CancellationToken.None);
        IReadOnlyList<IYubiKey> devices = initial;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            devices = await MonitorSnapshot.ReadAsync(monitor, CancellationToken.None);
            if (devices.All(device => Assert.IsType<YubiKeyDevice>(device).DeviceInfo is not null))
                break;
            await Task.Delay(TimeSpan.FromSeconds(6));
        }

        AssertMetadataReadyAndStableGroupsRetained(initial, devices);
    }

    internal static void AssertMetadataReadyAndStableGroupsRetained(
        IReadOnlyList<IYubiKey> initial, IReadOnlyList<IYubiKey> devices)
    {
        Assert.NotEmpty(devices);
        Assert.All(devices, device => Assert.NotNull(Assert.IsType<YubiKeyDevice>(device).DeviceInfo));
        // Compare the full member multiset, not device counts: regrouping must not lose or duplicate
        // an interface, even when several initial objects become one composite object.
        Assert.Equal(
            initial.SelectMany(device => Assert.IsType<YubiKeyDevice>(device).InterfaceIds).Order(StringComparer.Ordinal),
            devices.SelectMany(device => Assert.IsType<YubiKeyDevice>(device).InterfaceIds).Order(StringComparer.Ordinal));
        var currentByIdentity = devices.ToDictionary(YubiKeyDevice.PhysicalIdentityKeyFor, StringComparer.Ordinal);
        foreach (var previous in initial)
        {
            if (!currentByIdentity.TryGetValue(YubiKeyDevice.PhysicalIdentityKeyFor(previous), out var current) ||
                previous.AvailableConnections != current.AvailableConnections)
                continue;

            // Different known serials prove substitution even on identical member/connection sets.
            // Learning a formerly unknown serial (or a later serial-less read) does not.
            var serialContradiction = previous.SerialNumber is { } previousSerial &&
                current.SerialNumber is { } currentSerial && previousSerial != currentSerial;
            if (!serialContradiction)
                Assert.Same(previous, current);
        }
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task FindAllAsync_RepeatedScans_EventuallyExposeSerialNumberWithoutASession()
    {
        // IYubiKey.SerialNumber surfaces the discovery-read serial with no session or Management
        // dependency. Assert only the authorized serial-bearing fixtures: other attached devices may
        // legitimately be serial-less and remain null forever.
        var expectedSerials = AuthorizedSerialsOrSkip();
        var devices = await ScanUntilAsync(scan =>
            expectedSerials.All(serial => scan.Any(device => device.SerialNumber == serial)));

        Assert.NotEmpty(devices);
        Assert.All(expectedSerials, serial =>
            Assert.Contains(devices, device => device.SerialNumber == serial));
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task SameDeviceAs_AcrossConsecutiveScans_UsesKnownSerials()
    {
        // Conservative grouping can publish several objects for one physical key. Correlation follows
        // their known serials rather than assuming one published object per key.
        var expectedSerials = AuthorizedSerialsOrSkip();
        var scan1 = await ScanUntilAsync(scan =>
            expectedSerials.All(serial => scan.Any(device => device.SerialNumber == serial)));
        var scan2 = await ScanUntilAsync(scan =>
            expectedSerials.All(serial => scan.Any(device => device.SerialNumber == serial)));
        var known1 = scan1.Where(device => device.SerialNumber is { } serial && expectedSerials.Contains(serial)).ToList();
        var known2 = scan2.Where(device => device.SerialNumber is { } serial && expectedSerials.Contains(serial)).ToList();

        Assert.NotEmpty(known1);
        Assert.NotEmpty(known2);
        Assert.Equal(
            expectedSerials.Order(),
            known1.Select(device => device.SerialNumber!.Value).Distinct().Order());
        Assert.Equal(
            expectedSerials.Order(),
            known2.Select(device => device.SerialNumber!.Value).Distinct().Order());

        foreach (var device in known1)
        {
            foreach (var other in known2)
            {
                var expected = device.SerialNumber == other.SerialNumber
                    ? DeviceCorrelation.Same
                    : DeviceCorrelation.Different;
                Assert.Equal(expected, device.SameDeviceAs(other));
            }
        }
    }

    /// <summary>
    ///     Rescans (bounded attempts) until <paramref name="ready" /> holds, tolerating the four-worker
    ///     metadata budget leaving early scans partially populated on larger rigs.
    /// </summary>
    private static async Task<IReadOnlyList<IYubiKey>> ScanUntilAsync(
        Func<IReadOnlyList<IYubiKey>, bool> ready)
    {
        IReadOnlyList<IYubiKey> devices = [];
        for (var attempt = 0; attempt < 3; attempt++)
        {
            devices = await YubiKeyManager.FindAllAsync();
            if (devices.Count > 0 && ready(devices))
                break;
        }

        return devices;
    }

    private static HashSet<int> AuthorizedSerialsOrSkip()
    {
        var serials = AuthorizedDevices.GetAll()
            .Select(device => device.SerialNumber)
            .OfType<int>()
            .ToHashSet();
        if (serials.Count == 0)
        {
            throw new Xunit.SkipException(
                "This identity contract requires at least one authorized device with a readable serial number.");
        }

        return serials;
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task ConnectAsync_TypedTransports_OnEveryReturnedDevice_Succeed()
    {
        // Every returned device must honor its advertised interface set with a working typed connect
        // (no touch required for opening and closing a connection).
        var devices = await TransientScanRetry.ScanAsync(
            () => YubiKeyManager.FindAllAsync());
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
    // Raw enumeration independent of the merger (using production transport enumerators).
    // ---------------------------------------------------------------------------------------------

    private const short YubicoVendorId = 0x1050;

    // Mirror of ReaderNamePidParser.ExpectedConnectionsForPid (internal to Core): the USB Product ID's
    // advertised interface set. Keeping a local copy separates the shape oracle from merger execution,
    // while retaining a shared PID-policy assumption.
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

        var hidDevices = await FindHidInterfaces.Create().FindAllAsync();

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

    // Local mirror of ReaderNamePidParser: infer a PID from the interface names in a USB reader label.
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