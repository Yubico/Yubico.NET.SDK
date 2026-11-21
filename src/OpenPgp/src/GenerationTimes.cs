// Copyright 2025 Yubico AB
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

using System.Collections;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Stored generation-time metadata for reported key slots; zero denotes an unspecified time.
/// </summary>
/// <remarks>
///     Values are unsigned seconds since 1970-01-01 UTC. Card specification 3.4.1 §4.4.1
///     defines zero as unspecified, not the epoch.
/// </remarks>
public sealed class GenerationTimes : IReadOnlyDictionary<KeyRef, uint>
{
    private readonly Dictionary<KeyRef, uint> _entries;

    internal GenerationTimes(Dictionary<KeyRef, uint> entries) =>
        _entries = entries;

    /// <inheritdoc />
    public uint this[KeyRef key] => _entries[key];

    /// <inheritdoc />
    public IEnumerable<KeyRef> Keys => _entries.Keys;

    /// <inheritdoc />
    public IEnumerable<uint> Values => _entries.Values;

    /// <inheritdoc />
    public int Count => _entries.Count;

    /// <inheritdoc />
    public bool ContainsKey(KeyRef key) => _entries.ContainsKey(key);

    /// <inheritdoc />
    public bool TryGetValue(KeyRef key, out uint value) => _entries.TryGetValue(key, out value);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<KeyRef, uint>> GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
