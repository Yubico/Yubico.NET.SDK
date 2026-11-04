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

namespace Yubico.YubiKit.Ctap2.Cose;

/// <summary>
/// ARKG-P256 seed-key COSE_Key variant (alg -65700).
/// </summary>
/// <remarks>
/// <para>
/// <b>WARNING -- EXPERIMENTAL --</b> ARKG previewSign seed-key support is not ready for production use and
/// must not be treated as production cryptographic guidance.
/// </para>
/// <para>
/// The experimental SDK profile carries blinding and KEM public keys for offline key derivation.
/// Unlike ordinary EC2 curve/coordinate parameters, -1 and -2 contain byte strings whose contents
/// are encoded COSE EC2 maps. The decoder also accepts direct nested maps through CoseKey.Decode.
/// </para>
/// <para>
/// SDK-emitted wire profile (not a claim of draft or vendor conformance):
/// - kty (1) = 2 (EC2 family)
/// - alg (3) = -65700 (ARKG-P256 seed-key marker)
/// - -1 = pkBl as a byte string containing an EC2/P-256 COSE map
/// - -2 = pkKem as a byte string containing an EC2/P-256 COSE map
/// - -3 = derived-key algorithm (e.g., -9 for Esp256)
/// </para>
/// <para>
/// draft-bradleylundberg-cfrg-arkg-10 §5.1 agrees on BL=-1/KEM=-2 and illustrates derived ESP256=-9;
/// §§5.1/5.2/8.3 explicitly specify the ARKG-P256 seed algorithm placeholder -65700. The SDK's outer
/// kty=2 and byte-string wrappers differ from the draft's kty placeholder -65537 and direct maps.
/// The SDK also requires an integer derived algorithm; the draft's dkalg is optional and permits
/// int/tstr. These agreements do not establish full-profile interoperability. The Internet-Draft is
/// work in progress, with stated expiry 31 August 2026, not production cryptographic authority.
/// </para>
/// <para>
/// Decode dispatches on alg=-65700 before outer kty, then requires nested EC2 carriers and coordinates
/// no longer than 32 bytes. It does not check nested curve=1 or point validity. Coordinates, including
/// short/empty values, are left-padded into 04 || X[32] || Y[32] for downstream helpers; that SEC1 layout
/// is not proof of a valid P-256 point. Direct construction borrows both points and does not enforce
/// the algorithm marker or point shape; keep supplied storage unchanged during use.
/// </para>
/// </remarks>
/// <param name="Algorithm">Seed marker for this profile (ArkgP256SeedKey=-65700); construction does not enforce it.</param>
/// <param name="DerivedKeyAlgorithm">Algorithm for the derived signing key (e.g., Esp256 = -9).</param>
/// <param name="KemPublicKey">KEM public key as 65-byte SEC1 uncompressed point.</param>
/// <param name="BlPublicKey">Blinding public key as 65-byte SEC1 uncompressed point.</param>
public sealed record CoseArkgP256SeedKey(
    CoseAlgorithm Algorithm,
    CoseAlgorithm DerivedKeyAlgorithm,
    ReadOnlyMemory<byte> KemPublicKey,
    ReadOnlyMemory<byte> BlPublicKey) : CoseKey
{
    /// <summary>
    /// Gets the COSE key type (kty = 2, EC2 family).
    /// </summary>
    public override int KeyType => 2;

    /// <summary>
    /// Gets the supplied seed-key algorithm identifier; this profile uses -65700.
    /// </summary>
    public override CoseAlgorithm Algorithm { get; } = Algorithm;

    /// <summary>
    /// Decodes an ARKG-P256 seed-key from CBOR parameters.
    /// </summary>
    /// <param name="parameters">Decoded COSE_Key map parameters.</param>
    /// <param name="seedKeyAlgorithm">Seed-key COSE algorithm (must be -65700).</param>
    /// <returns>A <see cref="CoseArkgP256SeedKey"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// A required parameter is missing, a nested key is not an EC2 carrier, or coordinates exceed 32 bytes.
    /// </exception>
    internal static CoseArkgP256SeedKey Decode(Dictionary<int, object?> parameters, CoseAlgorithm seedKeyAlgorithm)
    {
        // Preserve the distinct derivation roles: -1 = BL, -2 = KEM (ARKG-10 §5.1 label mapping).
        byte[] blKeyCbor = parameters.TryGetValue(-1, out var v1) && v1 is byte[] b1
            ? b1
            : throw new InvalidOperationException("ARKG seed key missing -1 (BL public key COSE map)");

        byte[] kemKeyCbor = parameters.TryGetValue(-2, out var v2) && v2 is byte[] b2
            ? b2
            : throw new InvalidOperationException("ARKG seed key missing -2 (KEM public key COSE map)");

        // -3 is the derived-key alg (e.g. -9 = Esp256)
        int derivedAlgInt = parameters.TryGetValue(-3, out var v3) && v3 is int a
            ? a
            : throw new InvalidOperationException("ARKG seed key missing -3 (derived key algorithm)");

        // Require EC2 carriers; generic decoding does not establish curve=1 or valid P-256 points.
        var kemKey = CoseKey.Decode(kemKeyCbor) as CoseEc2Key
            ?? throw new InvalidOperationException("ARKG KEM public key is not CoseEc2Key");

        var blKey = CoseKey.Decode(blKeyCbor) as CoseEc2Key
            ?? throw new InvalidOperationException("ARKG BL public key is not CoseEc2Key");

        // Reconstruct SEC1 uncompressed (0x04 || x || y) — 65 bytes for P-256
        byte[] kemSec1 = ReconstructSec1Uncompressed(kemKey, "KEM");
        byte[] blSec1 = ReconstructSec1Uncompressed(blKey, "BL");

        return new CoseArkgP256SeedKey(
            seedKeyAlgorithm,                          // -65700
            new CoseAlgorithm(derivedAlgInt),          // -9 (Esp256) or other
            kemSec1,                                   // 65-byte SEC1
            blSec1);                                   // 65-byte SEC1
    }

    /// <summary>
    /// Reconstructs a 65-byte SEC1 uncompressed point from a CoseEc2Key.
    /// </summary>
    /// <param name="ec2">The EC2 key containing x and y coordinates.</param>
    /// <param name="keyName">Key name for error messages (e.g., "KEM", "BL").</param>
    /// <returns>65-byte array: 0x04 || x (32 bytes) || y (32 bytes).</returns>
    private static byte[] ReconstructSec1Uncompressed(CoseEc2Key ec2, string keyName)
    {
        // Limit slot width; short/empty coordinates are padded, without proving P-256 point validity.
        if (ec2.X.Length > 32 || ec2.Y.Length > 32)
        {
            throw new InvalidOperationException(
                $"ARKG {keyName} inner P-256 coordinate exceeds 32 bytes (X={ec2.X.Length}, Y={ec2.Y.Length})");
        }

        byte[] sec1 = new byte[65];
        sec1[0] = 0x04; // SEC1 uncompressed point marker

        // Right-align coordinates in their 32-byte slots (left-pad with zeros if needed)
        // X goes in bytes [1..33), Y goes in bytes [33..65)
        int xOffset = 33 - ec2.X.Length;
        int yOffset = 65 - ec2.Y.Length;

        ec2.X.Span.CopyTo(sec1.AsSpan(xOffset, ec2.X.Length));
        ec2.Y.Span.CopyTo(sec1.AsSpan(yOffset, ec2.Y.Length));

        return sec1;
    }

    /// <summary>
    /// Encodes this ARKG-P256 seed-key to CBOR bytes.
    /// </summary>
    /// <returns>A new byte array containing the encoded COSE map with byte-string-wrapped EC2 key maps.</returns>
    /// <remarks>
    /// Emits kty=2, the supplied alg, -1=bstr(encoded BL map), -2=bstr(encoded KEM map), and
    /// -3=derivedAlg. Nested maps use EC2/P-256/ES256 labels; the outer profile expects alg=-65700.
    /// SEC1 length/prefix are checked during encoding, but mathematical point validity is not.
    /// </remarks>
    public override byte[] Encode()
    {
        // Convert SEC1 points back to nested CoseEc2Key maps
        var kemKey = Sec1ToCoseEc2(KemPublicKey.Span, CoseAlgorithm.Es256); // P-256 default
        var blKey = Sec1ToCoseEc2(BlPublicKey.Span, CoseAlgorithm.Es256);

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(5);

        // Numeric insertion order is not canonical encoded order; the writer sorts to 1, 3, -1, -2, -3.
        writer.WriteInt32(-3);
        writer.WriteInt32(DerivedKeyAlgorithm.Value);

        writer.WriteInt32(-2);
        writer.WriteByteString(kemKey.Encode());

        writer.WriteInt32(-1);
        writer.WriteByteString(blKey.Encode());

        writer.WriteInt32(1);
        writer.WriteInt32(KeyType);

        writer.WriteInt32(3);
        writer.WriteInt32(Algorithm.Value);

        writer.WriteEndMap();
        return writer.Encode();
    }

    /// <summary>
    /// Converts a 65-byte SEC1 uncompressed point to a CoseEc2Key.
    /// </summary>
    private static CoseEc2Key Sec1ToCoseEc2(ReadOnlySpan<byte> sec1, CoseAlgorithm alg)
    {
        if (sec1.Length != 65 || sec1[0] != 0x04)
        {
            throw new InvalidOperationException("Invalid SEC1 uncompressed point");
        }

        // Extract x (bytes 1..33) and y (bytes 33..65)
        byte[] x = sec1.Slice(1, 32).ToArray();
        byte[] y = sec1.Slice(33, 32).ToArray();

        return new CoseEc2Key(alg, 1 /* P-256 curve ID */, x, y);
    }
}