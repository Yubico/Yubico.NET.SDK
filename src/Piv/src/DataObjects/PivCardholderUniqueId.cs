// Copyright 2026 Yubico AB
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

using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Piv.DataObjects;

/// <summary>
/// Typed representation of the CHUID (CardHolder Unique Identifier) object
/// (<see cref="PivDataObject.Chuid"/>).
/// </summary>
/// <remarks>
/// <para>
/// A YubiKey's FASC-N and expiration date are fixed. The only field a caller can set is the
/// 16-byte GUID.
/// </para>
/// </remarks>
public readonly record struct PivCardholderUniqueId
{
    private const int GuidLength = 16;
    private const int FascNumberTag = 0x30;
    private const int GuidTag = 0x34;
    private const int ExpirationDateTag = 0x35;
    private const int SignatureTag = 0x3E;
    private const int LrcTag = 0xFE;

    private static readonly byte[] FixedFascNumberBytes =
    [
        0xd4, 0xe7, 0x39, 0xda, 0x73, 0x9c, 0xed, 0x39, 0xce, 0x73, 0x9d, 0x83, 0x68, 0x58, 0x21, 0x08,
        0x42, 0x10, 0x84, 0x21, 0xc8, 0x42, 0x10, 0xc3, 0xeb
    ];

    private static readonly byte[] FixedExpirationDateAscii = Encoding.ASCII.GetBytes("20300101");

    private readonly ReadOnlyMemory<byte> _guidValue;

    private PivCardholderUniqueId(ReadOnlyMemory<byte> guidValue)
    {
        _guidValue = guidValue;
    }

    /// <summary>Gets an empty <see cref="PivCardholderUniqueId"/>, representing no CHUID stored on the YubiKey.</summary>
    public static PivCardholderUniqueId Empty { get; } = default;

    /// <summary>Gets whether this instance represents no CHUID on the YubiKey.</summary>
    public bool IsEmpty => _guidValue.IsEmpty;

    /// <summary>Gets the fixed 25-byte FASC-N. Identical for every YubiKey.</summary>
    public ReadOnlyMemory<byte> FascNumber => FixedFascNumberBytes;

    /// <summary>Gets the 16-byte GUID.</summary>
    public ReadOnlyMemory<byte> GuidValue => _guidValue;

    /// <summary>Gets the fixed card expiration date (2030-01-01). Identical for every YubiKey.</summary>
    public DateOnly ExpirationDate => new(2030, 1, 1);

    /// <summary>
    /// Creates a new <see cref="PivCardholderUniqueId"/> with the given GUID.
    /// </summary>
    /// <param name="guidValue">The 16-byte GUID.</param>
    /// <exception cref="ArgumentException"><paramref name="guidValue"/> is not exactly 16 bytes.</exception>
    public static PivCardholderUniqueId Create(ReadOnlyMemory<byte> guidValue)
    {
        if (guidValue.Length != GuidLength)
        {
            throw new ArgumentException($"GUID must be exactly {GuidLength} bytes.", nameof(guidValue));
        }

        return new PivCardholderUniqueId(guidValue);
    }

    /// <summary>
    /// Creates a new <see cref="PivCardholderUniqueId"/> with a random 16-byte GUID.
    /// </summary>
    public static PivCardholderUniqueId CreateWithRandomGuid()
    {
        byte[] guid = new byte[GuidLength];
        RandomNumberGenerator.Fill(guid);
        return new PivCardholderUniqueId(guid);
    }

    /// <summary>
    /// Encodes this instance as the inner content of the CHUID object, matching the bytes
    /// returned by <see cref="IPivSession.GetObjectAsync"/> and expected by
    /// <see cref="IPivSession.PutObjectAsync"/> (i.e. without the outer <c>0x53</c> wrapper).
    /// </summary>
    public ReadOnlyMemory<byte> Encode()
    {
        if (IsEmpty)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        Tlv[] tlvs =
        [
            new Tlv(FascNumberTag, FixedFascNumberBytes),
            new Tlv(GuidTag, _guidValue.Span),
            new Tlv(ExpirationDateTag, FixedExpirationDateAscii),
            new Tlv(SignatureTag, ReadOnlySpan<byte>.Empty),
            new Tlv(LrcTag, ReadOnlySpan<byte>.Empty)
        ];

        return TlvHelper.EncodeAndDisposeList(tlvs).ToArray();
    }

    /// <summary>
    /// Attempts to decode the inner content of the CHUID object (as returned by
    /// <see cref="IPivSession.GetObjectAsync"/>) into a <see cref="PivCardholderUniqueId"/>.
    /// </summary>
    /// <param name="encodedData">The object data, without the outer <c>0x53</c> wrapper.</param>
    /// <param name="value">The decoded value on success, or <see cref="Empty"/> on failure.</param>
    /// <returns><see langword="true"/> if decoding succeeded; otherwise <see langword="false"/>.</returns>
    public static bool TryDecode(ReadOnlyMemory<byte> encodedData, out PivCardholderUniqueId value)
    {
        value = Empty;

        if (encodedData.IsEmpty)
        {
            return true;
        }

        try
        {
            using var list = TlvHelper.DecodeList(encodedData.Span);
            if (list.Count != 5)
            {
                return false;
            }

            if (list[0].Tag != FascNumberTag || !list[0].Value.Span.SequenceEqual(FixedFascNumberBytes))
            {
                return false;
            }

            if (list[1].Tag != GuidTag || list[1].Length != GuidLength)
            {
                return false;
            }

            if (list[2].Tag != ExpirationDateTag || !list[2].Value.Span.SequenceEqual(FixedExpirationDateAscii))
            {
                return false;
            }

            if (list[3].Tag != SignatureTag || list[3].Length != 0)
            {
                return false;
            }

            if (list[4].Tag != LrcTag || list[4].Length != 0)
            {
                return false;
            }

            value = Create(list[1].Value.ToArray());
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException)
        {
            // Truncated/malformed TLV data can throw IndexOutOfRangeException from the
            // underlying Tlv parser rather than ArgumentException; treat both as decode failure.
            return false;
        }
    }
}