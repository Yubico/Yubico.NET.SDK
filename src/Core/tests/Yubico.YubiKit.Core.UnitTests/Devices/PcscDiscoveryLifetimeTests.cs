// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class PcscDiscoveryLifetimeTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task BuiltInPcscDiscovery_BudgetExpiryQuarantinesThenLateReleaseAllowsConnection()
    {
        var api = new ControlledSCardConnectionApi();
        var pcscDevice = new PcscDevice
        {
            ReaderName = $"async-boundary-discovery-{Guid.NewGuid():N}",
            Atr = null
        };
        var factory = new SmartCardConnectionFactory(api);
        var slot = new PcscConnectionSlot(pcscDevice, factory);
        var device = new YubiKeyDevice(slot.InterfaceId, slot, hidFido: null, hidOtp: null, deviceInfo: null);

        try
        {
            _ = await Assert.ThrowsAsync<TimeoutException>(() => ProtocolDeviceInfo.ReadSlotBoundedAsync(
                slot,
                ConnectionType.SmartCard,
                TimeSpan.FromMilliseconds(100),
                NullLogger.Instance,
                Ct));

            _ = await Assert.ThrowsAsync<UnrecoveredConnectionException>(
                () => device.ConnectAsync<ISmartCardConnection>(Ct));

            var recovered = DeviceConnectionRegistry.WaitForRecoveryForTest(slot.InterfaceId);
            api.ReleaseTransmit.Set();
            await recovered.WaitAsync(Ct);

            await using var connection = await device.ConnectAsync<ISmartCardConnection>(Ct);
        }
        finally
        {
            api.ReleaseTransmit.Set();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CustomFactory_DiscoveryPreOpenFailure_ReleasesClaimAndAllowsRetry(bool cancellation)
    {
        Exception failure = cancellation
            ? new OperationCanceledException("custom factory canceled before returning a connection")
            : new SCardException("custom factory found no card", ErrorCode.SCARD_E_NO_SMARTCARD);
        var factory = new FailOnceSmartCardConnectionFactory(failure);
        var device = PcscTestDevices.Create(factory, out var slot);

        _ = await Assert.ThrowsAnyAsync<Exception>(() => ProtocolDeviceInfo.ReadBoundedAsync(
            device,
            ConnectionType.SmartCard,
            TimeSpan.FromSeconds(5),
            NullLogger.Instance,
            Ct));

        using var retry = DeviceConnectionRegistry.TryAcquireDiscovery(slot.InterfaceId);
        Assert.NotNull(retry);
    }

    [Fact]
    public Task BuiltInPcscDiscovery_UnprovenDisconnectRetainsOriginalClaimAndNativeRoot() =>
        RunProbe("disconnect", nameof(BuiltInPcscDiscovery_UnprovenDisconnectRetainsOriginalClaimAndNativeRoot));

    [Fact]
    public Task BuiltInPcscDiscovery_UnprovenContextReleaseRetainsOriginalClaimAndNativeRoot() =>
        RunProbe("context", nameof(BuiltInPcscDiscovery_UnprovenContextReleaseRetainsOriginalClaimAndNativeRoot));

    [Fact]
    public Task PublishedDevicePcscDiscovery_UnprovenDisconnectRetainsOriginalClaimAndNativeRoot() =>
        RunProbe(
            "published-disconnect",
            nameof(PublishedDevicePcscDiscovery_UnprovenDisconnectRetainsOriginalClaimAndNativeRoot));

    [Fact]
    public Task PublishedDevicePcscDiscovery_UnprovenContextReleaseRetainsOriginalClaimAndNativeRoot() =>
        RunProbe(
            "published-context",
            nameof(PublishedDevicePcscDiscovery_UnprovenContextReleaseRetainsOriginalClaimAndNativeRoot));

    [Fact]
    public Task PublishedDevicePcscDiscovery_BudgetExpiryLateNativeProofReleasesClaim() =>
        RunProbe(
            "published-late-proof",
            nameof(PublishedDevicePcscDiscovery_BudgetExpiryLateNativeProofReleasesClaim));

    private static Task RunProbe(string failure, string methodName) =>
        PcscIsolatedProbe.RunAsync(
            failure,
            typeof(PcscDiscoveryLifetimeTests),
            methodName,
            () => PcscCleanupFailureProbe.RunAsync(failure));
}