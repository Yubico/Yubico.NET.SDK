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
///     The card stores unsigned four-byte seconds since 1970-01-01 UTC, in big-endian order. This
///     collection currently exposes each value as a signed int, so values above int.MaxValue appear
///     negative: for example, card bytes 80 00 00 00 become int.MinValue. They represent unsigned
///     seconds, rather than dates before 1970. Preserve the original four-byte representation, or
///     interpret the same 32 bits as an unsigned value, when inspecting that range. Raw application
///     data provides the original bytes. Card specification 3.4.1 §4.4.1 defines zero as unspecified;
///     RFC 4880 §3.5 defines the unsigned time field.
/// </remarks>
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