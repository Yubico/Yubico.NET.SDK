// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Piv;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace AuditPiv001.Hardware.IntegrationTests;

public sealed class HardwareProbeTests
{
    [SkippableTheory]
    [WithYubiKey(CustomFilter = typeof(Serial103Filter), MinFirmware = "5.7.0", ConnectionType = ConnectionType.SmartCard)]
    [Trait("Category", "RequiresHardware")]
    public async Task P01_X25519_Rfc7748AliceAndBobAgree(YubiKeyTestState state)
    {
        using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        CancellationToken ct = budget.Token;
        byte[] alice = Convert.FromHexString("77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a");
        byte[] expected = Convert.FromHexString("4a5d9d5ba4ce2de1728e3bf480350f25e07e21c947d19e3376f09b3c1e161742");
        byte[] publicAlice = Convert.FromHexString("8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a");
        byte[] publicBob = Convert.FromHexString("de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f");
        Memory<byte> actual = default;
        try
        {
            using Curve25519PrivateKey key = Curve25519PrivateKey.CreateFromValue(alice, KeyType.X25519);
            await using PivSession session = await ProbeSupport.OpenAsync(state, [Slot.KeyManagement], ct);
            await session.ImportKeyAsync(Slot.KeyManagement, key, ProbeSupport.Policies(PinPolicy.Once), ct);
            SlotMetadata? metadata = await session.GetSlotMetadataAsync(Slot.KeyManagement, ct);
            Assert.NotNull(metadata);
            Assert.Equal(KeyType.X25519, metadata.Value.KeyType);
            Assert.False(metadata.Value.IsGenerated);
            ReadOnlyMemory<byte> devicePublic = TlvHelper.GetValue(0x86, metadata.Value.PublicKey.Span);
            Assert.True(publicAlice.AsSpan().SequenceEqual(devicePublic.Span), "Imported scalar must produce RFC Alice public bytes.");
            await ProbeSupport.VerifyPinAsync(session, ct);
            Curve25519PublicKey peer = Curve25519PublicKey.CreateFromValue(publicBob, KeyType.X25519);
            actual = await session.CalculateSecretAsync(Slot.KeyManagement, peer, ct);
            Assert.Equal(32, actual.Length);
            Assert.True(CryptographicOperations.FixedTimeEquals(expected, actual.Span), "RFC7748 agreement mismatch; secret remains unlogged.");
            ProbeSupport.Event("P01-rfc7748-verified", new { slot = "9D", importedPublicMatches = true,
                sharedSecretMatches = true, encoding = "32-byte little-endian u-coordinate; no KDF" });
        }
        finally
        {
            CryptographicOperations.ZeroMemory(actual.Span);
            CryptographicOperations.ZeroMemory(alice);
            CryptographicOperations.ZeroMemory(expected);
        }
    }

    [SkippableTheory]
    [WithYubiKey(CustomFilter = typeof(Serial103Filter), MinFirmware = "5.3.0", ConnectionType = ConnectionType.SmartCard)]
    [Trait("Category", "RequiresHardware")]
    public async Task P02_PinAlwaysConsumesVerification_PinOnceReusesIt(YubiKeyTestState state)
    {
        using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        CancellationToken ct = budget.Token;
        byte[] digest = SHA256.HashData("Audit PIV001 PIN policy consumption"u8);
        Memory<byte> unexpected = default;
        try
        {
            await using PivSession session = await ProbeSupport.OpenAsync(state, [Slot.Signature, Slot.Authentication], ct);
            ECPublicKey always = Assert.IsType<ECPublicKey>(await session.GenerateKeyAsync(
                Slot.Signature, KeyType.P256, ProbeSupport.Policies(PinPolicy.Always), ct));
            ECPublicKey once = Assert.IsType<ECPublicKey>(await session.GenerateKeyAsync(
                Slot.Authentication, KeyType.P256, ProbeSupport.Policies(PinPolicy.Once), ct));
            await ProbeSupport.VerifyPinAsync(session, ct);
            await ProbeSupport.VerifyP256Async(session, Slot.Signature, always, digest, ct);
            Exception? rejection = await Record.ExceptionAsync(async () =>
                unexpected = await session.SignOrDecryptAsync(Slot.Signature, KeyType.P256, digest, ct));
            bool unexpectedValid = false;
            if (!unexpected.IsEmpty)
            {
                using ECDsa verifier = ECDsa.Create(always.Parameters);
                unexpectedValid = verifier.VerifyHash(digest, unexpected.Span, DSASignatureFormat.Rfc3279DerSequence);
            }
            ProbeSupport.Event("P02-second-always-operation", new { exception = rejection?.GetType().Name,
                producedSignature = !unexpected.IsEmpty, unexpectedValid,
                statusProvenance = "SDK maps raw 6982 to InvalidOperationException in PrivateKeyOperations.cs" });
            // Same-key recovery happens before the desired rejection assertion can fail.
            await ProbeSupport.VerifyPinAsync(session, ct);
            await ProbeSupport.VerifyP256Async(session, Slot.Signature, always, digest, ct);
            await ProbeSupport.VerifyPinAsync(session, ct);
            await ProbeSupport.VerifyP256Async(session, Slot.Authentication, once, digest, ct);
            await ProbeSupport.VerifyP256Async(session, Slot.Authentication, once, digest, ct);
            ProbeSupport.Event("P02-reverify-and-once-controls", new { reverifiedAlways = true, twoOnceSignatures = true });
            InvalidOperationException denied = Assert.IsType<InvalidOperationException>(rejection);
            Assert.Contains("Security status not satisfied", denied.Message, StringComparison.Ordinal);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(unexpected.Span);
            CryptographicOperations.ZeroMemory(digest);
        }
    }

    [SkippableTheory]
    [WithYubiKey(CustomFilter = typeof(Serial103Filter), MinFirmware = "5.7.0", ConnectionType = ConnectionType.SmartCard)]
    [Trait("Category", "RequiresHardware")]
    public async Task P03_MoveAndDeletePreserveIndependentCertificates(YubiKeyTestState state)
    {
        using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        CancellationToken ct = budget.Token;
        byte[] digest = SHA256.HashData("Audit PIV001 moved-key binding"u8);
        try
        {
            await using PivSession session = await ProbeSupport.OpenAsync(state, [Slot.Authentication, Slot.Retired1], ct);
            ECPublicKey generated = Assert.IsType<ECPublicKey>(await session.GenerateKeyAsync(
                Slot.Authentication, KeyType.P256, ProbeSupport.Policies(), ct));
            using ECDsa software = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var sourceRequest = new CertificateRequest("CN=Audit PIV001 unrelated source", software, HashAlgorithmName.SHA256);
            var targetRequest = new CertificateRequest("CN=Audit PIV001 unrelated target", software, HashAlgorithmName.SHA256);
            using X509Certificate2 source = sourceRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
            using X509Certificate2 target = targetRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
            Assert.False(generated.ExportSubjectPublicKeyInfo().AsSpan().SequenceEqual(software.ExportSubjectPublicKeyInfo()));
            await session.StoreCertificateAsync(Slot.Authentication, source, cancellationToken: ct);
            await session.StoreCertificateAsync(Slot.Retired1, target, cancellationToken: ct);
            await session.MoveKeyAsync(Slot.Authentication, Slot.Retired1, ct);
            Assert.Null(await session.GetSlotMetadataAsync(Slot.Authentication, ct));
            SlotMetadata? moved = await session.GetSlotMetadataAsync(Slot.Retired1, ct);
            Assert.NotNull(moved);
            using ECDsa extracted = moved.Value.GetECDsaPublicKey();
            Assert.Equal(generated.ExportSubjectPublicKeyInfo(), extracted.ExportSubjectPublicKeyInfo());
            await ProbeSupport.VerifyP256Async(session, Slot.Retired1, generated, digest, ct);
            await AssertCertificateBytesAsync(session, Slot.Authentication, source.RawData, ct);
            await AssertCertificateBytesAsync(session, Slot.Retired1, target.RawData, ct);
            await session.DeleteKeyAsync(Slot.Retired1, ct);
            Assert.Null(await session.GetSlotMetadataAsync(Slot.Retired1, ct));
            await AssertCertificateBytesAsync(session, Slot.Authentication, source.RawData, ct);
            await AssertCertificateBytesAsync(session, Slot.Retired1, target.RawData, ct);
            ProbeSupport.Event("P03-independent-objects-verified", new { source = "9A", destination = "82",
                movedKeyHostVerified = true, unrelatedCertificateBytesRetainedThroughMoveAndDelete = true });
        }
        finally { CryptographicOperations.ZeroMemory(digest); }
    }

    private static async Task AssertCertificateBytesAsync(IPivSession session, Slot slot, byte[] expected, CancellationToken ct)
    {
        using X509Certificate2 actual = await session.GetCertificateAsync(slot, ct)
            ?? throw new InvalidOperationException("Certificate disappeared during key-only operation.");
        Assert.Equal(expected, actual.RawData);
    }

    [SkippableTheory]
    [WithYubiKey(CustomFilter = typeof(Serial103Filter), MinFirmware = "5.3.0", ConnectionType = ConnectionType.SmartCard)]
    [Trait("Category", "RequiresHardware")]
    public async Task P04_AttestationBindsGeneratedKeyToPreservedIssuer(YubiKeyTestState state)
    {
        using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        CancellationToken ct = budget.Token;
        await using PivSession session = await ProbeSupport.OpenAsync(state, [Slot.Authentication], ct);
        using X509Certificate2 issuerBefore = await session.GetCertificateAsync(Slot.Attestation, ct)
            ?? throw new InvalidOperationException("Preserved issuer is missing.");
        ECPublicKey generated = Assert.IsType<ECPublicKey>(await session.GenerateKeyAsync(
            Slot.Authentication, KeyType.P256, ProbeSupport.Policies(), ct));
        using X509Certificate2 attested = await session.AttestKeyAsync(Slot.Authentication, ct);
        ProbeSupport.VerifyRsaCertificateSignature(attested, issuerBefore);
        using ECDsa attestedKey = attested.GetECDsaPublicKey() ?? throw new InvalidOperationException("Attested key must be P256.");
        Assert.Equal(generated.ExportSubjectPublicKeyInfo(), attestedKey.ExportSubjectPublicKeyInfo());
        SlotMetadata? metadata = await session.GetSlotMetadataAsync(Slot.Authentication, ct);
        Assert.NotNull(metadata);
        using ECDsa metadataKey = metadata.Value.GetECDsaPublicKey();
        Assert.Equal(generated.ExportSubjectPublicKeyInfo(), metadataKey.ExportSubjectPublicKeyInfo());
        using X509Certificate2 issuerAfter = await session.GetCertificateAsync(Slot.Attestation, ct)
            ?? throw new InvalidOperationException("Preserved issuer disappeared.");
        Assert.Equal(issuerBefore.RawData, issuerAfter.RawData);
        ProbeSupport.Event("P04-attestation-verified", new { generatedSlot = "9A", issuerSlot = "F9",
            issuerUnchanged = true, certificateSignatureValid = true, keySpkiMatches = true, manufacturerTrustEvaluated = false });
    }

    [SkippableTheory]
    [WithYubiKey(CustomFilter = typeof(Serial103Filter), MinFirmware = "5.3.0", ConnectionType = ConnectionType.SmartCard)]
    [Trait("Category", "RequiresHardware")]
    [Trait("AuditFinding", "PIVIMP-005")]
    public async Task P05_ActualPivToolRsaSignsOriginalMessage(YubiKeyTestState state)
    {
        using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        CancellationToken ct = budget.Token;
        byte[] message = "Audit PIV001 PivTool RSA original message"u8.ToArray();
        byte[] original = (byte[])message.Clone();
        byte[] encoded = ProbeSupport.Pkcs1Sha256(message, 256);
        Memory<byte> control = default;
        try
        {
            await using PivSession session = await ProbeSupport.OpenAsync(state, [Slot.Signature], ct);
            RSAPublicKey key = Assert.IsType<RSAPublicKey>(await session.GenerateKeyAsync(
                Slot.Signature, KeyType.Rsa2048, ProbeSupport.Policies(PinPolicy.Once), ct));
            await ProbeSupport.VerifyPinAsync(session, ct);
            using RSA verifier = RSA.Create();
            verifier.ImportSubjectPublicKeyInfo(key.ExportSubjectPublicKeyInfo(), out _);
            control = await session.SignOrDecryptAsync(Slot.Signature, KeyType.Rsa2048, encoded, ct);
            Assert.True(verifier.VerifyData(message, control.Span, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            var result = await Signing.SignDataAsync(session, Slot.Signature, message, HashAlgorithmName.SHA256, ct);
            bool verifies = result.Success && verifier.VerifyData(message, result.Signature.Span,
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            ProbeSupport.Event("P05-actual-sample-result", new { rawSdkControlVerified = true,
                sampleReportedSuccess = result.Success, originalMessageSignatureVerified = verifies,
                callerBytesPreserved = message.AsSpan().SequenceEqual(original), resultMemory = "sample-owned read-only signature; no writable cast" });
            Assert.Equal(original, message);
            Assert.True(result.Success, "Actual sample helper should return a signature for the provisioned key.");
            Assert.True(verifies, "PIVIMP-005: actual PivTool RSA result must verify as SHA256/PKCS1 over the original message.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(control.Span);
            CryptographicOperations.ZeroMemory(encoded);
            CryptographicOperations.ZeroMemory(message);
            CryptographicOperations.ZeroMemory(original);
        }
    }

    [SkippableTheory]
    [WithYubiKey(CustomFilter = typeof(Serial103Filter), MinFirmware = "5.7.0", ConnectionType = ConnectionType.SmartCard)]
    [Trait("Category", "RequiresHardware")]
    [Trait("AuditFinding", "PIVIMP-005")]
    public async Task P06_ActualPivToolEd25519SignsOriginalMessage(YubiKeyTestState state)
    {
        using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        CancellationToken ct = budget.Token;
        byte[] message = "Audit PIV001 PivTool PureEd25519 original message"u8.ToArray();
        byte[] original = (byte[])message.Clone();
        byte[] hashedMessage = SHA256.HashData(message);
        Memory<byte> control = default;
        try
        {
            await using PivSession session = await ProbeSupport.OpenAsync(state, [Slot.Signature], ct);
            Curve25519PublicKey key = Assert.IsType<Curve25519PublicKey>(await session.GenerateKeyAsync(
                Slot.Signature, KeyType.Ed25519, ProbeSupport.Policies(PinPolicy.Once), ct));
            await ProbeSupport.VerifyPinAsync(session, ct);
            control = await session.SignOrDecryptAsync(Slot.Signature, KeyType.Ed25519, message, ct);
            Assert.True(await ProbeSupport.OpenSslVerifyAsync(key.ExportSubjectPublicKeyInfo(), message, control, "P06-sdk-control", ct));
            var result = await Signing.SignDataAsync(session, Slot.Signature, message, HashAlgorithmName.SHA256, ct);
            bool verifies = result.Success && await ProbeSupport.OpenSslVerifyAsync(
                key.ExportSubjectPublicKeyInfo(), message, result.Signature, "P06-sample-original", ct);
            bool signsHash = result.Success && await ProbeSupport.OpenSslVerifyAsync(
                key.ExportSubjectPublicKeyInfo(), hashedMessage, result.Signature, "P06-sample-hash-control", ct);
            ProbeSupport.Event("P06-actual-sample-result", new { rawSdkControlVerified = true,
                sampleReportedSuccess = result.Success, originalMessageSignatureVerified = verifies, sampleSignsHash = signsHash,
                callerBytesPreserved = message.AsSpan().SequenceEqual(original), resultMemory = "sample-owned read-only signature; no writable cast" });
            Assert.Equal(original, message);
            Assert.True(result.Success, "Actual sample helper should return a signature for the provisioned key.");
            Assert.True(verifies, "PIVIMP-005: actual PivTool Ed25519 result must verify over the original unmodified message.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(control.Span);
            CryptographicOperations.ZeroMemory(message);
            CryptographicOperations.ZeroMemory(original);
            CryptographicOperations.ZeroMemory(hashedMessage);
        }
    }
}
