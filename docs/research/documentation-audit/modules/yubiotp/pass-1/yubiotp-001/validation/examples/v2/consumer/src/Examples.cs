using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid.Keyboard;
using Yubico.YubiKit.YubiOtp;

namespace AuditOtp001.Examples;

public static class ConsumerExamples
{
public static async Task Discovery()
{

IYubiKey device = await YubiKeyManager.FindFirstAsync();
{
    await using var session = await device.CreateYubiOtpSessionAsync();
    ConfigState state = session.GetConfigState();
    Console.WriteLine($"OTP applet firmware {state.FirmwareVersion}");
}

}
public static async Task UpdateRetainCode(IYubiKey device, byte[] accessCode)
{
try
{
    await using var session = await device.CreateYubiOtpSessionAsync();
    using var update = new UpdateConfiguration();
    update.AppendCr();
    update.FastTrigger();
    update.PacingChar10();
    await session.UpdateConfigurationAsync(
        Slot.Two, update, accessCode: accessCode, currentAccessCode: accessCode);
}
finally
{
    CryptographicOperations.ZeroMemory(accessCode);
}

}
public static async Task ProvisionHmac(IYubiKey device)
{
byte[] hmacKey = RandomNumberGenerator.GetBytes(20);
try
{
    // Securely enroll hmacKey with your verifier here as part of your application's provisioning.
    await using var session = await device.CreateYubiOtpSessionAsync(
        new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp });
    using var config = new HmacSha1SlotConfiguration(hmacKey);
    config.RequireTouch();
    config.UseShortChallenge();
    await session.PutConfigurationAsync(Slot.Two, config);
}
finally
{
    CryptographicOperations.ZeroMemory(hmacKey);
}

}
public static async Task CalculateHmac(IYubiKey device)
{
byte[] challenge = Encoding.UTF8.GetBytes("some-challenge");
ReadOnlyMemory<byte> response = default;
try
{
    response = await device.CalculateHmacSha1Async(
        Slot.Two, challenge,
        new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp });
    // Consume the response with your verifier here; do not log it.
}
finally
{
    CryptographicOperations.ZeroMemory(challenge);
    CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(response).Span);
}

}
public static async Task AesTicket(IYubiKey device)
{
byte[] aesKey = [];
byte[] challenge = [];
ReadOnlyMemory<byte> ticket = default;
try
{
    aesKey = RandomNumberGenerator.GetBytes(16);
    challenge = RandomNumberGenerator.GetBytes(6);
    // Securely provision aesKey to the verifier here as part of your application's enrollment.
    await using var session = await device.CreateYubiOtpSessionAsync(
        new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp });
    using var config = new YubicoOtpChallengeResponseSlotConfiguration(aesKey);
    await session.PutConfigurationAsync(Slot.Two, config);
    ticket = await session.CalculateYubicoOtpAsync(Slot.Two, challenge);
    // Send the ticket to your verifier through your application's secure channel.
}
finally
{
    CryptographicOperations.ZeroMemory(aesKey);
    CryptographicOperations.ZeroMemory(challenge);
    CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(ticket).Span);
}

}
public static async Task StaticPassword(IYubiKey device)
{
{
    await using var session = await device.CreateYubiOtpSessionAsync();
    using var config = new StaticPasswordSlotConfiguration("Correct-Horse-1", KeyboardLayout.en_US);
    config.AppendCr();
    await session.PutConfigurationAsync(Slot.Two, config);
}

}
public static async Task SwapDelete(IYubiKey device)
{
{
    await using var session = await device.CreateYubiOtpSessionAsync();
    await session.SwapSlotsAsync();
    await session.DeleteSlotAsync(Slot.Two);
}

}
public static async Task PromptSession(IYubiKey device)
{
{
    await using var session = await device.CreateYubiOtpSessionAsync(
        new SessionCreationOptions
        {
            PreferredConnectionType = ConnectionType.HidOtp,
            UserPresencePrompt = new TouchPrompt()
        });
    // Calculate a challenge-response on this session using the cleanup pattern above.
}

}
public static async Task BorrowedConnectionProvision(IConnection connection, byte[] key)
{
try
{
    await using var session = await YubiOtpSession.CreateAsync(connection);
    using var config = new HmacSha1SlotConfiguration(key);
    await session.PutConfigurationAsync(Slot.Two, config);
}
finally
{
    CryptographicOperations.ZeroMemory(key);
}

}
public static async Task ExistingSessionChallenge(YubiOtpSession session, byte[] challenge)
{
ReadOnlyMemory<byte> response = default;
try
{
    response = await session.CalculateHmacSha1Async(Slot.Two, challenge);
    // Consume with the application's verifier.
}
finally
{
    CryptographicOperations.ZeroMemory(challenge);
    CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(response).Span);
}

}
public static void CachedState(YubiOtpSession session)
{
var state = session.GetConfigState();
bool isConfigured = state.IsConfigured(Slot.One);

}
public static async Task MigrationCurrentV2(IYubiKey device, byte[] hmacKey, byte[] challenge, CancellationToken cancellationToken)
{

ReadOnlyMemory<byte> response = default;
try
{
    await using var otp = await device.CreateYubiOtpSessionAsync(
        new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp },
        cancellationToken);
    using var config = new HmacSha1SlotConfiguration(hmacKey);
    config.UseShortChallenge();

    await otp.PutConfigurationAsync(
        Slot.Two,
        config,
        cancellationToken: cancellationToken);

    response = await otp.CalculateHmacSha1Async(
        Slot.Two,
        challenge,
        cancellationToken);
    // Consume the caller-owned response with your verifier; do not log it.
}
finally
{
    CryptographicOperations.ZeroMemory(hmacKey);
    CryptographicOperations.ZeroMemory(challenge);
    // This is the documented caller-owned SDK output, not arbitrary borrowed read-only memory.
    CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(response).Span);
}

}
public static string SharedOperationWording(UserPresenceContext context)
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
public static HmacSha1SlotConfiguration InlineFixedChallenge(HmacSha1SlotConfiguration config)
{
return config.UseShortChallenge(false);
}
public static bool InlineConfigured(ConfigState state)
{
return state.IsConfigured(Slot.One);
}
public static Task<ConfigState> InlineReadState(IYubiKey device)
{
return device.GetConfigStateAsync();
}
public static SessionCreationOptions InlineHidOptions()
{
return new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp };
}
}
sealed class TouchPrompt : IUserPresencePrompt
{
    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) =>
        new(Console.Out.WriteLineAsync($"Touch your YubiKey for {context.Application} {context.Scope}."));
}

public sealed class IntentPrompt : IUserPresencePrompt
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
