// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Piv;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;
using Yubico.YubiKit.Tests.Shared;

namespace AuditPiv001.Hardware.IntegrationTests;

public sealed class Serial103Filter : IYubiKeyFilter
{
    public bool Matches(YubiKeyTestState device) => device.SerialNumber == 103;
    public string GetDescription() => "Dedicated PIV audit key 103";
}

internal static class ProbeSupport
{
    private static readonly Lock EventLock = new();
    internal static string EvidenceDirectory => Environment.GetEnvironmentVariable("AUDITPIV001_EVIDENCE")
        ?? throw new InvalidOperationException("Run through the serialized evidence wrapper.");

    internal static void Event(string stage, object detail)
    {
        lock (EventLock)
        {
            File.AppendAllText(Path.Combine(EvidenceDirectory, "probe-events.jsonl"),
                JsonSerializer.Serialize(new { utc = DateTimeOffset.UtcNow, stage, detail }) + Environment.NewLine);
        }
    }

    internal static async Task<PivSession> OpenAsync(
        YubiKeyTestState state, Slot[] emptySlots, CancellationToken ct, IUserPresencePrompt? prompt = null)
    {
        Assert.Equal(103, state.SerialNumber);
        foreach (Type type in new[] { typeof(HardwareProbeTests), typeof(PivSession), typeof(Signing), typeof(YubiKeyTestState) })
        {
            string path = type.Assembly.Location;
            Event("runtime-assembly", new { name = type.Assembly.GetName().Name, path,
                sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) });
        }
        PivSession session = await state.Device.CreatePivSessionAsync(new SessionCreationOptions
        {
            PreferredConnectionType = ConnectionType.SmartCard, UserPresencePrompt = prompt
        }, ct);
        try
        {
            Assert.Equal(103, await session.GetSerialNumberAsync(ct));
            ManagementKeyMetadata management = await session.GetManagementKeyMetadataAsync(ct);
            Assert.True(management.IsDefault);
            Assert.Equal(KeyType.Aes192, management.KeyType);
            Assert.Equal((byte)0x0A, management.RawAlgorithmId);
            Assert.Equal(TouchPolicy.Never, management.TouchPolicy);
            Assert.Equal(management.KeyType, session.ManagementKeyType);
            PinMetadata pin = await session.GetPinMetadataAsync(ct);
            Assert.True(pin.IsDefault);
            Assert.Equal(3, pin.RetriesRemaining);
            using X509Certificate2 issuer = await session.GetCertificateAsync(Slot.Attestation, ct)
                ?? throw new InvalidOperationException("Captured F9 certificate is missing.");
            Assert.Equal("FB2E0ABE368A14F434BDF76D398BB62DACA62CFA8478CC6C19FC2B48E4AA446A",
                Convert.ToHexString(SHA256.HashData(issuer.RawData)));
            foreach (Slot slot in emptySlots)
            {
                Assert.NotEqual(Slot.Attestation, slot);
                Assert.Null(await session.GetSlotMetadataAsync(slot, ct));
                using X509Certificate2? certificate = await session.GetCertificateAsync(slot, ct);
                Assert.Null(certificate);
            }
            byte[] managementKey = PivSession.DefaultManagementKey.ToArray();
            try { await session.AuthenticateAsync(managementKey, ct); }
            finally { CryptographicOperations.ZeroMemory(managementKey); }
            Event("prepared", new { serial = 103, managementAlgorithm = management.RawAlgorithmId,
                slots = emptySlots.Select(s => s.ToString()).ToArray() });
            return session;
        }
        catch
        {
            await session.DisposeAsync();
            throw;
        }
    }

    internal static async Task VerifyPinAsync(IPivSession session, CancellationToken ct)
    {
        byte[] pin = "123456"u8.ToArray();
        try { await session.VerifyPinAsync(pin, ct); }
        finally { CryptographicOperations.ZeroMemory(pin); }
    }

    internal static KeyCreationOptions Policies(PinPolicy pin = PinPolicy.Never, TouchPolicy touch = TouchPolicy.Never) =>
        new() { PinPolicy = pin, TouchPolicy = touch };

    internal static async Task VerifyP256Async(IPivSession session, Slot slot, ECPublicKey key,
        ReadOnlyMemory<byte> digest, CancellationToken ct)
    {
        Memory<byte> signature = default;
        try
        {
            signature = await session.SignOrDecryptAsync(slot, KeyType.P256, digest, ct);
            using ECDsa verifier = ECDsa.Create(key.Parameters);
            Assert.True(verifier.VerifyHash(digest.Span, signature.Span, DSASignatureFormat.Rfc3279DerSequence));
        }
        finally { CryptographicOperations.ZeroMemory(signature.Span); }
    }

    internal static void VerifyRsaCertificateSignature(X509Certificate2 certificate, X509Certificate2 issuer)
    {
        var reader = new AsnReader(certificate.RawData, AsnEncodingRules.DER);
        AsnReader sequence = reader.ReadSequence();
        ReadOnlyMemory<byte> tbs = sequence.ReadEncodedValue();
        AsnReader algorithm = sequence.ReadSequence();
        Assert.Equal("1.2.840.113549.1.1.11", algorithm.ReadObjectIdentifier());
        if (algorithm.HasData) algorithm.ReadNull();
        algorithm.ThrowIfNotEmpty();
        byte[] signature = sequence.ReadBitString(out int unusedBits);
        try
        {
            Assert.Equal(0, unusedBits);
            sequence.ThrowIfNotEmpty();
            reader.ThrowIfNotEmpty();
            using RSA rsa = issuer.GetRSAPublicKey() ?? throw new InvalidOperationException("F9 issuer must remain RSA.");
            Assert.True(rsa.VerifyData(tbs.Span, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            Assert.Equal(issuer.SubjectName.RawData, certificate.IssuerName.RawData);
        }
        finally { CryptographicOperations.ZeroMemory(signature); }
    }

    internal static byte[] Pkcs1Sha256(ReadOnlySpan<byte> message, int width)
    {
        byte[] block = new byte[width];
        ReadOnlySpan<byte> prefix = [0x30, 0x31, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86,
            0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05, 0x00, 0x04, 0x20];
        int separator = width - prefix.Length - 32 - 1;
        Assert.True(separator >= 10);
        block[1] = 1;
        block.AsSpan(2, separator - 2).Fill(0xFF);
        prefix.CopyTo(block.AsSpan(separator + 1));
        SHA256.HashData(message, block.AsSpan(width - 32));
        return block;
    }

    internal static async Task<bool> OpenSslVerifyAsync(byte[] spki, ReadOnlyMemory<byte> message,
        ReadOnlyMemory<byte> signature, string label, CancellationToken ct)
    {
        string fixtures = Environment.GetEnvironmentVariable("AUDITPIV001_FIXTURES")
            ?? throw new InvalidOperationException("The wrapper supplies the external public-fixture directory.");
        string prefix = Path.Combine(fixtures, label);
        await WritePublicAsync(prefix + ".spki.der", spki, ct);
        await WritePublicAsync(prefix + ".message.bin", message, ct);
        await WritePublicAsync(prefix + ".signature.bin", signature, ct);
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("openssl")
            {
                ArgumentList = { "pkeyutl", "-verify", "-rawin", "-pubin", "-keyform", "DER",
                    "-inkey", prefix + ".spki.der", "-in", prefix + ".message.bin",
                    "-sigfile", prefix + ".signature.bin" },
                RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false
            }
        };
        process.Start();
        Task<string> stdout = process.StandardOutput.ReadToEndAsync(ct);
        Task<string> stderr = process.StandardError.ReadToEndAsync(ct);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
        budget.CancelAfter(TimeSpan.FromSeconds(10));
        try { await process.WaitForExitAsync(budget.Token); }
        catch
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(stdout, stderr);
            throw;
        }
        string[] streams = await Task.WhenAll(stdout, stderr);
        Event("openssl-verification", new { label, exit = process.ExitCode, stdout = streams[0], stderr = streams[1] });
        return process.ExitCode == 0;
    }

    private static async Task WritePublicAsync(string path, ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        await using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await stream.WriteAsync(data, ct);
        }
        Event("public-fixture-identity", new { path, size = data.Length,
            sha256 = Convert.ToHexString(SHA256.HashData(data.Span)) });
    }
}
