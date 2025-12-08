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

using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.UnitTests.SmartCard.Fakes;

/// <summary>
///     Fake implementation of ISmartCardConnection for unit testing.
/// </summary>
internal sealed class FakeSmartCardConnection : ISmartCardConnection
{
    private readonly Queue<ReadOnlyMemory<byte>> _responses = new();
    private bool _disposed;

    public bool SupportsExtendedApduValue { get; set; } = true;
    public List<ReadOnlyMemory<byte>> TransmittedCommands { get; } = [];

    #region ISmartCardConnection Members

    public Transport Transport { get; set; } = Transport.Usb;

    public bool SupportsExtendedApdu() => SupportsExtendedApduValue;

    public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ReadOnlyMemory<byte> command,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FakeSmartCardConnection));

        cancellationToken.ThrowIfCancellationRequested();

        TransmittedCommands.Add(command);

        if (_responses.Count == 0)
            throw new InvalidOperationException("No response enqueued for transmission");

        return await Task.FromResult(_responses.Dequeue());
    }

    public void Dispose() => _disposed = true;

    #endregion

    public void EnqueueResponse(ReadOnlyMemory<byte> response) => _responses.Enqueue(response);
}