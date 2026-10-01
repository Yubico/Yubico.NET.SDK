// Copyright 2025 Yubico AB
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

using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Transports.SmartCard;

public interface ISmartCardConnection : IConnection
{
    Transport Transport { get; }

    /// <summary>Transmits caller-formatted SmartCard bytes and returns the raw response bytes.</summary>
    /// <remarks>
    ///     This Tier 2 method bypasses <see cref="Sessions.ApplicationSession" />, the connection session guard,
    ///     and the logical exchange guard. The caller owns APDU formatting, chaining, response correlation,
    ///     concurrency exclusion, cancellation recovery, and state integrity. Do not call it concurrently with
    ///     a live session or another raw operation. Dispose and reopen the connection when state is uncertain.
    ///     Keep borrowed <paramref name="command" /> memory valid until the returned task is terminal;
    ///     only then zero sensitive caller-owned input. This applies to every implementation. Built-in PC/SC
    ///     connections reject cancellation before native dispatch; after dispatch the call may succeed and the
    ///     task waits for native completion and resource drain. Cancellation alone does not make a raw connection
    ///     reusable; dispose and reopen if exchange state is uncertain.
    /// </remarks>
    Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ReadOnlyMemory<byte> command,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Starts a PC/SC transaction. The transaction is ended when the returned scope is disposed.
    ///     Uses LEAVE_CARD disposition when ending the transaction. Built-in synchronous begin and scope
    ///     disposal can block without a bound on native acquisition/end; prefer async begin and, for built-in
    ///     scopes, async disposal.
    /// </summary>
    IDisposable BeginTransaction(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Starts a PC/SC transaction. Built-in connections await native acquisition without blocking the caller.
    ///     Their returned <see cref="IDisposable" /> scope also implements <see cref="IAsyncDisposable" />;
    ///     use async disposal when available to await transaction end.
    /// </summary>
    /// <remarks>
    ///     The default for custom implementations calls <see cref="BeginTransaction" /> synchronously and may
    ///     block the caller; override it for nonblocking acquisition. Built-in connections reject cancellation
    ///     before native dispatch; afterward the transaction may begin successfully, and completion waits for
    ///     native work. Do not assume cancellation releases the connection or ends the transaction.
    /// </remarks>
    Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(BeginTransaction(cancellationToken));
    }

    bool SupportsExtendedApdu();
    // byte[] getAtr();
}