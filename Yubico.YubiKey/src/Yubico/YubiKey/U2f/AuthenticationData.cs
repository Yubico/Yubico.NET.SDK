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
using System.Buffers.Binary;
using System.Globalization;
using Yubico.Core.Logging;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.U2f
{
    /// <summary>
    /// Represents a single U2F authentication response.
    /// </summary>
    /// <remarks>
    /// This class is used to see what the was returned by the YubiKey in an
    /// authentication operation, as well as a method to verify the signature.
    /// </remarks>
    public class AuthenticationData : U2fSignedData
    {
        // The encoding is
        // (userPresence || counter || sig)
        // The signature can be 70, 71, or 72 bytes long, which means that there
        // really is a min length of 75 and a max length of 77. But we're going
        // to report the signature exactly as the YubiKey returned it, so the min
        // is just 1 byte for signature, and there is no max.
        private const int MinEncodedLength = 6;
        private const int MsgUserPresenceOffset = 0;
        private const int MsgCounterOffset = 1;
        private const int MsgSignatureOffset = 5;

        private const byte UserPresenceMask = 0x01;

        // The data to verify is
        // (appId || userPresence || counter || clientData)
        // where the challenge is in the client data.
        // We're also going to place the BER version of the signature onto the
        // end of the data.
        // (appId || userPresence || counter || clientData || berSignature)
        private const int AppIdOffset = 0;
        private const int UserPresenceOffset = AppIdOffset + AppIdHashLength;
        private const int CounterOffset = UserPresenceOffset + 1;
        private const int ClientDataOffset = CounterOffset + CounterLength;
        private const int SignatureOffset = ClientDataOffset + ClientDataHashLength;
        private const int PayloadLength = AppIdHashLength + ClientDataHashLength + CounterLength + MaxBerSignatureLength + 1;

        private readonly Logger _log = Log.GetLogger();

        /// <summary>
        /// If the user's presence was verified in the authentication operation,
        /// this will be <c>true</c>. Otherwise it will be <c>false</c>.
        /// </summary>
        public bool UserPresenceVerified { get; private set; }

        /// <summary>
        /// The counter used in computing the signature.
        /// </summary>
        public int Counter { get; private set; }

        /// <summary>
        /// Build a new <c>AuthenticationData</c> object from the encoded
        /// response, which is the data portion of the value returned by the
        /// YubiKey.
        /// </summary>
        public AuthenticationData(ReadOnlyMemory<byte> encodedResponse)
            : base(PayloadLength, AppIdOffset, ClientDataOffset, SignatureOffset)
        {
            _log.LogInformation("Create a new instance of U2F AuthenticationData by decoding.");
            if (encodedResponse.Length < MinEncodedLength
                || (encodedResponse.Span[MsgUserPresenceOffset] & ~UserPresenceMask) != 0)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidDataEncoding));
            }

            _buffer[UserPresenceOffset] = encodedResponse.Span[MsgUserPresenceOffset];
            UserPresenceVerified = (_buffer[UserPresenceOffset] & UserPresenceMask) != 0;
            encodedResponse.Slice(MsgCounterOffset, CounterLength).CopyTo(_bufferMemory.Slice(CounterOffset));
            Counter = BinaryPrimitives.ReadInt32BigEndian(encodedResponse.Span.Slice(MsgCounterOffset, CounterLength));
            Signature = encodedResponse.Slice(MsgSignatureOffset);
        }

        /// <summary>
        /// Use the given public key to verify the signature. Use the given
        /// Application ID (hash of origin data) and Client Data Hash (includes
        /// the challenge) to build the data to verify.
        /// </summary>
        /// <remarks>
        /// This will build the data to verify from the input
        /// <c>applicationId</c>, <c>clientDataHash</c>, along with the user
        /// presence and counter data inside this object. The user presence and
        /// counter were part of the authentication response, the encodedResponse
        /// of the constructor. It will then verify the signature inside this
        /// object (from the encoded response) using the public key.
        /// <para>
        /// The public key is returned by the YubiKey during registration. See
        /// the <see cref="RegistrationData"/> class.
        /// </para>
        /// </remarks>
        /// <param name="userPublicKey">
        /// The public key partner to the private key used to authenticate this
        /// credential, as an encoded EC Point.
        /// </param>
        /// <param name="applicationId">
        /// The original <c>applicationId</c> that was provided to the
        /// <c>AuthenticateCommand</c>. This is the hash of the origin data.
        /// </param>
        /// <param name="clientDataHash">
        /// The original <c>clientDataHash</c> that was provided to the
        /// <c>AuthenticateCommand</c>. This contains the challenge.
        /// </param>
        /// <returns>
        /// A <c>bool</c>, <c>true</c> if the signature verifies, <c>false</c>
        /// otherwise.
        /// </returns>
        public bool VerifySignature(
            ReadOnlyMemory<byte> userPublicKey, ReadOnlyMemory<byte> applicationId, ReadOnlyMemory<byte> clientDataHash)
        {
            _log.LogInformation("Verify a U2F AuthenticationData signature.");

            using var verifier = new EcdsaVerify(userPublicKey);
            return VerifySignature(verifier, applicationId, clientDataHash);
        }
    }
}
