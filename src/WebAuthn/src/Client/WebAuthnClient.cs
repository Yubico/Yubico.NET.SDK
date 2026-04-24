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

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Cryptography.Cose;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Attestation;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Client.Status;
using Yubico.YubiKit.WebAuthn.Client.UserVerification;
using Yubico.YubiKit.WebAuthn.Client.Validation;
using Yubico.YubiKit.WebAuthn.Cose;
using Yubico.YubiKit.WebAuthn.Extensions;
using Fido2AttestationStatement = Yubico.YubiKit.Fido2.Credentials.AttestationStatement;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <summary>
/// WebAuthn Client for high-level credential registration and authentication.
/// </summary>
/// <remarks>
/// This client wraps CTAP2 operations and handles WebAuthn protocol details like
/// clientDataJSON construction, RP ID validation, UV/PIN token acquisition, and retry logic.
/// </remarks>
public sealed class WebAuthnClient : IAsyncDisposable
{
    private readonly IWebAuthnBackend _backend;
    private readonly WebAuthnOrigin _origin;
    private readonly Func<string, bool> _isPublicSuffix;
    private readonly IReadOnlySet<string> _enterpriseRpIds;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="WebAuthnClient"/>.
    /// </summary>
    /// <param name="backend">The backend that performs CTAP2 operations (ownership transferred).</param>
    /// <param name="origin">The WebAuthn origin for this client.</param>
    /// <param name="isPublicSuffix">Predicate to determine if a domain is a public suffix.</param>
    /// <param name="enterpriseRpIds">Optional set of enterprise-allowed RP IDs.</param>
    public WebAuthnClient(
        IWebAuthnBackend backend,
        WebAuthnOrigin origin,
        Func<string, bool> isPublicSuffix,
        IReadOnlySet<string>? enterpriseRpIds = null)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _origin = origin ?? throw new ArgumentNullException(nameof(origin));
        _isPublicSuffix = isPublicSuffix ?? throw new ArgumentNullException(nameof(isPublicSuffix));
        _enterpriseRpIds = enterpriseRpIds ?? new HashSet<string>();
    }

    /// <summary>
    /// Creates a new WebAuthn credential via CTAP2 MakeCredential.
    /// </summary>
    /// <param name="options">The registration options.</param>
    /// <param name="pinBytes">Optional PIN bytes (UTF-8 encoded). Caller owns and zeroes this memory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registration response with credential details.</returns>
    /// <exception cref="WebAuthnClientError">Thrown on validation or operation failure.</exception>
    /// <remarks>
    /// This overload is a convenience wrapper that drains the underlying stream and auto-responds
    /// to PIN requests if pinBytes is provided. For manual control over PIN/UV interaction,
    /// use <see cref="MakeCredentialStreamAsync"/>.
    /// </remarks>
    public async Task<RegistrationResponse> MakeCredentialAsync(
        RegistrationOptions options,
        ReadOnlyMemory<byte>? pinBytes,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(options);

        await foreach (var status in MakeCredentialStreamAsync(options, cancellationToken).ConfigureAwait(false))
        {
            switch (status)
            {
                case WebAuthnStatusRequestingPin requestingPin:
                    if (pinBytes is null)
                    {
                        await requestingPin.Cancel().ConfigureAwait(false);
                        throw new WebAuthnClientError(
                            WebAuthnClientErrorCode.NotAllowed,
                            "PIN required but not provided");
                    }

                    await requestingPin.SubmitPin(pinBytes.Value).ConfigureAwait(false);
                    break;

                case WebAuthnStatusRequestingUv requestingUv:
                    // Auto-respond with false (no UV) when not explicitly opted-in
                    await requestingUv.SetUseUv(false).ConfigureAwait(false);
                    break;

                case WebAuthnStatusFinished<RegistrationResponse> finished:
                    return finished.Result;

                case WebAuthnStatusFailed failed:
                    throw failed.Error;
            }
        }

        throw new WebAuthnClientError(
            WebAuthnClientErrorCode.Unknown,
            "Stream completed without terminal state");
    }

    /// <summary>
    /// Authenticates using an existing credential (GetAssertion).
    /// </summary>
    /// <param name="options">The authentication options.</param>
    /// <param name="pinBytes">Optional PIN bytes (UTF-8 encoded). Caller owns and zeroes this memory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of matched credentials. Each credential exposes <see cref="MatchedCredential.SelectAsync"/>
    /// to complete the authentication and retrieve the assertion response.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload is a convenience wrapper that drains the underlying stream and auto-responds
    /// to PIN requests if pinBytes is provided. For manual control over PIN/UV interaction,
    /// use <see cref="GetAssertionStreamAsync"/>.
    /// </para>
    /// <para>
    /// This method follows the deferred-selection pattern: the authenticator enumerates
    /// all matching credentials, and the caller can present a credential picker UI before
    /// calling <see cref="MatchedCredential.SelectAsync"/> to retrieve the assertion.
    /// </para>
    /// <para>
    /// If the allow list is empty, discoverable credentials for the RP ID are returned.
    /// If no credentials match, an empty list is returned (not an exception).
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<MatchedCredential>> GetAssertionAsync(
        AuthenticationOptions options,
        ReadOnlyMemory<byte>? pinBytes,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(options);

        await foreach (var status in GetAssertionStreamAsync(options, cancellationToken).ConfigureAwait(false))
        {
            switch (status)
            {
                case WebAuthnStatusRequestingPin requestingPin:
                    if (pinBytes is null)
                    {
                        await requestingPin.Cancel().ConfigureAwait(false);
                        throw new WebAuthnClientError(
                            WebAuthnClientErrorCode.NotAllowed,
                            "PIN required but not provided");
                    }

                    await requestingPin.SubmitPin(pinBytes.Value).ConfigureAwait(false);
                    break;

                case WebAuthnStatusRequestingUv requestingUv:
                    // Auto-respond with false (no UV) when not explicitly opted-in
                    await requestingUv.SetUseUv(false).ConfigureAwait(false);
                    break;

                case WebAuthnStatusFinished<IReadOnlyList<MatchedCredential>> finished:
                    return finished.Result;

                case WebAuthnStatusFailed failed:
                    throw failed.Error;
            }
        }

        throw new WebAuthnClientError(
            WebAuthnClientErrorCode.Unknown,
            "Stream completed without terminal state");
    }

    /// <summary>
    /// Creates a new WebAuthn credential via CTAP2 MakeCredential with status streaming.
    /// </summary>
    /// <param name="options">The registration options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An async enumerable of status updates. Terminal states are <see cref="WebAuthnStatusFinished{T}"/>
    /// and <see cref="WebAuthnStatusFailed"/>. Interactive states like <see cref="WebAuthnStatusRequestingPin"/>
    /// require consumer response to proceed.
    /// </returns>
    /// <remarks>
    /// This is the underlying primitive for all MakeCredential operations. When PIN/UV is needed,
    /// the stream emits <see cref="WebAuthnStatusRequestingPin"/> or <see cref="WebAuthnStatusRequestingUv"/>.
    /// Consumers must call the provided callbacks to supply the required information.
    /// </remarks>
    public async IAsyncEnumerable<WebAuthnStatus> MakeCredentialStreamAsync(
        RegistrationOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(options);

        // Create linked CTS to cancel producer when iterator is disposed
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var producerCt = linked.Token;

        var channel = new StatusChannel<RegistrationResponse>();

        // Start producer in background
        var producerTask = Task.Run(async () =>
        {
            try
            {
                await channel.WriteAsync(new WebAuthnStatusProcessing(), producerCt).ConfigureAwait(false);

                // Delegate to core implementation
                var result = await MakeCredentialCoreAsync(options, channel, producerCt).ConfigureAwait(false);

                // Emit terminal success
                await channel.WriteAsync(new WebAuthnStatusFinished<RegistrationResponse>(result), producerCt)
                    .ConfigureAwait(false);
            }
            catch (WebAuthnClientError error)
            {
                await channel.WriteAsync(new WebAuthnStatusFailed(error), CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var wrappedError = new WebAuthnClientError(WebAuthnClientErrorCode.Unknown, "Unexpected error", ex);
                await channel.WriteAsync(new WebAuthnStatusFailed(wrappedError), CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                channel.Complete();
            }
        }, producerCt);

        try
        {
            // Yield statuses as they arrive
            await foreach (var status in channel.Reader(cancellationToken).ConfigureAwait(false))
            {
                yield return status;
            }
        }
        finally
        {
            // Cancel producer if consumer broke early (iterator disposal)
            linked.Cancel();
            try
            {
                await producerTask.ConfigureAwait(false);
            }
            catch
            {
                // Exceptions already observed via Failed status
            }
        }
    }

    /// <summary>
    /// Authenticates using an existing credential (GetAssertion) with status streaming.
    /// </summary>
    /// <param name="options">The authentication options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An async enumerable of status updates. Terminal states are <see cref="WebAuthnStatusFinished{T}"/>
    /// and <see cref="WebAuthnStatusFailed"/>. Interactive states like <see cref="WebAuthnStatusRequestingPin"/>
    /// require consumer response to proceed.
    /// </returns>
    /// <remarks>
    /// This is the underlying primitive for all GetAssertion operations. The terminal result is a list
    /// of <see cref="MatchedCredential"/> instances, each exposing <see cref="MatchedCredential.SelectAsync"/>
    /// for deferred authentication.
    /// </remarks>
    public async IAsyncEnumerable<WebAuthnStatus> GetAssertionStreamAsync(
        AuthenticationOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(options);

        // Create linked CTS to cancel producer when iterator is disposed
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var producerCt = linked.Token;

        var channel = new StatusChannel<IReadOnlyList<MatchedCredential>>();

        // Start producer in background
        var producerTask = Task.Run(async () =>
        {
            try
            {
                await channel.WriteAsync(new WebAuthnStatusProcessing(), producerCt).ConfigureAwait(false);

                // Delegate to core implementation
                var result = await GetAssertionCoreAsync(options, channel, producerCt).ConfigureAwait(false);

                // Emit terminal success
                await channel.WriteAsync(new WebAuthnStatusFinished<IReadOnlyList<MatchedCredential>>(result), producerCt)
                    .ConfigureAwait(false);
            }
            catch (WebAuthnClientError error)
            {
                await channel.WriteAsync(new WebAuthnStatusFailed(error), CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var wrappedError = new WebAuthnClientError(WebAuthnClientErrorCode.Unknown, "Unexpected error", ex);
                await channel.WriteAsync(new WebAuthnStatusFailed(wrappedError), CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                channel.Complete();
            }
        }, producerCt);

        try
        {
            // Yield statuses as they arrive
            await foreach (var status in channel.Reader(cancellationToken).ConfigureAwait(false))
            {
                yield return status;
            }
        }
        finally
        {
            // Cancel producer if consumer broke early (iterator disposal)
            linked.Cancel();
            try
            {
                await producerTask.ConfigureAwait(false);
            }
            catch
            {
                // Exceptions already observed via Failed status
            }
        }
    }

    /// <summary>
    /// Creates a new WebAuthn credential with automatic PIN/UV handling.
    /// </summary>
    /// <param name="options">The registration options.</param>
    /// <param name="pin">Optional PIN string. If null and PIN is required, throws <see cref="WebAuthnClientError"/>.</param>
    /// <param name="useUv">Whether to use user verification when requested.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registration response with credential details.</returns>
    /// <exception cref="WebAuthnClientError">Thrown on validation or operation failure.</exception>
    /// <remarks>
    /// This is a convenience wrapper over <see cref="MakeCredentialStreamAsync"/> that automatically responds
    /// to PIN and UV requests. The PIN string is converted to UTF-8 bytes and zeroed immediately after use.
    /// </remarks>
    public async Task<RegistrationResponse> MakeCredentialAsync(
        RegistrationOptions options,
        string? pin,
        bool useUv,
        CancellationToken cancellationToken = default)
    {
        IMemoryOwner<byte>? pinOwner = null;
        int pinByteCount = 0;

        try
        {
            // Pre-encode PIN if provided
            if (pin is not null)
            {
                pinByteCount = Encoding.UTF8.GetByteCount(pin);
                pinOwner = MemoryPool<byte>.Shared.Rent(pinByteCount);
                Encoding.UTF8.GetBytes(pin, pinOwner.Memory.Span);
            }

            await foreach (var status in MakeCredentialStreamAsync(options, cancellationToken).ConfigureAwait(false))
            {
                switch (status)
                {
                    case WebAuthnStatusRequestingPin requestingPin:
                        if (pinOwner is null)
                        {
                            // Cancel and continue iteration to drain Failed status
                            await requestingPin.Cancel().ConfigureAwait(false);
                            break; // Continue iteration - producer will emit Failed
                        }

                        await requestingPin.SubmitPin(pinOwner.Memory[..pinByteCount]).ConfigureAwait(false);
                        break;

                    case WebAuthnStatusRequestingUv requestingUv:
                        await requestingUv.SetUseUv(useUv).ConfigureAwait(false);
                        break;

                    case WebAuthnStatusFinished<RegistrationResponse> finished:
                        return finished.Result;

                    case WebAuthnStatusFailed failed:
                        throw failed.Error;
                }
            }

            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.Unknown,
                "Stream completed without terminal state");
        }
        finally
        {
            if (pinOwner is not null)
            {
                // Zero entire rented buffer for defense-in-depth even though only [..pinByteCount] was written.
                CryptographicOperations.ZeroMemory(pinOwner.Memory.Span);
                pinOwner.Dispose();
            }
        }
    }

    /// <summary>
    /// Authenticates using an existing credential with automatic PIN/UV handling.
    /// </summary>
    /// <param name="options">The authentication options.</param>
    /// <param name="pin">Optional PIN string. If null and PIN is required, throws <see cref="WebAuthnClientError"/>.</param>
    /// <param name="useUv">Whether to use user verification when requested.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of matched credentials.</returns>
    /// <exception cref="WebAuthnClientError">Thrown on validation or operation failure.</exception>
    /// <remarks>
    /// This is a convenience wrapper over <see cref="GetAssertionStreamAsync"/> that automatically responds
    /// to PIN and UV requests. The PIN string is converted to UTF-8 bytes and zeroed immediately after use.
    /// </remarks>
    public async Task<IReadOnlyList<MatchedCredential>> GetAssertionAsync(
        AuthenticationOptions options,
        string? pin,
        bool useUv,
        CancellationToken cancellationToken = default)
    {
        IMemoryOwner<byte>? pinOwner = null;
        int pinByteCount = 0;

        try
        {
            // Pre-encode PIN if provided
            if (pin is not null)
            {
                pinByteCount = Encoding.UTF8.GetByteCount(pin);
                pinOwner = MemoryPool<byte>.Shared.Rent(pinByteCount);
                Encoding.UTF8.GetBytes(pin, pinOwner.Memory.Span);
            }

            await foreach (var status in GetAssertionStreamAsync(options, cancellationToken).ConfigureAwait(false))
            {
                switch (status)
                {
                    case WebAuthnStatusRequestingPin requestingPin:
                        if (pinOwner is null)
                        {
                            // Cancel and continue iteration to drain Failed status
                            await requestingPin.Cancel().ConfigureAwait(false);
                            break; // Continue iteration - producer will emit Failed
                        }

                        await requestingPin.SubmitPin(pinOwner.Memory[..pinByteCount]).ConfigureAwait(false);
                        break;

                    case WebAuthnStatusRequestingUv requestingUv:
                        await requestingUv.SetUseUv(useUv).ConfigureAwait(false);
                        break;

                    case WebAuthnStatusFinished<IReadOnlyList<MatchedCredential>> finished:
                        return finished.Result;

                    case WebAuthnStatusFailed failed:
                        throw failed.Error;
                }
            }

            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.Unknown,
                "Stream completed without terminal state");
        }
        finally
        {
            if (pinOwner is not null)
            {
                // Zero entire rented buffer for defense-in-depth even though only [..pinByteCount] was written.
                CryptographicOperations.ZeroMemory(pinOwner.Memory.Span);
                pinOwner.Dispose();
            }
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _backend.DisposeAsync().ConfigureAwait(false);
            _disposed = true;
        }
    }

    /// <summary>
    /// Core MakeCredential implementation shared by all overloads.
    /// </summary>
    /// <remarks>
    /// This method handles validation, UV/PIN decision, token acquisition, and CTAP execution.
    /// It may write status updates to the channel (e.g., WaitingForUser) and awaits interactive
    /// responses when PIN/UV is needed.
    /// </remarks>
    private async Task<RegistrationResponse> MakeCredentialCoreAsync(
        RegistrationOptions options,
        StatusChannel<RegistrationResponse>? channel,
        CancellationToken cancellationToken)
    {
        // Validate options
        ValidateRegistrationOptions(options);

        // Validate RP ID against origin
        RpIdValidator.EnsureValid(options.Rp.Id, _origin, _enterpriseRpIds, _isPublicSuffix);

        // Build client data
        var clientData = WebAuthnClientData.Create(
            type: "webauthn.create",
            challenge: options.Challenge,
            origin: _origin,
            crossOrigin: options.CrossOrigin,
            topOrigin: options.TopOrigin);

        // Get authenticator info
        var info = await _backend.GetCachedInfoAsync(cancellationToken).ConfigureAwait(false);

        // Determine UV/PIN strategy
        var uvDecision = UvDecisionLogic.Decide(
            info,
            options.UserVerification,
            pinAvailable: channel is not null, // Stream mode can request PIN interactively
            requestedPermissions: PinUvAuthTokenPermissions.MakeCredential | PinUvAuthTokenPermissions.GetAssertion);

        // Acquire PIN/UV token with retry on PinAuthInvalid
        PinUvAuthTokenSession? tokenSession = null;
        IMemoryOwner<byte>? pinOwner = null;

        try
        {
            // Handle token acquisition if needed
            if (uvDecision.UseToken)
            {
                // Request PIN from consumer if needed
                ReadOnlyMemory<byte>? pinBytes = null;
                if (uvDecision.Method == PinUvAuthMethod.Pin && channel is not null)
                {
                    var (pinStatus, pinResponseTask) = channel.CreatePinRequest();
                    await channel.WriteAsync(pinStatus, cancellationToken).ConfigureAwait(false);

                    var response = await pinResponseTask.ConfigureAwait(false);
                    if (response is null)
                    {
                        throw new WebAuthnClientError(
                            WebAuthnClientErrorCode.NotAllowed,
                            "PIN required but cancelled");
                    }

                    // Copy to secure buffer
                    pinOwner = MemoryPool<byte>.Shared.Rent(response.Value.Length);
                    response.Value.Span.CopyTo(pinOwner.Memory.Span);
                    pinBytes = pinOwner.Memory[..response.Value.Length];
                }

                tokenSession = await AcquirePinUvTokenWithRetryAsync(
                    uvDecision.Method!.Value,
                    uvDecision.Permissions,
                    options.Rp.Id,
                    pinBytes,
                    cancellationToken).ConfigureAwait(false);
            }

            // Build backend request
            var request = BuildMakeCredentialRequest(options, clientData, tokenSession, uvDecision);

            // Execute MakeCredential
            MakeCredentialResponse ctapResponse;
            try
            {
                ctapResponse = await _backend.MakeCredentialAsync(request, progress: null, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (CtapException ex) when (options.Extensions?.PreviewSign is not null)
            {
                throw Extensions.PreviewSign.PreviewSignErrors.MapCtapError(ex);
            }

            // Build WebAuthn response
            return BuildRegistrationResponse(ctapResponse, clientData, options);
        }
        finally
        {
            tokenSession?.Dispose();

            if (pinOwner is not null)
            {
                CryptographicOperations.ZeroMemory(pinOwner.Memory.Span);
                pinOwner.Dispose();
            }
        }
    }

    /// <summary>
    /// Core GetAssertion implementation shared by all overloads.
    /// </summary>
    /// <remarks>
    /// This method handles validation, UV/PIN decision, token acquisition, credential matching, and CTAP execution.
    /// It may write status updates to the channel (e.g., WaitingForUser) and awaits interactive
    /// responses when PIN/UV is needed.
    /// </remarks>
    private async Task<IReadOnlyList<MatchedCredential>> GetAssertionCoreAsync(
        AuthenticationOptions options,
        StatusChannel<IReadOnlyList<MatchedCredential>>? channel,
        CancellationToken cancellationToken)
    {
        // TODO: Wire PreviewSignErrors.MapCtapError when GetAssertion backend integration is complete
        // (Phase 9 - authentication ceremonies not yet fully implemented)

        // Validate options
        ValidateAuthenticationOptions(options);

        // Validate RP ID against origin
        RpIdValidator.EnsureValid(options.RpId, _origin, _enterpriseRpIds, _isPublicSuffix);

        // Build client data
        var clientData = WebAuthnClientData.Create(
            type: "webauthn.get",
            challenge: options.Challenge,
            origin: _origin,
            crossOrigin: options.CrossOrigin,
            topOrigin: options.TopOrigin);

        // Get authenticator info
        var info = await _backend.GetCachedInfoAsync(cancellationToken).ConfigureAwait(false);

        // Determine UV/PIN strategy
        var uvDecision = UvDecisionLogic.Decide(
            info,
            options.UserVerification,
            pinAvailable: channel is not null, // Stream mode can request PIN interactively
            requestedPermissions: PinUvAuthTokenPermissions.GetAssertion);

        // Acquire PIN/UV token with retry on PinAuthInvalid
        PinUvAuthTokenSession? tokenSession = null;
        IMemoryOwner<byte>? pinOwner = null;

        try
        {
            // Handle token acquisition if needed
            if (uvDecision.UseToken)
            {
                // Request PIN from consumer if needed
                ReadOnlyMemory<byte>? pinBytes = null;
                if (uvDecision.Method == PinUvAuthMethod.Pin && channel is not null)
                {
                    var (pinStatus, pinResponseTask) = channel.CreatePinRequest();
                    await channel.WriteAsync(pinStatus, cancellationToken).ConfigureAwait(false);

                    var response = await pinResponseTask.ConfigureAwait(false);
                    if (response is null)
                    {
                        throw new WebAuthnClientError(
                            WebAuthnClientErrorCode.NotAllowed,
                            "PIN required but cancelled");
                    }

                    // Copy to secure buffer
                    pinOwner = MemoryPool<byte>.Shared.Rent(response.Value.Length);
                    response.Value.Span.CopyTo(pinOwner.Memory.Span);
                    pinBytes = pinOwner.Memory[..response.Value.Length];
                }

                tokenSession = await AcquirePinUvTokenWithRetryAsync(
                    uvDecision.Method!.Value,
                    uvDecision.Permissions,
                    options.RpId,
                    pinBytes,
                    cancellationToken).ConfigureAwait(false);
            }

            // Build backend request
            var request = BuildGetAssertionRequest(options, clientData, tokenSession, uvDecision);

            // Match credentials (handles allow-list probing and discoverable enumeration)
            var matches = await CredentialMatcher.MatchAsync(_backend, request, cancellationToken)
                .ConfigureAwait(false);

            // Wrap each match into a MatchedCredential with deferred SelectAsync
            var results = new List<MatchedCredential>();
            bool requiresSelection = matches.Count > 1;

            foreach (var (credId, user, response) in matches)
            {
                var matchedCred = new MatchedCredential(
                    id: credId,
                    user: user,
                    requiresSelection: requiresSelection,
                    responseFactory: _ => Task.FromResult(BuildAuthenticationResponse(response, clientData, options)));

                results.Add(matchedCred);
            }

            return results;
        }
        finally
        {
            tokenSession?.Dispose();

            if (pinOwner is not null)
            {
                CryptographicOperations.ZeroMemory(pinOwner.Memory.Span);
                pinOwner.Dispose();
            }
        }
    }

    private static void ValidateRegistrationOptions(RegistrationOptions options)
    {
        if (options.Challenge.Length == 0)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "Challenge cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(options.Rp.Id))
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "RP ID cannot be null or empty");
        }

        if (options.User.Id.Length is < 1 or > 64)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                $"User ID length must be 1-64 bytes, got {options.User.Id.Length}");
        }

        if (options.PubKeyCredParams.Count == 0)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "At least one public key credential parameter is required");
        }
    }

    private static void ValidateAuthenticationOptions(AuthenticationOptions options)
    {
        if (options.Challenge.Length == 0)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "Challenge cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(options.RpId))
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "RP ID cannot be null or empty");
        }
    }

    /// <summary>
    /// Acquires a PIN/UV auth token from the backend.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On PinAuthInvalid, this method throws immediately without retrying.
    /// The original Swift retry was for transient encryption-state mismatches
    /// that no longer apply in our current PinUvAuthProtocolV2 implementation.
    /// Retrying with identical PIN bytes would burn YubiKey PIN attempts on wrong-PIN scenarios.
    /// </para>
    /// <para>
    /// Callers experiencing PinAuthInvalid should re-invoke MakeCredentialAsync/GetAssertionAsync
    /// with fresh PIN bytes (after re-prompting the user).
    /// </para>
    /// </remarks>
    private async Task<PinUvAuthTokenSession> AcquirePinUvTokenWithRetryAsync(
        PinUvAuthMethod method,
        PinUvAuthTokenPermissions permissions,
        string rpId,
        ReadOnlyMemory<byte>? pinBytes,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _backend.GetPinUvTokenAsync(
                method,
                permissions,
                rpId,
                pinBytes,
                progress: null,
                cancellationToken).ConfigureAwait(false);

            return session;
        }
        catch (CtapException ex) when (ex.Status == CtapStatus.PinAuthInvalid)
        {
            // Throw immediately - do NOT retry with the same PIN bytes.
            // Retrying would burn PIN attempts on the hardware.
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.NotAllowed,
                "PIN authentication failed",
                ex);
        }
    }

    private BackendMakeCredentialRequest BuildMakeCredentialRequest(
        RegistrationOptions options,
        WebAuthnClientData clientData,
        PinUvAuthTokenSession? tokenSession,
        UvDecision uvDecision)
    {
        // Map options to backend request
        var optionsDict = new Dictionary<string, bool>();

        if (options.ResidentKey == Preferences.ResidentKeyPreference.Required)
        {
            optionsDict["rk"] = true;
        }

        if (uvDecision.UvOption is not null)
        {
            optionsDict["uv"] = uvDecision.UvOption.Value;
        }

        // Compute PIN/UV auth parameter if we have a token
        ReadOnlyMemory<byte>? pinUvAuthParam = null;
        byte? pinUvAuthProtocol = null;

        if (tokenSession is not null)
        {
            var authParam = tokenSession.Protocol.Authenticate(tokenSession.Token, clientData.Hash.Span);
            pinUvAuthParam = authParam;
            pinUvAuthProtocol = (byte)tokenSession.Protocol.Version;
        }

        // Build extensions CBOR via pipeline
        var extensionsCbor = ExtensionPipeline.BuildRegistrationExtensionsCbor(options.Extensions, options);

        return new BackendMakeCredentialRequest
        {
            ClientDataHash = clientData.Hash,
            Rp = options.Rp,
            User = options.User,
            PubKeyCredParams = options.PubKeyCredParams
                .Select(alg => new PublicKeyCredentialParameters { Algorithm = (CoseAlgorithmIdentifier)alg.Value })
                .ToList(),
            ExcludeList = options.ExcludeCredentials,
            Extensions = extensionsCbor,
            Options = optionsDict.Count > 0 ? optionsDict : null,
            PinUvAuthParam = pinUvAuthParam,
            PinUvAuthProtocol = pinUvAuthProtocol
        };
    }

    private RegistrationResponse BuildRegistrationResponse(
        MakeCredentialResponse ctapResponse,
        WebAuthnClientData clientData,
        RegistrationOptions options)
    {
        // Extract attested credential data from AuthenticatorData
        var attestedCred = ctapResponse.AuthenticatorData.AttestedCredentialData!;

        // Decode public key from COSE
        var publicKey = CoseKey.Decode(attestedCred.CredentialPublicKey);

        // Use the typed attestation statement from CTAP response (already decoded)
        var webAuthnStatement = ctapResponse.AttestationStatement;

        // Wrap authenticator data
        var webAuthnAuthData = WebAuthnAuthenticatorData.Decode(ctapResponse.AuthenticatorDataRaw);

        // Create attestation object from decoded components
        var attestationObject = WebAuthnAttestationObject.Create(webAuthnAuthData, webAuthnStatement);

        // Parse extension outputs via pipeline
        var extensionOutputs = ExtensionPipeline.ParseRegistrationOutputs(
            options.Extensions,
            webAuthnAuthData,
            ctapResponse.UnsignedExtensionOutputs,
            options);

        return new RegistrationResponse
        {
            CredentialId = attestedCred.CredentialId,
            AttestationObject = attestationObject,
            RawAttestationObject = attestationObject.RawCbor,
            AuthenticatorData = webAuthnAuthData,
            RawAuthenticatorData = ctapResponse.AuthenticatorDataRaw,
            AttestationStatement = webAuthnStatement,
            Transports = null, // TODO Phase 6+
            PublicKey = publicKey,
            Aaguid = new Aaguid(attestedCred.Aaguid),
            SignCount = ctapResponse.AuthenticatorData.SignCount,
            ClientData = clientData,
            ClientExtensionResults = extensionOutputs
        };
    }

    private BackendGetAssertionRequest BuildGetAssertionRequest(
        AuthenticationOptions options,
        WebAuthnClientData clientData,
        PinUvAuthTokenSession? tokenSession,
        UvDecision uvDecision)
    {
        // Map options to backend request
        var optionsDict = new Dictionary<string, bool>();

        if (uvDecision.UvOption.HasValue)
        {
            optionsDict["uv"] = uvDecision.UvOption.Value;
        }

        // Map allow credentials to backend descriptors
        IReadOnlyList<PublicKeyCredentialDescriptor>? allowList = null;
        if (options.AllowCredentials is not null && options.AllowCredentials.Count > 0)
        {
            allowList = options.AllowCredentials
                .Select(desc => new PublicKeyCredentialDescriptor(
                    desc.Id,
                    desc.Type,
                    desc.Transports))
                .ToList();
        }

        // Build PIN/UV auth params
        ReadOnlyMemory<byte>? pinUvAuthParam = null;
        byte? pinUvAuthProtocol = null;

        if (tokenSession is not null)
        {
            // Compute pinUvAuthParam = HMAC(token, clientDataHash)
            pinUvAuthParam = tokenSession.Protocol.Authenticate(tokenSession.Token, clientData.Hash.Span);
            pinUvAuthProtocol = (byte)tokenSession.Protocol.Version;
        }

        // Build extensions CBOR via pipeline
        var extensionsCbor = ExtensionPipeline.BuildAuthenticationExtensionsCbor(
            options.Extensions,
            options.AllowCredentials);

        return new BackendGetAssertionRequest
        {
            ClientDataHash = clientData.Hash,
            RpId = options.RpId,
            AllowList = allowList,
            Extensions = extensionsCbor,
            Options = optionsDict.Count > 0 ? optionsDict : null,
            PinUvAuthParam = pinUvAuthParam,
            PinUvAuthProtocol = pinUvAuthProtocol
        };
    }

    private static AuthenticationResponse BuildAuthenticationResponse(
        GetAssertionResponse ctapResponse,
        WebAuthnClientData clientData,
        AuthenticationOptions options)
    {
        // Wrap authenticator data
        var webAuthnAuthData = WebAuthnAuthenticatorData.Decode(ctapResponse.AuthenticatorDataRaw);

        // Extract credential ID from the response or use empty if not present
        var credentialId = ctapResponse.Credential?.Id ?? ReadOnlyMemory<byte>.Empty;

        // User from CTAP response can be used directly
        var user = ctapResponse.User;

        // Parse extension outputs via pipeline
        var extensionOutputs = ExtensionPipeline.ParseAuthenticationOutputs(
            options.Extensions,
            webAuthnAuthData);

        return new AuthenticationResponse
        {
            CredentialId = credentialId,
            AuthenticatorData = webAuthnAuthData,
            RawAuthenticatorData = ctapResponse.AuthenticatorDataRaw,
            Signature = ctapResponse.Signature,
            User = user,
            SignCount = ctapResponse.AuthenticatorData.SignCount,
            ClientData = clientData,
            ClientExtensionResults = extensionOutputs
        };
    }

}