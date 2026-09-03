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
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Preferences;

namespace Yubico.YubiKit.WebAuthn.Client.UserVerification;

/// <summary>
/// Decision result for user verification (UV) handling.
/// </summary>
/// <param name="UseToken">Whether to obtain a PIN/UV auth token.</param>
/// <param name="UseUv">Whether to use built-in user verification (biometric/etc).</param>
/// <param name="UvOption">Value to send in the CTAP 'uv' option (true/false/null).</param>
/// <param name="Method">The PIN/UV authentication method to use, if a token is needed.</param>
/// <param name="Permissions">The permissions to request for the PIN/UV token.</param>
internal readonly record struct UvDecision(
    bool UseToken,
    bool UseUv,
    bool? UvOption,
    PinUvAuthMethod? Method,
    PinUvAuthTokenPermissions Permissions);

/// <summary>
/// User verification decision logic.
/// </summary>
/// <remarks>
/// First decides <em>whether</em> user verification is needed, then decides <em>which</em> method
/// satisfies it. "Configured" means the option is both advertised and enabled: a YubiKey with no
/// PIN set still advertises <c>clientPin: false</c>, and treating advertisement alone as available
/// verification would request a token from a key that has no enabled verification method.
/// </remarks>
internal static class UvDecisionLogic
{
    /// <summary>
    /// Permissions that are part of an ordinary registration or authentication ceremony. Anything
    /// beyond these (credential management, bio enrollment, large-blob write, authenticator config)
    /// requires user verification even when the relying party discouraged it.
    /// </summary>
    private const PinUvAuthTokenPermissions CeremonyPermissions =
        PinUvAuthTokenPermissions.MakeCredential | PinUvAuthTokenPermissions.GetAssertion;

    /// <summary>
    /// Determines how to handle user verification based on authenticator capabilities and preferences.
    /// </summary>
    /// <param name="info">The authenticator info.</param>
    /// <param name="preference">The user verification preference from the request.</param>
    /// <param name="pinAvailable">
    /// Whether a PIN can be obtained — either the caller passed PIN bytes or the client has an
    /// <c>ICredentialPrompt</c> configured.
    /// </param>
    /// <param name="requestedPermissions">The permissions needed for the operation.</param>
    /// <returns>The UV decision.</returns>
    /// <exception cref="WebAuthnClientError">
    /// Thrown when UV is required but the authenticator doesn't support it and no PIN is available.
    /// </exception>
    public static UvDecision Decide(
        AuthenticatorInfo info,
        UserVerificationPreference preference,
        bool pinAvailable,
        PinUvAuthTokenPermissions requestedPermissions)
    {
        ArgumentNullException.ThrowIfNull(info);

        bool clientPinConfigured = IsOptionEnabled(info, "clientPin");
        bool builtInUvConfigured = IsOptionEnabled(info, "uv");
        bool uvConfigured = clientPinConfigured || builtInUvConfigured || IsOptionEnabled(info, "bioEnroll");

        var noUv = new UvDecision(
            UseToken: false,
            UseUv: false,
            UvOption: null,
            Method: null,
            Permissions: requestedPermissions);

        if (!ShouldUseUv(info, preference, requestedPermissions, uvConfigured))
        {
            return noUv;
        }

        // User verification is wanted. Pick a method: PIN first, then built-in UV.
        if (clientPinConfigured && pinAvailable)
        {
            return new UvDecision(
                UseToken: true,
                UseUv: false,
                UvOption: preference == UserVerificationPreference.Required ? true : (bool?)null,
                Method: PinUvAuthMethod.Pin,
                Permissions: requestedPermissions);
        }

        if (builtInUvConfigured)
        {
            return new UvDecision(
                UseToken: true,
                UseUv: true,
                UvOption: true,
                Method: PinUvAuthMethod.Uv,
                Permissions: requestedPermissions);
        }

        // Wanted, but nothing can satisfy it.
        if (preference == UserVerificationPreference.Required)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.NotAllowed,
                "User verification is required but the authenticator does not support UV " +
                "and no PIN is available (or PIN is not set on the authenticator).");
        }

        // The relying party did not require verification, so try the ceremony without it rather
        // than failing on our own reading of the authenticator's options. If the authenticator
        // really does insist, it answers CTAP2_ERR_PUAT_REQUIRED and WebAuthnClient retries the
        // whole ceremony with Required.
        return noUv;
    }

    /// <summary>
    /// Decides whether a PIN/UV auth token is needed at all, before any question of which method
    /// would provide it.
    /// </summary>
    private static bool ShouldUseUv(
        AuthenticatorInfo info,
        UserVerificationPreference preference,
        PinUvAuthTokenPermissions requestedPermissions,
        bool uvConfigured) =>
        preference switch
        {
            UserVerificationPreference.Required => true,

            // WebAuthn L2 5.4.2 allows Preferred to proceed without verification when none is
            // available, so only enabled verification methods count as configured here. This
            // intentionally differs from implementations that treat an advertised false option as
            // available verification.
            UserVerificationPreference.Preferred => uvConfigured,

            UserVerificationPreference.Discouraged =>
                RequiresUvDespiteDiscouraged(info, requestedPermissions, uvConfigured),

            _ => false
        };

    /// <summary>
    /// The relying party discouraged user verification, but the authenticator or the request may
    /// still force it.
    /// </summary>
    private static bool RequiresUvDespiteDiscouraged(
        AuthenticatorInfo info,
        PinUvAuthTokenPermissions requestedPermissions,
        bool uvConfigured)
    {
        // Every override below presupposes that verification is actually configured. A key with no
        // PIN and no biometrics has nothing to force.
        if (!uvConfigured)
        {
            return false;
        }

        // The authenticator is configured to always verify, which outranks the relying party.
        if (IsOptionEnabled(info, "alwaysUv"))
        {
            return true;
        }

        // Pre-CTAP-2.1 authenticators with verification configured refuse an unverified
        // makeCredential. CTAP 2.1 authenticators advertise makeCredUvNotRqd to opt out of that.
        bool isMakeCredential = (requestedPermissions & PinUvAuthTokenPermissions.MakeCredential) != 0;
        if (isMakeCredential && !IsOptionEnabled(info, "makeCredUvNotRqd"))
        {
            return true;
        }

        // Privileged permissions always need verification, whatever the relying party asked for.
        return (requestedPermissions & ~CeremonyPermissions) != 0;
    }

    /// <summary>
    /// Returns true only when the option is both advertised and enabled.
    /// </summary>
    private static bool IsOptionEnabled(AuthenticatorInfo info, string option) =>
        info.Options.TryGetValue(option, out bool enabled) && enabled;
}