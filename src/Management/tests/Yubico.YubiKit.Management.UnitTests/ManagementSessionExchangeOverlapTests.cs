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

namespace Yubico.YubiKit.Management.UnitTests;

using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;

/// <summary>
///     Pins the overlap contract on the public Management route: the protocol guard refuses a second
///     operation whose exchange starts while another exchange is admitted. It does not make a
///     multi-exchange operation atomic; see docs/architecture/connection-ownership-and-contention.md.
/// </summary>
public class ManagementSessionExchangeOverlapTests
{
    private const byte InsSelect = 0xA4;
    private const byte InsGetDeviceInfo = 0x1D;
    private const int SerialNumber = 0x01020304;

    private static readonly TimeSpan ObservationWindow = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan CompletionBound = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task GetDeviceInfoAsync_StartedWhileAnotherPageExchangeIsAdmitted_IsRefusedWithoutDisturbingIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = new HoldingDeviceInfoConnection();
        await using var session = await ManagementSession.CreateAsync(connection, cancellationToken: ct);
        connection.ClearWire();

        // Operation A's page-0 exchange is admitted and held on the wire; page 1 is still owed.
        connection.HoldTransmits();
        try
        {
            var operationA = session.GetDeviceInfoAsync(ct);
            Assert.True(await connection.WaitForArrivalsAsync(1, ObservationWindow, ct));

            var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => session.GetDeviceInfoAsync(ct));
            Assert.Contains("one operation at a time", refusal.Message, StringComparison.Ordinal);
            Assert.False(await connection.WaitForArrivalsAsync(2, ObservationWindow, ct));

            connection.ReleaseTransmits();
            var infoA = await operationA.WaitAsync(CompletionBound, ct);
            Assert.Equal(SerialNumber, infoA.SerialNumber);

            // The refused call put nothing on the wire: A's two page reads are the only traffic.
            Assert.Equal([(InsGetDeviceInfo, (byte)0), (InsGetDeviceInfo, (byte)1)], connection.Wire);

            // The refusal left the session reusable for a properly sequenced call.
            var infoB = await session.GetDeviceInfoAsync(ct).WaitAsync(CompletionBound, ct);
            Assert.Equal(SerialNumber, infoB.SerialNumber);
        }
        finally
        {
            // Idempotent. A failed assertion above must still free the held exchange, or session disposal
            // would drain it forever.
            connection.ReleaseTransmits();
        }
    }

    /// <summary>
    ///     A sequential Management applet that answers SELECT and a two-page GET DEVICE INFO, records every
    ///     APDU's INS/P1 in wire order, and can hold transmits in flight.
    /// </summary>
    private sealed class HoldingDeviceInfoConnection : ISmartCardConnection
    {
        private static readonly byte[] Success = [0x90, 0x00];

        private readonly SemaphoreSlim _arrivals = new(0);
        private readonly List<(byte Ins, byte P1)> _wire = [];
        private volatile TaskCompletionSource? _hold;

        public Transport Transport => Transport.Usb;

        public ConnectionType Type => ConnectionType.SmartCard;

        public List<(byte Ins, byte P1)> Wire
        {
            get
            {
                lock (_wire)
                    return [.. _wire];
            }
        }

        public void HoldTransmits() =>
            _hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReleaseTransmits()
        {
            var hold = _hold;
            _hold = null;
            hold?.SetResult();
        }

        public void ClearWire()
        {
            lock (_wire)
                _wire.Clear();

            while (_arrivals.Wait(0))
            {
            }
        }

        public async Task<bool> WaitForArrivalsAsync(int count, TimeSpan timeout, CancellationToken ct)
        {
            while (true)
            {
                lock (_wire)
                {
                    if (_wire.Count >= count)
                        return true;
                }

                if (!await _arrivals.WaitAsync(timeout, ct).ConfigureAwait(false))
                    return false;
            }
        }

        public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            var ins = command.Span[1];
            var p1 = command.Span[2];
            lock (_wire)
                _wire.Add((ins, p1));

            _arrivals.Release();

            var hold = _hold;
            if (hold is not null)
                await hold.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

            return (ins, p1) switch
            {
                (InsSelect, _) => Success,
                (InsGetDeviceInfo, 0) => Page([0x10, 0x01, 0x01]),
                (InsGetDeviceInfo, 1) => Page(
                [
                    0x0A, 0x01, 0x00,
                    0x04, 0x01, 0x01,
                    0x18, 0x01, 0x00,
                    0x03, 0x02, 0x00, 0x01,
                    0x01, 0x02, 0x00, 0x01,
                    0x0E, 0x01, 0x00,
                    0x0D, 0x01, 0x00,
                    0x14, 0x01, 0x00,
                    0x15, 0x01, 0x00,
                    0x06, 0x02, 0x00, 0x00,
                    0x07, 0x01, 0x0F,
                    0x08, 0x01, 0x00,
                    0x05, 0x03, 0x05, 0x07, 0x02,
                    0x02, 0x04, 0x01, 0x02, 0x03, 0x04
                ]),
                _ => throw new InvalidOperationException($"Unexpected APDU INS 0x{ins:X2} P1 0x{p1:X2}")
            };
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool SupportsExtendedApdu() => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private static byte[] Page(byte[] tlvs) => [(byte)tlvs.Length, .. tlvs, .. Success];
    }
}