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

namespace Yubico.YubiKit.IntegrationTests.Core;

public class MonitorService_Disabled_Tests()
    : IntegrationTestBase(options => options.EnableAutoDiscovery = false)
{
    [Fact]
    public async Task WhenDisabledMonitor_FindsDevices()
    {
        var devices = await YubiKeyManager.GetYubiKeysAsync();
        Assert.NotEmpty(devices);
    }

    [Fact]
    public async Task WhenDisabledMonitor_WithDisabledManualScan_DoesNotFindDevices()
    {
        SkipDeviceRepositoryManualScan(true);
        var devices = await YubiKeyManager.GetYubiKeysAsync();
        Assert.Empty(devices);
    }
}