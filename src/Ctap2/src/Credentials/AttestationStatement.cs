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

using System.Formats.Cbor;

namespace Yubico.YubiKit.Ctap2.Credentials;

/// <summary>
/// Base class for attestation statements.
/// </summary>
/// <remarks>
/// Typed decoding is provided for packed, fido-u2f, apple and none; other formats remain opaque.
/// Decoding does not verify signatures, certificates, trust or RP policy. RawCbor borrows the supplied
/// statement encoding (or the parent makeCredential buffer); keep it unchanged while referenced.
/// Typed decoders materialize byte-string fields separately. Direct constructors retain supplied
/// memory/collections without copying or validation; read-only views are not immutable snapshots.
/// </remarks>
public abstract record AttestationStatement
{
    /// <summary>
    /// Gets the attestation format identifier.
    /// </summary>
    public abstract AttestationFormat Format { get; }

    /// <summary>
    /// Gets the raw CBOR representation of the attestation statement.
    /// </summary>
    public abstract ReadOnlyMemory<byte> RawCbor { get; }

    /// <summary>
    /// Decodes an attestation statement from raw CBOR.
    /// </summary>
    internal static AttestationStatement Decode(AttestationFormat format, ReadOnlyMemory<byte> rawCbor)
    {
        if (format == AttestationFormat.Packed)
        {
            return PackedAttestationStatement.Decode(rawCbor);
        }

        if (format == AttestationFormat.FidoU2F)
        {
            return FidoU2FAttestationStatement.Decode(rawCbor);
        }

        if (format == AttestationFormat.Apple)
        {
            return AppleAttestationStatement.Decode(rawCbor);
        }

        if (format == AttestationFormat.None)
        {
            return NoneAttestationStatement.Decode(rawCbor);
        }

        // Unmodeled formats, including recognized IDs without a typed decoder, remain opaque.
        return new UnknownAttestationStatement(format, rawCbor);
    }
}

/// <summary>
/// Packed attestation statement.
/// </summary>
public sealed record PackedAttestationStatement : AttestationStatement
{
    /// <summary>Gets the received COSE signature algorithm identifier.</summary>
    public int Algorithm { get; }
    /// <summary>Gets the attestation signature bytes, not a verified signature result.</summary>
    public ReadOnlyMemory<byte> Signature { get; }
    /// <summary>Gets the optional DER certificate chain, leaf first; trust is not checked here.</summary>
    public IReadOnlyList<ReadOnlyMemory<byte>>? X5c { get; }
    /// <summary>Gets the optional ECDAA key identifier.</summary>
    public ReadOnlyMemory<byte>? EcdaaKeyId { get; }
    /// <summary>Gets the borrowed encoded statement, not the complete CTAP response.</summary>
    public override ReadOnlyMemory<byte> RawCbor { get; }
    /// <summary>Gets the packed format identifier.</summary>
    public override AttestationFormat Format => AttestationFormat.Packed;

    /// <summary>Creates a packed statement carrier without validation or copying.</summary>
    /// <param name="algorithm">The COSE signature algorithm.</param>
    /// <param name="signature">Borrowed signature bytes.</param>
    /// <param name="x5c">Optional borrowed certificate collection and bytes.</param>
    /// <param name="ecdaaKeyId">Optional borrowed ECDAA identifier.</param>
    /// <param name="rawCbor">Borrowed encoded statement.</param>
    public PackedAttestationStatement(
        int algorithm,
        ReadOnlyMemory<byte> signature,
        IReadOnlyList<ReadOnlyMemory<byte>>? x5c,
        ReadOnlyMemory<byte>? ecdaaKeyId,
        ReadOnlyMemory<byte> rawCbor)
    {
        Algorithm = algorithm;
        Signature = signature;
        X5c = x5c;
        EcdaaKeyId = ecdaaKeyId;
        RawCbor = rawCbor;
    }

    internal static PackedAttestationStatement Decode(ReadOnlyMemory<byte> rawCbor)
    {
        var reader = new CborReader(rawCbor, CborConformanceMode.Lax);
        var mapLength = reader.ReadStartMap();

        int? alg = null;
        byte[]? sig = null;
        List<ReadOnlyMemory<byte>>? x5c = null;
        byte[]? ecdaaKeyId = null;

        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadTextString();
            switch (key)
            {
                case "alg":
                    alg = reader.ReadInt32();
                    break;
                case "sig":
                    sig = reader.ReadByteString();
                    break;
                case "x5c":
                    x5c = [];
                    var certCount = reader.ReadStartArray();
                    for (var j = 0; j < certCount; j++)
                    {
                        x5c.Add(reader.ReadByteString());
                    }
                    reader.ReadEndArray();
                    break;
                case "ecdaaKeyId":
                    ecdaaKeyId = reader.ReadByteString();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();

        if (alg is null || sig is null)
        {
            throw new InvalidOperationException("Packed attestation statement missing required fields (alg, sig).");
        }

        ReadOnlyMemory<byte>? ecdaaKeyIdMemory = ecdaaKeyId is not null
            ? new ReadOnlyMemory<byte>(ecdaaKeyId)
            : null;

        return new PackedAttestationStatement(
            alg.Value,
            sig,
            x5c,
            ecdaaKeyIdMemory,
            rawCbor);
    }
}

/// <summary>
/// FIDO U2F attestation statement.
/// </summary>
public sealed record FidoU2FAttestationStatement : AttestationStatement
{
    /// <summary>Gets the received attestation signature, not a verification result.</summary>
    public ReadOnlyMemory<byte> Signature { get; }
    /// <summary>Gets the received DER certificates; decoding does not validate chain trust or format cardinality.</summary>
    public IReadOnlyList<ReadOnlyMemory<byte>> X5c { get; }
    /// <summary>Gets the borrowed encoded statement.</summary>
    public override ReadOnlyMemory<byte> RawCbor { get; }
    /// <summary>Gets the fido-u2f format identifier.</summary>
    public override AttestationFormat Format => AttestationFormat.FidoU2F;

    /// <summary>Creates a FIDO U2F statement carrier without validation or copying.</summary>
    /// <param name="signature">Borrowed signature bytes.</param>
    /// <param name="x5c">Borrowed certificate collection and bytes.</param>
    /// <param name="rawCbor">Borrowed encoded statement.</param>
    public FidoU2FAttestationStatement(
        ReadOnlyMemory<byte> signature,
        IReadOnlyList<ReadOnlyMemory<byte>> x5c,
        ReadOnlyMemory<byte> rawCbor)
    {
        Signature = signature;
        X5c = x5c;
        RawCbor = rawCbor;
    }

    internal static FidoU2FAttestationStatement Decode(ReadOnlyMemory<byte> rawCbor)
    {
        var reader = new CborReader(rawCbor, CborConformanceMode.Lax);
        var mapLength = reader.ReadStartMap();

        byte[]? sig = null;
        List<ReadOnlyMemory<byte>>? x5c = null;

        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadTextString();
            switch (key)
            {
                case "sig":
                    sig = reader.ReadByteString();
                    break;
                case "x5c":
                    x5c = [];
                    var certCount = reader.ReadStartArray();
                    for (var j = 0; j < certCount; j++)
                    {
                        x5c.Add(reader.ReadByteString());
                    }
                    reader.ReadEndArray();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();

        if (sig is null || x5c is null || x5c.Count == 0)
        {
            throw new InvalidOperationException("FIDO U2F attestation statement missing required fields (sig, x5c).");
        }

        return new FidoU2FAttestationStatement(sig, x5c, rawCbor);
    }
}

/// <summary>
/// Apple anonymous attestation statement.
/// </summary>
public sealed record AppleAttestationStatement : AttestationStatement
{
    /// <summary>Gets the received DER certificate chain; decoding does not verify chain trust or nonce binding.</summary>
    public IReadOnlyList<ReadOnlyMemory<byte>> X5c { get; }
    /// <summary>Gets the borrowed encoded statement.</summary>
    public override ReadOnlyMemory<byte> RawCbor { get; }
    /// <summary>Gets the apple format identifier.</summary>
    public override AttestationFormat Format => AttestationFormat.Apple;

    /// <summary>Creates an Apple statement carrier without validation or copying.</summary>
    /// <param name="x5c">Borrowed certificate collection and bytes.</param>
    /// <param name="rawCbor">Borrowed encoded statement.</param>
    public AppleAttestationStatement(
        IReadOnlyList<ReadOnlyMemory<byte>> x5c,
        ReadOnlyMemory<byte> rawCbor)
    {
        X5c = x5c;
        RawCbor = rawCbor;
    }

    internal static AppleAttestationStatement Decode(ReadOnlyMemory<byte> rawCbor)
    {
        var reader = new CborReader(rawCbor, CborConformanceMode.Lax);
        var mapLength = reader.ReadStartMap();

        List<ReadOnlyMemory<byte>>? x5c = null;

        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadTextString();
            switch (key)
            {
                case "x5c":
                    x5c = [];
                    var certCount = reader.ReadStartArray();
                    for (var j = 0; j < certCount; j++)
                    {
                        x5c.Add(reader.ReadByteString());
                    }
                    reader.ReadEndArray();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();

        if (x5c is null || x5c.Count == 0)
        {
            throw new InvalidOperationException("Apple attestation statement missing required field (x5c).");
        }

        return new AppleAttestationStatement(x5c, rawCbor);
    }
}

/// <summary>
/// None attestation: no attestation information, not signed self attestation.
/// </summary>
public sealed record NoneAttestationStatement : AttestationStatement
{
    /// <summary>
    /// The "none" statement WebAuthn clients substitute for an authenticator's statement: an empty
    /// CBOR map (WebAuthn L2 §8.7).
    /// </summary>
    public static NoneAttestationStatement Empty { get; } = new(new byte[] { 0xA0 });

    /// <summary>Gets the borrowed encoded statement; construction does not enforce an empty map.</summary>
    public override ReadOnlyMemory<byte> RawCbor { get; }
    /// <summary>Gets the none format identifier.</summary>
    public override AttestationFormat Format => AttestationFormat.None;

    /// <summary>Creates a none statement carrier without validating its encoding.</summary>
    /// <param name="rawCbor">Borrowed statement bytes; the protocol format requires an empty CBOR map.</param>
    public NoneAttestationStatement(ReadOnlyMemory<byte> rawCbor)
    {
        RawCbor = rawCbor;
    }

    internal static NoneAttestationStatement Decode(ReadOnlyMemory<byte> rawCbor)
    {
        // The format requires an empty map; this decoder retains bytes without enforcing that shape.
        return new NoneAttestationStatement(rawCbor);
    }
}

/// <summary>
/// Opaque statement for a format without a typed decoder, including recognized unmodeled formats.
/// </summary>
public sealed record UnknownAttestationStatement : AttestationStatement
{
    /// <summary>Gets the supplied format identifier.</summary>
    public override AttestationFormat Format { get; }
    /// <summary>Gets the borrowed encoded statement, not a validated attestation or complete response.</summary>
    public override ReadOnlyMemory<byte> RawCbor { get; }

    /// <summary>Creates an opaque statement carrier without validation or copying.</summary>
    /// <param name="format">The attestation format.</param>
    /// <param name="rawCbor">Borrowed encoded statement bytes.</param>
    public UnknownAttestationStatement(AttestationFormat format, ReadOnlyMemory<byte> rawCbor)
    {
        Format = format;
        RawCbor = rawCbor;
    }
}