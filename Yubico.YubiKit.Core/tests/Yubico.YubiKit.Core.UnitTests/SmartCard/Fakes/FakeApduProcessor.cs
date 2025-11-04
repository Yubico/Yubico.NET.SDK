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

namespace Yubico.YubiKit.Core.UnitTests.SmartCard.Fakes;

/// <summary>
///     Fake implementation of IApduProcessor for unit testing.
/// </summary>
internal class FakeApduProcessor : IApduProcessor
{
    private readonly Queue<ResponseApdu> _responses = new();

    public IApduFormatter Formatter { get; set; } = new FakeApduFormatter();
    public List<CommandApdu> TransmittedCommands { get; } = [];

    public void EnqueueResponse(ResponseApdu response)
    {
        _responses.Enqueue(response);
    }

    public void EnqueueResponse(byte sw1, byte sw2, ReadOnlyMemory<byte> data = default)
    {
        var responseBytes = new byte[data.Length + 2];
        data.Span.CopyTo(responseBytes);
        responseBytes[^2] = sw1;
        responseBytes[^1] = sw2;
        _responses.Enqueue(new ResponseApdu(responseBytes));
    }

    public async Task<ResponseApdu> TransmitAsync(
        CommandApdu command,
        bool useScp = true,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TransmittedCommands.Add(command);

        if (_responses.Count == 0)
            throw new InvalidOperationException("No response enqueued for transmission");

        return await Task.FromResult(_responses.Dequeue());
    }
}

/// <summary>
///     Fake implementation of IApduFormatter for testing.
/// </summary>
internal class FakeApduFormatter : IApduFormatter
{
    public ReadOnlyMemory<byte> Format(byte cla, byte ins, byte p1, byte p2, ReadOnlyMemory<byte> data, int le)
    {
        var buffer = new List<byte> { cla, ins, p1, p2 };

        if (data.Length > 0)
        {
            buffer.Add((byte)data.Length);
            buffer.AddRange(data.ToArray());
        }

        if (le > 0)
            buffer.Add((byte)le);

        return buffer.ToArray();
    }

    public ReadOnlyMemory<byte> Format(CommandApdu apdu)
    {
        return Format(apdu.Cla, apdu.Ins, apdu.P1, apdu.P2, apdu.Data, apdu.Le);
    }
}
