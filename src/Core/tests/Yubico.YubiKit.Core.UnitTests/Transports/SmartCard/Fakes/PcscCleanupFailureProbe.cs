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

namespace Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;

internal static class PcscCleanupFailureProbe
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static async Task RunAsync(string failure)
    {
        RecordingSCardContext.Reset();
        RecordingSCardCardHandle.Reset();
        var nativeFailure = failure switch
        {
            "published-disconnect" => "disconnect",
            "published-context" => "context",
            _ => failure
        };
        var api = new ControlledSCardConnectionApi
        {
            DisconnectResult = nativeFailure == "disconnect"
                ? ErrorCode.SCARD_E_NOT_TRANSACTED
                : ErrorCode.SCARD_S_SUCCESS,
            ReleaseContextResult = nativeFailure is "context" or "delegating-partial-open"
                ? ErrorCode.SCARD_E_NOT_TRANSACTED
                : ErrorCode.SCARD_S_SUCCESS
        };
        if (failure != "published-late-proof")
            api.ReleaseTransmit.Set();
        var pcscDevice = new PcscDevice
        {
            ReaderName = $"async-boundary-cleanup-{failure}-{Guid.NewGuid():N}",
            Atr = null
        };
        ISmartCardConnectionFactory factory = failure == "delegating-partial-open"
            ? new DelegatingSmartCardConnectionFactory(
                new SmartCardConnectionFactory(api))
            : new SmartCardConnectionFactory(api);
        var slot = new PcscConnectionSlot(pcscDevice, factory);
        var device = new YubiKeyDevice(slot.InterfaceId, slot, hidFido: null, hidOtp: null, deviceInfo: null);

        if (failure == "delegating-partial-open")
        {
            api.FailNextConnect = true;
            _ = await Assert.ThrowsAsync<UnrecoveredConnectionException>(
                () => device.ConnectAsync<ISmartCardConnection>(Ct));
        }
        else
        {
            var read = failure.StartsWith("published-", StringComparison.Ordinal)
                ? ProtocolDeviceInfo.ReadBoundedAsync(
                    device,
                    ConnectionType.SmartCard,
                    failure == "published-late-proof" ? TimeSpan.FromMilliseconds(100) : TimeSpan.FromSeconds(5),
                    NullLogger.Instance,
                    Ct)
                : ProtocolDeviceInfo.ReadSlotBoundedAsync(
                    slot,
                    ConnectionType.SmartCard,
                    TimeSpan.FromSeconds(5),
                    NullLogger.Instance,
                    Ct);
            _ = await Assert.ThrowsAnyAsync<Exception>(() => read);
        }

        if (failure == "published-late-proof")
        {
            _ = await Assert.ThrowsAsync<UnrecoveredConnectionException>(
                () => device.ConnectAsync<ISmartCardConnection>(Ct));
            var recovered = DeviceConnectionRegistry.WaitForRecoveryForTest(slot.InterfaceId);
            api.ReleaseTransmit.Set();
            await recovered.WaitAsync(Ct);
            await using var reopened = await device.ConnectAsync<ISmartCardConnection>(Ct);
            return;
        }

        Assert.Equal(failure == "delegating-partial-open" ? 0 : 1, api.DisconnectCalls);
        Assert.Equal(nativeFailure == "disconnect" ? 0 : 1, api.ReleaseContextCalls);
        _ = await Assert.ThrowsAsync<UnrecoveredConnectionException>(
            () => device.ConnectAsync<ISmartCardConnection>(Ct));

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.True(api.ContextReference?.IsAlive);
        if (failure != "delegating-partial-open")
            Assert.True(api.CardReference?.IsAlive);
        Assert.Equal(0, RecordingSCardContext.ReleaseHandleCalls);
        Assert.Equal(0, RecordingSCardCardHandle.ReleaseHandleCalls);
    }
}