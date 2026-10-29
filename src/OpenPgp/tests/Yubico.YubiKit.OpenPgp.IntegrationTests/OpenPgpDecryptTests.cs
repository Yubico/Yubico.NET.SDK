// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.OpenPgp.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.OpenPgp.IntegrationTests;

public class OpenPgpDecryptTests
{
    private static readonly byte[] DefaultUserPin = "123456"u8.ToArray();
    private static readonly byte[] DefaultAdminPin = "12345678"u8.ToArray();

    // ── RSA PKCS#1 Decrypt ──────────────────────────────────────────

    [SkippableTheory]
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
                using var key = RSAPrivateKey.CreateFromParameters(rsa.ExportParameters(includePrivateParameters: true));
                await session.PutKeyAsync(KeyRef.Dec, key);

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

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task Decrypt_EcdhP256_ProducesSharedSecret(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);

                // Import an EC P-256 key into the decryption slot
                using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                using var key = ECPrivateKey.CreateFromEcdh(ecdh);
                await session.PutKeyAsync(KeyRef.Dec, key);

                // Generate an ephemeral key pair for the other side
                using var ephemeral = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                var ephemeralParams = ephemeral.ExportParameters(includePrivateParameters: false);
                var ephemeralPubKey = new byte[1 + ephemeralParams.Q.X!.Length + ephemeralParams.Q.Y!.Length];
                ephemeralPubKey[0] = 0x04;
                ephemeralParams.Q.X.CopyTo(ephemeralPubKey, 1);
                ephemeralParams.Q.Y.CopyTo(ephemeralPubKey, 1 + ephemeralParams.Q.X.Length);

                // The card returns the shared x-coordinate, which must equal what the host derives with the same two keys.
                await session.VerifyPinAsync(DefaultUserPin, extended: true);
                var sharedSecret = await session.DecryptAsync(ephemeralPubKey);
                Assert.Equal(ephemeral.DeriveRawSecretAgreement(ecdh.PublicKey), sharedSecret.ToArray());
            });
}