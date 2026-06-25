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

using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Control Reference Template (CRT) values used to identify key slots in OpenPGP commands.
/// </summary>
public static class Crt
{
    /// <summary>
    ///     CRT for the Signature key slot.
    /// </summary>
    public static ReadOnlyMemory<byte> Sig { get; } = BuildCrt(0xB6);

    /// <summary>
    ///     CRT for the Decryption key slot.
    /// </summary>
    public static ReadOnlyMemory<byte> Dec { get; } = BuildCrt(0xB8);

    /// <summary>
    ///     CRT for the Authentication key slot.
    /// </summary>
    public static ReadOnlyMemory<byte> Aut { get; } = BuildCrt(0xA4);

    /// <summary>
    ///     CRT for the Attestation key slot.
    /// </summary>
    public static ReadOnlyMemory<byte> Att { get; } = BuildAttCrt();

    private static byte[] BuildCrt(int tag)
    {
        using var tlv = new Tlv(tag, ReadOnlySpan<byte>.Empty);
        return tlv.AsSpan().ToArray();
    }

    private static byte[] BuildAttCrt()
    {
        using var inner = new Tlv(0x84, [0x81]);
        using var outer = new Tlv(0xB6, inner.Value.Span);
        return outer.AsSpan().ToArray();
    }
}