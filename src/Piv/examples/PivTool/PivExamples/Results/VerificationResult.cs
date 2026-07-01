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
/// Result of a signature verification operation.
/// </summary>
public sealed record VerificationResult
{
    /// <summary>
    /// Gets whether the verification operation completed without errors.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets whether the signature is valid (only meaningful when Success is true).
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the error message when the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the time taken for the operation in milliseconds.
    /// </summary>
    public long ElapsedMilliseconds { get; init; }

    /// <summary>
    /// Creates a result indicating a valid signature.
    /// </summary>
    public static VerificationResult Valid(long elapsedMs) =>
        new() { Success = true, IsValid = true, ElapsedMilliseconds = elapsedMs };

    /// <summary>
    /// Creates a result indicating an invalid signature.
    /// </summary>
    public static VerificationResult Invalid(long elapsedMs) =>
        new() { Success = true, IsValid = false, ElapsedMilliseconds = elapsedMs };

    /// <summary>
    /// Creates a failed verification result.
    /// </summary>
    public static VerificationResult Failed(string error) =>
        new() { Success = false, ErrorMessage = error };
}
