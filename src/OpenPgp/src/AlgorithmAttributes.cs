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

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Base class for OpenPGP key algorithm attributes. Dispatches parsing to
///     <see cref="RsaAttributes" /> or <see cref="EcAttributes" /> based on the algorithm ID.
/// </summary>
public abstract class AlgorithmAttributes
{
    /// <summary>
    ///     The algorithm identifier byte.
    ///     RSA = 0x01, ECDH = 0x12, ECDSA = 0x13, EdDSA = 0x16.
    /// </summary>
    public int AlgorithmId { get; protected init; }

    /// <summary>
    ///     Serializes the algorithm attributes to their wire format.
    /// </summary>
    public abstract byte[] ToBytes();

    /// <summary>
    ///     Parses algorithm attributes from the encoded wire format.
    /// </summary>
    /// <param name="encoded">The raw algorithm attributes bytes.</param>
    /// <exception cref="ArgumentException">Thrown when the algorithm ID is not recognized.</exception>
    public static AlgorithmAttributes Parse(ReadOnlySpan<byte> encoded)
    {
        var algorithmId = encoded[0];
        var data = encoded[1..];

        return algorithmId switch
        {
            RsaAttributes.RsaAlgorithmId => RsaAttributes.ParseData(algorithmId, data),
            EcAttributes.EcdhAlgorithmId or
            EcAttributes.EcdsaAlgorithmId or
            EcAttributes.EddsaAlgorithmId => EcAttributes.ParseData(algorithmId, data),
            _ => throw new ArgumentException($"Unsupported algorithm ID: 0x{algorithmId:X2}.", nameof(encoded)),
        };
    }
}

/// <summary>
///     RSA algorithm attributes containing modulus length, exponent length, and import format.
/// </summary>
public sealed class RsaAttributes : AlgorithmAttributes
{
    internal const int RsaAlgorithmId = 0x01;

    /// <summary>
    ///     RSA modulus length in bits (e.g., 2048, 3072, 4096).
    /// </summary>
    public int NLen { get; init; }

    /// <summary>
    ///     RSA public exponent length in bits (typically 17, meaning e=65537).
    /// </summary>
    public int ELen { get; init; }

    /// <summary>
    ///     The import format for this RSA key.
    /// </summary>
    public RsaImportFormat ImportFormat { get; init; }

    /// <summary>
    ///     Creates RSA attributes with the specified key size and import format.
    /// </summary>
    public static RsaAttributes Create(
        RsaSize keySize,
        RsaImportFormat importFormat = RsaImportFormat.Standard) =>
        new()
        {
            AlgorithmId = RsaAlgorithmId,
            NLen = (int)keySize,
            ELen = 17,
            ImportFormat = importFormat,
        };

    internal static RsaAttributes ParseData(int algorithmId, ReadOnlySpan<byte> data)
    {
        // Format: 2 bytes nLen + 2 bytes eLen + 1 byte importFormat
        var nLen = BinaryPrimitives.ReadUInt16BigEndian(data[..2]);
        var eLen = BinaryPrimitives.ReadUInt16BigEndian(data[2..4]);
        var importFormat = (RsaImportFormat)data[4];

        return new RsaAttributes
        {
            AlgorithmId = algorithmId,
            NLen = nLen,
            ELen = eLen,
            ImportFormat = importFormat,
        };
    }

    /// <inheritdoc />
    public override byte[] ToBytes()
    {
        var result = new byte[6];
        result[0] = (byte)AlgorithmId;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(1, 2), (ushort)NLen);
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(3, 2), (ushort)ELen);
        result[5] = (byte)ImportFormat;
        return result;
    }
}

/// <summary>
///     Elliptic curve algorithm attributes containing the curve OID and import format.
/// </summary>
public sealed class EcAttributes : AlgorithmAttributes
{
    internal const int EcdhAlgorithmId = 0x12;
    internal const int EcdsaAlgorithmId = 0x13;
    internal const int EddsaAlgorithmId = 0x16;

    /// <summary>
    ///     The elliptic curve used by this key.
    /// </summary>
    public CurveOid Oid { get; init; }

    /// <summary>
    ///     The import format for this EC key.
    /// </summary>
    public EcImportFormat ImportFormat { get; init; }

    /// <summary>
    ///     Creates EC attributes for the given key slot and curve.
    ///     The algorithm ID is automatically determined:
    ///     Ed25519 → EdDSA (0x16), X25519 → ECDH (0x12), DEC slot → ECDH (0x12), others → ECDSA (0x13).
    /// </summary>
    public static EcAttributes Create(KeyRef keyRef, CurveOid oid) =>
        new()
        {
            AlgorithmId = oid switch
            {
                CurveOid.Ed25519 => EddsaAlgorithmId,
                CurveOid.X25519 => EcdhAlgorithmId,
                _ => keyRef == KeyRef.Dec ? EcdhAlgorithmId : EcdsaAlgorithmId,
            },
            Oid = oid,
            ImportFormat = EcImportFormat.Standard,
        };

    internal static EcAttributes ParseData(int algorithmId, ReadOnlySpan<byte> data)
    {
        EcImportFormat importFormat;
        ReadOnlySpan<byte> oidBytes;

        if (data.Length > 0 && data[^1] == (byte)EcImportFormat.StandardWithPubkey)
        {
            importFormat = EcImportFormat.StandardWithPubkey;
            oidBytes = data[..^1];
        }
        else
        {
            importFormat = EcImportFormat.Standard;
            oidBytes = data;
        }

        var oid = CurveOidExtensions.FromOidBytes(oidBytes);

        return new EcAttributes
        {
            AlgorithmId = algorithmId,
            Oid = oid,
            ImportFormat = importFormat,
        };
    }

    /// <inheritdoc />
    public override byte[] ToBytes()
    {
        var oidBytes = Oid.GetOidBytes();
        var hasImportFormat = ImportFormat == EcImportFormat.StandardWithPubkey;
        var result = new byte[1 + oidBytes.Length + (hasImportFormat ? 1 : 0)];
        result[0] = (byte)AlgorithmId;
        oidBytes.CopyTo(result.AsSpan(1));
        if (hasImportFormat)
        {
            result[^1] = (byte)ImportFormat;
        }

        return result;
    }
}