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

namespace Yubico.YubiKit.Core.Utilities;

/// <summary>Looks up borrowed views of dictionary values without copying their storage.</summary>
public static class MemoryExtensions
{
    /// <summary>Returns the stored memory view, or empty memory when the key is absent.</summary>
    public static ReadOnlyMemory<TValue> GetMemory<TKey, TValue>(
        this IDictionary<TKey, ReadOnlyMemory<TValue>> dictionary,
        TKey key) =>
        dictionary.TryGetValue(key, out var memory) ? memory : ReadOnlyMemory<TValue>.Empty;

    /// <summary>Returns a span over the stored value. The dictionary indexer throws when the key is absent.</summary>
    public static ReadOnlySpan<TValue> GetSpan<TKey, TValue>(this IDictionary<TKey, ReadOnlyMemory<TValue>> dictionary,
        TKey key) => dictionary[key].Span;

    /// <summary>Returns a stored span and true on a match, or an empty span and false when the key is absent.</summary>
    public static bool TryGetSpan<TKey, TValue>(this IDictionary<TKey, ReadOnlyMemory<TValue>> dictionary, TKey key,
        out ReadOnlySpan<TValue> span)
    {
        if (dictionary.TryGetValue(key, out var memory))
        {
            span = memory.Span;
            return true;
        }

        span = ReadOnlySpan<TValue>.Empty;
        return false;
    }
}