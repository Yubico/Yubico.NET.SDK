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
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Fido2.CredentialManagement;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.Examples.FidoTool.FidoExamples;

/// <summary>
/// Credential management operations: enumerate, delete, and update stored credentials.
/// Requires firmware 5.2+ and the credentialMgmt authenticator option.
/// </summary>
public static class CredentialManagementExample
{
    /// <summary>
    /// Result of a credential metadata query.
    /// </summary>
    public sealed record MetadataResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public CredentialMetadata? Metadata { get; init; }

        public static MetadataResult Succeeded(CredentialMetadata metadata) =>
            new() { Success = true, Metadata = metadata };

        public static MetadataResult Failed(string error) =>
            new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Result of enumerating relying parties.
    /// </summary>
    public sealed record EnumerateRpsResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public IReadOnlyList<RelyingPartyInfo> RelyingParties { get; init; } = [];

        public static EnumerateRpsResult Succeeded(IReadOnlyList<RelyingPartyInfo> rps) =>
            new() { Success = true, RelyingParties = rps };

        public static EnumerateRpsResult Failed(string error) =>
            new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Result of enumerating credentials for a relying party.
    /// </summary>
    public sealed record EnumerateCredentialsResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public IReadOnlyList<StoredCredentialInfo> Credentials { get; init; } = [];

        public static EnumerateCredentialsResult Succeeded(IReadOnlyList<StoredCredentialInfo> creds) =>
            new() { Success = true, Credentials = creds };

        public static EnumerateCredentialsResult Failed(string error) =>
            new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Result of a credential management operation (delete, update).
    /// </summary>
    public sealed record CredMgmtResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }

        public static CredMgmtResult Succeeded() => new() { Success = true };
        public static CredMgmtResult Failed(string error) => new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Gets credential storage metadata (existing count and remaining slots).
    /// </summary>
    public static async Task<MetadataResult> GetMetadataAsync(
        IYubiKey yubiKey,
        ReadOnlyMemory<byte> pinUtf8,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinToken = null;
        try
        {
            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pinUtf8,
                PinUvAuthTokenPermissions.CredentialManagement,
                cancellationToken: cancellationToken);

            var credMgmt = new Fido2.CredentialManagement.CredentialManagement(
                session, protocol, pinToken);

            var metadata = await credMgmt.GetCredentialsMetadataAsync(cancellationToken);

            return MetadataResult.Succeeded(metadata);
        }
        catch (CtapException ex)
        {
            return MetadataResult.Failed(MapCtapError(ex));
        }
        catch (Exception ex)
        {
            return MetadataResult.Failed($"Failed to get credential metadata: {ex.Message}");
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }

    /// <summary>
    /// Enumerates all relying parties with stored credentials, and all credentials per RP.
    /// </summary>
    public static async Task<EnumerateRpsResult> EnumerateRelyingPartiesAsync(
        IYubiKey yubiKey,
        ReadOnlyMemory<byte> pinUtf8,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinToken = null;
        try
        {
            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pinUtf8,
                PinUvAuthTokenPermissions.CredentialManagement,
                cancellationToken: cancellationToken);

            var credMgmt = new Fido2.CredentialManagement.CredentialManagement(
                session, protocol, pinToken);

            var rps = await credMgmt.EnumerateRelyingPartiesAsync(cancellationToken);

            return EnumerateRpsResult.Succeeded(rps);
        }
        catch (CtapException ex)
        {
            return EnumerateRpsResult.Failed(MapCtapError(ex));
        }
        catch (Exception ex)
        {
            return EnumerateRpsResult.Failed($"Failed to enumerate relying parties: {ex.Message}");
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }

    /// <summary>
    /// Enumerates all credentials for a specific relying party.
    /// </summary>
    public static async Task<EnumerateCredentialsResult> EnumerateCredentialsAsync(
        IYubiKey yubiKey,
        ReadOnlyMemory<byte> pinUtf8,
        ReadOnlyMemory<byte> rpIdHash,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinToken = null;
        try
        {
            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pinUtf8,
                PinUvAuthTokenPermissions.CredentialManagement,
                cancellationToken: cancellationToken);

            var credMgmt = new Fido2.CredentialManagement.CredentialManagement(
                session, protocol, pinToken);

            var creds = await credMgmt.EnumerateCredentialsAsync(rpIdHash, cancellationToken);

            return EnumerateCredentialsResult.Succeeded(creds);
        }
        catch (CtapException ex)
        {
            return EnumerateCredentialsResult.Failed(MapCtapError(ex));
        }
        catch (Exception ex)
        {
            return EnumerateCredentialsResult.Failed($"Failed to enumerate credentials: {ex.Message}");
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }

    /// <summary>
    /// Deletes a credential by its ID.
    /// </summary>
    public static async Task<CredMgmtResult> DeleteCredentialAsync(
        IYubiKey yubiKey,
        ReadOnlyMemory<byte> pinUtf8,
        ReadOnlyMemory<byte> credentialId,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinToken = null;
        try
        {
            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pinUtf8,
                PinUvAuthTokenPermissions.CredentialManagement,
                cancellationToken: cancellationToken);

            var credMgmt = new Fido2.CredentialManagement.CredentialManagement(
                session, protocol, pinToken);

            var descriptor = new PublicKeyCredentialDescriptor(credentialId);
            await credMgmt.DeleteCredentialAsync(descriptor, cancellationToken);

            return CredMgmtResult.Succeeded();
        }
        catch (CtapException ex)
        {
            return CredMgmtResult.Failed(MapCtapError(ex));
        }
        catch (Exception ex)
        {
            return CredMgmtResult.Failed($"Failed to delete credential: {ex.Message}");
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }

    /// <summary>
    /// Updates user information on an existing credential.
    /// </summary>
    public static async Task<CredMgmtResult> UpdateUserInfoAsync(
        IYubiKey yubiKey,
        ReadOnlyMemory<byte> pinUtf8,
        ReadOnlyMemory<byte> credentialId,
        string userName,
        string displayName,
        ReadOnlyMemory<byte> userId,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinToken = null;
        try
        {
            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pinUtf8,
                PinUvAuthTokenPermissions.CredentialManagement,
                cancellationToken: cancellationToken);

            var credMgmt = new Fido2.CredentialManagement.CredentialManagement(
                session, protocol, pinToken);

            var descriptor = new PublicKeyCredentialDescriptor(credentialId);
            var user = new PublicKeyCredentialUserEntity(userId, userName, displayName);
            await credMgmt.UpdateUserInformationAsync(descriptor, user, cancellationToken);

            return CredMgmtResult.Succeeded();
        }
        catch (CtapException ex)
        {
            return CredMgmtResult.Failed(MapCtapError(ex));
        }
        catch (Exception ex)
        {
            return CredMgmtResult.Failed($"Failed to update user info: {ex.Message}");
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }

    /// <summary>
    /// Checks if the authenticator supports credential management.
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
            return info.Options.TryGetValue("credMgmt", out var supported) && supported;
        }
        catch
        {
            return false;
        }
    }

    private static string MapCtapError(CtapException ex) =>
        ex.Status switch
        {
            CtapStatus.NoCredentials => "No credentials found on this authenticator.",
            CtapStatus.PinInvalid => "The PIN is incorrect.",
            CtapStatus.PinBlocked => "The PIN is blocked. The authenticator must be reset.",
            CtapStatus.PinAuthInvalid => "PIN authentication failed.",
            CtapStatus.PinNotSet => "No PIN is set. Set a PIN first.",
            CtapStatus.NotAllowed => "Operation not allowed. Credential management may not be supported.",
            CtapStatus.KeyStoreFull => "The authenticator's credential storage is full.",
            _ => $"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})"
        };
}
