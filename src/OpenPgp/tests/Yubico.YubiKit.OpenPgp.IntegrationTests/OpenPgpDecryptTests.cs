// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.OpenPgp.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.OpenPgp.IntegrationTests;

public class OpenPgpDecryptTests
{
    private const string DefaultUserPin = "123456";
    private const string DefaultAdminPin = "12345678";

    // ── RSA PKCS#1 Decrypt ──────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task Decrypt_RsaPkcs1_RecoverPlaintext(YubiKeyTestState state)
    {
        if (state.FirmwareVersion >= new FirmwareVersion(4, 2, 0) &&
            state.FirmwareVersion <= new FirmwareVersion(4, 3, 5))
        {
            return;
        }

        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);

                // Import an RSA 2048 key into the decryption slot
                using var rsa = RSA.Create(2048);
                var parameters = rsa.ExportParameters(includePrivateParameters: true);

                var template = new RsaKeyTemplate(
                    KeyRef.Dec,
                    e: parameters.Exponent!,
                    p: parameters.P!,
                    q: parameters.Q!);

                var attributes = RsaAttributes.Create(RsaSize.Rsa2048, RsaImportFormat.Standard);
                await session.PutKeyAsync(KeyRef.Dec, template, attributes);

                // Encrypt a message using the public key
                var plaintext = "Hello RSA decrypt"u8.ToArray();
                var ciphertext = rsa.Encrypt(plaintext, RSAEncryptionPadding.Pkcs1);

                // Decrypt on the YubiKey
                await session.VerifyPinAsync(DefaultUserPin, extended: true);
                var decrypted = await session.DecryptAsync(ciphertext);

                Assert.Equal(plaintext, decrypted.ToArray());
            });
    }

    // ── ECDH Decrypt (P-256) ────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task Decrypt_EcdhP256_ProducesSharedSecret(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);

                // Import an EC P-256 key into the decryption slot
                using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                var parameters = ecdh.ExportParameters(includePrivateParameters: true);

                var privateKey = parameters.D!;
                var publicKey = new byte[1 + parameters.Q.X!.Length + parameters.Q.Y!.Length];
                publicKey[0] = 0x04;
                parameters.Q.X.CopyTo(publicKey, 1);
                parameters.Q.Y.CopyTo(publicKey, 1 + parameters.Q.X.Length);

                var template = new EcKeyTemplate(KeyRef.Dec, privateKey, publicKey);
                var attributes = EcAttributes.Create(KeyRef.Dec, CurveOid.Secp256R1);
                await session.PutKeyAsync(KeyRef.Dec, template, attributes);

                // Generate an ephemeral key pair for the other side
                using var ephemeral = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                var ephemeralParams = ephemeral.ExportParameters(includePrivateParameters: false);
                var ephemeralPubKey = new byte[1 + ephemeralParams.Q.X!.Length + ephemeralParams.Q.Y!.Length];
                ephemeralPubKey[0] = 0x04;
                ephemeralParams.Q.X.CopyTo(ephemeralPubKey, 1);
                ephemeralParams.Q.Y.CopyTo(ephemeralPubKey, 1 + ephemeralParams.Q.X.Length);

                // Send the ephemeral public key to the card for ECDH
                await session.VerifyPinAsync(DefaultUserPin, extended: true);
                var sharedSecret = await session.DecryptAsync(ephemeralPubKey);

                // The shared secret should be a 32-byte value (P-256 x-coordinate)
                Assert.True(sharedSecret.Length > 0);
                Assert.True(sharedSecret.Length <= 32);
            });
}
