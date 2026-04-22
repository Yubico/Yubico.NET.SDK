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
using Yubico.YubiKit.WebAuthn.Client;

namespace Yubico.YubiKit.WebAuthn.Cose;

/// <summary>
/// COSE_Key base type representing cryptographic public keys.
/// </summary>
/// <remarks>
/// See <see href="https://datatracker.ietf.org/doc/html/rfc8152#section-7">
/// COSE Key Objects</see>.
/// </remarks>
public abstract record CoseKey
{
    /// <summary>
    /// Gets the COSE key type (kty parameter, label 1).
    /// </summary>
    public abstract int KeyType { get; }

    /// <summary>
    /// Gets the COSE algorithm identifier (alg parameter, label 3).
    /// </summary>
    public abstract CoseAlgorithm Algorithm { get; }

    /// <summary>
    /// Decodes a COSE_Key from CBOR-encoded bytes.
    /// </summary>
    /// <param name="coseEncoded">CBOR-encoded COSE_Key.</param>
    /// <returns>A <see cref="CoseKey"/> subclass based on the key type.</returns>
    public static CoseKey Decode(ReadOnlyMemory<byte> coseEncoded)
    {
        var reader = new CborReader(coseEncoded, CborConformanceMode.Ctap2Canonical);

        // Read map header
        int? mapSize = reader.ReadStartMap();
        var parameters = new Dictionary<int, object?>();

        int entriesRead = 0;
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            int key = reader.ReadInt32();
            object? value = reader.PeekState() switch
            {
                CborReaderState.ByteString => reader.ReadByteString(),
                CborReaderState.UnsignedInteger or CborReaderState.NegativeInteger => reader.ReadInt32(),
                _ => throw new WebAuthnClientError(WebAuthnClientErrorCode.InvalidState, $"Unsupported CBOR type for COSE key parameter {key}")
            };
            parameters[key] = value;
            entriesRead++;
        }
        reader.ReadEndMap();

        // Extract common parameters
        int kty = parameters.TryGetValue(1, out var ktyValue) && ktyValue is int k ? k :
            throw new WebAuthnClientError(WebAuthnClientErrorCode.InvalidState, "Missing required kty parameter");
        int alg = parameters.TryGetValue(3, out var algValue) && algValue is int a ? a :
            throw new WebAuthnClientError(WebAuthnClientErrorCode.InvalidState, "Missing required alg parameter");

        CoseAlgorithm algorithm = new(alg);

        return kty switch
        {
            2 => DecodeEc2(parameters, algorithm),
            1 => DecodeOkp(parameters, algorithm),
            3 => DecodeRsa(parameters, algorithm),
            _ => new CoseOtherKey(kty, algorithm, coseEncoded.ToArray())
        };
    }

    private static CoseEc2Key DecodeEc2(Dictionary<int, object?> parameters, CoseAlgorithm algorithm)
    {
        int crv = parameters.TryGetValue(-1, out var crvValue) && crvValue is int c ? c :
            throw new WebAuthnClientError(WebAuthnClientErrorCode.InvalidState, "Missing curve parameter for EC2 key");
        byte[] x = parameters.TryGetValue(-2, out var xValue) && xValue is byte[] xBytes ? xBytes :
            throw new WebAuthnClientError(WebAuthnClientErrorCode.InvalidState, "Missing x coordinate for EC2 key");
        byte[] y = parameters.TryGetValue(-3, out var yValue) && yValue is byte[] yBytes ? yBytes :
            throw new WebAuthnClientError(WebAuthnClientErrorCode.InvalidState, "Missing y coordinate for EC2 key");

        return new CoseEc2Key(algorithm, crv, x, y);
    }

    private static CoseOkpKey DecodeOkp(Dictionary<int, object?> parameters, CoseAlgorithm algorithm)
    {
        int crv = parameters.TryGetValue(-1, out var crvValue) && crvValue is int c ? c :
            throw new WebAuthnClientError(WebAuthnClientErrorCode.InvalidState, "Missing curve parameter for OKP key");
        byte[] x = parameters.TryGetValue(-2, out var xValue) && xValue is byte[] xBytes ? xBytes :
            throw new WebAuthnClientError(WebAuthnClientErrorCode.InvalidState, "Missing x coordinate for OKP key");

        return new CoseOkpKey(algorithm, crv, x);
    }

    private static CoseRsaKey DecodeRsa(Dictionary<int, object?> parameters, CoseAlgorithm algorithm)
    {
        byte[] n = parameters.TryGetValue(-1, out var nValue) && nValue is byte[] nBytes ? nBytes :
            throw new WebAuthnClientError(WebAuthnClientErrorCode.InvalidState, "Missing modulus for RSA key");
        byte[] e = parameters.TryGetValue(-2, out var eValue) && eValue is byte[] eBytes ? eBytes :
            throw new WebAuthnClientError(WebAuthnClientErrorCode.InvalidState, "Missing exponent for RSA key");

        return new CoseRsaKey(algorithm, n, e);
    }

    /// <summary>
    /// Encodes this COSE_Key to CBOR bytes.
    /// </summary>
    /// <returns>CBOR-encoded bytes.</returns>
    public abstract byte[] Encode();
}

/// <summary>
/// EC2 (Elliptic Curve) COSE key.
/// </summary>
/// <param name="Algorithm">COSE algorithm identifier.</param>
/// <param name="Curve">Curve identifier (crv, label -1).</param>
/// <param name="X">X coordinate (label -2).</param>
/// <param name="Y">Y coordinate (label -3).</param>
public sealed record CoseEc2Key(
    CoseAlgorithm Algorithm,
    int Curve,
    ReadOnlyMemory<byte> X,
    ReadOnlyMemory<byte> Y) : CoseKey
{
    public override int KeyType => 2;
    public override CoseAlgorithm Algorithm { get; } = Algorithm;

    public override byte[] Encode()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(5);

        // Keys must be in sorted order for canonical CBOR: -3, -2, -1, 1, 3
        writer.WriteInt32(-3);
        writer.WriteByteString(Y.Span);

        writer.WriteInt32(-2);
        writer.WriteByteString(X.Span);

        writer.WriteInt32(-1);
        writer.WriteInt32(Curve);

        writer.WriteInt32(1);
        writer.WriteInt32(KeyType);

        writer.WriteInt32(3);
        writer.WriteInt32(Algorithm.Value);

        writer.WriteEndMap();
        return writer.Encode();
    }
}

/// <summary>
/// OKP (Octet Key Pair) COSE key (e.g., Ed25519).
/// </summary>
/// <param name="Algorithm">COSE algorithm identifier.</param>
/// <param name="Curve">Curve identifier (crv, label -1).</param>
/// <param name="X">X coordinate (label -2).</param>
public sealed record CoseOkpKey(
    CoseAlgorithm Algorithm,
    int Curve,
    ReadOnlyMemory<byte> X) : CoseKey
{
    public override int KeyType => 1;
    public override CoseAlgorithm Algorithm { get; } = Algorithm;

    public override byte[] Encode()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(4);

        // Keys must be in sorted order for canonical CBOR: -2, -1, 1, 3
        writer.WriteInt32(-2);
        writer.WriteByteString(X.Span);

        writer.WriteInt32(-1);
        writer.WriteInt32(Curve);

        writer.WriteInt32(1);
        writer.WriteInt32(KeyType);

        writer.WriteInt32(3);
        writer.WriteInt32(Algorithm.Value);

        writer.WriteEndMap();
        return writer.Encode();
    }
}

/// <summary>
/// RSA COSE key.
/// </summary>
/// <param name="Algorithm">COSE algorithm identifier.</param>
/// <param name="Modulus">RSA modulus (n, label -1).</param>
/// <param name="Exponent">RSA public exponent (e, label -2).</param>
public sealed record CoseRsaKey(
    CoseAlgorithm Algorithm,
    ReadOnlyMemory<byte> Modulus,
    ReadOnlyMemory<byte> Exponent) : CoseKey
{
    public override int KeyType => 3;
    public override CoseAlgorithm Algorithm { get; } = Algorithm;

    public override byte[] Encode()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(4);

        // Keys must be in sorted order for canonical CBOR: -2, -1, 1, 3
        writer.WriteInt32(-2);
        writer.WriteByteString(Exponent.Span);

        writer.WriteInt32(-1);
        writer.WriteByteString(Modulus.Span);

        writer.WriteInt32(1);
        writer.WriteInt32(KeyType);

        writer.WriteInt32(3);
        writer.WriteInt32(Algorithm.Value);

        writer.WriteEndMap();
        return writer.Encode();
    }
}

/// <summary>
/// Unknown/Other COSE key type.
/// </summary>
/// <param name="KeyType">The COSE key type value.</param>
/// <param name="Algorithm">COSE algorithm identifier.</param>
/// <param name="RawCbor">The original CBOR-encoded bytes.</param>
public sealed record CoseOtherKey(
    int KeyType,
    CoseAlgorithm Algorithm,
    ReadOnlyMemory<byte> RawCbor) : CoseKey
{
    public override int KeyType { get; } = KeyType;
    public override CoseAlgorithm Algorithm { get; } = Algorithm;

    public override byte[] Encode() => RawCbor.ToArray();
}
