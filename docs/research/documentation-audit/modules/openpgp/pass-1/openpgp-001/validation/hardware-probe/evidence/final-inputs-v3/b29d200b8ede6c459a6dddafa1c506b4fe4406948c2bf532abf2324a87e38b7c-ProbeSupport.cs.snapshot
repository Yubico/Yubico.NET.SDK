using System.Collections.Concurrent;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit.Abstractions;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.OpenPgp;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.AuditPgp001.Hardware.IntegrationTests;

internal sealed class ProbeScope(IYubiKey device, ITestOutputHelper output)
{
    public async Task<OpenPgpSession> OpenAsync(RecordingPrompt? prompt, CancellationToken token)
    {
        Assert.Equal(103, device.SerialNumber);
        var session = await device.CreateOpenPgpSessionAsync(new SessionCreationOptions
        {
            PreferredConnectionType = ConnectionType.SmartCard,
            UserPresencePrompt = prompt,
        }, token);
        try
        {
            Assert.Equal(ConnectionType.SmartCard, session.ConnectionType);
            output.WriteLine($"OPEN serial=103 transport={session.ConnectionType} effective-firmware={session.FirmwareVersion}");
            return session;
        }
        catch
        {
            await session.DisposeAsync();
            throw;
        }
    }

    public static async Task RunAsync(YubiKeyTestState state, ITestOutputHelper output,
        string hypothesis, Func<ProbeScope, CancellationToken, Task> action, bool requireKdf = false)
    {
        Assert.Equal(103, state.SerialNumber);
        var scope = new ProbeScope(state.Device, output);
        output.WriteLine($"HYPOTHESIS {hypothesis}; serial=103 version-name={state.DeviceInfo.VersionName} UTC={DateTimeOffset.UtcNow:O}");
        using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        try
        {
            await using (var reset = await scope.OpenAsync(null, lifetime.Token))
            {
                var data = await reset.GetApplicationRelatedDataAsync(lifetime.Token);
                bool advertised = data.Discretionary.ExtendedCapabilities.Flags.HasFlag(ExtendedCapabilityFlags.Kdf);
                output.WriteLine($"PREMUTATION applet-KDF-capability={advertised}; gate=application-related-data");
                if (requireKdf)
                {
                    Assert.True(advertised, "The applet must advertise KDF before this probe mutates its configuration.");
                }
                await reset.ResetAsync(lifetime.Token);
            }
            await action(scope, lifetime.Token);
        }
        catch (Exception exception)
        {
            LogError(output, "body", exception);
            throw;
        }
        finally
        {
            // The coordinator runner invokes independent vendor cleanup after all
            // .NET sessions have disposed, including when a probe assertion fails.
            output.WriteLine("FINALLY sessions-disposing=true; independent-vendor-cleanup=coordinator-runner-finally; preserve-original-failure=true");
        }
    }

    public static async Task RecordPinsAsync(OpenPgpSession session, ITestOutputHelper output,
        string phase, CancellationToken token)
    {
        var status = await session.GetPinStatusAsync(token);
        output.WriteLine($"PINS phase={phase} user={status.AttemptsUser} reset={status.AttemptsReset} admin={status.AttemptsAdmin} policy={status.SignaturePinPolicy}");
    }

    public static void LogError(ITestOutputHelper output, string phase, Exception exception)
    {
        output.WriteLine($"ERROR phase={phase} type={exception.GetType().FullName} HResult=0x{exception.HResult:X8}");
        if (exception is ApduException apdu)
        {
            string sw = apdu.SW is short value ? $"0x{unchecked((ushort)value):X4}" : "absent";
            output.WriteLine($"APDU phase={phase} SW={sw} CLA={apdu.Cla:X2} INS={apdu.Ins:X2} P1={apdu.P1:X2} P2={apdu.P2:X2}");
        }
        if (exception is OpenPgpInvalidPinException pin)
        {
            output.WriteLine($"PIN_ERROR phase={phase} typed-retries={pin.RetriesRemaining}");
        }
    }

    public static ECDsa EcPublic(ReadOnlyMemory<byte> encoded)
    {
        using var outer = Tlv.Create(encoded.Span);
        var fields = TlvHelper.DecodeDictionary(outer.Value.Span);
        var point = fields[0x86];
        Assert.Equal(65, point.Length);
        Assert.Equal(4, point.Span[0]);
        // Public coordinate arrays escape into the framework ECParameters object.
        return ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = point.Slice(1, 32).ToArray(), Y = point.Slice(33, 32).ToArray() },
        });
    }

    public static RSA RsaPublic(ReadOnlyMemory<byte> encoded)
    {
        using var outer = Tlv.Create(encoded.Span);
        var fields = TlvHelper.DecodeDictionary(outer.Value.Span);
        var rsa = RSA.Create();
        try
        {
            // Public integer arrays escape into the framework RSAParameters object.
            rsa.ImportParameters(new RSAParameters { Modulus = fields[0x81].ToArray(), Exponent = fields[0x82].ToArray() });
            return rsa;
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    public static void VerifyCertificateSignature(X509Certificate2 certificate, X509Certificate2 issuer)
    {
        var reader = new AsnReader(certificate.RawDataMemory, AsnEncodingRules.DER);
        var sequence = reader.ReadSequence();
        var tbs = sequence.ReadEncodedValue();
        var algorithm = sequence.ReadSequence();
        var oid = algorithm.ReadObjectIdentifier();
        var signature = sequence.ReadBitString(out int unused);
        Assert.Equal(0, unused);
        sequence.ThrowIfNotEmpty();
        reader.ThrowIfNotEmpty();
        using var ec = issuer.GetECDsaPublicKey();
        using var rsa = issuer.GetRSAPublicKey();
        bool valid = oid switch
        {
            "1.2.840.10045.4.3.2" when ec is not null => ec.VerifyData(tbs.Span, signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence),
            "1.2.840.10045.4.3.3" when ec is not null => ec.VerifyData(tbs.Span, signature, HashAlgorithmName.SHA384, DSASignatureFormat.Rfc3279DerSequence),
            "1.2.840.113549.1.1.11" when rsa is not null => rsa.VerifyData(tbs.Span, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
            _ => throw new NotSupportedException($"Record issuer signature algorithm {oid} before extending the oracle."),
        };
        Assert.True(valid, "The certificate signature must verify under the preserved attestation issuer public key.");
    }

    public static void Clear(ReadOnlyMemory<byte> owned) =>
        CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(owned).Span);
}

internal sealed class RecordingPrompt(ITestOutputHelper output, bool inviteTouch) : IUserPresencePrompt
{
    public ConcurrentQueue<UserPresenceContext> Requests { get; } = new();
    public ConcurrentQueue<(UserPresenceContext Context, UserPresenceOutcome Outcome)> Resolutions { get; } = new();
    public TaskCompletionSource<UserPresenceContext> Requested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Stopwatch Clock { get; } = Stopwatch.StartNew();

    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken)
    {
        Requests.Enqueue(context);
        output.WriteLine($"PRESENCE requested operation={context.Operation} basis={context.Basis} elapsed-ms={Clock.ElapsedMilliseconds} action={(inviteTouch ? "TOUCH WHEN LED FLASHES" : "DO NOT TOUCH")}");
        if (inviteTouch)
        {
            Console.Error.WriteLine($"OpenPGP serial 103: touch for {context.Operation} when the LED flashes.");
        }
        Requested.TrySetResult(context);
        return default;
    }

    public ValueTask OnUserPresenceResolvedAsync(UserPresenceContext context, UserPresenceOutcome outcome, CancellationToken cancellationToken)
    {
        Resolutions.Enqueue((context, outcome));
        output.WriteLine($"PRESENCE resolved outcome={outcome} elapsed-ms={Clock.ElapsedMilliseconds} token-none={cancellationToken == CancellationToken.None}");
        return default;
    }

    public void AssertTerminal(string operation, UserPresenceOutcome outcome)
    {
        var request = Assert.Single(Requests);
        Assert.Equal(operation, request.Operation);
        Assert.Equal(UserPresenceBasis.PolicyRequires, request.Basis);
        var terminal = Assert.Single(Resolutions);
        Assert.Same(request, terminal.Context);
        Assert.Equal(outcome, terminal.Outcome);
    }
}
