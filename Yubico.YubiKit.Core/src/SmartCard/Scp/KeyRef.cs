// Copyright (C) 2024 Yubico.
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

namespace Yubico.YubiKit.Core.SmartCard.Scp;

/// <summary>
///     Reference to an SCP key, uniquely identified by KID (Key ID) and KVN (Key Version Number).
/// </summary>
public readonly record struct KeyRef(byte Kid, byte Kvn)
{
    /// <summary>
    ///     Gets the Key ID.
    /// </summary>
    public byte Kid { get; init; } = Kid;

    /// <summary>
    ///     Gets the Key Version Number.
    /// </summary>
    public byte Kvn { get; init; } = Kvn;

    /// <summary>
    ///     Returns a byte array representation of this key reference.
    /// </summary>
    /// <returns>A two-byte array containing [Kid, Kvn].</returns>
    public byte[] GetBytes() => [Kid, Kvn];

    /// <summary>
    ///     Returns a string representation of this key reference.
    /// </summary>
    public override string ToString() => $"KeyRef{{kid=0x{Kid:X2}, kvn=0x{Kvn:X2}}}";
}