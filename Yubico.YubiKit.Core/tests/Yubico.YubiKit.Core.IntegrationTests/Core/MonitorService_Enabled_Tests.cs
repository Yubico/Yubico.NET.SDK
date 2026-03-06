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

using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.IntegrationTests.Core;

/// <summary>
/// Tests that verify behavior when monitoring is enabled.
/// </summary>
public class MonitorService_Enabled_Tests : IAsyncLifetime
{
    public Task InitializeAsync()
    {
        YubiKeyManager.StartMonitoring();
        return Task.CompletedTask;
    }
    
    public async Task DisposeAsync() => await YubiKeyManager.ShutdownAsync();
    
    [Fact]
    public async Task WhenMonitoringEnabled_FindsDevices()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.All);
        Assert.NotEmpty(devices);
    }
    
    [Fact]
    public void WhenMonitoringEnabled_IsMonitoringReturnsTrue()
    {
        Assert.True(YubiKeyManager.IsMonitoring);
    }
}