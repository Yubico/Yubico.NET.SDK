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

using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.Hid.Fido;

/// <summary>
/// Wraps a synchronous HID IO report connection for FIDO/CTAP communication.
/// Provides async interface for 64-byte FIDO HID packets.
/// </summary>
internal class FidoHidConnection(IHidConnectionSync syncConnection) : IFidoHidConnection
{
    private bool _disposed;

    public int PacketSize => 64;
    public ConnectionType Type => ConnectionType.HidFido;

    public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (packet.Length != PacketSize)
            throw new ArgumentException($"FIDO packet must be exactly {PacketSize} bytes, got {packet.Length}",
                nameof(packet));

        syncConnection.SetReport(packet.ToArray());
        return Task.CompletedTask;
    }

    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var report = syncConnection.GetReport();
        if (report.Length != PacketSize)
            throw new InvalidOperationException($"Expected {PacketSize}-byte packet, got {report.Length} bytes");

        return Task.FromResult<ReadOnlyMemory<byte>>(report);
    }

    public void Dispose()
    {
        if (_disposed) return;

        syncConnection.Dispose();
        _disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
