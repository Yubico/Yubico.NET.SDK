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
/// Result of a PIV application reset.
/// </summary>
public sealed record ResetResult
{
    /// <summary>
    /// Gets whether the reset operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the error message when the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful reset result.
    /// </summary>
    public static ResetResult Succeeded() =>
        new() { Success = true };

    /// <summary>
    /// Creates a failed reset result.
    /// </summary>
    public static ResetResult Failed(string error) =>
        new() { Success = false, ErrorMessage = error };
}
