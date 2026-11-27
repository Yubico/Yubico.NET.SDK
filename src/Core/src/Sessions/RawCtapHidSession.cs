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

using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Protocols;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;

namespace Yubico.YubiKit.Core.Sessions;

/// <summary>Provides guarded, application-agnostic CTAP HID logical exchanges.</summary>
/// <remarks>
///     The session supplies channel initialization, framing, continuation, keep-alive handling, final-command
///     correlation, and CTAPHID error rejection, but no CTAP2 or WebAuthn operation semantics. It borrows a
///     directly supplied connection. Direct connection <c>SendAsync</c>/<c>ReceiveAsync</c> calls bypass the
///     session's overlap guard.
/// </remarks>
public sealed class RawCtapHidSession : ApplicationSession
{
    private IFidoHidProtocol FidoProtocol =>
        (IFidoHidProtocol)(Protocol ?? throw new ObjectDisposedException(nameof(RawCtapHidSession)));

    private RawCtapHidSession(IFidoHidConnection connection)
        : base(connection)
    {
    }

    /// <summary>Creates a raw FIDO HID session that borrows <paramref name="connection" />.</summary>
    /// <remarks>
    ///     Creation attaches the session without wire I/O and sets IsInitialized. The first exchange initializes
    ///     the channel; the session's inherited FirmwareVersion remains unknown (0.0.0).
    /// </remarks>
    public static Task<RawCtapHidSession> CreateAsync(
        IFidoHidConnection connection,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RawCtapHidSession session = Construct(connection, () => new RawCtapHidSession(connection));
        try
        {
            session.Protocol = ProtocolFactory.Create(connection);
            session.IsInitialized = true;
            return Task.FromResult(session);
        }
        catch
        {
            session.DisposeAfterInitializationFailure();
            throw;
        }
    }

    /// <summary>Sends one caller-defined CTAP HID command and returns its correlated response payload.</summary>
    /// <remarks>
    ///     Use a request/response CTAPHID command, not a CTAP2 operation code. CANCEL (0x11) has no response of
    ///     its own and is not an ordinary exchange for this method. Channel-control sequences require their own
    ///     protocol handling. After a partial framing or transport failure, dispose and reopen the connection.
    ///     Caller cancellation is observed between reads: after a keep-alive the protocol can send CANCEL and
    ///     drain the original response. Without another report, the operation can remain pending indefinitely.
    ///     CTAPHID cancellation is defined for active CBOR requests; vendor commands have their own semantics.
    ///     Keep payload storage unchanged until the actual returned task is terminal, including lazy INIT and
    ///     incremental packet sends. Canceling an outer wait does not end this borrow.
    ///     SDK-owned outgoing packet copies are cleared after use; caller-owned <paramref name="payload" /> is not
    ///     modified. Returned memory survives session disposal; the caller must clear its actual backing storage
    ///     when sensitive bytes are no longer needed. This cleanup does not reach intermediate received reports.
    /// </remarks>
    public Task<ReadOnlyMemory<byte>> SendAndReceiveAsync(
        byte command,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return FidoProtocol.SendCommandAsync(
            command,
            payload,
            UserPresenceNotification.None,
            cancellationToken);
    }
}