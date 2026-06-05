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
/// Preference for creating a discoverable (resident) credential.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#enumdef-residentkeyrequirement">
/// WebAuthn ResidentKeyRequirement</see>.
/// </remarks>
public enum ResidentKeyPreference
{
    /// <summary>
    /// Prefer a non-discoverable (server-side) credential.
    /// </summary>
    Discouraged,

    /// <summary>
    /// Prefer discoverable if supported, fall back to non-discoverable.
    /// </summary>
    Preferred,

    /// <summary>
    /// Require a discoverable credential. Fails if the authenticator doesn't support it.
    /// </summary>
    Required
}

/// <summary>
/// Extension methods for <see cref="ResidentKeyPreference"/>.
/// </summary>
internal static class ResidentKeyPreferenceExtensions
{
    /// <summary>
    /// Converts the preference to the WebAuthn specification string.
    /// </summary>
    internal static string ToSpecString(this ResidentKeyPreference preference) => preference switch
    {
        ResidentKeyPreference.Discouraged => "discouraged",
        ResidentKeyPreference.Preferred => "preferred",
        ResidentKeyPreference.Required => "required",
        _ => throw new ArgumentOutOfRangeException(nameof(preference))
    };
}
