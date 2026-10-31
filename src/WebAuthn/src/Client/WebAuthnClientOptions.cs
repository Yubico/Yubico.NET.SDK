// Copyright 2026 Yubico AB
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

using Yubico.YubiKit.Core.Credentials;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <summary>
/// Configuration that applies to every ceremony a <see cref="WebAuthnClient"/> runs.
/// </summary>
/// <remarks>
/// This is a non-positional record so later releases can add optional settings without breaking
/// callers. Values are validated as they are assigned, so an unusable configuration fails when the
/// options are built rather than part-way through a ceremony that has already reached the
/// authenticator.
/// </remarks>
public sealed record WebAuthnClientOptions
{
    /// <summary>
    /// The default value of <see cref="MaxPromptAttempts"/>.
    /// </summary>
    public const int DefaultMaxPromptAttempts = 3;

    private readonly int _maxPromptAttempts = DefaultMaxPromptAttempts;
    private readonly RpIdValidator? _rpIdValidator;

    /// <summary>
    /// Gets the required application callback checking RP validity and origin/RP authorization before ceremony device I/O.
    /// </summary>
    /// <remarks>
    /// The application owns RP syntax validity and the complete RP binding policy. The SDK checks
    /// only nonempty, faithfully UTF-8 encodable RP text. See <see cref="Client.RpIdValidator"/>
    /// for responsibilities, synchronous execution, failure behavior, and RP-server checks.
    /// </remarks>
    /// <exception cref="ArgumentNullException">The property is initialized with <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The property was not configured.</exception>
    public required RpIdValidator RpIdValidator
    {
        get => _rpIdValidator ?? throw new InvalidOperationException(
            "RpIdValidator must be configured before creating a WebAuthn client.");
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _rpIdValidator = value;
        }
    }

    /// <summary>
    /// Gets the maximum number of times the client asks <see cref="CredentialPrompt"/> for a PIN
    /// during a single ceremony. Defaults to <see cref="DefaultMaxPromptAttempts"/>.
    /// </summary>
    /// <remarks>
    /// Reaching the cap fails the ceremony. The authenticator independently enforces its own retry
    /// limit and may report a terminal PIN state before this cap is reached.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    public int MaxPromptAttempts
    {
        get => _maxPromptAttempts;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _maxPromptAttempts = value;
        }
    }

    /// <summary>
    /// Gets the prompt used to obtain a PIN when a ceremony needs one and the caller did not supply
    /// it. When <see langword="null"/>, such ceremonies fail with
    /// <see cref="WebAuthnClientErrorCode.NotAllowed"/> rather than prompting.
    /// </summary>
    public ICredentialPrompt? CredentialPrompt { get; init; }
}