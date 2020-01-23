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
using System.Security.Cryptography;

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    // This class contains methods that convert between key formats.
    //
    //   PEM string (PRIVATE KEY or PUBLIC KEY)
    //   PivPublicKey
    //   PivPrivateKey
    //   System.Security.Cryptography.RSA
    //   System.Security.Cryptography.ECDsa
    //   System.Security.Cryptography.ECDiffieHellman
    //
    // Each method will be "Get<format>From<format>", such as
    // "GetPemFromPivPublicKey" or "GetPivPrivateKeyFromDotNet".
    //
    // For example, if you have a PEM key:
    //
    //    -----BEGIN PRIVATE KEY-----
    //    <base64 data>
    //    -----END PRIVATE KEY-----
    // or
    //    -----BEGIN PUBLIC KEY-----
    //    <base64 data>
    //    -----END PUBLIC KEY-----
    //
    // call the relevant "FromPem" method to get that key data returned as a
    // PivPublicKey, PivPrivateKey, or an AsymmetricAlgorithm object. Note that
    // this class uses the term "DotNet" as one of the formats. That denotes the
    // input object or result returned will be from the .NET Base Class Libraries
    // (BCL), namely the AsymmetricAlgorithm object.
    //
    // When converting to DotNet, the AsymmetricAlgorithm object returned will be
    // an instance of System.Security.Cryptography.AsymmetricAlgorithm. That is
    // an abstract base class, the actual object will be an instance of either
    // RSA or ECDsa. Check the SignatureAlgorithm property to see what the
    // algorithm is. If the object returned is ECDsa, but you need it as
    // ECDiffieHellman, you can build a new ECDH object from the params of the
    // ECDsa object.
    //
    // When converting from DotNet, the AsymmetricAlgorithm can be an instance of
    // either RSA, ECDsa, or ECDiffieHellman.
    //
    // These are the following conversions in this class.
    //
    //   From:                      To:
    //
    //     PEM                        PivPublicKey
    //                                PivPrivateKey
    //                                DotNet
    //
    //     PivPublicKey               PEM
    //                                DotNet
    //
    //     PivPrivateKey              --none--
    //
    //     DotNet                     PEM
    //                                PivPublicKey
    //                                PivPrivateKey
    //
    // A DotNet (AsymmetricAlgorithm) object contains either the public key only
    // or the public and private key. If an object contains only the public key,
    // then of course you will not be able to build a PivPrivateKey or a PEM
    // private key. But if the object contains a private key, then you will be
    // able to build a PivPublicKey or a PEM public key from that object.
    //
    // A PEM private key also contains a public key. If you want to get the
    // PivPublicKey out of that PEM construction, you will have to build a
    // DotNet (AsymmetricAlgorithm) object and and then convert from DotNet to
    // PivPublicKey or PEM.
    //
    // For all From PEM methods, the key data must be of the form
    //    -----BEGIN PRIVATE KEY-----
    //    <base64 data>
    //    -----END PRIVATE KEY-----
    // or
    //    -----BEGIN PUBLIC KEY-----
    //    <base64 data>
    //    -----END PUBLIC KEY-----
    // If there are any "stray" characters at the beginning or end (even new
    // line or other whitespace), the method will throw an exception.
    // Note that there can be new line characters after the BEGIN line and
    // before the END line, just make sure there is nothing immediately in
    // front of the BEGIN line and nothing immediately after the END line.
    //
    // All PEM methods (To and From) use a char array. This is so that you can
    // overwrite sensitive data if you want. The string class is immutable, so if
    // you have a private key in PEM format, you cannot overwrite it when it's no
    // longer needed. But if you have your private key as a char[], you can
    // overwrite it when you're done with it (Array.Fill).
    // If you want to deal with strings, and are not concerned with
    // overwriting buffers, you can still use strings, just use the
    // ToCharArray method if your PEM data is a string, and if you have
    // output from this class as a char[], use the String constructor that
    // takes in a char[].
    //
    // This class accepts keys that are either
    //   RSA 1024, with public exponent of F4 (0x01 00 01 = decimal 65,537)
    //   RSA 2048, with public exponent of F4
    //   ECC from the NIST curve P256
    //   ECC from the NIST curve P384
    // If it encounters any other keys it will throw an exception.
    // For example, if a PEM key to convert is DSA, or it is RSA 2048 with public
    // exponent 3, the method will throw an exception.
    public static partial class KeyConverter
    {
        private const string InvalidKeyDataMessage = "The input key data was not recognized.";

        // Bit field. Rsa and Private set, it is an RSA private key. If only the
        // Rsa bit is set, it is RSA public. Same for Ecdsa.
        private const int AlgorithmFlagNone = 0;
        private const int AlgorithmFlagPrivate = 1;
        private const int AlgorithmFlagRsa = 2;
        private const int AlgorithmFlagEcdsa = 4;

        private const string AlgorithmRsa = "RSA";
        private const string AlgorithmEcdsa = "ECDsa";
        private const string AlgorithmEcdh = "ECDiffieHellman";
        private const string PrivateKeyTitle = "PRIVATE KEY";
        private const string PublicKeyTitle = "PUBLIC KEY";

        private const string OidP256 = "1.2.840.10045.3.1.7";
        private const string OidP384 = "1.3.132.0.34";

        // Make sure the params are P256 or P384.
        private static bool ValidateEccParameters(ECParameters eccParams)
        {
            if (string.Compare(eccParams.Curve.Oid.Value, OidP256, StringComparison.Ordinal) != 0)
            {
                if (string.Compare(eccParams.Curve.Oid.Value, OidP384, StringComparison.Ordinal) != 0)
                {
                    return false;
                }
            }

            return true;
        }

        public static void ClearRsaParameters(RSAParameters rsaParams)
        {
            OverwriteBytes(rsaParams.P);
            OverwriteBytes(rsaParams.Q);
            OverwriteBytes(rsaParams.DP);
            OverwriteBytes(rsaParams.DQ);
            OverwriteBytes(rsaParams.InverseQ);
            OverwriteBytes(rsaParams.D);

            rsaParams.D = null;
            rsaParams.DP = null;
            rsaParams.DQ = null;
            rsaParams.Exponent = null;
            rsaParams.InverseQ = null;
            rsaParams.Modulus = null;
            rsaParams.P = null;
            rsaParams.Q = null;
        }

        private static void ClearEccParameters(ECParameters eccParams)
        {
            OverwriteBytes(eccParams.D);
            eccParams.D = null;
        }

        public static void OverwriteBytes(byte[] buffer)
        {
            if (!(buffer is null))
            {
                Array.Fill<byte>(buffer, 0);
            }
        }

        public static void OverwriteChars(char[] buffer)
        {
            if (!(buffer is null))
            {
                Array.Fill<char>(buffer, '0');
            }
        }
    }
}
