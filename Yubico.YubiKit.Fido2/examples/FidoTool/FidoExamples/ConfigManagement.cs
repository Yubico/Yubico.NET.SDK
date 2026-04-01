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
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Fido2.Config;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.Examples.FidoTool.FidoExamples;

/// <summary>
/// Authenticator configuration operations: enterprise attestation, always-UV, min PIN length.
/// Requires firmware 5.4+ and the authnrCfg authenticator option.
/// </summary>
public static class ConfigManagement
{
    /// <summary>
    /// Result of a configuration operation.
    /// </summary>
    public sealed record ConfigResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }

        public static ConfigResult Succeeded() => new() { Success = true };
        public static ConfigResult Failed(string error) => new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Enables enterprise attestation on the authenticator.
    /// </summary>
    public static async Task<ConfigResult> EnableEnterpriseAttestationAsync(
        IYubiKey yubiKey,
        string pin,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinBytes = null;
        byte[]? pinToken = null;
        try
        {
            pinBytes = Encoding.UTF8.GetBytes(pin);

            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pin,
                PinUvAuthTokenPermissions.AuthenticatorConfig,
                cancellationToken: cancellationToken);

            var config = new AuthenticatorConfig(session, protocol, pinToken);
            await config.EnableEnterpriseAttestationAsync(cancellationToken);

            return ConfigResult.Succeeded();
        }
        catch (CtapException ex)
        {
            return ConfigResult.Failed(MapCtapError(ex));
        }
        catch (Exception ex)
        {
            return ConfigResult.Failed($"Failed to enable enterprise attestation: {ex.Message}");
        }
        finally
        {
            if (pinBytes is not null)
            {
                CryptographicOperations.ZeroMemory(pinBytes);
            }

            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }

    /// <summary>
    /// Toggles the always-UV setting on the authenticator.
    /// </summary>
    public static async Task<ConfigResult> ToggleAlwaysUvAsync(
        IYubiKey yubiKey,
        string pin,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinBytes = null;
        byte[]? pinToken = null;
        try
        {
            pinBytes = Encoding.UTF8.GetBytes(pin);

            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pin,
                PinUvAuthTokenPermissions.AuthenticatorConfig,
                cancellationToken: cancellationToken);

            var config = new AuthenticatorConfig(session, protocol, pinToken);
            await config.ToggleAlwaysUvAsync(cancellationToken);

            return ConfigResult.Succeeded();
        }
        catch (CtapException ex)
        {
            return ConfigResult.Failed(MapCtapError(ex));
        }
        catch (Exception ex)
        {
            return ConfigResult.Failed($"Failed to toggle always-UV: {ex.Message}");
        }
        finally
        {
            if (pinBytes is not null)
            {
                CryptographicOperations.ZeroMemory(pinBytes);
            }

            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }

    /// <summary>
    /// Sets the minimum PIN length on the authenticator.
    /// </summary>
    public static async Task<ConfigResult> SetMinPinLengthAsync(
        IYubiKey yubiKey,
        string pin,
        int newMinPinLength,
        IReadOnlyList<string>? rpIds = null,
        bool forceChangePin = false,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinBytes = null;
        byte[]? pinToken = null;
        try
        {
            pinBytes = Encoding.UTF8.GetBytes(pin);

            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pin,
                PinUvAuthTokenPermissions.AuthenticatorConfig,
                cancellationToken: cancellationToken);

            var config = new AuthenticatorConfig(session, protocol, pinToken);
            await config.SetMinPinLengthAsync(newMinPinLength, rpIds, forceChangePin, cancellationToken);

            return ConfigResult.Succeeded();
        }
        catch (CtapException ex)
        {
            return ConfigResult.Failed(MapCtapError(ex));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return ConfigResult.Failed(ex.Message);
        }
        catch (Exception ex)
        {
            return ConfigResult.Failed($"Failed to set minimum PIN length: {ex.Message}");
        }
        finally
        {
            if (pinBytes is not null)
            {
                CryptographicOperations.ZeroMemory(pinBytes);
            }

            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }

    /// <summary>
    /// Checks if the authenticator supports authenticator configuration.
    /// </summary>
    public static async Task<bool> IsSupported(
        IYubiKey yubiKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);
            var info = await session.GetInfoAsync(cancellationToken);
            return info.Options.TryGetValue("authnrCfg", out var supported) && supported;
        }
        catch
        {
            return false;
        }
    }

    private static string MapCtapError(CtapException ex) =>
        ex.Status switch
        {
            CtapStatus.PinInvalid => "The PIN is incorrect.",
            CtapStatus.PinBlocked => "The PIN is blocked. The authenticator must be reset.",
            CtapStatus.PinAuthInvalid => "PIN authentication failed.",
            CtapStatus.PinNotSet => "No PIN is set. Set a PIN first.",
            CtapStatus.NotAllowed => "Operation not allowed. Authenticator config may not be supported.",
            CtapStatus.InvalidCommand => "This configuration operation is not supported on this authenticator.",
            CtapStatus.PinPolicyViolation =>
                "The new minimum PIN length violates the authenticator's PIN policy.",
            _ => $"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})"
        };
}
