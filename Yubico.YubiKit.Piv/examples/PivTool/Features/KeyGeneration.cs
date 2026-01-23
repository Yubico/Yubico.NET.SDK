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

using System.Security.Cryptography;
using System.Text;
using Spectre.Console;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Piv.Examples.PivTool.Shared;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Features;

/// <summary>
/// Handles key pair generation in PIV slots.
/// </summary>
public static class KeyGenerationFeature
{
    /// <summary>
    /// Runs the key generation feature.
    /// </summary>
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Key Generation");

        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        await using var connection = await device.ConnectAsync<ISmartCardConnection>(cancellationToken);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: cancellationToken);

        // Select slot
        var slot = SelectSlot();
        if (slot is null)
        {
            return;
        }

        // Check if slot is occupied
        var slotMeta = await GetSlotMetadataAsync(session, slot.Value, cancellationToken);
        if (slotMeta is not null)
        {
            OutputHelpers.WriteWarning($"Slot {slot.Value} contains a key. Overwrite? [y/N]");
            if (!AnsiConsole.Confirm("Overwrite existing key?", defaultValue: false))
            {
                return;
            }
        }

        // Authenticate management key
        if (!await AuthenticateManagementKeyAsync(session, cancellationToken))
        {
            return;
        }

        // Select algorithm
        var algorithm = SelectAlgorithm();
        if (algorithm is null)
        {
            return;
        }

        // Select PIN policy
        var pinPolicy = SelectPinPolicy();

        // Select touch policy
        var touchPolicy = SelectTouchPolicy();

        // Generate key
        await GenerateKeyAsync(session, slot.Value, algorithm.Value, pinPolicy, touchPolicy, cancellationToken);
    }

    /// <summary>
    /// Prompts for slot selection.
    /// </summary>
    private static PivSlot? SelectSlot()
    {
        var standardSlots = new[]
        {
            ("Authentication (9A) - PIV standard, TLS client auth", PivSlot.Authentication),
            ("Signature (9C) - Digital signatures", PivSlot.Signature),
            ("Key Management (9D) - Key encryption/decryption", PivSlot.KeyManagement),
            ("Card Authentication (9E) - Physical access", PivSlot.CardAuthentication)
        };

        var retiredSlots = Enumerable.Range(1, 20)
            .Select(i => ($"Retired {i} - Additional key storage", (PivSlot)(0x81 + i)))
            .ToArray();

        var choices = standardSlots
            .Concat(retiredSlots)
            .Select(x => x.Item1)
            .Append("Cancel")
            .ToArray();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select key slot:")
                .PageSize(15)
                .AddChoices(choices));

        if (choice == "Cancel")
        {
            return null;
        }

        var index = Array.IndexOf(choices, choice);
        return index < standardSlots.Length
            ? standardSlots[index].Item2
            : retiredSlots[index - standardSlots.Length].Item2;
    }

    /// <summary>
    /// Prompts for algorithm selection.
    /// </summary>
    private static PivAlgorithm? SelectAlgorithm()
    {
        var algorithms = new (string Name, PivAlgorithm Algorithm, string Notes)[]
        {
            ("RSA 2048", PivAlgorithm.Rsa2048, "Standard, widely compatible"),
            ("RSA 3072", PivAlgorithm.Rsa3072, "Requires firmware 5.7+"),
            ("RSA 4096", PivAlgorithm.Rsa4096, "Requires firmware 5.7+, slow generation"),
            ("ECC P-256", PivAlgorithm.EccP256, "Fast, compact"),
            ("ECC P-384", PivAlgorithm.EccP384, "Stronger than P-256"),
            ("Ed25519", PivAlgorithm.Ed25519, "Requires firmware 5.7+, signatures only"),
            ("X25519", PivAlgorithm.X25519, "Requires firmware 5.7+, key exchange only"),
            ("RSA 1024", PivAlgorithm.Rsa1024, "⚠️ Not recommended - weak")
        };

        var choices = algorithms.Select(a => $"{a.Name} - {a.Notes}").Append("Cancel").ToArray();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select key algorithm:")
                .PageSize(12)
                .AddChoices(choices));

        if (choice == "Cancel")
        {
            return null;
        }

        var index = Array.IndexOf(choices, choice);
        return algorithms[index].Algorithm;
    }

    /// <summary>
    /// Prompts for PIN policy selection.
    /// </summary>
    private static PivPinPolicy SelectPinPolicy()
    {
        var policies = new (string Name, PivPinPolicy Policy)[]
        {
            ("Default - Use slot's default policy", PivPinPolicy.Default),
            ("Never - PIN never required", PivPinPolicy.Never),
            ("Once - PIN required once per session", PivPinPolicy.Once),
            ("Always - PIN required for every operation", PivPinPolicy.Always),
            ("Match Once - Biometric match once per session", PivPinPolicy.MatchOnce),
            ("Match Always - Biometric match every operation", PivPinPolicy.MatchAlways)
        };

        var choices = policies.Select(p => p.Name).ToArray();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select PIN policy:")
                .PageSize(10)
                .AddChoices(choices));

        var index = Array.IndexOf(choices, choice);
        return policies[index].Policy;
    }

    /// <summary>
    /// Prompts for touch policy selection.
    /// </summary>
    private static PivTouchPolicy SelectTouchPolicy()
    {
        var policies = new (string Name, PivTouchPolicy Policy)[]
        {
            ("Default - Use slot's default policy", PivTouchPolicy.Default),
            ("Never - Touch never required", PivTouchPolicy.Never),
            ("Always - Touch required for every operation", PivTouchPolicy.Always),
            ("Cached - Touch cached for 15 seconds", PivTouchPolicy.Cached)
        };

        var choices = policies.Select(p => p.Name).ToArray();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select touch policy:")
                .PageSize(8)
                .AddChoices(choices));

        var index = Array.IndexOf(choices, choice);
        return policies[index].Policy;
    }

    /// <summary>
    /// Generates a key in the specified slot.
    /// </summary>
    private static async Task GenerateKeyAsync(
        IPivSession session,
        PivSlot slot,
        PivAlgorithm algorithm,
        PivPinPolicy pinPolicy,
        PivTouchPolicy touchPolicy,
        CancellationToken cancellationToken)
    {
        IPublicKey? publicKey = null;

        try
        {
            await AnsiConsole.Status()
                .StartAsync($"Generating {algorithm} key in slot {slot}...", async ctx =>
                {
                    ctx.Status($"Generating {algorithm} key (this may take a moment)...");

                    publicKey = await session.GenerateKeyAsync(
                        slot,
                        algorithm,
                        pinPolicy,
                        touchPolicy,
                        cancellationToken);
                });

            OutputHelpers.WriteSuccess($"Key generated successfully in slot {slot}.");
            AnsiConsole.WriteLine();

            // Display public key
            if (publicKey is not null)
            {
                DisplayPublicKey(publicKey);
            }
        }
        catch (NotSupportedException ex)
        {
            HandleAlgorithmError(algorithm, ex);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Key generation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Displays the public key in PEM format.
    /// </summary>
    private static void DisplayPublicKey(IPublicKey publicKey)
    {
        OutputHelpers.WriteInfo("Generated Public Key (PEM format):");
        AnsiConsole.WriteLine();

        var spki = publicKey.ExportSubjectPublicKeyInfo();
        var pem = FormatAsPem(spki, "PUBLIC KEY");

        AnsiConsole.Write(new Panel(pem)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey)
            .Header("[green]Public Key[/]"));

        AnsiConsole.WriteLine();
        OutputHelpers.WriteInfo("Copy this public key to generate certificates or CSRs.");
    }

    /// <summary>
    /// Formats binary data as PEM.
    /// </summary>
    private static string FormatAsPem(byte[] data, string label)
    {
        var base64 = Convert.ToBase64String(data);
        var sb = new StringBuilder();

        sb.AppendLine($"-----BEGIN {label}-----");

        // Split into 64-character lines
        for (var i = 0; i < base64.Length; i += 64)
        {
            sb.AppendLine(base64.Substring(i, Math.Min(64, base64.Length - i)));
        }

        sb.AppendLine($"-----END {label}-----");

        return sb.ToString();
    }

    /// <summary>
    /// Gets slot metadata if available.
    /// </summary>
    private static async Task<PivSlotMetadata?> GetSlotMetadataAsync(
        IPivSession session,
        PivSlot slot,
        CancellationToken cancellationToken)
    {
        try
        {
            return await session.GetSlotMetadataAsync(slot, cancellationToken);
        }
        catch (NotSupportedException)
        {
            // Metadata not supported on older firmware
            return null;
        }
        catch
        {
            // Slot may be empty
            return null;
        }
    }

    /// <summary>
    /// Authenticates with management key.
    /// </summary>
    private static async Task<bool> AuthenticateManagementKeyAsync(
        IPivSession session,
        CancellationToken cancellationToken)
    {
        if (session.IsAuthenticated)
        {
            return true;
        }

        var useDefault = AnsiConsole.Confirm("Use default management key?", defaultValue: true);

        byte[]? key = null;
        try
        {
            if (useDefault)
            {
                key =
                [
                    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
                ];
            }
            else
            {
                var expectedLength = session.ManagementKeyType switch
                {
                    PivManagementKeyType.TripleDes => 24,
                    PivManagementKeyType.Aes128 => 16,
                    PivManagementKeyType.Aes192 => 24,
                    PivManagementKeyType.Aes256 => 32,
                    _ => 24
                };

                key = PinPrompt.GetManagementKey($"Enter management key ({session.ManagementKeyType})", expectedLength);
                if (key is null)
                {
                    return false;
                }
            }

            await session.AuthenticateAsync(key, cancellationToken);
            OutputHelpers.WriteSuccess("Management key authenticated.");
            return true;
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Incorrect management key: {ex.Message}");
            return false;
        }
        finally
        {
            if (key is not null)
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }
    }

    /// <summary>
    /// Handles algorithm-related errors.
    /// </summary>
    private static void HandleAlgorithmError(PivAlgorithm algorithm, NotSupportedException ex)
    {
        var firmwareRequirement = algorithm switch
        {
            PivAlgorithm.Rsa3072 or PivAlgorithm.Rsa4096 => "5.7+",
            PivAlgorithm.Ed25519 or PivAlgorithm.X25519 => "5.7+",
            _ => "unknown"
        };

        OutputHelpers.WriteError($"Algorithm {algorithm} not supported on firmware {firmwareRequirement}.");
        OutputHelpers.WriteInfo(ex.Message);
    }
}
