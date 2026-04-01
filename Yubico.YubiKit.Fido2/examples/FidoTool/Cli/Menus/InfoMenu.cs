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
using Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Output;
using Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Prompts;
using Yubico.YubiKit.Fido2.Examples.FidoTool.FidoExamples;

namespace Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Menus;

/// <summary>
/// Interactive menu for displaying authenticator information.
/// </summary>
public static class InfoMenu
{
    /// <summary>
    /// Runs the authenticator info display flow.
    /// </summary>
    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        OutputHelpers.WriteHeader("Authenticator Info");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        var result = await AnsiConsole.Status()
            .StartAsync("Querying authenticator...", async _ =>
                await GetAuthenticatorInfo.QueryAsync(selection.Device, cancellationToken));

        if (!result.Success)
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
            return;
        }

        DisplayAuthenticatorInfo(result.Info!);
    }

    /// <summary>
    /// Displays authenticator info for CLI verb mode (no interactive prompts).
    /// </summary>
    public static void DisplayAuthenticatorInfo(AuthenticatorInfo info)
    {
        // Versions
        OutputHelpers.WriteKeyValue("CTAP Versions", string.Join(", ", info.Versions));

        // AAGUID
        OutputHelpers.WriteHex("AAGUID", info.Aaguid);

        // Firmware version
        if (info.FirmwareVersion is not null)
        {
            OutputHelpers.WriteKeyValue("Firmware Version", info.FirmwareVersion.ToString());
        }

        // Extensions
        if (info.Extensions.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [green]Extensions:[/]");
            foreach (var ext in info.Extensions)
            {
                AnsiConsole.MarkupLine($"    - {Markup.Escape(ext)}");
            }
        }

        // Options
        if (info.Options.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [green]Options:[/]");
            foreach (var (key, value) in info.Options)
            {
                var color = value ? "green" : "grey";
                AnsiConsole.MarkupLine($"    [{color}]{Markup.Escape(key)}: {value}[/]");
            }
        }

        // Algorithms
        if (info.Algorithms.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [green]Algorithms:[/]");
            foreach (var alg in info.Algorithms)
            {
                AnsiConsole.MarkupLine($"    - {Markup.Escape(alg.Type)} ({alg.Algorithm})");
            }
        }

        // Transports
        if (info.Transports.Count > 0)
        {
            OutputHelpers.WriteKeyValue("Transports", string.Join(", ", info.Transports));
        }

        // PIN/UV auth protocols
        if (info.PinUvAuthProtocols.Count > 0)
        {
            OutputHelpers.WriteKeyValue("PIN/UV Auth Protocols",
                string.Join(", ", info.PinUvAuthProtocols.Select(p => $"V{p}")));
        }

        // Limits
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("  [green]Limits:[/]");

        if (info.MaxMsgSize.HasValue)
        {
            OutputHelpers.WriteKeyValue("  Max Message Size", $"{info.MaxMsgSize} bytes");
        }

        if (info.MaxCredentialCountInList.HasValue)
        {
            OutputHelpers.WriteKeyValue("  Max Credentials in List", info.MaxCredentialCountInList.ToString());
        }

        if (info.MaxCredentialIdLength.HasValue)
        {
            OutputHelpers.WriteKeyValue("  Max Credential ID Length", $"{info.MaxCredentialIdLength} bytes");
        }

        if (info.MaxCredBlobLength.HasValue)
        {
            OutputHelpers.WriteKeyValue("  Max CredBlob Length", $"{info.MaxCredBlobLength} bytes");
        }

        if (info.MaxSerializedLargeBlobArray.HasValue)
        {
            OutputHelpers.WriteKeyValue("  Max Large Blob Array", $"{info.MaxSerializedLargeBlobArray} bytes");
        }

        // PIN info
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("  [green]PIN Configuration:[/]");

        if (info.MinPinLength.HasValue)
        {
            OutputHelpers.WriteKeyValue("  Min PIN Length", info.MinPinLength.ToString());
        }

        if (info.MaxPinLength.HasValue)
        {
            OutputHelpers.WriteKeyValue("  Max PIN Length", info.MaxPinLength.ToString());
        }

        if (info.ForcePinChange.HasValue)
        {
            OutputHelpers.WriteBoolValue("  Force PIN Change", info.ForcePinChange.Value);
        }

        if (info.PinComplexityPolicy.HasValue)
        {
            OutputHelpers.WriteBoolValue("  PIN Complexity Policy", info.PinComplexityPolicy.Value);
        }

        // Credential storage
        if (info.RemainingDiscoverableCredentials.HasValue)
        {
            AnsiConsole.WriteLine();
            OutputHelpers.WriteKeyValue("Remaining Credential Slots",
                info.RemainingDiscoverableCredentials.ToString());
        }

        // Attestation formats
        if (info.AttestationFormats.Count > 0)
        {
            OutputHelpers.WriteKeyValue("Attestation Formats",
                string.Join(", ", info.AttestationFormats));
        }

        // Certifications
        if (info.Certifications.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [green]Certifications:[/]");
            foreach (var (name, level) in info.Certifications)
            {
                OutputHelpers.WriteKeyValue($"  {name}", $"Level {level}");
            }
        }
    }
}
