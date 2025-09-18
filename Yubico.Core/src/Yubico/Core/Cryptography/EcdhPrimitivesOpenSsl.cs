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

using System;
using System.Security;
using System.Security.Cryptography;
using Yubico.PlatformInterop;

namespace Yubico.Core.Cryptography;

/// <summary>
///     An OpenSSL implementation of the IEcdh interface, exposing ECDH primitives to the SDK.
/// </summary>
internal class EcdhPrimitivesOpenSsl : IEcdhPrimitives
{
    #region IEcdhPrimitives Members

    /// <inheritdoc />
    public ECParameters GenerateKeyPair(ECCurve curve)
    {
        int bitLength = curve.BitLength();
        int byteLength = GetByteLength(bitLength);
        byte msByteMask = GetLeadingByteMask(bitLength);

        // Create a random number as the private key and store it in an
        // OpenSSL big num.
        // Make sure it is no longer than bitLength bits by masking off any
        // "extra" bits.
        using var rng = RandomNumberGenerator.Create();

        byte[] privateValueBinary = new byte[byteLength];
        rng.GetBytes(privateValueBinary);
        privateValueBinary[0] &= msByteMask;

        using SafeBigNum privateValueBn = NativeMethods.BnBinaryToBigNum(privateValueBinary);

        // Create the curve that the public point should reside on.
        using SafeEcGroup group = NativeMethods.EcGroupNewByCurveName(curve.ToSslCurveId());
        using SafeEcPoint publicPoint = NativeMethods.EcPointNew(group);

        // Compute where the public point should reside on the curve, based on the private key.
        int result = NativeMethods.EcPointMul(
            group,
            publicPoint,
            privateValueBn.DangerousGetHandle(),
            IntPtr.Zero,
            IntPtr.Zero);

        if (result == 0)
        {
            throw new SecurityException(ExceptionMessages.EcdhKeygenFailed);
        }

        // Retrieve the X and Y coordinates from the computed EC point.
        using SafeBigNum xBn = NativeMethods.BnNew();
        using SafeBigNum yBn = NativeMethods.BnNew();
        result = NativeMethods.EcPointGetAffineCoordinates(group, publicPoint, xBn, yBn);

        if (result == 0)
        {
            throw new SecurityException(ExceptionMessages.EcdhKeygenFailed);
        }

        byte[] xBinary = new byte[byteLength];
        result = NativeMethods.BnBigNumToBinaryWithPadding(xBn, xBinary);

        if (result <= 0)
        {
            throw new SecurityException(ExceptionMessages.EcdhKeygenFailed);
        }

        byte[] yBinary = new byte[byteLength];
        result = NativeMethods.BnBigNumToBinaryWithPadding(yBn, yBinary);

        if (result <= 0)
        {
            throw new SecurityException(ExceptionMessages.EcdhKeygenFailed);
        }

        // Return the X, Y point, and the D private key in a .NET defined structure.
        return new ECParameters
        {
            Curve = curve,
            D = privateValueBinary,
            Q = new ECPoint
            {
                X = xBinary,
                Y = yBinary
            }
        };
    }

    /// <inheritdoc />
    public byte[] ComputeSharedSecret(ECParameters publicKey, ReadOnlySpan<byte> privateValue)
    {
        // Convert all fo the input into OpenSSL datatypes
        (SafeEcGroup? group, SafeEcPoint? publicPoint) = publicKey.ToSslPublicKey();

        using SafeEcPoint sharedPoint = NativeMethods.EcPointNew(group);

        byte[] privateValueBinary = privateValue.ToArray();
        using SafeBigNum privateValueBn = NativeMethods.BnBinaryToBigNum(privateValueBinary);
        CryptographicOperations.ZeroMemory(privateValueBinary);

        // Perform the scalar-multiplication to compute the shared point.
        int result = NativeMethods.EcPointMul(
            group,
            sharedPoint,
            IntPtr.Zero,
            publicPoint.DangerousGetHandle(),
            privateValueBn.DangerousGetHandle());

        if (result == 0)
        {
            throw new SecurityException(ExceptionMessages.EcdhComputationFailed);
        }

        // Retrieve the X and Y coordinates from the shared point.
        using SafeBigNum x = NativeMethods.BnNew();
        using SafeBigNum y = NativeMethods.BnNew();
        result = NativeMethods.EcPointGetAffineCoordinates(group, sharedPoint, x, y);

        if (result == 0)
        {
            throw new SecurityException(ExceptionMessages.EcdhComputationFailed);
        }

        // We only care about the X coordinate for the result of this
        // function.
        int secretLen = GetByteLength(publicKey.Curve.BitLength());
        byte[] sharedSecret = new byte[secretLen];
        result = NativeMethods.BnBigNumToBinaryWithPadding(x, sharedSecret);

        if (result <= 0)
        {
            throw new SecurityException(ExceptionMessages.EcdhComputationFailed);
        }

        return sharedSecret;
    }

    #endregion

    /// <summary>
    ///     Return the byte length of a buffer that can hold <c>bitLength</c>
    ///     bits.
    /// </summary>
    /// <param name="bitLength">
    ///     The length, in bits, of a canonical int for which the byte length is
    ///     requested.
    /// </param>
    /// <returns>
    ///     An int, the number of bytes needed to hold a canonical int whose
    ///     length, in bits, is given by <c>bitLength</c>.
    /// </returns>
    public static int GetByteLength(int bitLength) => (bitLength + 7) / 8;

    /// <summary>
    ///     Return a byte that will mask away unused bits in the most significant
    ///     byte of a canonical integer of length <c>bitLength</c>
    /// </summary>
    /// <param name="bitLength">
    ///     The length, in bits, of a canonical int for which the leading byte
    ///     mask is requested.
    /// </param>
    /// <returns>
    ///     A byte, the mask value, such as <c>0xFF</c> if the <c>bitLength</c>
    ///     is a multiple of 2 (don't mask any bits away) or <c>0x07</c> if the
    ///     <c>bitLength</c> is 35 (mask away the top 5 bits but retain the last
    ///     3).
    /// </returns>
    public static byte GetLeadingByteMask(int bitLength) => (byte)(0x00FF >> ((8 - (bitLength & 7)) & 7));
}
