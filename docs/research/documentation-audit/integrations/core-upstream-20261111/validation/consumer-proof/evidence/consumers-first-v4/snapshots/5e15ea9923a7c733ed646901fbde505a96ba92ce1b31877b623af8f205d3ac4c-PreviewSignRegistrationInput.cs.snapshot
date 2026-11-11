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

using Yubico.YubiKit.Ctap2.Cose;

namespace Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

/// <summary>
/// Input for the previewSign extension registration (key generation).
/// </summary>
/// <remarks>
/// <para>
/// The previewSign extension generates a separate signing key bound to the same authenticator.
/// This input specifies the acceptable signing algorithms for registration (key generation).
/// The ceremony derives the signing policy from <c>UserVerification</c>: <c>Required</c> requests
/// UP and UV; every other preference requests UP. There is no public flag setter, so this API cannot
/// request unattended signing. Unsupported previewSign requests can be omitted; inspect the result.
/// </para>
/// </remarks>
public sealed record class PreviewSignRegistrationInput
{
    /// <summary>
    /// Gets the ordered list of acceptable signature algorithms, from most preferred to least preferred.
    /// </summary>
    public IReadOnlyList<CoseAlgorithm> Algorithms { get; }

    /// <summary>
    /// Gets the stored internal flag value.
    /// </summary>
    /// <remarks>
    /// The ceremony encoder derives a separate flags byte from the registration UV preference;
    /// it does not read or overwrite this property.
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
        Flags = PreviewSignFlags.RequireUserPresence; // Stored default; ceremony encoding derives its own flags.
    }

    /// <summary>
    /// Creates a registration input for generating a new signing key.
    /// </summary>
    /// <param name="algorithms">A list of acceptable signature algorithms, ordered from most preferred to least preferred.</param>
    /// <returns>A <see cref="PreviewSignRegistrationInput"/> with the requested algorithms.</returns>
    /// <remarks>
    /// The ceremony derives signing policy from its UV preference: Required requests UP and UV;
    /// other values request UP. The factory does not let the caller choose flags independently.
    /// </remarks>
    public static PreviewSignRegistrationInput GenerateKey(params CoseAlgorithm[] algorithms) =>
        new(algorithms);

    /// <summary>
    /// Creates an input with an explicit stored internal flag value; ceremony encoding does not use it.
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