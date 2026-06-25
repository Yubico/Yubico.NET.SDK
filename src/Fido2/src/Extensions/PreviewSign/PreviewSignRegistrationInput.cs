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

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Input for the previewSign extension registration (key generation).
/// </summary>
/// <remarks>
/// <para>
/// The previewSign extension allows a FIDO2 credential to sign arbitrary data using a separate
/// signing key bound to the same authenticator. This input specifies the acceptable signing
/// algorithms for registration (key generation).
/// </para>
/// <para>
/// See: CTAP v4 draft Web Authentication sign extension
/// Reference: Plans/cnh-authenticator-rs-previewsign-parity.md
/// </para>
/// </remarks>
public sealed class PreviewSignRegistrationInput
{
    /// <summary>
    /// Gets the ordered list of acceptable COSE algorithms, from most to least preferred.
    /// The authenticator will select the first algorithm it supports.
    /// </summary>
    public IReadOnlyList<int> Algorithms { get; init; }

    /// <summary>
    /// Gets the user presence and verification policy for signing operations.
    /// </summary>
    /// <remarks>
    /// Flags: 0x01 = RequireUserPresence, 0x05 = RequireUserVerification.
    /// Per CTAP v4 draft specification, flags default to 0x01 if not specified.
    /// </remarks>
    public byte Flags { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewSignRegistrationInput"/>.
    /// </summary>
    /// <param name="algorithms">Ordered list of COSE algorithm identifiers.</param>
    /// <param name="flags">User presence/verification flags (default: 0x01).</param>
    public PreviewSignRegistrationInput(IReadOnlyList<int> algorithms, byte flags = 0x01)
    {
        ArgumentNullException.ThrowIfNull(algorithms);

        if (algorithms.Count == 0)
        {
            throw new ArgumentException("Algorithms list must contain at least one entry.", nameof(algorithms));
        }

        Algorithms = algorithms;
        Flags = flags;
    }
}
