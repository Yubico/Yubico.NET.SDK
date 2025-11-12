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
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv;


namespace Yubico.YubiKey.Sample.PivSampleCode
{
    // This file contains methods related to converting into and out of PEM
    // constructions.
    public static partial class KeyConverter
    {
        // Build a new PublicKey object from a PEM key string.
        // This method expects the buffer to contain one key only. It can be of
        // either form
        //    -----BEGIN PUBLIC KEY-----
        //    <base64 data>
        //    -----END PUBLIC KEY-----
        //
        //    -----BEGIN PRIVATE KEY-----
        //    <base64 data>
        //    -----END PRIVATE KEY-----
        

        public static PublicKey GetPublicKeyFromPem(char[] pemKeyString)
        {
            byte[] encodedKey = GetEncodedKey(pemKeyString, false, out int algorithmFlag);

            try
            {
                // algorithmFlag is used as a bitfield. AlgorithmFlagPrivate has value 1
                // and other algorithm flags are distinct powers-of-two. Use bitwise
                // checks instead of integer equality to avoid confusion about "+1".
                bool isCurve25519 = (algorithmFlag & AlgorithmFlagCurve25519) != 0;

                if (isCurve25519)
                {
                    bool isPemPrivateFlag = (algorithmFlag & AlgorithmFlagPrivate) != 0;

                    if (isPemPrivateFlag)
                    {
                        // We received a PKCS#8 private key encoding. The private-key
                        // structure for Curve25519 does not necessarily contain the
                        // public key material, and this sample code does not have an
                        // implementation to derive the Curve25519 public key from the
                        // private scalar. Attempting to parse the PKCS#8 bytes as a
                        // SubjectPublicKeyInfo will cause ASN.1 decoding error
                        throw new CryptographicException(
                            "PEM contains a Curve25519 private key; this method requires a PUBLIC KEY PEM for Curve25519 or an implementation to derive the public key from the private key.");
                    }

                    // For a PUBLIC KEY PEM, encodedKey is a SubjectPublicKeyInfo and
                    // we can decode it directly.
                    return Curve25519PublicKey.CreateFromSubjectPublicKeyInfo(encodedKey);
                }

                // For all other cases (including non-private curve25519 or
                // RSA/ECDSA), fall back to the existing DotNet path.
                using var dotNetObject = GetDotNetFromPem(pemKeyString, false);
                return GetPublicKeyFromDotNet(dotNetObject);
            }
            finally
            {
                OverwriteBytes(encodedKey);
            }
        }

        // Build the PEM string from a PublicKey.
        // This method will build the following PEM format.
        //    -----BEGIN PUBLIC KEY-----
        //    <base64 data>
        //    -----END PUBLIC KEY-----
        

        public static char[] GetPemFromPublicKey(IPublicKey publicKey)
        {
            return PemOperations.BuildPem(PublicKeyTitle, publicKey.ExportSubjectPublicKeyInfo());
        }

        // Build a new PrivateKey object from a PEM key string.
        // This method expects the PEM key to be of the form
        //    -----BEGIN PRIVATE KEY-----
        //    <base64 data>
        //    -----END PRIVATE KEY-----
        

        public static PrivateKey GetPrivateKeyFromPem(char[] pemKeyString)
        {
            byte[] encodedKey = GetEncodedKey(pemKeyString, true, out int algorithmFlag);

            try
            {
                // algorithmFlag is used as a bitfield. AlgorithmFlagPrivate has value 1
                // and other algorithm flags are distinct powers-of-two. Use bitwise checks
                bool isCurve25519 = (algorithmFlag & AlgorithmFlagCurve25519) != 0;

                if (isCurve25519)
                {
                    // For Curve25519, decode the PKCS#8 format directly.
                    return Curve25519PrivateKey.CreateFromPkcs8(encodedKey);
                }

                // For all other cases (including non-private curve25519 or
                // RSA/ECDSA), fall back to the existing DotNet path.
                using var dotNetObject = GetDotNetFromPem(pemKeyString, true);
                return GetPrivateKeyFromDotNet(dotNetObject);
            }
            finally
            {
                OverwriteBytes(encodedKey);
            }
        }

        // Build an AsymmetricAlgorithm object from a PEM key.
        // If the isPrivate arg is true, this method expects the PEM key to be of
        // the form
        //    -----BEGIN PRIVATE KEY-----
        //    <base64 data>
        //    -----END PRIVATE KEY-----
        // If not, the method will throw an exception.
        //
        // If the isPrivate arg is false, the method will accept the PEM private
        // key format or
        //    -----BEGIN PUBLIC KEY-----
        //    <base64 data>
        //    -----END PUBLIC KEY-----
        // Regardless of the format, the method will build an AsymmetricAlgorithm
        // object that contains only the public key.
        // If the key is ECC, this method will build an ECDsa object. If you need
        // it as ECDiffieHellman, you can build a new ECDH object from the params
        // of the ECDsa object.
        public static AsymmetricAlgorithm GetDotNetFromPem(char[] pemKeyString, bool isPrivate)
        {
            byte[] encodedKey = Array.Empty<byte>();
            var rsaParams = new RSAParameters();
            var eccParams = new ECParameters();

            try
            {
                encodedKey = GetEncodedKey(pemKeyString, isPrivate, out int algorithmFlag);

                switch (algorithmFlag)
                {
                    default:
                        throw new InvalidOperationException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                InvalidKeyDataMessage));

                    case AlgorithmFlagRsa:
                        var rsaObject = RSA.Create();
                        rsaObject.ImportSubjectPublicKeyInfo(encodedKey, out _);
                        return rsaObject;

                    case AlgorithmFlagRsa | AlgorithmFlagPrivate:
                        using (var rsaPrivateObject = RSA.Create())
                        {
                            rsaPrivateObject.ImportPkcs8PrivateKey(encodedKey, out _);
                            if (isPrivate)
                            {
                                rsaParams = rsaPrivateObject.ExportParameters(true);
                            }
                            else
                            {
                                // We have a private DotNet object, but the caller wanted
                                // a public. Get the public params out and build a new
                                // object.
                                rsaParams = rsaPrivateObject.ExportParameters(false);
                            }
                            return RSA.Create(rsaParams);
                        }

                    case AlgorithmFlagEcdsa:
                        var eccObject = ECDsa.Create();
                        eccObject.ImportSubjectPublicKeyInfo(encodedKey, out _);
                        return eccObject;

                    case AlgorithmFlagCurve25519:
                    case AlgorithmFlagCurve25519 | AlgorithmFlagPrivate:
                        throw new NotSupportedException(
                            "Curve25519 keys cannot be converted to AsymmetricAlgorithm. Use GetPrivateKeyFromPem instead.");

                    case AlgorithmFlagEcdsa | AlgorithmFlagPrivate:
                        using (var eccPrivateObject = ECDsa.Create())
                        {
                            eccPrivateObject.ImportPkcs8PrivateKey(encodedKey, out _);
                            if (isPrivate)
                            {
                                eccParams = eccPrivateObject.ExportParameters(true);
                            }
                            else
                            {
                                // We have a private DotNet object, but the caller wanted
                                // a public. Get the public params out and build a new
                                // object.
                                eccParams = eccPrivateObject.ExportParameters(false);
                            }
                            return ECDsa.Create(eccParams);
                        }
                }
            }
            finally
            {
                OverwriteBytes(encodedKey);
                ClearRsaParameters(rsaParams);
                ClearEccParameters(eccParams);
            }
        }

        // Build a PEM key string. This method will build either
        //    -----BEGIN PUBLIC KEY-----
        //    <base64 data>
        //    -----END PUBLIC KEY-----
        // or
        //    -----BEGIN PRIVATE KEY-----
        //    <base64 data>
        //    -----END PRIVATE KEY-----
        // If the isPrivate arg is true, the method will extract the PKCS 8
        // PrivateKeyInfo from the dotNetObject and build the PRIVATE KEY.
        // If the isPrivate arg is false, the method will extract the
        // SubjectPublicKeyInfo and build the PUBLIC KEY. It will do so even if
        // the dotNetObject contains the private key.
        // If isPrivate is true and the dotNetObject does not contain the private
        // key, this method will throw an exception.
        public static char[] GetPemFromDotNet(AsymmetricAlgorithm dotNetObject, bool isPrivate)
        {
            if (dotNetObject is null)
            {
                throw new ArgumentNullException(nameof(dotNetObject));
            }

            byte[] encodedKey = Array.Empty<byte>();
            string title;

            try
            {
                if (isPrivate)
                {
                    encodedKey = dotNetObject.ExportPkcs8PrivateKey();
                    title = PrivateKeyTitle;
                }
                else
                {
                    encodedKey = dotNetObject.ExportSubjectPublicKeyInfo();
                    title = PublicKeyTitle;
                }

                return PemOperations.BuildPem(title, encodedKey);
            }
            finally
            {
                OverwriteBytes(encodedKey);
            }
        }

        // Base64 decode the PEM key and return a new byte array containing the
        // result. Return an empty array on error.
        // If the isPrivate arg is true, the key must be private. If not, return
        // an empty encodedKey.
        // If the isPrivate arg is false, the key can be private or public. Go
        // ahead and return the encoded key.
        // Set the algorithmFlag to the algorithm (See AlgorithmFlag const
        // values). If there's an error set it to AlgorithmFlagNone.
        // This works for only PUBLIC KEY (SubjectPublicKeyInfo) and PRIVATE KEY
        // (PrivateKeyInfo).
        // Determine the algorithm based on the OID in the algorithm identifier.
        // For both pub and pri, read tags until reaching 06. For private, there
        // is an INTEGER in there, but all other tags will be SEQUENCE until
        // hitting the OID.
        private static byte[] GetEncodedKey(char[] pemKeyString, bool isPrivate, out int algorithmFlag)
        {
            algorithmFlag = AlgorithmFlagNone;
            byte[] encodedKey = Array.Empty<byte>();

            try
            {
                encodedKey = PemOperations.GetEncodingFromPem(pemKeyString, out string title);

                bool isPemPrivate = true;
                if (!string.Equals(PrivateKeyTitle, title, StringComparison.Ordinal))
                {
                    isPemPrivate = false;
                    if (!string.Equals(PublicKeyTitle, title, StringComparison.Ordinal))
                    {
                        return Array.Empty<byte>();
                    }
                }

                // If the caller wanted the private key, and this is the public
                // key, return an empty array.
                if (isPrivate && !isPemPrivate)
                {
                    return Array.Empty<byte>();
                }

                // There is an OID inside the Pkcs8PrivateKey and the
                // SubjectPublicKeyInfo that tells us whether this is RSA or ECC.
                // However, C# does not have any publicly available way to read
                // DER encoded data (it does but is only available in .NET 5.0).
                // For now, we'll write our own code to find the OID.
                // The DER encoding will be either
                //   30 len 02 len value 30 len 06 len OID
                // or
                //   30 len 30 len 06 len OID
                // So for this sample, we'll use a local method that reads TL or
                // TLV and returns the offset to the next tag. Read until we hit
                // the 06 (the OID tag).
                int offset = 0;
                do
                {
                    offset = GetNextTagOffset(encodedKey, offset);
                    if (offset < 0)
                    {
                        return Array.Empty<byte>();
                    }
                } while (encodedKey[offset] != 6);

                // encodedKey[offset] is where the OID begins.
                //   RSA: 06 09
                //           2A 86 48 86 F7 0D 01 01 01
                //   ECC: 06 07
                //           2A 86 48 CE 3D 02 01
                // Read the OID length and value bytes in a robust, ASN.1-aware way
                // encodedKey[offset] is the OID tag (0x06). The length byte(s)
                // follow at offset+1, and the value bytes follow the length field.
                int oidLenIndex = offset + 1;
                if (oidLenIndex >= encodedKey.Length)
                {
                    return Array.Empty<byte>();
                }

                int firstLen = encodedKey[oidLenIndex] & 0xFF;
                int oidLen;
                int oidValueStart;

                if (firstLen <= 0x7F)
                {
                    oidLen = firstLen;
                    oidValueStart = oidLenIndex + 1;
                }
                else
                {
                    // Long-form length. Support up to 3 length bytes as in GetNextTagOffset.
                    if (firstLen == 0x80 || firstLen > 0x83)
                    {
                        return Array.Empty<byte>();
                    }

                    int count = firstLen & 0x0F;
                    if (encodedKey.Length < oidLenIndex + 1 + count)
                    {
                        return Array.Empty<byte>();
                    }

                    oidLen = 0;
                    for (int i = 0; i < count; i++)
                    {
                        oidLen = (oidLen << 8) + (encodedKey[oidLenIndex + 1 + i] & 0xFF);
                    }

                    oidValueStart = oidLenIndex + 1 + count;
                }

                if (oidValueStart + oidLen > encodedKey.Length)
                {
                    return Array.Empty<byte>();
                }

                // Known OID value byte sequences (DER encoded value bytes):
                // RSA:      1.2.840.113549.1.1.1 -> 2A 86 48 86 F7 0D 01 01 01
                // ECDSA:    1.2.840.10045.2.1     -> 2A 86 48 CE 3D 02 01
                // Ed25519:  1.3.101.112           -> 2B 65 70
                // X25519:   1.3.101.110           -> 2B 65 6E
                byte[] oidRsa = new byte[] { 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01 };
                byte[] oidEcdsa = new byte[] { 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x02, 0x01 };
                byte[] oidEd25519 = new byte[] { 0x2B, 0x65, 0x70 };
                byte[] oidX25519 = new byte[] { 0x2B, 0x65, 0x6E };

                bool Matches(byte[] candidate)
                {
                    if (candidate.Length != oidLen)
                    {
                        return false;
                    }

                    for (int i = 0; i < candidate.Length; i++)
                    {
                        if (encodedKey[oidValueStart + i] != candidate[i])
                        {
                            return false;
                        }
                    }

                    return true;
                }

                if (Matches(oidRsa))
                {
                    algorithmFlag = AlgorithmFlagRsa;
                }
                else if (Matches(oidEcdsa))
                {
                    algorithmFlag = AlgorithmFlagEcdsa;
                }
                else if (Matches(oidEd25519) || Matches(oidX25519))
                {
                    algorithmFlag = AlgorithmFlagCurve25519;
                }
                else
                {
                    return Array.Empty<byte>();
                }

                if (isPemPrivate)
                {
                    algorithmFlag |= AlgorithmFlagPrivate;
                }

                return encodedKey;
            }
            finally
            {
                if (algorithmFlag == AlgorithmFlagNone)
                {
                    OverwriteBytes(encodedKey);
                }
            }
        }

        // Return the offset to the next tag. If there's an error, return -1.
        // Read the current tag and length at buffer[offset]. If the current tag
        // is 30, just return the offset beyond the TL. If the current tag is not
        // 30, return the offset beyond the TLV.
        private static int GetNextTagOffset(byte[] buffer, int offset)
        {
            // Make sure there are enough bytes to read.
            if (offset < 0 || buffer.Length < offset + 2)
            {
                return -1;
            }

            // Read the value (the V of TLV) only if the current tag is not 30.
            bool readValue = buffer[offset] != 0x30;

            // Look at the first length octet.
            // If the length is 0x7F or less, the length is one octet.
            // If the length octet is 0x80, that's BER and we shouldn't see it.
            // Otherwise the length octet should be 81, 82, or 83 (technically it
            // could be 84 or higher, but this method does not support anything
            // beyond 83). This says the length is the next 1, 2, or 3 octets.
            int length = buffer[offset + 1];
            int increment = 2;
            if (length == 0x80 || length > 0x83)
            {
                return -1;
            }
            if (length > 0x80)
            {
                int count = length & 0xf;
                if (buffer.Length < offset + increment + count)
                {
                    return -1;
                }
                increment += count;
                length = 0;
                while (count > 0)
                {
                    length <<= 8;
                    length += (int)buffer[offset + increment - count] & 0xFF;
                    count--;
                }
            }

            if (readValue)
            {
                if (buffer.Length < offset + increment + length)
                {
                    return -1;
                }

                increment += length;
            }

            return offset + increment;
        }
    }
}
