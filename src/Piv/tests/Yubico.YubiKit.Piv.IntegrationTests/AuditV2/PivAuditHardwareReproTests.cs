// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Diagnostics;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests.AuditV2;

public class PivAuditHardwareReproTests
{
    private static readonly byte[] ManagementKey =
    [
        1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8,
        1, 2, 3, 4, 5, 6, 7, 8
    ];

    // In-process approximation: separate independently opened PC/SC handles, not separate processes.
    // The process-local lease is released between connections; sharing across processes remains
    // an inference until a child-process test is run on a controlled reader.
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    [Trait("Audit", "YESDK-1630")]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task YESDK1630_PinOnceStateSurvivesConnectionDisposal(YubiKeyTestState state)
    {
        byte[] digest = SHA256.HashData("cross-connection pin state"u8);
        ECPublicKey publicKey;
        await using (var setup = await state.Device.CreatePivSessionAsync())
        {
            await setup.ResetAsync();
            await setup.AuthenticateAsync(ManagementKey);
            publicKey = (ECPublicKey)await setup.GenerateKeyAsync(PivSlot.Authentication,
                PivAlgorithm.EccP256,
                new PivKeyCreationOptions { PinPolicy = PivPinPolicy.Once, TouchPolicy = PivTouchPolicy.Never });
        }

        // The 6982 baseline proves the key is not usable without verification after reset.
        await using (var baseline = await state.Device.CreatePivSessionAsync())
        {
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                baseline.SignOrDecryptAsync(PivSlot.Authentication, PivAlgorithm.EccP256, digest));
            Assert.Contains("Security status not satisfied", exception.Message, StringComparison.Ordinal);
        }

        await using (var verifier = await state.Device.CreatePivSessionAsync())
        {
            await verifier.VerifyPinAsync("123456"u8.ToArray());
        }

        // This handle has never supplied a PIN. Desired behaviour under an isolation/release policy
        // (not a PIV spec requirement; see docs/audit-v2/findings/22-*): the verifier's disposal must
        // not leave PIN state usable by an unrelated handle. Today this signs successfully and
        // produces a signature that verifies against the slot key.
        await using var unverified = await state.Device.CreatePivSessionAsync();
        ReadOnlyMemory<byte> signature = ReadOnlyMemory<byte>.Empty;
        Exception? rejection = await Record.ExceptionAsync(async () =>
            signature = await unverified.SignOrDecryptAsync(PivSlot.Authentication, PivAlgorithm.EccP256, digest));
        if (rejection is null)
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKey.ExportSubjectPublicKeyInfo(), out _);
            bool verifies = ecdsa.VerifyHash(digest, signature.Span, DSASignatureFormat.Rfc3279DerSequence);
            Assert.Fail($"Unverified handle produced a signature without PIN (verifies against slot key: {verifies}).");
        }

        Assert.IsType<InvalidOperationException>(rejection);
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
    [Trait("Audit", "YESDK-1632")]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task YESDK1632_Ed25519SignsUnmodifiedShortAndLongMessages(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(ManagementKey);
        var publicKey = (Curve25519PublicKey)await session.GenerateKeyAsync(PivSlot.Authentication,
            PivAlgorithm.Ed25519,
            new PivKeyCreationOptions { PinPolicy = PivPinPolicy.Once, TouchPolicy = PivTouchPolicy.Never });
        await session.VerifyPinAsync("123456"u8.ToArray());

        foreach (int length in new[] { 10, 64 })
        {
            byte[] message = Enumerable.Range(1, length).Select(i => (byte)i).ToArray();
            ReadOnlyMemory<byte> signature = await session.SignOrDecryptAsync(
                PivSlot.Authentication, PivAlgorithm.Ed25519, message);
            Assert.Equal(64, signature.Length);
            await AssertOpenSslVerifiesAsync(publicKey.ExportSubjectPublicKeyInfo(), message, signature.ToArray());
        }
    }

    // OpenSSL verifies PureEd25519 independently; the runtime's ECDsa API cannot verify Ed25519.
    // This hardware test requires `openssl` on PATH (OpenSSL 3 supports `pkeyutl -rawin`).
    private static async Task AssertOpenSslVerifiesAsync(byte[] spki, byte[] message, byte[] signature)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"piv-audit-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            string publicPath = Path.Combine(directory, "public.pem");
            string messagePath = Path.Combine(directory, "message.bin");
            string signaturePath = Path.Combine(directory, "signature.bin");
            await File.WriteAllTextAsync(publicPath,
                $"-----BEGIN PUBLIC KEY-----\n{Convert.ToBase64String(spki, Base64FormattingOptions.InsertLineBreaks)}\n-----END PUBLIC KEY-----\n");
            await File.WriteAllBytesAsync(messagePath, message);
            await File.WriteAllBytesAsync(signaturePath, signature);

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo("openssl")
                {
                    ArgumentList = { "pkeyutl", "-verify", "-rawin", "-pubin", "-inkey", publicPath,
                        "-in", messagePath, "-sigfile", signaturePath },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            Assert.True(process.ExitCode == 0, $"OpenSSL verification failed: {output} {error}");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
