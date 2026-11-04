using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Xunit.Abstractions;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.YubiOtp;

[assembly: CollectionBehavior(DisableTestParallelization = true, MaxParallelThreads = 1)]

namespace Yubico.YubiKit.AuditOtp001.Hardware.IntegrationTests;

// Device selection/authorization stays in the real shared harness. No fixture
// programs a slot, and no test can select a different dedicated key accidentally.
public sealed class Serial103Filter : IYubiKeyFilter
{
    public bool Matches(YubiKeyTestState device) => device.SerialNumber == 103;
    public string GetDescription() => "Only the authorized YubiOTP probe key, serial 103";
}

[Trait(TestCategories.Category, TestCategories.RequiresHardware)]
[Trait(TestCategories.Category, TestCategories.Slow)]
public sealed class HardwareProbeTests(ITestOutputHelper output)
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidOtp, RequireUsb = true, CustomFilter = typeof(Serial103Filter))]
    public async Task StatusMatrix_ReportsFreshFlags_AndMeasuresSmartCardHmac(YubiKeyTestState state)
    {
        byte[] key = NewHmacKey();
        byte[] challenge = [0x41, 0x42, 0x43, 0x44];
        byte[] scanCodes = [0x04, 0x05, 0x06, 0x07];
        try
        {
            await ExecuteAsync(state, "status-matrix", async probe =>
            {
                using var lifetime = probe.Budget(TimeSpan.FromSeconds(45));
                using (var configuration = new StaticPasswordSlotConfiguration(scanCodes).AppendCr(false))
                {
                    await probe.ProgramAsync(configuration, lifetime.Token);
                }
                await probe.SnapshotBothAsync("raw-static-never-type", true, lifetime.Token);

                using (var configuration = new HmacSha1SlotConfiguration(key).RequireTouch(false))
                {
                    await probe.ProgramAsync(configuration, lifetime.Token);
                }
                await probe.SnapshotBothAsync("non-touch-hmac", true, lifetime.Token);
                await probe.AssertHmacAsync(key, challenge, lifetime.Token);
                await probe.MeasureSmartCardHmacAsync(key, challenge, lifetime.Token);

                using (var configuration = new HmacSha1SlotConfiguration(key).RequireTouch())
                {
                    await probe.ProgramAsync(configuration, lifetime.Token);
                }
                await probe.SnapshotBothAsync("require-touch-hmac-do-not-touch", true, lifetime.Token);
            }, recover: false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(scanCodes);
            CryptographicOperations.ZeroMemory(challenge);
            CryptographicOperations.ZeroMemory(key);
        }
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidOtp, RequireUsb = true, CustomFilter = typeof(Serial103Filter))]
    public async Task HidCancel_AfterDeviceWaiting_ResolvesCancelledOnce_AndReopens(YubiKeyTestState state)
    {
        byte[] key = NewHmacKey();
        byte[] challenge = [0x51, 0x52, 0x53, 0x54];
        try
        {
            await ExecuteAsync(state, "cancel-no-touch", async probe =>
            {
                using var lifetime = probe.Budget(TimeSpan.FromSeconds(25));
                using (var configuration = new HmacSha1SlotConfiguration(key).RequireTouch())
                {
                    await probe.ProgramAsync(configuration, lifetime.Token);
                }
                var prompt = new RecordingPrompt(output);
                await using var session = await probe.OpenAsync(ConnectionType.HidOtp, prompt, lifetime.Token);
                using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                var elapsed = Stopwatch.StartNew();
                Task<ReadOnlyMemory<byte>> operation = session.CalculateHmacSha1Async(
                    Slot.Two, challenge, operationCancellation.Token);
                ReadOnlyMemory<byte> response = default;
                Exception? operationError = null;
                Exception? waitingError = null;
                try
                {
                    try
                    {
                        await prompt.DeviceWaiting.Task.WaitAsync(TimeSpan.FromSeconds(10), lifetime.Token);
                        output.WriteLine($"CANCEL: DeviceWaiting observed after {elapsed.Elapsed.TotalMilliseconds:F0} ms; cancelling caller token now; DO NOT TOUCH.");
                    }
                    catch (Exception exception)
                    {
                        waitingError = exception;
                    }
                    finally
                    {
                        // Drain the same operation before disposing its session; cancelling
                        // only a WaitAsync proxy would leave device I/O alive during cleanup.
                        operationCancellation.Cancel();
                    }
                    try
                    {
                        response = await operation;
                    }
                    catch (Exception exception)
                    {
                        operationError = exception;
                        LogException("cancel-operation", exception);
                    }
                    output.WriteLine($"CANCEL: operation drained after {elapsed.Elapsed.TotalMilliseconds:F0} ms.");
                    if (waitingError is not null)
                    {
                        if (operationError is not null)
                        {
                            throw new AggregateException("DeviceWaiting was not observed; original wait and drained-operation errors retained.", waitingError, operationError);
                        }
                        ExceptionDispatchInfo.Capture(waitingError).Throw();
                    }
                    Assert.False(lifetime.IsCancellationRequested, "Outer watchdog, not the deliberate cancellation, won.");
                    Assert.IsAssignableFrom<OperationCanceledException>(operationError);
                    AssertWaitingTerminal(prompt, UserPresenceOutcome.Cancelled);
                }
                finally
                {
                    ClearResponse(response);
                }
            }, recover: true);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(challenge);
            CryptographicOperations.ZeroMemory(key);
        }
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidOtp, RequireUsb = true, CustomFilter = typeof(Serial103Filter))]
    public async Task HidNaturalTimeout_WithoutTouch_ResolvesTimedOutOnce_AndReopens(YubiKeyTestState state)
    {
        byte[] key = NewHmacKey();
        byte[] challenge = [0x61, 0x62, 0x63, 0x64];
        try
        {
            await ExecuteAsync(state, "natural-timeout-no-touch", async probe =>
            {
                // Manual 3.3 section 4.3 describes 15 seconds on legacy firmware.
                // 65 seconds deliberately allows much more; modern timing is measured,
                // not inferred from Core implementation or asserted equal to 15 seconds.
                using var lifetime = probe.Budget(TimeSpan.FromSeconds(65));
                using (var configuration = new HmacSha1SlotConfiguration(key).RequireTouch())
                {
                    await probe.ProgramAsync(configuration, lifetime.Token);
                }
                var prompt = new RecordingPrompt(output);
                await using var session = await probe.OpenAsync(ConnectionType.HidOtp, prompt, lifetime.Token);
                ReadOnlyMemory<byte> response = default;
                Exception? operationError = null;
                var elapsed = Stopwatch.StartNew();
                output.WriteLine("TIMEOUT: no physical touch; outer token budget at most 65 seconds, total probe budget 80 seconds, VSTest host limit 90 seconds.");
                try
                {
                    try
                    {
                        response = await session.CalculateHmacSha1Async(Slot.Two, challenge, lifetime.Token);
                    }
                    catch (Exception exception)
                    {
                        operationError = exception;
                        LogException("natural-timeout-operation", exception);
                    }
                    output.WriteLine($"TIMEOUT: elapsed={elapsed.Elapsed.TotalMilliseconds:F0} ms; outer-cancelled={lifetime.IsCancellationRequested}.");
                    Assert.False(lifetime.IsCancellationRequested, "Outer token expired: this is not natural device/protocol touch-timeout evidence.");
                    Exception observed = Assert.IsAssignableFrom<Exception>(operationError);
                    // This specific internal exception is identified by the module's
                    // HidBackend catch, not by reading Core implementation or taking IVT.
                    Assert.Equal("OtpHidTouchTimeoutException", observed.GetType().Name);
                    AssertWaitingTerminal(prompt, UserPresenceOutcome.TimedOut);
                }
                finally
                {
                    ClearResponse(response);
                }
            }, recover: true);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(challenge);
            CryptographicOperations.ZeroMemory(key);
        }
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidOtp, RequireUsb = true, CustomFilter = typeof(Serial103Filter))]
    public async Task AesTicket_DecryptsSixByteChallenge_AndHasVendorCrcResidual(YubiKeyTestState state)
    {
        byte[] key = [0x10, 0x21, 0x32, 0x43, 0x54, 0x65, 0x76, 0x87, 0x98, 0xA9, 0xBA, 0xCB, 0xDC, 0xED, 0xFE, 0x0F];
        byte[] challenge = [0x31, 0x42, 0x53, 0x64, 0x75, 0x86];
        try
        {
            await ExecuteAsync(state, "aes-ticket", async probe =>
            {
                using var lifetime = probe.Budget(TimeSpan.FromSeconds(25));
                using (var configuration = new YubicoOtpChallengeResponseSlotConfiguration(key).RequireTouch(false))
                {
                    await probe.ProgramAsync(configuration, lifetime.Token);
                }
                var prompt = new RecordingPrompt(output);
                await using var session = await probe.OpenAsync(ConnectionType.HidOtp, prompt, lifetime.Token);
                ReadOnlyMemory<byte> first = default;
                ReadOnlyMemory<byte> second = default;
                try
                {
                    first = await session.CalculateYubicoOtpAsync(Slot.Two, challenge, lifetime.Token);
                    VerifyTicket(key, challenge, first);
                    second = await session.CalculateYubicoOtpAsync(Slot.Two, challenge, lifetime.Token);
                    VerifyTicket(key, challenge, second);
                    output.WriteLine($"AES: two exact-16-byte tickets passed challenge and CRC checks; repeated-challenge ciphertext-equal={CryptographicOperations.FixedTimeEquals(first.Span, second.Span)} (observation only, no randomness guarantee asserted).");
                    Assert.Empty(prompt.Requests);
                    Assert.Empty(prompt.Resolutions);
                }
                finally
                {
                    ClearResponse(second);
                    ClearResponse(first);
                }
            }, recover: false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(challenge);
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private async Task ExecuteAsync(YubiKeyTestState state, string lane, Func<Probe, Task> action, bool recover)
    {
        // Binding may perform authorized discovery, but never probe provisioning.
        Assert.Equal(103, state.SerialNumber);
        Assert.Equal(103, state.Device.SerialNumber);
        output.WriteLine($"LANE={lane}; serial={state.SerialNumber}; cached-firmware={state.FirmwareVersion}; version-name={state.DeviceInfo.VersionName}; qualifier={state.DeviceInfo.VersionQualifier}; exposed={state.AvailableConnections}; OS={RuntimeInformation.OSDescription}; UTC={DateTimeOffset.UtcNow:O}; DO NOT TOUCH.");
        var probe = new Probe(state.Device, output);
        await probe.InitializeAsync();
        var failures = new List<Exception>();
        try
        {
            await action(probe);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
            LogException("primary", exception);
        }
        finally
        {
            bool clean = !probe.MutationAttempted;
            if (probe.MutationAttempted)
            {
                try
                {
                    await probe.CleanupAsync();
                    clean = true;
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                    LogException("cleanup-failed-inspect-slot2-before-continuing", exception);
                }
            }
            if (recover && clean && probe.MutationAttempted)
            {
                try
                {
                    await probe.RecoveryAsync();
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                    LogException("reopen-recovery-failed", exception);
                }
            }
            output.WriteLine($"LANE={lane}; total-elapsed={probe.Elapsed.TotalMilliseconds:F0} ms; recorded-failures={failures.Count}.");
        }
        if (failures.Count == 1)
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }
        if (failures.Count > 1)
        {
            throw new AggregateException("Probe failure and/or cleanup/recovery failures; all originals retained.", failures);
        }
    }

    private void LogException(string phase, Exception exception)
    {
        // No payloads, key material, access codes, plaintext or exception Data.
        output.WriteLine($"ERROR phase={phase}; type={exception.GetType().FullName}; HResult=0x{exception.HResult:X8}.");
        if (exception is ApduException apdu)
        {
            string sw = apdu.SW is short value ? $"0x{unchecked((ushort)value):X4}" : "absent";
            output.WriteLine($"APDU phase={phase}; SW={sw}; CLA={apdu.Cla:X2}; INS={apdu.Ins:X2}; P1={apdu.P1:X2}; P2={apdu.P2:X2}.");
        }
    }

    private static byte[] NewHmacKey() =>
        [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
         0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13];

    private static void AssertWaitingTerminal(RecordingPrompt prompt, UserPresenceOutcome outcome)
    {
        UserPresenceContext request = Assert.Single(prompt.Requests);
        Assert.Equal(UserPresenceBasis.DeviceWaiting, request.Basis);
        Assert.Equal("YubiOTP", request.Application);
        Assert.Equal(Slot.Two.ToString(), request.Scope);
        var terminal = Assert.Single(prompt.Resolutions);
        Assert.Same(request, terminal.Context);
        Assert.Equal(outcome, terminal.Outcome);
        Assert.Equal(CancellationToken.None, terminal.Token);
    }

    private static void ClearResponse(ReadOnlyMemory<byte> memory)
    {
        if (MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment) && segment.Array is byte[] owner)
        {
            // Clear the entire returned allocation, including any HID CRC trailer
            // outside the exposed slice. Never clone a returned sensitive buffer.
            CryptographicOperations.ZeroMemory(owner);
        }
        else if (!memory.IsEmpty)
        {
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(memory).Span);
        }
    }

    private static void VerifyTicket(byte[] key, byte[] challenge, ReadOnlyMemory<byte> ticket)
    {
        Assert.Equal(16, ticket.Length);
        Span<byte> plaintext = stackalloc byte[16];
        try
        {
            using var aes = Aes.Create();
            aes.Key = key;
            Assert.Equal(16, aes.DecryptEcb(ticket.Span, plaintext, PaddingMode.None));
            Assert.True(CryptographicOperations.FixedTimeEquals(challenge, plaintext[..6]), "Decrypted zero-UID XOR challenge field did not match the six-byte challenge.");
            Assert.True(VendorCrc16(plaintext) == 0xF0B8, "Decrypted ticket did not have the vendor CRC16 residual.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private static ushort VendorCrc16(ReadOnlySpan<byte> ticket)
    {
        // Independent bitwise verifier from YubiKey Manual 3.3 section 6.3:
        // init FFFF; reflected polynomial 8408; residual F0B8 over all 16 bytes.
        // Deliberately do not call the SDK's ChecksumUtils used by its HID lane.
        uint crc = 0xFFFF;
        foreach (byte value in ticket)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0x8408 : crc >> 1;
            }
        }
        return (ushort)crc;
    }

    private sealed class RecordingPrompt(ITestOutputHelper output) : IUserPresencePrompt
    {
        public ConcurrentQueue<UserPresenceContext> Requests { get; } = new();
        public ConcurrentQueue<(UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken Token)> Resolutions { get; } = new();
        public TaskCompletionSource<UserPresenceContext> DeviceWaiting { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken)
        {
            Requests.Enqueue(context);
            output.WriteLine($"PROMPT request UTC={DateTimeOffset.UtcNow:O}; basis={context.Basis}; application={context.Application}; scope={context.Scope}; token-cancelled={cancellationToken.IsCancellationRequested}; DO NOT TOUCH.");
            if (context.Basis == UserPresenceBasis.DeviceWaiting)
            {
                DeviceWaiting.TrySetResult(context);
            }
            return default;
        }

        public ValueTask OnUserPresenceResolvedAsync(UserPresenceContext context, UserPresenceOutcome outcome, CancellationToken cancellationToken)
        {
            Resolutions.Enqueue((context, outcome, cancellationToken));
            output.WriteLine($"PROMPT resolution UTC={DateTimeOffset.UtcNow:O}; outcome={outcome}; token-is-none={cancellationToken == CancellationToken.None}.");
            return default;
        }
    }

    private sealed class Probe(IYubiKey device, ITestOutputHelper output)
    {
        private readonly Stopwatch _elapsed = Stopwatch.StartNew();
        private bool _initialSlotOne;
        public bool MutationAttempted { get; private set; }
        public TimeSpan Elapsed => _elapsed.Elapsed;

        public CancellationTokenSource Budget(TimeSpan maximum)
        {
            TimeSpan remaining = TimeSpan.FromSeconds(80) - _elapsed.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                throw new TimeoutException("The complete probe's 80-second cooperative budget is exhausted.");
            }
            return new CancellationTokenSource(remaining < maximum ? remaining : maximum);
        }

        public async Task<YubiOtpSession> OpenAsync(ConnectionType transport, RecordingPrompt? prompt, CancellationToken cancellationToken)
        {
            Assert.Equal(103, device.SerialNumber);
            var session = await device.CreateYubiOtpSessionAsync(new SessionCreationOptions
            {
                PreferredConnectionType = transport,
                UserPresencePrompt = prompt
            }, cancellationToken);
            try
            {
                Assert.Equal(transport, session.ConnectionType);
                output.WriteLine($"OPEN serial=103; transport={session.ConnectionType}; effective-firmware={session.FirmwareVersion}.");
                return session;
            }
            catch
            {
                await session.DisposeAsync();
                throw;
            }
        }

        public async Task InitializeAsync()
        {
            using var lifetime = Budget(TimeSpan.FromSeconds(10));
            await using var session = await OpenAsync(ConnectionType.HidOtp, null, lifetime.Token);
            Assert.Equal(103, await session.GetSerialNumberAsync(lifetime.Token));
            ConfigState status = session.GetConfigState();
            _initialSlotOne = status.IsConfigured(Slot.One);
            WriteStatus("initial-read-only", session, status);
            Assert.False(status.IsConfigured(Slot.Two), "Probe requires initially empty slot 2; no writes performed. Inspect any previous run's cleanup first.");
        }

        public async Task ProgramAsync(SlotConfiguration configuration, CancellationToken cancellationToken)
        {
            await using var session = await OpenAsync(ConnectionType.HidOtp, null, cancellationToken);
            // The dedicated slot began empty. Even an ambiguous failed write is ours
            // to inspect/delete; marking before await avoids stranded configurations.
            MutationAttempted = true;
            await session.PutConfigurationAsync(Slot.Two, configuration, cancellationToken: cancellationToken);
            Assert.True(session.GetConfigState().IsConfigured(Slot.Two));
        }

        public async Task SnapshotBothAsync(string label, bool configured, CancellationToken cancellationToken)
        {
            await SnapshotAsync(label, ConnectionType.HidOtp, configured, cancellationToken);
            await SnapshotAsync(label, ConnectionType.SmartCard, configured, cancellationToken);
        }

        private async Task SnapshotAsync(string label, ConnectionType transport, bool configured, CancellationToken cancellationToken)
        {
            await using var session = await OpenAsync(transport, null, cancellationToken);
            ConfigState status = session.GetConfigState();
            WriteStatus(label, session, status);
            Assert.Equal(configured, status.IsConfigured(Slot.Two));
            Assert.Equal(_initialSlotOne, status.IsConfigured(Slot.One));
        }

        private void WriteStatus(string label, YubiOtpSession session, ConfigState status) =>
            output.WriteLine($"STATUS label={label}; serial=103; transport={session.ConnectionType}; raw-firmware={status.FirmwareVersion}; slot1-configured={status.IsConfigured(Slot.One)}; slot1-touch-indicator={status.IsTouchTriggered(Slot.One)}; slot2-configured={status.IsConfigured(Slot.Two)}; slot2-touch-indicator={status.IsTouchTriggered(Slot.Two)}; led-inverted={status.IsLedInverted()}.");

        public async Task AssertHmacAsync(byte[] key, byte[] challenge, CancellationToken cancellationToken)
        {
            var prompt = new RecordingPrompt(output);
            await using var session = await OpenAsync(ConnectionType.HidOtp, prompt, cancellationToken);
            byte[] expected = HMACSHA1.HashData(key, challenge);
            ReadOnlyMemory<byte> response = default;
            try
            {
                response = await session.CalculateHmacSha1Async(Slot.Two, challenge, cancellationToken);
                Assert.Equal(20, response.Length);
                Assert.True(CryptographicOperations.FixedTimeEquals(expected, response.Span), "Explicit HID non-touch HMAC did not match the host known answer.");
                Assert.Empty(prompt.Requests);
                Assert.Empty(prompt.Resolutions);
                output.WriteLine("HMAC transport=HidOtp; known-answer=match; response-length=20; prompt-events=0.");
            }
            finally
            {
                ClearResponse(response);
                CryptographicOperations.ZeroMemory(expected);
            }
        }

        public async Task MeasureSmartCardHmacAsync(byte[] key, byte[] challenge, CancellationToken cancellationToken)
        {
            var prompt = new RecordingPrompt(output);
            await using var session = await OpenAsync(ConnectionType.SmartCard, prompt, cancellationToken);
            byte[] expected = HMACSHA1.HashData(key, challenge);
            ReadOnlyMemory<byte> response = default;
            UserPresenceOutcome expectedTerminal = UserPresenceOutcome.Completed;
            try
            {
                try
                {
                    response = await session.CalculateHmacSha1Async(Slot.Two, challenge, cancellationToken);
                }
                catch (ApduException exception) when (exception.SW is short sw && unchecked((ushort)sw) == 0x6985)
                {
                    expectedTerminal = UserPresenceOutcome.Failed;
                    output.WriteLine($"CAPABILITY transport=SmartCard; non-touch HMAC device-rejected; type={exception.GetType().FullName}; SW=0x6985; CLA={exception.Cla:X2}; INS={exception.Ins:X2}; P1={exception.P1:X2}; P2={exception.P2:X2}; NOT a successful HMAC/conformance result.");
                }
                if (expectedTerminal == UserPresenceOutcome.Completed)
                {
                    Assert.Equal(20, response.Length);
                    Assert.True(CryptographicOperations.FixedTimeEquals(expected, response.Span), "SmartCard returned data but it did not match the host HMAC.");
                    output.WriteLine("CAPABILITY transport=SmartCard; non-touch HMAC known-answer=match; response-length=20.");
                }
                // A notification is measured, not required. If present, verify its
                // complete lifecycle without equating PolicyRequires with real touch.
                if (prompt.Requests.IsEmpty)
                {
                    Assert.Empty(prompt.Resolutions);
                }
                else
                {
                    UserPresenceContext request = Assert.Single(prompt.Requests);
                    Assert.Equal("YubiOTP", request.Application);
                    Assert.Equal(Slot.Two.ToString(), request.Scope);
                    var terminal = Assert.Single(prompt.Resolutions);
                    Assert.Same(request, terminal.Context);
                    Assert.Equal(expectedTerminal, terminal.Outcome);
                    Assert.Equal(CancellationToken.None, terminal.Token);
                }
                output.WriteLine($"CAPABILITY SmartCard prompt-requests={prompt.Requests.Count}; terminal-events={prompt.Resolutions.Count}; expected-lane-terminal={expectedTerminal}.");
            }
            finally
            {
                ClearResponse(response);
                CryptographicOperations.ZeroMemory(expected);
            }
        }

        public async Task CleanupAsync()
        {
            var errors = new List<Exception>();
            ConnectionType[] transports = [ConnectionType.HidOtp, ConnectionType.SmartCard];
            foreach (ConnectionType transport in transports)
            {
                try
                {
                    // Independent of the failed operation's cancellation. Two bounded
                    // attempts, only public session/delete APIs, never raw reset reports.
                    using var lifetime = Budget(TimeSpan.FromSeconds(6));
                    await using (var session = await OpenAsync(transport, null, lifetime.Token))
                    {
                        ConfigState status = session.GetConfigState();
                        WriteStatus("cleanup-before", session, status);
                        if (status.IsConfigured(Slot.Two))
                        {
                            await session.DeleteSlotAsync(Slot.Two, cancellationToken: lifetime.Token);
                            Assert.False(session.GetConfigState().IsConfigured(Slot.Two));
                        }
                    }
                    // Dispose the deleting session before fresh HID status readback.
                    await SnapshotAsync("cleanup-fresh-readback", ConnectionType.HidOtp, false, lifetime.Token);
                    output.WriteLine($"CLEANUP verified slot2-empty via fresh HID; delete-attempt-transport={transport}; earlier-cleanup-errors={errors.Count}.");
                    return;
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                    output.WriteLine($"CLEANUP attempt transport={transport} failed; type={exception.GetType().FullName}; HResult=0x{exception.HResult:X8}; next attempt uses a new session.");
                }
            }
            throw new AggregateException("Slot 2 cleanup could not be verified; stop and inspect before continuing.", errors);
        }

        public async Task RecoveryAsync()
        {
            byte[] key = NewHmacKey();
            byte[] challenge = [0x71, 0x72, 0x73, 0x74];
            Exception? primary = null;
            try
            {
                using var lifetime = Budget(TimeSpan.FromSeconds(8));
                using (var configuration = new HmacSha1SlotConfiguration(key).RequireTouch(false))
                {
                    await ProgramAsync(configuration, lifetime.Token);
                }
                await AssertHmacAsync(key, challenge, lifetime.Token);
                output.WriteLine("RECOVERY: new explicit HID session/configuration completed a host-matched non-touch HMAC after failed/cancelled/timed-out operation disposal and cleanup.");
            }
            catch (Exception exception)
            {
                primary = exception;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(challenge);
                CryptographicOperations.ZeroMemory(key);
                try
                {
                    await CleanupAsync();
                }
                catch (Exception cleanup)
                {
                    if (primary is not null)
                    {
                        throw new AggregateException("Reopen-recovery and its cleanup both failed; originals retained.", primary, cleanup);
                    }
                    throw;
                }
            }
            if (primary is not null)
            {
                ExceptionDispatchInfo.Capture(primary).Throw();
            }
        }
    }
}
