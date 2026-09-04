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
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client.UserVerification;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <content>PIN/UV auth token acquisition, including the prompt-driven retry loop.</content>
public sealed partial class WebAuthnClient
{
    /// <summary>CTAP 2.1 minimum PIN length in Unicode code points.</summary>
    private const int Ctap2MinPinLengthCodePoints = 4;

    /// <summary>CTAP 2.1 maximum PIN length in bytes.</summary>
    private const int Ctap2MaxPinLengthBytes = 63;

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
            if (_options.CredentialPrompt is null)
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

    /// <summary>
    /// Acquires a PIN/UV auth token from the backend using a secret the caller already supplied.
    /// </summary>
    /// <remarks>
    /// A caller-supplied secret is never retried because the SDK has no replacement value to
    /// submit. Deciding whether to ask the user again belongs to the caller, or to the prompt-driven path in
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
    /// Acquires a PIN/UV auth token by asking <see cref="WebAuthnClientOptions.CredentialPrompt"/>
    /// for the PIN, re-prompting after a rejected attempt.
    /// </summary>
    /// <remarks>
    /// Every attempt comes from a fresh prompt call; a rejected secret is zeroed immediately and
    /// never resubmitted. The loop stops when the prompt declines, when the authenticator reports
    /// a terminal PIN state, or when <see cref="WebAuthnClientOptions.MaxPromptAttempts"/> is reached.
    /// </remarks>
    /// <returns>
    /// The token session and the accepted secret, whose ownership passes to the caller.
    /// </returns>
    private async Task<(PinUvAuthTokenSession TokenSession, IMemoryOwner<byte> PinOwner)> AcquireTokenViaPromptAsync(
        PinUvAuthTokenPermissions permissions,
        string rpId,
        CancellationToken cancellationToken)
    {
        var prompt = _options.CredentialPrompt ?? throw new InvalidOperationException("No credential prompt configured.");

        var info = await _backend.GetCachedInfoAsync(cancellationToken).ConfigureAwait(false);
        var minPinLength = info.MinPinLength ?? Ctap2MinPinLengthCodePoints;
        var maxAttempts = _options.MaxPromptAttempts;
        int? retriesRemaining = null;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var context = new CredentialPromptContext
            {
                Kind = CredentialKind.Pin,
                Scope = rpId,
                IsRetry = attempt > 0,
                RetriesRemaining = retriesRemaining,
                MinLengthCodePoints = minPinLength,
                MaxLengthBytes = Ctap2MaxPinLengthBytes
            };

            var promptTask = prompt.RequestSecretAsync(context, cancellationToken).AsTask();
            IMemoryOwner<byte>? secret;
            try
            {
                secret = await promptTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _ = DisposeLatePromptResultAsync(promptTask);
                throw;
            }

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
            catch (CtapException ex) when (ex.Status == CtapStatus.PinInvalid)
            {
                ZeroAndDispose(secret);

                if (attempt + 1 >= maxAttempts)
                {
                    throw MapPinRejection(ex);
                }

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
            "PIN authentication did not complete within the prompt-attempt limit.");
    }

    private static async Task DisposeLatePromptResultAsync(Task<IMemoryOwner<byte>?> promptTask)
    {
        IMemoryOwner<byte>? owner;
        try
        {
            owner = await promptTask.ConfigureAwait(false);
        }
        catch
        {
            // Observe a fault from work that completed after the operation was cancelled.
            return;
        }

        try
        {
            ZeroAndDispose(owner);
        }
        catch
        {
            // Cleanup cannot be surfaced because the cancelled operation has already returned.
        }
    }

    /// <summary>
    /// Reads the authenticator's remaining PIN attempts for display in a retry prompt.
    /// </summary>
    /// <remarks>
    /// Purely informational: a failure to read the counter must not replace the PIN rejection
    /// the caller is actually dealing with, so failures collapse to <c>null</c>. Caller cancellation
    /// still propagates.
    /// </remarks>
    private async Task<int?> TryGetPinRetriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _backend.GetPinRetriesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
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
}