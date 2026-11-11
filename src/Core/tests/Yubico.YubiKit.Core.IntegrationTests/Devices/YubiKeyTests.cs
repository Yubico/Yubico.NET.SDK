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

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

/// <summary>
/// Hot-plug behaviour against real hardware. These double as the reference examples for how a
/// consumer waits for a YubiKey — no third-party dependency required.
/// </summary>
public class YubiKeyTests : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

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
        var deviceEvent = await WaitForChangeAsync<DeviceAdded>();

        Assert.NotNull(deviceEvent.Device);
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task WatchAsync_DetectsRemovedDevice()
    {
        // Device is populated for removals too, so the caller can correlate against the Added event.
        var deviceEvent = await WaitForChangeAsync<DeviceRemoved>();

        Assert.NotNull(deviceEvent.Device);
    }

    /// <summary>
    /// Waits for the next device event of a given kind. This is the canonical "insert your YubiKey"
    /// pattern: begin the enumeration first, then prompt the user, and let the token bound the wait.
    /// </summary>
    private static async Task<TChange> WaitForChangeAsync<TChange>() where TChange : DeviceChange
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // The owned stream atomically replays current membership, then follows changes.
        await using var monitor = await YubiKeyManager.CreateMonitorAsync(cts.Token);
        var events = monitor.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        await using (events.ConfigureAwait(false))
        {
            var pending = events.MoveNextAsync();

            while (await pending)
            {
                if (events.Current is TChange change)
                {
                    return change;
                }

                pending = events.MoveNextAsync();
            }
        }

        throw new InvalidOperationException($"Device change stream ended before a {typeof(TChange).Name} arrived.");
    }
}