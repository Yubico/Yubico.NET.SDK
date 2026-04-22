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
using Fido2AttestationStatement = Yubico.YubiKit.Fido2.Credentials.AttestationStatement;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <summary>
/// Concrete implementation of IWebAuthnBackend that wraps IFidoSession.
/// </summary>
/// <remarks>
/// This adapter owns the FidoSession lifetime and manages the PinUvAuthProtocolV2 instance.
/// </remarks>
internal sealed class FidoSessionWebAuthnBackend : IWebAuthnBackend
{
    private readonly IFidoSession _session;
    private PinUvAuthProtocolV2? _protocol;
    private AuthenticatorInfo? _cachedInfo;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="FidoSessionWebAuthnBackend"/>.
    /// </summary>
    /// <param name="session">The FIDO session (ownership transferred to this backend).</param>
    public FidoSessionWebAuthnBackend(IFidoSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <inheritdoc/>
    public async Task<AuthenticatorInfo> GetCachedInfoAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_cachedInfo is null)
        {
            _cachedInfo = await _session.GetInfoAsync(cancellationToken).ConfigureAwait(false);
        }

        return _cachedInfo;
    }

    /// <inheritdoc/>
    public async Task<int> GetUvRetriesAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        EnsureProtocolInitialized();
        var clientPin = new ClientPin(_session, _protocol!);

        var (retries, _) = await clientPin.GetUvRetriesAsync(cancellationToken).ConfigureAwait(false);
        return retries;
    }

    /// <inheritdoc/>
    public async Task<PinRetriesResult> GetPinRetriesAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        EnsureProtocolInitialized();
        var clientPin = new ClientPin(_session, _protocol!);

        var (retries, powerCycleRequired) = await clientPin.GetPinRetriesAsync(cancellationToken).ConfigureAwait(false);
        return new PinRetriesResult(retries, powerCycleRequired);
    }

    /// <inheritdoc/>
    public async Task<PinUvAuthTokenSession> GetPinUvTokenAsync(
        PinUvAuthMethod method,
        PinUvAuthTokenPermissions permissions,
        string? rpId,
        ReadOnlyMemory<byte>? pinBytes,
        IProgress<CtapStatus>? progress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        EnsureProtocolInitialized();
        var clientPin = new ClientPin(_session, _protocol!);

        byte[] token = method switch
        {
            PinUvAuthMethod.Pin when pinBytes is not null =>
                await clientPin.GetPinUvAuthTokenUsingPinAsync(pinBytes.Value, permissions, rpId, cancellationToken)
                    .ConfigureAwait(false),

            PinUvAuthMethod.Uv =>
                await clientPin.GetPinUvAuthTokenUsingUvAsync(permissions, rpId, cancellationToken)
                    .ConfigureAwait(false),

            PinUvAuthMethod.Pin =>
                throw new ArgumentNullException(nameof(pinBytes), "PIN bytes required when method is PIN"),

            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Invalid PIN/UV auth method")
        };

        return new PinUvAuthTokenSession(_protocol!, token);
    }

    /// <inheritdoc/>
    public async Task<MakeCredentialResponse> MakeCredentialAsync(
        BackendMakeCredentialRequest request,
        IProgress<CtapStatus>? progress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ArgumentNullException.ThrowIfNull(request);

        // Map WebAuthnRelyingParty to PublicKeyCredentialRpEntity
        var rpEntity = new PublicKeyCredentialRpEntity(request.Rp.Id, request.Rp.Name);

        // Map WebAuthnUser to PublicKeyCredentialUserEntity
        var userEntity = new PublicKeyCredentialUserEntity(
            request.User.Id,
            request.User.Name!,
            request.User.DisplayName!);

        // Build options
        var options = new MakeCredentialOptions
        {
            ExcludeList = request.ExcludeList?.Select(desc => new PublicKeyCredentialDescriptor(
                desc.Id,
                desc.Type,
                desc.Transports?.Select(t => t.Value).ToList()
            )).ToList(),

            ResidentKey = request.Options?.TryGetValue("rk", out var rk) == true && rk,
            UserVerification = request.Options?.TryGetValue("uv", out var uv) == true && uv
        };

        // Add PIN/UV auth if provided
        if (request.PinUvAuthParam is not null && request.PinUvAuthProtocol is not null)
        {
            options.PinUvAuthParam = request.PinUvAuthParam.Value.ToArray();
            options.PinUvAuthProtocol = request.PinUvAuthProtocol.Value;
        }

        // Extensions passthrough (opaque for Phase 3)
        if (request.Extensions is not null)
        {
            // For Phase 3, extensions are passed as raw CBOR - defer to Phase 6 for full wiring
        }

        var response = await _session.MakeCredentialAsync(
            request.ClientDataHash,
            rpEntity,
            userEntity,
            request.PubKeyCredParams,
            options,
            cancellationToken
        ).ConfigureAwait(false);

        return response;
    }

    /// <inheritdoc/>
    public Task<GetAssertionResponse> GetAssertionAsync(
        BackendGetAssertionRequest request,
        IProgress<CtapStatus>? progress,
        CancellationToken cancellationToken)
    {
        // Phase 4 implementation
        throw new NotImplementedException("GetAssertion is implemented in Phase 4");
    }

    /// <inheritdoc/>
    public Task<GetAssertionResponse> GetNextAssertionAsync(CancellationToken cancellationToken)
    {
        // Phase 4 implementation
        throw new NotImplementedException("GetNextAssertion is implemented in Phase 4");
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {

            _protocol?.Dispose();
            _protocol = null;

            if (_session is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (_session is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _disposed = true;
        }
    }

    private void EnsureProtocolInitialized()
    {
        if (_protocol is null)
        {
            _protocol = new PinUvAuthProtocolV2();
            // Protocol initialization is async in the session context, but we defer it
            // until the first use in ClientPin methods which handle initialization
        }
    }
}
