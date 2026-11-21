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
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Transports.Hid;

namespace Yubico.YubiKit.Core.Sessions;

/// <summary>Provides guarded, application-agnostic OTP HID logical exchanges.</summary>
/// <remarks>
///     The session supplies feature-report framing, sequencing, polling, and outbound CRC generation, but no OTP
///     applet or slot-configuration semantics. Inbound CRC is command-specific and is not validated. It borrows a
///     directly supplied connection. Direct connection <c>SendAsync</c>/<c>ReceiveAsync</c> calls bypass the
///     session's overlap guard.
///     After an interrupted frame, the protocol attempts one abort before allowing reuse. If the abort or
///     response-completion reset fails, this session refuses further exchanges. Dispose the session and reopen
///     its connection; creating another session over the same borrowed connection is not a recovery procedure.
/// </remarks>
public sealed class RawOtpHidSession : ApplicationSession
{
    private IOtpHidProtocol OtpProtocol =>
        (IOtpHidProtocol)(Protocol ?? throw new ObjectDisposedException(nameof(RawOtpHidSession)));

    private RawOtpHidSession(IOtpHidConnection connection)
        : base(connection)
    {
    }

    /// <summary>Creates a raw OTP HID session that borrows <paramref name="connection" />.</summary>
    /// <remarks>
    ///     Creation attaches the session without wire I/O and sets IsInitialized. The first exchange reads status;
    ///     the session's inherited FirmwareVersion remains unknown (0.0.0).
    /// </remarks>
    public static Task<RawOtpHidSession> CreateAsync(
        IOtpHidConnection connection,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RawOtpHidSession session = Construct(connection, () => new RawOtpHidSession(connection));
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

    /// <summary>Sends one caller-defined command or slot byte and returns the unvalidated response payload.</summary>
    /// <remarks>
    ///     For a command with a known response length, the caller can validate inbound CRC with
    ///     <see cref="ChecksumUtils.CheckCrc(ReadOnlySpan{byte},int)" /> over <c>expectedLength + 2</c> bytes.
    ///     CRC detects corruption rather than authenticating the response. Keep payload storage unchanged until
    ///     the actual returned task is terminal: lazy initialization precedes the payload copy. Canceling an
    ///     outer wait does not end this borrow.
    ///     SDK-owned outgoing buffers are cleared after use; caller-owned <paramref name="payload" /> is not
    ///     modified. Returned memory survives session disposal; the caller must clear its actual backing storage
    ///     when sensitive bytes are no longer needed. This cleanup does not reach intermediate received reports.
    /// </remarks>
    public Task<ReadOnlyMemory<byte>> SendAndReceiveAsync(
        byte command,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return OtpProtocol.SendAndReceiveAsync(command, payload, cancellationToken);
    }
}