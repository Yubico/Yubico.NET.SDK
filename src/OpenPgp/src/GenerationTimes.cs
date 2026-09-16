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
///     Generation timestamps for each key slot (Unix epoch seconds).
/// </summary>
public sealed class GenerationTimes : IReadOnlyDictionary<KeyRef, int>
{
    private readonly Dictionary<KeyRef, int> _entries;

    internal GenerationTimes(Dictionary<KeyRef, int> entries) =>
        _entries = entries;

    /// <inheritdoc />
    public int this[KeyRef key] => _entries[key];

    /// <inheritdoc />
    public IEnumerable<KeyRef> Keys => _entries.Keys;

    /// <inheritdoc />
    public IEnumerable<int> Values => _entries.Values;

    /// <inheritdoc />
    public int Count => _entries.Count;

    /// <inheritdoc />
    public bool ContainsKey(KeyRef key) => _entries.ContainsKey(key);

    /// <inheritdoc />
    public bool TryGetValue(KeyRef key, out int value) => _entries.TryGetValue(key, out value);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<KeyRef, int>> GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}