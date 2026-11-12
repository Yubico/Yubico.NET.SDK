# Additional A original whole units

**No content verdicts.** Source `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Read enclosing owner/helper/assertion/cleanup context from each named pinned source path.

## `core-001:unit:21bef981d5360feb`
`src/Core/src/Devices/DeviceChangeFeed.cs:62–95` — XML-contracts, original rank 20; current mapping pending final diff.

````csharp


    /// <summary>Replays membership then streams changes; cancellation, overflow, or failure ends this subscription.</summary>
    internal async IAsyncEnumerable<DeviceChange> Subscribe(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Dictionary<string, IYubiKey> snapshot;
        long revision;
        Subscription subscription;
        lock (_gate)
        {
            if (_completed)
                yield break;
            ThrowIfFaulted();
            cancellationToken.ThrowIfCancellationRequested();
            snapshot = _snapshot;
            revision = _revision;
            subscription = new Subscription(this);
            _subscribers = [.. _subscribers, subscription];
        }

        using (subscription)
        {
            foreach (var device in snapshot.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new DeviceAdded(revision, device);
            }
            cancellationToken.ThrowIfCancellationRequested();
            yield return new SnapshotComplete(revision);
            await foreach (var change in subscription.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                yield return change;
        }
    }
````

## `core-001:unit:8e7356e172293835`
`src/Tests.Shared/YubiKeyTestState.cs:459–471` — XML-contracts, original rank 21; current mapping pending final diff.

````csharp


    /// <summary>
    ///     Gets a device from the cache by serial number and connection type.
    /// </summary>
    public static YubiKeyTestState? GetDevice(int serialNumber, ConnectionType connectionType)
    {
        lock (s_lock)
        {
            var key = GetCacheKey(serialNumber, connectionType);
            return s_devices.GetValueOrDefault(key);
        }
    }
````

## `core-001:unit:2ab1f3fe98ee5ca4`
`src/Core/src/Abstractions/IDevice.cs:1–19` — XML-contracts, original rank 22; current mapping pending final diff.

````csharp
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

namespace Yubico.YubiKit.Core.Abstractions;

/// <summary>
///     Represents properties and methods common to all devices.
/// </summary>

````

## `core-001:unit:c680372cf0dd564e`
`src/Cli.Commands/src/Infrastructure/YkDeviceContext.cs:37–43` — XML-contracts, original rank 23; current mapping pending final diff.

````csharp


    /// <summary>
    ///     The concrete transport requested by the global <c>--transport</c> option, or <see langword="null" />
    ///     to use each applet's default transport order.
    /// </summary>
    public ConnectionType? PreferredConnection { get; init; }
````

## `core-001:unit:6e6307d613511921`
`src/Core/src/Devices/DiscoveryWorkerAdmission.cs:37–47` — XML-contracts, original rank 24; current mapping pending final diff.

````csharp


    /// <summary>
    ///     Waits (asynchronously, unbounded) for a worker slot. Used by identity reads, whose callers bound
    ///     their own wait via the read budget; the slot count still bounds concurrent native work.
    /// </summary>
    public static async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await Slots.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Admission();
    }
````

## `core-001:unit:d0757dfa453abbeb`
`src/Tests.Shared/Infrastructure/AllowList.cs:77–85` — XML-contracts, original rank 25; current mapping pending final diff.

````csharp


    /// <summary>
    ///     Determines whether the specified serial number is in the allow list.
    /// </summary>
    /// <param name="serialNumber">The serial number to check.</param>
    /// <returns>True if the serial number is allowed; otherwise, false.</returns>
    public bool IsDeviceAllowed(int? serialNumber) =>
        serialNumber is not null && _allowedSerials.Contains(serialNumber.Value);
````

## `core-001:unit:32032fade9d7fddb`
`src/Core/src/Devices/SharedDeviceInfoRead.cs:1–19` — XML-contracts, original rank 26; current mapping pending final diff.

````csharp
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

using Microsoft.Extensions.Logging;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>Owns one shared discovery device-info read and its connection/read pipeline.</summary>

````

## `core-001:unit:f1df5948d71dcc9d`
`src/Tests.Shared/Infrastructure/WithYubiKeyTraitDiscoverer.cs:1–23` — XML-contracts, original rank 27; current mapping pending final diff.

````csharp
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

using Xunit.Abstractions;
using Xunit.Sdk;

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
///     Trait discoverer that automatically adds "RequiresHardware" category
///     to any test method decorated with <see cref="WithYubiKeyAttribute"/>.
/// </summary>

````

## `core-001:unit:d5838c51b4cca87e`
`src/Tests.Shared/YubiKeyTestState.cs:229–235` — XML-contracts, original rank 28; current mapping pending final diff.

````csharp


    /// <summary>
    ///     Gets the form factor.
    ///     Convenience property for <c>DeviceInfo.FormFactor</c>.
    /// </summary>
    public FormFactor FormFactor => DeviceInfo.FormFactor;
````

## `core-001:unit:f713ea0c593064bf`
`src/Tests.Shared/Infrastructure/AuthorizedDevices.cs:74–94` — XML-contracts, original rank 29; current mapping pending final diff.

````csharp


    /// <summary>
    ///     Gets the first authorized device matching the specified criteria, or skips the test.
    /// </summary>
    /// <param name="criteria">The filter criteria to apply.</param>
    /// <returns>The first matching authorized device.</returns>
    /// <exception cref="Xunit.SkipException">Thrown when no matching authorized devices are available.</exception>
    public static YubiKeyTestState GetFirstOrSkip(FilterCriteria criteria)
    {
        var filtered = GetFiltered(criteria).ToList();
        if (filtered.Count == 0)
        {
            var available = string.Join(", ", GetAll().Select(d => d.ToString()));
            throw new Xunit.SkipException(
                $"No YubiKey matches filter criteria: {criteria.GetDescription()}. " +
                $"Available devices: [{available}]");
        }

        return filtered[0];
    }
````

## `core-001:unit:44a66dad89e6e9e9`
`src/Core/src/DeviceListenerStatus.cs:1–19` — XML-contracts, original rank 30; current mapping pending final diff.

````csharp
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

namespace Yubico.YubiKit.Core;

/// <summary>
/// Represents the current status of a device listener.
/// </summary>

````

## `core-001:unit:b530f2ba7c68da7e`
`src/Core/src/Abstractions/IYubiKey.cs:186–196` — XML-contracts, original rank 31; current mapping pending final diff.

````csharp


    /// <summary>
    ///     Opens the requested interface after claiming the physical YubiKey's known member interface IDs.
    /// </summary>
    /// <exception cref="ConnectionInUseException">The physical YubiKey already has a live connection.</exception>
    /// <exception cref="UnrecoveredConnectionException">
    ///     A previous native connection or discovery operation for this YubiKey was not proven released.
    /// </exception>
    Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : class, IConnection;
````

## `core-001:unit:a581a1ab8be230d2`
`src/Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs:55–64` — XML-contracts, original rank 32; current mapping pending final diff.

````csharp


    /// <summary>
    ///     Gets all authorized YubiKey devices discovered during test run initialization.
    /// </summary>
    /// <remarks>
    ///     Devices are discovered lazily on first access (not during type loading).
    ///     Includes only devices that passed allow list verification.
    /// </remarks>
    public static IReadOnlyList<YubiKeyTestState> AllAuthorizedDevices => LazyAuthorizedDevices.Value;
````

## `core-001:unit:7045e0544998269f`
`src/Core/src/Devices/FindYubiKeys.cs:35–40` — XML-contracts, original rank 33; current mapping pending final diff.

````csharp

/// <summary>
///     Discovers physical YubiKeys with bounded native enumeration and read waits. Await scans and pass a
///     cancellation token to cancel their waits. Enumeration timeouts omit only the affected transport;
///     caller cancellation throws <see cref="OperationCanceledException" />, and other enumeration failures propagate.
/// </summary>

````

## `core-001:unit:939161b425c3e89f`
`src/Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs:74–81` — XML-contracts, original rank 34; current mapping pending final diff.

````csharp


    /// <summary>
    ///     Gets or sets the required form factor.
    ///     Use FormFactor.Unknown (default) to match any form factor.
    ///     Current Core parsing can retain product flag bits, so exact base-value filters can miss flagged devices.
    /// </summary>
    public FormFactor FormFactor { get; set; } = FormFactor.Unknown;
````

## `core-001:unit:d28709e3276a4138`
`src/Tests.Shared/Infrastructure/FilterCriteria.cs:85–91` — XML-contracts, original rank 35; current mapping pending final diff.

````csharp


    /// <summary>
    ///     Gets or sets the assembly-qualified name of the custom filter type.
    ///     Used for xUnit serialization since <see cref="Type"/> cannot be directly serialized.
    /// </summary>
    public string? CustomFilterTypeName { get; init; }
````

## `core-001:unit:2a47e7cc5a9259e3`
`src/Tests.Shared/Infrastructure/AuthorizedDevices.cs:94–102` — XML-contracts, original rank 36; current mapping pending final diff.

````csharp


    /// <summary>
    ///     Gets authorized devices filtered by the specified criteria.
    /// </summary>
    /// <param name="criteria">The filter criteria to apply.</param>
    /// <returns>An enumerable of devices matching the criteria.</returns>
    public static IEnumerable<YubiKeyTestState> GetFiltered(FilterCriteria criteria) =>
        YubiKeyTestInfrastructure.FilterDevices(GetAll(), criteria);
````

## `core-001:unit:548bc1460a3f9b8f`
`src/Core/src/Devices/ConnectionTypeExtensions.cs:24–27` — XML-contracts, original rank 37; current mapping pending final diff.

````csharp


    /// <summary>True when the filter requests scanning any HID interface.</summary>
    public static bool IncludesHidScan(this ConnectionType filter) => (filter & HidGroup) != 0;
````

## `core-001:unit:c5b35440184cef31`
`src/Core/src/Devices/YubiKeyDevice.cs:256–275` — XML-contracts, original rank 38; current mapping pending final diff.

````csharp


    /// <summary>
    ///     The one slot-selection rule shared by normal connect, discovery connect, and registry identity
    ///     resolution.
    /// </summary>
    internal bool TryResolveSlot(
        ConnectionType connection,
        [NotNullWhen(true)] out IYubiKeyConnectionSlot? slot)
    {
        slot = connection switch
        {
            ConnectionType.SmartCard => _smartCard,
            ConnectionType.HidFido => _hidFido,
            ConnectionType.HidOtp => _hidOtp,
            ConnectionType.Hid => _hidFido ?? _hidOtp,
            _ => null
        };
        return slot is not null;
    }
````

## `core-001:unit:4f7f2feb81045f0f`
`src/Core/src/Devices/DeviceCapabilities.cs:25–30` — enum-values, original rank 5; current mapping pending final diff.

````csharp


    /// <summary>
    ///     Identifies the U2F (CTAP1) portion of the FIDO application.
    /// </summary>
    U2f = 0x02,
````

## `core-001:unit:abc48bf7ccf04d19`
`src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysFaultInjectionTests.cs:775–776` — enum-values, original rank 6; current mapping pending final diff.

````csharp

        Fail,
````

## `core-001:unit:26a01b51fa2d612f`
`src/Core/src/Devices/DiscoveryReadSkippedException.cs:37–44` — enum-values, original rank 7; current mapping pending final diff.

````csharp


    /// <summary>
    ///     Hotplug activity superseded this read before it opened the interface: the physical topology it
    ///     was started against may no longer exist, so it aborts (or its queued admission wait is
    ///     cancelled) instead of reading hardware it can no longer name. Retried on the next scan.
    /// </summary>
    SupersededByTransportActivity

````

## `core-001:unit:f7a53cd9184afd96`
`src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyMonitorTests.cs:351–351` — enum-values, original rank 8; current mapping pending final diff.

````csharp
 PublicationThrows,
````

