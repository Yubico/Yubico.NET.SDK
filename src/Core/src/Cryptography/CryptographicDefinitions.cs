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

using System.Collections.Frozen;

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>Immediately usable, immutable shared metadata for keys, curves and operations.</summary>
/// <remarks>Describes structure, not hardware/provider support, protocol profiles or recommended choices.</remarks>
public static partial class CryptographicDefinitions
{
    // ToFrozenDictionary may coalesce duplicate keys. Validate deliberately before freezing so a
    // mistaken catalog addition fails at initialization rather than silently replacing metadata.
    internal static FrozenDictionary<TId, TInfo> CreateIndex<TId, TInfo>(
        IEnumerable<TInfo> entries, Func<TInfo, TId> getId) where TId : notnull
    {
        var index = new Dictionary<TId, TInfo>();
        foreach (var entry in entries)
        {
            var id = getId(entry);
            if (!index.TryAdd(id, entry))
            {
                throw new InvalidOperationException($"Duplicate cryptographic catalog identity '{id}'.");
            }
        }
        return index.ToFrozenDictionary();
    }
}