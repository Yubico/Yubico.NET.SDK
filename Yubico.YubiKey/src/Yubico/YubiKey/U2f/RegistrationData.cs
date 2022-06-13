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
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.U2f
{
    /// <summary>
    /// Represents a single U2F registration.
    /// </summary>
    /// <remarks>
    /// This represents the registration data returned by the YubiKey when
    /// registering a new U2F credential. The information stored in this
    /// structure can be sent back to the relying party to store for future
    /// validation (authentication) attempts.
    /// <para>
    /// This class is useful for storing registration data, in scenarios like U2F
    /// preregistration.
    /// </para>
    /// </remarks>
    public class RegistrationData : U2fSignedData
    {
        private const int CertTag = 0x30;

        // The encoding is
        // (05 || pub key || len || keyHandle || cert || sig)
        // The cert is not a specified length (at the very least the signature is
        // variable length), plus the signature can be 70, 71, or 72 bytes long,
        // which means that is is really a min length of 75 and a max length of
        // 77. We're just going to set the min total length to a value that says
        // there must be at least two bytes beyond the keyHandle.
        private const int MinEncodedLength = 133;
        private const int MsgReservedOffset = 0;
        private const byte MsgReservedValue = 0x05;
        private const int MsgPublicKeyOffset = 1;
        private const int MsgKeyHandleOffset = MsgPublicKeyOffset + PublicKeyLength;
        private const int MsgCertOffset = MsgKeyHandleOffset + KeyHandleLength + 1;

        // The data to verify is
        // (00 || appId || clientData || keyHandle || publicKey)
        // where the challenge is in the client data.
        // We're also going to place the BER version of the signature onto the
        // end of the data.
        // (00 || appId || clientData || keyHandle || publicKey || berSignature)
        private const int ReservedOffset = 0;
        private const byte ReservedValue = 0x00;
        private const int AppIdOffset = 1;
        private const int ClientDataOffset = AppIdOffset + AppIdHashLength;
        private const int KeyHandleOffset = ClientDataOffset + ClientDataHashLength;
        private const int PublicKeyOffset = KeyHandleOffset + KeyHandleLength;
        private const int SignatureOffset = PublicKeyOffset + PublicKeyLength;
        private const int PayloadLength = AppIdHashLength + ClientDataHashLength + KeyHandleLength + PublicKeyLength + MaxBerSignatureLength + 1;

        /// <summary>
        /// The ECDSA public key for this user credential. Each coordinate must
        /// be 32 bytes and the point must be on the P256 curve.
        /// </summary>
        /// <remarks>
        /// This is the public key that will be used to verify an authentication.
        /// Save this key and pass it into the <see cref="VerifySignature"/> method
        /// when verifying for authentication.
        /// <para>
        /// This is a public key for ECDSA using the NIST P256 curve and SHA256,
        /// per the FIDO specifications.
        /// </para>
        /// <para>
        /// If you want to get the public key as an instance of <c>ECPoint</c>,
        /// do this.
        /// <code language="csharp">
        ///   var pubKeyPoint = new ECPoint
        ///   {
        ///       X = UserPublicKey.Slice(1, 32).ToArray(),
        ///       Y = UserPublicKey.Slice(33, 32).ToArray(),
        ///   };
        /// </code>
        /// </para>
        /// </remarks>
        public ReadOnlyMemory<byte> UserPublicKey
        {
            get => _bufferMemory.Slice(PublicKeyOffset, PublicKeyLength);
            set => SetBufferData(value, PublicKeyLength, PublicKeyOffset, nameof(UserPublicKey));
        }

        /// <summary>
        /// The private key handle created by the YubiKey. Save this value and
        /// use it when authenticating.
        /// </summary>
        public ReadOnlyMemory<byte> KeyHandle
        {
            get => _bufferMemory.Slice(KeyHandleOffset, KeyHandleLength);
            set => SetBufferData(value, KeyHandleLength, KeyHandleOffset, nameof(KeyHandle));
        }

        /// <summary>
        /// The Attestation cert used to verify a newly-registered credential.
        /// </summary>
        /// <remarks>
        /// There is a <see cref="VerifySignature"/> method that will use the public key
        /// inside the <c>AttestationCert</c> to verify the signature on the
        /// registration response. That verifies that the newly-generated public
        /// key was indeed generated on the device. However, the SDK has no
        /// classes or methods to verify the <c>AttestationCert</c> itself. The
        /// relying party app that performs verification must obtain any root and
        /// CA certs necessary and perform certificate verification using some
        /// other means.
        /// </remarks>
        public X509Certificate2 AttestationCert { get; private set; }

        /// <summary>
        /// Build a new <c>RegistrationData</c> object from the encoded
        /// response, which is the data portion of the value returned by the
        /// YubiKey.
        /// </summary>
        public RegistrationData(ReadOnlyMemory<byte> encodedResponse)
            : base(PayloadLength, AppIdOffset, ClientDataOffset, SignatureOffset)
        {
            bool isValid = false;
            int certLength = 1;
            if (encodedResponse.Length > MinEncodedLength)
            {
                if ((encodedResponse.Span[MsgReservedOffset] == MsgReservedValue)
                    && (encodedResponse.Span[MsgKeyHandleOffset] == KeyHandleLength)
                    && (encodedResponse.Span[MsgPublicKeyOffset] == PublicKeyTag))
                {
                    ReadOnlyMemory<byte> certAndSig = encodedResponse.Slice(MsgCertOffset);
                    var tlvReader = new TlvReader(certAndSig);
                    if (tlvReader.TryReadEncoded(out ReadOnlyMemory<byte> cert, CertTag))
                    {
                        certLength = cert.Length;
                        isValid = true;
                    }
                }
            }

            if (!isValid)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidDataEncoding));
            }

            _buffer[ReservedOffset] = ReservedValue;

            UserPublicKey = encodedResponse.Slice(MsgPublicKeyOffset, PublicKeyLength);
            KeyHandle = encodedResponse.Slice(MsgKeyHandleOffset + 1, KeyHandleLength);
            AttestationCert = new X509Certificate2(encodedResponse.Slice(MsgCertOffset, certLength).ToArray());
            Signature = encodedResponse.Slice(MsgCertOffset + certLength);
            _berSignatureLength = encodedResponse.Length - (MsgCertOffset + certLength);
        }

        /// <summary>
        /// Verify the signature using the public key in the attestation
        /// cert returned by the YubiKey in the registration command/response.
        /// Use the given Client Data Hash and Application ID to build the data
        /// to verify.
        /// </summary>
        /// <param name="applicationId">
        /// The appId (origin data or hash of origin) that was provided to create
        /// this registration.
        /// </param>
        /// <param name="clientDataHash">
        /// The `clientDataHash` (challenge data) that was provided to create
        /// this registration.
        /// </param>
        /// <returns>
        /// A `bool`, `true` if the signature verifies, `false` otherwise.
        /// </returns>
        public bool VerifySignature(ReadOnlyMemory<byte> applicationId, ReadOnlyMemory<byte> clientDataHash)
        {
            using ECDsa ecdsaObject = AttestationCert.GetECDsaPublicKey();
            return VerifySignature(ecdsaObject, applicationId, clientDataHash);
        }
    }
}
