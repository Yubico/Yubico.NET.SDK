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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

[Trait("Category", "RuntimeResilience")]
public class FindYubiKeysEnumerationBudgetTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task FindAllAsync_HungTransport_ReturnsOtherTransportAndIgnoresLateCompletion(
        bool hangPcsc, bool faultLater)
    {
        var pcsc = new PcscEnumerator();
        var hid = new HidEnumerator();
        var pendingPcsc = new TaskCompletionSource<IReadOnlyList<IPcscDevice>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingHid = new TaskCompletionSource<IReadOnlyList<IHidInterface>>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (hangPcsc)
            pcsc.Pending = pendingPcsc.Task;
        else
            hid.Pending = pendingHid.Task;

        // Zero expires an incomplete enumeration deterministically, without waiting five seconds.
        var finder = new FindYubiKeys(pcsc, hid, CreateSlot, enumerationBudget: TimeSpan.Zero);
        try
        {
            var first = await finder.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
            Assert.Equal(hangPcsc ? ConnectionType.HidOtp : ConnectionType.SmartCard,
                Assert.Single(first).AvailableConnections);
            Assert.False(hangPcsc ? pendingPcsc.Task.IsCompleted : pendingHid.Task.IsCompleted);

            if (faultLater)
            {
                var failure = new InvalidOperationException("Late enumeration failure.");
                if (hangPcsc)
                    pendingPcsc.SetException(failure);
                else
                    pendingHid.SetException(failure);
            }
            else if (hangPcsc)
                pendingPcsc.SetResult([]);
            else
                pendingHid.SetResult([]);

            pcsc.Pending = null;
            hid.Pending = null;
            var second = await finder.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(2, second.Count);
            Assert.Contains(second, key => key.AvailableConnections == ConnectionType.SmartCard);
            Assert.Contains(second, key => key.AvailableConnections == ConnectionType.HidOtp);
        }
        finally
        {
            pendingPcsc.TrySetResult([]);
            pendingHid.TrySetResult([]);
            finder.RetireReadScope();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MonitoredFindAllAsync_HungTransport_ThrowsInsteadOfReturningPartialScan(bool hangPcsc)
    {
        var pcsc = new PcscEnumerator();
        var hid = new HidEnumerator();
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (hangPcsc)
            pcsc.Pending = pending.Task.ContinueWith(_ => (IReadOnlyList<IPcscDevice>)[], TaskScheduler.Default);
        else
            hid.Pending = pending.Task.ContinueWith(_ => (IReadOnlyList<IHidInterface>)[], TaskScheduler.Default);

        var finder = new FindYubiKeys(pcsc, hid, CreateSlot, enumerationBudget: TimeSpan.Zero);
        IMonitoredDiscovery monitored = finder;
        try
        {
            // A monitor diffs against its previous snapshot, so a partial scan would read as removals.
            await Assert.ThrowsAsync<TimeoutException>(() => monitored.FindAllAsync(TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken));

            pending.SetResult();
            pcsc.Pending = null;
            hid.Pending = null;
            var recovered = await monitored.FindAllAsync(TestContext.Current.CancellationToken);
            Assert.Equal(2, recovered.Count);
        }
        finally
        {
            pending.TrySetResult();
            finder.RetireReadScope();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FindAllAsync_CancellationDuringHungEnumeration_ThrowsWithoutWaiting(bool hangPcsc)
    {
        var pcsc = new PcscEnumerator();
        var hid = new HidEnumerator();
        var pendingPcsc = new TaskCompletionSource<IReadOnlyList<IPcscDevice>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingHid = new TaskCompletionSource<IReadOnlyList<IHidInterface>>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (hangPcsc)
            pcsc.Pending = pendingPcsc.Task;
        else
            hid.Pending = pendingHid.Task;
        var finder = new FindYubiKeys(pcsc, hid, CreateSlot);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var scan = finder.FindAllAsync(cancellationToken: cancellation.Token);
        try
        {
            Assert.False(scan.IsCompleted);
            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                scan.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken));
            Assert.False(hangPcsc ? pendingPcsc.Task.IsCompleted : pendingHid.Task.IsCompleted);
        }
        finally
        {
            pendingPcsc.TrySetResult([]);
            pendingHid.TrySetResult([]);
            finder.RetireReadScope();
        }
    }

    private static IYubiKeyConnectionSlot CreateSlot(IDevice device) =>
        new UnavailableSlot($"enumeration-budget:{Guid.NewGuid():N}",
            device is IPcscDevice ? ConnectionType.SmartCard : ConnectionType.HidOtp);

    private sealed class UnavailableSlot(string interfaceId, ConnectionType connectionType) : IYubiKeyConnectionSlot
    {
        public string InterfaceId => interfaceId;
        public ConnectionType ConnectionType => connectionType;
        public Task<IConnection> OpenRawConnectionAsync(ConnectionType connectionType, CancellationToken cancellationToken) =>
            Task.FromException<IConnection>(new NotSupportedException("Metadata unavailable."));
    }

    private sealed class PcscEnumerator : IFindPcscDevices
    {
        internal Task<IReadOnlyList<IPcscDevice>>? Pending { get; set; }
        public Task<IReadOnlyList<IPcscDevice>> FindAllAsync(CancellationToken cancellationToken = default) =>
            Pending ?? Task.FromResult<IReadOnlyList<IPcscDevice>>([new PcscDevice()]);
    }

    private sealed class HidEnumerator : IFindHidInterfaces
    {
        internal Task<IReadOnlyList<IHidInterface>>? Pending { get; set; }
        public Task<IReadOnlyList<IHidInterface>> FindAllAsync(CancellationToken cancellationToken = default) =>
            Pending ?? Task.FromResult<IReadOnlyList<IHidInterface>>([new HidInterface()]);
    }

    private sealed class PcscDevice : IPcscDevice
    {
        public string ReaderName => "NFC reader";
        public AnswerToReset? Atr => null;
        public PscsConnectionKind Kind => PscsConnectionKind.Nfc;
    }

    private sealed class HidInterface : IHidInterface
    {
        public string ReaderName => "OTP interface";
        public HidDescriptorInfo DescriptorInfo { get; } = new() { VendorId = 0x1050, ProductId = 0x0401 };
        public HidInterfaceType InterfaceType => HidInterfaceType.Otp;
        public IHidConnection ConnectToFeatureReports() => throw new NotSupportedException();
        public IHidConnection ConnectToIOReports() => throw new NotSupportedException();
    }
}
