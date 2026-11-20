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

using System.Text;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Piv.DataObjects;

/// <summary>
/// Typed representation of the Key History object (<see cref="DataObject.KeyHistory"/>).
/// </summary>
/// <remarks>
/// Callers maintain this record of retired key-management keys and their certificate locations.
/// The SDK emits/requires four TLVs, including F3 even for a null URL, and validates URL encoding
/// length rather than the complete SP 800-73-4 Part 1 §3.3.3 profile (retired-key counts and URL form).
/// Inspect broader valid layouts through IPivSession.GetObjectAsync. Encode returns a new nonsecret buffer.
/// </remarks>
public readonly record struct KeyHistory
{
    private const int MaximumUrlLength = 118;
    private const int OnCardTag = 0xC1;
    private const int OffCardTag = 0xC2;
    private const int UrlTag = 0xF3;
    private const int UnusedTag = 0xFE;

    private readonly bool _hasValue;

    private KeyHistory(byte onCardCertificates, byte offCardCertificates, Uri? offCardCertificateUrl)
    {
        OnCardCertificates = onCardCertificates;
        OffCardCertificates = offCardCertificates;
        OffCardCertificateUrl = offCardCertificateUrl;
        _hasValue = true;
    }

    /// <summary>Gets an empty <see cref="KeyHistory"/>, representing no Key History stored on the YubiKey.</summary>
    public static KeyHistory Empty { get; } = default;

    /// <summary>Gets whether this instance represents no Key History on the YubiKey.</summary>
    public bool IsEmpty => !_hasValue;

    /// <summary>Gets the recorded number of retired key-management keys with on-card certificates.</summary>
    public byte OnCardCertificates { get; }

    /// <summary>Gets the recorded number of retired key-management keys with off-card certificates.</summary>
    public byte OffCardCertificates { get; }

    /// <summary>Gets the URL where off-card certificates can be found, or <see langword="null"/> if none.</summary>
    public Uri? OffCardCertificateUrl { get; }

    /// <summary>
    /// Creates a new <see cref="KeyHistory"/> with the given field values.
    /// </summary>
    /// <param name="onCardCertificates">The number of keys with on-card certificates.</param>
    /// <param name="offCardCertificates">The number of keys with off-card certificates.</param>
    /// <param name="offCardCertificateUrl">The off-card certificate URL, or <see langword="null"/>.</param>
    /// <exception cref="ArgumentException">
    /// The UTF-8 encoding of <paramref name="offCardCertificateUrl"/> exceeds 118 bytes.
    /// </exception>
    public static KeyHistory Create(byte onCardCertificates, byte offCardCertificates, Uri? offCardCertificateUrl = null)
    {
        if (offCardCertificateUrl is not null)
        {
            int byteCount = Encoding.UTF8.GetByteCount(offCardCertificateUrl.AbsoluteUri);
            if (byteCount > MaximumUrlLength)
            {
                throw new ArgumentException(
                    $"Off-card certificate URL must encode to {MaximumUrlLength} bytes or fewer.",
                    nameof(offCardCertificateUrl));
            }
        }

        return new KeyHistory(onCardCertificates, offCardCertificates, offCardCertificateUrl);
    }

    /// <summary>
    /// Encodes this instance as the inner content of the Key History object, matching the bytes
    /// returned by <see cref="IPivSession.GetObjectAsync"/> and expected by
    /// <see cref="IPivSession.PutObjectAsync"/> (i.e. without the outer <c>0x53</c> wrapper).
    /// </summary>
    public ReadOnlyMemory<byte> Encode()
    {
        if (IsEmpty)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        byte[] urlBytes = OffCardCertificateUrl is null
            ? []
            : Encoding.UTF8.GetBytes(OffCardCertificateUrl.AbsoluteUri);

        Tlv[] tlvs =
        [
            new Tlv(OnCardTag, [OnCardCertificates]),
            new Tlv(OffCardTag, [OffCardCertificates]),
            new Tlv(UrlTag, urlBytes),
            new Tlv(UnusedTag, ReadOnlySpan<byte>.Empty)
        ];

        return TlvHelper.EncodeAndDisposeList(tlvs).ToArray();
    }

    /// <summary>
    /// Attempts to decode the inner content of the Key History object (as returned by
    /// <see cref="IPivSession.GetObjectAsync"/>) into a <see cref="KeyHistory"/>.
    /// </summary>
    /// <param name="encodedData">The object data, without the outer <c>0x53</c> wrapper.</param>
    /// <param name="value">The decoded value on success, or <see cref="Empty"/> on failure.</param>
    /// <returns><see langword="true"/> if decoding succeeded; otherwise <see langword="false"/>.</returns>
    public static bool TryDecode(ReadOnlyMemory<byte> encodedData, out KeyHistory value)
    {
        value = Empty;

        if (encodedData.IsEmpty)
        {
            return true;
        }

        try
        {
            using var list = TlvHelper.DecodeList(encodedData.Span);
            if (list.Count != 4)
            {
                return false;
            }

            if (list[0].Tag != OnCardTag || list[0].Length != 1)
            {
                return false;
            }

            if (list[1].Tag != OffCardTag || list[1].Length != 1)
            {
                return false;
            }

            if (list[2].Tag != UrlTag)
            {
                return false;
            }

            if (list[3].Tag != UnusedTag || list[3].Length != 0)
            {
                return false;
            }

            byte onCard = list[0].Value.Span[0];
            byte offCard = list[1].Value.Span[0];
            Uri? url = list[2].Length > 0
                ? new Uri(Encoding.UTF8.GetString(list[2].Value.Span))
                : null;

            value = Create(onCard, offCard, url);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException)
        {
            // Truncated/malformed TLV data can throw IndexOutOfRangeException from the
            // underlying Tlv parser rather than ArgumentException; treat both as decode failure.
            return false;
        }
        catch (UriFormatException)
        {
            return false;
        }
    }
}