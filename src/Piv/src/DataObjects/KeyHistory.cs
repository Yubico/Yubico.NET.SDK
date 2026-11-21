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
/// This fixed provisioning model follows the count and F3 rules of SP 800-73-4 Part 1 §3.3.3.
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
    /// The counts exceed 20 in total, the required URL is missing, a URL is supplied for two zero
    /// counts, or the URL does not match the profile's HTTP DNS-name and ASCII hexadecimal hash format.
    /// </exception>
    public static KeyHistory Create(byte onCardCertificates, byte offCardCertificates, Uri? offCardCertificateUrl = null)
    {
        if (onCardCertificates + offCardCertificates > 20)
            throw new ArgumentException("Key History can describe at most 20 retired keys.", nameof(offCardCertificates));
        if (offCardCertificateUrl is null ? offCardCertificates > 0
            : onCardCertificates + offCardCertificates == 0 || !IsProfileUrl(offCardCertificateUrl))
            throw new ArgumentException("Key History requires the profile's HTTP DNS-name and SHA-256 URL for off-card certificates, and no URL when both counts are zero.",
                nameof(offCardCertificateUrl));

        return new KeyHistory(onCardCertificates, offCardCertificates, offCardCertificateUrl);
    }

    private static bool IsProfileUrl(Uri url)
    {
        if (!url.IsAbsoluteUri || url.Scheme != Uri.UriSchemeHttp || url.HostNameType != UriHostNameType.Dns)
            return false;

        string text = url.OriginalString;
        string prefix = $"http://{url.Host}/";
        return text.Length <= MaximumUrlLength && text.Length == prefix.Length + 64
            && text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && IsAsciiDnsName(text.AsSpan(7, url.Host.Length))
            && text.AsSpan(prefix.Length).IndexOfAnyExcept("0123456789abcdefABCDEF") == -1;
    }

    private static bool IsAsciiDnsName(ReadOnlySpan<char> host)
    {
        foreach (var range in host.Split('.'))
        {
            var label = host[range];
            if (label.Length is 0 or > 63 || label[0] == '-' || label[^1] == '-')
                return false;
            foreach (char character in label)
            {
                if (!char.IsAsciiLetterOrDigit(character) && character != '-')
                    return false;
            }
        }

        return true;
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

        var onCard = new Tlv(OnCardTag, [OnCardCertificates]);
        var offCard = new Tlv(OffCardTag, [OffCardCertificates]);
        var unused = new Tlv(UnusedTag, ReadOnlySpan<byte>.Empty);
        Tlv[] tlvs = OffCardCertificateUrl is { } url
            ? [onCard, offCard, new Tlv(UrlTag, Encoding.ASCII.GetBytes(url.OriginalString)), unused]
            : [onCard, offCard, unused];

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
            if (list.Count is not (3 or 4)
                || !HasField(list[0], OnCardTag, 1)
                || !HasField(list[1], OffCardTag, 1)
                || !HasField(list[^1], UnusedTag, 0))
                return false;

            value = Create(list[0].Value.Span[0], list[1].Value.Span[0], list.Count == 4 ? DecodeUrl(list[2]) : null);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException or UriFormatException)
        {
            // Truncated/malformed TLV data can throw IndexOutOfRangeException from the
            // underlying Tlv parser rather than ArgumentException; treat both as decode failure.
            return false;
        }
    }

    private static bool HasField(Tlv field, int tag, int length) => field.Tag == tag && field.Length == length;

    private static Uri DecodeUrl(Tlv field) => field.Tag == UrlTag && field.Length is > 0 and <= MaximumUrlLength
        ? new Uri(Encoding.UTF8.GetString(field.Value.Span))
        : throw new ArgumentException("Invalid Key History F3 field.");
}
