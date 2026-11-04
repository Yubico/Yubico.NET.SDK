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

using Xunit.Abstractions;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Management.IntegrationTests;

/// <summary>
///     Read-only public ScanAsync/CachedDeviceInfo coverage on idle, serial-bearing USB fixtures.
///     Tests every attached allow-listed key, not just WithYubiKey's first matching device.
/// </summary>
/// <remarks>
///     Does not inherit IntegrationTestBase: that base starts monitoring, which can probe disjoint
///     siblings while a scoped connection is live. All scans finish before any live session opens;
///     sessions and connections are sequential, including across disjoint views of the same key.
///     No reset, configuration, credentials, touch, or reinsertion. Management is referenced here,
///     not by Core, solely for independent live-read comparisons.
/// </remarks>
[Trait(TestCategories.Category, TestCategories.RequiresHardware)]
public class ScopedDiscoveryReadOnlyTests : IAsyncLifetime
{
    private const int MaxAttempts = 3;
    private static readonly ConnectionType[] ConcreteTypes =
        [ConnectionType.SmartCard, ConnectionType.HidFido, ConnectionType.HidOtp];
    private readonly ITestOutputHelper _output;
    private readonly AllowList _allowList;

    public ScopedDiscoveryReadOnlyTests(ITestOutputHelper output)
        : this(output, new AllowList(new AppSettingsAllowListProvider()))
    {
    }

    internal ScopedDiscoveryReadOnlyTests(ITestOutputHelper output, AllowList allowList)
    {
        _output = output;
        _allowList = allowList;
    }

    public Task InitializeAsync() => YubiKeyManager.ShutdownAsync();
    public Task DisposeAsync() => YubiKeyManager.ShutdownAsync();

    [SkippableTheory]
    [InlineData(ConnectionType.SmartCard)]
    [InlineData(ConnectionType.HidFido)]
    [InlineData(ConnectionType.HidOtp)]
    [InlineData(ConnectionType.Hid)]
    [InlineData(ConnectionType.SmartCard | ConnectionType.HidOtp)]
    [InlineData(ConnectionType.All)]
    public async Task ScanAsync_ScopedViewsCorrelateWithFullScanAndSequentialLiveReads(ConnectionType scope)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var token = timeout.Token;
        var reference = await ReferenceScanAsync(token);
        var concreteScope = scope == ConnectionType.Hid
            ? ConnectionType.HidFido | ConnectionType.HidOtp
            : scope & (ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp);
        var expected = ConnectionsBySerial(reference)
            .Where(pair => (pair.Value & concreteScope) != 0)
            .ToDictionary(pair => pair.Key, pair => pair.Value & concreteScope);
        Skip.If(expected.Count == 0, $"No authorized fixture exposes {scope}.");

        var scanned = await ScanUntilReadyAsync(scope, scan => expected.All(pair =>
            ConnectionsBySerial(Authorized(scan)).GetValueOrDefault(pair.Key) == pair.Value), token);
        var scoped = Authorized(scanned);
        Assert.Equal(expected.OrderBy(pair => pair.Key), ConnectionsBySerial(scoped).OrderBy(pair => pair.Key));

        foreach (var view in scoped)
        {
            var cached = Assert.IsType<DeviceInfo>(view.CachedDeviceInfo);
            foreach (var other in reference)
            {
                Assert.Equal(view.SerialNumber == other.SerialNumber ? DeviceCorrelation.Same : DeviceCorrelation.Different,
                    view.SameDeviceAs(other));
            }

            var baseline = reference.First(other => other.SerialNumber == view.SerialNumber);
            AssertMetadataMatches(Assert.IsType<DeviceInfo>(baseline.CachedDeviceInfo), cached);
            foreach (var type in ConcreteTypes.Where(view.SupportsConnection))
            {
                await AssertLiveReadAsync(view, type, cached, token);
            }
        }

        Assert.False(YubiKeyManager.IsMonitoring);
        _output.WriteLine($"Validated {scope}: {expected.Count} authorized physical key(s), {scoped.Count} view(s).");
    }

    [SkippableFact]
    public async Task ScanAsync_DefaultAndExplicitAllKeepIndependentViewsAndCachedSnapshots()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var first = await ReferenceScanAsync(timeout.Token); // Omitted scope exercises the public default.
        var snapshots = first.Select(view => view.CachedDeviceInfo).ToList();
        var second = Authorized(await ScanUntilReadyAsync(ConnectionType.All, scan =>
            ConnectionsBySerial(Authorized(scan)).OrderBy(pair => pair.Key)
                .SequenceEqual(ConnectionsBySerial(first).OrderBy(pair => pair.Key)), timeout.Token));

        foreach (var view in first)
        {
            var sameKey = second.Where(other => other.SerialNumber == view.SerialNumber).ToList();
            Assert.NotEmpty(sameKey);
            Assert.All(sameKey, other =>
            {
                Assert.NotSame(view, other);
                Assert.Equal(DeviceCorrelation.Same, view.SameDeviceAs(other));
                AssertMetadataMatches(Assert.IsType<DeviceInfo>(view.CachedDeviceInfo), Assert.IsType<DeviceInfo>(other.CachedDeviceInfo));
            });
        }

        Assert.Equal(snapshots, first.Select(view => view.CachedDeviceInfo));
        Assert.False(YubiKeyManager.IsMonitoring);
        _output.WriteLine($"Default and explicit All agree for {ConnectionsBySerial(first).Count} authorized physical key(s).");
    }

    internal async Task<IReadOnlyList<IYubiKey>> ReferenceScanAsync(
        CancellationToken token, Func<Task<IReadOnlyList<IYubiKey>>>? scanAsync = null)
    {
        var reference = Authorized(await ScanUntilReadyAsync(null, _ => true, token, scanAsync));
        Assert.NotEmpty(reference);
        return reference;
    }

    private IReadOnlyList<IYubiKey> Authorized(IReadOnlyList<IYubiKey> scan) =>
        scan.Where(view => _allowList.IsDeviceAllowed(view.SerialNumber)).ToList();

    private static Dictionary<int, ConnectionType> ConnectionsBySerial(IReadOnlyList<IYubiKey> views) =>
        views.GroupBy(view => Assert.IsType<int>(view.SerialNumber))
            .ToDictionary(group => group.Key, group => group.Aggregate(ConnectionType.Unknown, (all, view) => all | view.AvailableConnections));

    private async Task<IReadOnlyList<IYubiKey>> ScanUntilReadyAsync(
        ConnectionType? scope, Func<IReadOnlyList<IYubiKey>, bool> ready, CancellationToken token,
        Func<Task<IReadOnlyList<IYubiKey>>>? scanAsync = null)
    {
        IReadOnlyList<IYubiKey> scan = [];
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            scan = await TransientScanRetry.ScanAsync(scanAsync ?? (() => scope is { } requested
                ? YubiKeyManager.ScanAsync(requested, token)
                : YubiKeyManager.ScanAsync(cancellationToken: token)));
            Assert.Equal(scan.Count, scan.Select(view => view.DeviceId).Distinct(StringComparer.Ordinal).Count());
            var concrete = scope == ConnectionType.Hid
                ? ConnectionType.HidFido | ConnectionType.HidOtp
                : (scope ?? ConnectionType.All) & (ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp);
            Assert.All(scan, view =>
            {
                Assert.NotEqual(ConnectionType.Unknown, view.AvailableConnections);
                Assert.Equal(ConnectionType.Unknown, view.AvailableConnections & ~concrete);
            });
            // A complete Security Key snapshot may have no serial. Missing metadata must still block:
            // an unidentified view could be a contended authorized fixture, not an unrelated device.
            if (scan.Count > 0 && scan.All(view => view.CachedDeviceInfo is not null) && ready(scan))
                return scan;

            _output.WriteLine($"{scope ?? ConnectionType.All} attempt {attempt}: {scan.Count} view(s), " +
                $"{scan.Count(view => view.CachedDeviceInfo is null)} without metadata; fixture not ready (contention is possible).");
            if (attempt < MaxAttempts)
                await Task.Delay(TimeSpan.FromMilliseconds(250), token);
        }

        Skip.If(scope is null && scan.Count == 0, "No USB fixture found after bounded scan attempts.");
        Assert.Fail("Incomplete metadata or missing expected interfaces after three scans. " +
            "Requires idle, serial-bearing USB fixtures; check external contention before diagnosing a discovery regression.");
        return scan;
    }

    private async Task AssertLiveReadAsync(IYubiKey view, ConnectionType type, DeviceInfo cached, CancellationToken token)
    {
        // The helper returns only after disposing both the borrowed session and its connection.
        // Never scan or connect through a different scoped view while either is held.
        await using var connection = await ConnectWithRetryAsync(view, type, token);
        Assert.Equal(type, connection.Type);
        await using var session = await ManagementSession.CreateAsync(connection, cancellationToken: token);
        Assert.Equal(type, session.ConnectionType);
        var live = await session.GetDeviceInfoAsync(token);
        Assert.Equal(view.SerialNumber, live.SerialNumber);
        AssertMetadataMatches(cached, live);
        AssertMetadataMatches(cached, Assert.IsType<DeviceInfo>(view.CachedDeviceInfo));
        _output.WriteLine($"{type}: cached/live firmware {live.FirmwareVersion}, {live.FormFactor}, USB {live.UsbEnabled}; serial correlation passed.");
    }

    private async Task<IConnection> ConnectWithRetryAsync(IYubiKey view, ConnectionType type, CancellationToken token)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return type switch
                {
                    ConnectionType.SmartCard => await view.ConnectAsync<ISmartCardConnection>(token),
                    ConnectionType.HidFido => await view.ConnectAsync<IFidoHidConnection>(token),
                    ConnectionType.HidOtp => await view.ConnectAsync<IOtpHidConnection>(token),
                    _ => throw new InvalidOperationException("The test must request a concrete connection.")
                };
            }
            catch (ConnectionInUseException) when (attempt < MaxAttempts)
            {
                _output.WriteLine($"{type} connect attempt {attempt}: ConnectionInUseException; bounded retry.");
                await Task.Delay(TimeSpan.FromMilliseconds(250), token);
            }
        }
    }

    private static void AssertMetadataMatches(DeviceInfo expected, DeviceInfo actual)
    {
        // Compare semantic fields, not record equality over independently-owned ReadOnlyMemory values.
        Assert.Equal(expected.SerialNumber, actual.SerialNumber);
        Assert.Equal(expected.FirmwareVersion, actual.FirmwareVersion);
        Assert.Equal(expected.FormFactor, actual.FormFactor);
        Assert.Equal(expected.UsbSupported, actual.UsbSupported);
        Assert.Equal(expected.UsbEnabled, actual.UsbEnabled);
        Assert.Equal(expected.NfcSupported, actual.NfcSupported);
        Assert.Equal(expected.NfcEnabled, actual.NfcEnabled);
    }
}