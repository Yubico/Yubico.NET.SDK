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
using Yubico.YubiKit.WebAuthn.Util;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <summary>
/// Concrete implementation of IWebAuthnBackend that wraps IFidoSession.
/// </summary>
/// <remarks>
/// This adapter owns the FidoSession lifetime and manages the PinUvAuthProtocolV2 instance.
/// </remarks>
internal sealed class WebAuthnBackend : IWebAuthnBackend
{
    private readonly IFidoSession _session;
    private PinUvAuthProtocolV2? _protocol;
    private AuthenticatorInfo? _cachedInfo;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="WebAuthnBackend"/>.
    /// </summary>
    /// <param name="session">The FIDO session (ownership transferred to this backend).</param>
    public WebAuthnBackend(IFidoSession session)
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
    public async Task<int?> GetPinRetriesAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        EnsureProtocolInitialized();
        var clientPin = new ClientPin(_session, _protocol!);

        var (pinRetries, _) = await clientPin.GetPinRetriesAsync(cancellationToken).ConfigureAwait(false);
        return pinRetries;
    }

    /// <inheritdoc/>
    public async Task<PinUvAuthTokenSession> GetPinUvTokenAsync(
        PinUvAuthMethod method,
        PinUvAuthTokenPermissions permissions,
        string? rpId,
        ReadOnlyMemory<byte>? pinBytes,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        EnsureProtocolInitialized();
        var clientPin = new ClientPin(_session, _protocol!);

        // ClientPin allocates and returns the decrypted token to this single caller and keeps no
        // reference to it, so ownership passes straight to the session below. Copying it here (or
        // in the session) would leave a second live plaintext token that nothing zeroes.
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

        // Ownership transfer: the session zeroes this array when the caller disposes it.
        return new PinUvAuthTokenSession(_protocol!, token);
    }

    /// <inheritdoc/>
    public async Task<MakeCredentialResponse> MakeCredentialAsync(
        BackendMakeCredentialRequest request,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ArgumentNullException.ThrowIfNull(request);

        // Build options
        var options = new MakeCredentialOptions
        {
            ExcludeList = request.ExcludeList?.Select(desc => new PublicKeyCredentialDescriptor(
                desc.Id,
                desc.Type,
                desc.Transports
            )).ToList(),

            ResidentKey = request.Options?.TryGetValue("rk", out var rk) == true && rk,
            UserVerification = request.Options?.TryGetValue("uv", out var uv) == true && uv
        };

        // Add PIN/UV auth if provided
        if (request.PinUvAuthParam is not null && request.PinUvAuthProtocol is not null)
        {
            // The copy is load-bearing, not defensive: the finally below zeroes whatever is in
            // options, and callers reuse one pinUvAuthParam across several backend calls (see
            // ExcludeListPreflight's chunk loop). Zeroing the caller's buffer here would make
            // every call after the first send an all-zero parameter. The caller zeroes the
            // original when it is done with it.
            options.PinUvAuthParam = request.PinUvAuthParam.Value.ToArray();
            options.PinUvAuthProtocol = request.PinUvAuthProtocol.Value;
        }

        if (request.Extensions is not null)
        {
            options.Extensions = request.Extensions;
        }

        try
        {
            return await _session.MakeCredentialAsync(
                request.ClientDataHash,
                request.Rp,
                request.User,
                request.PubKeyCredParams,
                options,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            SensitiveMemory.Zero(options.PinUvAuthParam);
        }
    }

    /// <inheritdoc/>
    public async Task<GetAssertionResponse> GetAssertionAsync(
        BackendGetAssertionRequest request,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ArgumentNullException.ThrowIfNull(request);

        // Build options
        var options = new GetAssertionOptions();

        // Map allow list if provided
        if (request.AllowList is not null && request.AllowList.Count > 0)
        {
            options.AllowList = request.AllowList;
        }

        // Set user verification option
        if (request.Options?.TryGetValue("uv", out var uv) == true)
        {
            options.UserVerification = uv;
        }

        if (request.Options?.TryGetValue("up", out var up) == true)
        {
            options.UserPresence = up;
        }

        // Add PIN/UV auth if provided
        if (request.PinUvAuthParam is not null && request.PinUvAuthProtocol is not null)
        {
            // Load-bearing copy: see the note in MakeCredentialAsync. ExcludeListPreflight probes
            // several exclude-list chunks with one pinUvAuthParam, so this method must not zero
            // the caller's buffer.
            options.PinUvAuthParam = request.PinUvAuthParam.Value.ToArray();
            options.PinUvAuthProtocol = request.PinUvAuthProtocol.Value;
        }

        if (request.Extensions is not null)
        {
            options.Extensions = request.Extensions;
        }

        try
        {
            return await _session.GetAssertionAsync(
                request.RpId,
                request.ClientDataHash,
                options,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            SensitiveMemory.Zero(options.PinUvAuthParam);
        }
    }

    /// <inheritdoc/>
    public async Task<GetAssertionResponse> GetNextAssertionAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return await _session.GetNextAssertionAsync(cancellationToken).ConfigureAwait(false);
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