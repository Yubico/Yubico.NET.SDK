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

using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>A notification from a device monitoring subscription.</summary>
/// <remarks>The set of derived records is closed: <see cref="DeviceAdded"/>, <see cref="DeviceRemoved"/>,
/// <see cref="DeviceUpdated"/> and <see cref="SnapshotComplete"/>. A switch over those four is exhaustive.</remarks>
public abstract record DeviceChange
{
    private protected DeviceChange(long revision) => Revision = revision;

    /// <summary>Increments by one per applied scan; changes sharing a revision were produced by one scan.</summary>
    public long Revision { get; init; }
}

/// <summary>A physical device joined the snapshot, or is replayed to a new subscription.</summary>
/// <param name="Revision">The scan revision that produced the change or the replay snapshot.</param>
/// <param name="Device">The published device.</param>
public sealed record DeviceAdded(long Revision, IYubiKey Device) : DeviceChange(Revision);

/// <summary>A previously published physical device left the snapshot.</summary>
/// <param name="Revision">The scan revision that produced the change.</param>
/// <param name="Device">The originally published device.</param>
public sealed record DeviceRemoved(long Revision, IYubiKey Device) : DeviceChange(Revision);

/// <summary>A retained physical device received changed last-known metadata.</summary>
/// <param name="Revision">The scan revision that produced the change.</param>
/// <param name="Device">The retained device with updated metadata.</param>
public sealed record DeviceUpdated(long Revision, IYubiKey Device) : DeviceChange(Revision);

/// <summary>Marks the end of the initial snapshot replay.</summary>
/// <remarks>Emitted once per subscription that starts while the monitor is running, after the initial replay and
/// including when the replay is empty. A subscription started after disposal ends without any notification.</remarks>
/// <param name="Revision">The revision of the replayed snapshot.</param>
public sealed record SnapshotComplete(long Revision) : DeviceChange(Revision);