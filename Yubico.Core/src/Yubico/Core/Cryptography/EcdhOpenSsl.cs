// Copyright 2022 Yubico AB
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

namespace Yubico.Core
{
    /// <summary>
    /// An OpenSSL implementation of the IEcdh interface, exposing ECDH primitives to the SDK.
    /// </summary>
    internal class EcdhOpenSsl : IEcdh
    {
        /// <inheritdoc />
        public ECParameters GenerateKeyPair(ECCurve curve)
        {
            const int privateValueSize = 32;
            const int publicCoordinateSize = 32;

            // Create a random 32 bit number as the private key and store it in an OpenSSL big num.
            using var rng = RandomNumberGenerator.Create();

            byte[] privateValueBinary = new byte[privateValueSize];
            rng.GetBytes(privateValueBinary);

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

            byte[] xBinary = new byte[publicCoordinateSize];
            result = NativeMethods.BnBigNumToBinaryWithPadding(xBn, xBinary);

            if (result <= 0)
            {
                throw new SecurityException(ExceptionMessages.EcdhKeygenFailed);
            }

            byte[] yBinary = new byte[publicCoordinateSize];
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

            // We only care about the X coordinate for the result of this function.
            byte[] sharedSecret = new byte[32];
            result = NativeMethods.BnBigNumToBinaryWithPadding(x, sharedSecret);

            if (result <= 0)
            {
                throw new SecurityException(ExceptionMessages.EcdhComputationFailed);
            }

            return sharedSecret;
        }
    }
}
