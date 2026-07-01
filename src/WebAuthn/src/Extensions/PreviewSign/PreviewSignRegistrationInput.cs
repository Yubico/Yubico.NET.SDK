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

using Yubico.YubiKit.Fido2.Cose;

namespace Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

/// <summary>
/// Input for the previewSign extension registration (key generation).
/// </summary>
/// <remarks>
/// <para>
/// The previewSign extension generates a separate signing key bound to the same authenticator.
/// This input specifies the acceptable signing algorithms for registration (key generation).
/// </para>
/// </remarks>
public sealed record class PreviewSignRegistrationInput
{
    /// <summary>
    /// Gets the ordered list of acceptable signature algorithms, from most preferred to least preferred.
    /// </summary>
    public IReadOnlyList<CoseAlgorithm> Algorithms { get; }

    /// <summary>
    /// Gets the user presence (UP) and user verification (UV) policy for signing operations.
    /// </summary>
    /// <remarks>
    /// The WebAuthn adapter derives this value from the registration options.
    /// </remarks>
    internal PreviewSignFlags Flags { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewSignRegistrationInput"/>.
    /// </summary>
    /// <param name="algorithms">A list of acceptable signature algorithms, ordered from most preferred to least preferred.</param>
    /// <exception cref="WebAuthnClientError">
    /// Thrown when algorithms list is empty (InvalidRequest).
    /// </exception>
    /// <remarks>
    /// Flags are derived from registration options and are not user-controllable at this layer.
    /// </remarks>
    public PreviewSignRegistrationInput(IReadOnlyList<CoseAlgorithm> algorithms)
    {
        if (algorithms.Count == 0)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "previewSign requires at least one algorithm");
        }

        Algorithms = algorithms;
        Flags = PreviewSignFlags.RequireUserPresence; // Default, overridden by adapter
    }

    /// <summary>
    /// Creates a registration input for generating a new signing key.
    /// </summary>
    /// <param name="algorithms">A list of acceptable signature algorithms, ordered from most preferred to least preferred.</param>
    /// <returns>A <see cref="PreviewSignRegistrationInput"/> with default flags (RequireUserPresence).</returns>
    /// <remarks>
    /// This factory method mirrors the WebAuthn previewSign generate-key input shape.
    /// </remarks>
    public static PreviewSignRegistrationInput GenerateKey(params CoseAlgorithm[] algorithms) =>
        new(algorithms);

    /// <summary>
    /// Internal test helper to create input with explicit flags for encoder testing.
    /// </summary>
    internal static PreviewSignRegistrationInput WithFlags(
        IReadOnlyList<CoseAlgorithm> algorithms,
        PreviewSignFlags flags)
    {
        if (algorithms.Count == 0)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "previewSign requires at least one algorithm");
        }

        if (!flags.IsValid())
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "Invalid previewSign flags value");
        }

        return new PreviewSignRegistrationInput(algorithms) { Flags = flags };
    }
}