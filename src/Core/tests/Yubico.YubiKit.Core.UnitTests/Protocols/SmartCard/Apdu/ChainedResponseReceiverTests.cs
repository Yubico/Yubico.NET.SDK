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

using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Apdu.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Apdu;

public class ChainedResponseReceiverTests
{
    private const byte RemainingInstruction = 0xA5;

    [Fact]
    public async Task TransmitAsync_ChainedResponse_ClearsFragmentsAndReturnsIndependentResult()
    {
        var ct = TestContext.Current.CancellationToken;
        byte[] firstSource = [0xA1, 0x61, 0x02];
        byte[] secondSource = [0xB2, 0x61, 0x01];
        byte[] terminalSource = [0xC3, 0x62, 0x83];
        var processor = new FakeApduProcessor();
        var firstView = EnqueueFragment(processor, firstSource);
        var secondView = EnqueueFragment(processor, secondSource);
        var terminalView = EnqueueFragment(processor, terminalSource);
        using var receiver = new ChainedResponseReceiver(null, processor, RemainingInstruction);
        var command = new ApduCommand(0x84, 0xCA, 0x12, 0x34, le: 5);
        var remaining = new ApduCommand(0, RemainingInstruction, 0, 0);

        var result = await receiver.TransmitAsync(command, useScp: true, cancellationToken: ct);

        Assert.Equal<byte>([0xA1, 0xB2, 0xC3, 0x62, 0x83], result.RawData.ToArray());
        Assert.Equal<ApduCommand>([command, remaining, remaining], processor.TransmittedCommands);
        Assert.All(processor.TransmitOptions, options => Assert.Equal((true, ct), options));
        Assert.Equal<byte>([0xA1, 0x61, 0x02], firstSource);
        Assert.Equal<byte>([0xB2, 0x61, 0x01], secondSource);
        Assert.Equal<byte>([0xC3, 0x62, 0x83], terminalSource);
        Assert.Equal<byte>([0, 0, 0], firstView.ToArray());
        Assert.Equal<byte>([0, 0, 0], secondView.ToArray());
        Assert.Equal<byte>([0, 0, 0], terminalView.ToArray());
    }

    [Fact]
    public async Task TransmitAsync_FailedContinuation_ClearsConsumedFragmentsAndReportsOriginalFailure()
    {
        var processor = new FakeApduProcessor();
        var firstView = EnqueueFragment(processor, [0xA1, 0x61, 0x02]);
        var secondView = EnqueueFragment(processor, [0xB2, 0x61, 0x01]);
        var failure = new IOException("GET RESPONSE failed");
        processor.EnqueueResponse(() => throw failure);
        var notifications = new List<Exception>();
        using var receiver = new ChainedResponseReceiver(null, processor, RemainingInstruction, notifications.Add);

        var error = await Assert.ThrowsAsync<IOException>(() =>
            receiver.TransmitAsync(new ApduCommand(0, 0xCA, 0, 0), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Same(failure, error);
        Assert.Same(failure, Assert.Single(notifications));
        Assert.Equal(3, processor.TransmittedCommands.Count);
        Assert.Equal<byte>([0, 0, 0], firstView.ToArray());
        Assert.Equal<byte>([0, 0, 0], secondView.ToArray());
    }

    [Fact]
    public async Task TransmitAsync_SingleResponse_HandsOriginalOwnerToCallerIntact()
    {
        var processor = new FakeApduProcessor();
        var single = new ApduResponse(new byte[] { 0xA1, 0x90, 0x00 });
        processor.EnqueueResponse(single);
        using var receiver = new ChainedResponseReceiver(null, processor, RemainingInstruction);

        var result = await receiver.TransmitAsync(new ApduCommand(0, 0xCA, 0, 0), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Same(single, result);
        Assert.Equal<byte>([0xA1, 0x90, 0x00], result.RawData.ToArray());
        Assert.Single(processor.TransmittedCommands);
    }

    private static ReadOnlyMemory<byte> EnqueueFragment(FakeApduProcessor processor, byte[] source)
    {
        var fragment = new ApduResponse(source);
        processor.EnqueueResponse(fragment);
        return fragment.RawData;
    }
}