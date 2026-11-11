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

using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>Performs independent one-shot discovery with a new scoped finder for every call.</summary>
internal sealed class YubiKeyDiscovery(Func<ConnectionType, FindYubiKeys> createFinder)
{
    internal async Task<IReadOnlyList<IYubiKey>> FindAllAsync(
        ConnectionType interfaces, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var finder = createFinder(interfaces);
        try
        {
            return await finder.FindAllAsync(ConnectionType.All, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            finder.RetireReadScope();
        }
    }

    internal async Task<IYubiKey?> FindFirstOrDefaultAsync(
        Func<IYubiKey, bool>? predicate, ConnectionType interfaces, CancellationToken cancellationToken)
    {
        var devices = await FindAllAsync(interfaces, cancellationToken).ConfigureAwait(false);
        return predicate is null ? devices.FirstOrDefault() : devices.FirstOrDefault(predicate);
    }

    internal async Task<IYubiKey> FindFirstAsync(
        Func<IYubiKey, bool>? predicate, ConnectionType interfaces, CancellationToken cancellationToken) =>
        await FindFirstOrDefaultAsync(predicate, interfaces, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No matching YubiKey was found.");
}