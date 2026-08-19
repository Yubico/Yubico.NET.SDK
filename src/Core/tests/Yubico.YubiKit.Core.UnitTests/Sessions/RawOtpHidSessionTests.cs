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

using System.Buffers.Binary;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;

namespace Yubico.YubiKit.Core.UnitTests.Sessions;

public class RawOtpHidSessionTests
{
    [Fact]
    public async Task SendAndReceiveAsync_UsesOtpFramingAndCrcWithoutOwningConnection()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var connection = new ScriptedOtpConnection();
        connection.Enqueue(Status(versionMajor: 5));
        connection.Enqueue(Status(programmingSequence: 1));
        for (int i = 0; i < 10; i++)
            connection.Enqueue(Status(programmingSequence: 1));
        connection.Enqueue(Status(programmingSequence: 2));

        await using var raw = await RawOtpHidSession.CreateAsync(connection, cancellationToken);
        ReadOnlyMemory<byte> response = await raw.SendAndReceiveAsync(
            0x13,
            new byte[] { 0xAA, 0xBB },
            cancellationToken);

        Assert.Equal(6, response.Length);
        Assert.Equal(10, connection.SentReports.Count);
        // The protocol writes its 70-byte frame as ten reports carrying seven frame bytes each.
        byte[] frame = connection.SentReports.SelectMany(report => report.AsSpan(0, 7).ToArray()).ToArray();
        Assert.Equal(70, frame.Length);
        Assert.Equal(0xAA, frame[0]);
        Assert.Equal(0xBB, frame[1]);
        Assert.Equal(0x13, frame[64]);
        Assert.Equal(
            ChecksumUtils.CalculateCrc(frame, 64),
            BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(65, 2)));

        await raw.DisposeAsync();
        Assert.Equal(0, connection.DisposeCount);
    }

    [Fact]
    public async Task SendAndReceiveAsync_OverlappingOperationThrowsThenSequentialCallSucceeds()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var connection = new ScriptedOtpConnection();
        connection.Enqueue(Status(versionMajor: 5));
        QueueStatusOnlyExchange(connection, 1);
        QueueStatusOnlyExchange(connection, 2);
        await using var raw = await RawOtpHidSession.CreateAsync(connection, cancellationToken);
        connection.HoldNextReceive();

        Task<ReadOnlyMemory<byte>> first = raw.SendAndReceiveAsync(
            0x13,
            ReadOnlyMemory<byte>.Empty,
            cancellationToken);
        await connection.ReceiveStarted.Task.WaitAsync(cancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() => raw.SendAndReceiveAsync(
            0x14,
            ReadOnlyMemory<byte>.Empty,
            cancellationToken));

        connection.ReleaseReceive();
        Assert.Equal(6, (await first).Length);
        Assert.Equal(6, (await raw.SendAndReceiveAsync(
            0x14,
            ReadOnlyMemory<byte>.Empty,
            cancellationToken)).Length);
    }

    [Fact]
    public async Task SendAndReceiveAsync_ReturnsRawResponseWithoutCommandSpecificCrcValidation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var connection = new ScriptedOtpConnection();
        connection.Enqueue(Status(versionMajor: 5));
        connection.Enqueue(Status(programmingSequence: 1));
        for (int i = 0; i < 10; i++)
            connection.Enqueue(Status(programmingSequence: 1));
        connection.Enqueue(new byte[] { 0xAA, 0xBB, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40 });
        connection.Enqueue(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40 });
        await using var raw = await RawOtpHidSession.CreateAsync(connection, cancellationToken);

        ReadOnlyMemory<byte> response = await raw.SendAndReceiveAsync(
            0x13,
            ReadOnlyMemory<byte>.Empty,
            cancellationToken);

        Assert.Equal(new byte[] { 0xAA, 0xBB, 0x00, 0x00, 0x00, 0x00, 0x00 }, response.ToArray());
        Assert.False(ChecksumUtils.CheckCrc(response.Span, length: 4));
    }

    private static void QueueStatusOnlyExchange(ScriptedOtpConnection connection, byte programmingSequence)
    {
        connection.Enqueue(Status(programmingSequence: programmingSequence));
        for (int i = 0; i < 10; i++)
            connection.Enqueue(Status(programmingSequence: programmingSequence));
        connection.Enqueue(Status(programmingSequence: (byte)(programmingSequence + 1)));
    }

    private static byte[] Status(byte versionMajor = 0, byte programmingSequence = 0) =>
        [0x00, versionMajor, 0x04, 0x03, programmingSequence, 0x00, 0x00, 0x00];

    private sealed class ScriptedOtpConnection : IOtpHidConnection
    {
        private readonly Queue<ReadOnlyMemory<byte>> _reports = new();
        private TaskCompletionSource? _receiveHold;

        public List<byte[]> SentReports { get; } = [];
        public int DisposeCount { get; private set; }
        public int FeatureReportSize => 8;
        public ConnectionType Type => ConnectionType.HidOtp;
        public TaskCompletionSource ReceiveStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Enqueue(ReadOnlyMemory<byte> report) => _reports.Enqueue(report);

        public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SentReports.Add(report.ToArray());
            return Task.CompletedTask;
        }

        public async Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TaskCompletionSource? hold = _receiveHold;
            if (hold is not null)
            {
                ReceiveStarted.TrySetResult();
                await hold.Task.WaitAsync(cancellationToken);
                _receiveHold = null;
            }

            return _reports.Dequeue();
        }

        public void HoldNextReceive() =>
            _receiveHold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReleaseReceive() => _receiveHold?.TrySetResult();

        public void Dispose() => DisposeCount++;
        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }
}