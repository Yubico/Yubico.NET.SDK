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
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Piv.DataObjects;

/// <summary>
/// Typed representation of the CCC (Card Capability Container) object
/// (<see cref="PivDataObject.Capability"/>).
/// </summary>
/// <remarks>
/// Most CCC fields are fixed by the PIV standard / Yubico. The only field a caller can set is the
/// 14-byte Card Identifier portion of the Unique Card Identifier.
/// </remarks>
public readonly record struct PivCardCapabilityContainer
{
    private const int AidLength = 7;
    private const int CardIdLength = 14;
    private const int UniqueCardIdLength = AidLength + CardIdLength;

    private const int UniqueCardIdTag = 0xF0;
    private const int ContainerVersionTag = 0xF1;
    private const int GrammarVersionTag = 0xF2;
    private const int UnusedTag1 = 0xF3;
    private const int Pkcs15Tag = 0xF4;
    private const int DataModelTag = 0xF5;
    private const int UnusedTag2 = 0xF6;
    private const int UnusedTag3 = 0xF7;
    private const int UnusedTag4 = 0xFA;
    private const int UnusedTag5 = 0xFB;
    private const int UnusedTag6 = 0xFC;
    private const int UnusedTag7 = 0xFD;
    private const int UnusedTag8 = 0xFE;

    private const byte FixedContainerVersionNumber = 0x21;
    private const byte FixedGrammarVersionNumber = 0x21;
    private const byte FixedPkcs15VersionNumber = 0x00;
    private const byte FixedDataModelNumber = 0x10;

    private static readonly byte[] FixedApplicationIdentifierBytes = [0xA0, 0x00, 0x00, 0x01, 0x16, 0xFF, 0x02];

    // (tag, expected length [0 or 1], expected value when length is 1)
    private static readonly (int Tag, int Length, byte Value)[] FixedFields =
    [
        (ContainerVersionTag, 1, FixedContainerVersionNumber),
        (GrammarVersionTag, 1, FixedGrammarVersionNumber),
        (UnusedTag1, 0, 0),
        (Pkcs15Tag, 1, FixedPkcs15VersionNumber),
        (DataModelTag, 1, FixedDataModelNumber),
        (UnusedTag2, 0, 0),
        (UnusedTag3, 0, 0),
        (UnusedTag4, 0, 0),
        (UnusedTag5, 0, 0),
        (UnusedTag6, 0, 0),
        (UnusedTag7, 0, 0),
        (UnusedTag8, 0, 0)
    ];

    private readonly ReadOnlyMemory<byte> _cardIdentifier;

    private PivCardCapabilityContainer(ReadOnlyMemory<byte> cardIdentifier)
    {
        _cardIdentifier = cardIdentifier;
    }

    /// <summary>Gets an empty <see cref="PivCardCapabilityContainer"/>, representing no CCC stored on the YubiKey.</summary>
    public static PivCardCapabilityContainer Empty { get; } = default;

    /// <summary>Gets whether this instance represents no CCC on the YubiKey.</summary>
    public bool IsEmpty => _cardIdentifier.IsEmpty;

    /// <summary>Gets the 14-byte Card Identifier portion of the Unique Card Identifier.</summary>
    public ReadOnlyMemory<byte> CardIdentifier => _cardIdentifier;

    /// <summary>Gets the fixed 7-byte Application Identifier (GSC-RID || Manufacturer ID || Card Type).</summary>
    public ReadOnlyMemory<byte> ApplicationIdentifier => FixedApplicationIdentifierBytes;

    /// <summary>Gets the fixed CCC container version number (2.1).</summary>
    public byte ContainerVersionNumber => FixedContainerVersionNumber;

    /// <summary>Gets the fixed CCC grammar version number (2.1).</summary>
    public byte GrammarVersionNumber => FixedGrammarVersionNumber;

    /// <summary>Gets the fixed PKCS #15 version (0x00; not supported).</summary>
    public byte Pkcs15Version => FixedPkcs15VersionNumber;

    /// <summary>Gets the fixed PIV data model number (0x10).</summary>
    public byte DataModelNumber => FixedDataModelNumber;

    /// <summary>
    /// Creates a new <see cref="PivCardCapabilityContainer"/> with the given Card Identifier.
    /// </summary>
    /// <param name="cardIdentifier">The 14-byte Card Identifier.</param>
    /// <exception cref="ArgumentException"><paramref name="cardIdentifier"/> is not exactly 14 bytes.</exception>
    public static PivCardCapabilityContainer Create(ReadOnlyMemory<byte> cardIdentifier)
    {
        if (cardIdentifier.Length != CardIdLength)
        {
            throw new ArgumentException($"Card Identifier must be exactly {CardIdLength} bytes.", nameof(cardIdentifier));
        }

        return new PivCardCapabilityContainer(cardIdentifier);
    }

    /// <summary>
    /// Creates a new <see cref="PivCardCapabilityContainer"/> with a random 14-byte Card Identifier.
    /// </summary>
    public static PivCardCapabilityContainer CreateWithRandomCardId()
    {
        byte[] cardId = new byte[CardIdLength];
        RandomNumberGenerator.Fill(cardId);
        return new PivCardCapabilityContainer(cardId);
    }

    /// <summary>
    /// Encodes this instance as the inner content of the CCC object, matching the bytes returned by
    /// <see cref="IPivSession.GetObjectAsync"/> and expected by <see cref="IPivSession.PutObjectAsync"/>
    /// (i.e. without the outer <c>0x53</c> wrapper).
    /// </summary>
    public ReadOnlyMemory<byte> Encode()
    {
        if (IsEmpty)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        Span<byte> uniqueCardId = stackalloc byte[UniqueCardIdLength];
        FixedApplicationIdentifierBytes.CopyTo(uniqueCardId);
        _cardIdentifier.Span.CopyTo(uniqueCardId[AidLength..]);

        var tlvs = new Tlv[1 + FixedFields.Length];
        tlvs[0] = new Tlv(UniqueCardIdTag, uniqueCardId);
        for (int i = 0; i < FixedFields.Length; i++)
        {
            var (tag, length, value) = FixedFields[i];
            tlvs[i + 1] = length == 0
                ? new Tlv(tag, ReadOnlySpan<byte>.Empty)
                : new Tlv(tag, [value]);
        }

        return TlvHelper.EncodeAndDisposeList(tlvs).ToArray();
    }

    /// <summary>
    /// Attempts to decode the inner content of the CCC object (as returned by
    /// <see cref="IPivSession.GetObjectAsync"/>) into a <see cref="PivCardCapabilityContainer"/>.
    /// </summary>
    /// <param name="encodedData">The object data, without the outer <c>0x53</c> wrapper.</param>
    /// <param name="value">The decoded value on success, or <see cref="Empty"/> on failure.</param>
    /// <returns><see langword="true"/> if decoding succeeded; otherwise <see langword="false"/>.</returns>
    public static bool TryDecode(ReadOnlyMemory<byte> encodedData, out PivCardCapabilityContainer value)
    {
        value = Empty;

        if (encodedData.IsEmpty)
        {
            return true;
        }

        try
        {
            using var list = TlvHelper.DecodeList(encodedData.Span);
            if (list.Count != 1 + FixedFields.Length)
            {
                return false;
            }

            if (list[0].Tag != UniqueCardIdTag || list[0].Length != UniqueCardIdLength)
            {
                return false;
            }

            if (!list[0].Value.Span[..AidLength].SequenceEqual(FixedApplicationIdentifierBytes))
            {
                return false;
            }

            for (int i = 0; i < FixedFields.Length; i++)
            {
                var (tag, length, expectedValue) = FixedFields[i];
                var tlv = list[i + 1];
                if (tlv.Tag != tag || tlv.Length != length)
                {
                    return false;
                }

                if (length == 1 && tlv.Value.Span[0] != expectedValue)
                {
                    return false;
                }
            }

            value = Create(list[0].Value[AidLength..].ToArray());
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
