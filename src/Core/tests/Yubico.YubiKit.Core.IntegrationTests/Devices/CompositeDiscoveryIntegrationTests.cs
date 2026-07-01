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
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

/// <summary>
///     Safe hardware smoke for composite discovery (Phase 37 / Phase 37.5). Requires a single composite USB
///     YubiKey (OTP+FIDO+CCID) that is allow-listed; no touch / user-presence required.
/// </summary>
public class CompositeDiscoveryIntegrationTests : IAsyncLifetime
{
    private const ConnectionType FullKey =
        ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await YubiKeyManager.ShutdownAsync();

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task FindAllAsync_MergesAllInterfacesByPid_WithoutRequiringConnections()
    {
        // Phase 37.5: a single physical key's CCID + FIDO + OTP interfaces merge into one device by USB PID,
        // with no connection required for grouping — so the CCID is included even if another process
        // (e.g. GnuPG scdaemon) holds it exclusively. This asserts only the discovery result; it does not
        // depend on opening the CCID.
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: true);

        var device = Assert.Single(devices);
        Assert.Equal(FullKey, device.AvailableConnections);
        Assert.StartsWith("ykphysical:", device.DeviceId);
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task FindAllAsync_CompositeUsbKey_ReturnsOneMergedDevice()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: true);

        var device = Assert.Single(devices);
        Assert.Equal(FullKey, device.AvailableConnections);

        // The merged device is the physical key: device info is reachable over its preferred transport.
        var info = await device.GetDeviceInfoAsync();
        Assert.True(info.SerialNumber > 0);
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task FindAllAsync_PerConnectionFilters_ReturnTheSamePhysicalDevice()
    {
        var all = Assert.Single(await YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: true));
        var smartCard = Assert.Single(await YubiKeyManager.FindAllAsync(ConnectionType.SmartCard));
        var fido = Assert.Single(await YubiKeyManager.FindAllAsync(ConnectionType.HidFido));
        var otp = Assert.Single(await YubiKeyManager.FindAllAsync(ConnectionType.HidOtp));

        Assert.Equal(all.DeviceId, smartCard.DeviceId);
        Assert.Equal(all.DeviceId, fido.DeviceId);
        Assert.Equal(all.DeviceId, otp.DeviceId);
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task ConnectAsync_TypedTransports_OnMergedDevice_Succeed()
    {
        var device = Assert.Single(await YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: true));

        await using (var smartCard = await device.ConnectAsync<ISmartCardConnection>())
            Assert.NotNull(smartCard);

        await using (var fido = await device.ConnectAsync<IFidoHidConnection>())
            Assert.NotNull(fido);

        await using (var otp = await device.ConnectAsync<IOtpHidConnection>())
            Assert.NotNull(otp);
    }
}