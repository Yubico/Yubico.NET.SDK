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

using System.Buffers.Binary;
using System.Reflection;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Oath.Backend;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Oath.UnitTests;

public sealed class OathTimeTests
{
    private static byte[] Select() => [0x79, 3, 5, 7, 0, 0x71, 1, 1, 0x90, 0];
    private static byte[] Truncated() => [0x76, 5, 6, 0, 0, 0, 1, 0x90, 0];
    private static Credential CredentialFor(string deviceId, OathType type = OathType.Totp, int period = 30) =>
        new(deviceId, "a"u8.ToArray(), null, "a", type, period, false);

    [Theory]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    [InlineData(9223372036854775800L)]
    public async Task Calculations_UnrepresentableDefaultWindow_RejectBeforeWire(long timestamp)
    {
        var connection = new RecordingSmartCardConnection(Select(), Truncated(), [0x90, 0]);
        await using var session = await OathSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => session.CalculateCodeAsync(
            CredentialFor(session.DeviceId), timestamp, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => session.CalculateAllAsync(timestamp, TestContext.Current.CancellationToken));
        Assert.Single(connection.TransmittedCommands); // Only the initialization SELECT.
    }

    [Theory]
    [InlineData(1, long.MaxValue)]
    [InlineData(32, 9223372036854775799L)]
    [InlineData(int.MaxValue, long.MaxValue)]
    public async Task CalculateCodeAsync_KnownCustomWindowOverflow_RejectsBeforeWire(int period, long timestamp)
    {
        var connection = new RecordingSmartCardConnection(Select(), Truncated());
        await using var session = await OathSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => session.CalculateCodeAsync(
            CredentialFor(session.DeviceId, period: period), timestamp, TestContext.Current.CancellationToken));
        Assert.Single(connection.TransmittedCommands);
    }

    [Fact]
    public async Task CalculateCodeAsync_LastRepresentableOneSecondWindow_DoesNotImposeDefaultPeriodBound()
    {
        var connection = new RecordingSmartCardConnection(Select(), Truncated());
        await using var session = await OathSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        var code = await session.CalculateCodeAsync(CredentialFor(session.DeviceId, period: 1),
            long.MaxValue - 1, TestContext.Current.CancellationToken);
        Assert.Equal(long.MaxValue - 1, code.ValidFrom);
        Assert.Equal(long.MaxValue, code.ValidTo);
    }

    [Theory]
    [InlineData(0L, 0L, 0L, 30L)]
    [InlineData(29L, 0L, 0L, 30L)]
    [InlineData(30L, 1L, 30L, 60L)]
    [InlineData(59L, 1L, 30L, 60L)]
    [InlineData(60L, 2L, 60L, 90L)]
    [InlineData(20000000000L, 666666666L, 19999999980L, 20000000010L)]
    [InlineData(300000000000L, 10000000000L, 300000000000L, 300000000030L)]
    [InlineData(9223372036854775799L, 307445734561825859L, 9223372036854775770L, 9223372036854775800L)]
    public async Task Calculations_ValidTimestamp_EncodeExactChallengeAndWindow(long timestamp, long step, long from, long to)
    {
        var connection = new RecordingSmartCardConnection(Select(), Truncated(),
            [0x71, 1, 0x61, .. Truncated()]);
        await using var session = await OathSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        var individual = await session.CalculateCodeAsync(CredentialFor(session.DeviceId), timestamp, TestContext.Current.CancellationToken);
        var aggregate = Assert.Single(await session.CalculateAllAsync(timestamp, TestContext.Current.CancellationToken));

        Assert.Equal(individual, aggregate.Value);
        Assert.Equal(from, individual.ValidFrom);
        Assert.Equal(to, individual.ValidTo);
        foreach (byte[] command in connection.TransmittedCommands.Skip(1))
        {
            using var tlvs = TlvHelper.DecodeList(command.AsSpan(5, command[4]));
            var challenge = Assert.Single(tlvs, tlv => tlv.Tag == 0x74).Value;
            Assert.Equal(8, challenge.Length);
            Assert.Equal(step, BinaryPrimitives.ReadInt64BigEndian(challenge.Span));
        }
    }

    [Theory]
    [InlineData(OathType.Totp, -1L, 30)]
    [InlineData(OathType.Totp, long.MaxValue, 30)]
    [InlineData(OathType.Totp, long.MaxValue, 1)]
    [InlineData(OathType.Hotp, -1L, 0)]
    public void FormatCode_InvalidTime_DoesNotReturnWrappedMetadata(OathType type, long timestamp, int period)
    {
        var credential = CredentialFor("test", type, period);
        Assert.Throws<ArgumentOutOfRangeException>(() => Code.FormatCode(credential, timestamp, new byte[] { 6, 0, 0, 0, 1 }));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(long.MaxValue)]
    public async Task CalculateCodeAsync_Hotp_TimestampIsOnlyMetadata(long timestamp)
    {
        var connection = new RecordingSmartCardConnection(Select(), Truncated());
        await using var session = await OathSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        var code = await session.CalculateCodeAsync(CredentialFor(session.DeviceId, OathType.Hotp, 0), timestamp, TestContext.Current.CancellationToken);
        Assert.Equal(timestamp, code.ValidFrom);
        Assert.Equal(long.MaxValue, code.ValidTo);
        byte[] command = connection.TransmittedCommands[1];
        using var tlvs = TlvHelper.DecodeList(command.AsSpan(5, command[4]));
        Assert.Empty(Assert.Single(tlvs, tlv => tlv.Tag == 0x74).Value.ToArray());
    }

    [Fact]
    public async Task CalculateCodeAsync_NegativeHotpTimestamp_RejectsBeforeAdvancingCounter()
    {
        var connection = new RecordingSmartCardConnection(Select(), Truncated());
        await using var session = await OathSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => session.CalculateCodeAsync(
            CredentialFor(session.DeviceId, OathType.Hotp, 0), -1, TestContext.Current.CancellationToken));
        Assert.Single(connection.TransmittedCommands);
    }

    [Fact]
    public async Task CalculateAllAsync_CustomWindowOverflow_ClearsResponseWithoutFollowup()
    {
        var backend = new AggregateBackend();
        await using var session = await OathSession.CreateAsync(new RecordingSmartCardConnection(Select()),
            cancellationToken: TestContext.Current.CancellationToken);
        var field = typeof(OathSession).GetField("_backend", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("OATH backend seam was not found.");
        field.SetValue(session, backend);

        // Fits the 30-second window, but not the 32-second window revealed by the response.
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => session.CalculateAllAsync(
            9223372036854775799L, TestContext.Current.CancellationToken));
        Assert.Equal(1, backend.SendCount);
        Assert.All(backend.Response.RawData.ToArray(), value => Assert.Equal(0, value));
    }

    private sealed class AggregateBackend : IOathBackend
    {
        public ApduResponse Response { get; } = new(new byte[] { 0x71, 4, 0x33, 0x32, 0x2F, 0x61, 0x76, 5, 6, 0, 0, 0, 1, 0x90, 0 });
        public int SendCount { get; private set; }
        public Task<OathInitialization> ReadInitialStateAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ApduResponse> SendAsync(ApduCommand command, bool throwOnError = true, CancellationToken cancellationToken = default)
        {
            SendCount++;
            return Task.FromResult(Response);
        }
    }
}