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
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

/// <summary>Hardware-free regression cases for the assertion used by the attended hotplug test.</summary>
public class DeviceEventLifecycleTests
{
    [Fact]
    public void Lifecycle_TransientFragmentAddedThenRemoved_DoesNotResurrectObject()
    {
        var original = new TestKey("physical", 103);
        var fragment = new TestKey("pcsc", null);
        var returned = new TestKey("physical", 103);

        // Captured Linux ordering: a fragment replaces the original in revision 5,
        // then the reinserted composite replaces that fragment in revision 6.
        DeviceEventHotPlugTests.AssertNoRemovedObjectReadded([
            new DeviceAdded(1, original),
            new SnapshotComplete(1),
            new DeviceRemoved(5, original),
            new DeviceAdded(5, fragment),
            new DeviceRemoved(6, fragment),
            new DeviceAdded(6, returned)
        ]);
    }

    [Fact]
    public void Lifecycle_RemovedObjectAddedAgain_IsRejected()
    {
        var device = new TestKey("physical", 103);

        var error = Record.Exception(() => DeviceEventHotPlugTests.AssertNoRemovedObjectReadded([
            new DeviceAdded(1, device),
            new DeviceRemoved(2, device),
            new DeviceAdded(3, device)
        ]));

        Assert.IsAssignableFrom<Xunit.Sdk.XunitException>(error);
    }

    [Fact]
    public void Lifecycle_EqualValuedSuccessorWithDifferentReference_IsAllowed()
    {
        var original = new TestKey("physical", 103);
        var successor = new TestKey("physical", 103);

        DeviceEventHotPlugTests.AssertNoRemovedObjectReadded([
            new DeviceRemoved(2, original),
            new DeviceAdded(3, successor)
        ]);
    }

    // Value equality deliberately differs from reference identity, as custom providers may do.
    private sealed record TestKey(string DeviceId, int? SerialNumber) : IYubiKey
    {
        public ConnectionType AvailableConnections => ConnectionType.SmartCard;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection => throw new NotSupportedException();
    }
}