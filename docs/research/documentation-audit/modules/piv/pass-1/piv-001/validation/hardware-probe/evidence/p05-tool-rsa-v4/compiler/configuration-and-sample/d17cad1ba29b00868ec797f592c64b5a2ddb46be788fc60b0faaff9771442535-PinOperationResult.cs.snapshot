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
/// Result of a PIN/PUK operation.
/// </summary>
public sealed record PinOperationResult
{
    /// <summary>
    /// Gets whether the PIN/PUK operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the number of retries remaining after the operation.
    /// </summary>
    public int? RetriesRemaining { get; init; }

    /// <summary>
    /// Gets the error message when the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful PIN operation result.
    /// </summary>
    public static PinOperationResult Succeeded(int? retriesRemaining = null) =>
        new() { Success = true, RetriesRemaining = retriesRemaining };

    /// <summary>
    /// Creates a failed PIN operation result.
    /// </summary>
    public static PinOperationResult Failed(string error, int? retriesRemaining = null) =>
        new() { Success = false, ErrorMessage = error, RetriesRemaining = retriesRemaining };
}
