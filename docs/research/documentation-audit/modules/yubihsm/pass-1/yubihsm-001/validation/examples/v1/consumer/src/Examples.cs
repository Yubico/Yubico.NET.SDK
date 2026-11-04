using System.Security.Cryptography;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.YubiHsm;

namespace Yubico.YubiKit.AuditHsm001.Examples.V1.Consumer;

public static class ConsumerExamples
{
public static async Task ListingAsync()
{

IYubiKey device = await YubiKeyManager.FindFirstAsync();
await using var session = await device.CreateHsmAuthSessionAsync();
foreach (var credential in await session.ListCredentialsAsync())
{
    Console.WriteLine($"{credential.Label}: {credential.Algorithm}, {credential.RetriesRemaining} retries");
}
}

static async Task EnrollSymmetricAsync(
    IYubiKey device,
    ReadOnlyMemory<byte> managementKey,
    ReadOnlyMemory<byte> credentialPassword,
    string label,
    ushort hsmAuthenticationKeyId,
    Func<ushort, string, ReadOnlyMemory<byte>, ReadOnlyMemory<byte>, CancellationToken, Task> provisionHsmAsync,
    CancellationToken cancellationToken)
{
    byte[] keyMaterial = RandomNumberGenerator.GetBytes(32);
    try
    {
        await provisionHsmAsync(hsmAuthenticationKeyId, label,
            keyMaterial.AsMemory(0, 16), keyMaterial.AsMemory(16, 16), cancellationToken);
        await using var session = await device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);
        await session.PutCredentialSymmetricAsync(managementKey, label,
            keyMaterial.AsMemory(0, 16), keyMaterial.AsMemory(16, 16), credentialPassword,
            touchRequired: true, cancellationToken: cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(keyMaterial);
    }
}

interface ISymmetricHsmExchange : IAsyncDisposable
{
    Task<(byte[] HsmChallenge, byte[] CardCryptogram)> BeginAsync(
        ushort authenticationKeyId, ReadOnlyMemory<byte> hostChallenge, CancellationToken cancellationToken);
    Task AuthenticateAndUseAsync(SessionKeys keys, CancellationToken cancellationToken);
}

static async Task UseSymmetricHsmAsync(
    IYubiKey device, string label, ushort hsmAuthenticationKeyId,
    ReadOnlyMemory<byte> credentialPassword,
    Func<CancellationToken, Task<ISymmetricHsmExchange>> openExchangeAsync,
    CancellationToken cancellationToken)
{
    byte[] hostChallenge = RandomNumberGenerator.GetBytes(8);
    byte[]? context = null;
    byte[]? hsmChallenge = null;
    byte[]? cardCryptogram = null;
    try
    {
        context = new byte[16];
        await using var exchange = await openExchangeAsync(cancellationToken);
        (hsmChallenge, cardCryptogram) = await exchange.BeginAsync(
            hsmAuthenticationKeyId, hostChallenge, cancellationToken);
        if (hsmChallenge.Length != 8 || cardCryptogram.Length != 8)
            throw new InvalidOperationException("Unexpected symmetric HSM handshake response.");

        hostChallenge.CopyTo(context, 0);
        hsmChallenge.CopyTo(context, 8);
        await using var session = await device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);
        using var keys = await session.CalculateSessionKeysSymmetricAsync(
            label, context, credentialPassword, cardCryptogram, cancellationToken);
        await exchange.AuthenticateAndUseAsync(keys, cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(hostChallenge);
        if (context is not null) CryptographicOperations.ZeroMemory(context);
        if (hsmChallenge is not null) CryptographicOperations.ZeroMemory(hsmChallenge);
        if (cardCryptogram is not null) CryptographicOperations.ZeroMemory(cardCryptogram);
    }
}

static async Task RotateManagementKeyAsync(
    IYubiKey device, ReadOnlyMemory<byte> currentManagementKey,
    Func<ReadOnlyMemory<byte>, CancellationToken, Task> persistRecoverableKeyAsync,
    CancellationToken cancellationToken)
{
    byte[] replacement = RandomNumberGenerator.GetBytes(16);
    try
    {
        await persistRecoverableKeyAsync(replacement, cancellationToken);
        await using var session = await device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);
        await session.PutManagementKeyAsync(currentManagementKey, replacement, cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(replacement);
    }
}

public static async Task PromptSessionAsync(IYubiKey device)
{
var options = new SessionCreationOptions { UserPresencePrompt = new TouchPrompt() };
await using var session = await device.CreateHsmAuthSessionAsync(options);
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

public static Task<IReadOnlyList<HsmAuthCredential>> OneShotListAsync(IYubiKey device)
{
return device.ListHsmAuthCredentialsAsync();
}
}

sealed class TouchPrompt : IUserPresencePrompt
{
    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) =>
        new(Console.Out.WriteLineAsync($"Touch your YubiKey for {context.Application} {context.Scope}."));
}

public sealed class SharedIntentPrompt : IUserPresencePrompt
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
}
