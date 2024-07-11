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
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    // This class demonstrates how to perform some public key operations using
    // the .NET Base Class Library.
    // This sample demonstrates operations that are not part of PIV or the SDK.
    // It is only presented as a convenience to Yubico's customers.
    public static class PublicKeyOperations
    {
        // Use the .NET BCL to verify a signature.
        // The return value is a bool to indicate whether the function was able
        // to complete or not (this is used by the menu code to determine whether
        // it should run again or give up).
        // The result of the verification is the out bool arg isVerified.
        // Note that a signature not verifying is not an error. This method's job
        // is to determine if the signature verifies or not. If it is able to
        // make that determination, it succeeded at its job.
        // Hence, look at the isVerified result (not the return value) to
        // determine if the signature verified or not.
        // If the publicKey is ECC, the paddingScheme argument is ignored.
        public static bool SampleVerifySignature(
            PivPublicKey publicKey,
            byte[] dataToVerify,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding paddingScheme,
            byte[] signature,
            out bool isVerified)
        {
            if (publicKey is null)
            {
                throw new ArgumentNullException(nameof(publicKey));
            }

            using AsymmetricAlgorithm asymObject = KeyConverter.GetDotNetFromPivPublicKey(publicKey);

            // The algorithm is either RSA or ECC, otherwise the KeyConverter
            // call would have thrown an exception.
            if (publicKey.Algorithm.IsRsa())
            {
                var rsaObject = (RSA)asymObject;
                isVerified = rsaObject.VerifyData(dataToVerify, signature, hashAlgorithm, paddingScheme);
            }
            else
            {
                var eccObject = (ECDsa)asymObject;

                // This .NET class and method require the ECC signature to be in
                // a non-standard format.
                byte[] nonStandardSignature = DsaSignatureConverter.GetNonStandardDsaFromStandard(
                    signature, publicKey.Algorithm);
                isVerified = eccObject.VerifyData(dataToVerify, nonStandardSignature, hashAlgorithm);
            }

            return true;
        }

        // Use the .NET BCL to encrypt data using RSA.
        // The return value is a bool to indicate whether the function was able
        // to complete or not (this is used by the menu code to determine whether
        // it should run again or give up).
        // This method will create a new buffer containing the encrypted data and
        // return it in the out arg encryptedData. If the method is unable to
        // perform the operation, it will set encryptedData to be an empty array
        // and return false.
        public static bool SampleEncryptRsa(
            PivPublicKey publicKey,
            byte[] dataToEncrypt,
            RSAEncryptionPadding paddingScheme,
            out byte[] encryptedData)
        {
            encryptedData = Array.Empty<byte>();

            if (publicKey is null)
            {
                throw new ArgumentNullException(nameof(publicKey));
            }

            if (!publicKey.Algorithm.IsRsa())
            {
                return false;
            }

            using var rsaObject = (RSA)KeyConverter.GetDotNetFromPivPublicKey(publicKey);

            encryptedData = rsaObject.Encrypt(dataToEncrypt, paddingScheme);

            return true;
        }

        // Perform EC Key Agree phases 1 and 2. Determine which params to use
        // based on the public key.
        // First, generate a new private and public value.
        // Then use the just-generated private value along with the input public
        // key to build the shared secret.
        // Return the public key, so that we can use the YubiKey to perform
        // phase 2.
        // This sample returns the correspondent's public key as PEM.
        // Of course, in the real world, the correspondent would not send the
        // shared secret, but for this sample, we're returning it as well so that
        // we can compare the two results to make sure they match.
        public static bool SampleKeyAgreeEcc(
            PivPublicKey publicKey,
            HashAlgorithmName hashAlgorithm,
            out char[] correspondentPublicKey,
            out byte[] sharedSecret)
        {
            correspondentPublicKey = Array.Empty<char>();
            sharedSecret = Array.Empty<byte>();

            if (publicKey is null)
            {
                throw new ArgumentNullException(nameof(publicKey));
            }

            if (!publicKey.Algorithm.IsEcc())
            {
                return false;
            }

            // Build an ECDiffieHellman object from the public key.
            // The KeyConverter will build an ECDsa object, but we can build the
            // ECDH object from the EC parameters. So get the ECDsa object, then
            // get the parameters.
            using var ecDsaObject = (ECDsa)KeyConverter.GetDotNetFromPivPublicKey(publicKey);
            ECParameters ecParams = ecDsaObject.ExportParameters(includePrivateParameters: false);

            // This is the .NET version of the public key associated with the
            // private key on the YubiKey. The correspondent will combine this
            // public key with their private key to create the shared secret.
            using var yubiKeyPublic = ECDiffieHellman.Create(ecParams);

            // This will create a new EC Key pair based on the curve.
            // This is the correspondent's public and private key.
            using var correspondentObject = ECDiffieHellman.Create(ecParams.Curve);
            correspondentPublicKey = KeyConverter.GetPemFromDotNet(correspondentObject, isPrivate: false);

            // With the .NET ECDH key agree classes, the value returned is the
            // digest of the shared secret.
            sharedSecret = correspondentObject.DeriveKeyFromHash(yubiKeyPublic.PublicKey, hashAlgorithm);

            return true;
        }
    }
}
