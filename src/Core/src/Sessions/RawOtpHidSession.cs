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
///     The session supplies feature-report framing, sequencing, polling, and CRC handling, but no OTP applet
///     or slot-configuration semantics. It borrows a directly supplied connection. Direct connection
///     <c>SendAsync</c>/<c>ReceiveAsync</c> calls bypass the session's overlap guard.
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
    /// <remarks>OTP status initialization is deferred until the first operation.</remarks>
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

    /// <summary>Sends one caller-defined command or slot byte and returns the complete response payload.</summary>
    public Task<ReadOnlyMemory<byte>> SendAndReceiveAsync(
        byte command,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return OtpProtocol.SendAndReceiveAsync(command, payload, cancellationToken);
    }
}