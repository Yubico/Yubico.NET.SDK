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
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.Commands;
using CryptographicOperations = Yubico.Core.Cryptography.CryptographicOperations;

namespace Yubico.YubiKey.Fido2
{
    // This portion of the Fido2Session class deals with setting and getting
    // large blobs.
    public sealed partial class Fido2Session
    {
        private const int MessageOverhead = 64;
        private const int KeyEncodedArray = 1;
        private const int DataToAuthFfByteCount = 32;
        private const int DataToAuthPrefixLength = DataToAuthFfByteCount + 6;

        /// <summary>
        /// Get the current <c>Serialized Large Blob Array</c> out of the
        /// YubiKey. See also the
        /// <xref href="Fido2LargeBlobs">User's Manual entry</xref> on large
        /// blobs.
        /// </summary>
        /// <remarks>
        /// Note that this feature is not available on all YubiKeys. To determine
        /// if large blobs are supported on a YubiKey, check the
        /// <see cref="AuthenticatorInfo.Options"/> in the
        /// <see cref="AuthenticatorInfo"/> property of this class. For example,
        /// <code language="csharp">
        ///     OptionValue optionValue =
        ///         fido2Session.AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.largeBlobs);
        ///     if (optionValue != OptionValue.True)
        ///     {
        ///         return;
        ///     }
        ///     int maxLargeBlobsLength = authInfo.MaximumSerializedLargeBlobArray ?? 0;
        /// </code>
        /// <para>
        /// A serialized large blob array is the large blob array concatenated
        /// with the digest of the array. The digest is the first 16 bytes (left
        /// 16 bytes) of the SHA-256 of the CBOR-encoded array.
        /// </para>
        /// <para>
        /// Once you have the object containing the data, it is possible to read
        /// each entry by decrypting, using the appropriate <c>LargeBlobKey</c>.
        /// Each entry is encrypted using the large blob key of one of the
        /// credentials (each credential has a different large blob key). The
        /// standard specifies obtaining a large blob key (likely from a
        /// <c>GetAssertion</c> call), and trying to decrypt each entry in the
        /// array using that key. If it succeeds, that entry is associated with
        /// the credential and the decrypted data will be returned. See also
        /// <see cref="LargeBlobEntry.TryDecrypt"/>
        /// </para>
        /// <para>
        /// A YubiKey is manufactured with the initial large blob data, which is
        /// an array of zero elements plus the digest of the CBOR-encoding of a
        /// zero-element array. An array with zero elements is simply the single
        /// byte <c>0x80</c>. Hence, there will always be a current large blob
        /// array to retrieve.
        /// </para>
        /// <para>
        /// The standard specifies that when reading a serialized large blob
        /// array, a client must verify the digest. If the digest does not
        /// verify, the standard specifically says, "the configuration is corrupt
        /// and the platform MUST discard it and act as if the initial serialized
        /// large-blob array was received." This method will verify the digest
        /// value. If the digest does not verify, this method will return a new
        /// <c>SerializedLargeBlobArray</c> containing the initial value. It
        /// will not overwrite the data on the YubiKey, so you can still use the
        /// <see cref="GetLargeBlobCommand"/> to get the raw data.
        /// </para>
        /// <para>
        /// Because writing to the large blob area in a YubiKey means overwriting
        /// the existing data, it is recommended that to add to, remove from, or
        /// "edit" the large blob data, the caller should get the current large
        /// blob array, operate on the resulting <c>SerializedLargeBlobArray</c>,
        /// and then call <see cref="SetSerializedLargeBlobArray"/> with the
        /// updated data. Even if your application has not updated the large blob
        /// array, it is possible another application has stored data and you
        /// likely do not want to overwrite that data.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A new instance of the <see cref="SerializedLargeBlobArray"/> class
        /// containing the currently stored large blob data.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// The YubiKey selected does not support large blobs.
        /// </exception>
        public SerializedLargeBlobArray GetSerializedLargeBlobArray()
        {
            _log.LogInformation("Get the current large blob array.");

            // Does the YubiKey support Large Blobs?
            if (AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.largeBlobs) != OptionValue.True)
            {
                throw new NotSupportedException(ExceptionMessages.NotSupportedByYubiKeyVersion);
            }

            int offset = 0;
            int maxFragmentLength = AuthenticatorInfo.MaximumMessageSize ?? AuthenticatorInfo.DefaultMaximumMessageSize;
            using var fullEncoding = new MemoryStream(maxFragmentLength);

            maxFragmentLength -= MessageOverhead;

            ReadOnlyMemory<byte> currentData;

            do
            {
                var command = new GetLargeBlobCommand(offset, maxFragmentLength);
                var response = Connection.SendCommand(command);
                currentData = response.GetData();

                fullEncoding.Write(currentData.ToArray(), 0, currentData.Length);

                // For the next call, get the data starting where this call left
                // off.
                offset += currentData.Length;

            } while (currentData.Length >= maxFragmentLength);

            // The data from the YubiKey is a map of 1, that one being a byte
            // string. The contents of the byte string is the serialized large
            // blob array.
            //   A1
            //      01 byte string
            var cborMap = new CborMap<int>(
                fullEncoding.GetBuffer().AsMemory<byte>(0, (int)fullEncoding.Length));
            var encodedArray = cborMap.ReadByteString(KeyEncodedArray);

            var returnValue = new SerializedLargeBlobArray(encodedArray);

            // The standard specifies verifying the digest.
            if (returnValue.IsDigestVerified())
            {
                return returnValue;
            }

            // The standard says if the digest does not verify, the Large Blob is
            // the initial large blob.
            byte[] initialLargeBlobArray = new byte[] {
                0x80, 0x76, 0xbe, 0x8b, 0x52, 0x8d, 0x00, 0x75, 0xf7, 0xaa, 0xe9, 0x8d, 0x6f, 0xa5, 0x7a, 0x6d, 0x3c
            };
            return new SerializedLargeBlobArray(initialLargeBlobArray);
        }

        /// <summary>
        /// Set the <c>Serialized Large Blob Array</c> in the YubiKey to contain the
        /// data in the input <c>serializedLargeBlobArray</c>. See also the
        /// <xref href="Fido2LargeBlobs">User's Manual entry</xref> on large
        /// blobs.
        /// </summary>
        /// <remarks>
        /// Note that this feature is not available on all YubiKeys. To determine
        /// if large blobs are supported on a YubiKey, check the
        /// <see cref="AuthenticatorInfo.Options"/> in the
        /// <see cref="AuthenticatorInfo"/> property of this class. For example,
        /// <code language="csharp">
        ///     OptionValue optionValue =
        ///         fido2Session.AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.largeBlobs);
        ///     if (optionValue != OptionValue.True)
        ///     {
        ///         return;
        ///     }
        ///     int maxLargeBlobsLength = authInfo.MaximumSerializedLargeBlobArray ?? 0;
        /// </code>
        /// <para>
        /// This method will overwrite the current contents of the large blob
        /// data on the YubiKey. Hence, an application should get the current
        /// contents (<see cref="GetSerializedLargeBlobArray"/>) and add to, remove
        /// from, or "edit" the contents. Then use this method to store the
        /// updated large blob.
        /// </para>
        /// <para>
        /// This method will need the PIN to have been verified with the
        /// <see cref="PinUvAuthTokenPermissions.LargeBlobWrite"/>. If that
        /// permission is not set, this method will verify the PIN (even if it
        /// had already been verified during this session) with the permission,
        /// and use the <see cref="KeyCollector"/> in order to obtain the PIN. If
        /// you do not want this method to call the <c>KeyCollector</c> you must
        /// verify the PIN explicitly (see <c>TryVerifyPin</c>)
        /// with the <c>permissions</c> argument set with <c>LargeBlobWrite</c>. You will
        /// likely want to get the current permissions
        /// (<see cref="AuthTokenPermissions"/>) and add <c>LargeBlobWrite</c>.
        /// For example,
        /// <code language="csharp">
        ///     PinUvAuthTokenPermissions permissions = AuthTokenPermissions ?? PinUvAuthTokenPermissions.None;
        ///     if (!permissions.HasFlag(PinUvAuthTokenPermissions.LargeBlobWrite))
        ///     {
        ///         permissions |= PinUvAuthTokenPermissions.LargeBlobWrite;
        ///         bool isVerified = TryVerifyPin(
        ///            currentPin, permissions, null, out int retriesRemaining, out bool rebootRequired);
        ///     }
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="serializedLargeBlobArray">
        /// The object containing the data to store.
        /// </param>
        /// <exception cref="Fido2Exception">
        /// The YubiKey could not complete the operation, likely because of a
        /// wrong PIN or fingerprint.
        /// </exception>
        public void SetSerializedLargeBlobArray(SerializedLargeBlobArray serializedLargeBlobArray)
        {
            _log.LogInformation("Set the YubiKey with a new large blob array.");

            if (serializedLargeBlobArray is null)
            {
                throw new ArgumentNullException(nameof(serializedLargeBlobArray));
            }
            byte[] encodedArray = serializedLargeBlobArray.Encode();

            byte[] token = new byte[MaximumAuthTokenLength];
            try
            {
                int offset = 0;
                int remaining = encodedArray.Length;
                int maxFragmentLength = AuthenticatorInfo.MaximumMessageSize ?? AuthenticatorInfo.DefaultMaximumMessageSize;
                maxFragmentLength -= MessageOverhead;
                int currentLength;
                bool forceToken = false;

                using HashAlgorithm digester = CryptographyProviders.Sha256Creator();

                do
                {
                    var currentToken = GetAuthToken(
                        forceToken, PinUvAuthTokenPermissions.LargeBlobWrite, null);
                    currentToken.CopyTo(token.AsMemory());

                    currentLength = remaining >= maxFragmentLength ? maxFragmentLength : remaining;

                    byte[] dataToAuth = BuildDataToAuth(encodedArray, offset, currentLength, digester);
                    byte[] pinUvAuthParam = AuthProtocol.AuthenticateUsingPinToken(token, 0, currentToken.Length, dataToAuth);

                    var command = new SetLargeBlobCommand(
                        new ReadOnlyMemory<byte>(encodedArray, offset, currentLength),
                        offset,
                        encodedArray.Length,
                        pinUvAuthParam,
                        (int)AuthProtocol.Protocol);

                    var response = Connection.SendCommand(command);
                    if (response.Status == ResponseStatus.Success)
                    {
                        remaining -= currentLength;
                        offset += currentLength;
                        forceToken = false;
                    }
                    else if (response.CtapStatus == CtapStatus.PinAuthInvalid && !forceToken)
                    {
                        forceToken = true;
                    }
                    else
                    {
                        throw new Fido2Exception(response.StatusMessage);
                    }
                } while (remaining > 0);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(token);
            }
        }

        // Build a buffer that contains the data to authenticate when setting a
        // large blob.
        // The data is defined in the standard as
        //   ff ff ... ff || 0c 00 || little-endian-4(offset) ||  SHA-256(largeBlobArray)
        //  |<- 32 bytes->|
        private static byte[] BuildDataToAuth(byte[] inputData, int dataOffset, int dataLength, HashAlgorithm digester)
        {
            digester.Initialize();
            byte[] digest = digester.ComputeHash(inputData, dataOffset, dataLength);

            byte[] dataToAuth = new byte[DataToAuthPrefixLength + digest.Length];
            var dataSpan = new Span<byte>(dataToAuth);
            dataSpan.Fill(0xFF);
            int index = DataToAuthFfByteCount;
            dataToAuth[index++] = 0x0C;
            dataToAuth[index++] = 0x00;
            BinaryPrimitives.WriteInt32LittleEndian(dataSpan.Slice(DataToAuthFfByteCount + 2), dataOffset);

            digest.CopyTo(dataSpan.Slice(DataToAuthPrefixLength));

            return dataToAuth;
        }
    }
}
