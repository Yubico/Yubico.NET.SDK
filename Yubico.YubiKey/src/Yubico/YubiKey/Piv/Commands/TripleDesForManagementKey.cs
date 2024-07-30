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
using CryptographicOperations = Yubico.Core.Cryptography.CryptographicOperations;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// Perform Triple-DES and DES operations, even if the key is weak.
    /// &gt; [!WARNING]
    /// &gt; This is not a general purpose class and is specifically tailored for
    /// &gt; PIV management key operations. Do not use this class anywhere else.
    /// </summary>
    /// <remarks>
    /// A Triple-DES key is simply three DES keys. There is a concept of a "weak
    /// key". These are keys for which the first two or the last two DES keys are
    /// the same value. Because Triple-DES performs EDE
    /// (Encryption-Decryption-Encryption, encrypt with the first key, decrypt
    /// with the second and encrypt with the third), encryption or decryption
    /// with a weak key is equivalent to single DES. The default TripleDES
    /// implementation in the .NET BCL will throw an exception if it is called
    /// upon to encrypt or decrypt using a weak key.
    /// <para>
    /// This causes a problem with the SDK because the default management key for
    /// PIV (as required by the standard) is weak. Therefore, it seems we cannot
    /// use the default TripleDES implementation to perform authentication.
    /// </para>
    /// <para>
    /// The solution is to simply determine if a key is weak and if so, perform
    /// single DES.
    /// </para>
    /// <para>
    /// The next problem is that the default implementation of DES will throw an
    /// exception if it is called upon to encrypt or decrypt with a DES weak key.
    /// These are not the same as TripleDES weak keys. There is a small set of
    /// keys that reduce the effectiveness of DES. Encrypting a block with a weak
    /// key produces the same result as decrypting that block.
    /// </para>
    /// <para>
    /// Because it is possible someone can change a management key using a tool
    /// other than the SDK, it is possible to set the management key to something
    /// that is a TripleDES weak key, and the key data is a single DES weak key.
    /// We want to be able to authenticate a management key, even if it is weak,
    /// and even if it is equivalent to a single DES weak key.
    /// </para>
    /// <para>
    /// To perform single DES with a weak key, K1, we will perform TripleDES,
    /// because the default implementation of TripleDES does not check for DES
    /// weak keys. In order to obtain the correct result, we will perform
    /// <code>
    ///    TripleDES-with-K1-KA-KB(block)  -->  Result-1
    ///    DES-Decrypt-with-KB(Result-1)   -->  Result-2
    ///    DES-Encrypt-with-KA(Result-2)   -->  ActualResult
    /// </code>
    /// This works because TripleDES is simply
    /// <code>
    ///    DES-Encrypt-with-K1 (block)   -->  Block1
    ///    DES-Decrypt-with-K2 (Block1)  -->  Block2
    ///    DES-Encrypt-with-K3 (Block2)  -->  Result
    /// </code>
    /// This means that the process we'll be performing is
    /// <code>
    ///    DES-Encrypt-with-K1 (block)    -->  Block1
    ///    DES-Decrypt-with-KA (Block1)   -->  Block2
    ///    DES-Encrypt-with-KB (Block2)   -->  Result1
    ///    DES-Decrypt-with-KB (Result1)  -->  Block2
    ///    DES-Encrypt-with-KA (Result2)  -->  Block1
    /// <br/>Block1 is the result we want.
    /// </code>
    /// Note that TripleDES decryption is Decrypt-Encrypt-Decrypt, but in the
    /// order of third key, then second, then first.
    /// </para>
    /// <para>
    /// To use this class, create an instance, then call the
    /// <c>TransformBlock</c> method.
    /// <code language="csharp">
    ///    var tdes = new TripleDesForManagementKey(mgmtKey, true);
    ///    int bytesWritten = tdes.TransformBlock(
    ///      plaintext, offsetP, 8, encryptedData, offsetE);
    /// </code>
    /// The <c>TransformBlock</c> method is used because that is the method (and
    /// argument list) used in the .NET <c>TripleDES</c> classes.
    /// </para>
    /// </remarks>
    [Obsolete("This should only be used for PIV management key operations and nowhere else.")]
    internal sealed class TripleDesForManagementKey : ISymmetricForManagementKey
    {
        // Byte length of the key data
        private const int ValidTripleDesKeyLength = 24;
        private const int ValidDesKeyLength = 8;
        private const int KeyOffsetFirst = 0;
        private const int KeyOffsetSecond = 8;
        private const int KeyOffsetThird = 16;

        private const int TripleDesBlockSize = 8;

        // What the CheckForEqualKeys method can return.
        private const int NoEqualKey = 0;
        private const int EqualKeyOneAndTwo = 12;
        private const int EqualKeyTwoAndThree = 23;

        private static readonly byte[] parityBytes = new byte[] {
            0x01, 0x01, 0x02, 0x02, 0x04, 0x04, 0x07, 0x07, 0x08, 0x08, 0x0b, 0x0b, 0x0d, 0x0d, 0x0e, 0x0e,
            0x10, 0x10, 0x13, 0x13, 0x15, 0x15, 0x16, 0x16, 0x19, 0x19, 0x1a, 0x1a, 0x1c, 0x1c, 0x1f, 0x1f,
            0x20, 0x20, 0x23, 0x23, 0x25, 0x25, 0x26, 0x26, 0x29, 0x29, 0x2a, 0x2a, 0x2c, 0x2c, 0x2f, 0x2f,
            0x31, 0x31, 0x32, 0x32, 0x34, 0x34, 0x37, 0x37, 0x38, 0x38, 0x3b, 0x3b, 0x3d, 0x3d, 0x3e, 0x3e,
            0x40, 0x40, 0x43, 0x43, 0x45, 0x45, 0x46, 0x46, 0x49, 0x49, 0x4a, 0x4a, 0x4c, 0x4c, 0x4f, 0x4f,
            0x51, 0x51, 0x52, 0x52, 0x54, 0x54, 0x57, 0x57, 0x58, 0x58, 0x5b, 0x5b, 0x5d, 0x5d, 0x5e, 0x5e,
            0x61, 0x61, 0x62, 0x62, 0x64, 0x64, 0x67, 0x67, 0x68, 0x68, 0x6b, 0x6b, 0x6d, 0x6d, 0x6e, 0x6e,
            0x70, 0x70, 0x73, 0x73, 0x75, 0x75, 0x76, 0x76, 0x79, 0x79, 0x7a, 0x7a, 0x7c, 0x7c, 0x7f, 0x7f,
            0x80, 0x80, 0x83, 0x83, 0x85, 0x85, 0x86, 0x86, 0x89, 0x89, 0x8a, 0x8a, 0x8c, 0x8c, 0x8f, 0x8f,
            0x91, 0x91, 0x92, 0x92, 0x94, 0x94, 0x97, 0x97, 0x98, 0x98, 0x9b, 0x9b, 0x9d, 0x9d, 0x9e, 0x9e,
            0xa1, 0xa1, 0xa2, 0xa2, 0xa4, 0xa4, 0xa7, 0xa7, 0xa8, 0xa8, 0xab, 0xab, 0xad, 0xad, 0xae, 0xae,
            0xb0, 0xb0, 0xb3, 0xb3, 0xb5, 0xb5, 0xb6, 0xb6, 0xb9, 0xb9, 0xba, 0xba, 0xbc, 0xbc, 0xbf, 0xbf,
            0xc1, 0xc1, 0xc2, 0xc2, 0xc4, 0xc4, 0xc7, 0xc7, 0xc8, 0xc8, 0xcb, 0xcb, 0xcd, 0xcd, 0xce, 0xce,
            0xd0, 0xd0, 0xd3, 0xd3, 0xd5, 0xd5, 0xd6, 0xd6, 0xd9, 0xd9, 0xda, 0xda, 0xdc, 0xdc, 0xdf, 0xdf,
            0xe0, 0xe0, 0xe3, 0xe3, 0xe5, 0xe5, 0xe6, 0xe6, 0xe9, 0xe9, 0xea, 0xea, 0xec, 0xec, 0xef, 0xef,
            0xf1, 0xf1, 0xf2, 0xf2, 0xf4, 0xf4, 0xf7, 0xf7, 0xf8, 0xf8, 0xfb, 0xfb, 0xfd, 0xfd, 0xfe, 0xfe
        };

        private readonly ICryptoTransform _cryptoTransform;
        private ICryptoTransform? _cryptoTransformA;
        private ICryptoTransform? _cryptoTransformB;
        private bool _disposed;

        /// <inheritdoc/>
        public bool IsEncrypting { get; }

        /// <inheritdoc/>
        public int BlockSize { get; }

        /// <summary>
        /// Create a new instance of <c>TripleDesForManagementKey</c> using the
        /// given management key.
        /// </summary>
        /// <remarks>
        /// Each instance of this class will be able to encrypt or decrypt, but
        /// not both. Specify whether it should encrypt or decrypt with the
        /// <c>isEncrypting</c> argument.
        /// </remarks>
        /// <param name="managementKey">
        /// The bytes of the management key. This key must be exactly 24 bytes
        /// long.
        /// </param>
        /// <param name="isEncrypting">
        /// Indicates whether the object should be built to encrypt (if it is
        /// <c>true</c>), or to decrypt (if it is <c>false</c>).
        /// </param>
        /// <exception cref="ArgumentException">
        /// The <c>managementKey</c> argument is not a valid Triple-DES key.
        /// </exception>
        public TripleDesForManagementKey(ReadOnlySpan<byte> managementKey, bool isEncrypting)
        {
            IsEncrypting = isEncrypting;
            BlockSize = TripleDesBlockSize;
            _disposed = false;

            if (managementKey.Length != ValidTripleDesKeyLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncorrectTripleDesKeyLength));
            }

            // Set parity so that we can compare keys.
            byte[] keyData = SetParity(managementKey);

            try
            {
                _cryptoTransform = CheckForEqualKeys(keyData) switch
                {
                    EqualKeyOneAndTwo => BuildDes(keyData, KeyOffsetThird),
                    EqualKeyTwoAndThree => BuildDes(keyData, KeyOffsetFirst),
                    _ => BuildTripleDes(keyData),
                };
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keyData);
            }
        }

        /// <inheritdoc/>
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            if (inputBuffer is null)
            {
                throw new ArgumentNullException(nameof(inputBuffer));
            }
            if (outputBuffer is null)
            {
                throw new ArgumentNullException(nameof(outputBuffer));
            }
            if (inputCount == 0 || (inputCount & 7) != 0)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncorrectPlaintextLength));
            }
            if (inputOffset < 0 || inputBuffer.Length - inputOffset < inputCount ||
                outputOffset < 0 || outputBuffer.Length - outputOffset < inputCount)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidOutputBuffer));
            }
            EnsureNotDisposed();

            _ = _cryptoTransform.TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);

            if (!(_cryptoTransformA is null) && !(_cryptoTransformB is null))
            {
                _ = _cryptoTransformB.TransformBlock(outputBuffer, outputOffset, inputCount, outputBuffer, outputOffset);
                _ = _cryptoTransformA.TransformBlock(outputBuffer, outputOffset, inputCount, outputBuffer, outputOffset);
            }

            return inputCount;
        }
#pragma warning disable CA5401 // Justification: Allow the symmetric encryption to use
        // a non-default initialization vector

        // Build this object to use TripleDES with the given key.
        private ICryptoTransform BuildTripleDes(byte[] keyData)
        {
            using TripleDES tripleDesObject = CryptographyProviders.TripleDesCreator();
#pragma warning disable CA5358 // Allow the usage of cipher mode 'ECB'
            tripleDesObject.Mode = CipherMode.ECB;
#pragma warning restore CA5358
            tripleDesObject.Padding = PaddingMode.None;

            if (IsEncrypting)
            {
                return tripleDesObject.CreateEncryptor(keyData, null);
            }

            return tripleDesObject.CreateDecryptor(keyData, null);
        }

        // Build this object to use DES with the given key.
        private ICryptoTransform BuildDes(byte[] threeKeyData, int offset)
        {
            byte[] keyData = new byte[ValidDesKeyLength];
            Array.Copy(threeKeyData, offset, keyData, 0, ValidDesKeyLength);

            try
            {
#pragma warning disable CA5351 // Justification: In this case, allow to build
                // the object using DES with the given key
                if (DES.IsWeakKey(keyData))
                {
                    return BuildDesWithWeakKey(keyData);
                }
#pragma warning restore CA5351
                using DES desObject = CryptographyProviders.DesCreator();
#pragma warning disable CA5358 // Allow the usage of cipher mode 'ECB'
                desObject.Mode = CipherMode.ECB;
#pragma warning restore CA5358
                desObject.Padding = PaddingMode.None;

                if (IsEncrypting)
                {
                    return desObject.CreateEncryptor(keyData, null);
                }

                return desObject.CreateDecryptor(keyData, null);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keyData);
            }
        }

        // Build this object to use TripleDES with the given key as key 1, and
        // two arbitrary keys as key 2 and key 3.
        // Then build two DES objects using the keys A and B.
        // Let's say we're encrypting with 3DES. We will call on the 3DES API to
        // encrypt, which is really E with the input key data, D with the
        // arbitrary key A, and E with arbitrary key B.
        // We will then perform DES decryption on the result using key B and then
        // DES encryption on the result of that using key A.
        // So what we are doing is this.
        //   E-K  D-KA  E-KB  D-KB  E-KA
        // This will give us the result of E-K.
        // Decryption is the opposite.
        // If the input key is a weak key, the encryption and decryption don't
        // matter, the result of one is the result of the other (that's what
        // makes it a weak key). So we could write the code to just do
        // encryption. However, we're writing both encryption and decryption, so
        // that if in the future we need to use this code for semi weak keys, we
        // can.
        private ICryptoTransform BuildDesWithWeakKey(byte[] keyData)
        {
            byte[] threeKeyData = new byte[] {
                0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
                0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28
            };
            byte[] keyDataA = new byte[] {
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18
            };
            byte[] keyDataB = new byte[] {
                0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28
            };

            try
            {
#pragma warning disable CA5358 // Allow the usage of cipher mode 'ECB'
                using TripleDES tripleDesObject = CryptographyProviders.TripleDesCreator();
                tripleDesObject.Mode = CipherMode.ECB;
                tripleDesObject.Padding = PaddingMode.None;

                using DES desObject = CryptographyProviders.DesCreator();
                desObject.Mode = CipherMode.ECB;
                desObject.Padding = PaddingMode.None;
#pragma warning restore CA5358

#pragma warning disable CA5390 // Justification: In this case, allow to use the hard-code keys
                if (IsEncrypting)
                {
                    Array.Copy(keyData, 0, threeKeyData, 0, ValidDesKeyLength);

                    _cryptoTransformA = desObject.CreateEncryptor(keyDataA, null);
                    _cryptoTransformB = desObject.CreateDecryptor(keyDataB, null);
                    return tripleDesObject.CreateEncryptor(threeKeyData, null);
                }

                // Because decryption operates "in reverse", the third key will
                // be used in the first use of DES. So copy the target key to the
                // third position.
                Array.Copy(keyData, 0, threeKeyData, KeyOffsetThird, ValidDesKeyLength);

                _cryptoTransformA = desObject.CreateDecryptor(keyDataA, null);
                _cryptoTransformB = desObject.CreateEncryptor(keyDataB, null);
                return tripleDesObject.CreateDecryptor(threeKeyData, null);
#pragma warning restore CA5390
            }
            finally
            {
                CryptographicOperations.ZeroMemory(threeKeyData);
            }
        }
#pragma warning restore CA5401

        /// <summary>
        /// Create a new byte array containing the key data, but with the DES
        /// parity bits set correctly.
        /// </summary>
        /// <param name="keyDataSpan">
        /// A <c>ReadOnlySpan</c> containing the key data to set.
        /// </param>
        /// <returns>
        /// A new byte array containing the key data with the DES parity bits
        /// correctly set.
        /// </returns>
        public static byte[] SetParity(ReadOnlySpan<byte> keyDataSpan)
        {
            byte[] returnValue = new byte[keyDataSpan.Length];

            for (int index = 0; index < keyDataSpan.Length; index++)
            {
                returnValue[index] = parityBytes[keyDataSpan[index]];
            }

            return returnValue;
        }

        // Check to see if either the first key and second key are the same, or
        // the second key and third key are the same.
        // This is checking for a tripleDES weak key. But instead of returning an
        // answer yes or no (it is or is not a weak key), this returns a value to
        // indicate whether the weakness is because the first and second are the
        // same, or the second and third are the same.
        // If the first and second are equal, don't bother checking the second
        // and third.
        // If this is not a weak key, return NoEqualKey.
        // If this is a weak key because the first and second keys are the same,
        // return EqualKeyOneAndTwo.
        // If this is a weak key because the second and third keys are the same,
        // return EqualKeyTwoAndThree.
        private static int CheckForEqualKeys(ReadOnlySpan<byte> keyData)
        {
            if (MemoryExtensions.SequenceEqual(
                keyData.Slice(0, ValidDesKeyLength),
                keyData.Slice(KeyOffsetSecond, ValidDesKeyLength)))
            {
                return EqualKeyOneAndTwo;
            }

            if (MemoryExtensions.SequenceEqual(
                keyData.Slice(KeyOffsetSecond, ValidDesKeyLength),
                keyData.Slice(KeyOffsetThird, ValidDesKeyLength)))
            {
                return EqualKeyTwoAndThree;
            }

            return NoEqualKey;
        }

        /// <summary>
        /// When the object goes out of scope, this method is called. It will
        /// dispose local objects.
        /// </summary>
        // Note that .NET recommends a Dispose method call Dispose(true) and
        // GC.SuppressFinalize(this). The actual disposal is in the
        // Dispose(bool) method.
        //
        // However, that does not apply to sealed classes.
        // So the Dispose method will simply perform the
        // "closing" process, no call to Dispose(bool) or GC.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _cryptoTransform.Dispose();
            if (!(_cryptoTransformA is null))
            {
                _cryptoTransformA.Dispose();
            }
            if (!(_cryptoTransformB is null))
            {
                _cryptoTransformB.Dispose();
            }

            _disposed = true;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TripleDesForManagementKey));
            }
        }
    }
}
