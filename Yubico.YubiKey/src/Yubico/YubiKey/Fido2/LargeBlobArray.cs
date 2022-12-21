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
using System.Formats.Cbor;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Contains the data returned by the YubiKey after getting a the large blob.
    /// </summary>
    public class LargeBlobArray
    {
        private const int KeyArray = 1;
        private const int EntryCount = 1;
        private const int DigestLength = 16;

        /// <summary>
        /// The list of blobs.
        /// </summary>
        public IReadOnlyList<byte[]> BlobList { get; private set; }

        /// <summary>
        /// The full array of blobs. This is the data that is digested. That is,
        /// perform SHA-256(BlobArray) and it should equal the
        /// <see cref="Digest"/>.
        /// </summary>
        public ReadOnlyMemory<byte> BlobArray { get; private set; }

        /// <summary>
        /// The digest of the array elements (left 16 bytes of SHA-256).
        /// </summary>
        public ReadOnlyMemory<byte> Digest { get; private set; }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private LargeBlobArray()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a new instance of <see cref="LargeBlobArray"/> based on the
        /// given Cbor encoding.
        /// </summary>
        /// <remarks>
        /// The encoding must follow the definition of <c>serialized large blob
        /// array</c> in section 6.10 of the CTAP 2.1 standard.
        /// </remarks>
        /// <param name="cborEncoding">
        /// The serialized large blob array, encoded following the CTAP 2.1 and
        /// CBOR (RFC 8949) standards.
        /// </param>
        /// <exception cref="Ctap2DataException">
        /// The <c>cborEncoding</c> is not a valid CBOR encoding, or it is not a
        /// correct encoding for FIDO2 large blob data.
        /// </exception>
        public LargeBlobArray(ReadOnlyMemory<byte> cborEncoding)
        {
            try
            {
                var cborReader = new CborReader(cborEncoding, CborConformanceMode.Ctap2Canonical);
                int? entries = cborReader.ReadStartMap();
                int count = entries ?? 0;

                int mapKey = 0;
                byte[] arrayPlus = Array.Empty<byte>();
                if (count > 0)
                {
                    // The only element in the map is key/value pair with the key
                    // being 1 and the value a byte array.
                    // The byte array is the Cbor array followed by the digest.
                    mapKey = cborReader.ReadInt32();
                    arrayPlus = cborReader.ReadByteString();
                }
                cborReader.ReadEndMap();

                if ((count != EntryCount) || (mapKey != KeyArray) || (arrayPlus.Length <= DigestLength))
                {
                    throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info);
                }

                // arrayPlus is the array plus the trailing 16 bytes.
                // We now want to decode the array. So let's look at the array
                // data using a new CborReader.
                BlobArray = new ReadOnlyMemory<byte>(arrayPlus, 0, arrayPlus.Length - DigestLength);
                cborReader = new CborReader(BlobArray, CborConformanceMode.Ctap2Canonical);
                entries = cborReader.ReadStartArray();
                count = entries ?? 0;

                List<byte[]> destination = new List<byte[]>(count);
                for (int index = 0; index < count; index++)
                {
                    destination.Add(cborReader.ReadByteString());
                }
                cborReader.ReadEndArray();

                BlobList = destination;

                // The last 16 bytes make up the digest.
                Digest = new ReadOnlyMemory<byte>(arrayPlus, arrayPlus.Length - DigestLength, DigestLength);
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
    }
}
