// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.Cli.Commands.Management;
using Yubico.YubiKit.Cli.Commands.OpenPgp;
using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Cli.Commands.UnitTests.Infrastructure;

[CollectionDefinition("Command console", DisableParallelization = true)]
public sealed class CommandConsoleCollection;

[Collection("Command console")]
public sealed class CommandMetadataTests
{
    private static readonly CommandContext Context = new([], new EmptyArguments(), "test", null);

    [Theory]
    [InlineData("att", null, 0, ExitCode.GenericError)]
    [InlineData("sig", "invalid", 0, ExitCode.GenericError)]
    [InlineData("sig", null, 1, ExitCode.UserCancelled)]
    public async Task Attest_LocalRejectionOrDeclinedPin_DoesNotSelect(
        string slot, string? transport, int expectedPrompts, int expectedExit)
    {
        using var output = new CommandOutput();
        int prompts = 0;
        int selections = 0;
        var command = new OpenPgpKeysAttestCommand
        {
            AcquirePin = _ => { prompts++; return null; },
            SelectDeviceAsync = (_, _, _) => { selections++; return Task.FromResult<DeviceSelection?>(null); },
        };

        Assert.Equal(expectedExit, await command.ExecuteAsync(Context, new KeysAttestSettings { Key = slot, Transport = transport }));
        Assert.Equal(expectedPrompts, prompts);
        Assert.Equal(0, selections);
    }

    [Theory]
    [InlineData(ExitCode.DeviceNotFound)]
    [InlineData(ExitCode.UserCancelled)]
    [InlineData(ExitCode.FeatureUnsupported)]
    [InlineData(ExitCode.GenericError)]
    public async Task Attest_SelectionDoesNotReachDevice_ClearsOwnedPinAndPreservesExit(int expectedExit)
    {
        using var output = new CommandOutput();
        using var pin = SecureCredential.FromUtf8String("123456");
        Memory<byte> storage = pin.Memory;
        int prompts = 0;
        int selections = 0;
        bool pinAvailableAtSelection = false;
        var command = new OpenPgpKeysAttestCommand
        {
            AcquirePin = _ => { prompts++; return pin; },
            SelectDeviceAsync = (_, _, _) =>
            {
                selections++;
                pinAvailableAtSelection = prompts == 1 && CryptographicOperations.FixedTimeEquals("123456"u8, storage.Span);
                return expectedExit switch
                {
                    ExitCode.DeviceNotFound => Task.FromResult<DeviceSelection?>(null),
                    ExitCode.UserCancelled => Task.FromException<DeviceSelection?>(new OperationCanceledException()),
                    ExitCode.FeatureUnsupported => Task.FromException<DeviceSelection?>(new ArgumentException("Fixture selector rejection")),
                    _ => Task.FromException<DeviceSelection?>(new IOException("Fixture selector failure")),
                };
            },
        };

        Assert.Equal(expectedExit, await command.ExecuteAsync(Context, new KeysAttestSettings { Key = "sig" }));
        Assert.Equal(1, selections);
        Assert.True(pinAvailableAtSelection);
        Assert.All(storage.ToArray(), value => Assert.Equal(0, value));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Attest_SelectedOnceWithPin_VerifiesPw1BeforeFbAndClearsPin(bool failAttestation)
    {
        using var output = new CommandOutput();
        using var pin = SecureCredential.FromUtf8String("123456");
        Memory<byte> storage = pin.Memory;
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var cert = new CertificateRequest("CN=Fixture", key, HashAlgorithmName.SHA256)
            .CreateSelfSigned(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1));
        var device = new LiveKey();
        device.Connection.Reply = wire => ReplyToAttestation(wire.Span, failAttestation, cert);
        int prompts = 0;
        int selections = 0;
        var command = new OpenPgpKeysAttestCommand
        {
            AcquirePin = _ => { prompts++; return pin; },
            SelectDeviceAsync = (_, _, _) =>
            {
                Assert.Equal(1, prompts);
                Assert.Empty(device.OpenedTypes);
                selections++;
                return Task.FromResult<DeviceSelection?>(Selection(device));
            },
        };

        int exitCode = await command.ExecuteAsync(Context, new KeysAttestSettings { Key = "sig" });

        Assert.Equal(failAttestation ? ExitCode.GenericError : ExitCode.Success, exitCode);
        Assert.Equal(1, selections);
        Assert.Single(device.OpenedTypes);
        Assert.Equal(failAttestation ? new byte[] { 0xA4, 0xF1, 0xCA, 0x20, 0xFB }
            : [0xA4, 0xF1, 0xCA, 0x20, 0xFB, 0xA5, 0xCA], device.Connection.Instructions);
        Assert.All(storage.ToArray(), value => Assert.Equal(0, value));
        Assert.True(device.Connection.Disposed);
    }

    private static byte[] ReplyToAttestation(ReadOnlySpan<byte> wire, bool fail, X509Certificate2 certificate)
    {
        if (wire[1] == 0x20)
        {
            Assert.Equal(0x81, wire[3]);
            Assert.True(CryptographicOperations.FixedTimeEquals("123456"u8, wire.Slice(5, wire[4])));
        }

        return (wire[1], wire[3]) switch
        {
            (0xA4 or 0x20 or 0xA5, _) => [0x90, 0],
            (0xF1, _) => [5, 8, 0, 0x90, 0],
            // Required application data: AID, C0 (no KDF), three RSA attributes, C4 PIN profile.
            (0xCA, 0x6E) => Convert.FromHexString("6E3F4F0ED276000124010304000612345678732DC00A00000000000000000000" +
                "C106010800001100C206010800001100C306010800001100C407007F7F7F0300039000"),
            (0xFB, _) => fail ? [0x69, 0x85] : [0x90, 0],
            (0xCA, 0x21) => [.. certificate.RawData, 0x90, 0],
            _ => throw new InvalidOperationException("Unexpected OpenPGP APDU in attest fixture."),
        };
    }

    [Fact]
    public async Task OrdinaryCommand_CarriesSelectionMetadataAndRequestedSessionTransport_WithoutExtraRead()
    {
        using var output = new CommandOutput();
        var device = new LiveKey();
        var selection = Selection(device);
        int selections = 0;
        var command = new ProbeCommand
        {
            SelectDeviceAsync = (transports, serial, requested) =>
            {
                selections++;
                Assert.Equal([ConnectionType.SmartCard, ConnectionType.HidFido], transports);
                Assert.Equal(222, serial);
                Assert.Equal(ConnectionType.HidFido, requested);
                return Task.FromResult<DeviceSelection?>(selection);
            }
        };

        int exitCode = await command.ExecuteAsync(Context, new GlobalSettings { Serial = 222, Transport = "fido" });

        Assert.Equal(ExitCode.Success, exitCode);
        Assert.Equal(1, selections);
        Assert.Equal(1, command.Executions);
        Assert.NotNull(command.Context);
        Assert.Same(selection, command.Context.Selection);
        Assert.Equal(selection.Info, command.Context.Info);
        Assert.Equal(ConnectionType.HidFido, command.Context.SessionOptions.PreferredConnectionType);
        Assert.Empty(device.OpenedTypes);
        Assert.Contains("cached-part", output.Console.Output, StringComparison.Ordinal);
        Assert.Contains("222", output.Console.Output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("invalid", ExitCode.GenericError, 0)]
    [InlineData(null, ExitCode.DeviceNotFound, 1)]
    [InlineData(null, ExitCode.FeatureUnsupported, 1)]
    [InlineData(null, ExitCode.UserCancelled, 1)]
    public async Task OrdinaryCommand_SelectionFailuresKeepExitCodesAndSkipCommand(string? transport, int exit, int expectedSelections)
    {
        using var output = new CommandOutput();
        int selections = 0;
        var command = new ProbeCommand
        {
            SelectDeviceAsync = (_, _, _) =>
            {
                selections++;
                return exit switch
                {
                    ExitCode.FeatureUnsupported => Task.FromException<DeviceSelection?>(new ArgumentException("Fixture selector rejection")),
                    ExitCode.UserCancelled => Task.FromException<DeviceSelection?>(new OperationCanceledException()),
                    _ => Task.FromResult<DeviceSelection?>(null),
                };
            },
        };

        Assert.Equal(exit, await command.ExecuteAsync(Context, new GlobalSettings { Transport = transport }));
        Assert.Equal(expectedSelections, selections);
        Assert.Equal(0, command.Executions);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ManagementInfoAndConfig_ReadAuthoritativeLiveMetadata_NotSelectionSnapshot(bool infoCommand)
    {
        using var output = new CommandOutput();
        var device = new LiveKey();
        YkCommandBase<GlobalSettings> command = infoCommand
            ? new ManagementInfoCommand { SelectDeviceAsync = (_, _, _) => Task.FromResult<DeviceSelection?>(Selection(device)) }
            : new ManagementConfigCommand { SelectDeviceAsync = (_, _, _) => Task.FromResult<DeviceSelection?>(Selection(device)) };

        int exitCode = await command.ExecuteAsync(Context, new GlobalSettings { Transport = "smartcard" });

        Assert.Equal(ExitCode.Success, exitCode);
        Assert.Equal([typeof(ISmartCardConnection)], device.OpenedTypes);
        Assert.True(device.Connection.InfoReads > 0);
        Assert.True(device.Connection.Disposed);
        Assert.Contains(infoCommand ? "333" : "30 seconds", output.Console.Output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ManagementInfoAndConfig_LiveReadFailure_DoesNotPrintStaleConfiguration(bool infoCommand)
    {
        using var output = new CommandOutput();
        var device = new LiveKey { FailOpen = true };
        YkCommandBase<GlobalSettings> command = infoCommand
            ? new ManagementInfoCommand { SelectDeviceAsync = (_, _, _) => Task.FromResult<DeviceSelection?>(Selection(device)) }
            : new ManagementConfigCommand { SelectDeviceAsync = (_, _, _) => Task.FromResult<DeviceSelection?>(Selection(device)) };

        int exitCode = await command.ExecuteAsync(Context, new GlobalSettings());

        Assert.Equal(ExitCode.Success, exitCode);
        Assert.Single(device.OpenedTypes);
        Assert.Contains("Could not retrieve", output.Console.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("USB Enabled", output.Console.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("USB Capabilities", output.Console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ManagementReset_UnsupportedSessionFirmware_ReportsBeforeConfirmationWithoutExtraRead()
    {
        using var output = new CommandOutput();
        var device = new LiveKey();
        var command = new ManagementResetCommand
        {
            SelectDeviceAsync = (_, _, _) => Task.FromResult<DeviceSelection?>(Selection(device))
        };

        int exitCode = await command.ExecuteAsync(Context, new ManagementResetSettings());

        Assert.Equal(ExitCode.FeatureUnsupported, exitCode);
        Assert.Equal([typeof(ISmartCardConnection)], device.OpenedTypes);
        Assert.Equal(new byte[] { 0xA4, 0x1D }, device.Connection.Instructions);
        Assert.True(device.Connection.Disposed);
        Assert.Contains("5.4.0", output.Console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ManagementReset_SessionOpenFails_ReportsOriginalFailureWithoutRetry()
    {
        using var output = new CommandOutput();
        var device = new LiveKey { FailOpen = true };
        var command = new ManagementResetCommand
        {
            SelectDeviceAsync = (_, _, _) => Task.FromResult<DeviceSelection?>(Selection(device))
        };

        int exitCode = await command.ExecuteAsync(Context, new ManagementResetSettings { Force = true });

        Assert.Equal(ExitCode.GenericError, exitCode);
        Assert.Single(device.OpenedTypes);
        Assert.Contains("Live read unavailable", output.Console.Output, StringComparison.Ordinal);
        Assert.Empty(device.Connection.Instructions);
    }

    [Fact]
    public async Task ManagementLiveRead_RequestedFido_DoesNotSilentlyOpenSmartCard()
    {
        using var output = new CommandOutput();
        var device = new LiveKey();
        var command = new ManagementInfoCommand
        {
            SelectDeviceAsync = (_, _, _) => Task.FromResult<DeviceSelection?>(Selection(device))
        };

        await command.ExecuteAsync(Context, new GlobalSettings { Transport = "fido" });

        Assert.Equal([typeof(IFidoHidConnection)], device.OpenedTypes);
    }

    [Fact]
    public async Task ManagementLiveRead_Cancellation_MapsToCancelledInsteadOfMissingInformation()
    {
        using var output = new CommandOutput();
        var device = new LiveKey { CancelOpen = true };
        var command = new ManagementInfoCommand
        {
            SelectDeviceAsync = (_, _, _) => Task.FromResult<DeviceSelection?>(Selection(device))
        };

        Assert.Equal(ExitCode.UserCancelled, await command.ExecuteAsync(Context, new GlobalSettings()));
        Assert.Single(device.OpenedTypes);
        Assert.DoesNotContain("Could not retrieve", output.Console.Output, StringComparison.Ordinal);
    }

    private static DeviceSelection Selection(IYubiKey device) => new(
        device, 222, FormFactor.UsbAKeychain, "5.8.0", device.AvailableConnections)
    {
        Info = DeviceInfo.CreateFromTlvs([], new FirmwareVersion(5, 8, 0)) with { PartNumber = "cached-part" }
    };

    private sealed class ProbeCommand : YkCommandBase<GlobalSettings>
    {
        public YkDeviceContext? Context { get; private set; }
        public int Executions { get; private set; }
        protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard, ConnectionType.HidFido];
        protected override Task<int> ExecuteCommandAsync(CommandContext context, GlobalSettings settings, YkDeviceContext deviceContext)
        {
            Executions++;
            Context = deviceContext;
            return Task.FromResult(ExitCode.Success);
        }
    }

    private sealed class LiveKey : IYubiKey
    {
        public string DeviceId => "opaque";
        public int? SerialNumber => 222;
        public ConnectionType AvailableConnections => ConnectionType.SmartCard | ConnectionType.HidFido;
        public List<Type> OpenedTypes { get; } = [];
        public ReadOnlyConnection Connection { get; } = new();
        public bool FailOpen { get; init; }
        public bool CancelOpen { get; init; }
        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            cancellationToken.ThrowIfCancellationRequested();
            OpenedTypes.Add(typeof(TConnection));
            if (CancelOpen)
            {
                throw new OperationCanceledException("Cancelled live read");
            }
            if (FailOpen || typeof(TConnection) != typeof(ISmartCardConnection))
            {
                throw new IOException("Live read unavailable");
            }

            return Task.FromResult((TConnection)(IConnection)Connection);
        }
    }

    private sealed class ReadOnlyConnection : ISmartCardConnection
    {
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;
        public int InfoReads { get; private set; }
        public bool Disposed { get; private set; }
        public List<byte> Instructions { get; } = [];
        public Func<ReadOnlyMemory<byte>, byte[]>? Reply { get; set; }
        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte instruction = command.Span[1];
            Instructions.Add(instruction);
            if (Reply is { } reply)
                return Task.FromResult<ReadOnlyMemory<byte>>(reply(command));

            ReadOnlyMemory<byte> response = instruction switch
            {
                0xA4 => new byte[] { 5, 4, 0, 0x90, 0 },
                // Management response: length, firmware 5.4.0, serial 333, USB enabled, auto-eject 30s, SW.
                0x1D => ReadInfo(),
                _ => throw new InvalidOperationException($"Unit fake forbids non-read instruction {instruction:X2}")
            };
            return Task.FromResult(response);
        }

        private ReadOnlyMemory<byte> ReadInfo()
        {
            InfoReads++;
            return new byte[] { 19, 5, 3, 5, 4, 0, 2, 4, 0, 0, 1, 0x4D, 3, 2, 0, 8, 6, 2, 0, 30, 0x90, 0 };
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => new NoopScope();
        public bool SupportsExtendedApdu() => false;
        public void Dispose() => Disposed = true;
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
        private sealed class NoopScope : IDisposable { public void Dispose() { } }
    }

    private sealed class EmptyArguments : IRemainingArguments
    {
        public ILookup<string, string?> Parsed => Array.Empty<string>().ToLookup(value => value, value => (string?)value);
        public IReadOnlyList<string> Raw => [];
    }

    private sealed class CommandOutput : IDisposable
    {
        private readonly IAnsiConsole _previous = AnsiConsole.Console;
        public TestConsole Console { get; } = new();
        public CommandOutput() => AnsiConsole.Console = Console;
        public void Dispose()
        {
            AnsiConsole.Console = _previous;
            Console.Dispose();
        }
    }
}
