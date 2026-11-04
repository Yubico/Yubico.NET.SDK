using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.OpenPgp;

namespace Yubico.YubiKit.AuditPiv745Integration.Examples;

public static class OpenPgpExamples
{
public static async Task InspectAsync()
{
IYubiKey device = await YubiKeyManager.FindFirstAsync();
await using var session = await device.CreateOpenPgpSessionAsync();
ApplicationRelatedData data = await session.GetApplicationRelatedDataAsync();
Console.WriteLine($"Card {data.Aid.Version}, applet {session.FirmwareVersion}");
foreach (var entry in await session.ListKeyInformationAsync())
{
    Console.WriteLine($"{entry.Key}: {entry.Value}");
}
}

static async Task ChangeCredentialsAsync(
    IYubiKey device, ReadOnlyMemory<byte> currentUserPin, ReadOnlyMemory<byte> newUserPin,
    ReadOnlyMemory<byte> currentAdminPin, ReadOnlyMemory<byte> newAdminPin,
    ReadOnlyMemory<byte> resetCode, CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.ChangePinAsync(currentUserPin, newUserPin, cancellationToken);
    await session.ChangeAdminAsync(currentAdminPin, newAdminPin, cancellationToken);
    await session.VerifyAdminAsync(newAdminPin, cancellationToken);
    await session.SetResetCodeAsync(resetCode, cancellationToken);
}

static async Task RecoverWithResetCodeAsync(
    IYubiKey device, ReadOnlyMemory<byte> resetCode, ReadOnlyMemory<byte> newUserPin,
    CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.ResetPinUsingResetCodeAsync(resetCode, newUserPin, cancellationToken);
}

static async Task RecoverWithAdminAsync(
    IYubiKey device, ReadOnlyMemory<byte> adminPin, ReadOnlyMemory<byte> newUserPin,
    CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.VerifyAdminAsync(adminPin, cancellationToken);
    await session.ResetPinUsingAdminAuthenticationAsync(newUserPin, cancellationToken);
}

static async Task ConfigureInitialKdfAsync(
    IYubiKey device, ReadOnlyMemory<byte> currentAdminPin,
    ReadOnlyMemory<byte> defaultUserPin, ReadOnlyMemory<byte> defaultAdminPin,
    CancellationToken cancellationToken)
{
    byte[] saltUser = RandomNumberGenerator.GetBytes(8);
    byte[] saltReset = RandomNumberGenerator.GetBytes(8);
    byte[] saltAdmin = RandomNumberGenerator.GetBytes(8);
    byte[] hashUser = [];
    byte[] hashAdmin = [];
    try
    {
        using var derivation = new KdfIterSaltedS2k
        {
            HashAlgorithm = KdfHashAlgorithm.Sha256, IterationCount = 100000,
            SaltUser = saltUser, SaltReset = saltReset, SaltAdmin = saltAdmin
        };
        hashUser = derivation.Process(Pw.User, defaultUserPin.Span);
        hashAdmin = derivation.Process(Pw.Admin, defaultAdminPin.Span);
        using var configured = new KdfIterSaltedS2k
        {
            HashAlgorithm = KdfHashAlgorithm.Sha256, IterationCount = 100000,
            SaltUser = saltUser, SaltReset = saltReset, SaltAdmin = saltAdmin,
            InitialHashUser = hashUser, InitialHashAdmin = hashAdmin
        };
        await using (var provisioning = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken))
        {
            await provisioning.VerifyAdminAsync(currentAdminPin, cancellationToken);
            await provisioning.SetKdfAsync(configured, cancellationToken);
        }
        await using var verification = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
        await verification.VerifyAdminAsync(defaultAdminPin, cancellationToken);
        await verification.VerifyPinAsync(defaultUserPin, cancellationToken: cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(hashUser);
        CryptographicOperations.ZeroMemory(hashAdmin);
        CryptographicOperations.ZeroMemory(saltUser);
        CryptographicOperations.ZeroMemory(saltReset);
        CryptographicOperations.ZeroMemory(saltAdmin);
    }
}

static void ShowKeyMetadata()
{
    KeyInfo keyInfo = CryptographicDefinitions.Keys.Ed25519;
    OpenPgpKeyInfo? openPgpInfo = keyInfo.GetOpenPgpInfo();
    Console.WriteLine(openPgpInfo?.CurveOid); // 1.3.6.1.4.1.11591.15.1 (OpenPGP)
    Console.WriteLine(keyInfo.Asn1?.AlgorithmOid); // 1.3.101.112 (RFC 8410)
    EcAttributes attributes = EcAttributes.Create(KeyRef.Sig, KeyType.Ed25519); // EdDSA, 0x16
    Console.WriteLine(attributes.AlgorithmId);
}

static async Task GenerateSigningKeyAsync(
    IYubiKey device, ReadOnlyMemory<byte> adminPin, CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.VerifyAdminAsync(adminPin, cancellationToken);
    await session.GenerateKeyAsync(KeyRef.Sig,
        EcAttributes.Create(KeyRef.Sig, KeyType.P256), cancellationToken);
    Console.WriteLine((await session.ListKeyInformationAsync(cancellationToken))[KeyRef.Sig]);
}

static async Task ImportRsaSigningKeyAsync(
    IYubiKey device, ReadOnlyMemory<byte> adminPin, byte[] pkcs8Der,
    CancellationToken cancellationToken)
{
    using var privateKey = RSAPrivateKey.CreateFromPkcs8(pkcs8Der);
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.VerifyAdminAsync(adminPin, cancellationToken);
    await session.PutKeyAsync(KeyRef.Sig, privateKey, cancellationToken);
}

static ECParameters DecodeP256PublicKey(ReadOnlyMemory<byte> template)
{
    using var outer = Tlv.Create(template.Span);
    if (outer.Tag != 0x7F49)
        throw new CryptographicException("Expected a card public-key template.");
    var point = TlvHelper.DecodeDictionary(outer.Value.Span)[0x86];
    if (point.Length != 65 || point.Span[0] != 0x04)
        throw new CryptographicException("Expected an uncompressed P-256 point.");
    return new ECParameters
    {
        Curve = ECCurve.NamedCurves.nistP256,
        Q = new ECPoint { X = point.Slice(1, 32).ToArray(), Y = point.Slice(33, 32).ToArray() }
    };
}

static async Task<bool> SignAndVerifyP256Async(
    IYubiKey device, ReadOnlyMemory<byte> userPin, ReadOnlyMemory<byte> message,
    CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    var attributes = await session.GetAlgorithmAttributesAsync(KeyRef.Sig, cancellationToken);
    if (attributes is not EcAttributes { KeyType: KeyType.P256 })
        throw new InvalidOperationException("Provision a P-256 signing key first.");
    using var verifier = ECDsa.Create(DecodeP256PublicKey(
        await session.GetPublicKeyAsync(KeyRef.Sig, cancellationToken)));
    await session.VerifyPinAsync(userPin, cancellationToken: cancellationToken);
    ReadOnlyMemory<byte> signature = await session.SignAsync(message, HashAlgorithmName.SHA256, cancellationToken);
    try
    {
        return verifier.VerifyData(message.Span, signature.Span, HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(signature).Span);
    }
}

static async Task DecipherAndConsumeAsync(
    IYubiKey device, ReadOnlyMemory<byte> userPin, ReadOnlyMemory<byte> rawCiphertextOrPoint,
    Func<ReadOnlyMemory<byte>, CancellationToken, Task> consume,
    CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.VerifyPinAsync(userPin, extended: true, cancellationToken: cancellationToken);
    ReadOnlyMemory<byte> result = await session.DecryptAsync(rawCiphertextOrPoint, cancellationToken);
    try
    {
        await consume(result, cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(result).Span);
    }
}

static async Task<byte[]> StoreAndReadCertificateAsync(
    IYubiKey device, KeyRef slot, ReadOnlyMemory<byte> adminPin, ReadOnlyMemory<byte> certificateDer,
    CancellationToken cancellationToken)
{
    using var certificate = X509CertificateLoader.LoadCertificate(certificateDer.Span);
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.VerifyAdminAsync(adminPin, cancellationToken);
    await session.PutCertificateAsync(slot, certificate, cancellationToken);
    using var readBack = await session.GetCertificateAsync(slot, cancellationToken);
    return readBack?.RawData ?? throw new InvalidOperationException("Certificate was absent after writing.");
}

static async Task<byte[]> AttestGeneratedKeyAsync(
    IYubiKey device, KeyRef slot, ReadOnlyMemory<byte> userPin, CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.VerifyPinAsync(userPin, extended: false, cancellationToken: cancellationToken);
    using var attestation = await session.AttestKeyAsync(slot, cancellationToken);
    return attestation.RawData;
}

static async Task ConfigureSigningPolicyAsync(
    IYubiKey device, ReadOnlyMemory<byte> adminPin, CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.VerifyAdminAsync(adminPin, cancellationToken);
    await session.SetUifAsync(KeyRef.Sig, Uif.On, cancellationToken);
    await session.SetSignaturePinPolicyAsync(PinPolicy.Always, cancellationToken);
    Console.WriteLine(await session.GetUifAsync(KeyRef.Sig, cancellationToken));
}

static async Task<ApplicationRelatedData> ResetAndInspectAsync(
    IYubiKey device, CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.ResetAsync(cancellationToken);
    return await session.GetApplicationRelatedDataAsync(cancellationToken);
}
}
