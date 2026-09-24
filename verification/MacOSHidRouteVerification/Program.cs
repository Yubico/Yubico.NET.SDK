using System.Diagnostics;
using System.Globalization;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Fido2;

// The verification executable itself contains the integration assertions. No fake bridge, HID
// experiment library, device mutation, or implicit transport fallback is involved.
Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
if (!OperatingSystem.IsMacOS())
{
    Console.Error.WriteLine("BLOCKED: macOS is required");
    return 2;
}

if (args is ["--child", "--list"])
    return await DiscoverAsync(null);
if (args is ["--child", "--listener-drain"])
    return await ListenerDrainScenario.RunAsync();
if (args is ["--child", "--listener-late-drain"])
    return ListenerDrainScenario.RunLateDrain();
if (args is ["--child", "--listener-remove", "--serial", var removalSerial]
    && int.TryParse(removalSerial, NumberStyles.None, CultureInfo.InvariantCulture, out int selectedRemovalSerial)
    && selectedRemovalSerial > 0)
    return await ListenerDrainScenario.RunRemovalAsync(selectedRemovalSerial);
if (args is ["--child", "--probe", "--serial", var childSerial]
    && int.TryParse(childSerial, NumberStyles.None, CultureInfo.InvariantCulture, out int selectedSerial))
    return await DiscoverAsync(selectedSerial);
if (args is ["--child", "--otp-get", "--serial", var childOtpSerial]
    && int.TryParse(childOtpSerial, NumberStyles.None, CultureInfo.InvariantCulture, out int selectedOtpSerial))
    return await ProbeOtpAsync(selectedOtpSerial);
if (args is ["--child", "--active-cancel" or "--otp-info" or "--touch" or "--removal", "--serial", var scenarioSerial]
    && int.TryParse(scenarioSerial, NumberStyles.None, CultureInfo.InvariantCulture, out int selectedScenarioSerial)
    && selectedScenarioSerial > 0)
    return await AcceptanceScenarios.RunAsync(args[1], selectedScenarioSerial);
if (args is ["--child", "--expert-io", "--serial", var expertSerial]
    && int.TryParse(expertSerial, NumberStyles.None, CultureInfo.InvariantCulture, out int selectedExpertSerial)
    && selectedExpertSerial > 0)
    return await ExpertIoScenario.RunAsync(selectedExpertSerial);
if (args is ["--child", "--expert-feature", "--serial", var featureSerial]
    && int.TryParse(featureSerial, NumberStyles.None, CultureInfo.InvariantCulture, out int selectedFeatureSerial)
    && selectedFeatureSerial > 0)
    return await ExpertFeatureScenario.RunAsync(selectedFeatureSerial);
if (args is ["--child", "--smartcard", "--serial", var smartCardSerial]
    && int.TryParse(smartCardSerial, NumberStyles.None, CultureInfo.InvariantCulture, out int selectedSmartCardSerial)
    && selectedSmartCardSerial > 0)
    return await SmartCardScenario.RunAsync(selectedSmartCardSerial);

if (args is not ["--list"] and not ["--listener-drain"] and not ["--listener-late-drain"] and not ["--probe" or "--otp-get" or "--active-cancel" or "--otp-info" or "--touch" or "--removal" or "--expert-io" or "--expert-feature" or "--smartcard" or "--listener-remove", "--serial", _])
{
    Console.Error.WriteLine("Usage: MacOSHidRouteVerification --list | --listener-drain | --listener-late-drain | (--probe | --otp-get | --active-cancel | --otp-info | --touch | --removal | --expert-io | --expert-feature | --smartcard | --listener-remove) --serial SERIAL");
    return 2;
}
if (args is [_, "--serial", var serial]
    && (!int.TryParse(serial, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) || parsed <= 0))
{
    Console.Error.WriteLine("BLOCKED: specify one positive decimal serial explicitly");
    return 2;
}

using var child = new Process();
child.StartInfo = new ProcessStartInfo(Environment.ProcessPath ?? throw new InvalidOperationException("No executable path"))
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true
};
child.StartInfo.ArgumentList.Add("--child");
foreach (string arg in args)
    child.StartInfo.ArgumentList.Add(arg);
child.Start();
Task output = CopyAsync(child.StandardOutput, Console.Out);
Task error = CopyAsync(child.StandardError, Console.Error);
try
{
    await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(args[0] == "--listener-remove" ? 200 : args[0] == "--removal" ? 180 : args[0] == "--touch" ? 60 : args[0] == "--expert-io" ? 30 : 20));
    await Task.WhenAll(output, error);
    Console.WriteLine($"child exit={child.ExitCode}");
    return child.ExitCode;
}
catch (TimeoutException)
{
    child.Kill(entireProcessTree: true);
    await child.WaitForExitAsync();
    await Task.WhenAll(output, error);
    Console.Error.WriteLine("FAIL child watchdog expired; process kill is OS cleanup, NOT SDK native drain proof");
    return 3;
}

static async Task CopyAsync(StreamReader source, TextWriter target)
{
    string? line;
    while ((line = await source.ReadLineAsync()) is not null)
        await target.WriteLineAsync(line);
}

static async Task<int> ProbeOtpAsync(int serial)
{
    try
    {
        IReadOnlyList<IYubiKey> devices = await YubiKeyManager.FindAllAsync();
        IYubiKey[] matches = devices.Where(d => d.SerialNumber == serial &&
            d.SupportsConnection(ConnectionType.HidOtp)).ToArray();
        if (matches.Length != 1)
        {
            Console.Error.WriteLine($"BLOCKED: serial={serial} matched {matches.Length} OTP HID devices");
            return 2;
        }

        for (int cycle = 1; cycle <= 3; cycle++)
        {
            var timer = Stopwatch.StartNew();
            Task<IOtpHidConnection> opening;
            try
            {
                opening = matches[0].ConnectAsync<IOtpHidConnection>();
                Console.WriteLine($"otp cycle={cycle} open invocationMs={timer.Elapsed.TotalMilliseconds:F3} pending={!opening.IsCompleted}");
                await opening;
            }
            catch (UnrecoveredConnectionException) when (cycle == 1)
            {
                // Observe whether an abandoned discovery read eventually releases its claim.
                // A retry is diagnostic, not permission to release an unproven native owner.
                var recovery = Stopwatch.StartNew();
                while (true)
                {
                    if (recovery.Elapsed > TimeSpan.FromSeconds(8))
                        throw new TimeoutException("OTP discovery claim remained unrecovered for 8 seconds");
                    await Task.Delay(250);
                    try
                    {
                        opening = matches[0].ConnectAsync<IOtpHidConnection>();
                        await opening;
                        Console.WriteLine($"otp discovery claim released after {recovery.Elapsed.TotalMilliseconds:F0}ms");
                        break;
                    }
                    catch (UnrecoveredConnectionException) { }
                }
            }
            Console.WriteLine($"otp cycle={cycle} open completedMs={timer.Elapsed.TotalMilliseconds:F3} (includes any discovery recovery wait)");
            await using IOtpHidConnection connection = await opening;
            if (connection.GetType().Name != "MacOSOtpHidConnection")
                throw new InvalidOperationException("OTP did not use the connection-owned macOS feature worker");
            ReadOnlyMemory<byte> report = await connection.ReceiveAsync();
            if (report.Length != connection.FeatureReportSize)
                throw new InvalidOperationException($"Unexpected feature report length: {report.Length}");
            Console.WriteLine($"otp cycle={cycle} read-only GET length={report.Length}; close on dispose");
        }

        Console.WriteLine($"PASS OTP read-only feature GET/open/dispose/reopen; serial={serial}, cycles=3");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FAIL OTP serial={serial}: {ex}");
        return 1;
    }
    finally
    {
        await YubiKeyManager.ShutdownAsync();
    }
}

static async Task<int> DiscoverAsync(int? serial)
{
    try
    {
        IReadOnlyList<IYubiKey> devices = await YubiKeyManager.FindAllAsync();
        Console.WriteLine($"discovered={devices.Count}");
        foreach (IYubiKey device in devices)
            Console.WriteLine($"fixture serial={device.SerialNumber?.ToString(CultureInfo.InvariantCulture) ?? "unknown"} " +
                $"id={device.DeviceId} connections={device.AvailableConnections}");

        if (serial is null)
        {
            if (devices.Count == 0)
                Console.Error.WriteLine("BLOCKED: no YubiKey discovered; no selected serial or HID route");
            return devices.Count == 0 ? 2 : 0;
        }

        IYubiKey[] matches = devices.Where(d => d.SerialNumber == serial
            && d.SupportsConnection(ConnectionType.HidFido)).ToArray();
        if (matches.Length != 1)
        {
            Console.Error.WriteLine($"BLOCKED: serial={serial} matched {matches.Length} FIDO HID devices; no target operation attempted");
            return 2;
        }

        IYubiKey selected = matches[0];
        Console.WriteLine($"SELECTED serial={serial} id={selected.DeviceId} connections={selected.AvailableConnections}");
        for (int cycle = 1; cycle <= 3; cycle++)
        {
            IFidoHidConnection? connection = null;
            FidoSession? session = null;
            try
            {
                int callerThread = Environment.CurrentManagedThreadId;
                var timer = Stopwatch.StartNew();
                Task<IFidoHidConnection> opening = selected.ConnectAsync<IFidoHidConnection>();
                Console.WriteLine($"cycle={cycle} open invocationMs={timer.Elapsed.TotalMilliseconds:F3} pending={!opening.IsCompleted} callerThread={callerThread} returnThread={Environment.CurrentManagedThreadId}");
                connection = await opening;
                Console.WriteLine($"cycle={cycle} open completedMs={timer.Elapsed.TotalMilliseconds:F3} connectionType={connection.GetType().Name}");
                if (connection.GetType().Name != "MacOSFidoHidConnection")
                    throw new InvalidOperationException("Selected connection did not use the production macOS native owner");

                timer.Restart();
                Task<FidoSession> creating = FidoSession.CreateAsync(connection);
                Console.WriteLine($"cycle={cycle} init+getInfo invocationMs={timer.Elapsed.TotalMilliseconds:F3} pending={!creating.IsCompleted} callerThread={Environment.CurrentManagedThreadId} returnThread={Environment.CurrentManagedThreadId}");
                session = await creating;
                Console.WriteLine($"cycle={cycle} CTAPHID_INIT + session authenticatorGetInfo completedMs={timer.Elapsed.TotalMilliseconds:F3} sessionFirmware={session.FirmwareVersion}");

                timer.Restart();
                Task<AuthenticatorInfo> getting = session.GetInfoAsync();
                Console.WriteLine($"cycle={cycle} getInfo invocationMs={timer.Elapsed.TotalMilliseconds:F3} pending={!getting.IsCompleted} callerThread={Environment.CurrentManagedThreadId}");
                AuthenticatorInfo info = await getting;
                if (info.Versions.Count == 0)
                    throw new InvalidOperationException("authenticatorGetInfo returned no versions");
                Console.WriteLine($"cycle={cycle} authenticatorGetInfo completedMs={timer.Elapsed.TotalMilliseconds:F3} responseFirmware={info.FirmwareVersion} versionCount={info.Versions.Count}");
            }
            finally
            {
                try
                {
                    if (session is not null)
                        await session.DisposeAsync();
                }
                finally
                {
                    if (connection is not null)
                    {
                        var drain = Stopwatch.StartNew();
                        await connection.DisposeAsync();
                        Console.WriteLine($"cycle={cycle} public connection DisposeAsync completedMs={drain.Elapsed.TotalMilliseconds:F3} (production native cancel/wait/destroy path)");
                    }
                }
            }
        }
        // A fresh connection has not sent INIT: there should be no solicited packet to complete
        // the raw read. The invocation barrier proves ReceiveAsync registered before disposal.
        IFidoHidConnection pendingConnection = await selected.ConnectAsync<IFidoHidConnection>();
        try
        {
            if (pendingConnection.GetType().Name != "MacOSFidoHidConnection")
                throw new InvalidOperationException("Pending receive did not use the production macOS native owner");
            var invoked = new TaskCompletionSource<Task<ReadOnlyMemory<byte>>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ = Task.Run(() =>
            {
                try { invoked.TrySetResult(pendingConnection.ReceiveAsync()); }
                catch (Exception ex) { invoked.TrySetException(ex); }
            });
            Task<ReadOnlyMemory<byte>> receiving = await invoked.Task;
            if (receiving.IsCompleted)
                throw new InvalidOperationException("Raw receive completed before disposal; no pending-read lifetime evidence");
            Console.WriteLine("pendingRawReceive invocation barrier reached; receive pending=True; no packet sent");

            var drain = Stopwatch.StartNew();
            await pendingConnection.DisposeAsync();
            Console.WriteLine($"pendingRawReceive public connection DisposeAsync completedMs={drain.Elapsed.TotalMilliseconds:F3}");
            if (!receiving.IsCompleted)
                throw new InvalidOperationException("Raw receive was not terminal after public connection disposal");
            try
            {
                _ = await receiving;
                throw new InvalidOperationException("Raw receive returned a packet instead of terminal disposal");
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"pendingRawReceive terminalException={ex.GetType().Name}");
            }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("FIDO input terminated:", StringComparison.Ordinal))
            {
                Console.WriteLine($"pendingRawReceive terminalException={ex.GetType().Name}: {ex.Message}");
            }
        }
        finally
        {
            await pendingConnection.DisposeAsync();
        }

        IFidoHidConnection reopened = await selected.ConnectAsync<IFidoHidConnection>();
        string? reopenResult = null;
        try
        {
            if (reopened.GetType().Name != "MacOSFidoHidConnection")
                throw new InvalidOperationException("Reopened connection did not use the production macOS native owner");
            await using FidoSession session = await FidoSession.CreateAsync(reopened);
            AuthenticatorInfo info = await session.GetInfoAsync();
            if (info.Versions.Count == 0)
                throw new InvalidOperationException("Reopened authenticatorGetInfo returned no versions");
            reopenResult = $"serial={serial} sessionFirmware={session.FirmwareVersion} responseFirmware={info.FirmwareVersion} versionCount={info.Versions.Count}";
        }
        finally
        {
            await reopened.DisposeAsync();
        }
        Console.WriteLine($"PASS pending raw ReceiveAsync → public connection DisposeAsync → terminal read → reopen/init/getInfo/dispose; {reopenResult}");
        // An already-cancelled caller must not submit a command; this is not a keepalive
        // cancellation probe, which requires an operator-controlled held ceremony.
        IFidoHidConnection cancelConnection = await selected.ConnectAsync<IFidoHidConnection>();
        try
        {
            if (cancelConnection.GetType().Name != "MacOSFidoHidConnection")
                throw new InvalidOperationException("Cancellation probe did not use the production macOS native owner");
            await using RawFidoHidSession raw = await RawFidoHidSession.CreateAsync(cancelConnection);
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            try
            {
                _ = await raw.SendAndReceiveAsync(0x10, new byte[] { 0x04 }, cancelled.Token);
                throw new InvalidOperationException("Already-cancelled command was accepted");
            }
            catch (OperationCanceledException) when (cancelled.IsCancellationRequested)
            {
                Console.WriteLine("predispatch cancellation=OperationCanceledException");
            }
            ReadOnlyMemory<byte> response = await raw.SendAndReceiveAsync(0x10, new byte[] { 0x04 });
            if (response.IsEmpty || response.Span[0] != 0)
                throw new InvalidOperationException("Post-cancellation read-only getInfo failed");
        }
        finally
        {
            await cancelConnection.DisposeAsync();
        }
        await using (IFidoHidConnection finalConnection = await selected.ConnectAsync<IFidoHidConnection>())
        await using (FidoSession finalSession = await FidoSession.CreateAsync(finalConnection))
        {
            AuthenticatorInfo finalInfo = await finalSession.GetInfoAsync();
            if (finalInfo.Versions.Count == 0)
                throw new InvalidOperationException("Post-cancellation reopen getInfo returned no versions");
        }
        Console.WriteLine("PASS predispatch cancellation → read-only getInfo → dispose → reopen/getInfo/dispose");
        Console.WriteLine("PASS 5 production HID scenarios (3 cycles + pending read disposal/reopen + predispatch cancellation/reopen); removal and touch pending (not performed)");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FAIL {ex}");
        return 1;
    }
    finally
    {
        await YubiKeyManager.ShutdownAsync();
    }
}
