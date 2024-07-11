// Copyright 2021 Yubico AB
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
using System.Globalization;
using System.Security.Cryptography;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    // This file contains methods related to converting from a PivPublicKey into
    // an AsymmetricAlgorithm, and converting from AsymmetricAlgorithm
    // (the C# classes RSA and ECDsa) into PivPublicKey and PivPrivateKey.
    public static partial class KeyConverter
    {
        // Build a PivPublicKey object from an AsymmetricAlgorithm object.
        public static PivPublicKey GetPivPublicKeyFromDotNet(AsymmetricAlgorithm dotNetObject)
        {
            if (dotNetObject is null)
            {
                throw new ArgumentNullException(nameof(dotNetObject));
            }

            // Look at the SignatureAlgorithm property. If it is "RSA", we can
            // cast the input to RSA.
            if (string.Equals(dotNetObject.SignatureAlgorithm, AlgorithmRsa, StringComparison.Ordinal))
            {
                RSAParameters rsaParams = ((RSA)dotNetObject).ExportParameters(false);
                // This constructor will validate the modulus and exponent.
                var rsaPubKey = new PivRsaPublicKey(rsaParams.Modulus, rsaParams.Exponent);
                return (PivPublicKey)rsaPubKey;
            }

            var eccParams = new ECParameters();

            // If the SignatureAlgorithm is "ECDsa", we can cast to ECDsa.
            if (string.Equals(dotNetObject.SignatureAlgorithm, AlgorithmEcdsa, StringComparison.Ordinal))
            {
                eccParams = ((ECDsa)dotNetObject).ExportParameters(false);
            }
            else if (string.Equals(dotNetObject.KeyExchangeAlgorithm, AlgorithmEcdh, StringComparison.Ordinal))
            {
                eccParams = ((ECDiffieHellman)dotNetObject).ExportParameters(false);
            }

            if (ValidateEccParameters(eccParams))
            {
                int keySize = dotNetObject.KeySize / 8;

                byte[] point = new byte[(keySize * 2) + 1];
                point[0] = 4;
                int offset = 1 + (keySize - eccParams.Q.X.Length);
                Array.Copy(eccParams.Q.X, 0, point, offset, eccParams.Q.X.Length);
                offset += keySize + (keySize - eccParams.Q.Y.Length);
                Array.Copy(eccParams.Q.Y, 0, point, offset, eccParams.Q.Y.Length);

                var eccPubKey = new PivEccPublicKey(point);
                return (PivPublicKey)eccPubKey;
            }

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    InvalidKeyDataMessage));
        }

        // Build an AsymmetricAlgorithm object (either RSA or ECDsa) from a
        // PivPublicKey.
        public static AsymmetricAlgorithm GetDotNetFromPivPublicKey(PivPublicKey pivPublicKey)
        {
            if (pivPublicKey is null)
            {
                throw new ArgumentNullException(nameof(pivPublicKey));
            }

            if (pivPublicKey.Algorithm.IsRsa())
            {
                var rsaPublic = (PivRsaPublicKey)pivPublicKey;

                var rsaParams = new RSAParameters
                {
                    Modulus = rsaPublic.Modulus.ToArray(),
                    Exponent = rsaPublic.PublicExponent.ToArray()
                };

                return RSA.Create(rsaParams);
            }

            var eccCurve = ECCurve.CreateFromValue("1.2.840.10045.3.1.7");
            if (pivPublicKey.Algorithm != PivAlgorithm.EccP256)
            {
                if (pivPublicKey.Algorithm != PivAlgorithm.EccP384)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            InvalidKeyDataMessage));
                }

                eccCurve = ECCurve.CreateFromValue("1.3.132.0.34");
            }

            var eccParams = new ECParameters
            {
                Curve = (ECCurve)eccCurve
            };

            var eccPublic = (PivEccPublicKey)pivPublicKey;
            int coordLength = (eccPublic.PublicPoint.Length - 1) / 2;
            eccParams.Q.X = eccPublic.PublicPoint.Slice(1, coordLength).ToArray();
            eccParams.Q.Y = eccPublic.PublicPoint.Slice(1 + coordLength, coordLength).ToArray();

            return ECDsa.Create(eccParams);
        }

        // Build a PivPrivateKey object from an AsymmetricAlgorithm object that
        // contains a private key.
        public static PivPrivateKey GetPivPrivateKeyFromDotNet(AsymmetricAlgorithm dotNetObject)
        {
            if (dotNetObject is null)
            {
                throw new ArgumentNullException(nameof(dotNetObject));
            }

            var rsaParams = new RSAParameters();
            var eccParams = new ECParameters();

            try
            {
                // Look at the SignatureAlgorithm property. If it is "RSA", we can
                // cast the input to RSA.
                if (string.Equals(dotNetObject.SignatureAlgorithm, AlgorithmRsa, StringComparison.Ordinal))
                {
                    rsaParams = ((RSA)dotNetObject).ExportParameters(true);
                    var rsaPriKey = new PivRsaPrivateKey(
                        rsaParams.P,
                        rsaParams.Q,
                        rsaParams.DP,
                        rsaParams.DQ,
                        rsaParams.InverseQ);
                    return (PivPrivateKey)rsaPriKey;
                }

                // If the SignatureAlgorithm is "ECDsa", we can cast to ECDsa.
                if (string.Equals(dotNetObject.SignatureAlgorithm, AlgorithmEcdsa, StringComparison.Ordinal))
                {
                    eccParams = ((ECDsa)dotNetObject).ExportParameters(true);
                }
                else if (string.Equals(dotNetObject.KeyExchangeAlgorithm, AlgorithmEcdh, StringComparison.Ordinal))
                {
                    eccParams = ((ECDiffieHellman)dotNetObject).ExportParameters(true);
                }

                if (ValidateEccParameters(eccParams))
                {
                    int keySize = dotNetObject.KeySize / 8;

                    byte[] privateValue = new byte[keySize];
                    int offset = keySize - eccParams.D.Length;
                    Array.Copy(eccParams.D, 0, privateValue, offset, eccParams.D.Length);
                    var eccPriKey = new PivEccPrivateKey(privateValue);
                    return (PivPrivateKey)eccPriKey;
                }
            }
            finally
            {
                ClearRsaParameters(rsaParams);
                ClearEccParameters(eccParams);
            }

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    InvalidKeyDataMessage));
        }
    }
}
