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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

/// <summary>
/// Hot-plug behaviour against real hardware. These double as the reference examples for how a
/// consumer waits for a YubiKey — no third-party dependency required.
/// </summary>
public class YubiKeyTests : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await YubiKeyManager.ShutdownAsync();

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task FindAllAsync_ReturnsAtLeastOne()
    {
        var devices = await TransientScanRetry.ScanAsync(() => YubiKeyManager.FindAllAsync());
        Assert.NotEmpty(devices);
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task WatchAsync_DetectsAddedDevice()
    {
        var deviceEvent = await WaitForActionAsync(DeviceAction.Added);

        Assert.NotNull(deviceEvent.Device);
        Assert.Equal(DeviceAction.Added, deviceEvent.Action);
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task WatchAsync_DetectsRemovedDevice()
    {
        // Device is populated for removals too, so the caller can correlate against the Added event.
        var deviceEvent = await WaitForActionAsync(DeviceAction.Removed);

        Assert.NotNull(deviceEvent.Device);
        Assert.Equal(DeviceAction.Removed, deviceEvent.Action);
    }

    /// <summary>
    /// Waits for the next device event of a given kind. This is the canonical "insert your YubiKey"
    /// pattern: begin the enumeration first, then prompt the user, and let the token bound the wait.
    /// </summary>
    private static async Task<DeviceEvent> WaitForActionAsync(DeviceAction action)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Enumerate first: WatchAsync subscribes on the first MoveNextAsync, so starting the
        // monitor beforehand could let the initial rescan's events land before anyone is listening.
        var events = YubiKeyManager.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        await using (events.ConfigureAwait(false))
        {
            var pending = events.MoveNextAsync();
            YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(1));

            while (await pending)
            {
                if (events.Current.Action == action)
                {
                    return events.Current;
                }

                pending = events.MoveNextAsync();
            }
        }

        throw new InvalidOperationException($"Device event stream ended before a {action} event arrived.");
    }
}