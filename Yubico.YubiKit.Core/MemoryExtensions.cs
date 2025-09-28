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

namespace Yubico.YubiKit.Core;

public static class MemoryExtensions
{
    public static ReadOnlyMemory<TValue> GetMemory<TKey, TValue>(
        this IDictionary<TKey, ReadOnlyMemory<TValue>> dictionary,
        TKey key) =>
        dictionary.TryGetValue(key, out var memory) ? memory : ReadOnlyMemory<TValue>.Empty;

    public static ReadOnlySpan<TValue> GetSpan<TKey, TValue>(this IDictionary<TKey, ReadOnlyMemory<TValue>> dictionary,
        TKey key) => dictionary[key].Span;

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