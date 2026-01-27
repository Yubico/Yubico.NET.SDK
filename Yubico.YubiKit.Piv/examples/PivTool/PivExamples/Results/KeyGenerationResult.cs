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

namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

/// <summary>
/// Result of a PIV key generation operation.
/// </summary>
public sealed record KeyGenerationResult
{
    /// <summary>
    /// Gets whether the key generation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the slot where the key was generated.
    /// </summary>
    public PivSlot Slot { get; init; }

    /// <summary>
    /// Gets the algorithm of the generated key.
    /// </summary>
    public PivAlgorithm Algorithm { get; init; }

    /// <summary>
    /// Gets the public key bytes when successful.
    /// </summary>
    public ReadOnlyMemory<byte> PublicKey { get; init; }

    /// <summary>
    /// Gets the error message when the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful key generation result.
    /// </summary>
    public static KeyGenerationResult Succeeded(PivSlot slot, PivAlgorithm algorithm, ReadOnlyMemory<byte> publicKey) =>
        new() { Success = true, Slot = slot, Algorithm = algorithm, PublicKey = publicKey };

    /// <summary>
    /// Creates a failed key generation result.
    /// </summary>
    public static KeyGenerationResult Failed(string error) =>
        new() { Success = false, ErrorMessage = error };
}
