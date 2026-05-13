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

namespace Yubico.YubiKit.Fido2.Cose;

/// <summary>
/// ARKG-P256 seed-key COSE_Key variant (alg -65700).
/// </summary>
/// <remarks>
/// <para>
/// An ARKG-P256 seed-key is a specialized COSE_Key structure encoding two P-256 public keys
/// (pkKem and pkBl) used for offline key derivation. Unlike standard EC2 keys where parameter -1
/// is the curve identifier, ARKG seed-keys use -1/-2 to carry NESTED COSE_Key maps (not flat
/// SEC1 byte strings).
/// </para>
/// <para>
/// Wire format (per draft-bradleylundberg-cfrg-arkg-10, python-fido2 ARKG_P256_PLACEHOLDER
/// at fido2/cose.py:428-433, and the legacy SDK's PreviewSignExtension.cs:317-323):
/// - kty (1) = 2 (EC2 family)
/// - alg (3) = -65700 (ARKG-P256 seed-key marker)
/// - -1 = pkBl  as NESTED COSE_Key map (CoseEc2Key with P-256 coordinates)
/// - -2 = pkKem as NESTED COSE_Key map (CoseEc2Key with P-256 coordinates)
/// - -3 = derived-key algorithm (e.g., -9 for Esp256)
/// </para>
/// <para>
/// This class stores the reconstructed 65-byte SEC1 uncompressed points for each nested key
/// to simplify downstream ARKG derivation logic.
/// </para>
/// <para>
/// Discriminate this variant from standard EC2 keys by checking alg == -65700 AFTER kty == 2.
/// </para>
/// </remarks>
/// <param name="Algorithm">COSE algorithm identifier (must be ArkgP256SeedKey = -65700).</param>
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
    /// Gets the COSE algorithm identifier (alg = -65700).
    /// </summary>
    public override CoseAlgorithm Algorithm { get; } = Algorithm;

    /// <summary>
    /// Decodes an ARKG-P256 seed-key from CBOR parameters.
    /// </summary>
    /// <param name="parameters">Decoded COSE_Key map parameters.</param>
    /// <param name="seedKeyAlgorithm">Seed-key COSE algorithm (must be -65700).</param>
    /// <returns>A <see cref="CoseArkgP256SeedKey"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if required parameters are missing or nested keys are invalid.
    /// </exception>
    internal static CoseArkgP256SeedKey Decode(Dictionary<int, object?> parameters, CoseAlgorithm seedKeyAlgorithm)
    {
        // -1 = pkBl, -2 = pkKem (per draft-bradleylundberg-cfrg-arkg-10 and python-fido2)
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

        // Recursively decode each nested COSE_Key — both must be CoseEc2Key (kty=2, P-256)
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
        // P-256: x and y are each 32 bytes (may be shorter if leading zeros were stripped)
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
    /// <returns>CBOR-encoded COSE_Key with nested EC2 keys.</returns>
    /// <remarks>
    /// Wire format: {1: 2, 3: -65700, -3: derivedAlg, -2: kemKeyMap, -1: blKeyMap}
    /// Each nested key is a full COSE_Key EC2 map. Per spec: -1 = pkBl, -2 = pkKem.
    /// </remarks>
    public override byte[] Encode()
    {
        // Convert SEC1 points back to nested CoseEc2Key maps
        var kemKey = Sec1ToCoseEc2(KemPublicKey.Span, CoseAlgorithm.Es256); // P-256 default
        var blKey = Sec1ToCoseEc2(BlPublicKey.Span, CoseAlgorithm.Es256);

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(5);

        // Keys must be in sorted order for canonical CBOR: -3, -2, -1, 1, 3
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
