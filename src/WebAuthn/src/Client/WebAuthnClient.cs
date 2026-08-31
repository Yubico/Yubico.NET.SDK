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
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Credentials;
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
    /// <summary>
    /// The maximum number of times the client asks an <see cref="ICredentialPrompt"/>
    /// for a PIN during a single operation.
    /// </summary>
    /// <remarks>
    /// The authenticator's own retry counter is the security boundary; this cap is a
    /// blast-radius bound so that a prompt implementation which repeatedly returns the
    /// same wrong secret cannot consume every hardware attempt and block the credential.
    /// Reaching the cap fails the operation, which the user can simply retry.
    /// </remarks>
    public const int MaxPromptAttempts = 3;

    /// <summary>CTAP 2.1 minimum PIN length in bytes, used when the authenticator reports none.</summary>
    private const int Ctap2MinPinLengthBytes = 4;

    /// <summary>CTAP 2.1 maximum PIN length in bytes.</summary>
    private const int Ctap2MaxPinLengthBytes = 63;

    private readonly IWebAuthnBackend _backend;
    private readonly WebAuthnOrigin _origin;
    private readonly Func<string, bool> _isPublicSuffix;
    private readonly IReadOnlySet<string> _enterpriseRpIds;
    private readonly ICredentialPrompt? _prompt;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="WebAuthnClient"/>.
    /// </summary>
    /// <param name="fidoSession">The FIDO2 session that performs CTAP2 operations (ownership transferred).</param>
    /// <param name="origin">The WebAuthn origin for this client.</param>
    /// <param name="isPublicSuffix">Checker used to reject public-suffix RP IDs.</param>
    /// <param name="enterpriseRpIds">Optional set of enterprise-allowed RP IDs.</param>
    /// <param name="prompt">
    /// Optional prompt used to obtain a PIN when an operation needs one and the caller
    /// did not supply it. When omitted, such operations fail with
    /// <see cref="WebAuthnClientErrorCode.NotAllowed"/> rather than prompting.
    /// </param>
    public WebAuthnClient(
        IFidoSession fidoSession,
        WebAuthnOrigin origin,
        PublicSuffixChecker isPublicSuffix,
        IReadOnlySet<string>? enterpriseRpIds = null,
        ICredentialPrompt? prompt = null)
    {
        ArgumentNullException.ThrowIfNull(fidoSession);
        _origin = origin ?? throw new ArgumentNullException(nameof(origin));
        ArgumentNullException.ThrowIfNull(isPublicSuffix);
        _backend = new FidoSessionWebAuthnBackend(fidoSession);
        _isPublicSuffix = domain => isPublicSuffix(domain);
        _enterpriseRpIds = enterpriseRpIds ?? new HashSet<string>();
        _prompt = prompt;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="WebAuthnClient"/>.
    /// </summary>
    /// <param name="backend">The backend that performs CTAP2 operations (ownership transferred).</param>
    /// <param name="origin">The WebAuthn origin for this client.</param>
    /// <param name="isPublicSuffix">Predicate to determine if a domain is a public suffix.</param>
    /// <param name="enterpriseRpIds">Optional set of enterprise-allowed RP IDs.</param>
    /// <param name="prompt">
    /// Optional prompt used to obtain a PIN when an operation needs one and the caller
    /// did not supply it. When omitted, such operations fail with
    /// <see cref="WebAuthnClientErrorCode.NotAllowed"/> rather than prompting.
    /// </param>
    public WebAuthnClient(
        IWebAuthnBackend backend,
        WebAuthnOrigin origin,
        Func<string, bool> isPublicSuffix,
        IReadOnlySet<string>? enterpriseRpIds = null,
        ICredentialPrompt? prompt = null)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _origin = origin ?? throw new ArgumentNullException(nameof(origin));
        _isPublicSuffix = isPublicSuffix ?? throw new ArgumentNullException(nameof(isPublicSuffix));
        _enterpriseRpIds = enterpriseRpIds ?? new HashSet<string>();
        _prompt = prompt;
    }

    /// <summary>
    /// Creates a new WebAuthn credential via CTAP2 MakeCredential.
    /// </summary>
    /// <param name="options">The registration options.</param>
    /// <param name="pinBytes">
    /// Optional PIN bytes (UTF-8 encoded). The caller owns and zeroes this memory. When omitted
    /// and the operation needs a PIN, the client asks the configured
    /// <see cref="ICredentialPrompt"/>; if none is configured the operation fails with
    /// <see cref="WebAuthnClientErrorCode.NotAllowed"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registration response with credential details.</returns>
    /// <exception cref="WebAuthnClientError">Thrown on validation or operation failure.</exception>
    /// <remarks>
    /// To observe ceremony progress (for example, to show a "touch your key" prompt), use
    /// <see cref="MakeCredentialStreamAsync"/> instead.
    /// </remarks>
    public async Task<RegistrationResponse> MakeCredentialAsync(
        RegistrationOptions options,
        ReadOnlyMemory<byte>? pinBytes = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(options);

        return await MakeCredentialCoreAsync(options, pinBytes, channel: null, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
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
    /// To observe ceremony progress (for example, to show a "touch your key" prompt), use
    /// <see cref="GetAssertionStreamAsync"/> instead.
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
        ReadOnlyMemory<byte>? pinBytes = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(options);

        return await GetAssertionCoreAsync(options, pinBytes, channel: null, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a new WebAuthn credential via CTAP2 MakeCredential with status streaming.
    /// </summary>
    /// <param name="options">The registration options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="pinBytes">
    /// Optional PIN bytes (UTF-8 encoded). The caller owns and zeroes this memory. When omitted
    /// and the operation needs a PIN, the client asks the configured
    /// <see cref="ICredentialPrompt"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of ceremony status updates. Terminal states are
    /// <see cref="WebAuthnStatusFinished{T}"/> and <see cref="WebAuthnStatusFailed"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The stream reports progress; it does not gather input. A PIN, when needed, comes from
    /// <paramref name="pinBytes"/> or the configured <see cref="ICredentialPrompt"/>.
    /// </para>
    /// <para>
    /// Enumerating the returned sequence starts a ceremony, so enumerating it a second time
    /// starts another one. Abandoning enumeration early cancels the ceremony in progress.
    /// </para>
    /// </remarks>
    public async IAsyncEnumerable<WebAuthnStatus> MakeCredentialStreamAsync(
        RegistrationOptions options,
        ReadOnlyMemory<byte>? pinBytes = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(options);

        await foreach (var status in RunStatusStreamAsync<RegistrationResponse>(
            (channel, producerCt) => MakeCredentialCoreAsync(options, pinBytes, channel, producerCt),
            cancellationToken).ConfigureAwait(false))
        {
            yield return status;
        }
    }

    /// <summary>
    /// Authenticates using an existing credential (GetAssertion) with status streaming.
    /// </summary>
    /// <param name="options">The authentication options.</param>
    /// <param name="pinBytes">
    /// Optional PIN bytes (UTF-8 encoded). The caller owns and zeroes this memory. When omitted
    /// and the operation needs a PIN, the client asks the configured
    /// <see cref="ICredentialPrompt"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An async enumerable of ceremony status updates. Terminal states are
    /// <see cref="WebAuthnStatusFinished{T}"/> and <see cref="WebAuthnStatusFailed"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The stream reports progress; it does not gather input. A PIN, when needed, comes from
    /// <paramref name="pinBytes"/> or the configured <see cref="ICredentialPrompt"/>. The terminal
    /// result is a list of <see cref="MatchedCredential"/> instances, each exposing
    /// <see cref="MatchedCredential.SelectAsync"/> for deferred authentication.
    /// </para>
    /// <para>
    /// Enumerating the returned sequence starts a ceremony, so enumerating it a second time
    /// starts another one. Abandoning enumeration early cancels the ceremony in progress.
    /// </para>
    /// </remarks>
    public async IAsyncEnumerable<WebAuthnStatus> GetAssertionStreamAsync(
        AuthenticationOptions options,
        ReadOnlyMemory<byte>? pinBytes = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(options);

        await foreach (var status in RunStatusStreamAsync<IReadOnlyList<MatchedCredential>>(
            (channel, producerCt) => GetAssertionCoreAsync(options, pinBytes, channel, producerCt),
            cancellationToken).ConfigureAwait(false))
        {
            yield return status;
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

    private async IAsyncEnumerable<WebAuthnStatus> RunStatusStreamAsync<TResult>(
        Func<StatusChannel<TResult>, CancellationToken, Task<TResult>> produceResultAsync,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var producerCt = linked.Token;
        var channel = new StatusChannel<TResult>();

        var producerTask = Task.Run(async () =>
        {
            try
            {
                await channel.WriteAsync(new WebAuthnStatusProcessing(), producerCt).ConfigureAwait(false);

                var result = await produceResultAsync(channel, producerCt).ConfigureAwait(false);

                await channel.WriteAsync(new WebAuthnStatusFinished<TResult>(result), producerCt)
                    .ConfigureAwait(false);
            }
            catch (WebAuthnClientError error)
            {
                await channel.WriteAsync(new WebAuthnStatusFailed(error), CancellationToken.None).ConfigureAwait(false);
            }
            catch (OperationCanceledException oce)
            {
                // Cancellation is semantically distinct from backend failure; consumers need the typed signal.
                var cancelledError = new WebAuthnClientError(
                    WebAuthnClientErrorCode.Cancelled, "Operation was cancelled", oce);
                await channel.WriteAsync(new WebAuthnStatusFailed(cancelledError), CancellationToken.None).ConfigureAwait(false);
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
            await foreach (var status in channel.Reader(cancellationToken).ConfigureAwait(false))
            {
                yield return status;
            }
        }
        finally
        {
            linked.Cancel();
            try
            {
                await producerTask.ConfigureAwait(false);
            }
            catch
            {
                // Exceptions are surfaced as Failed statuses by the producer.
            }
        }
    }

    private static void ZeroAndDispose(IMemoryOwner<byte>? owner)
    {
        if (owner is null)
        {
            return;
        }

        // Zero entire rented buffer for defense-in-depth even though only the PIN prefix was written.
        CryptographicOperations.ZeroMemory(owner.Memory.Span);
        owner.Dispose();
    }

    /// <summary>
    /// Core MakeCredential implementation shared by all overloads.
    /// </summary>
    /// <remarks>
    /// This method handles validation, UV/PIN decision, token acquisition, and CTAP execution.
    /// It may write status updates to the channel and awaits interactive
    /// responses when PIN/UV is needed.
    /// </remarks>
    private async Task<RegistrationResponse> MakeCredentialCoreAsync(
        RegistrationOptions options,
        ReadOnlyMemory<byte>? callerPinBytes,
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
            pinAvailable: callerPinBytes is not null || _prompt is not null,
            requestedPermissions: PinUvAuthTokenPermissions.MakeCredential | PinUvAuthTokenPermissions.GetAssertion);

        // Acquire PIN/UV token with retry on PinAuthInvalid
        PinUvAuthTokenSession? tokenSession = null;
        IMemoryOwner<byte>? pinOwner = null;
        ReadOnlyMemory<byte>? pinBytes = callerPinBytes;

        try
        {
            (tokenSession, pinOwner, pinBytes) = await AcquireTokenForDecisionAsync(
                uvDecision,
                options.Rp.Id,
                pinOwner,
                pinBytes,
                cancellationToken).ConfigureAwait(false);

            // Pre-flight excludeList when non-empty (mirroring yubikit-android Ctap2Client.filterCreds)
            PublicKeyCredentialDescriptor? matchedExclude = null;
            bool preflightPerformed;
            (matchedExclude, tokenSession, preflightPerformed) = await PreflightExcludeListAndRemintAsync(
                options,
                info,
                uvDecision,
                tokenSession,
                pinBytes,
                cancellationToken).ConfigureAwait(false);

            // Build backend request with filtered exclude list
            var request = BuildMakeCredentialRequest(options, clientData, tokenSession, uvDecision, matchedExclude, preflightPerformed);

            // Execute MakeCredential
            MakeCredentialResponse ctapResponse;
            try
            {
                ctapResponse = await ExecuteMakeCredentialAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (CtapException ex) when (ShouldRetryWithRequiredUv(ex, options.UserVerification))
            {
                tokenSession?.Dispose();
                tokenSession = null;

                uvDecision = UvDecisionLogic.Decide(
                    info,
                    Preferences.UserVerificationPreference.Required,
                    pinAvailable: callerPinBytes is not null || _prompt is not null,
                    requestedPermissions: PinUvAuthTokenPermissions.MakeCredential | PinUvAuthTokenPermissions.GetAssertion);

                (tokenSession, pinOwner, pinBytes) = await AcquireTokenForDecisionAsync(
                    uvDecision,
                    options.Rp.Id,
                    pinOwner,
                    pinBytes,
                    cancellationToken).ConfigureAwait(false);

                (matchedExclude, tokenSession, preflightPerformed) = await PreflightExcludeListAndRemintAsync(
                    options,
                    info,
                    uvDecision,
                    tokenSession,
                    pinBytes,
                    cancellationToken).ConfigureAwait(false);

                request = BuildMakeCredentialRequest(options, clientData, tokenSession, uvDecision, matchedExclude, preflightPerformed);
                try
                {
                    ctapResponse = await ExecuteMakeCredentialAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (CtapException retryEx)
                {
                    throw MapMakeCredentialCtapException(retryEx, options.Extensions?.PreviewSign is not null);
                }
            }
            catch (CtapException ex) when (ex.Status == CtapStatus.CredentialExcluded)
            {
                // WebAuthn L2 §5.1.3 step 3: when the authenticator returns
                // CredentialExcluded, the client surfaces an InvalidStateError.
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.InvalidState,
                    "A credential matching the exclude list already exists on this authenticator.",
                    ex);
            }
            catch (CtapException ex) when (options.Extensions?.PreviewSign is not null)
            {
                throw Extensions.PreviewSign.PreviewSignErrors.MapCtapError(ex);
            }
            catch (CtapException ex)
            {
                // Map remaining CTAP statuses to typed WebAuthn errors per CLAUDE.md:
                // "never expose raw CTAP status codes to high-level API consumers".
                throw MapCtapStatusToWebAuthnError(ex);
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
    /// It may write status updates to the channel and awaits interactive
    /// responses when PIN/UV is needed.
    /// </remarks>
    private async Task<IReadOnlyList<MatchedCredential>> GetAssertionCoreAsync(
        AuthenticationOptions options,
        ReadOnlyMemory<byte>? callerPinBytes,
        StatusChannel<IReadOnlyList<MatchedCredential>>? channel,
        CancellationToken cancellationToken)
    {
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
            pinAvailable: callerPinBytes is not null || _prompt is not null,
            requestedPermissions: PinUvAuthTokenPermissions.GetAssertion);

        // Acquire PIN/UV token with retry on PinAuthInvalid
        PinUvAuthTokenSession? tokenSession = null;
        IMemoryOwner<byte>? pinOwner = null;
        ReadOnlyMemory<byte>? pinBytes = callerPinBytes;

        try
        {
            (tokenSession, pinOwner, pinBytes) = await AcquireTokenForDecisionAsync(
                uvDecision,
                options.RpId,
                pinOwner,
                pinBytes,
                cancellationToken).ConfigureAwait(false);

            // Build backend request
            var request = BuildGetAssertionRequest(options, clientData, tokenSession, uvDecision);

            // Match credentials (handles allow-list probing and discoverable enumeration)
            IReadOnlyList<(ReadOnlyMemory<byte> Id, PublicKeyCredentialUserEntity? User, GetAssertionResponse Response)> matches;
            try
            {
                matches = await MatchCredentialsAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (CtapException ex) when (ShouldRetryWithRequiredUv(ex, options.UserVerification))
            {
                tokenSession?.Dispose();
                tokenSession = null;

                uvDecision = UvDecisionLogic.Decide(
                    info,
                    Preferences.UserVerificationPreference.Required,
                    pinAvailable: callerPinBytes is not null || _prompt is not null,
                    requestedPermissions: PinUvAuthTokenPermissions.GetAssertion);

                (tokenSession, pinOwner, pinBytes) = await AcquireTokenForDecisionAsync(
                    uvDecision,
                    options.RpId,
                    pinOwner,
                    pinBytes,
                    cancellationToken).ConfigureAwait(false);

                request = BuildGetAssertionRequest(options, clientData, tokenSession, uvDecision);
                try
                {
                    matches = await MatchCredentialsAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (CtapException retryEx)
                {
                    throw MapGetAssertionCtapException(retryEx, options.Extensions?.PreviewSign is not null);
                }
            }
            catch (CtapException ex) when (options.Extensions?.PreviewSign is not null)
            {
                throw Extensions.PreviewSign.PreviewSignErrors.MapCtapError(ex);
            }
            catch (CtapException ex)
            {
                // Map remaining CTAP statuses to typed WebAuthn errors per CLAUDE.md:
                // "never expose raw CTAP status codes to high-level API consumers".
                throw MapCtapStatusToWebAuthnError(ex);
            }

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
    /// Maps a raw <see cref="CtapException"/> to a typed <see cref="WebAuthnClientError"/> per
    /// the WebAuthn module rule that low-level CTAP status codes never escape the public API.
    /// CredentialExcluded and previewSign-specific statuses are handled by their own catch arms
    /// upstream and never reach this mapper.
    /// </summary>
    internal static WebAuthnClientError MapCtapStatusToWebAuthnError(CtapException ex) =>
        ex.Status switch
        {
            CtapStatus.PinAuthInvalid or CtapStatus.PinInvalid or CtapStatus.PinAuthBlocked
                or CtapStatus.PinBlocked or CtapStatus.PinPolicyViolation
                or CtapStatus.PuatRequired or CtapStatus.PinTokenExpired
                or CtapStatus.NotAllowed or CtapStatus.OperationDenied
                => new WebAuthnClientError(WebAuthnClientErrorCode.NotAllowed, ex.Message, ex),
            CtapStatus.KeyStoreFull or CtapStatus.LargeBlobStorageFull or CtapStatus.FpDatabaseFull
                or CtapStatus.LimitExceeded or CtapStatus.RequestTooLarge or CtapStatus.UserActionTimeout
                or CtapStatus.ActionTimeout or CtapStatus.Timeout
                => new WebAuthnClientError(WebAuthnClientErrorCode.Constraint, ex.Message, ex),
            CtapStatus.UnsupportedAlgorithm or CtapStatus.UnsupportedOption or CtapStatus.InvalidOption
                => new WebAuthnClientError(WebAuthnClientErrorCode.NotSupported, ex.Message, ex),
            CtapStatus.PinNotSet or CtapStatus.UpRequired
                => new WebAuthnClientError(WebAuthnClientErrorCode.Security, ex.Message, ex),
            CtapStatus.NoCredentials or CtapStatus.InvalidCredential
                => new WebAuthnClientError(WebAuthnClientErrorCode.InvalidState, ex.Message, ex),
            _ => new WebAuthnClientError(WebAuthnClientErrorCode.Unknown, ex.Message, ex),
        };

    private static WebAuthnClientError MapMakeCredentialCtapException(
        CtapException ex,
        bool hasPreviewSign)
    {
        if (ex.Status == CtapStatus.CredentialExcluded)
        {
            return new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidState,
                "A credential matching the exclude list already exists on this authenticator.",
                ex);
        }

        return hasPreviewSign
            ? Extensions.PreviewSign.PreviewSignErrors.MapCtapError(ex)
            : MapCtapStatusToWebAuthnError(ex);
    }

    private static WebAuthnClientError MapGetAssertionCtapException(
        CtapException ex,
        bool hasPreviewSign) =>
        hasPreviewSign
            ? Extensions.PreviewSign.PreviewSignErrors.MapCtapError(ex)
            : MapCtapStatusToWebAuthnError(ex);

    private static bool ShouldRetryWithRequiredUv(
        CtapException ex,
        Preferences.UserVerificationPreference userVerification) =>
        ex.Status == CtapStatus.PuatRequired &&
        userVerification != Preferences.UserVerificationPreference.Required;

    private async Task<MakeCredentialResponse> ExecuteMakeCredentialAsync(
        BackendMakeCredentialRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _backend.MakeCredentialAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            ZeroMemory(request.PinUvAuthParam);
        }
    }

    private async Task<IReadOnlyList<(ReadOnlyMemory<byte> Id, PublicKeyCredentialUserEntity? User, GetAssertionResponse Response)>> MatchCredentialsAsync(
        BackendGetAssertionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await CredentialMatcher.MatchAsync(_backend, request, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            ZeroMemory(request.PinUvAuthParam);
        }
    }

    private static void ZeroMemory(ReadOnlyMemory<byte>? memory)
    {
        if (memory is null || memory.Value.IsEmpty)
        {
            return;
        }

        if (MemoryMarshal.TryGetArray(memory.Value, out var segment) && segment.Array is not null)
        {
            CryptographicOperations.ZeroMemory(segment.AsSpan());
        }
    }

    private async Task<(
        PinUvAuthTokenSession? TokenSession,
        IMemoryOwner<byte>? PinOwner,
        ReadOnlyMemory<byte>? PinBytes)> AcquireTokenForDecisionAsync(
        UvDecision uvDecision,
        string rpId,
        IMemoryOwner<byte>? pinOwner,
        ReadOnlyMemory<byte>? pinBytes,
        CancellationToken cancellationToken)
    {
        if (!uvDecision.UseToken)
        {
            return (null, pinOwner, pinBytes);
        }

        if (uvDecision.Method == PinUvAuthMethod.Pin && pinBytes is null)
        {
            if (_prompt is null)
            {
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.NotAllowed,
                    "A PIN is required for this operation, but none was supplied and no credential prompt is configured.");
            }

            var (promptedSession, acceptedSecret) = await AcquireTokenViaPromptAsync(
                uvDecision.Permissions,
                rpId,
                cancellationToken).ConfigureAwait(false);

            // The accepted secret is returned so a later token re-mint in the same
            // ceremony can reuse it; the core operation zeroes and disposes it.
            return (promptedSession, acceptedSecret, acceptedSecret.Memory);
        }

        var tokenSession = await AcquirePinUvTokenAsync(
            uvDecision.Method!.Value,
            uvDecision.Permissions,
            rpId,
            pinBytes,
            cancellationToken).ConfigureAwait(false);

        return (tokenSession, pinOwner, pinBytes);
    }

    private async Task<(
        PublicKeyCredentialDescriptor? MatchedExclude,
        PinUvAuthTokenSession? TokenSession,
        bool PreflightPerformed)> PreflightExcludeListAndRemintAsync(
        RegistrationOptions options,
        AuthenticatorInfo info,
        UvDecision uvDecision,
        PinUvAuthTokenSession? tokenSession,
        ReadOnlyMemory<byte>? pinBytes,
        CancellationToken cancellationToken)
    {
        if (options.ExcludeCredentials is null || options.ExcludeCredentials.Count == 0 || tokenSession is null)
        {
            return (null, tokenSession, false);
        }

        // ToArray() creates a copy; pre-flight needs to pass token across async boundary.
        var tokenCopy = tokenSession.Token.ToArray();
        try
        {
            var matchedExclude = await Internal.ExcludeListPreflight.FindFirstMatchAsync(
                _backend,
                options.Rp.Id,
                options.ExcludeCredentials,
                info,
                tokenCopy,
                tokenSession.Protocol,
                cancellationToken).ConfigureAwait(false);

            // Re-mint the pinUvAuthToken between pre-flight and MakeCredential.
            // CTAP 2.1 §6.5.5.7: authenticators MAY consume permissions on use. On
            // YubiKey 5.8.0, the pre-flight's GetAssertion(up=false) consumes the
            // GetAssertion permission and the same token can no longer authorize a
            // subsequent MakeCredential; the device returns PinAuthInvalid.
            tokenSession.Dispose();
            tokenSession = await AcquirePinUvTokenAsync(
                uvDecision.Method!.Value,
                PinUvAuthTokenPermissions.MakeCredential,
                options.Rp.Id,
                pinBytes,
                cancellationToken).ConfigureAwait(false);

            return (matchedExclude, tokenSession, true);
        }
        catch (CtapException preflightEx)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.Unknown,
                $"Pre-flight excludeList probe failed (device returned {preflightEx.Status}). " +
                "This authenticator may not support silent excludeList probing.",
                preflightEx);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(tokenCopy);
        }
    }

    /// <summary>
    /// Acquires a PIN/UV auth token from the backend using a secret the caller already supplied.
    /// </summary>
    /// <remarks>
    /// A caller-supplied secret is never retried: resubmitting identical PIN bytes would burn
    /// authenticator attempts without any chance of a different outcome. Deciding whether to ask
    /// the user again belongs to the caller, or to the prompt-driven path in
    /// <see cref="AcquireTokenViaPromptAsync"/>.
    /// </remarks>
    private async Task<PinUvAuthTokenSession> AcquirePinUvTokenAsync(
        PinUvAuthMethod method,
        PinUvAuthTokenPermissions permissions,
        string rpId,
        ReadOnlyMemory<byte>? pinBytes,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _backend.GetPinUvTokenAsync(
                method,
                permissions,
                rpId,
                pinBytes,
                cancellationToken).ConfigureAwait(false);
        }
        catch (CtapException ex) when (IsPinRejection(ex.Status))
        {
            throw MapPinRejection(ex);
        }
    }

    /// <summary>
    /// Acquires a PIN/UV auth token by asking <see cref="_prompt"/> for the PIN, re-prompting
    /// after a rejected attempt.
    /// </summary>
    /// <remarks>
    /// Every attempt comes from a fresh prompt call; a rejected secret is zeroed immediately and
    /// never resubmitted. The loop stops when the prompt declines, when the authenticator reports
    /// a terminal PIN state, or when <see cref="MaxPromptAttempts"/> is reached.
    /// </remarks>
    /// <returns>
    /// The token session and the accepted secret, whose ownership passes to the caller.
    /// </returns>
    private async Task<(PinUvAuthTokenSession TokenSession, IMemoryOwner<byte> PinOwner)> AcquireTokenViaPromptAsync(
        PinUvAuthTokenPermissions permissions,
        string rpId,
        CancellationToken cancellationToken)
    {
        var prompt = _prompt ?? throw new InvalidOperationException("No credential prompt configured.");

        var info = await _backend.GetCachedInfoAsync(cancellationToken).ConfigureAwait(false);
        var minPinLength = info.MinPinLength ?? Ctap2MinPinLengthBytes;
        int? retriesRemaining = null;

        for (var attempt = 0; attempt < MaxPromptAttempts; attempt++)
        {
            var context = new CredentialPromptContext
            {
                Kind = CredentialKind.Pin,
                Scope = rpId,
                IsRetry = attempt > 0,
                RetriesRemaining = retriesRemaining,
                MinLengthBytes = minPinLength,
                MaxLengthBytes = Ctap2MaxPinLengthBytes
            };

            // WaitAsync bounds the wait even if an implementation ignores the token it is handed,
            // so a stuck prompt cannot strand the operation.
            var secret = await prompt.RequestSecretAsync(context, cancellationToken)
                .AsTask()
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (secret is null)
            {
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.NotAllowed,
                    "PIN required but declined");
            }

            try
            {
                var tokenSession = await _backend.GetPinUvTokenAsync(
                    PinUvAuthMethod.Pin,
                    permissions,
                    rpId,
                    secret.Memory,
                        cancellationToken).ConfigureAwait(false);

                return (tokenSession, secret);
            }
            catch (CtapException ex) when (ex.Status == CtapStatus.PinInvalid
                                           && attempt + 1 < MaxPromptAttempts)
            {
                ZeroAndDispose(secret);
                retriesRemaining = await TryGetPinRetriesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (CtapException ex) when (IsPinRejection(ex.Status))
            {
                ZeroAndDispose(secret);
                throw MapPinRejection(ex);
            }
            catch
            {
                ZeroAndDispose(secret);
                throw;
            }
        }

        throw new WebAuthnClientError(
            WebAuthnClientErrorCode.NotAllowed,
            $"PIN was rejected on {MaxPromptAttempts} attempts.");
    }

    /// <summary>
    /// Reads the authenticator's remaining PIN attempts for display in a retry prompt.
    /// </summary>
    /// <remarks>
    /// Purely informational: a failure to read the counter must not replace the PIN rejection
    /// the caller is actually dealing with, so all errors collapse to <c>null</c>.
    /// </remarks>
    private async Task<int?> TryGetPinRetriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _backend.GetPinRetriesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (CtapException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static bool IsPinRejection(CtapStatus status) =>
        status is CtapStatus.PinInvalid
            or CtapStatus.PinBlocked
            or CtapStatus.PinAuthInvalid
            or CtapStatus.PinAuthBlocked;

    /// <summary>
    /// Maps a terminal CTAP PIN status onto the client's error vocabulary, so raw CTAP status
    /// codes never reach high-level consumers.
    /// </summary>
    private static WebAuthnClientError MapPinRejection(CtapException ex) => ex.Status switch
    {
        CtapStatus.PinInvalid => new WebAuthnClientError(
            WebAuthnClientErrorCode.NotAllowed, "PIN was incorrect.", ex),
        CtapStatus.PinBlocked => new WebAuthnClientError(
            WebAuthnClientErrorCode.NotAllowed,
            "PIN is blocked. The authenticator must be reset before it can be used again.", ex),
        CtapStatus.PinAuthBlocked => new WebAuthnClientError(
            WebAuthnClientErrorCode.NotAllowed,
            "PIN authentication is blocked until the authenticator is power-cycled.", ex),
        _ => new WebAuthnClientError(
            WebAuthnClientErrorCode.NotAllowed, "PIN authentication failed.", ex)
    };

    private BackendMakeCredentialRequest BuildMakeCredentialRequest(
        RegistrationOptions options,
        WebAuthnClientData clientData,
        PinUvAuthTokenSession? tokenSession,
        UvDecision uvDecision,
        PublicKeyCredentialDescriptor? matchedExclude,
        bool preflightPerformed)
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

        // Use filtered exclude list only after pre-flight actually ran. Without pre-flight, preserve
        // the caller's original exclude list so authenticators still enforce it.
        IReadOnlyList<PublicKeyCredentialDescriptor>? excludeList = preflightPerformed
            ? matchedExclude is not null ? new[] { matchedExclude } : Array.Empty<PublicKeyCredentialDescriptor>()
            : options.ExcludeCredentials;

        return new BackendMakeCredentialRequest
        {
            ClientDataHash = clientData.Hash,
            Rp = options.Rp,
            User = options.User,
            PubKeyCredParams = options.PubKeyCredParams
                .Select(alg => new PublicKeyCredentialParameters { Algorithm = (CoseAlgorithmIdentifier)alg.Value })
                .ToList(),
            ExcludeList = excludeList,
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