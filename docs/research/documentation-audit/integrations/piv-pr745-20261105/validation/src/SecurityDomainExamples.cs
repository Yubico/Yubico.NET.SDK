using System.Security.Cryptography;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.SecurityDomain;

namespace Yubico.YubiKit.AuditPiv745Integration.Examples;

public static class SecurityDomainExamples
{
public static async Task ReadOnlyAsync()
{
IYubiKey device = await YubiKeyManager.FindFirstAsync();
await using var session = await device.CreateSecurityDomainSessionAsync();

foreach (var keyInfo in await session.ListKeyInformationAsync())
{
    Console.WriteLine(keyInfo.KeyReference);
}
}

public static async Task AuthenticateAsync(IYubiKey device)
{
using var scpKeyParameters = Scp03KeyParameters.Default;
await using var session = await device.CreateSecurityDomainSessionAsync(
    new SessionCreationOptions { ScpKeyParameters = scpKeyParameters });

IReadOnlyList<Yubico.YubiKit.SecurityDomain.KeyInfo> keys = await session.ListKeyInformationAsync();
}

public static async Task ImportAsync(IYubiKey device)
{
using var scpKeyParameters = Scp03KeyParameters.Default;
await using var session = await device.CreateSecurityDomainSessionAsync(
    new SessionCreationOptions { ScpKeyParameters = scpKeyParameters });

IReadOnlyList<Yubico.YubiKit.SecurityDomain.KeyInfo> keys = await session.ListKeyInformationAsync();

byte[] keyMaterial = RandomNumberGenerator.GetBytes(48);
try
{
    // Escrow keyMaterial before you send it: the device will not give it back.
    using var newKeys = new StaticKeys(keyMaterial.AsSpan(0, 16), keyMaterial.AsSpan(16, 16), keyMaterial.AsSpan(32, 16));
    await session.PutKeyAsync(new KeyReference(ScpKid.SCP03, Kvn: 0x02), newKeys, replaceKvn: 0);
}
finally
{
    CryptographicOperations.ZeroMemory(keyMaterial);
}
}

public static async Task GenerateAsync(IYubiKey device)
{
using var scpKeyParameters = Scp03KeyParameters.Default;
await using var session = await device.CreateSecurityDomainSessionAsync(
    new SessionCreationOptions { ScpKeyParameters = scpKeyParameters });

IReadOnlyList<Yubico.YubiKit.SecurityDomain.KeyInfo> keys = await session.ListKeyInformationAsync();

var keyReference = new KeyReference(ScpKid.SCP11b, Kvn: 0x01);
ECPublicKey devicePublicKey = await session.GenerateKeyAsync(keyReference, replaceKvn: 0);
}

public static async Task ResetAsync(IYubiKey device)
{
await using var session = await device.CreateSecurityDomainSessionAsync();
await session.ResetAsync();
}

public static async Task OpenScp11Async(IYubiKey device, KeyReference keyReference, ECPublicKey devicePublicKey)
{
using var parameters = new Scp11KeyParameters(keyReference, devicePublicKey);
await using var session = await device.CreateSecurityDomainSessionAsync(
    new SessionCreationOptions { ScpKeyParameters = parameters });
}
}
