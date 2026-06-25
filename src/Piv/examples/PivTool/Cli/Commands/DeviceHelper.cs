// Copyright 2026 Yubico AB
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

using System.Text;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Cli.Commands;

/// <summary>
/// Shared helpers for non-interactive CLI command handlers.
/// </summary>
internal static class DeviceHelper
{
    /// <summary>
    /// Finds a YubiKey with SmartCard support. If <paramref name="serial"/> is provided,
    /// selects that specific device; otherwise auto-selects if only one is connected.
    /// Writes diagnostic messages to stderr so stdout stays clean for JSON output.
    /// </summary>
    internal static async Task<IYubiKey?> GetDeviceAsync(int? serial, CancellationToken ct = default)
    {
        var all = await YubiKeyManager.FindAllAsync(ConnectionType.All, cancellationToken: ct);
        var smartCard = all.Where(d => d.ConnectionType == ConnectionType.SmartCard).ToList();

        if (smartCard.Count == 0)
        {
            Console.Error.WriteLine("Error: No YubiKey with SmartCard interface detected.");
            return null;
        }

        if (serial is null)
        {
            if (smartCard.Count > 1)
            {
                Console.Error.WriteLine(
                    $"Error: {smartCard.Count} YubiKeys detected. Use --serial <number> to select one.");
                return null;
            }

            return smartCard[0];
        }

        // Resolve serial number by querying each device
        foreach (var device in smartCard)
        {
            try
            {
                var info = await device.GetDeviceInfoAsync(ct);
                if (info.SerialNumber == serial)
                {
                    return device;
                }
            }
            catch
            {
                // Skip devices that fail to respond
            }
        }

        Console.Error.WriteLine($"Error: No YubiKey found with serial number {serial}.");
        return null;
    }

    /// <summary>
    /// Parses a required flag value as UTF-8 bytes (for PIN / PUK).
    /// Writes an error to stderr and returns null if the flag is missing.
    /// </summary>
    internal static byte[]? ParseCredentialBytes(string? value, string paramName)
    {
        if (value is null)
        {
            Console.Error.WriteLine($"Error: --{paramName} is required.");
            return null;
        }

        return Encoding.UTF8.GetBytes(value);
    }

    /// <summary>
    /// Parses a hex string (with optional space/colon/dash separators) into bytes.
    /// Used for management keys.
    /// </summary>
    internal static byte[]? ParseHex(string? hexString, string paramName)
    {
        if (hexString is null)
        {
            Console.Error.WriteLine($"Error: --{paramName} is required.");
            return null;
        }

        var clean = hexString
            .Replace(" ", string.Empty)
            .Replace(":", string.Empty)
            .Replace("-", string.Empty);

        if (clean.Length % 2 != 0)
        {
            Console.Error.WriteLine($"Error: --{paramName} has an odd number of hex digits.");
            return null;
        }

        try
        {
            return Convert.FromHexString(clean);
        }
        catch (FormatException)
        {
            Console.Error.WriteLine($"Error: --{paramName} contains invalid hex characters.");
            return null;
        }
    }

    /// <summary>
    /// Parses a slot string ("9a", "9c", "9d", "9e") into a <see cref="PivSlot"/>.
    /// </summary>
    internal static PivSlot? ParseSlot(string? value)
    {
        if (value is null)
        {
            Console.Error.WriteLine("Error: --slot is required. Valid values: 9a, 9c, 9d, 9e");
            return null;
        }

        var slot = value.ToUpperInvariant() switch
        {
            "9A" => (PivSlot?)PivSlot.Authentication,
            "9C" => PivSlot.Signature,
            "9D" => PivSlot.KeyManagement,
            "9E" => PivSlot.CardAuthentication,
            _ => null
        };

        if (slot is null)
        {
            Console.Error.WriteLine($"Error: Invalid --slot '{value}'. Valid: 9a, 9c, 9d, 9e");
        }

        return slot;
    }

    /// <summary>
    /// Parses a key algorithm string into a <see cref="PivAlgorithm"/>.
    /// </summary>
    internal static PivAlgorithm? ParseKeyAlgorithm(string? value)
    {
        if (value is null)
        {
            Console.Error.WriteLine(
                "Error: --algorithm is required. Valid: rsa2048, rsa3072, rsa4096, eccp256, eccp384, ed25519, x25519");
            return null;
        }

        var alg = value.ToLowerInvariant() switch
        {
            "rsa2048" => (PivAlgorithm?)PivAlgorithm.Rsa2048,
            "rsa3072" => PivAlgorithm.Rsa3072,
            "rsa4096" => PivAlgorithm.Rsa4096,
            "eccp256" => PivAlgorithm.EccP256,
            "eccp384" => PivAlgorithm.EccP384,
            "ed25519" => PivAlgorithm.Ed25519,
            "x25519" => PivAlgorithm.X25519,
            _ => null
        };

        if (alg is null)
        {
            Console.Error.WriteLine(
                $"Error: Invalid --algorithm '{value}'. Valid: rsa2048, rsa3072, rsa4096, eccp256, eccp384, ed25519, x25519");
        }

        return alg;
    }

    /// <summary>
    /// Parses a PIN policy string.
    /// </summary>
    internal static PivPinPolicy ParsePinPolicy(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "never" => PivPinPolicy.Never,
            "once" => PivPinPolicy.Once,
            "always" => PivPinPolicy.Always,
            _ => PivPinPolicy.Default
        };

    /// <summary>
    /// Parses a touch policy string.
    /// </summary>
    internal static PivTouchPolicy ParseTouchPolicy(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "never" => PivTouchPolicy.Never,
            "always" => PivTouchPolicy.Always,
            "cached" => PivTouchPolicy.Cached,
            _ => PivTouchPolicy.Default
        };

    /// <summary>
    /// Parses a management key algorithm type string.
    /// </summary>
    internal static PivManagementKeyType ParseMgmtKeyType(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "aes128" => PivManagementKeyType.Aes128,
            "aes192" => PivManagementKeyType.Aes192,
            "aes256" => PivManagementKeyType.Aes256,
            _ => PivManagementKeyType.TripleDes
        };

    /// <summary>
    /// Parses a hash algorithm name.
    /// </summary>
    internal static System.Security.Cryptography.HashAlgorithmName ParseHashAlgorithm(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "sha384" => System.Security.Cryptography.HashAlgorithmName.SHA384,
            "sha512" => System.Security.Cryptography.HashAlgorithmName.SHA512,
            _ => System.Security.Cryptography.HashAlgorithmName.SHA256
        };

    /// <summary>
    /// Returns true if a flag is present (boolean switch) in the flags dictionary.
    /// </summary>
    internal static bool HasFlag(Dictionary<string, string?> flags, string name) =>
        flags.ContainsKey(name);

    /// <summary>
    /// Gets a flag value, or null if not present.
    /// </summary>
    internal static string? GetFlag(Dictionary<string, string?> flags, string name) =>
        flags.TryGetValue(name, out var v) ? v : null;
}