// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Yubico.YubiKit.WebAuthn.Util;

/// <summary>
/// Zeroing for the optional secret-derived buffers the client and backend carry on their
/// request and options records.
/// </summary>
internal static class SensitiveMemory
{
    /// <summary>
    /// Zeroes <paramref name="memory"/> in place. Null and empty are no-ops.
    /// </summary>
    /// <remarks>
    /// A zeroing helper that quietly skips is a secret left in memory, so make the only
    /// unreachable case loud rather than silent. Callers here always pass array-backed memory;
    /// this cannot throw instead because every call site is a finally block, where throwing
    /// would swallow the exception already in flight.
    /// </remarks>
    public static void Zero(ReadOnlyMemory<byte>? memory)
    {
        if (memory is null || memory.Value.IsEmpty)
        {
            return;
        }

        var isArrayBacked = MemoryMarshal.TryGetArray(memory.Value, out var segment) && segment.Array is not null;
        Debug.Assert(isArrayBacked, "pinUvAuthParam must be array-backed so it can be zeroed");

        if (isArrayBacked)
        {
            CryptographicOperations.ZeroMemory(segment.AsSpan());
        }
    }
}