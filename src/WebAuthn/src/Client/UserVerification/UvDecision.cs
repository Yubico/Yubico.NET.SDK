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
internal static class UvDecisionLogic
{
    /// <summary>
    /// Determines how to handle user verification based on authenticator capabilities and preferences.
    /// </summary>
    /// <param name="info">The authenticator info.</param>
    /// <param name="preference">The user verification preference from the request.</param>
    /// <param name="pinAvailable">Whether a PIN is available (caller has PIN bytes).</param>
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

        bool clientPinSet = info.Options.TryGetValue("clientPin", out var pinSet) && pinSet;
        bool uvSupported = info.Options.TryGetValue("uv", out var uv) && uv;

        // If UV is required, ensure at least one method is available
        if (preference == UserVerificationPreference.Required)
        {
            bool hasUvMethod = (clientPinSet && pinAvailable) || uvSupported;
            if (!hasUvMethod)
            {
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.NotAllowed,
                    "User verification is required but the authenticator does not support UV " +
                    "and no PIN is available (or PIN is not set on the authenticator).");
            }
        }

        // Decide which method to use
        // Priority: PIN (if available) > built-in UV > none
        if (clientPinSet && pinAvailable)
        {
            // Use PIN method
            return new UvDecision(
                UseToken: true,
                UseUv: false,
                UvOption: preference == UserVerificationPreference.Required ? true : (bool?)null,
                Method: PinUvAuthMethod.Pin,
                Permissions: requestedPermissions);
        }

        if (uvSupported)
        {
            // Use built-in UV (biometric, etc.)
            return new UvDecision(
                UseToken: true,
                UseUv: true,
                UvOption: true,
                Method: PinUvAuthMethod.Uv,
                Permissions: requestedPermissions);
        }

        // No UV available - only allowed if preference is not Required
        if (preference == UserVerificationPreference.Required)
        {
            // This shouldn't happen due to the check above, but defensively handle it
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.NotAllowed,
                "User verification is required but no UV method is available.");
        }

        // UV is Preferred or Discouraged, and no method available - proceed without UV
        return new UvDecision(
            UseToken: false,
            UseUv: false,
            UvOption: null,
            Method: null,
            Permissions: requestedPermissions);
    }
}
