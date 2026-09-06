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
    /// <see cref="MemoryMarshal.AsMemory{T}(ReadOnlyMemory{T})"/> is what makes this total: it
    /// yields a writable span whatever the memory is backed by, so there is no "could not zero
    /// this one" branch left to get wrong. The previous form asserted array backing in debug
    /// builds and silently skipped it in release, which is the single behaviour a zeroing helper
    /// must never have - a release build would have left the secret live. Throwing instead is not
    /// an option either, because every call site is a finally block where it would swallow the
    /// exception already in flight. Writing through a read-only view is sound here: every buffer
    /// reaching this method is secret material the caller allocated in order to destroy.
    /// </remarks>
    public static void Zero(ReadOnlyMemory<byte>? memory)
    {
        if (memory is null || memory.Value.IsEmpty)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(memory.Value).Span);
    }
}