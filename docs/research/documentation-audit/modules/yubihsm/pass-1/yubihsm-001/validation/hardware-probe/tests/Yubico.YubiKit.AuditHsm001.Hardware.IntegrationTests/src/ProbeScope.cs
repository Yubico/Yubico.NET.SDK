using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Xunit.Abstractions;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.YubiHsm;

namespace Yubico.YubiKit.AuditHsm001.Hardware.IntegrationTests;

internal sealed class ProbeScope(IYubiKey device, ITestOutputHelper output)
{
    private readonly Stopwatch _elapsed = Stopwatch.StartNew();
    private bool _mutationAttempted;

    public static async Task ExecuteAsync(YubiKeyTestState state, ITestOutputHelper output,
        string lane, Func<ProbeScope, CancellationToken, Task> action)
    {
        // Validate the independent primitive before the placeholder can bind to hardware.
        SymmetricOracle.SelfTest();
        output.WriteLine("ORACLE RFC4493 vectors=4; primitive=self-tested; no device operation yet.");
        IYubiKey device = state.Device;
        Assert.Equal(103, state.SerialNumber);
        Assert.Equal(103, device.SerialNumber);
        output.WriteLine($"LANE={lane}; serial=103; cached-firmware={state.FirmwareVersion}; version-name={state.DeviceInfo.VersionName}; qualifier={state.DeviceInfo.VersionQualifier}; OS={RuntimeInformation.OSDescription}; UTC={DateTimeOffset.UtcNow:O}.");
        var probe = new ProbeScope(device, output);
        var failures = new List<Exception>();
        try
        {
            // Reserve the final 20 seconds of the case for independent cleanup attempts.
            using var body = probe.Budget(60);
            await probe.ResetAndReadbackAsync("start", body.Token);
            await action(probe, body.Token);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
            LogError(output, "body", exception);
        }
        finally
        {
            if (probe._mutationAttempted)
            {
                try
                {
                    await probe.CleanupAsync();
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                    LogError(output, "cleanup-stop-and-inspect", exception);
                }
            }
            output.WriteLine($"LANE={lane}; elapsed-ms={probe._elapsed.Elapsed.TotalMilliseconds:F0}; failures={failures.Count}; cleanup-scope=YubiHSM-Auth-applet-only.");
        }
        if (failures.Count == 1)
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }
        if (failures.Count > 1)
        {
            throw new AggregateException("Body/cleanup failures retained; inspect applet state before continuing.", failures);
        }
    }

    public CancellationTokenSource Budget(int maximumSeconds)
    {
        TimeSpan remaining = TimeSpan.FromSeconds(80) - _elapsed.Elapsed;
        if (remaining <= TimeSpan.Zero)
        {
            throw new TimeoutException("The complete lane's cooperative 80-second budget is exhausted.");
        }
        TimeSpan maximum = TimeSpan.FromSeconds(maximumSeconds);
        return new CancellationTokenSource(remaining < maximum ? remaining : maximum);
    }

    public async Task<HsmAuthSession> OpenAsync(RecordingPrompt? prompt, CancellationToken token)
    {
        Assert.Equal(103, device.SerialNumber);
        HsmAuthSession session = await device.CreateHsmAuthSessionAsync(new SessionCreationOptions
        {
            PreferredConnectionType = ConnectionType.SmartCard,
            UserPresencePrompt = prompt
        }, token);
        try
        {
            Assert.Equal(ConnectionType.SmartCard, session.ConnectionType);
            output.WriteLine($"OPEN serial=103; transport={session.ConnectionType}; session-effective-version={session.FirmwareVersion}.");
            return session;
        }
        catch
        {
            await session.DisposeAsync();
            throw;
        }
    }

    public async Task ResetAndReadbackAsync(string phase, CancellationToken token)
    {
        await using (HsmAuthSession reset = await OpenAsync(null, token))
        {
            if (phase == "start")
            {
                var before = await reset.ListCredentialsAsync(token);
                int retries = await reset.GetManagementKeyRetriesAsync(token);
                output.WriteLine($"RESET phase={phase}; before-count={before.Count}; management-retries={retries}; mutation=authorized-applet-reset.");
            }
            // Cleanup can reset even when a previous LIST failed.
            _mutationAttempted = true;
            await reset.ResetAsync(token);
        }
        // A new owned connection/session reads the reset result after disposal.
        await using HsmAuthSession readback = await OpenAsync(null, token);
        Assert.Empty(await readback.ListCredentialsAsync(token));
        Assert.Equal(8, await readback.GetManagementKeyRetriesAsync(token));
        output.WriteLine($"RESET phase={phase}; fresh-readback-empty=true; management-retries=8.");
    }

    private async Task CleanupAsync()
    {
        var errors = new List<Exception>();
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                using var lifetime = Budget(8);
                await ResetAndReadbackAsync($"finally-attempt-{attempt}", lifetime.Token);
                output.WriteLine($"CLEANUP verified=true; earlier-attempt-errors={errors.Count}.");
                return;
            }
            catch (Exception exception)
            {
                errors.Add(exception);
                LogError(output, $"cleanup-attempt-{attempt}", exception);
            }
        }
        throw new AggregateException("Fresh-session reset/readback exhausted; stop before another lane.", errors);
    }

    public static async Task AssertCredentialAsync(HsmAuthSession session, string label, int retries,
        bool touch, CancellationToken token)
    {
        HsmAuthCredential credential = Assert.Single(await session.ListCredentialsAsync(token));
        Assert.Equal(label, credential.Label);
        Assert.Equal(HsmAuthAlgorithm.Aes128YubicoAuthentication, credential.Algorithm);
        Assert.Equal(retries, credential.RetriesRemaining);
        Assert.Equal(touch, credential.TouchRequired);
        Assert.Equal(8, await session.GetManagementKeyRetriesAsync(token));
    }

    public static void LogError(ITestOutputHelper output, string phase, Exception exception)
    {
        output.WriteLine($"ERROR phase={phase}; type={exception.GetType().FullName}; HResult=0x{exception.HResult:X8}.");
        if (exception is ApduException apdu)
        {
            string sw = apdu.SW is short value ? $"0x{unchecked((ushort)value):X4}" : "absent";
            output.WriteLine($"APDU phase={phase}; SW={sw}; CLA={apdu.Cla:X2}; INS={apdu.Ins:X2}; P1={apdu.P1:X2}; P2={apdu.P2:X2}.");
        }
        if (exception is HsmAuthRetryException retry)
        {
            output.WriteLine($"RETRY phase={phase}; typed-retries={retry.RetriesRemaining}.");
        }
    }
}

internal sealed class RecordingPrompt(ITestOutputHelper output, bool inviteTouch = false) : IUserPresencePrompt
{
    public ConcurrentQueue<UserPresenceContext> Requests { get; } = new();
    public ConcurrentQueue<(UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken Token)> Resolutions { get; } = new();
    public TaskCompletionSource<UserPresenceContext> Requested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken)
    {
        Requests.Enqueue(context);
        output.WriteLine($"PROMPT request basis={context.Basis}; application={context.Application}; operation={context.Operation}; scope={context.Scope}; pre-command-policy=true; UTC={DateTimeOffset.UtcNow:O}; action={(inviteTouch ? "TOUCH THE KEY NOW" : "DO NOT TOUCH")}.");
        Requested.TrySetResult(context);
        if (inviteTouch)
        {
            Console.Error.WriteLine("YubiHSM Auth probe: touch serial 103 when its LED requests presence.");
        }
        return default;
    }

    public ValueTask OnUserPresenceResolvedAsync(UserPresenceContext context, UserPresenceOutcome outcome, CancellationToken cancellationToken)
    {
        Resolutions.Enqueue((context, outcome, cancellationToken));
        output.WriteLine($"PROMPT resolution outcome={outcome}; token-is-none={cancellationToken == CancellationToken.None}; UTC={DateTimeOffset.UtcNow:O}.");
        return default;
    }

    public void AssertTerminal(string label, UserPresenceOutcome outcome)
    {
        UserPresenceContext request = Assert.Single(Requests);
        Assert.Equal(UserPresenceBasis.PolicyRequires, request.Basis);
        Assert.Equal("YubiHSM Auth", request.Application);
        Assert.Equal(UserPresenceOperations.YubiHsmAuth.CalculateSessionKeysSymmetric, request.Operation);
        Assert.Equal(label, request.Scope);
        var resolution = Assert.Single(Resolutions);
        Assert.Same(request, resolution.Context);
        Assert.Equal(outcome, resolution.Outcome);
        Assert.Equal(CancellationToken.None, resolution.Token);
    }
}
