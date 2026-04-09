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

using System.Buffers.Binary;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Hash algorithms supported by the OpenPGP KDF.
/// </summary>
public enum KdfHashAlgorithm : byte
{
    Sha256 = 0x08,
    Sha512 = 0x0A,
}

/// <summary>
///     Base class for OpenPGP Key Derivation Function (KDF) configuration.
///     Dispatches parsing to <see cref="KdfNone" /> or <see cref="KdfIterSaltedS2k" />.
/// </summary>
public abstract class Kdf
{
    /// <summary>
    ///     The KDF algorithm identifier.
    /// </summary>
    public abstract int Algorithm { get; }

    /// <summary>
    ///     Derives PIN bytes from the given UTF-8 PIN bytes for the specified PIN type.
    /// </summary>
    /// <param name="pw">The PIN type (User, Reset, or Admin).</param>
    /// <param name="pinUtf8Bytes">The PIN as UTF-8 encoded bytes.</param>
    /// <returns>The derived PIN bytes.</returns>
    public abstract byte[] Process(Pw pw, ReadOnlySpan<byte> pinUtf8Bytes);

    /// <summary>
    ///     Serializes the KDF configuration to its wire format (concatenated TLVs).
    /// </summary>
    public abstract byte[] ToBytes();

    /// <summary>
    ///     Parses a KDF configuration from the encoded TLV data (DO 0xF9).
    /// </summary>
    public static Kdf Parse(ReadOnlySpan<byte> encoded)
    {
        var data = TlvHelper.DecodeDictionary(encoded);

        if (data.TryGetValue(0x81, out var algorithmData) && algorithmData.Length > 0)
        {
            var algorithm = algorithmData.Span[0];
            if (algorithm == KdfIterSaltedS2k.KdfAlgorithm)
            {
                return KdfIterSaltedS2k.ParseData(data);
            }
        }

        return new KdfNone();
    }
}

/// <summary>
///     No KDF applied — PINs are sent as raw UTF-8 bytes.
/// </summary>
public sealed class KdfNone : Kdf
{
    /// <inheritdoc />
    public override int Algorithm => 0;

    /// <inheritdoc />
    public override byte[] Process(Pw pw, ReadOnlySpan<byte> pinUtf8Bytes) =>
        pinUtf8Bytes.ToArray();

    /// <inheritdoc />
    public override byte[] ToBytes()
    {
        using var tlv = new Tlv(0x81, [(byte)Algorithm]);
        return tlv.AsMemory().ToArray();
    }
}

/// <summary>
///     Iterated-Salted-S2K key derivation as defined in OpenPGP (RFC 4880 §3.7.1.3).
/// </summary>
/// <remarks>
///     <para>
///         IMPORTANT: The <c>IterationCount</c> is the total number of BYTES to feed into the
///         hash function, NOT the number of rounds. The salt+pin data is repeated until the
///         byte count is reached, then the hash is finalized once.
///     </para>
///     <para>
///         This is NOT PBKDF2. The algorithm is:
///         <code>
///         data = salt + pin_bytes
///         (data_count, trailing) = divmod(iteration_count, len(data))
///         feed data data_count times into hash
///         feed data[:trailing] into hash
///         return hash.finalize()
///         </code>
///     </para>
/// </remarks>
public sealed class KdfIterSaltedS2k : Kdf
{
    internal const int KdfAlgorithm = 3;

    /// <inheritdoc />
    public override int Algorithm => KdfAlgorithm;

    /// <summary>
    ///     The hash algorithm used for key derivation.
    /// </summary>
    public KdfHashAlgorithm HashAlgorithm { get; init; }

    /// <summary>
    ///     The total number of bytes to feed into the hash function.
    /// </summary>
    public int IterationCount { get; init; }

    /// <summary>
    ///     8-byte salt for the User PIN.
    /// </summary>
    public ReadOnlyMemory<byte> SaltUser { get; init; }

    /// <summary>
    ///     8-byte salt for the Reset Code. Null if not configured.
    /// </summary>
    public ReadOnlyMemory<byte>? SaltReset { get; init; }

    /// <summary>
    ///     8-byte salt for the Admin PIN. Null if not configured.
    /// </summary>
    public ReadOnlyMemory<byte>? SaltAdmin { get; init; }

    /// <summary>
    ///     Pre-computed hash of the default User PIN. Null if not stored.
    /// </summary>
    public ReadOnlyMemory<byte>? InitialHashUser { get; init; }

    /// <summary>
    ///     Pre-computed hash of the default Admin PIN. Null if not stored.
    /// </summary>
    public ReadOnlyMemory<byte>? InitialHashAdmin { get; init; }

    /// <summary>
    ///     Gets the salt for the specified PIN type. Falls back to <see cref="SaltUser" />
    ///     if the specific salt is not available.
    /// </summary>
    public ReadOnlyMemory<byte> GetSalt(Pw pw) =>
        pw switch
        {
            Pw.User => SaltUser,
            Pw.Reset => SaltReset ?? SaltUser,
            Pw.Admin => SaltAdmin ?? SaltUser,
            _ => SaltUser,
        };

    /// <inheritdoc />
    public override byte[] Process(Pw pw, ReadOnlySpan<byte> pinUtf8Bytes)
    {
        var salt = GetSalt(pw);

        // data = salt + pinUtf8Bytes
        var data = new byte[salt.Length + pinUtf8Bytes.Length];
        salt.Span.CopyTo(data);
        pinUtf8Bytes.CopyTo(data.AsSpan(salt.Length));

        try
        {
            return DoProcess(HashAlgorithm, IterationCount, data);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(data);
        }
    }

    /// <summary>
    ///     Performs the iterated-salted-S2K hash computation.
    /// </summary>
    internal static byte[] DoProcess(KdfHashAlgorithm hashAlgorithm, int iterationCount, ReadOnlySpan<byte> data)
    {
        var (dataCount, trailing) = Math.DivRem(iterationCount, data.Length);

        using var incrementalHash = hashAlgorithm switch
        {
            KdfHashAlgorithm.Sha256 => IncrementalHash.CreateHash(HashAlgorithmName.SHA256),
            KdfHashAlgorithm.Sha512 => IncrementalHash.CreateHash(HashAlgorithmName.SHA512),
            _ => throw new ArgumentOutOfRangeException(nameof(hashAlgorithm)),
        };

        for (var i = 0; i < dataCount; i++)
        {
            incrementalHash.AppendData(data);
        }

        if (trailing > 0)
        {
            incrementalHash.AppendData(data[..trailing]);
        }

        return incrementalHash.GetHashAndReset();
    }

    /// <inheritdoc />
    public override byte[] ToBytes()
    {
        var tlvs = new List<Tlv>();
        try
        {
            tlvs.Add(new Tlv(0x81, [(byte)Algorithm]));
            tlvs.Add(new Tlv(0x82, [(byte)HashAlgorithm]));

            Span<byte> iterBytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(iterBytes, (uint)IterationCount);
            tlvs.Add(new Tlv(0x83, iterBytes));

            tlvs.Add(new Tlv(0x84, SaltUser.Span));

            if (SaltReset is { } saltReset)
            {
                tlvs.Add(new Tlv(0x85, saltReset.Span));
            }

            if (SaltAdmin is { } saltAdmin)
            {
                tlvs.Add(new Tlv(0x86, saltAdmin.Span));
            }

            if (InitialHashUser is { } hashUser)
            {
                tlvs.Add(new Tlv(0x87, hashUser.Span));
            }

            if (InitialHashAdmin is { } hashAdmin)
            {
                tlvs.Add(new Tlv(0x88, hashAdmin.Span));
            }

            return TlvHelper.EncodeList([.. tlvs]).ToArray();
        }
        finally
        {
            foreach (var tlv in tlvs)
            {
                tlv.Dispose();
            }
        }
    }

    internal static KdfIterSaltedS2k ParseData(IDictionary<int, ReadOnlyMemory<byte>> data) =>
        new()
        {
            HashAlgorithm = (KdfHashAlgorithm)data[0x82].Span[0],
            IterationCount = (int)BinaryPrimitives.ReadUInt32BigEndian(data[0x83].Span),
            SaltUser = data[0x84].ToArray(),
            SaltReset = data.TryGetValue(0x85, out var sr) ? sr.ToArray() : null,
            SaltAdmin = data.TryGetValue(0x86, out var sa) ? sa.ToArray() : null,
            InitialHashUser = data.TryGetValue(0x87, out var hu) ? hu.ToArray() : null,
            InitialHashAdmin = data.TryGetValue(0x88, out var ha) ? ha.ToArray() : null,
        };
}