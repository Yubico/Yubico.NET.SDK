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

namespace Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

/// <summary>
/// User presence and verification policy flags for previewSign extension.
/// </summary>
/// <remarks>
/// <para>
/// Defines the authenticator behavior required for signing operations.
/// These flags are set at registration time and enforced during signing.
/// </para>
/// <para>
/// Per CTAP v4 draft Web Authentication sign extension, only three bit patterns are valid:
/// - 0b000 (Unattended): No user presence or verification required
/// - 0b001 (RequireUserPresence): User presence required (default)
/// - 0b101 (RequireUserVerification): User presence AND verification required
/// </para>
/// </remarks>
[Flags]
public enum PreviewSignFlags : byte
{
    /// <summary>
    /// No user presence or verification required (unattended signing).
    /// </summary>
    Unattended = 0b000,

    /// <summary>
    /// User presence required (default). Authenticator will require physical touch.
    /// </summary>
    RequireUserPresence = 0b001,

    /// <summary>
    /// User presence AND verification required. Authenticator will require physical touch and PIN/biometric.
    /// </summary>
    RequireUserVerification = 0b101
}

/// <summary>
/// Extension methods for <see cref="PreviewSignFlags"/>.
/// </summary>
internal static class PreviewSignFlagsExtensions
{
    /// <summary>
    /// Determines whether the flags value is one of the three valid patterns.
    /// </summary>
    /// <param name="flags">The flags value to validate.</param>
    /// <returns>True if the flags represent a valid policy; otherwise false.</returns>
    public static bool IsValid(this PreviewSignFlags flags) =>
        flags is PreviewSignFlags.Unattended
            or PreviewSignFlags.RequireUserPresence
            or PreviewSignFlags.RequireUserVerification;
}
