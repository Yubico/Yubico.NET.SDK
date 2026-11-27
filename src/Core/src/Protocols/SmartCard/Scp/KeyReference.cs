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
///     Carries an SCP key ID (KID) and key version number (KVN).
/// </summary>
/// <remarks>
///     Equality and encoding use the current KID and KVN. A zero-initialized reference encodes as 00 00,
///     not as an empty value or the factory SCP03 reference provided by <see cref="Default"/>.
/// </remarks>
public readonly record struct KeyReference(byte Kid, byte Kvn)
{
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
    ///     Encodes the current key reference into a new buffer.
    /// </summary>
    /// <returns>Two bytes in KID, KVN order.</returns>
    public ReadOnlyMemory<byte> AsMemory() => new byte[] { Kid, Kvn };

    /// <inheritdoc cref="AsMemory()"/>
    public ReadOnlySpan<byte> AsSpan() => AsMemory().Span;

    /// <summary>
    ///     Returns a string representation of this key reference.
    /// </summary>
    public override string ToString() => $"KeyRef{{kid=0x{Kid:X2}, kvn=0x{Kvn:X2}}}";
}