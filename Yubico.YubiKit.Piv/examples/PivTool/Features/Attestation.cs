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

using Spectre.Console;
using Yubico.YubiKit.Piv.Examples.PivTool.Shared;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Features;

/// <summary>
/// Key attestation operations for PIV.
/// </summary>
public static class AttestationFeature
{
    /// <summary>
    /// Runs the attestation feature.
    /// </summary>
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Key Attestation");

        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        await using var session = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);

        var slot = SelectSlot("Select slot to attest");

        // Check if slot has a key
        var metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
        if (metadata is null)
        {
            OutputHelpers.WriteError($"Slot {slot} is empty.");
            return;
        }

        var slotMetadata = metadata.Value;

        // Check if key was generated on device
        if (!slotMetadata.IsGenerated)
        {
            OutputHelpers.WriteError("Attestation only available for keys generated on-device.");
            OutputHelpers.WriteInfo("This key was imported and cannot be attested.");
            return;
        }

        try
        {
            // Get attestation certificate
            var attestationCert = await AnsiConsole.Status()
                .StartAsync("Generating attestation...", async _ =>
                    await session.AttestKeyAsync(slot, cancellationToken));

            // Get attestation signing certificate
            var attestationSigningCert = await session.GetCertificateAsync(PivSlot.Attestation, cancellationToken);

            OutputHelpers.WriteSuccess("Attestation generated successfully.");
            AnsiConsole.WriteLine();

            // Display attestation chain
            var tree = new Tree("[blue]Attestation Chain[/]")
                .Guide(TreeGuide.Line);

            // Slot certificate (attested key)
            var slotNode = tree.AddNode($"[green]Slot {slot} Key[/]");
            slotNode.AddNode($"Algorithm: {slotMetadata.Algorithm}");
            slotNode.AddNode($"PIN Policy: {slotMetadata.PinPolicy}");
            slotNode.AddNode($"Touch Policy: {slotMetadata.TouchPolicy}");
            slotNode.AddNode($"Generated On-Device: {slotMetadata.IsGenerated}");

            // Attestation certificate
            var attestNode = tree.AddNode("[yellow]Attestation Certificate[/]");
            attestNode.AddNode($"Subject: {Markup.Escape(attestationCert.Subject)}");
            attestNode.AddNode($"Issuer: {Markup.Escape(attestationCert.Issuer)}");
            attestNode.AddNode($"Valid: {attestationCert.NotBefore:yyyy-MM-dd} to {attestationCert.NotAfter:yyyy-MM-dd}");
            attestNode.AddNode($"Serial: {attestationCert.SerialNumber}");

            // Yubico attestation signing certificate
            if (attestationSigningCert is not null)
            {
                var signingNode = tree.AddNode("[cyan]Yubico Attestation CA[/]");
                signingNode.AddNode($"Subject: {Markup.Escape(attestationSigningCert.Subject)}");
                signingNode.AddNode($"Issuer: {Markup.Escape(attestationSigningCert.Issuer)}");
                signingNode.AddNode($"Valid: {attestationSigningCert.NotBefore:yyyy-MM-dd} to {attestationSigningCert.NotAfter:yyyy-MM-dd}");
            }
            else
            {
                tree.AddNode("[grey]Yubico Root CA (not stored on device)[/]");
            }

            AnsiConsole.Write(tree);
            AnsiConsole.WriteLine();

            // Verify attestation signature
            if (attestationSigningCert is not null)
            {
                OutputHelpers.WriteInfo("Verifying attestation signature...");

                try
                {
                    using var chain = new System.Security.Cryptography.X509Certificates.X509Chain();
                    chain.ChainPolicy.ExtraStore.Add(attestationSigningCert);
                    chain.ChainPolicy.RevocationMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck;
                    chain.ChainPolicy.VerificationFlags = System.Security.Cryptography.X509Certificates.X509VerificationFlags.AllowUnknownCertificateAuthority;

                    var chainBuilt = chain.Build(attestationCert);
                    if (chainBuilt)
                    {
                        OutputHelpers.WriteSuccess("✔️  Attestation signature verified.");
                    }
                    else
                    {
                        OutputHelpers.WriteWarning("⚠️  Could not verify full chain (may need Yubico root CA).");
                    }
                }
                catch
                {
                    OutputHelpers.WriteWarning("⚠️  Signature verification requires Yubico root CA.");
                }
            }

            // Display key generation metadata
            AnsiConsole.WriteLine();
            var panel = new Panel(
                $"[green]Key Metadata from Attestation[/]\n\n" +
                $"• Key was generated on this YubiKey\n" +
                $"• Algorithm: {slotMetadata.Algorithm}\n" +
                $"• PIN Policy: {slotMetadata.PinPolicy}\n" +
                $"• Touch Policy: {slotMetadata.TouchPolicy}")
                .Header("[blue]Generation Proof[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue);

            AnsiConsole.Write(panel);

            // Option to export
            if (AnsiConsole.Confirm("Export attestation certificate?", defaultValue: false))
            {
                var filePath = AnsiConsole.Ask("Output file path:", $"attestation_{slot.ToString().ToLowerInvariant()}.pem");
                var pem = attestationCert.ExportCertificatePem();
                await File.WriteAllTextAsync(filePath, pem, cancellationToken);
                OutputHelpers.WriteSuccess($"Attestation certificate saved to: {filePath}");
            }
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Attestation failed: {ex.Message}");
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
