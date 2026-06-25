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
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Core.IntegrationTests.Core;

public class YubiKeyTests : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await YubiKeyManager.ShutdownAsync();

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task FindAllAsync_ReturnsAtLeastOne()
    {
        var devices = await YubiKeyManager.FindAllAsync();
        Assert.NotEmpty(devices);
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task DeviceChanges_DetectsAddedDevice()
    {
        // Subscribe to device changes
        var tcs = new TaskCompletionSource<DeviceEvent>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        cts.Token.Register(() => tcs.TrySetCanceled());

        using var subscription = YubiKeyManager.DeviceChanges
            .Where(e => e.Action == DeviceAction.Added)
            .Take(1)
            .Subscribe(e => tcs.TrySetResult(e));

        // Start monitoring
        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(1));

        // Wait for device insertion (requires manual interaction)
        var deviceEvent = await tcs.Task;

        Assert.NotNull(deviceEvent.Device);
        Assert.Equal(DeviceAction.Added, deviceEvent.Action);
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task DeviceChanges_DetectsRemovedDevice()
    {
        // Subscribe to device changes
        var tcs = new TaskCompletionSource<DeviceEvent>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        cts.Token.Register(() => tcs.TrySetCanceled());

        using var subscription = YubiKeyManager.DeviceChanges
            .Where(e => e.Action == DeviceAction.Removed)
            .Take(1)
            .Subscribe(e => tcs.TrySetResult(e));

        // Start monitoring
        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(1));

        // Wait for device removal (requires manual interaction)
        var deviceEvent = await tcs.Task;

        Assert.NotNull(deviceEvent.Device); // Device should be populated for removed events
        Assert.Equal(DeviceAction.Removed, deviceEvent.Action);
    }
}