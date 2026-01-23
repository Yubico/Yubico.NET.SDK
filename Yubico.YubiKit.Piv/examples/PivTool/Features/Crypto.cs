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

using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Spectre.Console;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Piv.Examples.PivTool.Shared;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Features;

/// <summary>
/// Cryptographic operations for PIV.
/// </summary>
public static class CryptoFeature
{
    private static readonly string[] CryptoMenuOptions =
    [
        "✍️  Sign Data",
        "🔓 Decrypt Data (RSA)",
        "✔️  Verify Signature",
        "⬅️  Back"
    ];

    /// <summary>
    /// Runs the cryptographic operations feature.
    /// </summary>
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            OutputHelpers.WriteHeader("Cryptographic Operations");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select operation:")
                    .AddChoices(CryptoMenuOptions));

            if (choice == "⬅️  Back")
            {
                return;
            }

            switch (choice)
            {
                case "✍️  Sign Data":
                    await SignDataAsync(cancellationToken);
                    break;
                case "🔓 Decrypt Data (RSA)":
                    await DecryptDataAsync(cancellationToken);
                    break;
                case "✔️  Verify Signature":
                    await VerifySignatureAsync(cancellationToken);
                    break;
            }

            AnsiConsole.WriteLine();
            OutputHelpers.WaitForKey();
        }
    }

    /// <summary>
    /// Signs data with the private key in a slot.
    /// </summary>
    private static async Task SignDataAsync(CancellationToken cancellationToken)
    {
        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        await using var connection = await device.ConnectAsync<ISmartCardConnection>(cancellationToken);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: cancellationToken);

        var slot = SelectSlot("Select slot with signing key");

        // Check if slot has a key
        var metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
        if (metadata is null)
        {
            OutputHelpers.WriteError($"Slot {slot} is empty. Generate or import a key first.");
            return;
        }

        var slotMetadata = metadata.Value;

        // Get hash algorithm
        var hashAlgorithm = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Hash algorithm:")
                .AddChoices("SHA-256", "SHA-384", "SHA-512"));

        var hashName = hashAlgorithm switch
        {
            "SHA-256" => HashAlgorithmName.SHA256,
            "SHA-384" => HashAlgorithmName.SHA384,
            "SHA-512" => HashAlgorithmName.SHA512,
            _ => HashAlgorithmName.SHA256
        };

        // Get data to sign
        var dataInput = AnsiConsole.Ask<string>("Data to sign (or path to file):");
        byte[] dataBytes;

        if (File.Exists(dataInput))
        {
            dataBytes = await File.ReadAllBytesAsync(dataInput, cancellationToken);
        }
        else
        {
            dataBytes = System.Text.Encoding.UTF8.GetBytes(dataInput);
        }

        // PIN verification
        if (slotMetadata.PinPolicy != PivPinPolicy.Never)
        {
            byte[]? pin = null;
            try
            {
                pin = PinPrompt.GetPin("Enter PIN:");
                if (pin is null)
                {
                    return;
                }

                await session.VerifyPinAsync(pin, cancellationToken);
            }
            finally
            {
                if (pin is not null)
                {
                    CryptographicOperations.ZeroMemory(pin);
                }
            }
        }

        // Touch reminder
        if (slotMetadata.TouchPolicy is PivTouchPolicy.Always or PivTouchPolicy.Cached)
        {
            OutputHelpers.WriteInfo("Touch the YubiKey when it blinks...");
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();

            // Hash the data
            byte[] hash;
            using (var hasher = hashName.Name switch
            {
                "SHA256" => (HashAlgorithm)SHA256.Create(),
                "SHA384" => SHA384.Create(),
                "SHA512" => SHA512.Create(),
                _ => SHA256.Create()
            })
            {
                hash = hasher.ComputeHash(dataBytes);
            }

            var signature = await AnsiConsole.Status()
                .StartAsync("Signing data...", async _ =>
                    await session.SignOrDecryptAsync(slot, slotMetadata.Algorithm, hash, cancellationToken));

            stopwatch.Stop();

            OutputHelpers.WriteSuccess($"Data signed successfully in {stopwatch.ElapsedMilliseconds}ms.");
            AnsiConsole.WriteLine();

            var signatureB64 = Convert.ToBase64String(signature.ToArray());
            var panel = new Panel(signatureB64)
                .Header("[green]Signature (Base64)[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Green);

            AnsiConsole.Write(panel);

            if (AnsiConsole.Confirm("Save signature to file?", defaultValue: false))
            {
                var filePath = AnsiConsole.Ask("Output file path:", "signature.bin");
                await File.WriteAllBytesAsync(filePath, signature.ToArray(), cancellationToken);
                OutputHelpers.WriteSuccess($"Signature saved to: {filePath}");
            }
        }
        catch (OperationCanceledException)
        {
            OutputHelpers.WriteError("Touch timeout. Please touch the YubiKey when prompted.");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Signing failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Decrypts data with RSA private key.
    /// </summary>
    private static async Task DecryptDataAsync(CancellationToken cancellationToken)
    {
        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        await using var connection = await device.ConnectAsync<ISmartCardConnection>(cancellationToken);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: cancellationToken);

        var slot = SelectSlot("Select slot with RSA decryption key");

        // Check if slot has an RSA key
        var metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
        if (metadata is null)
        {
            OutputHelpers.WriteError($"Slot {slot} is empty. Generate or import a key first.");
            return;
        }

        var slotMetadata = metadata.Value;
        var isRsa = slotMetadata.Algorithm is PivAlgorithm.Rsa1024 or PivAlgorithm.Rsa2048
            or PivAlgorithm.Rsa3072 or PivAlgorithm.Rsa4096;

        if (!isRsa)
        {
            OutputHelpers.WriteError("Decryption requires an RSA key. Selected slot has an ECC key.");
            return;
        }

        // Get encrypted data
        var filePath = AnsiConsole.Ask<string>("Encrypted data file path:");
        if (!File.Exists(filePath))
        {
            OutputHelpers.WriteError($"File not found: {filePath}");
            return;
        }

        var encryptedData = await File.ReadAllBytesAsync(filePath, cancellationToken);

        // PIN verification
        if (slotMetadata.PinPolicy != PivPinPolicy.Never)
        {
            byte[]? pin = null;
            try
            {
                pin = PinPrompt.GetPin("Enter PIN:");
                if (pin is null)
                {
                    return;
                }

                await session.VerifyPinAsync(pin, cancellationToken);
            }
            finally
            {
                if (pin is not null)
                {
                    CryptographicOperations.ZeroMemory(pin);
                }
            }
        }

        // Touch reminder
        if (slotMetadata.TouchPolicy is PivTouchPolicy.Always or PivTouchPolicy.Cached)
        {
            OutputHelpers.WriteInfo("Touch the YubiKey when it blinks...");
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();

            var decrypted = await AnsiConsole.Status()
                .StartAsync("Decrypting data...", async _ =>
                    await session.SignOrDecryptAsync(slot, slotMetadata.Algorithm, encryptedData, cancellationToken));

            stopwatch.Stop();

            OutputHelpers.WriteSuccess($"Data decrypted successfully in {stopwatch.ElapsedMilliseconds}ms.");
            AnsiConsole.WriteLine();

            if (AnsiConsole.Confirm("Display decrypted data as text?", defaultValue: true))
            {
                var text = System.Text.Encoding.UTF8.GetString(decrypted.ToArray());
                AnsiConsole.MarkupLine($"[green]Decrypted:[/] {Markup.Escape(text)}");
            }

            if (AnsiConsole.Confirm("Save decrypted data to file?", defaultValue: false))
            {
                var outputPath = AnsiConsole.Ask("Output file path:", "decrypted.bin");
                await File.WriteAllBytesAsync(outputPath, decrypted.ToArray(), cancellationToken);
                OutputHelpers.WriteSuccess($"Decrypted data saved to: {outputPath}");
            }
        }
        catch (OperationCanceledException)
        {
            OutputHelpers.WriteError("Touch timeout. Please touch the YubiKey when prompted.");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Decryption failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies a signature using the certificate in a slot.
    /// </summary>
    private static async Task VerifySignatureAsync(CancellationToken cancellationToken)
    {
        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        await using var connection = await device.ConnectAsync<ISmartCardConnection>(cancellationToken);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: cancellationToken);

        var slot = SelectSlot("Select slot with certificate");

        // Get certificate
        var cert = await session.GetCertificateAsync(slot, cancellationToken);
        if (cert is null)
        {
            OutputHelpers.WriteError($"Slot {slot} has no certificate.");
            return;
        }

        // Get hash algorithm
        var hashAlgorithm = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Hash algorithm:")
                .AddChoices("SHA-256", "SHA-384", "SHA-512"));

        var hashName = hashAlgorithm switch
        {
            "SHA-256" => HashAlgorithmName.SHA256,
            "SHA-384" => HashAlgorithmName.SHA384,
            "SHA-512" => HashAlgorithmName.SHA512,
            _ => HashAlgorithmName.SHA256
        };

        // Get original data
        var dataInput = AnsiConsole.Ask<string>("Original data (or path to file):");
        byte[] dataBytes;

        if (File.Exists(dataInput))
        {
            dataBytes = await File.ReadAllBytesAsync(dataInput, cancellationToken);
        }
        else
        {
            dataBytes = System.Text.Encoding.UTF8.GetBytes(dataInput);
        }

        // Get signature
        var sigInput = AnsiConsole.Ask<string>("Signature file path (or base64):");
        byte[] signature;

        if (File.Exists(sigInput))
        {
            signature = await File.ReadAllBytesAsync(sigInput, cancellationToken);
        }
        else
        {
            try
            {
                signature = Convert.FromBase64String(sigInput);
            }
            catch
            {
                OutputHelpers.WriteError("Invalid signature format. Expected file path or base64.");
                return;
            }
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var isValid = false;

            var rsaKey = cert.GetRSAPublicKey();
            if (rsaKey is not null)
            {
                isValid = rsaKey.VerifyData(dataBytes, signature, hashName, RSASignaturePadding.Pkcs1);
            }
            else
            {
                var ecdsaKey = cert.GetECDsaPublicKey();
                if (ecdsaKey is not null)
                {
                    isValid = ecdsaKey.VerifyData(dataBytes, signature, hashName);
                }
            }

            stopwatch.Stop();

            if (isValid)
            {
                OutputHelpers.WriteSuccess($"✔️  Signature is VALID ({stopwatch.ElapsedMilliseconds}ms)");
            }
            else
            {
                OutputHelpers.WriteError($"❌ Signature is INVALID ({stopwatch.ElapsedMilliseconds}ms)");
            }
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Verification failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Selects a PIV slot.
    /// </summary>
    private static PivSlot SelectSlot(string prompt)
    {
        var choices = new[]
        {
            "9A - Authentication",
            "9C - Digital Signature",
            "9D - Key Management",
            "9E - Card Authentication"
        };

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(prompt)
                .AddChoices(choices));

        return selection[..2] switch
        {
            "9A" => PivSlot.Authentication,
            "9C" => PivSlot.Signature,
            "9D" => PivSlot.KeyManagement,
            "9E" => PivSlot.CardAuthentication,
            _ => PivSlot.Authentication
        };
    }
}
