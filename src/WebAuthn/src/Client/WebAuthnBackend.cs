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

using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Cbor;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.LargeBlobs;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.WebAuthn.Util;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <summary>
/// Concrete implementation of IWebAuthnBackend that wraps ICtap2Session.
/// </summary>
/// <remarks>
/// This adapter owns the CTAP session and the PIN/UV protocol selected from GetInfo.
/// </remarks>
internal sealed class WebAuthnBackend : IWebAuthnBackend
{
    private readonly ICtap2Session _session;
    private IPinUvAuthProtocol? _protocol;
    private AuthenticatorInfo? _cachedInfo;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="WebAuthnBackend"/>.
    /// </summary>
    /// <param name="session">The FIDO session (ownership transferred to this backend).</param>
    public WebAuthnBackend(ICtap2Session session)
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
            _protocol = _cachedInfo.PinUvAuthProtocols.Contains(2) ? new PinUvAuthProtocolV2()
                : _cachedInfo.PinUvAuthProtocols.Contains(1) ? new PinUvAuthProtocolV1()
                : null;
        }

        return _cachedInfo;
    }

    /// <inheritdoc/>
    public IPinUvAuthProtocol Protocol
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _protocol ?? throw new WebAuthnClientError(WebAuthnClientErrorCode.NotSupported,
                "The authenticator does not advertise a supported PIN/UV protocol.");
        }
    }

    /// <inheritdoc/>
    public async Task<PinUvSharedSecret> EncapsulateAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var protocol = Protocol;
        var clientPin = new ClientPin(_session, protocol);
        var authenticatorKey = await clientPin.GetKeyAgreementAsync(cancellationToken).ConfigureAwait(false);
        var (keyAgreement, sharedSecret) = protocol.Encapsulate(authenticatorKey);
        return new PinUvSharedSecret(keyAgreement, sharedSecret);
    }

    /// <inheritdoc/>
    public ILargeBlobStorage CreateLargeBlobStorage(PinUvAuthTokenSession? token)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return token is null
            ? new LargeBlobStorage(_session)
            : new LargeBlobStorage(_session, token.Protocol, token.Token);
    }

    /// <inheritdoc/>
    public async Task<int?> GetPinRetriesAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var clientPin = new ClientPin(_session, Protocol);

        var status = await clientPin.GetPinRetriesAsync(cancellationToken).ConfigureAwait(false);
        return status.RetriesRemaining;
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

        var protocol = Protocol;
        var clientPin = new ClientPin(_session, protocol);

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
        return new PinUvAuthTokenSession(protocol, token);
    }

    /// <inheritdoc/>
    public async Task<MakeCredentialResponse> MakeCredentialAsync(
        BackendMakeCredentialRequest request,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ArgumentNullException.ThrowIfNull(request);

        var options = ProjectMakeCredentialOptions(request);
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

    /// <summary>Checks the full command, including the command byte, without sending it.</summary>
    internal static bool FitsMakeCredential(BackendMakeCredentialRequest request, AuthenticatorInfo info)
    {
        var options = ProjectMakeCredentialOptions(request);
        byte[]? encoded = null;
        try
        {
            encoded = Ctap2SessionRequestEncoding.BuildMakeCredentialRequest(
                request.ClientDataHash, request.Rp, request.User, request.PubKeyCredParams, options);
            return encoded.Length <= (info.MaxMsgSize ?? Ctap2Session.DefaultMaxMessageSize);
        }
        finally
        {
            SensitiveMemory.Zero(encoded);
            SensitiveMemory.Zero(options.PinUvAuthParam);
        }
    }

    private static MakeCredentialOptions ProjectMakeCredentialOptions(BackendMakeCredentialRequest request)
    {
        var options = new MakeCredentialOptions
        {
            ExcludeList = request.ExcludeList,

            // Absent stays absent, as in GetAssertionAsync: an explicit "uv": false is still the uv
            // option key, which CTAP §6.1 forbids next to pinUvAuthParam.
            ResidentKey = request.Options?.TryGetValue("rk", out var rk) == true ? rk : null,
            UserVerification = request.Options?.TryGetValue("uv", out var uv) == true ? uv : null,
            EnterpriseAttestation = request.EnterpriseAttestation,
            Extensions = request.Extensions
        };

        // Add PIN/UV auth if provided
        if (request.PinUvAuthParam is not null && request.PinUvAuthProtocol is not null)
        {
            // The caller owns the source parameter; the backend clears only its projection's copy.
            options.PinUvAuthParam = request.PinUvAuthParam.Value.ToArray();
            options.PinUvAuthProtocol = request.PinUvAuthProtocol.Value;
        }

        return options;
    }

    /// <inheritdoc/>
    public async Task<GetAssertionResponse> GetAssertionAsync(
        BackendGetAssertionRequest request,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ArgumentNullException.ThrowIfNull(request);

        var options = ProjectGetAssertionOptions(request);
        try
        {
            return await _session.GetAssertionAsync(
                request.RpId, request.ClientDataHash, options, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            SensitiveMemory.Zero(options.PinUvAuthParam);
        }
    }

    internal static bool FitsGetAssertion(BackendGetAssertionRequest request, AuthenticatorInfo info)
    {
        var options = ProjectGetAssertionOptions(request);
        byte[]? encoded = null;
        try
        {
            encoded = Ctap2SessionRequestEncoding.BuildGetAssertionRequest(request.RpId, request.ClientDataHash, options);
            return encoded.Length <= (info.MaxMsgSize ?? Ctap2Session.DefaultMaxMessageSize);
        }
        finally
        {
            SensitiveMemory.Zero(encoded);
            SensitiveMemory.Zero(options.PinUvAuthParam);
        }
    }

    private static GetAssertionOptions ProjectGetAssertionOptions(BackendGetAssertionRequest request)
    {
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
            // Load-bearing copy: see the note in MakeCredentialAsync. CredentialFilter probes
            // several exclude-list chunks with one pinUvAuthParam, so this method must not zero
            // the caller's buffer.
            options.PinUvAuthParam = request.PinUvAuthParam.Value.ToArray();
            options.PinUvAuthProtocol = request.PinUvAuthProtocol.Value;
        }

        if (request.Extensions is not null)
        {
            options.Extensions = request.Extensions;
        }

        return options;
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

}