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
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Piv.Examples.PivTool.Shared;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Features;

/// <summary>
/// Displays an overview of all PIV slots.
/// </summary>
public static class SlotOverviewFeature
{
    private static readonly PivSlot[] StandardSlots =
    [
        PivSlot.Authentication,
        PivSlot.Signature,
        PivSlot.KeyManagement,
        PivSlot.CardAuthentication
    ];

    /// <summary>
    /// Runs the slot overview feature.
    /// </summary>
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("PIV Slot Overview");

        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        await using var connection = await device.ConnectAsync<ISmartCardConnection>(cancellationToken);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: cancellationToken);

        var table = new Table()
            .Title("[blue]PIV Slots[/]")
            .Border(TableBorder.Rounded)
            .AddColumn("Slot")
            .AddColumn("Name")
            .AddColumn("Algorithm")
            .AddColumn("PIN Policy")
            .AddColumn("Touch Policy")
            .AddColumn("Certificate");

        var hasAnyKey = false;

        await AnsiConsole.Status()
            .StartAsync("Reading slot information...", async _ =>
            {
                foreach (var slot in StandardSlots)
                {
                    var (algorithm, pinPolicy, touchPolicy, hasCert) = await GetSlotInfoAsync(session, slot, cancellationToken);

                    if (algorithm != "-")
                    {
                        hasAnyKey = true;
                    }

                    table.AddRow(
                        $"[yellow]{GetSlotHex(slot)}[/]",
                        GetSlotName(slot),
                        algorithm,
                        pinPolicy,
                        touchPolicy,
                        hasCert);
                }

                // Add retired slots section
                table.AddEmptyRow();
                table.AddRow("[grey]---[/]", "[grey]Retired Slots[/]", "[grey]---[/]", "[grey]---[/]", "[grey]---[/]", "[grey]---[/]");

                for (var i = 1; i <= 20; i++)
                {
                    var retiredSlot = GetRetiredSlot(i);
                    if (retiredSlot is null)
                    {
                        continue;
                    }

                    var (algorithm, pinPolicy, touchPolicy, hasCert) = await GetSlotInfoAsync(session, retiredSlot.Value, cancellationToken);

                    if (algorithm != "-")
                    {
                        hasAnyKey = true;
                        table.AddRow(
                            $"[yellow]{GetSlotHex(retiredSlot.Value)}[/]",
                            $"Retired {i}",
                            algorithm,
                            pinPolicy,
                            touchPolicy,
                            hasCert);
                    }
                }
            });

        AnsiConsole.Write(table);

        if (!hasAnyKey)
        {
            AnsiConsole.WriteLine();
            OutputHelpers.WriteInfo("All slots are empty. Use Key Generation to create keys.");
        }

        // Quick access option
        AnsiConsole.WriteLine();
        if (AnsiConsole.Confirm("View detailed info for a specific slot?", defaultValue: false))
        {
            await ViewSlotDetailsAsync(session, cancellationToken);
        }
    }

    /// <summary>
    /// Gets information about a slot.
    /// </summary>
    private static async Task<(string Algorithm, string PinPolicy, string TouchPolicy, string HasCert)> GetSlotInfoAsync(
        IPivSession session,
        PivSlot slot,
        CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
            if (metadata is null)
            {
                return ("-", "-", "-", "-");
            }

            var slotMetadata = metadata.Value;
            var cert = await session.GetCertificateAsync(slot, cancellationToken);

            return (
                FormatAlgorithm(slotMetadata.Algorithm),
                FormatPinPolicy(slotMetadata.PinPolicy),
                FormatTouchPolicy(slotMetadata.TouchPolicy),
                cert is not null ? "[green]Yes[/]" : "[grey]No[/]"
            );
        }
        catch
        {
            return ("-", "-", "-", "-");
        }
    }

    /// <summary>
    /// Views detailed information about a specific slot.
    /// </summary>
    private static async Task ViewSlotDetailsAsync(IPivSession session, CancellationToken cancellationToken)
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
                .Title("Select slot:")
                .AddChoices(choices));

        var slot = selection[..2] switch
        {
            "9A" => PivSlot.Authentication,
            "9C" => PivSlot.Signature,
            "9D" => PivSlot.KeyManagement,
            "9E" => PivSlot.CardAuthentication,
            _ => PivSlot.Authentication
        };

        try
        {
            var metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
            var cert = await session.GetCertificateAsync(slot, cancellationToken);

            var tree = new Tree($"[blue]Slot {GetSlotHex(slot)} - {GetSlotName(slot)}[/]")
                .Guide(TreeGuide.Line);

            if (metadata is null)
            {
                tree.AddNode("[grey]Empty - No key present[/]");
            }
            else
            {
                var slotMetadata = metadata.Value;
                var keyNode = tree.AddNode("[green]Key Information[/]");
                keyNode.AddNode($"Algorithm: {slotMetadata.Algorithm}");
                keyNode.AddNode($"PIN Policy: {slotMetadata.PinPolicy}");
                keyNode.AddNode($"Touch Policy: {slotMetadata.TouchPolicy}");
                keyNode.AddNode($"Generated On-Device: {slotMetadata.IsGenerated}");

                if (slotMetadata.PublicKey.Length > 0)
                {
                    keyNode.AddNode($"Public Key Length: {slotMetadata.PublicKey.Length} bytes");
                }
            }

            if (cert is not null)
            {
                var certNode = tree.AddNode("[yellow]Certificate[/]");
                certNode.AddNode($"Subject: {Markup.Escape(cert.Subject)}");
                certNode.AddNode($"Issuer: {Markup.Escape(cert.Issuer)}");
                certNode.AddNode($"Valid: {cert.NotBefore:yyyy-MM-dd} to {cert.NotAfter:yyyy-MM-dd}");
                certNode.AddNode($"Thumbprint: {cert.Thumbprint}");

                var isValid = cert.NotBefore <= DateTime.UtcNow && cert.NotAfter >= DateTime.UtcNow;
                certNode.AddNode($"Status: {(isValid ? "[green]Valid[/]" : "[red]Expired/Invalid[/]")}");
            }
            else
            {
                tree.AddNode("[grey]No certificate stored[/]");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.Write(tree);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to read slot: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the hex representation of a slot.
    /// </summary>
    private static string GetSlotHex(PivSlot slot) =>
        $"0x{(byte)slot:X2}";

    /// <summary>
    /// Gets the friendly name of a slot.
    /// </summary>
    private static string GetSlotName(PivSlot slot) =>
        slot switch
        {
            PivSlot.Authentication => "Authentication",
            PivSlot.Signature => "Digital Signature",
            PivSlot.KeyManagement => "Key Management",
            PivSlot.CardAuthentication => "Card Authentication",
            _ => slot.ToString()
        };

    /// <summary>
    /// Gets a retired slot by number.
    /// </summary>
    private static PivSlot? GetRetiredSlot(int number) =>
        number switch
        {
            1 => PivSlot.Retired1,
            2 => PivSlot.Retired2,
            3 => PivSlot.Retired3,
            4 => PivSlot.Retired4,
            5 => PivSlot.Retired5,
            6 => PivSlot.Retired6,
            7 => PivSlot.Retired7,
            8 => PivSlot.Retired8,
            9 => PivSlot.Retired9,
            10 => PivSlot.Retired10,
            11 => PivSlot.Retired11,
            12 => PivSlot.Retired12,
            13 => PivSlot.Retired13,
            14 => PivSlot.Retired14,
            15 => PivSlot.Retired15,
            16 => PivSlot.Retired16,
            17 => PivSlot.Retired17,
            18 => PivSlot.Retired18,
            19 => PivSlot.Retired19,
            20 => PivSlot.Retired20,
            _ => null
        };

    /// <summary>
    /// Formats an algorithm for display.
    /// </summary>
    private static string FormatAlgorithm(PivAlgorithm algorithm) =>
        algorithm switch
        {
            PivAlgorithm.Rsa1024 => "[yellow]RSA-1024[/]",
            PivAlgorithm.Rsa2048 => "[green]RSA-2048[/]",
            PivAlgorithm.Rsa3072 => "[green]RSA-3072[/]",
            PivAlgorithm.Rsa4096 => "[green]RSA-4096[/]",
            PivAlgorithm.EccP256 => "[cyan]ECC P-256[/]",
            PivAlgorithm.EccP384 => "[cyan]ECC P-384[/]",
            PivAlgorithm.Ed25519 => "[blue]Ed25519[/]",
            PivAlgorithm.X25519 => "[blue]X25519[/]",
            _ => algorithm.ToString()
        };

    /// <summary>
    /// Formats a PIN policy for display.
    /// </summary>
    private static string FormatPinPolicy(PivPinPolicy policy) =>
        policy switch
        {
            PivPinPolicy.Default => "[grey]Default[/]",
            PivPinPolicy.Never => "[yellow]Never[/]",
            PivPinPolicy.Once => "[green]Once[/]",
            PivPinPolicy.Always => "[cyan]Always[/]",
            PivPinPolicy.MatchOnce => "[blue]MatchOnce[/]",
            PivPinPolicy.MatchAlways => "[blue]MatchAlways[/]",
            _ => policy.ToString()
        };

    /// <summary>
    /// Formats a touch policy for display.
    /// </summary>
    private static string FormatTouchPolicy(PivTouchPolicy policy) =>
        policy switch
        {
            PivTouchPolicy.Default => "[grey]Default[/]",
            PivTouchPolicy.Never => "[yellow]Never[/]",
            PivTouchPolicy.Always => "[cyan]Always[/]",
            PivTouchPolicy.Cached => "[green]Cached[/]",
            _ => policy.ToString()
        };
}
