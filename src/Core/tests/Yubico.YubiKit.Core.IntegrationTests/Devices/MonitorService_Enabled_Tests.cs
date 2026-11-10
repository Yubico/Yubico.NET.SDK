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
/// Tests that verify behavior when monitoring is enabled.
/// </summary>
public class MonitorService_Enabled_Tests : IAsyncLifetime
{
    private YubiKeyMonitor? _monitor;

    public async Task InitializeAsync() => _monitor = await YubiKeyManager.CreateMonitorAsync();

    public async Task DisposeAsync()
    {
        if (_monitor is not null) await _monitor.DisposeAsync();
    }

    [Fact]
    public async Task WhenMonitoringEnabled_FindsDevices()
    {
        var monitor = Assert.IsType<YubiKeyMonitor>(_monitor);
        var devices = await MonitorSnapshot.ReadAsync(monitor, CancellationToken.None);
        Assert.NotEmpty(devices);
    }

}