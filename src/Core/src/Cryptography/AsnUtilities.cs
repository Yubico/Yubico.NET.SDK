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

using System.Globalization;
using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
/// Why an EC point failed <see cref="AsnUtilities.InspectUncompressedEcPoint"/>.
/// </summary>
internal enum EcPointDefect
{
    /// <summary>The point is a well-formed uncompressed point for the curve.</summary>
    None = 0,

    /// <summary>The point has no octets at all, so there is no format prefix to inspect.</summary>
    Empty,

    /// <summary>The point does not start with the 0x04 uncompressed-format prefix.</summary>
    NotUncompressed,

    /// <summary>The point is not exactly <c>1 + 2 * coordinateLengthBytes</c> octets long.</summary>
    WrongLength
}

internal static class AsnUtilities
{
    private const byte UncompressedPointPrefix = 0x04;

    /// <summary>
    /// The single authority on the shape of an EC point in this SDK: an uncompressed point is
    /// <c>0x04 || X || Y</c> with both coordinates exactly the curve's coordinate size.
    /// </summary>
    /// <remarks>
    /// <para>This mirrors the strictness of the Rust <c>yubikit</c> crate. Compressed and hybrid
    /// point formats are deliberately rejected rather than decompressed. Callers translate the
    /// result into the exception type their layer owes: <see cref="CryptographicException"/> for
    /// malformed encoded key data, and <see cref="ArgumentException"/> for bad caller-supplied
    /// input.</para>
    /// <para>That split governs <em>point shape</em> only. Whether a curve is supported at all is
    /// each decoder's own concern and each rejects it before reaching this file:
    /// <c>AsnPublicKeyDecoder</c> throws <see cref="NotSupportedException"/> and
    /// <c>AsnPrivateKeyDecoder</c> throws <see cref="InvalidOperationException"/>. The
    /// <see cref="CryptographicException"/> that <see cref="GetDecodedCoordinateLengthBytes"/> raises for
    /// an unsupported curve is therefore a defensive backstop, not a taxonomy this file imposes on
    /// its callers.</para>
    /// </remarks>
    public static EcPointDefect InspectUncompressedEcPoint(ReadOnlySpan<byte> point, int coordinateLengthBytes)
    {
        if (point.IsEmpty)
        {
            return EcPointDefect.Empty;
        }

        if (point[0] != UncompressedPointPrefix)
        {
            return EcPointDefect.NotUncompressed;
        }

        return point.Length != GetUncompressedPointLength(coordinateLengthBytes)
            ? EcPointDefect.WrongLength
            : EcPointDefect.None;
    }

    /// <summary>
    /// Validates an EC point that was read out of an encoded key. Malformed encoded data is a
    /// <see cref="CryptographicException"/>, never an argument exception.
    /// </summary>
    /// <exception cref="CryptographicException">
    /// The point is not a valid uncompressed point, or the curve is not a supported prime curve.
    /// </exception>
    public static void ValidateDecodedEcPoint(ReadOnlySpan<byte> point, string curveOid)
    {
        var coordinateLengthBytes = GetDecodedCoordinateLengthBytes(curveOid);
        var defect = InspectUncompressedEcPoint(point, coordinateLengthBytes);
        if (defect != EcPointDefect.None)
        {
            throw new CryptographicException(defect == EcPointDefect.NotUncompressed
                ? "Unsupported EC point format" : "Invalid EC public key encoding");
        }
    }

    /// <summary>
    /// Validates an EC point supplied by a caller of an encoder. Bad caller input is an
    /// <see cref="ArgumentException"/>, never a <see cref="CryptographicException"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The point is not a valid uncompressed point, or the curve is not a supported prime curve.
    /// </exception>
    public static void ValidateEcPointArgument(ReadOnlySpan<byte> point, string curveOid, string paramName)
    {
        var coordinateLengthBytes = GetCoordinateLengthBytesArgument(curveOid, paramName);
        var defect = InspectUncompressedEcPoint(point, coordinateLengthBytes);
        if (defect != EcPointDefect.None)
        {
            throw defect switch
            {
                EcPointDefect.WrongLength => new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "Invalid EC public point size for the specified curve. Expected {0} bytes, but got {1}.",
                        GetUncompressedPointLength(coordinateLengthBytes),
                        point.Length),
                    paramName),
                _ => new ArgumentException(
                    "EC public point must be in uncompressed format (starting with 0x04).",
                    paramName)
            };
        }
    }

    /// <summary>
    /// Builds the uncompressed point <c>0x04 || X || Y</c> for the named curve, rejecting
    /// coordinates that are not exactly the curve's coordinate size.
    /// </summary>
    /// <remarks>
    /// Checking each coordinate separately is stricter than checking the assembled point, because
    /// an oversized X paired with an undersized Y produces a correctly sized but silently wrong point.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// A coordinate does not match the curve, or the curve is not a supported prime curve.
    /// </exception>
    public static byte[] BuildUncompressedEcPoint(
        ReadOnlySpan<byte> xCoordinate,
        ReadOnlySpan<byte> yCoordinate,
        string curveOid,
        string paramName)
    {
        var coordinateLengthBytes = GetCoordinateLengthBytesArgument(curveOid, paramName);
        if (xCoordinate.Length != coordinateLengthBytes || yCoordinate.Length != coordinateLengthBytes)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "EC point coordinates do not match curve '{0}'. Expected {1} bytes for each of X and Y, but got {2} and {3}.",
                    curveOid,
                    coordinateLengthBytes,
                    xCoordinate.Length,
                    yCoordinate.Length),
                paramName);
        }

        var point = new byte[GetUncompressedPointLength(coordinateLengthBytes)];
        point[0] = UncompressedPointPrefix;
        xCoordinate.CopyTo(point.AsSpan(1));
        yCoordinate.CopyTo(point.AsSpan(1 + coordinateLengthBytes));
        return point;
    }

    private static int GetUncompressedPointLength(int coordinateLengthBytes) => 1 + (2 * coordinateLengthBytes);

    public static ReadOnlySpan<byte> TrimLeadingZeroes(ReadOnlySpan<byte> data)
    {
        var startIndex = GetLeadingZeroCount(data);
        return data.Slice(startIndex);
    }

    public static Span<byte> TrimLeadingZeroes(Span<byte> data)
    {
        var startIndex = GetLeadingZeroCount(data);
        return data.Slice(startIndex);
    }

    /// <summary>
    /// Validates a caller-supplied EC private scalar against the curve it will be encoded under,
    /// and in doing so gates the curve OID itself before it is written into an
    /// <c>id-ecPublicKey</c> AlgorithmIdentifier.
    /// </summary>
    /// <remarks>
    /// The scalar of an RFC 5915 <c>ECPrivateKey</c> is a fixed-width, left-padded integer of the
    /// curve's subgroup-order width. Any other width contradicts the curve parameter sitting in the same
    /// structure. The message reports lengths only: the value being rejected is key material.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// The scalar does not match the curve, or the curve is not a supported prime curve.
    /// </exception>
    public static void ValidateEcPrivateScalarArgument(
        ReadOnlySpan<byte> privateScalar,
        string curveOid,
        string paramName)
    {
        _ = GetCoordinateLengthBytesArgument(curveOid, paramName); // preserves argument-error taxonomy and curve-form gate
        var curveInfo = CryptographicDefinitions.Curves.GetByOid(curveOid);
        var scalarLengthBytes = (curveInfo.OrderSizeBits + 7) / 8;
        if (privateScalar.Length != scalarLengthBytes)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "EC private scalar does not match curve '{0}'. Expected {1} bytes, but got {2}.",
                    curveOid,
                    scalarLengthBytes,
                    privateScalar.Length),
                paramName);
        }
    }

    /// <summary>
    /// Resolves the coordinate size of an EC point read out of encoded key data.
    /// </summary>
    /// <remarks>
    /// Both decoders reject an unsupported curve before calling in here, each with its own
    /// exception type, so the throwing branch is a backstop that no production caller reaches. See
    /// the remarks on <see cref="InspectUncompressedEcPoint"/>.
    /// </remarks>
    /// <exception cref="CryptographicException">The curve is not a supported prime curve.</exception>
    public static int GetDecodedCoordinateLengthBytes(string curveOid) =>
        TryGetCoordinateLengthBytes(curveOid, out var coordinateLengthBytes)
            ? coordinateLengthBytes
            : throw new CryptographicException(UnsupportedCurveMessage(curveOid));

    /// <summary>
    /// Resolves the coordinate size for a curve OID supplied by a caller of an encoder.
    /// </summary>
    /// <exception cref="ArgumentException">The curve is not a supported prime curve.</exception>
    private static int GetCoordinateLengthBytesArgument(string curveOid, string paramName) =>
        TryGetCoordinateLengthBytes(curveOid, out var coordinateLengthBytes)
            ? coordinateLengthBytes
            : throw new ArgumentException(UnsupportedCurveMessage(curveOid), paramName);

    /// <summary>
    /// Resolves the coordinate size of one of the EC prime curves this SDK supports.
    /// </summary>
    /// <remarks>
    /// Only the named-curve index is consulted. A key algorithm or CBC operation OID must never
    /// pass an ordinary EC point check just because its key representation has the same width.
    /// </remarks>
    private static bool TryGetCoordinateLengthBytes(string? curveOid, out int coordinateLengthBytes)
    {
        if (!CryptographicDefinitions.Curves.TryGetByOid(curveOid, out var curveInfo)
            || curveInfo.Form != CurveForm.ShortWeierstrass)
        {
            coordinateLengthBytes = 0;
            return false;
        }

        coordinateLengthBytes = curveInfo.CoordinateLengthBytes;
        return true;
    }

    private static string UnsupportedCurveMessage(string? curveOid) =>
        string.Format(
            CultureInfo.CurrentCulture,
            "Unsupported ordinary EC named-curve OID '{0}'.",
            curveOid);

    // Ensures the integer value is treated as positive by adding a leading zero if needed
    public static byte[] EnsurePositive(byte[]? value)
    {
        if (value is null || value.Length == 0)
        {
            return [];
        }
        // Check if the most significant bit is set, indicating a negative number in two's complement
        if ((value[0] & 0x80) != 0)
        {
            var padded = new byte[value.Length + 1];
            padded[0] = 0x0; // Add leading zero
            Buffer.BlockCopy(value, 0, padded, 1, value.Length);
            return padded;
        }

        return value;
    }

    public static Span<byte> GetIntegerBytes(Span<byte> value)
    {
        if (value.Length == 0)
        {
            return new byte[] { 0x00 }.AsSpan();
        }

        // Check if the most significant bit is set, indicating a negative number
        // if so, add a leading zero to indicate a positive number
        var trimmedBytes = TrimLeadingZeroes(value);
        if ((trimmedBytes[0] & 0x80) != 0)
        {
            var padded = new byte[trimmedBytes.Length + 1];
            padded[0] = 0x00;
            trimmedBytes.CopyTo(padded.AsSpan(1));
            return padded;
        }

        return trimmedBytes;
    }

    /// <summary>
    /// Returns a newly allocated buffer containing the minimal positive ASN.1 INTEGER content octets.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="GetIntegerBytes(Span{byte})"/>, this method never aliases the input.
    /// The caller owns the returned buffer and is responsible for zeroing it when it contains
    /// sensitive material.
    /// </remarks>
    public static byte[] GetOwnedIntegerContentOctets(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
        {
            return [0x00];
        }

        var trimmedBytes = TrimLeadingZeroes(value);
        var paddingLength = (trimmedBytes[0] & 0x80) != 0 ? 1 : 0;
        var ownedBytes = new byte[trimmedBytes.Length + paddingLength];
        trimmedBytes.CopyTo(ownedBytes.AsSpan(paddingLength));
        return ownedBytes;
    }
    private static int GetLeadingZeroCount(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return 0;
        }

        var startIndex = 0;
        while (startIndex < data.Length && data[startIndex] == 0)
        {
            startIndex++;
        }

        return startIndex == data.Length // reached the end, all bytes were zero
            ? data.Length - 1 // return last byte position
            : startIndex; // return first non-zero byte position
    }
}