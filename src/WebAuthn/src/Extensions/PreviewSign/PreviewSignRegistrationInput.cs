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

using Yubico.YubiKit.WebAuthn.Cose;

namespace Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

/// <summary>
/// Input parameters for previewSign registration (key generation).
/// </summary>
/// <remarks>
/// <para>
/// Specifies the acceptable signing algorithms and user verification policy for a new signing key pair.
/// The authenticator will choose the first supported algorithm from the list.
/// </para>
/// <para>
/// Per CTAP v4 draft specification §3.1:
/// - Algorithms list must contain at least one entry
/// - Flags default to RequireUserPresence (0b001) if not specified
/// - Invalid flag values will cause registration to fail
/// </para>
/// </remarks>
public sealed record class PreviewSignRegistrationInput
{
    /// <summary>
    /// Gets the ordered list of acceptable COSE algorithms, from most to least preferred.
    /// The authenticator will select the first algorithm it supports.
    /// </summary>
    public IReadOnlyList<CoseAlgorithm> Algorithms { get; }

    /// <summary>
    /// Gets the user presence and verification policy for signing operations.
    /// </summary>
    /// <remarks>
    /// NOTE: Per WebAuthn previewSign spec §10.2.1 step 4, flags are derived from
    /// RegistrationOptions.UserVerification and SHOULD NOT be user-controllable at the
    /// WebAuthn client layer. This field is internal and set by the adapter based on UV preference.
    /// </remarks>
    internal PreviewSignFlags Flags { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewSignRegistrationInput"/>.
    /// </summary>
    /// <param name="algorithms">Ordered list of acceptable COSE algorithms.</param>
    /// <exception cref="WebAuthnClientError">
    /// Thrown when algorithms list is empty (InvalidRequest).
    /// </exception>
    /// <remarks>
    /// Flags are derived from RegistrationOptions.UserVerification per spec and are not
    /// user-controllable at this layer.
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
    /// <param name="algorithms">Ordered list of acceptable algorithms.</param>
    /// <returns>A <see cref="PreviewSignRegistrationInput"/> with default flags (RequireUserPresence).</returns>
    /// <remarks>
    /// This factory method provides parity with Swift's <c>PreviewSign.Registration.Input.generateKey(algorithms:)</c>.
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
