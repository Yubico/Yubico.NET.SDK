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

namespace Yubico.YubiKit.Core.Transports.Hid;

/// <summary>
/// Diagnostic information from a HID listener indicating that device discovery should be rescanned.
/// </summary>
/// <remarks>
/// HID listener hints are not authoritative physical-device state. Consumers that need YubiKey
/// arrivals and removals should use <c>YubiKeyManager.WatchAsync</c>, which emits only
/// after a repository rescan and diff.
/// </remarks>
/// <param name="ChangeKind">The platform-reported HID topology change.</param>
/// <param name="PlatformDeviceId">A platform-specific diagnostic identifier when available.</param>
/// <param name="DevicePath">A platform-specific path when available.</param>
public sealed record HidDeviceRescanHint(
    HidDeviceChangeKind ChangeKind,
    string? PlatformDeviceId = null,
    string? DevicePath = null)
{
    /// <summary>
    /// Gets a generic rescan hint for a HID topology change whose details are unknown.
    /// </summary>
    public static HidDeviceRescanHint Unknown { get; } = new(HidDeviceChangeKind.Unknown);
}