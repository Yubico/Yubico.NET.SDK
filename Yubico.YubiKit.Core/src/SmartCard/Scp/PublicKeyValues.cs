// Copyright (C) 2024 Yubico.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.SmartCard.Scp;

/// <summary>
/// Abstract base class for public key representations.
/// </summary>
internal abstract class PublicKeyValues
{
    /// <summary>
    /// Creates a <see cref="PublicKeyValues"/> instance from an asymmetric algorithm public key.
    /// </summary>
    /// <param name="publicKey">The public key.</param>
    /// <returns>A <see cref="PublicKeyValues"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown if the key type is not supported.</exception>
    public static PublicKeyValues FromPublicKey(AsymmetricAlgorithm publicKey)
    {
        return publicKey switch
        {
            ECDiffieHellman ecdh => new Ec(ecdh.PublicKey),
            _ => throw new ArgumentException($"Unsupported public key type: {publicKey.GetType().Name}", nameof(publicKey))
        };
    }

    /// <summary>
    /// Elliptic curve public key representation.
    /// </summary>
    internal sealed class Ec : PublicKeyValues
    {
        /// <summary>
        /// Gets the uncompressed encoded point (0x04 + X + Y).
        /// </summary>
        public byte[] EncodedPoint { get; }

        /// <summary>
        /// Gets the elliptic curve parameters.
        /// </summary>
        public ECCurve CurveParams { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ec"/> class from an ECDiffieHellmanPublicKey.
        /// </summary>
        /// <param name="publicKey">The ECDH public key.</param>
        public Ec(ECDiffieHellmanPublicKey publicKey)
        {
            ECParameters parameters = publicKey.ExportParameters();
            CurveParams = parameters.Curve;

            // Export as uncompressed point: 0x04 + X + Y
            int coordinateLength = parameters.Q.X!.Length;
            EncodedPoint = new byte[1 + coordinateLength * 2];
            EncodedPoint[0] = 0x04; // Uncompressed point indicator
            parameters.Q.X.CopyTo(EncodedPoint, 1);
            parameters.Q.Y!.CopyTo(EncodedPoint, 1 + coordinateLength);
        }

        /// <summary>
        /// Creates an <see cref="Ec"/> instance from an encoded point.
        /// </summary>
        /// <param name="curve">The elliptic curve.</param>
        /// <param name="encodedPoint">The encoded point (uncompressed format: 0x04 + X + Y).</param>
        /// <returns>An <see cref="Ec"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown if the encoded point is invalid.</exception>
        public static Ec FromEncodedPoint(ECCurve curve, ReadOnlySpan<byte> encodedPoint)
        {
            if (encodedPoint.Length < 3 || encodedPoint[0] != 0x04)
            {
                throw new ArgumentException("Invalid encoded point format. Expected uncompressed point (0x04 + X + Y).", nameof(encodedPoint));
            }

            int coordinateLength = (encodedPoint.Length - 1) / 2;
            if ((encodedPoint.Length - 1) % 2 != 0)
            {
                throw new ArgumentException("Invalid encoded point length.", nameof(encodedPoint));
            }

            byte[] x = encodedPoint.Slice(1, coordinateLength).ToArray();
            byte[] y = encodedPoint.Slice(1 + coordinateLength, coordinateLength).ToArray();

            using var ecdh = ECDiffieHellman.Create(new ECParameters
            {
                Curve = curve,
                Q = new ECPoint
                {
                    X = x,
                    Y = y
                }
            });

            return new Ec(ecdh.PublicKey);
        }

        /// <summary>
        /// Converts this EC public key to an <see cref="ECDiffieHellmanPublicKey"/>.
        /// </summary>
        /// <returns>An <see cref="ECDiffieHellmanPublicKey"/> instance.</returns>
        public ECDiffieHellmanPublicKey ToECDiffieHellmanPublicKey()
        {
            int coordinateLength = (EncodedPoint.Length - 1) / 2;
            byte[] x = EncodedPoint.AsSpan(1, coordinateLength).ToArray();
            byte[] y = EncodedPoint.AsSpan(1 + coordinateLength, coordinateLength).ToArray();

            using var ecdh = ECDiffieHellman.Create(new ECParameters
            {
                Curve = CurveParams,
                Q = new ECPoint
                {
                    X = x,
                    Y = y
                }
            });

            return ecdh.PublicKey;
        }
    }
}
