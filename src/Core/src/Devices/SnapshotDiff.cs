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

/// <summary>Computes physical membership changes and refreshes retained devices' last-known metadata.</summary>
internal static class SnapshotDiff
{
    internal static (Dictionary<string, IYubiKey> Next, List<DeviceChange> Changes) Apply(
        Dictionary<string, IYubiKey> current, IEnumerable<IYubiKey> observed, long revision)
    {
        var next = new Dictionary<string, IYubiKey>();
        foreach (var device in observed)
            next[YubiKeyDevice.PhysicalIdentityKeyFor(device)] = device;

        var removals = new List<DeviceChange>();
        var additions = new List<DeviceChange>();
        var updates = new List<DeviceChange>();
        foreach (var (key, existing) in current)
        {
            if (!next.ContainsKey(key))
                removals.Add(new DeviceRemoved(revision, existing));
        }

        // DeviceId describes the discovery evidence tier, not physical identity. Retain the published
        // object across tier changes so later removal still correlates with the original arrival.
        foreach (var (key, updated) in next)
        {
            if (!current.TryGetValue(key, out var existing))
            {
                additions.Add(new DeviceAdded(revision, updated));
                continue;
            }

            if (RequiresReplacement(existing, updated))
            {
                removals.Add(new DeviceRemoved(revision, existing));
                additions.Add(new DeviceAdded(revision, updated));
                continue;
            }

            next[key] = existing;
            if (UpdateMetadata(existing, updated))
                updates.Add(new DeviceUpdated(revision, existing));
        }

        return (next, [.. removals, .. additions, .. updates]);
    }

    private static bool RequiresReplacement(IYubiKey existing, IYubiKey updated) =>
        existing.AvailableConnections != updated.AvailableConnections ||
        (existing.SerialNumber is { } oldSerial && updated.SerialNumber is { } newSerial && oldSerial != newSerial);

    private static bool UpdateMetadata(IYubiKey existing, IYubiKey updated)
    {
        if (existing is YubiKeyDevice retained && updated is YubiKeyDevice latest &&
            latest.DeviceInfo is { } metadata && retained.DeviceInfo != metadata)
        {
            retained.DeviceInfo = metadata;
            return true;
        }
        return false;
    }
}