// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKit.SecurityDomain;

/// <summary>Specifies whether a Security Domain operation adds a version or replaces an existing version.</summary>
public readonly record struct KeyReplacement
{
    private KeyReplacement(byte kvn) => Kvn = kvn;

    internal byte Kvn { get; }

    /// <summary>Requests adding a new version. This is also the default value of the struct.</summary>
    public static KeyReplacement New { get; } = new(0);

    /// <summary>Requests replacing the specified existing version.</summary>
    /// <param name="existingKvn">The existing key version, 0x01 through 0xFE.</param>
    /// <exception cref="ArgumentOutOfRangeException">The existing key version is 0x00 or 0xFF.</exception>
    public static KeyReplacement Of(byte existingKvn) => existingKvn is >= 0x01 and <= 0xFE
        ? new(existingKvn)
        : throw new ArgumentOutOfRangeException(nameof(existingKvn), "KVN must be 0x01–0xFE.");

    /// <summary>Gets whether this value requests adding a new version.</summary>
    public bool IsNew => Kvn == 0;
}