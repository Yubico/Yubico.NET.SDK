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
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    // This file contains methods related to converting from a PivPublicKey into
    // an AsymmetricAlgorithm, and converting from AsymmetricAlgorithm
    // (the C# classes RSA and ECDsa) into PivPublicKey and PivPrivateKey.
    public static partial class KeyConverter
    {
        // Build a PublicKey object from an AsymmetricAlgorithm object.
        public static Cryptography.PublicKey GetPublicKeyFromDotNet(AsymmetricAlgorithm dotNetObject) 
        {
            if (dotNetObject is null)
            {
                throw new ArgumentNullException(nameof(dotNetObject));
            }

            // Look at the SignatureAlgorithm property. If it is "RSA", we can
            // cast the input to RSA.
            if (string.Equals(dotNetObject.SignatureAlgorithm, AlgorithmRsa, StringComparison.Ordinal))
            {
                var rsaParams = ((RSA)dotNetObject).ExportParameters(false);
                // This constructor will validate the modulus and exponent.
                var rsaPubKey = RSAPublicKey.CreateFromParameters(rsaParams);
                return rsaPubKey;
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
                var eccPubKey = ECPublicKey.CreateFromParameters(eccParams);
                return eccPubKey;
            }

            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,InvalidKeyDataMessage));

        }

        // Build an AsymmetricAlgorithm object (either RSA or ECDsa) from a
        // PublicKey.


        public static AsymmetricAlgorithm GetDotNetFromPublicKey(IPublicKey publicKey)
        {
            if (publicKey is null)
            {
                throw new ArgumentNullException(nameof(publicKey));
            }

            if (publicKey.KeyType.IsRSA())
            {
                var rsaPubKey = (RSAPublicKey)publicKey;

                var rsaParams = new RSAParameters
                {
                    Modulus = rsaPubKey.Parameters.Modulus,
                    Exponent = rsaPubKey.Parameters.Exponent
                };

                return RSA.Create(rsaParams);
            }

            var eccCurve = ECCurve.CreateFromValue("1.2.840.10045.3.1.7");
            if (publicKey.KeyType != KeyType.ECP256)
            {
                if (publicKey.KeyType != KeyType.ECP384)
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

            var ecPublic = (ECPublicKey)publicKey;
            int coordLength = (ecPublic.PublicPoint.Length - 1) / 2;
            eccParams.Q.X = ecPublic.PublicPoint.Slice(1, coordLength).ToArray();
            eccParams.Q.Y = ecPublic.PublicPoint.Slice(1 + coordLength, coordLength).ToArray();

            return ECDsa.Create(eccParams);
        }
        // Build a PrivateKey object from an AsymmetricAlgorithm object that
        // contains a private key.
        public static PrivateKey GetPrivateKeyFromDotNet(AsymmetricAlgorithm dotNetObject)
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
                    
                    var rsaPriKey = RSAPrivateKey.CreateFromParameters(rsaParams);
                    return rsaPriKey;
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
                    var eccPriKey = ECPrivateKey.CreateFromParameters(eccParams);
                    return eccPriKey;
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
