using System.Diagnostics;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid.Linux;
using Yubico.YubiKit.Ctap2;

namespace LinuxHidRouteVerification;

// These modes require separately coordinated operator actions; building this host does not execute them.
// No persistent provisioning or MakeCredential/GetAssertion/PIN operation is requested; setup can
// still open interfaces and select an applet. The assertions
// cover paired notification and selected GetInfo/reopen outcomes, not a native interruption deadline.
internal static class OperatorScenarios
{
    internal static async Task<int> RunAsync(string mode, IYubiKey selected, int serial, Evidence evidence)
    {
        if (mode == "--removal") await RemovalAsync(selected, serial, evidence);
        else await PresenceAsync(selected, mode == "--active-cancel", evidence);
        return 0;
    }

    private static async Task PresenceAsync(IYubiKey selected, bool cancel, Evidence evidence)
    {
        using var operation = new CancellationTokenSource();
        var prompt = new PresencePrompt(cancel ? operation : null);
        await using (IFidoHidConnection connection = await selected.ConnectAsync<IFidoHidConnection>())
        {
            evidence.Check(connection is LinuxFidoHidConnection, "operator-selection-production-Linux-owner");
            await using Ctap2Session session = await Ctap2Session.CreateAsync(connection,
                new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidFido, UserPresencePrompt = prompt });
            Console.WriteLine(cancel ? "READY_ACTIVE_CANCEL: do not touch the selected key" : "READY_TOUCH: touch only the selected key");
            bool cancelled = false;
            try { await session.SelectionAsync(operation.Token); }
            catch (OperationCanceledException) when (cancel && operation.IsCancellationRequested) { cancelled = true; }
            await prompt.CancellationTask;
            VerifyPresence(evidence, prompt, cancel, cancelled);
            evidence.Check((await session.GetInfoAsync()).Versions.Count > 0, "operator-selection-same-session-getinfo-recovery");
            if (cancel)
                evidence.Measurements.Add(new("operator-cancel-request-to-getinfo-recovery", null,
                    Stopwatch.GetElapsedTime(prompt.CancelTimestamp).TotalMilliseconds, null));
        }
        await ReopenedAsync(selected, evidence);
    }

    private static void VerifyPresence(Evidence evidence, PresencePrompt prompt, bool cancel, bool cancelled) =>
        evidence.Check(cancelled == cancel && prompt.Requested is { Basis: UserPresenceBasis.DeviceWaiting } &&
            prompt.RequestCount == 1 && prompt.ResolutionCount == 1 &&
            ReferenceEquals(prompt.Requested, prompt.Resolved) &&
            prompt.Outcome == (cancel ? UserPresenceOutcome.Cancelled : UserPresenceOutcome.Completed),
            "operator-selection-device-wait-presence-paired-once");

    private static async Task RemovalAsync(IYubiKey selected, int serial, Evidence evidence)
    {
        IFidoHidConnection connection = await selected.ConnectAsync<IFidoHidConnection>();
        try
        {
            evidence.Check(connection is LinuxFidoHidConnection, "operator-removal-production-Linux-owner");
            Task<ReadOnlyMemory<byte>> reading = connection.ReceiveAsync();
            evidence.Check(!reading.IsCompleted, "operator-removal-read-pending-before-unplug");
            Console.WriteLine($"READY_UNPLUG serial={serial}");
            bool terminal = false;
            try { _ = await reading.WaitAsync(TimeSpan.FromSeconds(60)); }
            catch (IOException) { terminal = true; }
            catch (InvalidOperationException) { terminal = true; }
            // No disposal has been requested yet. An observed absent SDK snapshot is mandatory.
            IReadOnlyList<IYubiKey> absent = await YubiKeyManager.FindAllAsync();
            evidence.Check(terminal && !absent.Any(device => device.SerialNumber == serial),
                "operator-removal-terminal-read-and-observed-physical-absence");
        }
        finally { await evidence.MeasureAsync("operator-removal-dispose", () => connection.DisposeAsync().AsTask()); }
        Console.WriteLine($"READY_REPLUG serial={serial}");
        IYubiKey replacement = await ReplacementAsync(serial);
        // One-shot discovery creates independent views; this is not monitor-generation evidence.
        evidence.Check(!ReferenceEquals(replacement, selected), "operator-removal-independent-discovery-result");
        await ReopenedAsync(replacement, evidence);
    }

    private static async Task<IYubiKey> ReplacementAsync(int serial)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        while (true)
        {
            IReadOnlyList<IYubiKey> devices = await YubiKeyManager.FindAllAsync(cancellationToken: deadline.Token);
            IYubiKey[] matches = [.. devices.Where(device => device.SerialNumber == serial && device.SupportsConnection(ConnectionType.HidFido))];
            if (matches.Length > 1) throw new InvalidOperationException("Ambiguous replacement");
            if (matches.Length == 1) return matches[0];
            await Task.Delay(250, deadline.Token);
        }
    }

    private static async Task ReopenedAsync(IYubiKey selected, Evidence evidence)
    {
        await using IFidoHidConnection connection = await selected.ConnectAsync<IFidoHidConnection>();
        evidence.Check(connection is LinuxFidoHidConnection, "operator-reopen-production-Linux-owner");
        await using Ctap2Session session = await Ctap2Session.CreateAsync(connection);
        evidence.Check((await session.GetInfoAsync()).Versions.Count > 0, "operator-reopen-readonly-getinfo");
    }

    private sealed class PresencePrompt(CancellationTokenSource? cancel) : IUserPresencePrompt
    {
        internal UserPresenceContext? Requested { get; private set; }
        internal UserPresenceContext? Resolved { get; private set; }
        internal UserPresenceOutcome Outcome { get; private set; }
        internal int RequestCount { get; private set; }
        internal int ResolutionCount { get; private set; }
        internal long CancelTimestamp { get; private set; }
        internal Task CancellationTask { get; private set; } = Task.CompletedTask;

        public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken)
        {
            Requested = context;
            RequestCount++;
            if (cancel is not null)
            {
                CancelTimestamp = Stopwatch.GetTimestamp();
                // Avoid running transport cancellation callbacks inside the presence callback.
                CancellationTask = Task.Run(cancel.Cancel, cancellationToken);
            }
            return ValueTask.CompletedTask;
        }

        public ValueTask OnUserPresenceResolvedAsync(UserPresenceContext context, UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            Resolved = context;
            Outcome = outcome;
            ResolutionCount++;
            return ValueTask.CompletedTask;
        }
    }
}