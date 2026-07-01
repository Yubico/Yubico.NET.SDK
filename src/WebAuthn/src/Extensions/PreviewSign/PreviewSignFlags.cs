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
/// Supported flag combinations encoded in the MakeCredential previewSign extension input.
/// </summary>
/// <remarks>
/// <para>
/// Defines the authenticator behavior required for signing operations. These flags are set at
/// registration time and enforced during signing.
/// </para>
/// </remarks>
[Flags]
public enum PreviewSignFlags : byte
{
    /// <summary>
    /// Do not require user presence or user verification for signing operations.
    /// </summary>
    Unattended = 0b000,

    /// <summary>
    /// Require user presence for signing operations.
    /// </summary>
    RequireUserPresence = 0b001,

    /// <summary>
    /// Require user presence and user verification for signing operations.
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