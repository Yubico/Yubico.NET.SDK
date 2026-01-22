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
    // This class is a sample demonstrating how to get the the .NET Base Class
    // Library X509Certificate classes to use the YubiKey to sign.
    // In order to use an alternate signer, create a subclass of
    // X509SignatureGenerator, and implement the methods
    //   BuildPublicKey
    //   GetSignatureAlgorithmIdentifier
    //   SignData
    public sealed partial class YubiKeySignatureGenerator : X509SignatureGenerator
    {
        private const string InvalidAlgorithmMessage = "The algorithm was not recognized.";
        private const string InvalidSlotMessage = "The slot number was invalid.";

        private readonly PivSession _pivSession;
        private readonly byte _slotNumber;
        private readonly KeyType _algorithm;

        private readonly RSASignaturePaddingMode _rsaPaddingMode;
        private readonly X509SignatureGenerator _defaultGenerator;

        // The constructor copies a reference to the PivSession.
        // If the key is RSA, specify the padding scheme. If no padding scheme is
        // given, the default will be used. The default is
        // RSASignaturePaddingMode.Pss. THe only other possible padding scheme is
        // RSASignaturePaddingMode.Pkcs1.
        // If the key is ECC, there's no padding scheme, so no need to provide
        // that argument.
        public YubiKeySignatureGenerator(
            PivSession pivSession,
            byte slotNumber,
            IPublicKey publicKey,
            RSASignaturePaddingMode rsaPaddingMode = RSASignaturePaddingMode.Pss)
        {
            
            ArgumentNullException.ThrowIfNull(pivSession);
            ArgumentNullException.ThrowIfNull(publicKey);
            
            if (!PivSlot.IsValidSlotNumberForSigning(slotNumber))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        InvalidSlotMessage));
            }

            _pivSession = pivSession;
            _slotNumber = slotNumber;
            _algorithm = publicKey.KeyType;
            _rsaPaddingMode = rsaPaddingMode;

            using var dotNetPublicKey = KeyConverter.GetDotNetFromPublicKey(publicKey);

            if (_algorithm.IsRSA())
            {
                var paddingScheme = rsaPaddingMode == RSASignaturePaddingMode.Pss ?
                    RSASignaturePadding.Pss : RSASignaturePadding.Pkcs1;
                _defaultGenerator = X509SignatureGenerator.CreateForRSA((RSA)dotNetPublicKey, paddingScheme);
            }
            else if (_algorithm.IsEllipticCurve())
            {
                _defaultGenerator = X509SignatureGenerator.CreateForECDsa((ECDsa)dotNetPublicKey);
            }
            else
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        InvalidAlgorithmMessage));
            }
        }

        // Return the public key as an instance of PublicKey.
        protected override System.Security.Cryptography.X509Certificates.PublicKey BuildPublicKey()
        {
            return _defaultGenerator.PublicKey;
        }

        // Get the AlgorithmIdentifier of the signature algorithm. This is the
        // algorithm that will be used to sign the cert request, cert, etc.
        public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
        {
            return _defaultGenerator.GetSignatureAlgorithmIdentifier(hashAlgorithm);
        }

        // Sign the data.
        // The data is the cert request's "ToBeSigned". So we need to use the
        // hashAlgorithm specified to digest it first. If this is RSA signing,
        // pad the data next. If ECC, there's no padding. Finally, perform the
        // YubiKey signing operation.
        public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            byte[] dataToSign = DigestData(data, hashAlgorithm);

            if (_algorithm.IsRSA())
            {
                dataToSign = PadRsa(dataToSign, hashAlgorithm);
            }

            return _pivSession.Sign(_slotNumber, dataToSign);
        }

        // Compute the message digest of the data using the given hashAlgorithm.
        // For RSA keys, returns the raw digest (PadRsa handles signature padding).
        // For ECC keys, pads the digest to key size with leading zeros if needed.
        public byte[] DigestData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            byte[] digest = MessageDigestOperations.ComputeMessageDigest(data, hashAlgorithm);

            // For RSA, return the raw digest - PadRsa handles the signature padding
            if (_algorithm.IsRSA())
            {
                return digest;
            }

            // For ECC, the digest must match the key size (e.g., 32 bytes for P-256)
            // Pad with leading zeros if necessary
            int keySizeBytes = _algorithm.GetKeySizeBytes();

            if (digest.Length == keySizeBytes)
            {
                return digest;
            }

            if (digest.Length > keySizeBytes)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        InvalidAlgorithmMessage));
            }

            // Pad with leading zeros
            byte[] paddedDigest = new byte[keySizeBytes];
            int offset = keySizeBytes - digest.Length;
            Array.Copy(digest, 0, paddedDigest, offset, digest.Length);

            return paddedDigest;
        }

        // Create a block of data that is the data to sign padded following the
        // RsaPadding specified. The size of the block will be based on the
        // _publicKey. Both supported padding schemes, PKCS 1 v1.5 and PSS rely
        // on the hashAlgorithm.
        private byte[] PadRsa(byte[] digest, HashAlgorithmName hashAlgorithm)
        {
            int digestAlgorithm = hashAlgorithm.Name switch
            {
                "SHA1" => RsaFormat.Sha1,
                "SHA256" => RsaFormat.Sha256,
                "SHA384" => RsaFormat.Sha384,
                "SHA512" => RsaFormat.Sha512,
                _ => 0,
            };

            if (_rsaPaddingMode == RSASignaturePaddingMode.Pss)
            {
                return RsaFormat.FormatPkcs1Pss(digest, digestAlgorithm, _algorithm.GetKeySizeBits());
            }

            return RsaFormat.FormatPkcs1Sign(digest, digestAlgorithm, _algorithm.GetKeySizeBits());
        }
    }
}
