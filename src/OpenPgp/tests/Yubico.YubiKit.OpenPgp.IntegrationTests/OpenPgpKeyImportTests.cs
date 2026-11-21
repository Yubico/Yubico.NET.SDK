// Copyright 2025 Yubico AB
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.OpenPgp.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.OpenPgp.IntegrationTests;

/// <summary>
///     PutKeyAsync round trips: the card must hold the key the host generated, shown by verifying the
///     card's signature with the host's public key, or by comparing the card's public key with a known
///     test vector's.
/// </summary>
public class OpenPgpKeyImportTests
{
    private static readonly byte[] DefaultUserPin = "123456"u8.ToArray();
    private static readonly byte[] DefaultAdminPin = "12345678"u8.ToArray();

    // RFC 8032 §7.1, test 1: the 32-byte private input is a seed.
    private static readonly byte[] Ed25519TestPrivateKey = Convert.FromHexString(
        "9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60");
    private static readonly byte[] Ed25519TestPublicKey = Convert.FromHexString(
        "d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a");

    // RFC 7748 §6.1, Alice
    private static readonly byte[] X25519TestPrivateKey = Convert.FromHexString(
        "77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a");
    private static readonly byte[] X25519TestPublicKey = Convert.FromHexString(
        "8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a");

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task ImportRsaKey_2048_ThenSign_VerifiesWithTheHostPublicKey(YubiKeyTestState state)
    {
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                using var rsa = RSA.Create(2048);
                using var key = RSAPrivateKey.CreateFromParameters(rsa.ExportParameters(includePrivateParameters: true));

                await session.PutKeyAsync(KeyRef.Sig, key);

                var keyInfo = await session.ListKeyInformationAsync();
                Assert.Equal(KeyStatus.Imported, keyInfo[KeyRef.Sig]);
                Assert.Equal(RsaImportFormat.Standard, Assert.IsType<RsaAttributes>(await session.GetAlgorithmAttributesAsync(KeyRef.Sig)).ImportFormat);

                var message = "Import RSA test"u8.ToArray();
                await session.VerifyPinAsync(DefaultUserPin);
                var signature = await session.SignAsync(message, HashAlgorithmName.SHA256);
                Assert.True(rsa.VerifyData(message, signature.Span, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            });
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task ImportEcKey_P256_ThenSign_VerifiesWithTheHostPublicKey(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                using var key = ECPrivateKey.CreateFromParameters(ecdsa.ExportParameters(includePrivateParameters: true));

                await session.PutKeyAsync(KeyRef.Sig, key);

                var keyInfo = await session.ListKeyInformationAsync();
                Assert.Equal(KeyStatus.Imported, keyInfo[KeyRef.Sig]);
                Assert.Equal(KeyType.P256, Assert.IsType<EcAttributes>(await session.GetAlgorithmAttributesAsync(KeyRef.Sig)).KeyType);

                var message = "Import EC P256 test"u8.ToArray();
                await session.VerifyPinAsync(DefaultUserPin);
                var signature = await session.SignAsync(message, HashAlgorithmName.SHA256);
                Assert.True(ecdsa.VerifyData(message, signature.Span, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence));
            });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task ImportEcKey_P384_ThenSign_VerifiesWithTheHostPublicKey(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
                using var key = ECPrivateKey.CreateFromParameters(ecdsa.ExportParameters(includePrivateParameters: true));

                await session.PutKeyAsync(KeyRef.Sig, key);

                Assert.Equal(KeyStatus.Imported, (await session.ListKeyInformationAsync())[KeyRef.Sig]);

                var message = "Import EC P384 test"u8.ToArray();
                await session.VerifyPinAsync(DefaultUserPin);
                var signature = await session.SignAsync(message, HashAlgorithmName.SHA384);
                Assert.True(ecdsa.VerifyData(message, signature.Span, HashAlgorithmName.SHA384, DSASignatureFormat.Rfc3279DerSequence));
            });

    /// <summary>Compares the card public key with RFC 8032's public key for the imported seed; no signing is exercised.</summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task ImportEcKey_Ed25519_CardReportsTheTestVectorPublicKey(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                using var key = Curve25519PrivateKey.CreateFromValue(Ed25519TestPrivateKey, KeyType.Ed25519);

                await session.PutKeyAsync(KeyRef.Sig, key);

                Assert.Equal(KeyStatus.Imported, (await session.ListKeyInformationAsync())[KeyRef.Sig]);
                Assert.Equal(KeyType.Ed25519, Assert.IsType<EcAttributes>(await session.GetAlgorithmAttributesAsync(KeyRef.Sig)).KeyType);
                Assert.Equal(Ed25519TestPublicKey, await ReadEcPublicPointAsync(session, KeyRef.Sig));
            });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task Sign_Ed25519_MatchesRfc8032Signature(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                // RFC 8032 §7.1, test 2: one-byte message and deterministic signature for this seed.
                var seed = Convert.FromHexString("4ccd089b28ff96da9db6c346ec114e0f5b8a319f35aba624da8cf6ed4fb8a6fb");
                var expected = Convert.FromHexString(
                    "92a009a9f0d4cab8720e820b5f642540a2b27b5416503f8fb3762223ebdb69da" +
                    "085ac1e43e15996e458f3613d0f11d8c387b2eaeb4302aeeb00d291612bb0c00");
                await session.VerifyAdminAsync(DefaultAdminPin);
                using var key = Curve25519PrivateKey.CreateFromValue(seed, KeyType.Ed25519);
                await session.PutKeyAsync(KeyRef.Sig, key);
                await session.VerifyPinAsync(DefaultUserPin);

                var signature = await session.SignAsync(new byte[] { 0x72 }, HashAlgorithmName.SHA256);
                Assert.Equal(expected, signature.ToArray());
            });

    /// <summary>
    ///     Compares the card public key with the RFC 7748 vector after scalar-byte reversal for import.
    ///     This checks import/public-key correspondence; X25519 agreement requires a separate operation.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task ImportEcKey_X25519_ToDec_CardReportsTheTestVectorPublicKey(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                using var key = Curve25519PrivateKey.CreateFromValue(X25519TestPrivateKey, KeyType.X25519);

                await session.PutKeyAsync(KeyRef.Dec, key);

                Assert.Equal(KeyStatus.Imported, (await session.ListKeyInformationAsync())[KeyRef.Dec]);
                Assert.Equal(KeyType.X25519, Assert.IsType<EcAttributes>(await session.GetAlgorithmAttributesAsync(KeyRef.Dec)).KeyType);
                Assert.Equal(X25519TestPublicKey, await ReadEcPublicPointAsync(session, KeyRef.Dec));
            });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task X25519Decipher_MatchesRfc7748SharedSecret(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                // RFC 7748 §6.1: Alice's scalar, Bob's public key and their independently published secret.
                var bobPublic = Convert.FromHexString("de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f");
                var expected = Convert.FromHexString("4a5d9d5ba4ce2de1728e3bf480350f25e07e21c947d19e3376f09b3c1e161742");
                await session.VerifyAdminAsync(DefaultAdminPin);
                using var key = Curve25519PrivateKey.CreateFromValue(X25519TestPrivateKey, KeyType.X25519);
                await session.PutKeyAsync(KeyRef.Dec, key);

                await session.VerifyPinAsync(DefaultUserPin, extended: true);
                var actual = await session.DecryptAsync(bobPublic);
                Assert.True(CryptographicOperations.FixedTimeEquals(expected, actual.Span));
            });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task ImportEcKey_P256_ToAuth_ThenAuthenticate_VerifiesWithTheHostPublicKey(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                using var key = ECPrivateKey.CreateFromParameters(ecdsa.ExportParameters(includePrivateParameters: true));

                await session.PutKeyAsync(KeyRef.Aut, key);

                Assert.Equal(KeyStatus.Imported, (await session.ListKeyInformationAsync())[KeyRef.Aut]);

                var data = "Auth import test"u8.ToArray();
                await session.VerifyPinAsync(DefaultUserPin, extended: true);
                var signature = await session.AuthenticateAsync(data, HashAlgorithmName.SHA256);
                Assert.True(ecdsa.VerifyData(data, signature.Span, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence));
            });

    /// <summary>GENERATE ASYMMETRIC KEY PAIR (read) returns 7F49 { 86 = public point }.</summary>
    private static async Task<byte[]> ReadEcPublicPointAsync(IOpenPgpSession session, KeyRef keyRef)
    {
        var response = await session.GetPublicKeyAsync(keyRef);
        using var outer = Tlv.Create(response.Span);
        Assert.Equal(0x7F49, outer.Tag);
        return TlvHelper.DecodeDictionary(outer.Value.Span)[0x86].ToArray();
    }
}