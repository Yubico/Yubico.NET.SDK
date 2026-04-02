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

using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.OpenPgp.IntegrationTests.Helpers;

/// <summary>
///     A wrapper around an <see cref="ISmartCardConnection" /> that ignores disposal.
///     Allows sharing a connection between a reset session and a test session.
/// </summary>
internal sealed class SharedSmartCardConnection(ISmartCardConnection connection) : ISmartCardConnection
{
    public Transport Transport => connection.Transport;

    public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ReadOnlyMemory<byte> command,
        CancellationToken cancellationToken = default) =>
        connection.TransmitAndReceiveAsync(command, cancellationToken);

    public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
        connection.BeginTransaction(cancellationToken);

    public bool SupportsExtendedApdu() => connection.SupportsExtendedApdu();

    public void Dispose()
    {
        // Do nothing - connection lifecycle is managed by the owner
    }

    public ValueTask DisposeAsync() => default;

    public ConnectionType Type { get; } = ConnectionType.SmartCard;
}
