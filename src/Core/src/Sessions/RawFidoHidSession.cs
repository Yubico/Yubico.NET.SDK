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

using Yubico.YubiKit.Core.Protocols;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;

namespace Yubico.YubiKit.Core.Sessions;

/// <summary>Provides guarded, application-agnostic CTAP HID logical exchanges.</summary>
/// <remarks>
///     The session supplies channel initialization, framing, continuation, keep-alive handling, final-command
///     correlation, and CTAPHID error rejection, but no FIDO2 or WebAuthn operation semantics. It borrows a
///     directly supplied connection. Direct connection <c>SendAsync</c>/<c>ReceiveAsync</c> calls bypass the
///     session's overlap guard.
/// </remarks>
public sealed class RawFidoHidSession : ApplicationSession
{
    private IFidoHidProtocol FidoProtocol =>
        (IFidoHidProtocol)(Protocol ?? throw new ObjectDisposedException(nameof(RawFidoHidSession)));

    private RawFidoHidSession(IFidoHidConnection connection)
        : base(connection)
    {
    }

    /// <summary>Creates a raw FIDO HID session that borrows <paramref name="connection" />.</summary>
    /// <remarks>CTAP HID channel initialization is deferred until the first operation.</remarks>
    public static Task<RawFidoHidSession> CreateAsync(
        IFidoHidConnection connection,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RawFidoHidSession session = Construct(connection, () => new RawFidoHidSession(connection));
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
    ///     SDK-owned outgoing packet copies are cleared after use; caller-owned <paramref name="payload" /> is not
    ///     modified. Returned memory remains live after return and is caller-owned for sensitive-data handling.
    /// </remarks>
    public Task<ReadOnlyMemory<byte>> SendAndReceiveAsync(
        byte command,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return FidoProtocol.SendVendorCommandAsync(command, payload, cancellationToken);
    }
}