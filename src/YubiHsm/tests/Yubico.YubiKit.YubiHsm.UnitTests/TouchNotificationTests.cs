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

using System.Text;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.YubiHsm.UnitTests;

/// <summary>
///     Proves ISC-32: touch-requiring YubiHSM Auth operations expose an in-flight notification
///     callback, fired before the blocking CALCULATE exchange, using a fake protocol.
/// </summary>
public class TouchNotificationTests
{
    [Fact]
    public async Task OnTouchRequired_Property_IsNullableAndSettable()
    {
        var connection = CreateInitializedConnection();
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 4, 3),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(session.OnTouchRequired);

        session.OnTouchRequired = () => { };

        Assert.NotNull(session.OnTouchRequired);
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WhenCredentialRequiresTouch_InvokesCallbackBeforeCalculateCommand()
    {
        var connection = CreateInitializedConnection(
            ListResponse("cred", HsmAuthAlgorithm.Aes128YubicoAuthentication, touchByte: 0x01, counter: 8),
            SessionKeyResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 4, 3),
            cancellationToken: TestContext.Current.CancellationToken);

        var commandCountWhenNotified = -1;
        session.OnTouchRequired = () => commandCountWhenNotified = connection.TransmittedCommands.Count;

        using var keys = await session.CalculateSessionKeysSymmetricAsync(
            "cred",
            Sequence(0x40, 16),
            "pass"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotEqual(-1, commandCountWhenNotified);
        // SELECT (1) + LIST (2) were sent by the time the callback fired; CALCULATE (3) had not.
        Assert.Equal(2, commandCountWhenNotified);
        Assert.Equal(3, connection.TransmittedCommands.Count);
        Assert.Equal(0x03, connection.TransmittedCommands[^1][1]); // last command is CALCULATE
        Assert.Equal(Sequence(0xA0, 16), keys.SEnc.ToArray());
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WhenCredentialDoesNotRequireTouch_DoesNotInvokeCallback()
    {
        var connection = CreateInitializedConnection(
            ListResponse("cred", HsmAuthAlgorithm.Aes128YubicoAuthentication, touchByte: 0x00, counter: 8),
            SessionKeyResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 4, 3),
            cancellationToken: TestContext.Current.CancellationToken);

        var invoked = false;
        session.OnTouchRequired = () => invoked = true;

        using var keys = await session.CalculateSessionKeysSymmetricAsync(
            "cred",
            Sequence(0x40, 16),
            "pass"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(invoked);
        Assert.Equal(3, connection.TransmittedCommands.Count); // SELECT + LIST + CALCULATE
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WhenCallbackNotRegistered_DoesNotQueryCredentialList()
    {
        // Only SELECT + CALCULATE responses are queued. If the implementation queried
        // ListCredentialsAsync despite no subscriber, the connection would throw for the
        // missing third queued response, failing this test.
        var connection = CreateInitializedConnection(SessionKeyResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 4, 3),
            cancellationToken: TestContext.Current.CancellationToken);

        using var keys = await session.CalculateSessionKeysSymmetricAsync(
            "cred",
            Sequence(0x40, 16),
            "pass"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, connection.TransmittedCommands.Count); // SELECT + CALCULATE only
        Assert.Equal(Sequence(0xA0, 16), keys.SEnc.ToArray());
    }

    [Fact]
    public async Task CalculateSessionKeysAsymmetricAsync_WhenCredentialRequiresTouch_InvokesCallbackBeforeCalculateCommand()
    {
        var connection = CreateInitializedConnection(
            ListResponse("cred", HsmAuthAlgorithm.EcP256YubicoAuthentication, touchByte: 0x01, counter: 3),
            SessionKeyResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 6, 0),
            cancellationToken: TestContext.Current.CancellationToken);

        var invoked = false;
        session.OnTouchRequired = () => invoked = true;

        using var keys = await session.CalculateSessionKeysAsymmetricAsync(
            "cred",
            Sequence(0x40, 16),
            Sequence(0x60, 8),
            "pass"u8.ToArray(),
            Sequence(0x70, 8),
            TestContext.Current.CancellationToken);

        Assert.True(invoked);
        Assert.Equal(Sequence(0xA0, 16), keys.SEnc.ToArray());
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WhenTouchSemanticsUnknown_InvokesCallbackConservatively()
    {
        // Touch byte 0x02 does not map to a known true/false value; HsmAuthCredential.TouchRequired
        // parses this as null. The callback should still fire conservatively.
        var connection = CreateInitializedConnection(
            ListResponse("cred", HsmAuthAlgorithm.Aes128YubicoAuthentication, touchByte: 0x02, counter: 8),
            SessionKeyResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 4, 3),
            cancellationToken: TestContext.Current.CancellationToken);

        var invoked = false;
        session.OnTouchRequired = () => invoked = true;

        using var keys = await session.CalculateSessionKeysSymmetricAsync(
            "cred",
            Sequence(0x40, 16),
            "pass"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(invoked);
        Assert.Equal(Sequence(0xA0, 16), keys.SEnc.ToArray());
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WhenOnTouchRequiredCallbackThrows_PropagatesExactlyOnceWithoutBeingCaught()
    {
        // Regression test: NotifyTouchIfRequiredAsync must not wrap the OnTouchRequired.Invoke()
        // call in the same try/catch that guards the ListCredentialsAsync query. If it did, a
        // throwing callback would be caught by the generic "failed to query credential list"
        // handler, misdiagnosed, and invoked a second time (which would then throw unhandled).
        var connection = CreateInitializedConnection(
            ListResponse("cred", HsmAuthAlgorithm.Aes128YubicoAuthentication, touchByte: 0x01, counter: 8),
            SessionKeyResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 4, 3),
            cancellationToken: TestContext.Current.CancellationToken);

        var invocationCount = 0;
        session.OnTouchRequired = () =>
        {
            invocationCount++;
            throw new InvalidOperationException("callback boom");
        };

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.CalculateSessionKeysSymmetricAsync(
                "cred",
                Sequence(0x40, 16),
                "pass"u8.ToArray(),
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("callback boom", thrown.Message);
        Assert.Equal(1, invocationCount);
        // SELECT (1) + LIST (2) were sent; CALCULATE must never be sent once the callback throws.
        Assert.Equal(2, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WhenCallbackClearedWhileListPending_InvokesCapturedCallbackOnceAndSucceeds()
    {
        var connection = new GatedListSmartCardConnection(OkResponse(), SessionKeyResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 4, 3),
            cancellationToken: TestContext.Current.CancellationToken);

        var invocationCount = 0;
        session.OnTouchRequired = () => invocationCount++;

        var operation = session.CalculateSessionKeysSymmetricAsync(
            "cred",
            Sequence(0x40, 16),
            "pass"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken);

        await connection.ListCommandReceived.WaitAsync(TestContext.Current.CancellationToken);
        session.OnTouchRequired = null;
        connection.CompleteList(
            ListResponse("cred", HsmAuthAlgorithm.Aes128YubicoAuthentication, touchByte: 0x01, counter: 8));

        using var keys = await operation;

        Assert.Equal(1, invocationCount);
        Assert.Equal(3, connection.TransmittedCommands.Count);
        Assert.Equal(Sequence(0xA0, 16), keys.SEnc.ToArray());
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WhenCallbackReplacedWhileFailingListPending_InvokesCapturedCallbackOnceAndSucceeds()
    {
        var connection = new GatedListSmartCardConnection(OkResponse(), SessionKeyResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 4, 3),
            cancellationToken: TestContext.Current.CancellationToken);

        var originalInvocationCount = 0;
        var replacementInvocationCount = 0;
        session.OnTouchRequired = () => originalInvocationCount++;

        var operation = session.CalculateSessionKeysSymmetricAsync(
            "cred",
            Sequence(0x40, 16),
            "pass"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken);

        await connection.ListCommandReceived.WaitAsync(TestContext.Current.CancellationToken);
        session.OnTouchRequired = () => replacementInvocationCount++;
        connection.FailList(new InvalidOperationException("LIST failed"));

        using var keys = await operation;

        Assert.Equal(1, originalInvocationCount);
        Assert.Equal(0, replacementInvocationCount);
        Assert.Equal(3, connection.TransmittedCommands.Count);
        Assert.Equal(Sequence(0xA0, 16), keys.SEnc.ToArray());
    }

    private static RecordingSmartCardConnection CreateInitializedConnection(params byte[][] trailingResponses) =>
        new([OkResponse(), .. trailingResponses]);

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

    private static byte[] ListResponse(string label, HsmAuthAlgorithm algorithm, byte touchByte, byte counter)
    {
        var labelBytes = Encoding.UTF8.GetBytes(label);
        byte[] value = [(byte)algorithm, touchByte, .. labelBytes, counter];

        return [0x72, (byte)value.Length, .. value, 0x90, 0x00];
    }

    private sealed class GatedListSmartCardConnection(params byte[][] nonListResponses) : ISmartCardConnection
    {
        private readonly Queue<byte[]> _nonListResponses = new(nonListResponses);
        private readonly TaskCompletionSource _listCommandReceived =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<ReadOnlyMemory<byte>> _listResponse =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ListCommandReceived => _listCommandReceived.Task;

        public List<byte[]> TransmittedCommands { get; } = [];

        public Transport Transport { get; } = Transport.Usb;

        public ConnectionType Type { get; } = ConnectionType.SmartCard;

        public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TransmittedCommands.Add(command.ToArray());

            if (command.Span.Length > 1 && command.Span[1] == HsmAuthSession.InsList)
            {
                _listCommandReceived.SetResult();
                return await _listResponse.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            if (_nonListResponses.Count == 0)
            {
                throw new InvalidOperationException("No response enqueued for transmission.");
            }

            return _nonListResponses.Dequeue();
        }

        public void CompleteList(ReadOnlyMemory<byte> response) => _listResponse.SetResult(response);

        public void FailList(Exception exception) => _listResponse.SetException(exception);

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            NullDisposable.Instance;

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
}