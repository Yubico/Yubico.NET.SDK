using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.OpenPgp;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.AuditPgp001.Hardware.IntegrationTests;

public sealed partial class HardwareProbeTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, RequireUsb = true, MinFirmware = "5.6.0", CustomFilter = typeof(Serial103Filter))]
    public Task X25519Decipher_Rfc7748AliceBob_MatchesSharedSecret(YubiKeyTestState state) =>
        ProbeScope.RunAsync(state, output,
            "SDK X25519 Decipher with RFC 7748 Alice's imported key and Bob's public key returns the RFC shared secret", async (probe, token) =>
            {
                // Published RFC 7748 section 6.1 fixtures; treat private/derived
                // arrays as owned secrets even though these test values are public.
                byte[] alicePrivate = Convert.FromHexString("77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a");
                byte[] alicePublic = Convert.FromHexString("8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a");
                byte[] bobPublic = Convert.FromHexString("de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f");
                byte[] expected = Convert.FromHexString("4a5d9d5ba4ce2de1728e3bf480350f25e07e21c947d19e3376f09b3c1e161742");
                byte[] admin = [.. "12345678"u8];
                byte[] user = [.. "123456"u8];
                try
                {
                    await using var session = await probe.OpenAsync(null, token);
                    Assert.IsType<KdfNone>(await session.GetKdfAsync(token));
                    Assert.Equal(Uif.Off, await session.GetUifAsync(KeyRef.Dec, token));
                    await session.VerifyAdminAsync(admin, token);
                    // This is the canonical existing module test's public Core
                    // Curve25519 import API, including its owned-key disposal.
                    using var key = Curve25519PrivateKey.CreateFromValue(alicePrivate, KeyType.X25519);
                    await session.PutKeyAsync(KeyRef.Dec, key, cancellationToken: token);
                    Assert.Equal(KeyStatus.Imported, (await session.ListKeyInformationAsync(token))[KeyRef.Dec]);
                    Assert.Equal(CurveOid.X25519, Assert.IsType<EcAttributes>(await session.GetAlgorithmAttributesAsync(KeyRef.Dec, token)).Oid);
                    using var template = Tlv.Create((await session.GetPublicKeyAsync(KeyRef.Dec, token)).Span);
                    Assert.Equal(0x7F49, (int)template.Tag);
                    var fields = TlvHelper.DecodeDictionary(template.Value.Span);
                    Assert.True(alicePublic.AsSpan().SequenceEqual(fields[0x86].Span), "Imported Alice key must report the RFC public point before Decipher.");
                    output.WriteLine("X25519 fixture=RFC7748-section-6.1-Alice-Bob; imported-public-point=matched; next=SDK-DecryptAsync; no private/shared bytes logged");
                    await session.VerifyPinAsync(user, extended: true, cancellationToken: token);
                    var shared = await session.DecryptAsync(bobPublic, token);
                    try
                    {
                        Assert.True(CryptographicOperations.FixedTimeEquals(expected, shared.Span), "SDK Decipher must match the RFC 7748 shared secret, not merely return a nonempty response.");
                        output.WriteLine("X25519 SDK-Decipher=RFC-shared-secret-matched");
                    }
                    finally
                    {
                        ProbeScope.Clear(shared);
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(alicePrivate);
                    CryptographicOperations.ZeroMemory(alicePublic);
                    CryptographicOperations.ZeroMemory(bobPublic);
                    CryptographicOperations.ZeroMemory(expected);
                    CryptographicOperations.ZeroMemory(admin);
                    CryptographicOperations.ZeroMemory(user);
                    output.WriteLine("X25519 FINALLY owned-fixture-buffers=cleared; leave-Dec-key-for-vendor-reference; coordinator-must-run-reference-before-cleanup");
                }
            });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, RequireUsb = true, MinFirmware = "5.6.0", CustomFilter = typeof(Serial103Filter))]
    public async Task AttPublicKey_ReadOnly_MatchesPreservedIssuerSpki(YubiKeyTestState state)
    {
        // This read-only case deliberately bypasses RunAsync's reset setup.
        // It verifies no PIN and issues no key/configuration/reset command.
        Assert.Equal(103, state.SerialNumber);
        Assert.Equal(103, state.Device.SerialNumber);
        using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var token = lifetime.Token;
        var probe = new ProbeScope(state.Device, output);
        try
        {
            await using var session = await probe.OpenAsync(null, token);
            using var issuer = await session.GetCertificateAsync(KeyRef.Att, token);
            Assert.NotNull(issuer);
            using var issuerRsa = issuer.GetRSAPublicKey();
            Assert.NotNull(issuerRsa);
            output.WriteLine($"ATT_READ read-only=true reset=false authentication=false issuer-DER-SHA256={Convert.ToHexString(SHA256.HashData(issuer.RawDataMemory.Span))}; next=SDK-GetPublicKeyAsync-Att");
            var encoded = await session.GetPublicKeyAsync(KeyRef.Att, token);
            using var template = Tlv.Create(encoded.Span);
            Assert.Equal(0x7F49, (int)template.Tag);
            using var reported = ProbeScope.RsaPublic(encoded);
            Assert.Equal(issuerRsa.ExportSubjectPublicKeyInfo(), reported.ExportSubjectPublicKeyInfo());
            output.WriteLine("ATT_READ public-key-template=parsed-RSA; SDK-public-key-SPKI=preserved-issuer-SPKI; reset=false; key-replacement=false");
        }
        catch (Exception exception)
        {
            ProbeScope.LogError(output, "att-public-key-read-only", exception);
            throw;
        }
        finally
        {
            output.WriteLine("ATT_READ FINALLY read-only-session-disposed; no reset, PIN verification, generation, import or configuration write requested");
        }
    }
}
