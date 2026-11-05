// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Piv;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace AuditPiv001.Hardware.IntegrationTests;

public sealed class CancellationProbe
{
    [SkippableTheory]
    [WithYubiKey(CustomFilter = typeof(Serial103Filter), MinFirmware = "5.3.0", ConnectionType = ConnectionType.SmartCard)]
    [Trait("Category", "RequiresHardware")]
    [Trait("Category", "RequiresUserPresence")]
    [Trait("Category", "Slow")]
    public async Task P07_CancelTouchWait_DrainThenFreshSameKeyRecovery(YubiKeyTestState state)
    {
        using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(150));
        CancellationToken ct = budget.Token;
        using var cancel = new CancellationTokenSource();
        var prompt = new PresenceRecorder("cancel-wait");
        byte[] digest = SHA256.HashData("Audit PIV001 cancellation same-key control"u8);
        string pending = Path.Combine(ProbeSupport.EvidenceDirectory, "recovery-pending.txt");
        Memory<byte> returned = default;
        Exception? terminal = null;
        ECPublicKey publicKey;
        try
        {
            await using (PivSession session = await ProbeSupport.OpenAsync(state, [Slot.Retired20], ct, prompt))
            {
                publicKey = Assert.IsType<ECPublicKey>(await session.GenerateKeyAsync(
                    Slot.Retired20, KeyType.P256, ProbeSupport.Policies(PinPolicy.Never, TouchPolicy.Always), ct));
                File.WriteAllText(pending, "Keep same-key recovery evidence before reset.\n");
                ProbeSupport.Event("P07-human-instruction", new { action = "Leave the key untouched during cancellation; wait for fresh-recovery prompt." });
                Task<Memory<byte>> operation = Task.Run(async () => await session.SignOrDecryptAsync(
                    Slot.Retired20, KeyType.P256, digest, cancel.Token), CancellationToken.None);
                // Request precedes the crypto APDU. The delay gives an untouched native wait
                // time to begin; it supplies timing evidence rather than a native-abort guarantee.
                await AwaitOrStopAsync(prompt.FirstRequest.Task, TimeSpan.FromSeconds(10), "request-not-observed");
                await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None);
                ProbeSupport.Event("P07-cancel-requested", new { completedBeforeCancellation = operation.IsCompleted,
                    timestamp = Stopwatch.GetTimestamp() });
                cancel.Cancel();
                await AwaitOrStopAsync(operation, TimeSpan.FromSeconds(45), "native-operation-undrained");
                try { returned = await operation; }
                catch (Exception error) { terminal = error; }
                bool returnedSignatureVerified = false;
                if (!returned.IsEmpty)
                {
                    using ECDsa verifier = ECDsa.Create(publicKey.Parameters);
                    returnedSignatureVerified = verifier.VerifyHash(digest, returned.Span, DSASignatureFormat.Rfc3279DerSequence);
                }
                ProbeSupport.Event("P07-terminal-operation", new { status = operation.Status.ToString(),
                    exception = terminal?.GetType().Name, returnedSignatureVerified,
                    requests = prompt.Requested.Count, resolutions = prompt.Resolved.Count });
            }
            ProbeSupport.Event("P07-original-session-disposed", new { resetIssued = false });
            var recovery = new PresenceRecorder("fresh-recovery-touch-now");
            // The original task is terminal and its connection disposed before this open.
            await using (PivSession fresh = await state.Device.CreatePivSessionAsync(new SessionCreationOptions
            {
                PreferredConnectionType = ConnectionType.SmartCard, UserPresencePrompt = recovery
            }, ct))
            {
                Assert.Equal(103, await fresh.GetSerialNumberAsync(ct));
                SlotMetadata? metadata = await fresh.GetSlotMetadataAsync(Slot.Retired20, ct);
                Assert.NotNull(metadata);
                using ECDsa metadataKey = metadata.Value.GetECDsaPublicKey();
                Assert.Equal(publicKey.ExportSubjectPublicKeyInfo(), metadataKey.ExportSubjectPublicKeyInfo());
                ProbeSupport.Event("P07-human-instruction", new { action = "Touch serial 103 for fresh-session same-key recovery now." });
                Task recoveryTask = Task.Run(async () => await ProbeSupport.VerifyP256Async(
                    fresh, Slot.Retired20, publicKey, digest, ct), CancellationToken.None);
                await AwaitOrStopAsync(recoveryTask, TimeSpan.FromSeconds(60), "fresh-same-key-recovery-undrained");
                await recoveryTask;
                ProbeSupport.Event("P07-fresh-same-key-host-verified", new { serial = 103, resetIssued = false });
            }
            // Rename the marker only after fresh-session host verification and disposal.
            File.Move(pending, Path.Combine(ProbeSupport.EvidenceDirectory, "recovery-completed.txt"));
            Assert.IsAssignableFrom<OperationCanceledException>(terminal);
            UserPresenceContext request = Assert.Single(prompt.Requested);
            Assert.Equal(UserPresenceBasis.PolicyRequires, request.Basis);
            Assert.Equal("PIV", request.Application);
            Assert.Equal(Slot.Retired20.ToString(), request.Scope);
            var resolution = Assert.Single(prompt.Resolved);
            Assert.Same(request, resolution.Context);
            Assert.Equal(UserPresenceOutcome.Cancelled, resolution.Outcome);
            Assert.Equal(CancellationToken.None, resolution.Token);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(returned.Span);
            CryptographicOperations.ZeroMemory(digest);
        }
    }

    private static async Task AwaitOrStopAsync(Task task, TimeSpan limit, string reason)
    {
        Task winner = await Task.WhenAny(task, Task.Delay(limit, CancellationToken.None));
        if (ReferenceEquals(winner, task)) return;
        ProbeSupport.Event("P07-drain-watchdog", new { reason, seconds = limit.TotalSeconds,
            disposition = "Terminate host; preserve pending marker; coordinator investigates before reset." });
        File.WriteAllText(Path.Combine(ProbeSupport.EvidenceDirectory, "drain-blocked.txt"), reason);
        // Process termination releases native resources without concurrently disposing an
        // active session or starting cleanup traffic. The wrapper retains the failed run.
        Environment.Exit(86);
        throw new TimeoutException("Drain watchdog terminated the probe host.");
    }

    private sealed class PresenceRecorder(string label) : IUserPresencePrompt
    {
        internal TaskCompletionSource FirstRequest { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal ConcurrentQueue<UserPresenceContext> Requested { get; } = new();
        internal ConcurrentQueue<(UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken Token)> Resolved { get; } = new();

        public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken)
        {
            Requested.Enqueue(context);
            ProbeSupport.Event("presence-request", new { label, context.Application, context.Scope,
                basis = context.Basis.ToString(), timestamp = Stopwatch.GetTimestamp() });
            FirstRequest.TrySetResult();
            return ValueTask.CompletedTask;
        }

        public ValueTask OnUserPresenceResolvedAsync(UserPresenceContext context, UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            Resolved.Enqueue((context, outcome, cancellationToken));
            ProbeSupport.Event("presence-resolution", new { label, outcome = outcome.ToString(),
                callbackTokenCancelled = cancellationToken.IsCancellationRequested, timestamp = Stopwatch.GetTimestamp() });
            return ValueTask.CompletedTask;
        }
    }
}
