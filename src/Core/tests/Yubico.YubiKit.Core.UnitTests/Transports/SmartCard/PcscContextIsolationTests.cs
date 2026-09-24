// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
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

using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.Transports.SmartCard;

public sealed class PcscContextIsolationTests
{
    [Fact]
    public async Task ListenerDisposal_CancelsOnlyMonitorContext_WhileConnectionTransmits()
    {
        using var monitorApi = new MonitoringApi();
        var connectionApi = new ControlledSCardConnectionApi();
        using var listener = new DesktopSmartCardDeviceListener(monitorApi, _ => { });
        UsbSmartCardConnection? connection = null;

        try
        {
            listener.Start();
            Assert.Equal(DeviceListenerStatus.Started, listener.Status);
            var monitoredHandle = await monitorApi.MonitorEntered.Task.WaitAsync(TestContext.Current.CancellationToken);

            connection = new UsbSmartCardConnection(new Reader(), connectionApi);
            await connection.InitializeAsync(TestContext.Current.CancellationToken);
            var transmit = connection.TransmitAndReceiveAsync(new byte[] { 0x00 }, TestContext.Current.CancellationToken);
            await connectionApi.FirstTransmitEntered.Task.WaitAsync(TestContext.Current.CancellationToken);

            // Stop and dispose while the other context is still borrowed by native transmit.
            listener.Dispose();
            Assert.Equal(monitorApi.EstablishedHandle, monitoredHandle);
            Assert.Equal(monitorApi.EstablishedHandle, Assert.Single(monitorApi.CanceledHandles));
            Assert.Equal(monitorApi.EstablishedHandle, Assert.Single(monitorApi.ReleasedHandles));
            Assert.NotEqual(monitorApi.EstablishedHandle, connectionApi.EstablishedContextHandle);
            Assert.Equal(connectionApi.EstablishedContextHandle, connectionApi.ConnectedContextHandle);
            Assert.False(transmit.IsCompleted);
            Assert.Equal(0, connectionApi.ReleaseContextCalls);
            Assert.Equal(0, connectionApi.DisconnectCalls);

            connectionApi.ReleaseTransmit.Set();
            Assert.Equal(new byte[] { 0x90, 0x00 }, (await transmit).ToArray());
            await connection.DisposeAsync();
            Assert.Equal(1, connectionApi.ReleaseContextCalls);
            Assert.Equal(connectionApi.EstablishedContextHandle, connectionApi.ReleasedContextHandle);
            Assert.Equal(1, connectionApi.DisconnectCalls);
            Assert.Equal(monitorApi.EstablishedHandle, Assert.Single(monitorApi.ReleasedHandles));
        }
        finally
        {
            monitorApi.Unblock();
            connectionApi.ReleaseTransmit.Set();
            listener.Dispose();
            if (connection is not null)
                await connection.DisposeAsync();
        }
    }

    private sealed class Reader : IPcscDevice
    {
        public string ReaderName => "fake reader";
        public AnswerToReset? Atr => null;
        public PscsConnectionKind Kind => PscsConnectionKind.Usb;
    }

    private sealed class MonitoringApi : ISCardApi, IDisposable
    {
        private readonly ManualResetEventSlim _unblock = new();
        private readonly List<nint> _canceled = [];
        private readonly List<nint> _released = [];
        private const int MonitorAddress = 0x39A;

        public TaskCompletionSource<nint> MonitorEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public nint EstablishedHandle { get; private set; }
        public nint[] CanceledHandles
        {
            get { lock (_canceled) return [.. _canceled]; }
        }

        public nint[] ReleasedHandles
        {
            get { lock (_released) return [.. _released]; }
        }

        public uint SCardEstablishContext(SCARD_SCOPE scope, out SCardContext context)
        {
            context = new MonitoringContext((nint)MonitorAddress, this);
            EstablishedHandle = context.DangerousGetHandle();
            return ErrorCode.SCARD_S_SUCCESS;
        }

        public uint SCardListReaders(SCardContext context, string[]? groups, out string[] readerNames)
        {
            readerNames = [];
            return ErrorCode.SCARD_E_NO_READERS_AVAILABLE;
        }

        public uint SCardGetStatusChange(SCardContext context, int timeout, SCARD_READER_STATE[] readerStates, int readerStatesCount)
        {
            MonitorEntered.TrySetResult(context.DangerousGetHandle());
            if (!_unblock.Wait(TimeSpan.FromSeconds(5)))
                throw new TimeoutException("The monitor cancel did not unblock the fake status change.");
            return ErrorCode.SCARD_E_CANCELLED;
        }

        public uint SCardCancel(SCardContext context)
        {
            lock (_canceled) _canceled.Add(context.DangerousGetHandle());
            _unblock.Set();
            return ErrorCode.SCARD_S_SUCCESS;
        }

        public void Unblock() => _unblock.Set();

        public void Dispose()
        {
            _unblock.Set();
            _unblock.Dispose();
        }

        private sealed class MonitoringContext(nint handle, MonitoringApi owner) : SCardContext(handle)
        {
            protected override bool ReleaseHandle()
            {
                lock (owner._released) owner._released.Add(DangerousGetHandle());
                return true;
            }
        }
    }
}