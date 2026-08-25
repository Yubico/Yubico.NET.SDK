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
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.UnitTests.Infrastructure;

/// <summary>
/// Inert <see cref="IYubiKey"/> for tests that only need identity and advertised connections.
/// </summary>
/// <remarks>
/// Connecting throws — this is for tests about discovery, caching, and event plumbing, not about
/// transports. Several older test files still declare their own private equivalents; prefer this one
/// for new tests so the copies can be retired opportunistically.
/// </remarks>
internal sealed class StubYubiKey(
    string deviceId,
    ConnectionType availableConnections = ConnectionType.SmartCard) : IYubiKey
{
    public string DeviceId { get; } = deviceId;

    public ConnectionType AvailableConnections { get; } = availableConnections;

    public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : class, IConnection =>
        throw new NotSupportedException();
}