// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <summary>
/// Result of PIN retries query.
/// </summary>
/// <param name="Retries">The number of PIN retries remaining.</param>
/// <param name="PowerCycleRequired">Indicates if the authenticator requires a power cycle to retry PIN.</param>
public sealed record class PinRetriesResult(int Retries, bool PowerCycleRequired);

/// <summary>
/// Internal abstraction over CTAP2 operations for testability.
/// </summary>
/// <remarks>
/// This interface allows WebAuthn Client logic to be tested with mock implementations
/// while the production implementation delegates to IFidoSession.
/// </remarks>
public interface IWebAuthnBackend : IAsyncDisposable
{
    /// <summary>
    /// Gets cached authenticator info (does not require user presence).
    /// </summary>
    Task<AuthenticatorInfo> GetCachedInfoAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the number of user verification retries remaining.
    /// </summary>
    Task<int> GetUvRetriesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the number of PIN retries remaining and power-cycle state.
    /// </summary>
    Task<PinRetriesResult> GetPinRetriesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Obtains a PIN/UV auth token with the specified permissions.
    /// </summary>
    /// <param name="method">The authentication method (PIN or UV).</param>
    /// <param name="permissions">The requested token permissions.</param>
    /// <param name="rpId">Optional RP ID to bind the token to (required for some permissions).</param>
    /// <param name="pinBytes">PIN bytes (UTF-8 encoded) when method is PIN, otherwise null.</param>
    /// <param name="progress">Optional progress reporter for CTAP status updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A session holding the PIN/UV auth token and protocol instance.</returns>
    Task<PinUvAuthTokenSession> GetPinUvTokenAsync(
        PinUvAuthMethod method,
        PinUvAuthTokenPermissions permissions,
        string? rpId,
        ReadOnlyMemory<byte>? pinBytes,
        IProgress<CtapStatus>? progress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new credential via CTAP2 MakeCredential.
    /// </summary>
    Task<MakeCredentialResponse> MakeCredentialAsync(
        BackendMakeCredentialRequest request,
        IProgress<CtapStatus>? progress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Performs CTAP2 GetAssertion to authenticate with a credential.
    /// </summary>
    /// <remarks>Implemented in Phase 4.</remarks>
    Task<GetAssertionResponse> GetAssertionAsync(
        BackendGetAssertionRequest request,
        IProgress<CtapStatus>? progress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the next assertion when multiple credentials are available.
    /// </summary>
    /// <remarks>Implemented in Phase 4.</remarks>
    Task<GetAssertionResponse> GetNextAssertionAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Authentication method for PIN/UV token acquisition.
/// </summary>
public enum PinUvAuthMethod
{
    /// <summary>
    /// Use PIN for authentication.
    /// </summary>
    Pin,

    /// <summary>
    /// Use built-in user verification (biometric, etc.).
    /// </summary>
    Uv
}

/// <summary>
/// Request parameters for CTAP2 MakeCredential via backend.
/// </summary>
public sealed record class BackendMakeCredentialRequest
{
    /// <summary>
    /// Hash of the client data JSON.
    /// </summary>
    public required ReadOnlyMemory<byte> ClientDataHash { get; init; }

    /// <summary>
    /// Relying party information.
    /// </summary>
    public required WebAuthnRelyingParty Rp { get; init; }

    /// <summary>
    /// User information.
    /// </summary>
    public required WebAuthnUser User { get; init; }

    /// <summary>
    /// Supported public key credential parameters.
    /// </summary>
    public required IReadOnlyList<PublicKeyCredentialParameters> PubKeyCredParams { get; init; }

    /// <summary>
    /// Credentials to exclude (already registered).
    /// </summary>
    public IReadOnlyList<WebAuthnCredentialDescriptor>? ExcludeList { get; init; }

    /// <summary>
    /// Raw CBOR-encoded extensions map (opaque passthrough for Phase 3).
    /// </summary>
    public ReadOnlyMemory<byte>? Extensions { get; init; }

    /// <summary>
    /// Authenticator options (e.g., rk, uv).
    /// </summary>
    public IReadOnlyDictionary<string, bool>? Options { get; init; }

    /// <summary>
    /// PIN/UV auth parameter (signature over clientDataHash).
    /// </summary>
    public ReadOnlyMemory<byte>? PinUvAuthParam { get; init; }

    /// <summary>
    /// PIN/UV auth protocol version (1 or 2).
    /// </summary>
    public byte? PinUvAuthProtocol { get; init; }

    /// <summary>
    /// Enterprise attestation mode.
    /// </summary>
    public int? EnterpriseAttestation { get; init; }
}

/// <summary>
/// Request parameters for CTAP2 GetAssertion via backend.
/// </summary>
public sealed record class BackendGetAssertionRequest
{
    /// <summary>
    /// Hash of the client data JSON.
    /// </summary>
    public required ReadOnlyMemory<byte> ClientDataHash { get; init; }

    /// <summary>
    /// Relying party identifier.
    /// </summary>
    public required string RpId { get; init; }

    /// <summary>
    /// List of allowed credential descriptors.
    /// </summary>
    /// <remarks>
    /// If null or empty, the authenticator will search for discoverable credentials.
    /// </remarks>
    public IReadOnlyList<PublicKeyCredentialDescriptor>? AllowList { get; init; }

    /// <summary>
    /// Raw CBOR-encoded extensions map (opaque passthrough).
    /// </summary>
    public ReadOnlyMemory<byte>? Extensions { get; init; }

    /// <summary>
    /// Authenticator options (e.g., up, uv).
    /// </summary>
    public IReadOnlyDictionary<string, bool>? Options { get; init; }

    /// <summary>
    /// PIN/UV auth parameter (signature over clientDataHash).
    /// </summary>
    public ReadOnlyMemory<byte>? PinUvAuthParam { get; init; }

    /// <summary>
    /// PIN/UV auth protocol version (1 or 2).
    /// </summary>
    public byte? PinUvAuthProtocol { get; init; }
}
