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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.UnitTests.Devices;
using Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.Sessions;

[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class RawSmartCardNativeBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task BorrowedRawSession_CancelDuringNativeTransmit_RefusesOverlapAndDrainsBeforeSequentialReuse()
    {
        var api = new ControlledSCardConnectionApi();
        await using ISmartCardConnection connection =
            await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var raw = await RawSmartCardSession.CreateAsync(connection, Ct);

        try
        {
            Task<ApduResponse> first = raw.TransmitAndReceiveAsync(
                new ApduCommand(0, 0xA1, 0, 0), cancellationToken: cancellation.Token);
            await api.FirstTransmitEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), Ct);
            await cancellation.CancelAsync();

            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => raw.TransmitAndReceiveAsync(
                new ApduCommand(0, 0xB2, 0, 0), cancellationToken: Ct));
            Assert.False(first.IsCompleted);
            Assert.Equal(1, api.TransmitCalls);

            Task disposal = raw.DisposeAsync().AsTask();
            Assert.False(disposal.IsCompleted);
            _ = await Assert.ThrowsAsync<ConnectionInUseException>(() =>
                RawSmartCardSession.CreateAsync(connection, Ct));
            Assert.Equal(0, api.DisconnectCalls);

            api.ReleaseTransmit.Set();
            Assert.True((await first.WaitAsync(TimeSpan.FromSeconds(5), Ct)).IsOK());
            await disposal.WaitAsync(TimeSpan.FromSeconds(5), Ct);
            Assert.Equal(0, api.DisconnectCalls);

            await using (RawSmartCardSession next = await RawSmartCardSession.CreateAsync(connection, Ct))
                Assert.True((await next.TransmitAndReceiveAsync(
                    new ApduCommand(0, 0xC3, 0, 0), cancellationToken: Ct)).IsOK());

            Assert.Equal(2, api.TransmitCalls);
        }
        finally
        {
            api.ReleaseTransmit.Set();
            await raw.DisposeAsync();
        }
    }

    [Fact]
    public async Task BorrowedRawSession_CancelDuringInitialNativeTransmit_DrainsResponseContinuationBeforeReuse()
    {
        var api = new ControlledSCardConnectionApi
        {
            CaptureTransmitBuffers = true,
            TransmitResult = call => call switch
            {
                1 => (ErrorCode.SCARD_S_SUCCESS, new byte[] { 0xDE, 0x61, 0x01 }),
                2 => (ErrorCode.SCARD_S_SUCCESS, new byte[] { 0xAD, 0x90, 0x00 }),
                3 => (ErrorCode.SCARD_S_SUCCESS, new byte[] { 0x90, 0x00 }),
                _ => throw new InvalidOperationException("Unexpected native transmit")
            }
        };
        await using ISmartCardConnection connection =
            await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        await using var raw = await RawSmartCardSession.CreateAsync(connection, Ct);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);

        try
        {
            Task<ApduResponse> first = raw.TransmitAndReceiveAsync(
                new ApduCommand(0, 0xA1, 0, 0), cancellationToken: cancellation.Token);
            await api.FirstTransmitEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), Ct);
            await cancellation.CancelAsync();
            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => raw.TransmitAndReceiveAsync(
                new ApduCommand(0, 0xB2, 0, 0), cancellationToken: Ct));
            Assert.Equal(1, api.TransmitCalls);

            api.ReleaseTransmit.Set();
            Assert.Equal(new byte[] { 0xDE, 0xAD },
                (await first.WaitAsync(TimeSpan.FromSeconds(5), Ct)).Data.ToArray());
            Assert.True((await raw.TransmitAndReceiveAsync(
                new ApduCommand(0, 0xC3, 0, 0), cancellationToken: Ct)).IsOK());
            Assert.Equal(3, api.TransmitCalls);
            Assert.Equal(new byte[] { 0x00, 0xA1, 0x00, 0x00, 0x00 }, api.TransmitBuffers[0]);
            Assert.Equal(new byte[] { 0x00, 0xC0, 0x00, 0x00, 0x00 }, api.TransmitBuffers[1]);
            Assert.Equal(new byte[] { 0x00, 0xC3, 0x00, 0x00, 0x00 }, api.TransmitBuffers[2]);
        }
        finally
        {
            api.ReleaseTransmit.Set();
        }
    }

    [Fact]
    public async Task BorrowedRawSession_PreCanceledExchange_DoesNotDispatchAndRemainsReusable()
    {
        var api = new ControlledSCardConnectionApi();
        api.ReleaseTransmit.Set();
        await using ISmartCardConnection connection =
            await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        await using var raw = await RawSmartCardSession.CreateAsync(connection, Ct);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => raw.TransmitAndReceiveAsync(
            new ApduCommand(0, 0xA1, 0, 0), cancellationToken: cancellation.Token));
        Assert.Equal(0, api.TransmitCalls);
        Assert.True((await raw.TransmitAndReceiveAsync(
            new ApduCommand(0, 0xB2, 0, 0), cancellationToken: Ct)).IsOK());
        Assert.Equal(1, api.TransmitCalls);
    }

    [Fact]
    public async Task BorrowedRawSession_FailedNativeResponseContinuation_RefusesSameSessionWithoutReplay()
    {
        var api = new ControlledSCardConnectionApi
        {
            CaptureTransmitBuffers = true,
            TransmitResult = call => call switch
            {
                1 => (ErrorCode.SCARD_S_SUCCESS, new byte[] { 0xDE, 0x61, 0x01 }),
                2 => (ErrorCode.SCARD_E_NOT_TRANSACTED, Array.Empty<byte>()),
                _ => throw new InvalidOperationException("Unexpected native transmit")
            }
        };
        api.ReleaseTransmit.Set();
        await using ISmartCardConnection connection =
            await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        await using var raw = await RawSmartCardSession.CreateAsync(connection, Ct);

        _ = await Assert.ThrowsAsync<SCardException>(() => raw.TransmitAndReceiveAsync(
            new ApduCommand(0, 0xA1, 0, 0), cancellationToken: Ct));
        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            raw.TransmitAndReceiveAsync(new ApduCommand(0, 0xB2, 0, 0), cancellationToken: Ct));
        Assert.Contains("reopen", refusal.Message, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<SCardException>(refusal.InnerException);
        Assert.Equal(2, api.TransmitCalls);
        Assert.Equal(new byte[] { 0x00, 0xA1, 0x00, 0x00, 0x00 }, api.TransmitBuffers[0]);
        Assert.Equal(new byte[] { 0x00, 0xC0, 0x00, 0x00, 0x00 }, api.TransmitBuffers[1]);
    }
}