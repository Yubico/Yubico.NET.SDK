// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Testing;
using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Cli.Shared.UnitTests.Device;

public sealed class DeviceSelectorTests
{
    [Fact]
    public async Task Select_KnownSerialAndCachedLabel_DoesNotReadLiveMetadata()
    {
        var cached = Info(null) with { FormFactor = FormFactor.UsbCKeychain };
        var device = new FakeKey(ConnectionType.SmartCard, 222, cached);
        using var flow = new SelectionFlow([device], serial: 222);

        var selection = await flow.SelectAsync();

        Assert.NotNull(selection);
        Assert.Same(device, selection.Device);
        Assert.Equal(222, selection.SerialNumber);
        Assert.Equal(FormFactor.UsbCKeychain, selection.FormFactor);
        Assert.Equal(cached, selection.Info);
        Assert.Empty(flow.Reads);
    }

    [Fact]
    public async Task Select_KnownNonmatchingSerial_DoesNotProbe()
    {
        using var flow = new SelectionFlow([new FakeKey(ConnectionType.SmartCard, 111)], serial: 222);

        Assert.Null(await flow.SelectAsync());
        Assert.Empty(flow.Reads);
        Assert.Equal(1, flow.Discoveries);
        Assert.Contains("222", flow.Console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Select_MissingIdentity_ReusesSerialFallbackForSelectedLabel()
    {
        var device = new FakeKey(ConnectionType.SmartCard);
        using var flow = new SelectionFlow([device], serial: 222) { Read = (_, _) => Task.FromResult<DeviceInfo?>(Info(222)) };

        var selection = await flow.SelectAsync();

        Assert.NotNull(selection);
        Assert.Equal(222, selection.SerialNumber);
        Assert.Equal(Info(222), selection.Info);
        Assert.Equal([device], flow.Reads);
    }

    [Fact]
    public async Task Select_CachedSerialLessDevice_DoesNotAssumeSerialWillBecomeAvailable()
    {
        using var flow = new SelectionFlow([new FakeKey(ConnectionType.SmartCard, cached: Info(null))], serial: 222);

        Assert.Null(await flow.SelectAsync());
        Assert.Empty(flow.Reads);
    }

    [Fact]
    public async Task Select_CachedSerialWithoutLatchedIdentity_MatchesWithoutLiveRead()
    {
        var device = new FakeKey(ConnectionType.SmartCard, cached: Info(222));
        using var flow = new SelectionFlow([device], serial: 222);

        var selection = await flow.SelectAsync();

        Assert.NotNull(selection);
        Assert.Same(device, selection.Device);
        Assert.Empty(flow.Reads);
    }

    [Theory]
    [InlineData(ConnectionType.SmartCard)]
    [InlineData(ConnectionType.HidFido)]
    [InlineData(ConnectionType.HidOtp)]
    public async Task Select_RequestedTransport_FiltersOnceAndRetainsCompleteDevice(ConnectionType requested)
    {
        var excluded = new FakeKey(requested == ConnectionType.SmartCard ? ConnectionType.HidFido : ConnectionType.SmartCard);
        var composite = new FakeKey(ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp, cached: Info(222));
        using var flow = new SelectionFlow([excluded, composite], requested: requested,
            transports: [ConnectionType.SmartCard, ConnectionType.HidFido, ConnectionType.HidOtp]);

        var selection = await flow.SelectAsync();

        Assert.NotNull(selection);
        Assert.Same(composite, selection.Device);
        Assert.Equal(composite.AvailableConnections, selection.ConnectionType);
        Assert.Equal(1, excluded.SupportChecks);
        Assert.Equal(1, composite.SupportChecks);
        Assert.Empty(flow.Reads);
    }

    [Theory]
    [InlineData(NonInteractiveSelection.FirstEligible, ConnectionType.SmartCard, 0)]
    [InlineData(NonInteractiveSelection.PreferFirstTransport, ConnectionType.SmartCard, 1)]
    [InlineData(NonInteractiveSelection.PreferFirstTransport, ConnectionType.HidFido, 2)]
    [InlineData(NonInteractiveSelection.PreferFirstTransport, ConnectionType.HidOtp, 0)]
    public async Task Select_NoninteractiveMultiple_UsesExplicitPolicy(
        NonInteractiveSelection policy, ConnectionType firstTransport, int expected)
    {
        FakeKey[] devices = [new(ConnectionType.HidOtp, cached: Info(1)),
            new(ConnectionType.SmartCard, cached: Info(2)), new(ConnectionType.HidFido, cached: Info(3))];
        using var flow = new SelectionFlow(devices, policy,
            transports: [firstTransport, ConnectionType.SmartCard, ConnectionType.HidFido, ConnectionType.HidOtp]);

        var selection = await flow.SelectAsync();

        Assert.NotNull(selection);
        Assert.Same(devices[expected], selection.Device);
        Assert.Empty(flow.Reads);
    }

    [Fact]
    public async Task Select_PreferredTransportAbsent_UsesFirstEligible()
    {
        var first = new FakeKey(ConnectionType.SmartCard, cached: Info(1));
        using var flow = new SelectionFlow([first, new FakeKey(ConnectionType.SmartCard, cached: Info(2))],
            NonInteractiveSelection.PreferFirstTransport, transports: [ConnectionType.HidFido, ConnectionType.SmartCard]);

        Assert.Same(first, (await flow.SelectAsync())?.Device);
    }

    [Fact]
    public async Task Select_NoninteractiveChoiceRequired_ReturnsNoneWithoutProbingOrPrompting()
    {
        using var flow = new SelectionFlow([new FakeKey(ConnectionType.SmartCard), new FakeKey(ConnectionType.SmartCard)],
            NonInteractiveSelection.RequireChoice);

        Assert.Null(await flow.SelectAsync());
        Assert.Empty(flow.Reads);
        Assert.Contains("explicit selection", flow.Console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Select_NoEligibleDevice_NeverPromptsOrReadsInNoninteractiveMode()
    {
        using var flow = new SelectionFlow([new FakeKey(ConnectionType.HidOtp)]);

        Assert.Null(await flow.SelectAsync());
        Assert.Empty(flow.Reads);
        Assert.Equal(1, flow.Discoveries);
        Assert.Contains("SmartCard", flow.Console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Select_InteractionDisallowedOnTerminal_StillRequiresExplicitChoice()
    {
        using var flow = new SelectionFlow([new FakeKey(ConnectionType.SmartCard), new FakeKey(ConnectionType.SmartCard)],
            NonInteractiveSelection.RequireChoice, interactive: true);

        Assert.Null(await flow.SelectAsync(allowInteraction: false));
        Assert.Empty(flow.Reads);
    }

    [Fact]
    public async Task Select_InteractiveSort_MapsToOriginalDeviceAndRetainsMetadata()
    {
        var secondInMenu = new FakeKey(ConnectionType.SmartCard, cached: Info(222));
        var firstInMenu = new FakeKey(ConnectionType.SmartCard, cached: Info(111));
        using var flow = new SelectionFlow([secondInMenu, firstInMenu], interactive: true);
        flow.Console.Input.PushKey(ConsoleKey.DownArrow);
        flow.Console.Input.PushKey(ConsoleKey.Enter);

        var selection = await flow.SelectAsync();

        Assert.NotNull(selection);
        Assert.Same(secondInMenu, selection.Device);
        Assert.Equal(Info(222), selection.Info);
        Assert.Empty(flow.Reads);
    }

    [Fact]
    public async Task Select_InteractiveIdenticalLabels_CanSelectSecondDevice()
    {
        var first = new FakeKey(ConnectionType.SmartCard, cached: Info(null));
        var second = new FakeKey(ConnectionType.SmartCard, cached: Info(null));
        using var flow = new SelectionFlow([first, second], interactive: true);
        flow.Console.Input.PushKey(ConsoleKey.DownArrow);
        flow.Console.Input.PushKey(ConsoleKey.Enter);

        Assert.Same(second, (await flow.SelectAsync())?.Device);
        Assert.Empty(flow.Reads);
    }

    [Fact]
    public async Task Select_InteractiveCancel_ReturnsNone()
    {
        using var flow = new SelectionFlow([new FakeKey(ConnectionType.SmartCard, cached: Info(1)),
            new FakeKey(ConnectionType.SmartCard, cached: Info(2))], interactive: true);
        flow.Console.Input.PushKey(ConsoleKey.DownArrow);
        flow.Console.Input.PushKey(ConsoleKey.DownArrow);
        flow.Console.Input.PushKey(ConsoleKey.Enter);

        Assert.Null(await flow.SelectAsync());
    }

    [Fact]
    public async Task Select_FallbackReadsForAmbiguousViews_AreSequential()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var flow = new SelectionFlow([new FakeKey(ConnectionType.SmartCard), new FakeKey(ConnectionType.SmartCard)], interactive: true);
        flow.Console.Input.PushKey(ConsoleKey.Enter);
        flow.Read = async (_, token) =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(token);
            return Info(null);
        };

        var selecting = flow.SelectAsync();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Single(flow.Reads);
        }
        finally
        {
            release.TrySetResult();
        }

        Assert.NotNull(await selecting);
        Assert.Equal(2, flow.Reads.Count);
    }

    [Fact]
    public async Task Select_SameSelectorInvokedAgain_DoesNotReuseFallbackMetadata()
    {
        using var flow = new SelectionFlow([new FakeKey(ConnectionType.SmartCard)]);
        int serial = 111;
        flow.Read = (_, _) => Task.FromResult<DeviceInfo?>(Info(serial++));

        Assert.Equal(111, (await flow.SelectAsync())?.SerialNumber);
        Assert.Equal(112, (await flow.SelectAsync())?.SerialNumber);
        Assert.Equal(2, flow.Reads.Count);
    }

    [Fact]
    public async Task Select_FailedFallbackIsBestEffort_AndNotRetainedAcrossInvocations()
    {
        var device = new FakeKey(ConnectionType.SmartCard, 222);
        using var flow = new SelectionFlow([device], serial: 222);
        flow.Read = (_, _) => throw new IOException("Unavailable");

        var first = await flow.SelectAsync();
        Assert.NotNull(first);
        Assert.Equal(222, first.SerialNumber);
        Assert.Null(first.Info);
        Assert.Single(flow.Reads);

        flow.Read = (_, _) => Task.FromResult<DeviceInfo?>(Info(222));
        Assert.Equal(Info(222), (await flow.SelectAsync())?.Info);
        Assert.Equal(2, flow.Reads.Count);
    }

    [Fact]
    public async Task Select_RetrySameUnknownReference_DoesNotRepeatFailedFallback()
    {
        using var flow = new SelectionFlow([new FakeKey(ConnectionType.SmartCard)], serial: 222, interactive: true);
        flow.Read = (_, _) => throw new IOException("Unavailable");
        flow.Console.Input.PushTextWithEnter("y");
        flow.Console.Input.PushTextWithEnter("n");

        Assert.Null(await flow.SelectAsync());
        Assert.Single(flow.Reads);
        Assert.Equal(2, flow.Discoveries);
    }

    [Fact]
    public async Task Select_CancelledBeforeDiscovery_PropagatesWithoutIo()
    {
        using var flow = new SelectionFlow([]);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => flow.Selector.SelectDeviceAsync(cancellation.Token));
        Assert.Equal(0, flow.Discoveries);
    }

    [Fact]
    public async Task Select_CancellationDuringSerialFallback_IsNotSwallowedAsNoMatch()
    {
        using var flow = new SelectionFlow([new FakeKey(ConnectionType.SmartCard)], serial: 222);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        flow.Read = (_, token) =>
        {
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            return Task.FromResult<DeviceInfo?>(null);
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => flow.Selector.SelectDeviceAsync(cancellation.Token));
        Assert.Single(flow.Reads);
        Assert.DoesNotContain("No YubiKey", flow.Console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Select_CancelledFallbackFailsWithIoError_StillPropagatesCancellation()
    {
        using var flow = new SelectionFlow([new FakeKey(ConnectionType.SmartCard, 222)]);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        flow.Read = (_, _) =>
        {
            cancellation.Cancel();
            throw new IOException("Connection closed during cancellation");
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => flow.Selector.SelectDeviceAsync(cancellation.Token));
    }

    [Fact]
    public async Task Select_SameSelectorObservesNewCachedSnapshot_OnNextInvocation()
    {
        var device = new FakeKey(ConnectionType.SmartCard, 222, Info(null));
        using var flow = new SelectionFlow([device]);

        Assert.Equal(FormFactor.UsbAKeychain, (await flow.SelectAsync())?.FormFactor);
        device.CachedDeviceInfo = Info(null) with { FormFactor = FormFactor.UsbCKeychain };
        Assert.Equal(FormFactor.UsbCKeychain, (await flow.SelectAsync())?.FormFactor);
        Assert.Empty(flow.Reads);
    }

    private sealed class SelectionFlow : IDisposable
    {
        public SelectionFlow(IReadOnlyList<IYubiKey> devices,
            NonInteractiveSelection policy = NonInteractiveSelection.FirstEligible, int? serial = null,
            ConnectionType? requested = null, ConnectionType[]? transports = null, bool interactive = false)
        {
            Console.Profile.Capabilities.Interactive = interactive;
            Selector = new DeviceSelector(transports ?? [ConnectionType.SmartCard], policy, Console,
                token =>
                {
                    token.ThrowIfCancellationRequested();
                    Discoveries++;
                    return Task.FromResult(devices);
                }, (device, token) =>
                {
                    Reads.Add(device);
                    return Read(device, token);
                }, serial, requested);
        }

        public TestConsole Console { get; } = new();
        public DeviceSelector Selector { get; }
        public int Discoveries { get; private set; }
        public List<IYubiKey> Reads { get; } = [];
        public Func<IYubiKey, CancellationToken, Task<DeviceInfo?>> Read { get; set; } =
            (_, _) => throw new InvalidOperationException("Unexpected live metadata read");
        public Task<DeviceSelection?> SelectAsync(bool allowInteraction = true) =>
            Selector.SelectDeviceAsync(TestContext.Current.CancellationToken, allowInteraction);
        public void Dispose() => Console.Dispose();
    }

    private sealed class FakeKey(ConnectionType connections, int? serial = null, DeviceInfo? cached = null) : IYubiKey
    {
        public string DeviceId => "opaque";
        public ConnectionType AvailableConnections => connections;
        public int? SerialNumber => serial;
        public DeviceInfo? CachedDeviceInfo { get; set; } = cached;
        public int SupportChecks { get; private set; }
        public bool SupportsConnection(ConnectionType type)
        {
            SupportChecks++;
            return (connections & type) == type;
        }

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection => throw new InvalidOperationException("No hardware in unit tests");
    }

    private static DeviceInfo Info(int? serial) => new()
    {
        IsSky = false, IsFips = false, FormFactor = FormFactor.UsbAKeychain, SerialNumber = serial, IsLocked = false,
        UsbEnabled = DeviceCapabilities.Piv, UsbSupported = DeviceCapabilities.Piv,
        NfcEnabled = DeviceCapabilities.None, NfcSupported = DeviceCapabilities.None,
        ResetBlocked = DeviceCapabilities.None, FipsCapabilities = DeviceCapabilities.None, FipsApproved = DeviceCapabilities.None,
        HasPinComplexity = false, PartNumber = "test-part", IsNfcRestricted = false, AutoEjectTimeout = 0,
        ChallengeResponseTimeout = ReadOnlyMemory<byte>.Empty, DeviceFlags = DeviceFlags.None,
        FirmwareVersion = new FirmwareVersion(5, 8, 0),
        VersionQualifier = new VersionQualifier(new FirmwareVersion(5, 8, 0), VersionQualifierType.Final, 0)
    };
}
