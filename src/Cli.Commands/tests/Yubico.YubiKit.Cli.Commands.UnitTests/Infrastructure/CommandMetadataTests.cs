// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.Cli.Commands.Management;
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

    [Fact]
    public async Task OrdinaryCommand_CarriesSelectionMetadataAndRequestedSessionTransport_WithoutExtraRead()
    {
        using var output = new CommandOutput();
        var device = new LiveKey();
        var selection = Selection(device);
        var command = new ProbeCommand
        {
            SelectDeviceAsync = (transports, serial, requested) =>
            {
                Assert.Equal([ConnectionType.SmartCard, ConnectionType.HidFido], transports);
                Assert.Equal(222, serial);
                Assert.Equal(ConnectionType.HidFido, requested);
                return Task.FromResult<DeviceSelection?>(selection);
            }
        };

        int exitCode = await command.ExecuteAsync(Context, new GlobalSettings { Serial = 222, Transport = "fido" });

        Assert.Equal(ExitCode.Success, exitCode);
        Assert.NotNull(command.Context);
        Assert.Same(selection, command.Context.Selection);
        Assert.Equal(selection.Info, command.Context.Info);
        Assert.Equal(ConnectionType.HidFido, command.Context.SessionOptions.PreferredConnectionType);
        Assert.Empty(device.OpenedTypes);
        Assert.Contains("cached-part", output.Console.Output, StringComparison.Ordinal);
        Assert.Contains("222", output.Console.Output, StringComparison.Ordinal);
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
    public async Task ManagementReset_UsesLiveFirmwarePrerequisite_BeforeAnyResetOrConfirmation()
    {
        using var output = new CommandOutput();
        var device = new LiveKey();
        var command = new ManagementResetCommand
        {
            SelectDeviceAsync = (_, _, _) => Task.FromResult<DeviceSelection?>(Selection(device))
        };

        int exitCode = await command.ExecuteAsync(Context, new ManagementResetSettings { Force = true });

        Assert.Equal(ExitCode.FeatureUnsupported, exitCode);
        Assert.Equal([typeof(ISmartCardConnection)], device.OpenedTypes);
        Assert.True(device.Connection.InfoReads > 0);
        Assert.DoesNotContain((byte)0x1F, device.Connection.Instructions);
        Assert.Contains("5.4.0", output.Console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ManagementReset_LiveReadUnavailable_DoesNotUseCachedPrerequisite()
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
        Assert.Contains("Could not retrieve device information", output.Console.Output, StringComparison.Ordinal);
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
        protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard, ConnectionType.HidFido];
        protected override Task<int> ExecuteCommandAsync(CommandContext context, GlobalSettings settings, YkDeviceContext deviceContext)
        {
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
        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte instruction = command.Span[1];
            Instructions.Add(instruction);
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
