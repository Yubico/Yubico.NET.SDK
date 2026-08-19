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
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Transports.Hid;

namespace Yubico.YubiKit.Core.UnitTests.Protocols;

/// <summary>
///     A protocol handed a connection is a USER of it, never its owner: whoever created the connection
///     disposes it. These pin that rule for the HID protocols.
/// </summary>
/// <remarks>
///     <para>
///         This is not a stylistic preference — it is load-bearing for connection contention. The interface
///         lease belongs to the connection and is released on connection disposal, so a protocol that
///         disposes a connection it was merely handed releases that lease out from under its owner, and
///         breaks the borrow-versus-own split the session layer depends on.
///     </para>
///     <para>
///         <see cref="Devices.ConnectionOwnershipContractTests" /> already pins this for
///         <c>PcscProtocol</c>. The HID protocols had no equivalent, which made the rule silently
///         reversible for them — exactly the kind of one-line semantic regression a large merge can
///         introduce without turning a single test red. These close that gap.
///     </para>
/// </remarks>
public class ProtocolConnectionOwnershipTests
{
    [Fact]
    public void FidoHidProtocol_Dispose_DoesNotDisposeTheConnection()
    {
        var connection = new RecordingFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);

        protocol.Dispose();

        Assert.Equal(0, connection.DisposeCount);
    }

    [Fact]
    public void OtpHidProtocol_Dispose_DoesNotDisposeTheConnection()
    {
        var connection = new RecordingOtpHidConnection();
        var protocol = new OtpHidProtocol(connection);

        protocol.Dispose();

        Assert.Equal(0, connection.DisposeCount);
    }

    private sealed class RecordingFidoHidConnection : IFidoHidConnection
    {
        public int DisposeCount { get; private set; }

        public ConnectionType Type => ConnectionType.HidFido;
        public int PacketSize => 64;

        public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ReadOnlyMemory<byte>.Empty);

        public void Dispose() => DisposeCount++;

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingOtpHidConnection : IOtpHidConnection
    {
        public int DisposeCount { get; private set; }

        public ConnectionType Type => ConnectionType.HidOtp;
        public int FeatureReportSize => 8;

        public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ReadOnlyMemory<byte>.Empty);

        public void Dispose() => DisposeCount++;

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }
}