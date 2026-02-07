// Copyright 2025 Yubico AB
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

using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core;

public enum DeviceAction
{
    Added,
    Removed,
    Updated
}

/// <summary>
/// Represents a device event (added, removed, or updated).
/// </summary>
/// <remarks>
/// <para>
/// Events can reference either a transport-level <see cref="IYubiKeyReference"/> or a
/// composite <see cref="IYubiKey"/>. Use whichever property is appropriate for your use case.
/// </para>
/// <para>
/// For device removal events, the device reference may be null. Use <see cref="DeviceId"/>
/// to identify which device was removed.
/// </para>
/// </remarks>
public class DeviceEvent
{
    /// <summary>
    /// Creates a device event for a transport-level reference.
    /// </summary>
    public DeviceEvent(DeviceAction action, IYubiKeyReference? reference)
    {
        Action = action;
        Reference = reference;
        DeviceId = reference?.DeviceId;
    }

    /// <summary>
    /// Creates a device event for a composite YubiKey.
    /// </summary>
    public DeviceEvent(DeviceAction action, IYubiKey? yubiKey)
    {
        Action = action;
        YubiKey = yubiKey;
        DeviceId = yubiKey?.DeviceId;
    }

    /// <summary>
    /// The action that occurred (Added, Removed, Updated).
    /// </summary>
    public DeviceAction Action { get; }

    /// <summary>
    /// The transport-level reference, if this event is at the transport level.
    /// </summary>
    public IYubiKeyReference? Reference { get; }

    /// <summary>
    /// The composite YubiKey, if this event is at the composite level.
    /// </summary>
    public IYubiKey? YubiKey { get; }

    /// <summary>
    /// The device ID. For removal events, this may be set even when Device is null.
    /// </summary>
    public string? DeviceId { get; init; }

    /// <summary>
    /// When the event occurred.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the device reference (for backwards compatibility).
    /// </summary>
    [Obsolete("Use Reference or YubiKey property instead.")]
    public IYubiKeyReference? Device => Reference;
}