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

using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.IntegrationTests.Core;

public class YubiKeyTests
{
    [Fact]
    public async Task FindAllAsync_ReturnsAtLeastOne()
    {
        var devices = await YubiKey.FindAllAsync();
        Assert.NotEmpty(devices);
    }

    [Fact]
    public async Task MonitorAsync_ReturnsAtLeastOne()
    {
        var insertedDevice = false;
        var removedDevice = false;
        await foreach (var change in YubiKey.MonitorAsync(TimeSpan.FromSeconds(1), CancellationToken.None))
            if (change.Action == DeviceAction.Added)
            {
                Assert.NotNull(change.Device);
                insertedDevice = true;
                break;
            }

        await foreach (var change in YubiKey.MonitorAsync(TimeSpan.FromSeconds(1), CancellationToken.None))
            if (change.Action == DeviceAction.Removed)
            {
                Assert.Null(change.Device);
                removedDevice = true;
                break;
            }

        Assert.True(insertedDevice);
        Assert.True(removedDevice);
        Thread.Sleep(TimeSpan.FromSeconds(5));
    }
}