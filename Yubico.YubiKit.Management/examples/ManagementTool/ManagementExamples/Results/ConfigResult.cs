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

namespace Yubico.YubiKit.Management.Examples.ManagementTool.ManagementExamples.Results;

/// <summary>
/// Result of a device configuration operation.
/// </summary>
public sealed record ConfigResult
{
    /// <summary>
    /// Gets whether the configuration operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets whether the device needs to be rebooted for changes to take effect.
    /// </summary>
    public bool RebootRequired { get; init; }

    /// <summary>
    /// Gets the error message when the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful configuration result.
    /// </summary>
    /// <param name="rebootRequired">Whether the device requires a reboot.</param>
    public static ConfigResult Succeeded(bool rebootRequired = false) =>
        new() { Success = true, RebootRequired = rebootRequired };

    /// <summary>
    /// Creates a failed configuration result.
    /// </summary>
    public static ConfigResult Failed(string error) =>
        new() { Success = false, ErrorMessage = error };
}
