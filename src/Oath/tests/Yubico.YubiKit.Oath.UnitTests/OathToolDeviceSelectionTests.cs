// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Rendering;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli;

namespace Yubico.YubiKit.Oath.UnitTests;

public class OathToolDeviceSelectionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WaitingForRetryOrSelection_CancelsWithoutFurtherInputOrOathOpen(bool multipleDevices)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var console = new WaitingConsole();
        var first = new FakeDevice("first");
        var second = new FakeDevice("second");
        int discoveries = 0;
        var open = OathToolServices.OpenSelectedSessionAsync(cancellation.Token, console, token =>
        {
            Assert.Equal(cancellation.Token, token);
            discoveries++;
            return Task.FromResult<IReadOnlyList<IYubiKey>>(multipleDevices ? [first, second] : []);
        });

        await console.Waiting.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            open.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(cancellation.Token, console.Waiting.Token);
        Assert.Equal(0, console.Waiting.SyncReads);
        Assert.Equal(1, discoveries);
        // Only the two metadata lookups precede selection. No OATH connection is opened afterward.
        Assert.Equal(multipleDevices ? 1 : 0, first.ConnectionAttempts);
        Assert.Equal(multipleDevices ? 1 : 0, second.ConnectionAttempts);
    }

    [Fact]
    public async Task MetadataCancellation_IsNotConvertedToMissingInfoOrFollowedByOathOpen()
    {
        using var console = new WaitingConsole();
        var device = new FakeDevice("cancelled") { CancelMetadata = true };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => OathToolServices.OpenSelectedSessionAsync(
            TestContext.Current.CancellationToken, console,
            _ => Task.FromResult<IReadOnlyList<IYubiKey>>([device])));

        Assert.Equal(1, device.ConnectionAttempts);
        Assert.False(console.Waiting.Started.Task.IsCompleted);
    }

    [Fact]
    public async Task MetadataFinishingAfterCancellation_DoesNotOpenOathSession()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var console = new WaitingConsole();
        var device = new FakeDevice("late") { OnConnect = cancellation.Cancel };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => OathToolServices.OpenSelectedSessionAsync(
            cancellation.Token, console, _ => Task.FromResult<IReadOnlyList<IYubiKey>>([device])));

        Assert.Equal(1, device.ConnectionAttempts);
    }

    private sealed class FakeDevice(string id) : IYubiKey
    {
        public string DeviceId => id;
        public ConnectionType AvailableConnections => ConnectionType.SmartCard;
        public int ConnectionAttempts { get; private set; }
        public bool CancelMetadata { get; init; }
        public Action OnConnect { get; init; } = () => { };

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            ConnectionAttempts++;
            OnConnect();
            return Task.FromException<TConnection>(CancelMetadata
                ? new OperationCanceledException(cancellationToken)
                : new NotSupportedException("Fake metadata is unavailable."));
        }
    }

    private sealed class WaitingInput : IAnsiConsoleInput
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken Token { get; private set; }
        public int SyncReads { get; private set; }
        public bool IsKeyAvailable() => false;
        public ConsoleKeyInfo? ReadKey(bool intercept)
        {
            SyncReads++;
            Started.TrySetResult();
            throw new InvalidOperationException("Synchronous prompt would require more user input.");
        }

        public async Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
        {
            Token = cancellationToken;
            Started.TrySetResult();
            // Spectre's synchronous Show delegates to ReadKeyAsync with CancellationToken.None.
            // Fail immediately on that regression rather than trapping the test in its blocking wrapper.
            if (!cancellationToken.CanBeCanceled)
            {
                throw new InvalidOperationException("Prompt dropped its cancellation token.");
            }

            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return null;
        }
    }

    private sealed class WaitingConsole : IAnsiConsole, IDisposable
    {
        private readonly StringWriter _output = new();
        private readonly IAnsiConsole _console;
        public WaitingInput Waiting { get; } = new();

        public WaitingConsole()
        {
            _console = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.Yes,
                Interactive = InteractionSupport.Yes,
                Out = new AnsiConsoleOutput(_output)
            });
        }

        public Profile Profile => _console.Profile;
        public IAnsiConsoleCursor Cursor => _console.Cursor;
        public IAnsiConsoleInput Input => Waiting;
        public IExclusivityMode ExclusivityMode => _console.ExclusivityMode;
        public RenderPipeline Pipeline => _console.Pipeline;
        public void Clear(bool home) => _console.Clear(home);
        public void Write(IRenderable renderable) => _console.Write(renderable);
        public void Dispose() => _output.Dispose();
    }
}