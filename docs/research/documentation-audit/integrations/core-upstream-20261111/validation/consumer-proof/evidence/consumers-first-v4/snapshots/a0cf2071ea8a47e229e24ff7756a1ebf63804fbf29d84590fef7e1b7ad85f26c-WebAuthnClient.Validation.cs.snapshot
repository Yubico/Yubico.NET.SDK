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

using System.Text;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <content>Request validation and the mapping from CTAP statuses to WebAuthn errors.</content>
public sealed partial class WebAuthnClient
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private void EnsureRpIdAuthorized(string rpId)
    {
        try
        {
            // CTAP text strings are UTF-8 (CBOR major type 3); never replace invalid UTF-16
            // while encoding/hashing an identifier, which would silently change its credential scope.
            _ = StrictUtf8.GetByteCount(rpId);
        }
        catch (EncoderFallbackException ex)
        {
            throw new WebAuthnClientError(WebAuthnClientErrorCode.InvalidRequest, "RP ID must be encodable as UTF-8 without replacement.", ex);
        }

        bool authorized;
        try
        {
            authorized = _rpIdValidator(_origin, rpId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new WebAuthnClientError(WebAuthnClientErrorCode.Security, "Application RP validation failed.", ex);
        }

        if (!authorized)
        {
            throw new WebAuthnClientError(WebAuthnClientErrorCode.Security, "The application did not approve this origin/RP pair.");
        }
    }

    private static void ValidateRegistrationOptions(PublicKeyCredentialCreationOptions options)
    {
        if (options.Challenge.Length == 0)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "Challenge cannot be empty");
        }

        if (string.IsNullOrEmpty(options.Rp.Id))
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

        if (options.PubKeyCredParams.Distinct().Count() != options.PubKeyCredParams.Count)
        {
            // CTAP 2.1-2.3 §6.1: pubKeyCredParams "MUST NOT include duplicate entries" (a platform requirement).
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "PubKeyCredParams must not contain duplicate algorithms");
        }

        if (options.Timeout is { } timeout && timeout <= TimeSpan.Zero)
        {
            // WebAuthn L3 §5.1.3 corrects an out-of-range timeout; this client does not clamp, so a
            // value that could never be waited for is a malformed request (null means no deadline).
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "Timeout must be positive, or null for no deadline");
        }

        if (options.TopOrigin is not null && options.CrossOrigin != true)
        {
            // WebAuthn L3 §5.8.1: topOrigin "is set only if the call was made from a context that is not
            // same-origin with its ancestors, i.e. if crossOrigin is true."
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "TopOrigin is only meaningful for a cross-origin request; set CrossOrigin = true or clear TopOrigin");
        }
    }

    private static void ValidateAuthenticationOptions(PublicKeyCredentialRequestOptions options)
    {
        if (options.Challenge.Length == 0)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "Challenge cannot be empty");
        }

        if (string.IsNullOrEmpty(options.RpId))
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "RP ID cannot be null or empty");
        }

        if (options.TopOrigin is not null && options.CrossOrigin != true)
        {
            // WebAuthn L3 §5.8.1: topOrigin "is set only if the call was made from a context that is not
            // same-origin with its ancestors, i.e. if crossOrigin is true."
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "TopOrigin is only meaningful for a cross-origin request; set CrossOrigin = true or clear TopOrigin");
        }

        if (options.Timeout is { } timeout && timeout <= TimeSpan.Zero)
        {
            // WebAuthn L3 §5.1.4 corrects an out-of-range timeout; this client does not clamp, so a
            // value that could never be waited for is a malformed request (null means no deadline).
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "Timeout must be positive, or null for no deadline");
        }
    }

    /// <summary>
    /// WebAuthn L3 §5.1.3 / §5.1.4: "If lifetimeTimer expires ... invoke the authenticatorCancel operation
    /// ... Throw a "NotAllowedError" DOMException." The cancel was already signalled through the deadline
    /// token; this turns a resulting cancellation into the typed error. There is no final deadline check
    /// that rejects a successful response racing cancellation. Caller cancellation is not
    /// wrapped: an OperationCanceledException propagates unchanged when the caller's token is cancelled.
    /// </summary>
    private static WebAuthnClientError CeremonyTimedOut(TimeSpan? timeout, OperationCanceledException inner) =>
        new(WebAuthnClientErrorCode.NotAllowed, $"The ceremony did not complete within its timeout of {timeout}", inner);

    /// <summary>
    /// Maps a caught <see cref="CtapException"/> to a typed <see cref="WebAuthnClientError"/>, retaining
    /// the original exception as its cause. This mapper does not cover the whole ceremony: discovery,
    /// token acquisition, and extension output processing can propagate lower-level failures.
    /// During ceremonies, MapCeremonyCtapException handles CredentialExcluded and previewSign-specific
    /// mapping before delegating here.
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

    private static bool ShouldRetryWithRequiredUv(
        CtapException ex,
        Preferences.UserVerificationPreference userVerification) =>
        ex.Status == CtapStatus.PuatRequired &&
        userVerification != Preferences.UserVerificationPreference.Required;
}