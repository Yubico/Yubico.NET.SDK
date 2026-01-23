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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Spectre.Console;
using Yubico.YubiKit.Piv.Examples.PivTool.Shared;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Features;

/// <summary>
/// Certificate operations for PIV slots.
/// </summary>
public static class CertificatesFeature
{
    private static readonly string[] CertMenuOptions =
    [
        "📋 View Certificate",
        "📥 Import Certificate",
        "📤 Export Certificate",
        "🗑️  Delete Certificate",
        "🔏 Generate Self-Signed Certificate",
        "📝 Generate CSR",
        "⬅️  Back"
    ];

    /// <summary>
    /// Runs the certificate management feature.
    /// </summary>
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            OutputHelpers.WriteHeader("Certificate Operations");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select operation:")
                    .AddChoices(CertMenuOptions));

            if (choice == "⬅️  Back")
            {
                return;
            }

            switch (choice)
            {
                case "📋 View Certificate":
                    await ViewCertificateAsync(cancellationToken);
                    break;
                case "📥 Import Certificate":
                    await ImportCertificateAsync(cancellationToken);
                    break;
                case "📤 Export Certificate":
                    await ExportCertificateAsync(cancellationToken);
                    break;
                case "🗑️  Delete Certificate":
                    await DeleteCertificateAsync(cancellationToken);
                    break;
                case "🔏 Generate Self-Signed Certificate":
                    await GenerateSelfSignedAsync(cancellationToken);
                    break;
                case "📝 Generate CSR":
                    await GenerateCsrAsync(cancellationToken);
                    break;
            }

            AnsiConsole.WriteLine();
            OutputHelpers.WaitForKey();
        }
    }

    /// <summary>
    /// Views certificate details.
    /// </summary>
    private static async Task ViewCertificateAsync(CancellationToken cancellationToken)
    {
        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        await using var session = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);

        var slot = SelectSlot("Select slot to view");

        try
        {
            var cert = await AnsiConsole.Status()
                .StartAsync("Reading certificate...", async _ =>
                    await session.GetCertificateAsync(slot, cancellationToken));

            if (cert is null)
            {
                OutputHelpers.WriteError($"Slot {slot} is empty. Generate or import a key first.");
                return;
            }

            DisplayCertificateDetails(cert, slot);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to read certificate: {ex.Message}");
        }
    }

    /// <summary>
    /// Imports a certificate from file.
    /// </summary>
    private static async Task ImportCertificateAsync(CancellationToken cancellationToken)
    {
        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        await using var session = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);

        var slot = SelectSlot("Select slot for import");

        var filePath = AnsiConsole.Ask<string>("Certificate file path (PEM or DER):");
        if (!File.Exists(filePath))
        {
            OutputHelpers.WriteError($"File not found: {filePath}");
            return;
        }

        try
        {
            var fileContent = await File.ReadAllBytesAsync(filePath, cancellationToken);
            X509Certificate2? cert = null;

            // Try PEM first
            var text = Encoding.UTF8.GetString(fileContent);
            if (text.Contains("-----BEGIN CERTIFICATE-----"))
            {
                cert = X509Certificate2.CreateFromPem(text);
            }
            else
            {
                // Try DER
                cert = X509CertificateLoader.LoadCertificate(fileContent);
            }

            if (cert is null)
            {
                OutputHelpers.WriteError("Certificate format not recognized. Expected PEM or DER.");
                return;
            }

            // Authenticate
            if (!await AuthenticateAsync(session, cancellationToken))
            {
                return;
            }

            var compress = AnsiConsole.Confirm("Compress certificate data?", defaultValue: false);

            await AnsiConsole.Status()
                .StartAsync("Storing certificate...", async _ =>
                    await session.StoreCertificateAsync(slot, cert, compress, cancellationToken));

            OutputHelpers.WriteSuccess($"Certificate imported to slot {slot}.");
        }
        catch (CryptographicException)
        {
            OutputHelpers.WriteError("Certificate format not recognized. Expected PEM or DER.");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Import failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Exports a certificate to file.
    /// </summary>
    private static async Task ExportCertificateAsync(CancellationToken cancellationToken)
    {
        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        await using var session = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);

        var slot = SelectSlot("Select slot to export");

        try
        {
            var cert = await session.GetCertificateAsync(slot, cancellationToken);
            if (cert is null)
            {
                OutputHelpers.WriteError($"Slot {slot} is empty. Generate or import a key first.");
                return;
            }

            var format = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Export format:")
                    .AddChoices("PEM", "DER"));

            var defaultName = $"certificate_{slot.ToString().ToLowerInvariant()}.{(format == "PEM" ? "pem" : "der")}";
            var filePath = AnsiConsole.Ask("Output file path:", defaultName);

            if (format == "PEM")
            {
                var pem = cert.ExportCertificatePem();
                await File.WriteAllTextAsync(filePath, pem, cancellationToken);
            }
            else
            {
                var der = cert.RawData;
                await File.WriteAllBytesAsync(filePath, der, cancellationToken);
            }

            OutputHelpers.WriteSuccess($"Certificate exported to: {filePath}");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Export failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a certificate from a slot.
    /// </summary>
    private static async Task DeleteCertificateAsync(CancellationToken cancellationToken)
    {
        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        await using var session = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);

        var slot = SelectSlot("Select slot to delete certificate from");

        if (!OutputHelpers.ConfirmDangerous($"Delete certificate from slot {slot}?"))
        {
            OutputHelpers.WriteInfo("Operation cancelled.");
            return;
        }

        if (!await AuthenticateAsync(session, cancellationToken))
        {
            return;
        }

        try
        {
            await AnsiConsole.Status()
                .StartAsync("Deleting certificate...", async _ =>
                    await session.DeleteCertificateAsync(slot, cancellationToken));

            OutputHelpers.WriteSuccess($"Certificate deleted from slot {slot}.");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Delete failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a self-signed certificate.
    /// </summary>
    private static async Task GenerateSelfSignedAsync(CancellationToken cancellationToken)
    {
        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        await using var session = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);

        var slot = SelectSlot("Select slot with key");

        // Check if slot has a key
        try
        {
            var metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
            if (metadata is null)
            {
                OutputHelpers.WriteError($"Slot {slot} is empty. Generate or import a key first.");
                return;
            }

            var slotMetadata = metadata.Value;
            var subject = AnsiConsole.Ask("Subject (e.g., CN=Test User):", "CN=YubiKey PIV Test");
            var validDays = AnsiConsole.Ask("Validity period (days):", 365);

            if (!await AuthenticateAsync(session, cancellationToken))
            {
                return;
            }

            // Need PIN for signing
            var needsPin = slotMetadata.PinPolicy != PivPinPolicy.Never;
            if (needsPin)
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

            // Get public key directly from slot metadata (SDK pain point #4 - now fixed)
            // No longer need to rely on existing certificate
            using var publicKey = slotMetadata.Algorithm.IsRsa()
                ? (AsymmetricAlgorithm)slotMetadata.GetRsaPublicKey()
                : slotMetadata.GetECDsaPublicKey();

            if (publicKey is null)
            {
                OutputHelpers.WriteError("Failed to extract public key from slot metadata.");
                return;
            }

            var subjectName = new X500DistinguishedName(subject);
            var hashAlgorithm = GetHashAlgorithm(slotMetadata.Algorithm);

            X509Certificate2? newCert = null;

            await AnsiConsole.Status()
                .StartAsync("Generating self-signed certificate...", async _ =>
                {
                    await Task.Yield();

                    if (publicKey is RSA rsa)
                    {
                        var request = new CertificateRequest(subjectName, rsa, hashAlgorithm, RSASignaturePadding.Pkcs1);
                        newCert = request.CreateSelfSigned(
                            DateTimeOffset.UtcNow,
                            DateTimeOffset.UtcNow.AddDays(validDays));
                    }
                    else if (publicKey is ECDsa ecdsa)
                    {
                        var request = new CertificateRequest(subjectName, ecdsa, hashAlgorithm);
                        newCert = request.CreateSelfSigned(
                            DateTimeOffset.UtcNow,
                            DateTimeOffset.UtcNow.AddDays(validDays));
                    }
                });

            if (newCert is not null)
            {
                await session.StoreCertificateAsync(slot, newCert, false, cancellationToken);
                OutputHelpers.WriteSuccess($"Self-signed certificate generated and stored in slot {slot}.");
            }
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Generation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a CSR for the key in a slot.
    /// </summary>
    private static async Task GenerateCsrAsync(CancellationToken cancellationToken)
    {
        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        await using var session = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);

        var slot = SelectSlot("Select slot with key");

        try
        {
            var metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
            if (metadata is null)
            {
                OutputHelpers.WriteError($"Slot {slot} is empty. Generate or import a key first.");
                return;
            }

            var slotMetadata = metadata.Value;
            var subject = AnsiConsole.Ask("Subject (e.g., CN=User,O=Company):", "CN=YubiKey PIV User");

            // Need PIN for signing
            var needsPin = slotMetadata.PinPolicy != PivPinPolicy.Never;
            if (needsPin)
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

            // Get public key directly from slot metadata (SDK pain point #4 - now fixed)
            using var publicKey = slotMetadata.Algorithm.IsRsa()
                ? (AsymmetricAlgorithm)slotMetadata.GetRsaPublicKey()
                : slotMetadata.GetECDsaPublicKey();

            if (publicKey is null)
            {
                OutputHelpers.WriteError("Failed to extract public key from slot metadata.");
                return;
            }

            var subjectName = new X500DistinguishedName(subject);
            var hashAlgorithm = GetHashAlgorithm(slotMetadata.Algorithm);
            string? csr = null;

            await AnsiConsole.Status()
                .StartAsync("Generating CSR...", async _ =>
                {
                    await Task.Yield();

                    if (publicKey is RSA rsa)
                    {
                        var request = new CertificateRequest(subjectName, rsa, hashAlgorithm, RSASignaturePadding.Pkcs1);
                        csr = request.CreateSigningRequestPem();
                    }
                    else if (publicKey is ECDsa ecdsa)
                    {
                        var request = new CertificateRequest(subjectName, ecdsa, hashAlgorithm);
                        csr = request.CreateSigningRequestPem();
                    }
                });

            if (csr is not null)
            {
                AnsiConsole.WriteLine();
                OutputHelpers.WriteSuccess("Certificate Signing Request (CSR):");
                AnsiConsole.WriteLine();

                var panel = new Panel(csr)
                    .Header("[green]CSR (PEM)[/]")
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Green);

                AnsiConsole.Write(panel);

                if (AnsiConsole.Confirm("Save CSR to file?", defaultValue: true))
                {
                    var filePath = AnsiConsole.Ask("Output file path:", $"csr_{slot.ToString().ToLowerInvariant()}.pem");
                    await File.WriteAllTextAsync(filePath, csr, cancellationToken);
                    OutputHelpers.WriteSuccess($"CSR saved to: {filePath}");
                }
            }
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"CSR generation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Displays certificate details.
    /// </summary>
    private static void DisplayCertificateDetails(X509Certificate2 cert, PivSlot slot)
    {
        var table = new Table()
            .Title($"[blue]Certificate in Slot {slot}[/]")
            .Border(TableBorder.Rounded)
            .AddColumn("Property")
            .AddColumn("Value");

        table.AddRow("Subject", Markup.Escape(cert.Subject));
        table.AddRow("Issuer", Markup.Escape(cert.Issuer));
        table.AddRow("Serial Number", cert.SerialNumber);
        table.AddRow("Valid From", cert.NotBefore.ToString("yyyy-MM-dd HH:mm:ss"));
        table.AddRow("Valid Until", cert.NotAfter.ToString("yyyy-MM-dd HH:mm:ss"));
        table.AddRow("Thumbprint", cert.Thumbprint);
        table.AddRow("Algorithm", cert.GetKeyAlgorithm());

        var keySize = GetKeySize(cert);
        table.AddRow("Key Size", keySize?.ToString() ?? "Unknown");

        var isValid = cert.NotBefore <= DateTime.UtcNow && cert.NotAfter >= DateTime.UtcNow;
        table.AddRow("Status", isValid ? "[green]Valid[/]" : "[red]Expired/Not Yet Valid[/]");

        AnsiConsole.Write(table);
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

    /// <summary>
    /// Authenticates with management key.
    /// </summary>
    private static async Task<bool> AuthenticateAsync(IPivSession session, CancellationToken cancellationToken)
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
                // Use SDK-provided constant (SDK pain point #3 - now fixed)
                key = PivSession.DefaultManagementKey.ToArray();
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
            return true;
        }
        catch
        {
            OutputHelpers.WriteError("Incorrect management key.");
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
    /// Gets the hash algorithm for a PIV algorithm.
    /// </summary>
    private static HashAlgorithmName GetHashAlgorithm(PivAlgorithm algorithm) =>
        algorithm switch
        {
            PivAlgorithm.EccP384 => HashAlgorithmName.SHA384,
            _ => HashAlgorithmName.SHA256
        };

    /// <summary>
    /// Gets the key size from a certificate.
    /// </summary>
    private static int? GetKeySize(X509Certificate2 cert)
    {
        try
        {
            var rsa = cert.GetRSAPublicKey();
            if (rsa is not null)
            {
                return rsa.KeySize;
            }

            var ecdsa = cert.GetECDsaPublicKey();
            if (ecdsa is not null)
            {
                return ecdsa.KeySize;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
