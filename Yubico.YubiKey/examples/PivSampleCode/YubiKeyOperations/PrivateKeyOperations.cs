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
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    public static class PrivateKeyOperations
    {
        // Sign data. This creates a digital signature.
        // If the keyAlgorithm is ECC the paddingScheme arg is ignored.
        public static bool RunSignData(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            byte slotNumber,
            byte[] dataToSign,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding paddingScheme,
            PivAlgorithm keyAlgorithm,
            out byte[] signature)
        {
            if (paddingScheme is null)
            {
                throw new ArgumentNullException(nameof(paddingScheme));
            }

            signature = Array.Empty<byte>();
            int keySizeBits = keyAlgorithm.KeySizeBits();

            // Before signing the data, we need to digest it.
            byte[] digest = MessageDigestOperations.ComputeMessageDigest(dataToSign, hashAlgorithm);

            if (keyAlgorithm.IsEcc())
            {
                // If the key is ECC, the digested data must be exactly the key
                // size. For example, if the key is EccP384, then the digest
                // buffer must be exactly 48 bytes. If you use SHA-256, the
                // digest is 32 bytes. In that case, create a 48-byte buffer, set
                // the first 16 bytes to be 00, then copy in the 32 SHA-256
                // digest bytes: 00 00 ... 00 || SHA-256 digest.
                // Of course, it is easier to simply use SHA-384 when using
                // EccP384.
                int bufferSize = keySizeBits / 8;
                if (digest.Length < bufferSize)
                {
                    byte[] newBuffer = new byte[bufferSize];
                    Array.Copy(digest, 0, newBuffer, bufferSize - digest.Length, digest.Length);
                    digest = newBuffer;
                }
            }
            else
            {
                // If the key is RSA, we need to pad.
                // The RsaFormat class supports only SHA-1, SHA-256, SHA-384, and
                // SHA-512. If the caller tries to use an unsupported algorithm,
                // set the digestAlgorithm variable to -1.
                int digestAlgorithm = hashAlgorithm.Name switch
                {
                    "SHA1" => RsaFormat.Sha1,
                    "SHA256" => RsaFormat.Sha256,
                    "SHA384" => RsaFormat.Sha384,
                    "SHA512" => RsaFormat.Sha512,
                    _ => -1,
                };

                // If digestAlgorithm is < 0, then the caller wanted to use an
                // unsupported digest algorithm. If so, return false, indicating
                // we could not perform the operation.
                if (digestAlgorithm < 0)
                {
                    return false;
                }

                if (paddingScheme.Mode == RSASignaturePaddingMode.Pss)
                {
                    digest = RsaFormat.FormatPkcs1Pss(digest, digestAlgorithm, keySizeBits);
                }
                else
                {
                    digest = RsaFormat.FormatPkcs1Sign(digest, digestAlgorithm, keySizeBits);
                }
            }
            
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = KeyCollectorDelegate;
                signature = pivSession.Sign(slotNumber, digest);
            }

            return true;
        }

        public static bool RunDecryptData(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            byte slotNumber,
            byte[] encryptedData,
            RSAEncryptionPadding paddingScheme,
            out byte[] decryptedData)
        {
            if (paddingScheme is null)
            {
                throw new ArgumentNullException(nameof(paddingScheme));
            }

            decryptedData = Array.Empty<byte>();
            bool isValid;

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = KeyCollectorDelegate;
                byte[] rawDecryptedData = pivSession.Decrypt(slotNumber, encryptedData);

                // Now that it is decrypted, unpad.
                // There are two unpad algorithms supported in the
                // Yubico.YubiKey.Cryptogrpahy.RsaFormat class: PKCS 1 v1.5 and
                // OAEP.
                // If the padding scheme is OAEP there will be a digest algorithm
                // specified. If the padding scheme is P1.5, there will not.
                // So we can determine which RsaFormat method to call and if OAE
                // which digest algorithm by looking at the padding scheme's
                // HashAlgorithm name.
                // If the padding scheme is not OAEP, then the HashAlgorithm name
                // will be null. In that case, set the digestAlgorithm to -1.
                int digestAlgorithm = paddingScheme.OaepHashAlgorithm.Name switch
                {
                    "SHA1" => RsaFormat.Sha1,
                    "SHA256" => RsaFormat.Sha256,
                    "SHA384" => RsaFormat.Sha384,
                    "SHA512" => RsaFormat.Sha512,
                    _ => -1,
                };

                // If the digest algorithm is -1, we're not using a supported
                // OAEP. Hence, use P1.5.
                if (digestAlgorithm < 0)
                {
                    isValid = RsaFormat.TryParsePkcs1Decrypt(rawDecryptedData, out decryptedData);
                }
                else
                {
                    isValid = RsaFormat.TryParsePkcs1Oaep(rawDecryptedData, digestAlgorithm, out decryptedData);
                }
            }

            return isValid;
        }

        public static bool RunKeyAgree(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            byte slotNumber,
            PivPublicKey correspondentPublicKey,
            out byte[] computedSecret)
        {
                using (var pivSession = new PivSession(yubiKey))
                {
                    pivSession.KeyCollector = KeyCollectorDelegate;
                    computedSecret = pivSession.KeyAgree(slotNumber, correspondentPublicKey);
                }

            return true;
        }
    }
}
