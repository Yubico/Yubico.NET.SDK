// Shared plumbing for the Solutions Weekly demos, kept out of the demo files so they show only what matters.
using System.Buffers.Text;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Extensions;
using Yubico.YubiKit.WebAuthn;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;
using Yubico.YubiKit.WebAuthn.Preferences;

namespace DemoSupport;

/// <summary>Device discovery, client setup and default WebAuthn options for example.com.</summary>
public static class Demo
{
    public const string RpId = "example.com";

    public static async Task<IYubiKey> WaitForYubiKeyAsync()
    {
        Console.WriteLine("Plug in a YubiKey...");
        return (await YubiKeyManager.WatchAsync().OfType<DeviceAdded>().FirstAsync()).Device;
    }

    public static Task<WebAuthnClient> OpenClientAsync(IYubiKey yubiKey)
    {
        WebAuthnOrigin.TryParse("https://example.com", out var origin);
        var clientOptions = new WebAuthnClientOptions
        {
            // The app decides which websites may use which RP IDs. Demo: exactly one pair.
            RpIdValidator = (o, rpId) => o.StringValue == "https://example.com" && rpId == RpId
        };
        var sessionOptions = new SessionCreationOptions { UserPresencePrompt = new ConsoleTouchPrompt() };

        return yubiKey.CreateWebAuthnClientAsync(origin!, clientOptions, sessionOptions);
    }

    /// <summary>Registration options. The challenge only matters if your server checks the attestation.</summary>
    public static PublicKeyCredentialCreationOptions Registration(RegistrationExtensionInputs? extensions = null) => new()
    {
        Challenge = RandomNumberGenerator.GetBytes(32),
        Rp = new PublicKeyCredentialRpEntity(RpId, "Example"),
        User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "ada", "Ada"),
        PubKeyCredParams = [CoseAlgorithm.Es256],
        UserVerification = UserVerificationPreference.Discouraged, // touch only, no PIN
        Extensions = extensions
    };

    /// <summary>Sign-in options. The challenge only matters if your server verifies the sign-in signature.</summary>
    public static PublicKeyCredentialRequestOptions SignIn(
        ReadOnlyMemory<byte> credentialId,
        AuthenticationExtensionInputs? extensions = null,
        byte[]? challenge = null) => new()
    {
        Challenge = challenge ?? RandomNumberGenerator.GetBytes(32),
        RpId = RpId,
        AllowCredentials = [new PublicKeyCredentialDescriptor(credentialId)],
        UserVerification = UserVerificationPreference.Discouraged,
        Extensions = extensions
    };
}

/// <summary>
/// Tells the user to touch the key. One prompt, registered once per session, works for
/// PIV, FIDO2/WebAuthn, OATH, OpenPGP, YubiOTP and YubiHSM Auth.
/// </summary>
public sealed class ConsoleTouchPrompt : IUserPresencePrompt
{
    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken)
    {
        // Operation: what the key is doing (from the SDK). Intent: why (from the app's BeginScope).
        Console.WriteLine($"  -> Touch your YubiKey to {context.Intent ?? "continue"} ({context.Operation})");
        return ValueTask.CompletedTask;
    }

    public ValueTask OnUserPresenceResolvedAsync(
        UserPresenceContext context,
        UserPresenceOutcome outcome,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

/// <summary>
/// A stand-in for the relying-party server, only so the demo can show the sign-in signature checking out.
/// A real server also checks the origin, RP ID hash, flags and sign count.
/// Use a maintained server-side WebAuthn library for that; the SDK is client-side only.
/// </summary>
public static class PretendServer
{
    public static bool VerifySignIn(CoseKey publicKey, ReadOnlySpan<byte> expectedChallenge, AuthenticationResponse assertion)
    {
        // Is this a sign-in, for the challenge we just issued? (An old, replayed response fails here.)
        using var clientData = JsonDocument.Parse(assertion.ClientData.JsonBytes);
        var isFreshSignIn =
            clientData.RootElement.GetProperty("type").GetString() == "webauthn.get" &&
            clientData.RootElement.GetProperty("challenge").GetString() == Base64Url.EncodeToString(expectedChallenge);

        // WebAuthn signs authenticatorData || SHA-256(clientDataJSON) with the passkey's P-256 key.
        var key = (CoseEc2Key)publicKey;
        using var ecdsa = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = key.X.ToArray(), Y = key.Y.ToArray() }
        });
        byte[] signedData = [.. assertion.RawAuthenticatorData.Span, .. assertion.ClientData.Hash.Span];

        return isFreshSignIn && ecdsa.VerifyData(signedData, assertion.Signature.Span,
            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
    }
}

/// <summary>A public key derived with ARKG, plus what the YubiKey needs to sign for it.</summary>
public sealed record ArkgPublicKey(ReadOnlyMemory<byte> KeyHandle, ReadOnlyMemory<byte> SignArgs, ECDsa PublicKey);

/// <summary>
/// ARKG key derivation (EXPERIMENTAL; not production cryptographic guidance). Runs without the YubiKey.
/// </summary>
public static class Arkg
{
    public static ArkgPublicKey DerivePublicKey(GeneratedSigningKey signingKey, string context)
    {
        var seed = (CoseArkgP256SeedKey)signingKey.PublicKey;
        var derived = ToCtap2(signingKey.KeyHandle, seed)
            .DerivePublicKey(RandomNumberGenerator.GetBytes(32), System.Text.Encoding.UTF8.GetBytes(context));

        var signArgs = PreviewSignCbor.EncodeAdditionalArgs(CoseSignArgs.ArkgP256(derived.ArkgKeyHandle, derived.Context));

        // The derived key is an ordinary P-256 public key (SEC1 uncompressed: 0x04 || X || Y).
        var point = derived.PublicKey.Span;
        var ecdsa = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = point[1..33].ToArray(), Y = point[33..].ToArray() }
        });

        return new ArkgPublicKey(signingKey.KeyHandle, signArgs, ecdsa);
    }

    // Workaround: the type that derives ARKG keys has an internal constructor in this alpha.
    // Mirrors the SDK's own previewSign integration test.
    private static PreviewSignGeneratedKey ToCtap2(ReadOnlyMemory<byte> keyHandle, CoseArkgP256SeedKey seed)
    {
        var ctor = typeof(PreviewSignGeneratedKey).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(ReadOnlyMemory<byte>), typeof(ReadOnlyMemory<byte>), typeof(ReadOnlyMemory<byte>), typeof(CoseAlgorithm)],
            null)!;

        return (PreviewSignGeneratedKey)ctor.Invoke([keyHandle, seed.BlPublicKey, seed.KemPublicKey, seed.Algorithm]);
    }
}
