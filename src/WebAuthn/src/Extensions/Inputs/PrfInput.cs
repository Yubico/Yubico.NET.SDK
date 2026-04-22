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

namespace Yubico.YubiKit.WebAuthn.Extensions.Inputs;

/// <summary>
/// PRF evaluation salts (1-2 values).
/// </summary>
/// <param name="First">The first salt (required, typically 32 bytes).</param>
/// <param name="Second">The second salt (optional, typically 32 bytes).</param>
public sealed record class PrfEvaluation(
    ReadOnlyMemory<byte> First,
    ReadOnlyMemory<byte>? Second = null);

/// <summary>
/// Input for the PRF (Pseudo-Random Function) extension.
/// </summary>
/// <remarks>
/// For registration: requests PRF support on the credential.
/// For authentication: provides salt(s) for PRF evaluation.
/// </remarks>
public sealed record class PrfInput
{
    /// <summary>
    /// Gets the default evaluation salts (for all credentials).
    /// </summary>
    public PrfEvaluation? Eval { get; init; }

    /// <summary>
    /// Gets per-credential evaluation salts (authentication only).
    /// </summary>
    /// <remarks>
    /// Key is the raw credential ID bytes. Values are salts for that specific credential.
    /// </remarks>
    public IReadOnlyDictionary<ReadOnlyMemory<byte>, PrfEvaluation>? EvalByCredential { get; init; }
}
