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

namespace Yubico.YubiKit.Core.Protocols.SmartCard.Scp;

/// <summary>
///     Carries an SCP key ID (KID), key version number (KVN), and cached constructor encoding.
/// </summary>
/// <remarks>
///     Construct a new value when changing the reference. Init-property changes, including with-expressions,
///     leave the cached encoding unchanged; default(KeyReference) has empty encoding. Generated equality also
///     includes that cached memory, so separately constructed values are not a content-key comparison contract.
/// </remarks>
public readonly record struct KeyReference(byte Kid, byte Kvn)
{
    private readonly Memory<byte> _bytes = new byte[] { Kid, Kvn };

    /// <summary>
    ///     Gets the Key ID.
    /// </summary>
    public byte Kid { get; init; } = Kid;

    /// <summary>
    ///     Gets the Key Version Number.
    /// </summary>
    public byte Kvn { get; init; } = Kvn;

    public static KeyReference Default => new(0x01, 0xFF);

    /// <summary>
    ///     Returns a view of the cached constructor encoding.
    /// </summary>
    /// <returns>Constructor bytes [Kid, Kvn], or empty memory for the default value.</returns>
    public ReadOnlyMemory<byte> AsMemory() => _bytes;

    public ReadOnlySpan<byte> AsSpan() => _bytes.Span;

    /// <summary>
    ///     Returns a string representation of this key reference.
    /// </summary>
    public override string ToString() => $"KeyRef{{kid=0x{Kid:X2}, kvn=0x{Kvn:X2}}}";
}