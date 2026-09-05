// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.Management;

/// <summary>Configures optional policy when applying a device configuration.</summary>
/// <remarks>
///     Lock-code memory is borrowed for the duration of the operation. The caller retains ownership and must
///     clear sensitive input after the operation completes. The options object is not retained.
/// </remarks>
public sealed class SetDeviceConfigOptions
{
    /// <summary>Gets whether the YubiKey reboots after applying the configuration.</summary>
    public bool Reboot { get; init; }

    /// <summary>Gets the optional borrowed 16-byte current configuration lock code.</summary>
    public ReadOnlyMemory<byte>? CurrentLockCode { get; init; }

    /// <summary>Gets the optional borrowed 16-byte new configuration lock code.</summary>
    public ReadOnlyMemory<byte>? NewLockCode { get; init; }
}
