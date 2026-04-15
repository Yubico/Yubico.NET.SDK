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

using System.Security.Cryptography;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Identifies elliptic curves supported by the OpenPGP applet, using their OID byte encoding.
/// </summary>
public enum CurveOid
{
    Secp256R1,
    Secp256K1,
    Secp384R1,
    Secp521R1,
    BrainpoolP256R1,
    BrainpoolP384R1,
    BrainpoolP512R1,
    X25519,
    Ed25519,
}

/// <summary>
///     Extension methods and lookup tables for <see cref="CurveOid" />.
/// </summary>
public static class CurveOidExtensions
{
    // OID byte encodings for each curve (DER-encoded OID value, without the tag/length wrapper)
    private static readonly byte[] OidSecp256R1 = [0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x03, 0x01, 0x07];
    private static readonly byte[] OidSecp256K1 = [0x2B, 0x81, 0x04, 0x00, 0x0A];
    private static readonly byte[] OidSecp384R1 = [0x2B, 0x81, 0x04, 0x00, 0x22];
    private static readonly byte[] OidSecp521R1 = [0x2B, 0x81, 0x04, 0x00, 0x23];
    private static readonly byte[] OidBrainpoolP256R1 = [0x2B, 0x24, 0x03, 0x03, 0x02, 0x08, 0x01, 0x01, 0x07];
    private static readonly byte[] OidBrainpoolP384R1 = [0x2B, 0x24, 0x03, 0x03, 0x02, 0x08, 0x01, 0x01, 0x0B];
    private static readonly byte[] OidBrainpoolP512R1 = [0x2B, 0x24, 0x03, 0x03, 0x02, 0x08, 0x01, 0x01, 0x0D];
    private static readonly byte[] OidX25519 = [0x2B, 0x06, 0x01, 0x04, 0x01, 0x97, 0x55, 0x01, 0x05, 0x01];
    private static readonly byte[] OidEd25519 = [0x2B, 0x06, 0x01, 0x04, 0x01, 0xDA, 0x47, 0x0F, 0x01];

    private static readonly Dictionary<CurveOid, byte[]> OidBytes = new()
    {
        [CurveOid.Secp256R1] = OidSecp256R1,
        [CurveOid.Secp256K1] = OidSecp256K1,
        [CurveOid.Secp384R1] = OidSecp384R1,
        [CurveOid.Secp521R1] = OidSecp521R1,
        [CurveOid.BrainpoolP256R1] = OidBrainpoolP256R1,
        [CurveOid.BrainpoolP384R1] = OidBrainpoolP384R1,
        [CurveOid.BrainpoolP512R1] = OidBrainpoolP512R1,
        [CurveOid.X25519] = OidX25519,
        [CurveOid.Ed25519] = OidEd25519,
    };

    /// <summary>
    ///     Gets the DER-encoded OID bytes for this curve.
    /// </summary>
    public static ReadOnlySpan<byte> GetOidBytes(this CurveOid curveOid) =>
        OidBytes[curveOid];

    /// <summary>
    ///     Maps a <see cref="CurveOid" /> to the corresponding .NET <see cref="ECCurve" />.
    /// </summary>
    /// <exception cref="NotSupportedException">
    ///     Thrown for X25519 and Ed25519 which do not have <see cref="ECCurve" /> representations.
    /// </exception>
    public static ECCurve ToEcCurve(this CurveOid curveOid) =>
        curveOid switch
        {
            CurveOid.Secp256R1 => ECCurve.NamedCurves.nistP256,
            CurveOid.Secp384R1 => ECCurve.NamedCurves.nistP384,
            CurveOid.Secp521R1 => ECCurve.NamedCurves.nistP521,
            CurveOid.Secp256K1 => ECCurve.CreateFromValue("1.3.132.0.10"),
            CurveOid.BrainpoolP256R1 => ECCurve.CreateFromValue("1.3.36.3.3.2.8.1.1.7"),
            CurveOid.BrainpoolP384R1 => ECCurve.CreateFromValue("1.3.36.3.3.2.8.1.1.11"),
            CurveOid.BrainpoolP512R1 => ECCurve.CreateFromValue("1.3.36.3.3.2.8.1.1.13"),
            CurveOid.X25519 => throw new NotSupportedException("X25519 does not use ECCurve."),
            CurveOid.Ed25519 => throw new NotSupportedException("Ed25519 does not use ECCurve."),
            _ => throw new ArgumentOutOfRangeException(nameof(curveOid)),
        };

    /// <summary>
    ///     Attempts to parse a <see cref="CurveOid" /> from DER-encoded OID bytes.
    /// </summary>
    public static bool TryFromOidBytes(ReadOnlySpan<byte> oidBytes, out CurveOid curveOid)
    {
        foreach (var (curve, bytes) in OidBytes)
        {
            if (oidBytes.SequenceEqual(bytes))
            {
                curveOid = curve;
                return true;
            }
        }

        curveOid = default;
        return false;
    }

    /// <summary>
    ///     Parses a <see cref="CurveOid" /> from DER-encoded OID bytes.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the OID bytes do not match a known curve.</exception>
    public static CurveOid FromOidBytes(ReadOnlySpan<byte> oidBytes) =>
        TryFromOidBytes(oidBytes, out var result)
            ? result
            : throw new ArgumentException("Unknown curve OID.", nameof(oidBytes));

    /// <summary>
    ///     Gets the dotted-string representation of the OID (e.g., "1.2.840.10045.3.1.7").
    /// </summary>
    public static string ToDottedString(this CurveOid curveOid) =>
        curveOid switch
        {
            CurveOid.Secp256R1 => "1.2.840.10045.3.1.7",
            CurveOid.Secp256K1 => "1.3.132.0.10",
            CurveOid.Secp384R1 => "1.3.132.0.34",
            CurveOid.Secp521R1 => "1.3.132.0.35",
            CurveOid.BrainpoolP256R1 => "1.3.36.3.3.2.8.1.1.7",
            CurveOid.BrainpoolP384R1 => "1.3.36.3.3.2.8.1.1.11",
            CurveOid.BrainpoolP512R1 => "1.3.36.3.3.2.8.1.1.13",
            CurveOid.X25519 => "1.3.6.1.4.1.3029.1.5.1",
            CurveOid.Ed25519 => "1.3.6.1.4.1.11591.15.1",
            _ => throw new ArgumentOutOfRangeException(nameof(curveOid)),
        };
}