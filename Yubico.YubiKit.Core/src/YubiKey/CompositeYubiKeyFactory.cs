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

using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
/// Default implementation of <see cref="ICompositeYubiKeyFactory"/>.
/// </summary>
/// <remarks>
/// <para>
/// This factory correlates transport references by reading device identity information
/// and grouping references that share the same correlation key.
/// </para>
/// <para>
/// References that fail identity reading or cannot be correlated are treated as
/// singleton composites (one reference = one composite).
/// </para>
/// </remarks>
internal sealed class CompositeYubiKeyFactory : ICompositeYubiKeyFactory
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<IYubiKey>> CreateCompositesAsync(
        IReadOnlyList<IYubiKeyReference> references,
        Func<IYubiKeyReference, CancellationToken, Task<IDeviceIdentity>> identityReader,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(references);
        ArgumentNullException.ThrowIfNull(identityReader);

        if (references.Count == 0)
        {
            return [];
        }

        // Read identity for all references
        var identities = new Dictionary<IYubiKeyReference, IDeviceIdentity>();
        foreach (var reference in references)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var identity = await identityReader(reference, cancellationToken).ConfigureAwait(false);
                identities[reference] = identity;
            }
            catch (Exception)
            {
                // If identity reading fails, we'll create a singleton composite for this reference
                // using a minimal identity with null serial
            }
        }

        // Group references by correlation key
        var groups = new Dictionary<DeviceCorrelationKey, List<(IYubiKeyReference Reference, IDeviceIdentity Identity)>>();
        var uncorrelatable = new List<IYubiKeyReference>();

        foreach (var reference in references)
        {
            if (!identities.TryGetValue(reference, out var identity))
            {
                uncorrelatable.Add(reference);
                continue;
            }

            var key = DeviceCorrelationKey.From(identity);
            if (!groups.TryGetValue(key, out var group))
            {
                group = [];
                groups[key] = group;
            }

            group.Add((reference, identity));
        }

        // Create composite devices
        var composites = new List<IYubiKey>();

        // Correlated groups
        foreach (var (_, group) in groups)
        {
            // Use the first identity (they should all be equivalent for correlation purposes)
            var identity = group[0].Identity;
            var refs = group.Select(g => g.Reference).ToList();
            composites.Add(new CompositeYubiKey(identity, refs));
        }

        // Uncorrelatable references become singleton composites with minimal identity
        foreach (var reference in uncorrelatable)
        {
            var minimalIdentity = new MinimalDeviceIdentity(reference.DeviceId);
            composites.Add(new CompositeYubiKey(minimalIdentity, [reference]));
        }

        return composites;
    }
}
