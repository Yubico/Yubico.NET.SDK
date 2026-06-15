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

using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

/// <summary>
/// Result of a device info query.
/// </summary>
public sealed record DeviceInfoResult
{
    /// <summary>
    /// Gets whether the device info query succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the device information.
    /// </summary>
    public DeviceInfo? DeviceInfo { get; init; }

    /// <summary>
    /// Gets the number of PIN retries remaining.
    /// </summary>
    public int? PinRetriesRemaining { get; init; }

    /// <summary>
    /// Gets the number of PUK retries remaining.
    /// </summary>
    public int? PukRetriesRemaining { get; init; }

    /// <summary>
    /// Gets the error message when the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful device info result.
    /// </summary>
    public static DeviceInfoResult Succeeded(DeviceInfo info, int? pinRetries = null, int? pukRetries = null) =>
        new() { Success = true, DeviceInfo = info, PinRetriesRemaining = pinRetries, PukRetriesRemaining = pukRetries };

    /// <summary>
    /// Creates a successful result with only retry information (no device info).
    /// </summary>
    public static DeviceInfoResult RetryInfoOnly(int? pinRetries, int? pukRetries) =>
        new() { Success = true, PinRetriesRemaining = pinRetries, PukRetriesRemaining = pukRetries };

    /// <summary>
    /// Creates a failed device info result.
    /// </summary>
    public static DeviceInfoResult Failed(string error) =>
        new() { Success = false, ErrorMessage = error };
}
