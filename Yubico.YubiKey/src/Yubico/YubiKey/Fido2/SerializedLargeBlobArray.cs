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
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Globalization;
using System.Security.Cryptography;
using Yubico.Core.Logging;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.Cbor;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Contains the Serialized Large Blob Array data. See also the
    /// <xref href="Fido2LargeBlobs">user's manual entry</xref> on large blobs.
    /// </summary>
    /// <remarks>
    /// The Large Blob data is stored on the YubiKey as a "Serialized Large Blob
    /// Array". This is the "Large Blob Array" followed by a message digest
    /// value:
    /// <code language="adoc">
    ///   Large Blob Array || digest value
    /// </code>
    /// The digest value is the first 16 bytes of the SHA-256 digest of the Large
    /// Blob Array.
    /// <para>
    /// The Large Blob Array is a CBOR array (major type 4). For example,
    /// an array of 3 elements is encoded as
    /// <code language="adoc">
    ///   0x83  element0  element1  element2
    /// </code>
    /// </para>
    /// <para>
    /// A YubiKey begins with no Large Blob data. It is possible to retrieve the
    /// Serialized Large Blob Array and the result will be a zero-count array
    /// with digest value:
    /// <code language="adoc">
    ///  80 76be8b528d0075f7aae98d6fa57a6d3c
    /// </code>
    /// The <c>80</c> is the Large Blob Array (an array with zero elements),
    /// followed by the first 16 bytes of the SHA-256 digest of the single byte
    /// <c>0x80</c>.
    /// </para>
    /// <para>
    /// Each element in the Large Blob Array is a CBOR map consisting of three
    /// key/value pairs:
    /// <code language="adoc">
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
    /// The key used to encrypt is the <c>LargeBlobKey</c> There is a different
    /// <c>LargeBlobKey</c> for each credential. Hence, each element in the Large
    /// Blob Array is data associated with one credential.
    /// </para>
    /// <para>
    /// This class is the input to the
    /// <see cref="Fido2Session.SetSerializedLargeBlobArray"/>. To set a Large
    /// Blob Array, get the current array
    /// (<see cref="Fido2Session.GetSerializedLargeBlobArray"/>) and remove,
    /// replace, or add entries. Even if there are no entries in the YubiKey
    /// (e.g. it is a new YubiKey with the initial serialized large blob array)
    /// get the current array.
    /// </para>
    /// <para>
    /// To add an entry, you will need the <c>LargeBlobKey</c> for one of the
    /// credentials.
    /// </para>
    /// <para>
    /// This class is also the return from
    /// <see cref="Fido2Session.GetSerializedLargeBlobArray"/>. After getting the
    /// array, if there are any elements, they will be encrypted. Determine which
    /// elements you want to decrypt, obtain the <c>LargeBlobKey</c> for the
    /// associated credential and call the decryption method.
    /// </para>
    /// </remarks>
    public class SerializedLargeBlobArray
    {
        private const int DigestLength = 16;

        private readonly Logger _log = Log.GetLogger();
        private readonly List<LargeBlobEntry> _entryList;

        /// <summary>
        /// The list of entries in the Large Blob Array.
        /// </summary>
        /// <remarks>
        /// After getting a Serialized Large Blob Array from a YubiKey, this list
        /// will contain all of the entries currently stored. You can now delete
        /// or add entries. If you want to "edit" an existing entry, add a new
        /// entry with the updated information, then delete the previous version.
        /// <para>
        /// Upon retrieval, each entry's blob data is still encrypted. Use
        /// <see cref="LargeBlobEntry.TryDecrypt"/> to see the actual data.
        /// </para>
        /// </remarks>
        public IReadOnlyList<LargeBlobEntry> Entries { get; private set; }

        /// <summary>
        /// The encoded Large Blob Array. This is the data that is digested. That
        /// is, perform Left16Bytes(SHA-256(EncodedArray)) and it should equal the
        /// <see cref="Digest"/>.
        /// </summary>
        /// <remarks>
        /// When you get the Serialized Large Blob Array from the YubiKey, this
        /// property (and the <see cref="Digest"/> property) are set. You can
        /// verify the digest at this point.
        /// <para>
        /// As soon as this class detects a change to one of the entries, this
        /// property and the <c>Digest</c> property are no longer valid and will
        /// be set to null. If you call <see cref="Encode"/> or
        /// <see cref="Fido2Session.SetSerializedLargeBlobArray"/> and this property is
        /// null, the array will be rebuilt.
        /// </para>
        /// </remarks>
        public ReadOnlyMemory<byte>? EncodedArray { get; private set; }

        /// <summary>
        /// The digest of the array elements (left 16 bytes of SHA-256).
        /// </summary>
        /// <remarks>
        /// When you get the Serialized Large Blob Array from the YubiKey, this
        /// property (and the <see cref="EncodedArray"/> property) are set. You
        /// can verify the digest at this point.
        /// <para>
        /// As soon as this class detects a change to one of the entries, this
        /// property and the <c>EncodedArray</c> property are no longer valid and
        /// will be set to null. If you call <see cref="Encode"/> or
        /// <see cref="Fido2Session.SetSerializedLargeBlobArray"/> and this property is
        /// null, the array will be rebuilt and a new digest will be computed.
        /// </para>
        /// </remarks>
        public ReadOnlyMemory<byte>? Digest { get; private set; }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private SerializedLargeBlobArray()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a new instance of <see cref="SerializedLargeBlobArray"/> based on the
        /// given CBOR encoding.
        /// </summary>
        /// <remarks>
        /// The encoding must follow the definition of <c>serialized large blob
        /// array</c> in section 6.10 of the CTAP 2.1 standard.
        /// </remarks>
        /// <param name="cborEncoding">
        /// The serialized large blob array, encoded following the CTAP 2.1 and
        /// CBOR (RFC 8949) standards. That is, the expected encoding is either
        /// <code language="adoc">
        ///    80
        ///      80 76 be 8b 52 8d 00 75 f7 aa e9 8d 6f a5 7a 6d 3c
        ///   for the initial array (the 80 is an array with zero elements)
        ///   or
        ///    8x
        ///      A3 --entry map--
        ///       . . .
        ///      A3 --entry map--
        ///    --digest value--
        ///   where x is the number of entries and there are x
        ///      A3 --entry map--
        ///   and the digest value is 16 bytes.
        /// </code>
        /// </param>
        /// <exception cref="Ctap2DataException">
        /// The <c>cborEncoding</c> is not a valid CBOR encoding, or it is not a
        /// correct encoding for FIDO2 large blob data.
        /// </exception>
        public SerializedLargeBlobArray(ReadOnlyMemory<byte> cborEncoding)
        {
            _log.LogInformation("Create a new LargeBlobArray from the encoded Serialized Large Blob Array.");
            if (cborEncoding.Length <= DigestLength)
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info);
            }

            EncodedArray = cborEncoding.Slice(0, cborEncoding.Length - DigestLength);
            Digest = cborEncoding.Slice(cborEncoding.Length - DigestLength, DigestLength);

            try
            {
                var cborReader = new CborReader(EncodedArray.Value, CborConformanceMode.Ctap2Canonical);
                int? entries = cborReader.ReadStartArray();
                int count = entries ?? 0;

                // Set the initial size of the list to one more than the current
                // count, because a common use case is someone getting the
                // current list and adding an entry.
                _entryList = new List<LargeBlobEntry>(count + 1);
                for (int index = 0; index < count; index++)
                {
                    _entryList.Add(new LargeBlobEntry(cborReader.ReadEncodedValue()));
                }

                cborReader.ReadEndArray();

                Entries = _entryList;
            }
            catch (CborContentException cborException)
            {
                throw new Ctap2DataException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidFido2Info),
                    cborException);
            }
        }

        /// <summary>
        /// Add a new entry to the <see cref="Entries"/>. This method will
        /// build a new <see cref="LargeBlobEntry"/> from the <c>blobData</c> and
        /// the <c>largeBlobKey</c>
        /// </summary>
        /// <remarks>
        /// Generally you will obtain the current Large Blob Array, then remove
        /// or add entries. If you want to add an entry, call this method
        /// providing the data you want to store along with the
        /// <see cref="GetAssertionData.LargeBlobKey"/> in the returned
        /// <see cref="GetAssertionData"/> object. To "edit" an existing entry,
        /// add a new entry with the updated information, then delete the
        /// previous version.
        /// </remarks>
        /// <param name="blobData">
        /// The data to store in the Large Blob Array.
        /// </param>
        /// <param name="largeBlobKey">
        /// The 32-byte key returned by the YubiKey in an assertion, it will be
        /// used to encrypt the <c>blobData</c>.
        /// </param>
        public void AddEntry(ReadOnlyMemory<byte> blobData, ReadOnlyMemory<byte> largeBlobKey)
        {
            _log.LogInformation("Add a new entry to the Large Blob Array.");
            EncodedArray = null;
            Digest = null;
            var largeBlobEntry = new LargeBlobEntry(blobData, largeBlobKey);
            _entryList.Add(largeBlobEntry);
        }

        /// <summary>
        /// Remove the <c>LargeBlobEntry</c> at the given <c>index</c> from the
        /// <see cref="Entries"/>. Note that this can change the indices of the
        /// remaining entries.
        /// </summary>
        /// <remarks>
        /// The <c>LargeBlobEntry</c> is a disposable class. This method will
        /// call the <c>Dispose</c> method for the given entry as well as
        /// removing it from the list.
        /// <para>
        /// If there is no entry at the index (index >= list.Count), this method
        /// will do nothing (i.e. that is not an error).
        /// </para>
        /// </remarks>
        public void RemoveEntry(int index)
        {
            if (index < _entryList.Count)
            {
                _log.LogInformation("Remove an entry from the Large Blob Array.");
                EncodedArray = null;
                Digest = null;
                _entryList.RemoveAt(index);
            }
        }

        /// <summary>
        /// Build the Serialized Large Blob Array. This builds the CBOR encoding
        /// of the large blob array, digests that array, and appends the digest.
        /// </summary>
        /// <remarks>
        /// There is the Large Blob Array, which is the CBOR encoded array of
        /// entries. Then the Serialized Large Blob Array is the concatenation of
        /// the Large Blob Array with the digest of the Large Blob Array. This
        /// builds the Serialized Large Blob Array.
        /// <para>
        /// This is simply the concatenation of the <see cref="EncodedArray"/>
        /// and <see cref="Digest"/> properties. However, those can be null until
        /// a call is made to encode. For example, suppose you get a Large Blob
        /// Array from a YubiKey, and the <c>EncodedArray</c> and <c>Digest</c>
        /// properties are set. But now you <see cref="AddEntry"/>, which will
        /// mean the array and digest must change. This class will not update the
        /// array and digest until you call this method to encode (in case you
        /// want to add another entry).
        /// </para>
        /// <para>
        /// Once you call this method, the array and digest will be computed and
        /// those properties will be set.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A new byte array containing the Serialized Large Blob Array.
        /// </returns>
        public byte[] Encode()
        {
            _log.LogInformation("Build the Serialized Large Blob Array.");
            ReadOnlyMemory<byte> encoding = EncodeBlobArray();
            ReadOnlyMemory<byte> digest = ComputeDigest(encoding);

            byte[] returnValue = new byte[encoding.Length + digest.Length];
            var destination = new Memory<byte>(returnValue);
            encoding.CopyTo(destination);
            digest.CopyTo(destination.Slice(encoding.Length));

            return returnValue;
        }

        // Create the CBOR Array of each of the entries. Set EncodedArray to this
        // value, and return it as well.
        // If EncodedArray is not null, just return its value.
        private ReadOnlyMemory<byte> EncodeBlobArray()
        {
            if (EncodedArray is null)
            {
                Digest = null;
                var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
                cbor.WriteStartArray(_entryList.Count);
                foreach (LargeBlobEntry entry in _entryList)
                {
                    cbor.WriteEncodedValue(new ReadOnlySpan<byte>(entry.CborEncode()));
                }

                cbor.WriteEndArray();

                EncodedArray = new ReadOnlyMemory<byte>(cbor.Encode());
            }

            return EncodedArray.Value;
        }

        /// <summary>
        /// Determine if the <see cref="Digest"/> verifies for the given
        /// <see cref="EncodedArray"/>.
        /// </summary>
        /// <remarks>
        /// If either or both <c>EncodedData</c> and <c>Digest</c> is null, this
        /// method returns <c>false</c>. If they are both present, this method
        /// will compute the SHA-256 digest of the <c>EncodedData</c>, and
        /// compare the "left" 16 bytes of that result with <c>Digest</c>.
        /// </remarks>
        /// <returns>
        /// A boolean, <c>true</c> if there is <c>EncodedData</c> and a
        /// <c>Digest</c>, and the digest is correct. <c>false</c> otherwise.
        /// </returns>
        public bool IsDigestVerified()
        {
            bool returnValue = false;

            if (!(EncodedArray is null) && !(Digest is null))
            {
                using SHA256 digester = CryptographyProviders.Sha256Creator();
                byte[] computedDigest = digester.ComputeHash(EncodedArray.Value.ToArray());
                var digestSpan = new Span<byte>(computedDigest, 0, DigestLength);
                returnValue = MemoryExtensions.SequenceEqual<byte>(digestSpan, Digest.Value.Span);
            }

            return returnValue;
        }

        // left 16 bytes of SHA-256(EncodedArray)
        // Set Digest and return the result (as a non-nullable).
        // If Digest is not null, don't compute, just return its value.
        private ReadOnlyMemory<byte> ComputeDigest(ReadOnlyMemory<byte> encoding)
        {
            if (Digest is null)
            {
                using SHA256 digester = CryptographyProviders.Sha256Creator();
                Digest = new ReadOnlyMemory<byte>(digester.ComputeHash(encoding.ToArray()), 0, DigestLength);
            }

            return Digest.Value;
        }
    }
}
