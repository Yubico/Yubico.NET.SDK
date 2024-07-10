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
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    // This class demonstrates how to perform some hash (message digest)
    // operations using the .NET Base Class Library.
    // This sample demonstrates operations that are not part of PIV or the SDK.
    // It is only presented as a convenience to Yubico's customers.
    public static class MessageDigestOperations
    {
        // Create the message digest (hash) of the inputData.
        // This method digests all the bytes in dataToDigest, using the specified
        // hashAlgorithm.
        public static byte[] ComputeMessageDigest(byte[] dataToDigest, HashAlgorithmName hashAlgorithm)
        {
            if (dataToDigest is null)
            {
                throw new ArgumentNullException(nameof(dataToDigest));
            }

            // The CryptographyProviders class is in
            // Yubico.Authenticators.Cryptography. It is used to provide various
            // crypto objects. By default, this class returns instances of the
            // default .NET BCL classes. It is possible to change the
            // CryptographyProviders class to return instances of different
            // classes, so long as they are subclasses of the appropriate .NET
            // classes.
            using HashAlgorithm digester = hashAlgorithm.Name switch
            {
                "SHA1" => CryptographyProviders.Sha1Creator(),
                "SHA256" => CryptographyProviders.Sha256Creator(),
                "SHA384" => CryptographyProviders.Sha384Creator(),
                "SHA512" => CryptographyProviders.Sha512Creator(),
                _ => throw new ArgumentException("Unsupported by sample code")
            };

            byte[] digest = new byte[digester.HashSize / 8];

            _ = digester.TransformFinalBlock(dataToDigest, inputOffset: 0, dataToDigest.Length);
            Array.Copy(digester.Hash, sourceIndex: 0, digest, destinationIndex: 0, digest.Length);

            return digest;
        }
    }
}
