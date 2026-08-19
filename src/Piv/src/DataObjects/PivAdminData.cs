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

using System.Buffers.Binary;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Piv.DataObjects;

/// <summary>
/// Typed representation of the Yubico ADMIN DATA object (<see cref="PivDataObject.AdminData"/>).
/// </summary>
/// <remarks>
/// <para>
/// ADMIN DATA records whether the PIV management key is PIN-protected and/or PIN-derived, along
/// with the salt used for PIN-derivation and the date the PIN was last changed. It is used by
/// <see cref="IPivSession.GetPinOnlyModeAsync"/>, <see cref="IPivSession.SetPinOnlyModeAsync"/>,
/// and <see cref="IPivSession.RecoverPinOnlyModeAsync"/>.
/// </para>
/// <para>
/// The salt is not secret; it is only used to derive a management key from a PIN and is safe to
/// read and write like any other PIV data object field.
/// </para>
/// </remarks>
public readonly record struct PivAdminData
{
    private const int SaltLength = 16;
    private const int EncodingTag = 0x80;
    private const int BitFieldTag = 0x81;
    private const int SaltTag = 0x82;
    private const int DateTag = 0x83;
    private const byte PukBlockedBit = 1;
    private const byte PinProtectedBit = 2;

    private readonly bool _hasValue;

    private PivAdminData(bool pukBlocked, bool pinProtected, ReadOnlyMemory<byte>? salt, DateTimeOffset? pinLastUpdated)
    {
        PukBlocked = pukBlocked;
        PinProtected = pinProtected;
        Salt = salt;
        PinLastUpdated = pinLastUpdated;
        _hasValue = true;
    }

    /// <summary>Gets an empty <see cref="PivAdminData"/>, representing no data stored on the YubiKey.</summary>
    public static PivAdminData Empty { get; } = default;

    /// <summary>Gets whether this instance represents no data on the YubiKey.</summary>
    public bool IsEmpty => !_hasValue;

    /// <summary>Gets whether the PUK has been blocked (expected when a PIN-only mode is set).</summary>
    public bool PukBlocked { get; }

    /// <summary>Gets whether the management key is PIN-protected (stored in the PRINTED object).</summary>
    public bool PinProtected { get; }

    /// <summary>Gets the 16-byte salt used to derive the management key from the PIN, or <see langword="null"/> if not PIN-derived.</summary>
    public ReadOnlyMemory<byte>? Salt { get; }

    /// <summary>Gets the date the PIN was last updated, or <see langword="null"/> if not recorded.</summary>
    public DateTimeOffset? PinLastUpdated { get; }

    /// <summary>
    /// Creates a new <see cref="PivAdminData"/> with the given field values.
    /// </summary>
    /// <param name="pukBlocked">Whether the PUK has been blocked.</param>
    /// <param name="pinProtected">Whether the management key is PIN-protected.</param>
    /// <param name="salt">The 16-byte PIN-derivation salt, or <see langword="null"/>.</param>
    /// <param name="pinLastUpdated">The date the PIN was last updated, or <see langword="null"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="salt"/> is not exactly 16 bytes.</exception>
    public static PivAdminData Create(
        bool pukBlocked,
        bool pinProtected,
        ReadOnlyMemory<byte>? salt = null,
        DateTimeOffset? pinLastUpdated = null)
    {
        if (salt is { Length: not SaltLength })
        {
            throw new ArgumentException($"Salt must be exactly {SaltLength} bytes.", nameof(salt));
        }

        return new PivAdminData(pukBlocked, pinProtected, salt, pinLastUpdated);
    }

    /// <summary>
    /// Encodes this instance as the inner content of the ADMIN DATA object, matching the bytes
    /// returned by <see cref="IPivSession.GetObjectAsync"/> and expected by
    /// <see cref="IPivSession.PutObjectAsync"/> (i.e. without the outer <c>0x53</c> wrapper).
    /// </summary>
    public ReadOnlyMemory<byte> Encode()
    {
        if (IsEmpty)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        byte bitField = 0;
        if (PukBlocked)
        {
            bitField |= PukBlockedBit;
        }
        if (PinProtected)
        {
            bitField |= PinProtectedBit;
        }

        var children = new List<Tlv> { new(BitFieldTag, [bitField]) };
        if (Salt is { } salt)
        {
            children.Add(new Tlv(SaltTag, salt.Span));
        }
        if (PinLastUpdated is { } updated)
        {
            children.Add(new Tlv(DateTag, EncodeUnixTimeTrimmed(updated)));
        }

        ReadOnlyMemory<byte> inner;
        try
        {
            inner = TlvHelper.EncodeList([.. children]);
        }
        finally
        {
            foreach (var child in children)
            {
                child.Dispose();
            }
        }

        using var outer = new Tlv(EncodingTag, inner.Span);
        return outer.AsMemory().ToArray();
    }

    /// <summary>
    /// Attempts to decode the inner content of the ADMIN DATA object (as returned by
    /// <see cref="IPivSession.GetObjectAsync"/>) into a <see cref="PivAdminData"/>.
    /// </summary>
    /// <param name="encodedData">The object data, without the outer <c>0x53</c> wrapper.</param>
    /// <param name="value">The decoded value on success, or <see cref="Empty"/> on failure.</param>
    /// <returns><see langword="true"/> if decoding succeeded; otherwise <see langword="false"/>.</returns>
    public static bool TryDecode(ReadOnlyMemory<byte> encodedData, out PivAdminData value)
    {
        value = Empty;

        if (encodedData.IsEmpty)
        {
            return true;
        }

        try
        {
            using var outer = Tlv.Create(encodedData.Span);
            if (outer.Tag != EncodingTag)
            {
                return false;
            }

            // Iterate sequentially (rather than via TlvHelper.DecodeDictionary) so duplicate and
            // unrecognized tags can be explicitly rejected, matching v1 AdminData.TryDecode's
            // strictness (its elementsRead XOR check rejects repeats; its tag switch has a
            // `_ => false` default for anything that isn't the bit field/salt/date elements).
            using var children = TlvHelper.DecodeList(outer.Value.Span);

            bool sawBitField = false;
            bool sawSalt = false;
            bool sawDate = false;

            bool pukBlocked = false;
            bool pinProtected = false;
            ReadOnlyMemory<byte>? salt = null;
            DateTimeOffset? pinLastUpdated = null;
            Span<byte> dateBuffer = stackalloc byte[8];

            foreach (var child in children)
            {
                switch (child.Tag)
                {
                    case BitFieldTag:
                        if (sawBitField || child.Length != 1)
                        {
                            return false;
                        }

                        sawBitField = true;
                        pukBlocked = (child.Value.Span[0] & PukBlockedBit) != 0;
                        pinProtected = (child.Value.Span[0] & PinProtectedBit) != 0;
                        break;

                    case SaltTag:
                        if (sawSalt || child.Length != SaltLength)
                        {
                            return false;
                        }

                        sawSalt = true;
                        salt = child.Value.ToArray();
                        break;

                    case DateTag:
                        if (sawDate || child.Length > 8)
                        {
                            return false;
                        }

                        sawDate = true;
                        if (child.Length > 0)
                        {
                            dateBuffer.Clear();
                            child.Value.Span.CopyTo(dateBuffer);
                            long unixSeconds = BinaryPrimitives.ReadInt64LittleEndian(dateBuffer);
                            pinLastUpdated = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
                        }
                        break;

                    default:
                        // Unrecognized element inside ADMIN DATA - reject rather than silently ignore.
                        return false;
                }
            }

            value = new PivAdminData(pukBlocked, pinProtected, salt, pinLastUpdated);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException)
        {
            // Truncated/malformed TLV data can throw IndexOutOfRangeException from the
            // underlying Tlv parser rather than ArgumentException; treat both as decode failure.
            return false;
        }
    }

    private static byte[] EncodeUnixTimeTrimmed(DateTimeOffset value)
    {
        long unixSeconds = value.ToUnixTimeSeconds();
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, unchecked((ulong)unixSeconds));

        int lastNonZero = -1;
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] != 0)
            {
                lastNonZero = i;
            }
        }

        int length = lastNonZero < 0 ? 1 : lastNonZero + 1;
        return buffer[..length].ToArray();
    }
}