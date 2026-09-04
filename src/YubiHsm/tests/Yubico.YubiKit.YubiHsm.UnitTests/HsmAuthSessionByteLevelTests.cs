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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Tests.Shared;

using Yubico.YubiKit.Core.Sessions;
namespace Yubico.YubiKit.YubiHsm.UnitTests;

public class HsmAuthSessionByteLevelTests
{
    [Fact]
    public async Task CreateAsync_AppletProbeFailure_DoesNotDisposeTheBorrowedConnection()
    {
        var connection = new RecordingSmartCardConnection();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            HsmAuthSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken));

        // Borrowed: the session did not create this connection, so disposal is the caller's.
        // Upstream asserted 1 here because its protocols disposed the connection; this branch
        // deliberately removed that (see ProtocolConnectionOwnershipTests).
        Assert.Equal(0, connection.DisposeCount);
    }

    [Fact]
    public async Task PutCredentialSymmetricAsync_TransmitsOrderedCredentialTlvs()
    {
        var connection = CreateInitializedConnection(OkResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 4, 3) },
            cancellationToken: TestContext.Current.CancellationToken);

        await session.PutCredentialSymmetricAsync(
            Sequence(0x10, 16),
            "cred",
            Sequence(0x20, 16),
            Sequence(0x30, 16),
            "pass"u8.ToArray(),
            touchRequired: true,
            TestContext.Current.CancellationToken);

        var command = LastCommand(connection);
        // APDU header: INS=PUT, P1/P2 default to 0 for YubiHSM Auth credential storage.
        Assert.Equal(0x01, command[1]);
        Assert.Equal(0x00, command[2]);
        Assert.Equal(0x00, command[3]);
        // Data: management key(7B), label(71), algorithm(74), K-ENC(75), K-MAC(76), password(73), touch(7A).
        Assert.Equal([
            0x7B, 0x10, .. Sequence(0x10, 16),
            0x71, 0x04, (byte)'c', (byte)'r', (byte)'e', (byte)'d',
            0x74, 0x01, (byte)HsmAuthAlgorithm.Aes128YubicoAuthentication,
            0x75, 0x10, .. Sequence(0x20, 16),
            0x76, 0x10, .. Sequence(0x30, 16),
            0x73, 0x10, (byte)'p', (byte)'a', (byte)'s', (byte)'s', .. new byte[12],
            0x7A, 0x01, 0x01
        ], CommandData(command).ToArray());
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_TransmitsOrderedCalculateTlvs()
    {
        var connection = CreateInitializedConnection(SessionKeyResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 4, 3) },
            cancellationToken: TestContext.Current.CancellationToken);

        using var keys = await session.CalculateSessionKeysSymmetricAsync(
            "cred",
            Sequence(0x40, 16),
            "pass"u8.ToArray(),
            Sequence(0x50, 8),
            TestContext.Current.CancellationToken);

        Assert.Equal(Sequence(0xA0, 16), keys.SEnc.ToArray());
        var command = LastCommand(connection);
        // APDU header: INS=CALCULATE with default P1/P2.
        Assert.Equal(0x03, command[1]);
        Assert.Equal(0x00, command[2]);
        Assert.Equal(0x00, command[3]);
        // Data: label(71), context(77), card cryptogram response(78), credential password(73).
        Assert.Equal([
            0x71, 0x04, (byte)'c', (byte)'r', (byte)'e', (byte)'d',
            0x77, 0x10, .. Sequence(0x40, 16),
            0x78, 0x08, .. Sequence(0x50, 8),
            0x73, 0x10, (byte)'p', (byte)'a', (byte)'s', (byte)'s', .. new byte[12]
        ], CommandData(command).ToArray());
    }

    [Theory]
    [InlineData(15)]
    [InlineData(17)]
    public async Task CalculateSessionKeysSymmetricAsync_InvalidContextLength_ThrowsBeforeDeviceIo(int length)
    {
        var connection = CreateInitializedConnection(SessionKeyResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 4, 3) },
            cancellationToken: TestContext.Current.CancellationToken);
        var commandCount = connection.TransmittedCommands.Count;
        var touchCallbackInvoked = false;
        session.OnTouchRequired = () => touchCallbackInvoked = true;

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            session.CalculateSessionKeysSymmetricAsync(
                "cred",
                new byte[length],
                "pass"u8.ToArray(),
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("context", exception.ParamName);
        Assert.False(touchCallbackInvoked);
        Assert.Equal(commandCount, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData(129)]
    [InlineData(131)]
    public async Task CalculateSessionKeysAsymmetricAsync_InvalidContextLength_ThrowsBeforeDeviceIo(int length)
    {
        var connection = CreateInitializedConnection(SessionKeyResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 6, 0) },
            cancellationToken: TestContext.Current.CancellationToken);
        var commandCount = connection.TransmittedCommands.Count;
        var touchCallbackInvoked = false;
        session.OnTouchRequired = () => touchCallbackInvoked = true;

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            session.CalculateSessionKeysAsymmetricAsync(
                "cred",
                new byte[length],
                Sequence(0x60, 65),
                "pass"u8.ToArray(),
                Sequence(0x70, 8),
                TestContext.Current.CancellationToken));

        Assert.Equal("context", exception.ParamName);
        Assert.False(touchCallbackInvoked);
        Assert.Equal(commandCount, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task GetChallengeAsync_Firmware560WithoutPassword_TransmitsOnlyLabel()
    {
        var connection = CreateInitializedConnection([.. Sequence(0x40, 8), 0x90, 0x00]);
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 6, 0) },
            cancellationToken: TestContext.Current.CancellationToken);

        var challenge = await session.GetChallengeAsync(
            "cred",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(Sequence(0x40, 8), challenge.ToArray());
        Assert.Equal([
            0x71, 0x04, (byte)'c', (byte)'r', (byte)'e', (byte)'d'
        ], CommandData(LastCommand(connection)).ToArray());
    }

    [Fact]
    public async Task GetChallengeAsync_Firmware560WithPassword_DoesNotTransmitPassword()
    {
        var connection = CreateInitializedConnection([.. Sequence(0x40, 8), 0x90, 0x00]);
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 6, 0) },
            cancellationToken: TestContext.Current.CancellationToken);

        await session.GetChallengeAsync(
            "cred",
            "pass"u8.ToArray(),
            TestContext.Current.CancellationToken);

        Assert.Equal([
            0x71, 0x04, (byte)'c', (byte)'r', (byte)'e', (byte)'d'
        ], CommandData(LastCommand(connection)).ToArray());
    }

    [Fact]
    public async Task GetChallengeAsync_Firmware571WithPassword_TransmitsPassword()
    {
        var connection = CreateInitializedConnection([.. Sequence(0x40, 8), 0x90, 0x00]);
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 7, 1) },
            cancellationToken: TestContext.Current.CancellationToken);

        await session.GetChallengeAsync(
            "cred",
            "pass"u8.ToArray(),
            TestContext.Current.CancellationToken);

        Assert.Equal([
            0x71, 0x04, (byte)'c', (byte)'r', (byte)'e', (byte)'d',
            0x73, 0x10, (byte)'p', (byte)'a', (byte)'s', (byte)'s', .. new byte[12]
        ], CommandData(LastCommand(connection)).ToArray());
    }

    [Fact]
    public async Task DeleteCredentialAsync_WhenManagementKeyRetryFailure_ThrowsHsmAuthRetryException()
    {
        // SW 63C2 is the YubiHSM Auth retry-counter failure response with 2 attempts left.
        var connection = CreateInitializedConnection([0x63, 0xC2]);
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 4, 3) },
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.DeleteCredentialAsync(
            Sequence(0x10, 16),
            "cred",
            TestContext.Current.CancellationToken));

        Assert.Equal(unchecked((short)0x63C2), exception.SW);
        Assert.Equal(2, exception.RetriesRemaining);
        Assert.Equal((byte)0x02, exception.Ins.GetValueOrDefault());
        Assert.Contains("2 attempt(s) remaining", exception.Message);
        var command = LastCommand(connection);
        // APDU header: INS=DELETE; retry-aware exception proves throwOnError:false allowed SW 63Cx parsing.
        Assert.Equal(0x02, command[1]);
        // Data: management key(7B) followed by credential label(71).
        Assert.Equal([
            0x7B, 0x10, .. Sequence(0x10, 16),
            0x71, 0x04, (byte)'c', (byte)'r', (byte)'e', (byte)'d'
        ], CommandData(command).ToArray());
    }

    /// <summary>
    ///     Pins the CHANGE credential password APDU for the password-authenticated path.
    /// </summary>
    /// <remarks>
    ///     The hardware test for this operation skips on applet builds that answer SW=0x6D00, so the
    ///     instruction byte, P1 selector, and TLV order are pinned here instead. Without this, a
    ///     regression in the instruction byte would look identical to an applet that lacks the
    ///     instruction and would be skipped rather than caught.
    /// </remarks>
    [Fact]
    public async Task ChangeCredentialPasswordAsync_TransmitsChangeInstructionWithPasswordSelector()
    {
        var connection = CreateInitializedConnection(OkResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 8, 0) },
            cancellationToken: TestContext.Current.CancellationToken);

        await session.ChangeCredentialPasswordAsync(
            "cred",
            "pass"u8.ToArray(),
            "newpass"u8.ToArray(),
            TestContext.Current.CancellationToken);

        var command = LastCommand(connection);
        Assert.Equal(0x00, command[0]);
        Assert.Equal(0x0B, command[1]);
        Assert.Equal(0x00, command[2]); // P1=0 selects password authentication.
        Assert.Equal(0x00, command[3]);

        // Label, then current password, then new password.
        var data = CommandData(command).ToArray();
        Assert.Equal(0x71, data[0]);
        var afterLabel = 2 + data[1];
        Assert.Equal(0x73, data[afterLabel]);
        var afterCurrent = afterLabel + 2 + data[afterLabel + 1];
        Assert.Equal(0x73, data[afterCurrent]);
    }

    /// <summary>
    ///     Pins the CHANGE credential password APDU for the management-key path, which differs only
    ///     by its P1 selector and by carrying the management key instead of the current password.
    /// </summary>
    [Fact]
    public async Task ChangeCredentialPasswordAdminAsync_TransmitsChangeInstructionWithManagementKeySelector()
    {
        var connection = CreateInitializedConnection(OkResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 8, 0) },
            cancellationToken: TestContext.Current.CancellationToken);

        await session.ChangeCredentialPasswordAdminAsync(
            Sequence(0x10, 16),
            "cred",
            "newpass"u8.ToArray(),
            TestContext.Current.CancellationToken);

        var command = LastCommand(connection);
        Assert.Equal(0x0B, command[1]);
        Assert.Equal(0x01, command[2]); // P1=1 selects management-key authentication.

        var data = CommandData(command).ToArray();
        Assert.Equal(0x71, data[0]);
        var afterLabel = 2 + data[1];
        Assert.Equal(0x7B, data[afterLabel]); // Management key, not a credential password.
    }

    private static RecordingSmartCardConnection CreateInitializedConnection(params byte[][] trailingResponses) =>
        new([OkResponse(), .. trailingResponses]);

    private static byte[] LastCommand(RecordingSmartCardConnection connection) =>
        connection.TransmittedCommands[^1];

    private static ReadOnlySpan<byte> CommandData(byte[] command) =>
        // Short APDU format: CLA INS P1 P2 Lc Data; the recorder reports SupportsExtendedApdu=false.
        command.AsSpan(5, command[4]);

    private static byte[] OkResponse() => [0x90, 0x00];

    private static byte[] Sequence(byte start, int length)
    {
        var bytes = new byte[length];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)(start + i);

        return bytes;
    }

    private static byte[] SessionKeyResponse() =>
    [
        .. Sequence(0xA0, 16),
        .. Sequence(0xB0, 16),
        .. Sequence(0xC0, 16),
        0x90, 0x00
    ];
}