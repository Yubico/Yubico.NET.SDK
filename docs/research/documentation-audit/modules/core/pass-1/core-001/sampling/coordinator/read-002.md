PROPOSED SAMPLE — no verdict: core-001:unit:468e384d30b9c33b / A / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/RescanScheduler.cs:119–120
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    Interval,


PROPOSED SAMPLE — no verdict: core-001:unit:749973e915170867 / A / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysFaultInjectionTests.cs:774–775
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        Succeed,


PROPOSED SAMPLE — no verdict: core-001:unit:a124a683e6029d5b / A / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyMonitorTests.cs:351–351
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
 EnumerationTimeout 


PROPOSED SAMPLE — no verdict: core-001:unit:1780289b48616248 / A / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:scripts/toolchain/Discovery.cs:49–74
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static string[] DiscoverActiveDocumentationFiles(string repoRoot)
    {
        var rootDocs = Directory.GetFiles(repoRoot, "*.md", SearchOption.TopDirectoryOnly);
        var docsRoot = Directory.Exists(Path.Combine(repoRoot, "docs"))
            ? Directory.GetFiles(Path.Combine(repoRoot, "docs"), "*.md", SearchOption.TopDirectoryOnly)
            : [];
        var docsUsage = GetMarkdownFilesUnder(repoRoot, "docs", "usage");
        var docsTroubleshooting = GetMarkdownFilesUnder(repoRoot, "docs", "troubleshooting");
        var docsArchitecture = GetMarkdownFilesUnder(repoRoot, "docs", "architecture");
        var srcReadmes = Directory.GetFiles(Path.Combine(repoRoot, "src"), "README.md", SearchOption.AllDirectories);
        var srcClaudeDocs = Directory.GetFiles(Path.Combine(repoRoot, "src"), "CLAUDE.md", SearchOption.AllDirectories);

        return [..rootDocs
            .Concat(docsRoot)
            .Concat(docsUsage)
            .Concat(docsTroubleshooting)
            .Concat(docsArchitecture)
            .Concat(srcReadmes)
            .Concat(srcClaudeDocs)
            .Select(p => Path.GetRelativePath(repoRoot, p))
            .Where(p => !IsArchivedOrPlanningDoc(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)];
    }


PROPOSED SAMPLE — no verdict: core-001:unit:2b3cc1caf0dfec3f / A / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Commands/src/HsmAuth/HsmAuthCommands.cs:157–178
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, GlobalSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateHsmAuthSessionAsync();

        var retries = await session.GetManagementKeyRetriesAsync();
        var credentials = await session.ListCredentialsAsync();

        OutputHelpers.WriteHeader("YubiHSM Auth");
        OutputHelpers.WriteKeyValue("Version", session.FirmwareVersion.ToString());
        OutputHelpers.WriteKeyValue("Management Key Retries", retries.ToString());
        OutputHelpers.WriteKeyValue("Stored Credentials", credentials.Count.ToString());

        if (retries <= 3)
        {
            OutputHelpers.WriteWarning("Low retry count! Incorrect attempts will reduce this further.");
        }

        return ExitCode.Success;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:d46b1ef7a44e6284 / A / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Commands/src/HsmAuth/HsmAuthCommands.cs:183–203
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, HsmAuthResetSettings settings, YkDeviceContext deviceContext)
    {
        if (!settings.Force)
        {
            if (!ConfirmationPrompts.ConfirmDestructive(
                    "reset the YubiHSM Auth applet, deleting ALL credentials and resetting the management key"))
            {
                OutputHelpers.WriteInfo("Reset cancelled.");
                return ExitCode.UserCancelled;
            }
        }

        await using var session = await deviceContext.Device.CreateHsmAuthSessionAsync();
        await session.ResetAsync();

        OutputHelpers.WriteSuccess("YubiHSM Auth applet has been reset.");
        return ExitCode.Success;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:05c27da3274a61c3 / A / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Commands/src/Management/ManagementInfoCommand.cs:62–65
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static string FormatCapabilities(DeviceCapabilities capabilities) =>
        capabilities == DeviceCapabilities.None ? "None" : capabilities.ToString();


PROPOSED SAMPLE — no verdict: core-001:unit:2d775717e74d6f32 / A / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Commands/src/Oath/OathCommands.cs:169–182
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OathPasswordSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOathSessionAsync();

        OutputHelpers.WriteHeader("OATH Application");
        OutputHelpers.WriteKeyValue("OATH Version", session.FirmwareVersion.ToString());
        OutputHelpers.WriteBoolValue("Password Protected", session.IsLocked);
        OutputHelpers.WriteKeyValue("Device ID", session.DeviceId);

        return ExitCode.Success;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:3fb51ea489737870 / A / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Commands/src/OpenPgp/OpenPgpAccessCommands.cs:109–134
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, AccessChangePinSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        using var currentPin = GetPin(settings.Pin, "Enter current User PIN");
        using var newPin = GetPin(settings.NewPin, "Enter new User PIN");
        if (currentPin is null || newPin is null)
        {
            OutputHelpers.WriteError("Current and new User PINs are required.");
            return ExitCode.GenericError;
        }

        if (string.IsNullOrEmpty(settings.NewPin) &&
            !PinPrompt.ConfirmMatches(newPin, "Confirm new User PIN"))
        {
            OutputHelpers.WriteError("New PINs do not match.");
            return ExitCode.GenericError;
        }

        await session.ChangePinAsync(currentPin.Memory, newPin.Memory);
        OutputHelpers.WriteSuccess("User PIN has been changed.");
        return ExitCode.Success;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:10653b759a2209f8 / A / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Commands/src/Piv/PivCommands.cs:545–566
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, PivSlotSettings settings, YkDeviceContext deviceContext)
    {
        var slot = PivHelpers.ParseSlot(settings.Slot);

        await using var session = await deviceContext.Device.CreatePivSessionAsync();
        var cert = await session.AttestKeyAsync(slot);

        OutputHelpers.WriteHeader($"Attestation — Slot {PivHelpers.FormatSlot(slot)}");
        OutputHelpers.WriteKeyValue("Subject", cert.Subject);
        OutputHelpers.WriteKeyValue("Issuer", cert.Issuer);
        OutputHelpers.WriteKeyValue("Serial", cert.SerialNumber);
        OutputHelpers.WriteKeyValue("Not Before", cert.NotBefore.ToString("O"));
        OutputHelpers.WriteKeyValue("Not After", cert.NotAfter.ToString("O"));

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine(cert.ExportCertificatePem());

        return ExitCode.Success;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:34cf0693a537f0df / A / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Shared/src/Device/DeviceSelector.cs:24–25
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private readonly Func<CancellationToken, Task<IReadOnlyList<IYubiKey>>> _findAll;


PROPOSED SAMPLE — no verdict: core-001:unit:010596d531cf6d98 / A / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/ProductAtrs.cs:44–53
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    // YubiKey 5 Series
    public static readonly AnswerToReset YubiKey5Usb =
        new(
            [
                0x3B, 0xFD, 0x13, 0x00, 0x00, 0x81, 0x31, 0xFE, 0x15, 0x80, 0x73, 0xC0, 0x21, 0xC0, 0x57, 0x59,
                0x75, 0x62, 0x69, 0x4B, 0x65, 0x79, 0x40
            ]
        );


PROPOSED SAMPLE — no verdict: core-001:unit:d73e12e719d3fff0 / A / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Tests.Shared/YubiKeyTestState.cs:195–197
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private DeviceInfo _deviceInfo;


PROPOSED SAMPLE — no verdict: core-001:unit:7573e895ff24c450 / A / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Tests.Shared/YubiKeyTestState.cs:440–441
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private static readonly object s_lock = new();


PROPOSED SAMPLE — no verdict: core-001:unit:fffbb1e91e795937 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/CompositeDeviceMerger.cs:374–379
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static ConnectionType ObservedConnections(IEnumerable<DeviceInterfaceDescriptor> descriptors) =>
        descriptors.Aggregate(
            ConnectionType.Unknown,
            static (current, descriptor) => current | descriptor.Connection);


PROPOSED SAMPLE — no verdict: core-001:unit:bf32732f3b6ebcda / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/CompositeDeviceMerger.cs:454–462
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static int ConnectionOrder(ConnectionType connection) => connection switch
    {
        ConnectionType.SmartCard => 0,
        ConnectionType.HidFido => 1,
        ConnectionType.HidOtp => 2,
        _ => 3
    };


PROPOSED SAMPLE — no verdict: core-001:unit:60dd6f6febeebdd8 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/ConnectionTypeExtensions.cs:21–24
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private const ConnectionType HidGroup =
        ConnectionType.Hid | ConnectionType.HidFido | ConnectionType.HidOtp;


PROPOSED SAMPLE — no verdict: core-001:unit:896dd543e29befc7 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/DeviceChange.cs:38–38
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public sealed record DeviceRemoved(long Revision, IYubiKey Device) : DeviceChange(Revision);


PROPOSED SAMPLE — no verdict: core-001:unit:a6706ccc51c5b8b2 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/DeviceChangeFeed.cs:28–29
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal sealed class DeviceChangeFeed
{


PROPOSED SAMPLE — no verdict: core-001:unit:0345a4013ca4230b / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/DeviceChangeFeed.cs:37–38
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private bool _completed;


PROPOSED SAMPLE — no verdict: core-001:unit:25aa4af785e881de / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/DeviceChangeFeed.cs:140–141
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class Subscription(DeviceChangeFeed owner) : IDisposable
    {


PROPOSED SAMPLE — no verdict: core-001:unit:c1935efa8b55d4fd / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/DeviceChangeFeed.cs:168–170
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public void Dispose() => owner.Unsubscribe(this);


PROPOSED SAMPLE — no verdict: core-001:unit:441ecd53f2034d3a / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/DeviceInfoReader.cs:109–130
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static async Task<DisposableTlvList> ReadPageAsync(
        IProtocol protocol,
        byte page,
        CancellationToken cancellationToken)
    {
        var encodedResult = await ReadRawPageAsync(protocol, page, cancellationToken).ConfigureAwait(false);

        if (encodedResult.Length < 1)
            throw new BadResponseException($"Empty response for page {page}");

        var declaredLength = encodedResult[0];
        var actualLength = encodedResult.Length - 1;
        if (actualLength != declaredLength)
        {
            throw new BadResponseException(
                $"Invalid device info length for page {page}: declared {declaredLength}, actual {actualLength}.");
        }

        return TlvHelper.DecodeList(encodedResult.AsSpan()[1..]);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:e321afb584ee7490 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/DeviceInfoReader.cs:159–174
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static async Task<byte[]> ReadFidoPageAsync(
        IFidoHidProtocol protocol,
        byte page,
        CancellationToken cancellationToken)
    {
        var pagePayload = new byte[] { page };
        var response = await protocol.SendCommandAsync(
                CtapReadConfig,
                pagePayload,
                UserPresenceNotification.None,
                cancellationToken)
            .ConfigureAwait(false);
        return response.ToArray();
    }


PROPOSED SAMPLE — no verdict: core-001:unit:461949ca0e95ac25 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/DiscoveryBudgets.cs:25–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    internal static readonly TimeSpan IdentityRead = TimeSpan.FromSeconds(2);


PROPOSED SAMPLE — no verdict: core-001:unit:1577e5d955f076e7 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/DiscoveryDeviceInfoReader.cs:90–130
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static async Task<DeviceInfo> ReadBoundedCoreAsync(
        string interfaceId,
        string deviceId,
        IDiscoveryConnectionProvider? provider,
        ConnectionType connection,
        TimeSpan budget,
        ILogger logger,
        CancellationToken cancellationToken,
        bool waitForWorkerSlot,
        DiscoveryReadScope scope)
    {
        // One atomic epoch capture: the key's generation and the supersede token used while queued are
        // guaranteed to belong to the same hotplug epoch, so a read created against an already-retired
        // epoch always holds a token that has been (or is about to be) cancelled.
        if (!scope.TryCaptureEpoch(out var epoch))
            throw new DiscoveryReadKey(interfaceId, connection, Generation: -1).Superseded();

        var key = new DiscoveryReadKey(interfaceId, connection, epoch.Generation);
        var sharedRead = scope.GetOrAdd(
            key,
            _ => new SharedDeviceInfoRead(scope, key, epoch, deviceId, provider, connection, logger, waitForWorkerSlot));

        try
        {
            return await sharedRead.Task.WaitAsync(budget, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            if (!sharedRead.Task.IsCompleted)
                sharedRead.RecordAbandonment();
            throw;
        }
        catch (OperationCanceledException)
        {
            if (!sharedRead.Task.IsCompleted)
                sharedRead.RecordAbandonment();
            throw;
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:01a6cfa0b7c38940 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/DiscoveryReadAdmission.cs:22–23
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private static readonly Lock AdmissionLock = new();


PROPOSED SAMPLE — no verdict: core-001:unit:55cd5be1cd4ebf76 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/DiscoveryReadAdmission.cs:31–33
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private DiscoveryReadAdmission(IReadOnlyList<SemaphoreSlim> gates) => _gates = gates;


PROPOSED SAMPLE — no verdict: core-001:unit:8347f3486ba80697 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/DiscoveryWorkerAdmission.cs:22–25
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static readonly SemaphoreSlim Slots =
        new(MaximumConcurrentWorkers, MaximumConcurrentWorkers);


PROPOSED SAMPLE — no verdict: core-001:unit:353f089f75aa1ae7 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/FindYubiKeys.cs:42–43
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<FindYubiKeys>();


PROPOSED SAMPLE — no verdict: core-001:unit:3582bbf6b676e34c / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/FirmwareVersion.cs:263–265
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public override string ToString() => $"{Major}.{Minor}.{Patch}";


PROPOSED SAMPLE — no verdict: core-001:unit:52bcea037a7afb20 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/FirmwareVersion.cs:268–269
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    internal static readonly FirmwareVersion V4_3_0 = new(4, 3);


PROPOSED SAMPLE — no verdict: core-001:unit:942a063a6ba4b64c / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/RescanScheduler.cs:21–22
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal sealed class RescanScheduler
{


PROPOSED SAMPLE — no verdict: core-001:unit:1aa9379b97a656a3 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/RescanScheduler.cs:27–29
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private readonly ChannelReader<ConnectionType> _signals;


PROPOSED SAMPLE — no verdict: core-001:unit:a8576919fc5ca1ac / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/RescanScheduler.cs:29–30
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private readonly TimeSpan _interval;


PROPOSED SAMPLE — no verdict: core-001:unit:704bec4f7f9cbcc7 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/RescanScheduler.cs:117–118
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal enum RescanReason
{


PROPOSED SAMPLE — no verdict: core-001:unit:bc1ab2f42466f2f4 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/SharedDeviceInfoRead.cs:246–293
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static async Task<DeviceInfo> ConnectAndReadAsync(
        DiscoveryReadScope scope,
        DiscoveryReadKey key,
        TransportEpoch epoch,
        IDiscoveryConnectionProvider? provider,
        DiscoveryReadLeaseTracker leaseTracker,
        DiscoveryReadAdmission? nativeAdmission,
        CancellationToken cancellationToken)
    {
        var interfaceId = key.InterfaceId;
        if (provider is null)
            throw new DiscoveryReadSkippedException(interfaceId, DiscoveryReadSkipCause.NoDiscoveryProvider);

        var discoveryLease = DeviceConnectionRegistry.TryAcquireDiscovery(interfaceId, nativeAdmission);
        if (discoveryLease is null)
            throw new DiscoveryReadSkippedException(interfaceId, DiscoveryReadSkipCause.InterfaceLeaseHeld);
        nativeAdmission?.TransferToLease();
        leaseTracker.Register(discoveryLease);
        var registrationTransferred = false;
        try
        {
            // Final supersession check, as close to the hardware open as possible: a superseded read must not
            // START a new open against hardware its evidence no longer names.
            ThrowIfSuperseded(scope, key, epoch);

            // Discovery creates and disposes this connection. Built-in PC/SC and macOS/Linux HID owners
            // take the exact discovery registration before native open, including failed/late opens.
            // Resolve the published composite's selected member just as for a direct slot read. Set transfer
            // before invoking the owner: a throw or disposal diagnostic is not permission to release twice.
            var pcscSlot = ResolvePcscSlot(provider, key.Connection);
            var hidSlot = ResolveOwnedHidSlot(provider, key.Connection);
            registrationTransferred = pcscSlot is not null || hidSlot is not null;
            var conn = pcscSlot is not null
                ? await pcscSlot.OpenRegisteredConnectionAsync(discoveryLease, cancellationToken).ConfigureAwait(false)
                : hidSlot is not null
                    ? await hidSlot.OpenRegisteredConnectionAsync(discoveryLease, cancellationToken).ConfigureAwait(false)
                    : await provider.ConnectForDiscoveryAsync(key.Connection, cancellationToken).ConfigureAwait(false);
            await using (conn.ConfigureAwait(false))
                return await ProtocolDeviceInfo.ReadAsync(conn, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (!registrationTransferred)
                discoveryLease.Dispose();
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:06b9b030214228fd / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/WindowsTopologyNativeOps.cs:33–34
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal sealed class WindowsTopologyNativeOps : IWindowsTopologyNativeOps
{


PROPOSED SAMPLE — no verdict: core-001:unit:dc379bca80113a80 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/WindowsTopologyNativeOps.cs:94–109
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static bool TryReadContainerId(Func<CmDevice> deviceFactory, out Guid containerId)
    {
        containerId = Guid.Empty;
        try
        {
            containerId = deviceFactory().ContainerId;
            return containerId != Guid.Empty;
        }
        catch (Exception e) when (e is KeyNotFoundException or PlatformApiException or InvalidOperationException)
        {
            // Missing ContainerId property, stale devnode, or a malformed path: unknown topology.
            return false;
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:9c98c412616de662 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/YubiKeyDevice.cs:235–256
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    async Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
        ConnectionType connection,
        CancellationToken cancellationToken)
    {
        if (!TryResolveSlot(connection, out var slot))
        {
            throw new NotSupportedException(
                $"Connection type {connection} is not available on this physical YubiKey " +
                $"(available connections: {AvailableConnections}).");
        }

        try
        {
            return await slot.OpenRawConnectionAsync(connection, cancellationToken).ConfigureAwait(false);
        }
        catch (NonOpenableConnectionSlotException)
        {
            throw new DiscoveryReadSkippedException(slot.InterfaceId, DiscoveryReadSkipCause.NoDiscoveryProvider);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:e6ca5d7aa8b2f21d / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/YubiKeyMonitor.cs:41–43
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private readonly DeviceChangeFeed _feed = new();


PROPOSED SAMPLE — no verdict: core-001:unit:af296d4153432a33 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/YubiKeyMonitor.cs:44–45
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private readonly HotplugSignals _signals;


PROPOSED SAMPLE — no verdict: core-001:unit:3fed58fa1f83f08d / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/YubiKeyMonitor.cs:53–64
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private YubiKeyMonitor(
        IMonitoredDiscovery discovery,
        IReadOnlyList<ListenerSource> listeners,
        TimeSpan interval,
        TimeProvider timeProvider)
    {
        _discovery = discovery;
        _signals = new HotplugSignals(listeners, Logger);
        _scheduler = new RescanScheduler(_signals.Signals, interval, timeProvider);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:ce4ac427cb0cef28 / A / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/YubiKeyMonitor.cs:81–89
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    internal static Task<YubiKeyMonitor> CreateAsync(CancellationToken cancellationToken) =>
        CreateAsync(
            FindYubiKeys.Create(),
            [new(ConnectionType.Hid, HidDeviceListener.Create), new(ConnectionType.SmartCard, DesktopSmartCardDeviceListener.Create)],
            DefaultRescanInterval,
            TimeProvider.System,
            cancellationToken);


PROPOSED SAMPLE — no verdict: core-001:unit:22ce88df9daddd31 / A / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/architecture/connection-ownership-and-contention.md:115–140
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Discovery coordination

Discovery leases remain per-interface and nonblocking:

- discovery on any claimed member is refused while a grouped connection is live;
- a connection may wait cancellably for an active discovery read;
- while waiting, its earlier member claims prevent later discovery from overtaking it;
- cancellation or failure rolls back those earlier claims;
- discovery uses `IDiscoveryConnectionProvider`, bypassing public connection registration while it
  already owns the discovery lease.

Idle ownership records remain for the process lifetime to avoid unsafe remove/recreate races. Their
count is bounded by unique interface IDs observed.

Separately, discovery's native-read admission serializes potentially sibling USB probes by PID before
they request a worker or interface lease. It covers both identity and metadata reads, including connection
disposal, across finders and evidence epochs. An unknown USB PID conservatively acquires all known PID
gates atomically. Pending requests reserve no gates and await release notifications, so an unknown request
blocked by one PID does not prevent an available different PID from progressing. These fixed gates are
scheduling only: they contain no metadata and add no public connection claims.
Once an interface lease is acquired, it owns this admission until native release; an abandoned waiter or
retired scope cannot release it. Built-in native owners that cannot prove teardown retain it with their
quarantined interface claim. See [native discovery scheduling](device-discovery-guarantees.md#native-discovery-scheduling)
for the throughput tradeoff and pinning tests. Direct raw reads outside the finder and caller-owned public
connections do not acquire these discovery PID gates; the grouping bound above remains unchanged.




PROPOSED SAMPLE — no verdict: core-001:unit:cf1e2081af8b8ac4 / A / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/architecture/event-driven-device-discovery.md:124–149
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Monitor lifecycle and disposal

Creation starts listeners, launches the loop, and awaits its first publication. Empty success is ready.
First-scan failure fails fast with the original error; initialization failure or cancellation disposes
partial ownership. The creation token is independent of the monitor lifetime after return.

Later scan/publication exceptions are logged and retried on the next trigger. Discovery failures before
publication retain the last membership. Unexpected failures outside that retry path fault the feed before
best-effort diagnostic logging. A new subscription does not restart a failed monitor: dispose and create
a new instance. There is no pause, restart, manual rescan, interval replacement, or monitor generation.

Disposal is idempotent and strictly sequential:

1. Clear callbacks, stop all listeners, dispose all listeners, and complete the signal channel.
2. Cancel the lifetime token and await the actual scan loop, including any in-flight scan/publication.
3. Dispose the lifetime token source and retire discovery.
4. Complete the change feed.

There is no managed-loop abandonment or disposal timeout. In-flight scan waits are bounded by the
discovery budgets; listener cleanup and total disposal are not a hard wall-clock promise. No producer
can publish after feed completion because the loop has already ended. Caller-owned connections and
sessions are not disposed. Concurrent `WatchAsync` enumerations are supported, and `DisposeAsync` may
overlap them: it ends each active enumeration normally. `DisposeAsync` must not be called concurrently
with itself. Applications that want consumers to observe a final state before shutdown do so on their
own terms; disposal does not wait for consumers.




PROPOSED SAMPLE — no verdict: core-001:unit:a84e2cc173ca0753 / A / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/architecture/event-driven-device-discovery.md:150–165
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
### Finder evidence epochs are not monitor generations

Hotplug invalidation atomically replaces the whole identity/metadata evidence epoch and retires its
read scope, even when only one transport signalled. Old native completion can write only into unreachable
old caches; retaining sibling evidence could mix two physical keys. Scan-observed absence also evicts
evidence. Without either hint or observed absence, same-interface substitution can remain unknowable.

Read-scope retirement cancels queued admission and prevents later opens. Already-open reads retain
actual worker admission and registry leases until native teardown. Sibling admission, same-product
serialization, and unknown-product atomic reservation remain; a pending unknown-product request
reserves no gates. Neither a wait budget nor scope retirement proves native release. See
[native discovery scheduling](device-discovery-guarantees.md#native-discovery-scheduling).

`SnapshotDiffTests` pins retention/replacement and metadata changes; `DeviceChangeFeedTests` pins replay,
ordering, revisions, overflow, and termination. Monitor, scheduler, and hotplug tests cover initialization,
sequential scans, timing, fallback, and disposal; enumeration-budget tests cover partial results and late work.



PROPOSED SAMPLE — no verdict: core-001:unit:cb2232cff0d50f0d / A / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/migration/v1-to-v2-map.yml:72–83
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
  - id: device-discovery-model
    status: manual
    confidence: medium
    migration_kind: behavior
    v1: v1 device discovery and device access APIs
    v2: Yubico.YubiKit.Core device repository, device, connection, and protocol abstractions
    evidence:
      - develop:Yubico.YubiKey/src/Yubico/YubiKey/YubiKeyDevice.Static.cs
      - develop:Yubico.YubiKey/src/Yubico/YubiKey/IYubiKeyDevice.cs
      - yubikit:src/Core/
    notes: "Review discovery, transport selection, and ownership at call sites. Every YubiKeyManager.Find…Async performs fresh independent one-shot discovery; interfaces restricts enumeration/probes, not just results. Preserve All plus local filtering where later operations need composite slots. YubiKeyManager.WatchAsync owns a private monitor per enumeration; CreateMonitorAsync returns an initialized shared monitor to dispose with await using. Build membership from DeviceAdded replay through SnapshotComplete, then live DeviceChange records, or use FindAllAsync for a one-off. No process-global cache remains. CachedDeviceInfo is last-known metadata; Management remains the live-read API."
    last_reviewed_commit: 7c534e8b7b8ad8cf53b964f972cd9a45e9ebd857



PROPOSED SAMPLE — no verdict: core-001:unit:b77ef6f0677fffbf / A / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/migration/v1-to-v2-map.yml:596–608
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
  - id: core-find-first-discovery-helpers
    status: assisted
    confidence: high
    migration_kind: member
    v1: YubiKeyDevice.FindAll().First() / YubiKeyDevice.FindByTransport(...).First()
    v2: YubiKeyManager.FindFirstAsync(predicate, interfaces, cancellationToken) / YubiKeyManager.FindFirstOrDefaultAsync(predicate, interfaces, cancellationToken)
    evidence:
      - yubikit:src/Core/src/Devices/YubiKeyManager.cs
      - yubikit:src/Core/src/Devices/FindYubiKeys.cs
      - yubikit:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyManagerStaticTests.cs
      - yubikit:docs/usage/device-discovery.md
    notes: "FindFirstAsync returns the first matching IYubiKey or throws InvalidOperationException; FindFirstOrDefaultAsync returns null for no match. Both perform one independent fresh FindAllAsync operation with the same interfaces scope, cancellation, and exception propagation; they never wait for insertion or use stable ordering. Predicates are optional. Token-only forwarding overloads were removed; use cancellationToken: named arguments. This supersedes devices.First() for single-device callers. Remove forceRescan and ScanAsync; use FindAllAsync for fresh discovery or build a projection from monitor replay through SnapshotComplete and the live tail."
    last_reviewed_commit: 7c534e8b7b8ad8cf53b964f972cd9a45e9ebd857



PROPOSED SAMPLE — no verdict: core-001:unit:b97af2e9d8692339 / A / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/migration/v1-to-v2.md:232–266
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
### Device Discovery

V1 discovery returned v1 device objects synchronously:

```csharp
using Yubico.YubiKey;

var devices = YubiKeyDevice.FindByTransport(Transport.All).ToList();
var device = devices.First();
```

V2 discovery is async and returns physical `IYubiKey` instances with explicit available connections:

```csharp
using Yubico.YubiKit.Core.Devices;

var devices = await YubiKeyManager.FindAllAsync(
    ConnectionType.All,
    cancellationToken: cancellationToken);
var device = devices.First();
```

Migration notes:

- Replace synchronous enumeration assumptions with async flow and cancellation support.
- Every Find uses fresh per-call evidence, without global cache or monitoring. `interfaces` restricts
  enumeration/probes, not just results; discover All and filter locally to preserve composite slots.
- Earlier v2 prerelease users: replace `ScanAsync` with `FindAllAsync(interfaces: ...)`, remove `forceRescan`,
  and replace static monitoring/shutdown with `await using var monitor = await YubiKeyManager.CreateMonitorAsync(ct)`.
  Build membership from `monitor.WatchAsync(ct)` replay through `SnapshotComplete`, then apply live changes;
  use `FindAllAsync` for a one-off.
- V2 models one physical YubiKey with one or more available connections. Avoid assuming that the discovered object is a single transport handle.
- If v1 code filtered by `Transport.HidFido`, `Transport.UsbSmartCard`, or NFC-specific behavior, review the v2 `ConnectionType` choice rather than applying a mechanical enum rename.
- For the common "just get one matching device" case, prefer `YubiKeyManager.FindFirstAsync(predicate, cancellationToken: ct)` (throws `InvalidOperationException` when none match) or `FindFirstOrDefaultAsync(...)` (returns `null`) over `FindAllAsync(...).First()`. Use named cancellation arguments; token-only overloads were removed. See `core-find-first-discovery-helpers` in `v1-to-v2-map.yml` and `docs/usage/device-discovery.md`.




PROPOSED SAMPLE — no verdict: core-001:unit:689d848fe4e3b5fb / A / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/usage/device-discovery.md:24–33
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
### Manager methods

| Method | Description |
|--------|-------------|
| `FindAllAsync(interfaces: All, cancellationToken)` | Fresh independent discovery, limited to selected interfaces |
| `FindFirstAsync(predicate, interfaces: All, cancellationToken)` | Discover once and select the first match, or throw `InvalidOperationException` |
| `FindFirstOrDefaultAsync(predicate, interfaces: All, cancellationToken)` | Discover once and select the first match, or return `null` |
| `WatchAsync(cancellationToken)` | Create a private monitor per enumeration; replay, completion marker, then live changes |
| `CreateMonitorAsync(cancellationToken)` | Create a shared monitor; await initial publication, including empty success |




PROPOSED SAMPLE — no verdict: core-001:unit:c7ac3ec8651f40bf / A / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/usage/device-discovery.md:34–40
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
### Monitor methods

| Method | Description |
|--------|-------------|
| `monitor.WatchAsync(cancellationToken)` | Subscribe to the existing producer: `DeviceAdded` replay, `SnapshotComplete`, then live changes |
| `monitor.DisposeAsync()` | Stop and dispose this owned lifetime; normally use `await using` |




PROPOSED SAMPLE — no verdict: core-001:unit:f799935177b672e5 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Device/ConnectionTypeFormatterTests.cs:46–47
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class FakeYubiKey : IYubiKey
    {


PROPOSED SAMPLE — no verdict: core-001:unit:ae6fac7719619268 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Device/ConnectionTypeFormatterTests.cs:48–49
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public ConnectionType AvailableConnections => ConnectionType.SmartCard | ConnectionType.HidFido;


PROPOSED SAMPLE — no verdict: core-001:unit:7ea29e97c9a024e8 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Devices/CompositeDiscoveryRetentionTests.cs:1–20
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

using Xunit.Sdk;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

/// <summary>Hardware-free checks of the monitor hardware test's actual retention expectation.</summary>



PROPOSED SAMPLE — no verdict: core-001:unit:bf27ed1d1811391d / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Devices/CompositeDiscoveryRetentionTests.cs:21–22
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public class CompositeDiscoveryRetentionTests
{


PROPOSED SAMPLE — no verdict: core-001:unit:6d7b5fd78fd2e972 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Devices/CompositeDiscoveryRetentionTests.cs:150–151
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public string InterfaceId => id;


PROPOSED SAMPLE — no verdict: core-001:unit:d2b1387699593935 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Devices/DeviceEventHotPlugTests.cs:167–168
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private TaskCompletionSource<DeviceChange>? _pending;


PROPOSED SAMPLE — no verdict: core-001:unit:a9ee526a01572633 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Devices/DeviceEventHotPlugTests.cs:262–275
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        /// <summary>Full timeline, attached to any failure so a manual run leaves a usable trace.</summary>
        public override string ToString()
        {
            lock (_gate)
            {
                var lines = _entries.Select(e =>
                    $"  {e.At.TotalSeconds,7:0.000}s  {e.Event}");

                return $"Device event timeline ({_entries.Count} events):{Environment.NewLine}" +
                       string.Join(Environment.NewLine, lines);
            }
        }


PROPOSED SAMPLE — no verdict: core-001:unit:d8900ebff4499887 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Devices/DiscoveryUnattendedTests.cs:31–32
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public class DiscoveryUnattendedTests(ITestOutputHelper output)
{
