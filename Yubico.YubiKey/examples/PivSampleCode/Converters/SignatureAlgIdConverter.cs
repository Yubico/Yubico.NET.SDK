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
using System.Security.Cryptography;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    // This class holds an AlgorithmIdentifier and its info, when the AlgId is
    // for a signature algorithm.
    // Currently supported signature algorithms and support algorithms are
    //   RSA
    //     PKCS 1 v1.5
    //     PKCS 1 PSS
    //       MGF1
    //   ECDSA
    //
    //   Each signature algorithm works with a message digest.
    //   The message digests supported are the following.
    //   SHA1
    //   SHA256
    //   SHA384
    //   SHA512
    //
    // Suppose you have the DER encoding of an AlgorithmIdentifier. What
    // algorithm does it represent? What are its parameters?
    // Build an instance of this class with the DER of the algId, and then look
    // at the properties to determine what the algorithm and parameters are.
    //
    // For example, if the algId is for RSA with SHA-1 and PKCS #1 v1.5 padding,
    // create and instance of this class using the encoded algId. After
    // instantiation, the fields will be
    //
    //   AlgorithmName    "RSA"
    //   HashAlgorithm    HashAlgorithmName.SHA1
    //   Padding          RSASignaturePadding.Pkcs1
    //   PssSaltLength    0
    //   PssTrailerField  0
    //
    // In the future, this class might be extended to be able to build an
    // algId from the information.
    public class SignatureAlgIdConverter
    {
        public const string AlgorithmRsa = "RSA";
        public const string AlgorithmEcdsa = "ECDsa";
        public const string AlgorithmUnknown = "Unknown";

        public const byte TrailerBC = 0xBC;

        // This will be the name the .NET Base Class Libraries uses. For RSA, the
        // string is "RSA", for ECDSA, the string is "ECDsa".
        // If the algId is not for a signature algorithm, this will be "Unknown".
        public string AlgorithmName { get; private set; }

        // This is the digest algorithm used.
        public HashAlgorithmName HashAlgorithm { get; private set; }

        // This is the RSA padding scheme.
        // If the algorithm is "RSA", and the padding is Pss, then the PSS digest
        // algorithm and the MGF supporting algorithm will be the same as the
        // HashAlgorithm.
        // Otherwise, this can be ignored. It will be RSASignaturePadding.Pkcs1
        // because there is no option for None.
        public RSASignaturePadding Padding { get; private set; }

        // If the algorithm is "RSA" and the Padding is Pss, this will be the
        // length, in bytes, of the salt used. Otherwise this will be 0 and can
        // be ignored.
        public int PssSaltLength { get; private set; }

        // If the algorithm is "RSA" and the Padding is Pss, this will be the
        // trailer field value (the last byte of the padded block). Otherwise
        // this will be 0 and can be ignored.
        public byte PssTrailerField { get; private set; }

        // Build a new instance of the SignatureAlgIdConverter class. The input
        // is the DER encoding of an algId. Check the properties after
        // instantiation to get information about the algorithm represented.
        // If the algId was not one of the supported algorithms, the Algorithm
        // property will be SignatureAlgIdConverter.AlgorithmUnknown (the string
        // "Unknown").
        public SignatureAlgIdConverter(byte[] algIdDer)
        {
            AlgorithmName = AlgorithmUnknown;
            HashAlgorithm = HashAlgorithmName.MD5;
            Padding = RSASignaturePadding.Pkcs1;
            PssSaltLength = 0;
            PssTrailerField = 0;
            var tlvReader = new TlvReader(algIdDer);
            var seqReader = tlvReader.ReadNestedTlv(0x30);
            var oid = seqReader.ReadValue(0x06);

            if (SetFromOid(oid))
            {
                var algIdParams = new ReadOnlyMemory<byte>(new byte[] { 0x30, 0x00 });

                // We're expecting parameters. If there is no data left to read,
                // there are no params. That is generally an error. But we're
                // going to allow for it because if we reach this point, the
                // algorithm is RSA with PSS. In that case, it is possible to
                // have 30 00 for the params, which really means no params, just
                // use the DEFAULT for everything. The DEFAULT is SHA-1.
                if (seqReader.HasData)
                {
                    algIdParams = seqReader.ReadEncoded(0x30);
                }

                ReadPssParams(algIdParams);
            }
        }

        // Based on the OID, set as many properties as possible. Certainly the
        // Algorithm, but possibly the HashAlgorithm as well. If RSA, the
        // Padding, too.
        // Return true if there are parameters to be read. False otherwise.
        // If the OID is unknown, don't set Algorithm (it was init to "Unknown").
        private bool SetFromOid(ReadOnlyMemory<byte> oid)
        {
            byte[] target;
            ReadOnlySpan<byte> oidSpan;

            // This class supports
            //   0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, y
            // where y = 5, 11, 12, 13, and 10
            // This OID is for RSA
            // With y = 5 (SHA-1), 11 (SHA-256), 12 (SHA-384), 13 (SHA-512)
            // it's P1.5 and there are no params.
            // With y = 10 it is PSS and the digest algorithm is in the params.
            if (oid.Length == 9)
            {
                target = new byte[] { 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01 };
                oidSpan = oid.Span.Slice(0, 8);

                if (MemoryExtensions.SequenceEqual(oidSpan, target) == false)
                {
                    return false;
                }

                switch (oid.Span[oid.Length - 1])
                {
                    default:
                        return false;

                    case 10:
                        Padding = RSASignaturePadding.Pss;
                        break;

                    case 5:
                        HashAlgorithm = HashAlgorithmName.SHA1;
                        break;

                    case 11:
                        HashAlgorithm = HashAlgorithmName.SHA256;
                        break;

                    case 12:
                        HashAlgorithm = HashAlgorithmName.SHA384;
                        break;

                    case 13:
                        HashAlgorithm = HashAlgorithmName.SHA512;
                        break;
                }

                AlgorithmName = AlgorithmRsa;

                return Padding.Mode == RSASignaturePaddingMode.Pss;
            }

            // This class supports
            //   0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x04, 0x01
            // which is ECDSA with SHA-1.
            if (oid.Length == 7)
            {
                target = new byte[] { 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x04, 0x01 };

                if (MemoryExtensions.SequenceEqual(oid.Span, target))
                {
                    AlgorithmName = AlgorithmEcdsa;
                    HashAlgorithm = HashAlgorithmName.SHA1;
                }

                return false;
            }

            if (oid.Length != 8)
            {
                return false;
            }

            // This class supports
            //   0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x04, 0x03, z
            // where z = 2, 3, 4
            // This OID is for ECDSA with SHA-256 (2), SHA-384 (3), or
            // SHA-512 (4)
            target = new byte[] { 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x04, 0x03 };
            oidSpan = oid.Span.Slice(0, 7);

            if (MemoryExtensions.SequenceEqual(oidSpan, target) == false)
            {
                return false;
            }

            switch (oid.Span[oid.Length - 1])
            {
                default:
                    return false;

                case 2:
                    HashAlgorithm = HashAlgorithmName.SHA256;
                    break;

                case 3:
                    HashAlgorithm = HashAlgorithmName.SHA384;
                    break;

                case 4:
                    HashAlgorithm = HashAlgorithmName.SHA512;
                    break;
            }

            AlgorithmName = AlgorithmEcdsa;

            return false;
        }

        // Determine the PSS info from the encoded params.
        // This class accepts only two values:
        //   30 00
        // or
        //   30 30
        //      a0 0d
        //         30 0b
        //            06 09
        //               60 86 48 01 65 03 04 02 y
        //      a1 1a
        //         30 18
        //            06 09
        //               2a 86 48 86 f7 0d 01 01 08
        //            30 0b
        //               06 09
        //                  60 86 48 01 65 03 04 02 y
        //      a2 03
        //         02 01
        //            len(y)
        // Anything else is unsupported.
        // The value of y can be 1 (SHA-256), 2 (SHA-384), or 3 (SHA-512).
        // If the params are 30 00, then the HashAlgorithm is SHA-1.
        // Otherwise, look at params[16] and [44] to determine the HashAlgorithm.
        // Then verify the len(y) is supported.
        private void ReadPssParams(ReadOnlyMemory<byte> algIdParams)
        {
            if (algIdParams.Length == 2)
            {
                if (algIdParams.Span[0] == 0x30 && algIdParams.Span[1] == 0)
                {
                    PssSaltLength = 20;
                }
            }
            else if (algIdParams.Length == 50)
            {
                if (algIdParams.Span[16] == algIdParams.Span[44])
                {
                    PssSaltLength = algIdParams.Span[16] switch
                    {
                        1 => 32,
                        2 => 48,
                        3 => 64,
                        _ => 0,
                    };
                }
            }

            switch (PssSaltLength)
            {
                default:
                    AlgorithmName = AlgorithmUnknown;
                    return;

                case 20:
                    HashAlgorithm = HashAlgorithmName.SHA1;
                    break;

                case 32:
                    HashAlgorithm = HashAlgorithmName.SHA256;
                    break;

                case 48:
                    HashAlgorithm = HashAlgorithmName.SHA384;
                    break;

                case 64:
                    HashAlgorithm = HashAlgorithmName.SHA512;
                    break;
            }

            PssTrailerField = TrailerBC;
        }
    }
}
