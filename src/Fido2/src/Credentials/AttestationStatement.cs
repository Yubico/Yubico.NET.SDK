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

namespace Yubico.YubiKit.Fido2.Credentials;

/// <summary>
/// Base class for attestation statements.
/// </summary>
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

        // Unknown format - capture as opaque
        return new UnknownAttestationStatement(format, rawCbor);
    }
}

/// <summary>
/// Packed attestation statement.
/// </summary>
public sealed record PackedAttestationStatement : AttestationStatement
{
    public int Algorithm { get; }
    public ReadOnlyMemory<byte> Signature { get; }
    public IReadOnlyList<ReadOnlyMemory<byte>>? X5c { get; }
    public ReadOnlyMemory<byte>? EcdaaKeyId { get; }
    public override ReadOnlyMemory<byte> RawCbor { get; }
    public override AttestationFormat Format => AttestationFormat.Packed;

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
    public ReadOnlyMemory<byte> Signature { get; }
    public IReadOnlyList<ReadOnlyMemory<byte>> X5c { get; }
    public override ReadOnlyMemory<byte> RawCbor { get; }
    public override AttestationFormat Format => AttestationFormat.FidoU2F;

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
    public IReadOnlyList<ReadOnlyMemory<byte>> X5c { get; }
    public override ReadOnlyMemory<byte> RawCbor { get; }
    public override AttestationFormat Format => AttestationFormat.Apple;

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
/// None attestation (self-attestation).
/// </summary>
public sealed record NoneAttestationStatement : AttestationStatement
{
    public override ReadOnlyMemory<byte> RawCbor { get; }
    public override AttestationFormat Format => AttestationFormat.None;

    public NoneAttestationStatement(ReadOnlyMemory<byte> rawCbor)
    {
        RawCbor = rawCbor;
    }

    internal static NoneAttestationStatement Decode(ReadOnlyMemory<byte> rawCbor)
    {
        // "none" attestation should be an empty CBOR map
        return new NoneAttestationStatement(rawCbor);
    }
}

/// <summary>
/// Unknown attestation format (opaque).
/// </summary>
public sealed record UnknownAttestationStatement : AttestationStatement
{
    public override AttestationFormat Format { get; }
    public override ReadOnlyMemory<byte> RawCbor { get; }

    public UnknownAttestationStatement(AttestationFormat format, ReadOnlyMemory<byte> rawCbor)
    {
        Format = format;
        RawCbor = rawCbor;
    }
}
