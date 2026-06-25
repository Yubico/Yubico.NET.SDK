// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.WebAuthn.Preferences;

/// <summary>
/// Preference for user verification during an operation.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#enumdef-userverificationrequirement">
/// WebAuthn UserVerificationRequirement</see>.
/// </remarks>
public enum UserVerificationPreference
{
    /// <summary>
    /// Skip user verification if possible.
    /// </summary>
    Discouraged,

    /// <summary>
    /// Prefer user verification if available, but allow without.
    /// </summary>
    Preferred,

    /// <summary>
    /// Require user verification (PIN or biometric). Fails if not possible.
    /// </summary>
    Required
}

/// <summary>
/// Extension methods for <see cref="UserVerificationPreference"/>.
/// </summary>
internal static class UserVerificationPreferenceExtensions
{
    /// <summary>
    /// Converts the preference to the WebAuthn specification string.
    /// </summary>
    internal static string ToSpecString(this UserVerificationPreference preference) => preference switch
    {
        UserVerificationPreference.Discouraged => "discouraged",
        UserVerificationPreference.Preferred => "preferred",
        UserVerificationPreference.Required => "required",
        _ => throw new ArgumentOutOfRangeException(nameof(preference))
    };
}
