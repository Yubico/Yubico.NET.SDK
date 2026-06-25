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

using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Tests.Shared;

/// <summary>
///     SmartCard test connection that records transmitted APDUs and returns queued responses.
/// </summary>
/// <remarks>
///     Use this in unit tests that assert byte-level SmartCard command flow. It is not a hardware
///     integration-test abstraction and does not emulate APDU processing beyond queued responses.
/// </remarks>
public sealed class RecordingSmartCardConnection(params byte[][] responses) : ISmartCardConnection
{
    private readonly Queue<byte[]> _responses = new(responses);

    /// <summary>
    ///     Gets the commands transmitted through this connection, in order.
    /// </summary>
    public List<byte[]> TransmittedCommands { get; } = [];

    public Transport Transport { get; } = Transport.Usb;

    public ConnectionType Type { get; } = ConnectionType.SmartCard;

    public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ReadOnlyMemory<byte> command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TransmittedCommands.Add(command.ToArray());

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("No response enqueued for transmission.");
        }

        return Task.FromResult((ReadOnlyMemory<byte>)_responses.Dequeue());
    }

    public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => NullDisposable.Instance;

    public bool SupportsExtendedApdu() => false;

    public void Dispose()
    {
    }

    public ValueTask DisposeAsync() => default;

    private sealed class NullDisposable : IDisposable
    {
        public static NullDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}