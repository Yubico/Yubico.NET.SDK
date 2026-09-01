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

using System.Reactive.Linq;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.UnitTests.Infrastructure;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
/// Guards the compatibility promise made when <c>System.Reactive</c> was removed from the SDK: the
/// SDK no longer <em>forces</em> Rx on consumers, but a consumer who adds the package themselves
/// still gets the full operator surface over <see cref="YubiKeyManager.DeviceChanges"/>.
/// </summary>
/// <remarks>
/// <para>
/// <c>System.Reactive</c> is referenced by this test project only — never by a shipping SDK library.
/// Rx's operators are extension methods on <see cref="IObservable{T}"/>, so they compose with the
/// SDK's own broadcaster without it knowing anything about Rx.
/// </para>
/// <para>
/// This is also the migration example for consumers upgrading from a build where Rx arrived
/// transitively: add one <c>PackageReference</c> and existing code compiles unchanged.
/// </para>
/// </remarks>
public class RxInteropTests
{
    private static DeviceEvent Event(DeviceAction action, string deviceId) =>
        new(action, new FakeYubiKey(deviceId));

    [Fact]
    public void RxSubscribeActionOverload_WorksAgainstTheSdkObservable()
    {
        using var repository = new YubiKeyDeviceRepository();
        var seen = new List<string>();

        // ObservableExtensions.Subscribe(Action<T>) comes from System.Reactive, not the BCL.
        using var subscription = repository.DeviceChanges.Subscribe(e => seen.Add(e.Device.DeviceId));

        repository.UpdateCache([new FakeYubiKey("device-1")]);

        Assert.Equal(["device-1"], seen);
    }

    [Fact]
    public void RxQueryOperators_ComposeOverTheSdkObservable()
    {
        using var repository = new YubiKeyDeviceRepository();
        var added = new List<string>();

        using var subscription = repository.DeviceChanges
            .Where(e => e.Action == DeviceAction.Added)
            .Select(e => e.Device.DeviceId)
            .Subscribe(added.Add);

        repository.UpdateCache([new FakeYubiKey("device-1"), new FakeYubiKey("device-2")]);
        repository.UpdateCache([new FakeYubiKey("device-1")]);

        // Two arrivals; the removal is filtered out by Where.
        Assert.Equal(2, added.Count);
        Assert.Contains("device-2", added);
    }

    [Fact]
    public void RxTake_CompletesAfterTheRequestedCount()
    {
        using var repository = new YubiKeyDeviceRepository();
        var completed = false;
        var received = 0;

        using var subscription = repository.DeviceChanges
            .Take(1)
            .Subscribe(onNext: _ => received++, onCompleted: () => completed = true);

        repository.UpdateCache([new FakeYubiKey("device-1")]);
        repository.UpdateCache([new FakeYubiKey("device-1"), new FakeYubiKey("device-2")]);

        Assert.Equal(1, received);
        Assert.True(completed);
    }

    [Fact]
    public void RxOnCompleted_FiresWhenTheSdkCompletesTheSequence()
    {
        var repository = new YubiKeyDeviceRepository();
        var completed = false;

        using var subscription = repository.DeviceChanges.Subscribe(
            onNext: _ => { },
            onCompleted: () => completed = true);

        repository.Dispose();

        Assert.True(completed);
    }
}