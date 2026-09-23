using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Management;

internal static class AcceptanceScenarios
{
    internal static async Task<int> RunAsync(string mode, int serial)
    {
        try
        {
            return mode switch
            {
                "--active-cancel" => await PresenceAsync(serial, cancel: true),
                "--touch" => await PresenceAsync(serial, cancel: false),
                "--otp-info" => await OtpInfoAsync(serial),
                "--removal" => await RemovalAsync(serial),
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL mode={mode} serial={serial}: {ex}");
            return 1;
        }
        finally
        {
            await YubiKeyManager.ShutdownAsync();
        }
    }

    private static async Task<IYubiKey> SelectedAsync(int serial, ConnectionType type, bool forceRescan = false)
    {
        var devices = await YubiKeyManager.FindAllAsync(forceRescan: forceRescan);
        var matches = devices.Where(d => d.SerialNumber == serial && d.SupportsConnection(type)).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException($"Selected serial={serial} matched {matches.Length} {type} devices");
        return matches[0];
    }

    private static async Task PresenceAsyncOnSession(FidoSession session, PresencePrompt prompt, bool cancel,
        CancellationToken token)
    {
        if (cancel)
        {
            var cancelled = false;
            try
            {
                await session.SelectionAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                await prompt.CancellationTask;
                cancelled = true;
            }
            catch (CtapException ex) when (ex.Status == CtapStatus.InvalidCommand)
            {
                throw new NotSupportedException("BLOCKED: selected firmware does not support CTAP Selection", ex);
            }
            if (!cancelled)
                throw new InvalidOperationException("Selection succeeded despite cancellation");
        }
        else
        {
            try { await session.SelectionAsync(token); }
            catch (CtapException ex) when (ex.Status == CtapStatus.InvalidCommand)
            {
                throw new NotSupportedException("BLOCKED: selected firmware does not support CTAP Selection", ex);
            }
        }

        if (prompt.Requested.Count != 1 || prompt.Requested[0].Basis != UserPresenceBasis.DeviceWaiting ||
            prompt.Resolved.Count != 1 || !ReferenceEquals(prompt.Requested[0], prompt.Resolved[0].Context) ||
            prompt.Resolved[0].Outcome != (cancel ? UserPresenceOutcome.Cancelled : UserPresenceOutcome.Completed) ||
            prompt.Resolved[0].Token != CancellationToken.None)
            throw new InvalidOperationException("User presence request/resolution did not pair exactly once");

        var info = await session.GetInfoAsync();
        if (info.Versions.Count == 0)
            throw new InvalidOperationException("Same-session getInfo returned no versions");
        Console.WriteLine($"PHASE_SAME_SESSION_INFO serial={prompt.Serial}");
    }

    private static async Task<int> PresenceAsync(int serial, bool cancel)
    {
        var selected = await SelectedAsync(serial, ConnectionType.HidFido);
        using var operation = new CancellationTokenSource();
        if (!cancel)
            operation.CancelAfter(TimeSpan.FromSeconds(50));
        var prompt = new PresencePrompt(serial, cancel ? "ACTIVE_CANCEL" : "TOUCH_SELECTED", cancel ? operation.Cancel : null);
        await using (var connection = await selected.ConnectAsync<IFidoHidConnection>())
        {
            if (connection.GetType().Name != "MacOSFidoHidConnection")
                throw new InvalidOperationException("Selected FIDO route is not the built-in macOS connection");
            await using (var session = await FidoSession.CreateAsync(connection,
                new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidFido, UserPresencePrompt = prompt }))
            {
                await PresenceAsyncOnSession(session, prompt, cancel, operation.Token);
            }
        }

        await using (var reopened = await selected.ConnectAsync<IFidoHidConnection>())
        {
            if (reopened.GetType().Name != "MacOSFidoHidConnection")
                throw new InvalidOperationException("Reopened FIDO route is not built-in macOS");
            await using var session = await FidoSession.CreateAsync(reopened);
            if ((await session.GetInfoAsync()).Versions.Count == 0)
                throw new InvalidOperationException("Reopened getInfo returned no versions");
        }
        Console.WriteLine($"PASS mode={(cancel ? "active-cancel" : "touch")} serial={serial} resolved=1 sameSessionInfo=1 reopenInfo=1");
        return 0;
    }

    private static async Task<int> OtpInfoAsync(int serial)
    {
        var selected = await SelectedAsync(serial, ConnectionType.HidOtp);
        for (var cycle = 1; cycle <= 3; cycle++)
        {
            await using (var connection = await selected.ConnectAsync<IOtpHidConnection>())
            {
                if (connection.GetType().Name != "MacOSOtpHidConnection")
                    throw new InvalidOperationException("Selected OTP route is not the built-in macOS connection");
                await using var session = await ManagementSession.CreateAsync(connection,
                    new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp });
                if (session.ConnectionType != ConnectionType.HidOtp)
                    throw new InvalidOperationException("Management selected a different transport");
                var info = await session.GetDeviceInfoAsync();
                if (info.SerialNumber != serial)
                    throw new InvalidOperationException("OTP device-info serial does not match selected serial");
                Console.WriteLine($"PHASE_OTP_INFO serial={serial} cycle={cycle} transport=HidOtp");
            }
        }
        Console.WriteLine($"PASS mode=otp-info serial={serial} cycles=3 deviceInfo=3");
        return 0;
    }

    private static async Task<int> RemovalAsync(int serial)
    {
        var selected = await SelectedAsync(serial, ConnectionType.HidFido);
        IFidoHidConnection connection = await selected.ConnectAsync<IFidoHidConnection>();
        bool observedAbsent = false;
        try
        {
            if (connection.GetType().Name != "MacOSFidoHidConnection")
                throw new InvalidOperationException("Selected FIDO route is not built-in macOS");
            // No command was sent on this fresh connection; this is a pending native input wait.
            Task<ReadOnlyMemory<byte>> reading = connection.ReceiveAsync();
            if (reading.IsCompleted)
                throw new InvalidOperationException("Raw receive completed before removal checkpoint");
            Console.WriteLine($"READY_UNPLUG serial={serial} utc={DateTimeOffset.UtcNow:O}");
            try
            {
                _ = await reading.WaitAsync(TimeSpan.FromSeconds(60));
                throw new InvalidOperationException("Raw receive returned a report instead of terminal removal");
            }
            catch (InvalidOperationException ex) when (ex.Message == "FIDO input terminated: 1")
            {
                // A disposal-induced terminal is not evidence of physical removal.
                var absent = await YubiKeyManager.FindAllAsync(forceRescan: true);
                if (absent.Any(d => d.SerialNumber == serial))
                    throw new InvalidOperationException("Selected device was not observed absent after terminal input");
                observedAbsent = true;
                Console.WriteLine($"PHASE_REMOVAL_TERMINAL serial={serial} exception={ex.GetType().Name} utc={DateTimeOffset.UtcNow:O}");
            }
        }
        finally
        {
            // A failed native close retains the claim; never treat that failure as safe reuse.
            await connection.DisposeAsync();
        }
        Console.WriteLine($"PHASE_REMOVAL_DISPOSED serial={serial}");

        // Require an observed absent snapshot before accepting a replacement, not just a cached device.
        if (!observedAbsent)
            throw new InvalidOperationException("Physical absence was not observed");
        Console.WriteLine($"READY_REPLUG serial={serial} utc={DateTimeOffset.UtcNow:O}");

        IYubiKey? replacement = null;
        using var rescan = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        while (!rescan.IsCancellationRequested)
        {
            var found = await YubiKeyManager.FindAllAsync(forceRescan: true, cancellationToken: rescan.Token);
            var matches = found.Where(d => d.SerialNumber == serial && d.SupportsConnection(ConnectionType.HidFido)).ToArray();
            if (matches.Length > 1)
                throw new InvalidOperationException("More than one matching replacement device");
            if (matches.Length == 1)
            {
                replacement = matches[0];
                break;
            }
            await Task.Delay(250, rescan.Token);
        }
        if (replacement is null || ReferenceEquals(replacement, selected))
            throw new InvalidOperationException("Fresh replacement generation was not observed");
        await using (var reopened = await replacement.ConnectAsync<IFidoHidConnection>())
        {
            if (reopened.GetType().Name != "MacOSFidoHidConnection")
                throw new InvalidOperationException("Replacement route is not built-in macOS");
            await using var session = await FidoSession.CreateAsync(reopened);
            if ((await session.GetInfoAsync()).Versions.Count == 0)
                throw new InvalidOperationException("Replacement getInfo returned no versions");
        }
        Console.WriteLine($"PASS mode=removal serial={serial} terminal=1 nativeDispose=1 freshGeneration=1 getInfo=1");
        return 0;
    }

    private sealed class PresencePrompt(int serial, string phase, Action? cancel) : IUserPresencePrompt
    {
        public int Serial { get; } = serial;
        public List<UserPresenceContext> Requested { get; } = [];
        public List<(UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken Token)> Resolved { get; } = [];
        public Task CancellationTask { get; private set; } = Task.CompletedTask;

        public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken)
        {
            if (cancel is not null && cancellationToken.IsCancellationRequested)
                throw new InvalidOperationException("Selection was cancelled before device-waiting notification");
            Requested.Add(context);
            Console.WriteLine($"{phase} serial={Serial} basis={context.Basis}");
            // Avoid running cancellation callbacks inside the presence prompt.
            if (cancel is not null)
                CancellationTask = Task.Run(cancel);
            return default;
        }

        public ValueTask OnUserPresenceResolvedAsync(UserPresenceContext context, UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            Resolved.Add((context, outcome, cancellationToken));
            Console.WriteLine($"PHASE_PRESENCE_RESOLVED serial={Serial} outcome={outcome}");
            return default;
        }
    }
}