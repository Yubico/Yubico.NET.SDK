using System.Security.Cryptography;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.OpenPgp;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.AuditPgp001.Hardware.IntegrationTests;

public sealed partial class HardwareProbeTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, RequireUsb = true, MinFirmware = "5.6.0", CustomFilter = typeof(Serial103Filter))]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public Task CancelSign_NoTouch_ObserveTerminalThenFreshRecoveryBeforeReset(YubiKeyTestState state) =>
        ProbeScope.RunAsync(state, output,
            "A no-touch Sign cancellation is observed through its actual terminal outcome, then a fresh session recovers before any reset", async (probe, token) =>
            {
                byte[] admin = [.. "12345678"u8];
                byte[] user = [.. "123456"u8];
                byte[] message = [.. "OpenPGP cancellation recovery proof"u8];
                Exception? terminal = null;
                var prompt = new RecordingPrompt(output, inviteTouch: false);
                try
                {
                    await using (var session = await probe.OpenAsync(prompt, token))
                    {
                        await session.VerifyAdminAsync(admin, token);
                        await session.GenerateKeyAsync(KeyRef.Sig, EcAttributes.Create(KeyRef.Sig, CurveOid.Secp256R1), token);
                        await session.SetUifAsync(KeyRef.Sig, Uif.On, token);
                        await session.VerifyPinAsync(user, cancellationToken: token);
                        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(token);
                        using var registration = cancel.Token.Register(() => output.WriteLine($"CANCEL requested elapsed-ms={prompt.Clock.ElapsedMilliseconds}; caller-request-only=true; DO NOT TOUCH"));
                        cancel.CancelAfter(TimeSpan.FromMilliseconds(1500));
                        output.WriteLine("CANCEL scheduled-after-ms=1500; awaiting-native-terminal=true; DO NOT TOUCH until recovery completes");
                        try
                        {
                            // Await the operation itself: WaitAsync would only bound
                            // this caller and could leave native work in flight.
                            var unexpected = await session.SignAsync(message, HashAlgorithmName.SHA256, cancel.Token);
                            ProbeScope.Clear(unexpected);
                            output.WriteLine("CANCEL terminal=success-unexpected-without-touch");
                        }
                        catch (Exception exception)
                        {
                            terminal = exception;
                            ProbeScope.LogError(output, "no-touch-terminal", exception);
                            output.WriteLine($"CANCEL terminal-observed elapsed-ms={prompt.Clock.ElapsedMilliseconds}; operation-task-awaited=true");
                        }
                    }

                    // This is a new owned connection after native completion and
                    // disposal. Recovery precedes external reset even after failure.
                    // A separate finite token permits recovery after caller cancellation.
                    using var recoveryLifetime = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                    var recoveryToken = recoveryLifetime.Token;
                    await using var recovery = await probe.OpenAsync(null, recoveryToken);
                    await ProbeScope.RecordPinsAsync(recovery, output, "fresh-recovery-before-any-reset", recoveryToken);
                    int counter = await recovery.GetSignatureCounterAsync(recoveryToken);
                    var policy = await recovery.GetUifAsync(KeyRef.Sig, recoveryToken);
                    output.WriteLine($"RECOVERY before-reset=true counter={counter} signature-UIF={policy}; next=admin-verify-then-UIF-Off-and-host-verified-sign");
                    Assert.Equal(Uif.On, policy);
                    await recovery.VerifyAdminAsync(admin, recoveryToken);
                    await recovery.SetUifAsync(KeyRef.Sig, Uif.Off, recoveryToken);
                    await recovery.VerifyPinAsync(user, cancellationToken: recoveryToken);
                    using var host = ProbeScope.EcPublic(await recovery.GetPublicKeyAsync(KeyRef.Sig, recoveryToken));
                    var signature = await recovery.SignAsync(message, HashAlgorithmName.SHA256, recoveryToken);
                    try
                    {
                        Assert.True(host.VerifyData(message, signature.Span, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence));
                    }
                    finally
                    {
                        ProbeScope.Clear(signature);
                    }
                    output.WriteLine("RECOVERY fresh-session=operational; post-cancel-signature=host-verified; reset-since-cancel=false");
                    Assert.NotNull(terminal);
                    prompt.AssertTerminal(UserPresenceOperations.OpenPgp.Sign,
                        terminal is OperationCanceledException ? UserPresenceOutcome.Cancelled : UserPresenceOutcome.Failed);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(admin);
                    CryptographicOperations.ZeroMemory(user);
                    CryptographicOperations.ZeroMemory(message);
                }
            });
}
