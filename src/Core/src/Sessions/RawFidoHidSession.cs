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

public sealed class RawFidoHidSession : ApplicationSession
{
    private IFidoHidProtocol FidoProtocol =>
        (IFidoHidProtocol)(Protocol ?? throw new ObjectDisposedException(nameof(RawFidoHidSession)));

    private RawFidoHidSession(IFidoHidConnection connection)
        : base(connection)
    {
    }

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

    public Task<ReadOnlyMemory<byte>> SendAndReceiveAsync(
        byte command,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return FidoProtocol.SendVendorCommandAsync(command, payload, cancellationToken);
    }
}