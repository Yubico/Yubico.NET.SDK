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
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.TestUtilities 
{
    public static class SampleKeyPairs
    {
        // Get a private key with its matching certificate
        public static bool GetMatchingKeyAndCert(
            PivAlgorithm algorithm, 
            out X509Certificate2? cert,
            out PivPrivateKey? privateKey)
        {
            string curve = GetCurveFromAlgorithm(algorithm);
            if (string.IsNullOrEmpty(curve))
            {
                cert = null;
                privateKey = null;
                return false;
            }

            cert = TestKeys.GetCertificate(curve).AsX509Certificate2();
            privateKey = TestKeys.GetPrivateKey(curve).AsPrivateKey();
            return true;
        }

        public static bool GetKeysAndCertPem(
            PivAlgorithm algorithm,
            bool validAttest,
            out string? cert, 
            out string? publicKey,
            out string? privateKey)
        {
            string curve = GetCurveFromAlgorithm(algorithm);
            if (string.IsNullOrEmpty(curve))
            {
                cert = null;
                privateKey = null; 
                publicKey = null;
                return false;
            }

            var testCert = TestKeys.GetCertificate(curve, validAttest);
            var testPrivKey = TestKeys.GetPrivateKey(curve);
            var testPubKey = TestKeys.GetPublicKey(curve);

            cert = testCert.AsPemString();
            privateKey = testPrivKey.AsPemString();
            publicKey = testPubKey.AsPemString();
            return true;
        }

        public static PivPublicKey GetPivPublicKey(PivAlgorithm algorithm)
        {
            var curve = GetCurveFromAlgorithm(algorithm);
            return TestKeys.GetPublicKey(curve).AsPublicKey();
        }

        public static PivPrivateKey GetPivPrivateKey(PivAlgorithm algorithm) 
        {
            var curve = GetCurveFromAlgorithm(algorithm);
            return TestKeys.GetPrivateKey(curve).AsPrivateKey();
        }

        public static X509Certificate2 GetCert(PivAlgorithm algorithm)
        {
            var curve = GetCurveFromAlgorithm(algorithm);
            return TestKeys.GetCertificate(curve).AsX509Certificate2();
        }

        private static string GetCurveFromAlgorithm(PivAlgorithm algorithm)
        {
            return algorithm switch
            {
                PivAlgorithm.Rsa1024 => "rsa1024",
                PivAlgorithm.Rsa2048 => "rsa2048", 
                PivAlgorithm.Rsa3072 => "rsa3072",
                PivAlgorithm.Rsa4096 => "rsa4096",
                PivAlgorithm.EccP256 => "p256",
                PivAlgorithm.EccP384 => "p384",
                PivAlgorithm.EccP521 => "p521",
                _ => throw new ArgumentException("No curve mapped", nameof(algorithm))
            };
        }
    }
}
