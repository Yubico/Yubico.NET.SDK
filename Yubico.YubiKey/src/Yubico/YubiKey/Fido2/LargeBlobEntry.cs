// Copyright 2022 Yubico AB
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
using System.IO;
using System.IO.Compression;
using System.Buffers.Binary;
using System.Formats.Cbor;
using System.Security.Cryptography;
using Yubico.Core.Cryptography;
using Yubico.Core.Logging;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.Cbor;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Contains the data from one entry in the Large Blob Array. See also the
    /// <xref href="Fido2LargeBlobs">user's manual entry</xref> on large blobs.
    /// </summary>
    /// <remarks>
    /// The <see cref="SerializedLargeBlobArray"/> class contains a <c>List</c> of
    /// <c>LargeBlobEntry</c>, this class. When you get a Large Blob Array from a
    /// YubiKey (<see cref="Fido2Session.GetSerializedLargeBlobArray"/>), you get a
    /// <c>LargeBlobArray</c> object. You then have access to each of the
    /// individual entries in the Large Blob Array through that list of
    /// <c>LargeBlobEntry</c>. If you want to add a new <c>LargeBlobEntry</c> to
    /// the Array's <c>List</c>, call the
    /// <see cref="SerializedLargeBlobArray.AddEntry"/> method.
    /// <para>
    /// This class contains only properties and a <see cref="TryDecrypt"/>
    /// method. You will not build an individual entry yourself, only the
    /// <c>LargeBlobArray</c> class can do that. But you will be able to see the
    /// data of the entry.
    /// </para>
    /// </remarks>
    public class LargeBlobEntry
    {
        private const int KeyCiphertext = 1;
        private const int KeyNonce = 2;
        private const int KeyOrigSize = 3;
        private const int NonceSize = 12;
        private const int GcmTagSize = 16;
        // To write out associated info, which is
        //   62 6C 6F 62 || littleEndian64(originalSize)
        private const int AssociatedDataSize = 12;
        private const int AssociatedBlob = 0x626C6F62;
        private const int AssociatedSizeOffset = 4;

        private readonly Logger _log = Log.GetLogger();

        /// <summary>
        /// The encrypted data. This is either the retrieved encrypted data when
        /// getting a Large Blob Array, or the provided data encrypted using the
        /// specified <c>LargeBlobKey</c> when creating a new entry to store. The
        /// last 16 bytes make up the GCM authentication tag.
        /// </summary>
        /// <remarks>
        /// The plaintext data is compressed before encrypting.
        /// </remarks>
        public ReadOnlyMemory<byte> Ciphertext { get; private set; }

        /// <summary>
        /// The nonce used to perform the AES-GCD operation. This is either the
        /// retrieved nonce when getting a Large Blob Array, or the generated
        /// nonce used when creating a new entry to store.
        /// </summary>
        public ReadOnlyMemory<byte> Nonce { get; private set; }

        /// <summary>
        /// The length, in bytes, of the unencrypted, uncompressed data. This is
        /// either the retrieved <c>origSize</c> in the Large Blob Map when
        /// getting a Large Blob Array, or the length, in bytes, of the provided
        /// data when creating a new entry to store.
        /// </summary>
        public int OriginalDataLength { get; private set; }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private LargeBlobEntry()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a new instance of <see cref="LargeBlobEntry"/> that will be
        /// added to the <see cref="SerializedLargeBlobArray"/>.
        /// </summary>
        /// <remarks>
        /// Use this constructor when you want to create a new entry and add it
        /// to the array.
        /// <para>
        /// Generally you will get an assertion, requesting the
        /// <c>LargeBlobKey</c> extension, and provide the data you want to store
        /// along with the <see cref="GetAssertionData.LargeBlobKey"/> in the
        /// returned <see cref="GetAssertionData"/> object.
        /// </para>
        /// </remarks>
        /// <param name="blobData">
        /// The data to store in the Large Blob Array.
        /// </param>
        /// <param name="largeBlobKey">
        /// The 32-byte key returned by the YubiKey in an assertion, it will be
        /// used to encrypt the <c>blobData</c>.
        /// </param>
        internal LargeBlobEntry(ReadOnlyMemory<byte> blobData, ReadOnlyMemory<byte> largeBlobKey)
        {
            _log.LogInformation("Creating a new LargeBlobEntry from a key and data.");

            OriginalDataLength = blobData.Length;

            using RandomNumberGenerator randomObject = CryptographyProviders.RngCreator();
            byte[] nonce = new byte[NonceSize];
            randomObject.GetBytes(nonce, 0, NonceSize);
            Nonce = new ReadOnlyMemory<byte>(nonce);

            Ciphertext = EncryptBlobData(blobData, largeBlobKey);
        }

        /// <summary>
        /// Build a new instance of <see cref="LargeBlobEntry"/> based on the
        /// given CBOR encoding.
        /// </summary>
        /// <remarks>
        /// This constructor is used by the <see cref="SerializedLargeBlobArray"/>
        /// class when it is decoding an existing Serialized Large Blob Array. After
        /// decoding, use the <see cref="TryDecrypt"/> method to obtain the
        /// actual data.
        /// <para>
        /// The encoding must follow the definition of the <c>large blob map</c>
        /// in section 6.10.3 of the CTAP 2.1 standard:
        /// <code>
        ///   A3                      -- map of 3 key/value pairs
        ///     01  --byte string--    -- key = 1, value is a byte string
        ///     02  --byte string--    -- key = 2, value is a byte string
        ///     03  --unsigned int--   -- key = 3, value is an unsigned int
        ///  where the byte string for key 01 is the AEAD-AES-GCM ciphertext
        ///    containing the encrypted data and an authentication tag
        ///  the byte string for key 02 is the AES-GCM nonce, 12 bytes
        ///  and the unsigned int is the length, in bytes, of the original,
        ///    uncompressed data
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="cborEncoding">
        /// The map that is a large blob array entry, encoded following the CTAP
        /// 2.1 and CBOR (RFC 8949) standards.
        /// </param>
        /// <exception cref="Ctap2DataException">
        /// The <c>cborEncoding</c> is not a valid CBOR encoding, or it is not a
        /// correct encoding for FIDO2 large blob map.
        /// </exception>
        internal LargeBlobEntry(ReadOnlyMemory<byte> cborEncoding)
        {
            _log.LogInformation("Creating a new LargeBlobEntry from a Cbor encoding.");

            string cborMessage = "";

            try
            {
                var mapReader = new CborMap<int>(cborEncoding);
                Ciphertext = mapReader.ReadByteString(KeyCiphertext);
                Nonce = mapReader.ReadByteString(KeyNonce);
                OriginalDataLength = mapReader.ReadInt32(KeyOrigSize);

                // Make sure the data includes the tag plus at least one byte,
                // and that there is original data.
                if ((OriginalDataLength > 0) && (Ciphertext.Length > GcmTagSize))
                {
                    return;
                }
            }
            catch (CborContentException cborException)
            {
                cborMessage = " " + cborException.Message;
            }

            throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info + cborMessage);
        }

        /// <summary>
        /// Try to decrypt the data using the given key. If the key is correct,
        /// this will set the return <c>true</c> and return the plaintext in the
        /// out argument (decrypted and decompressed).
        /// </summary>
        /// <remarks>
        /// Because the data is encrypted using AES-GCD, the ciphertext contains
        /// both the encrypted data and an "authentication tag". While any key
        /// will be able to decrypt the data and produce a result (some result),
        /// only the correct key will be able to authenticate the tag. Hence,
        /// this method will be able to determine whether the key provided was
        /// the correct key and the decrypted data is the correct data.
        /// <para>
        /// If the method is able to decrypt using the key, it will then
        /// decompress the decrypted data.
        /// </para>
        /// <para>
        /// When reading a Large Blob Array, you will likely obtain the large
        /// blob data from the YubiKey, resulting in a
        /// <see cref="SerializedLargeBlobArray"/> object. At that point, each of
        /// the entries contain only the encrypted data. You will then obtain the
        /// <c>LargeBlobKey</c> from the target credential, and use it to try to
        /// decrypt the data of each entry in the Large Blob Array.
        /// </para>
        /// <para>
        /// Note that the plaintext returned is a <c>Memory</c> object, not a
        /// <c>ReadOnlyMemory</c> object. This is so you can overwrite it for
        /// security reasons if you want.
        /// </para>
        /// </remarks>
        /// <param name="largeBlobKey">
        /// The key to use to decrypt.
        /// </param>
        /// <param name="plaintext">
        /// An output argument. A new object containing the plaintext if the
        /// decryption succeeds, or an empty <c>Memory</c> object otherwise.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the data is successfully decrypted using
        /// the given key, and <c>false</c> otherwise.
        /// </returns>
        public bool TryDecrypt(ReadOnlyMemory<byte> largeBlobKey, out Memory<byte> plaintext)
        {
            _log.LogInformation("Try to decrypt a LargeBlobEntry.");

            plaintext = Memory<byte>.Empty;
            int dataToDecryptLength = Ciphertext.Length - GcmTagSize;
            byte[] decryptedData = new byte[dataToDecryptLength];

            try
            {
                var associatedData = new Span<byte>(new byte[AssociatedDataSize]);
                BinaryPrimitives.WriteInt32BigEndian(associatedData, AssociatedBlob);
                BinaryPrimitives.WriteInt64LittleEndian(associatedData.Slice(AssociatedSizeOffset), (long)OriginalDataLength);

                IAesGcmPrimitives decryptor = CryptographyProviders.AesGcmPrimitivesCreator();
                bool returnValue = decryptor.DecryptAndVerify(
                    largeBlobKey.Span, Nonce.Span,
                    Ciphertext.Slice(0, dataToDecryptLength).Span,
                    Ciphertext.Slice(dataToDecryptLength, GcmTagSize).Span, decryptedData, associatedData);

                if (returnValue)
                {
                    using var compressedStream = new MemoryStream(decryptedData);
                    using var decompressedStream = new MemoryStream();
                    using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
                    deflateStream.CopyTo(decompressedStream);
                    deflateStream.Flush();

                    returnValue = false;
                    if (decompressedStream.Length == OriginalDataLength)
                    {
                        byte[] dataToReturn = decompressedStream.ToArray();
                        plaintext = new Memory<byte>(dataToReturn);
                        returnValue = true;
                    }
                }

                return returnValue;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(decryptedData);
            }
        }

        // Compress the input data, then encrypt the compressed data using
        // AES-GCD and the largeBlobKey.
        // Return the ciphertext as encrypted data || tag.
        private ReadOnlyMemory<byte> EncryptBlobData(ReadOnlyMemory<byte> blobData, ReadOnlyMemory<byte> largeBlobKey)
        {
            _log.LogInformation("Encrypt a LargeBlobEntry.");

            byte[] plaintext = blobData.ToArray();
            byte[] dataToEncrypt = Array.Empty<byte>();

            try
            {
                using var uncompressedStream = new MemoryStream(plaintext);
                using var compressedStream = new MemoryStream();
                using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Compress);
                uncompressedStream.CopyTo(deflateStream);
                deflateStream.Flush();
                dataToEncrypt = compressedStream.ToArray();

                byte[] ciphertext = new byte[dataToEncrypt.Length + GcmTagSize];
                byte[] encryptedData = new byte[dataToEncrypt.Length];
                byte[] gcmTag = new byte[GcmTagSize];

                var associatedData = new Span<byte>(new byte[AssociatedDataSize]);
                BinaryPrimitives.WriteInt32BigEndian(associatedData, AssociatedBlob);
                BinaryPrimitives.WriteInt64LittleEndian(
                    associatedData.Slice(AssociatedSizeOffset), (long)blobData.Length);

                IAesGcmPrimitives encryptor = CryptographyProviders.AesGcmPrimitivesCreator();
                encryptor.EncryptAndAuthenticate(
                    largeBlobKey.Span, Nonce.Span, dataToEncrypt, encryptedData, gcmTag, associatedData);
                Array.Copy(encryptedData, 0, ciphertext, 0, encryptedData.Length);
                Array.Copy(gcmTag, 0, ciphertext, encryptedData.Length, gcmTag.Length);

                return new ReadOnlyMemory<byte>(ciphertext);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
                CryptographicOperations.ZeroMemory(dataToEncrypt);
            }
        }

        /// <inheritdoc/>
        internal byte[] CborEncode()
        {
            // An encoded LargeBlobEntry is
            //   map
            //     01  byte string: ciphertext
            //     02  byte string: nonce
            //     03  unsigned int : originalplaintext length
            return new CborMapWriter<int>()
                .Entry(KeyCiphertext, Ciphertext)
                .Entry(KeyNonce, Nonce)
                .Entry(KeyOrigSize, OriginalDataLength)
                .Encode();
        }
    }
}
