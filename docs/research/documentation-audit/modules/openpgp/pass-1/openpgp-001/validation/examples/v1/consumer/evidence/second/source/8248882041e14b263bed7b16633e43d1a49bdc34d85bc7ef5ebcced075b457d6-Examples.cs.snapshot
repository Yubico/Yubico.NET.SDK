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

using Yubico.YubiKit.Core.Credentials;

namespace Yubico.YubiKit.AuditPgp001.Examples.V1.Consumer;

public static class ConsumerExamples
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

static async Task GenerateSigningKeyAsync(
    IYubiKey device, ReadOnlyMemory<byte> adminPin, CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.VerifyAdminAsync(adminPin, cancellationToken);
    await session.GenerateKeyAsync(KeyRef.Sig,
        EcAttributes.Create(KeyRef.Sig, CurveOid.Secp256R1), cancellationToken);
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
    if (attributes is not EcAttributes { Oid: CurveOid.Secp256R1 })
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

public static string OperationMessage(UserPresenceContext context)
{
string message = context.Operation switch
{
    UserPresenceOperations.Ctap2.MakeCredential => "Touch your YubiKey to create a passkey.",
    UserPresenceOperations.Ctap2.GetAssertion => "Touch your YubiKey to sign in.",
    UserPresenceOperations.Piv.SignOrDecrypt => "Touch your YubiKey for the PIV key operation.",
    _ when context.Application == UserPresenceApplications.Oath => "Touch your YubiKey to generate a code.",
    _ => "Touch your YubiKey to continue."
};

return message;
}

public static RsaAttributes Rsa2048Attributes()
{
return RsaAttributes.Create(RsaSize.Rsa2048);
}

public static async Task<int> ReadUserAttemptsAsync(IOpenPgpSession session)
{
return (await session.GetPinStatusAsync()).AttemptsUser;
}

public static Task DeleteCertificateInAuthorizedSessionAsync(IOpenPgpSession session, KeyRef slot)
{
return session.DeleteCertificateAsync(slot);
}
}

public sealed class SharedIntentPrompt(
    Func<UserPresenceContext, UserPresenceOutcome, CancellationToken, ValueTask> resolve) : IUserPresencePrompt
{
public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken)
{
    string operation = context.Operation switch
    {
        UserPresenceOperations.Ctap2.GetAssertion => "sign-in",
        UserPresenceOperations.Ctap2.MakeCredential => "passkey registration",
        _ => "key operation"
    };

    // "Touch your YubiKey to approve the transfer of 500 EUR (sign-in)."
    Console.WriteLine(context.Intent is { } intent
        ? $"Touch your YubiKey to {intent} ({operation})."
        : $"Touch your YubiKey to confirm the {operation}.");
    return default;
}

public ValueTask OnUserPresenceResolvedAsync(
    UserPresenceContext context, UserPresenceOutcome outcome, CancellationToken cancellationToken)
    => resolve(context, outcome, cancellationToken);
}
