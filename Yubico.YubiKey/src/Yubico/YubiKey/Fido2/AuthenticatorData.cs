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
using System.Buffers.Binary;
using System.Formats.Cbor;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Contains information about the credential, assertion, or the
    /// authenticator itself after making a credential or getting an assertion.
    /// </summary>
    /// <remarks>
    /// When a new credential is made, or a credential is used to get an
    /// assertion, the YubiKey returns data about that operation. When making a
    /// credential, this includes information about the authenticator itself,
    /// such as the aaguid.
    /// > The <c>authenticator data object</c> defined in the FIDO2 standard is
    /// > encoded but not following the rules of Cbor or DER or any other
    /// > standard encoding scheme. The encoding is defined in the W3C standard.
    /// </remarks>
    public class AuthenticatorData
    {
        private const int RelyingPartyIdHashLength = 32;
        private const int SignCountLength = 4;
        private const int CredentialIdLengthLength = 2;
        private const int AaguidLength = 16;
        private const byte UserPresenceBit = 0x01;
        private const byte UserVerificationBit = 0x04;
        private const byte AttestedBit = 0x40;
        private const byte ExtensionsBit = 0x80;

        /// <summary>
        /// The encoded authenticator data is used to verify the attestation
        /// statement (make credential) or assertion signature (get assertion).
        /// </summary>
        public ReadOnlyMemory<byte> EncodedAuthenticatorData { get; private set; }

        /// <summary>
        /// The digest of the relying party ID. It is the SHA-256 digest of the
        /// <c>Id</c> property of the <see cref="RelyingParty"/> class passed to
        /// the <c>MakeCredential</c> method or command as part of the
        /// <see cref="MakeCredentialParameters"/>.
        /// </summary>
        public ReadOnlyMemory<byte> RelyingPartyIdHash { get; private set; }

        /// <summary>
        /// If <c>true</c>, a test of user presence indicates a user is indeed
        /// present before making the credential (e.g. the YubiKey was touched).
        /// Otherwise it will be <c>false</c>.
        /// </summary>
        public bool UserPresence { get; private set; }

        /// <summary>
        /// If <c>true</c>, a test of user verification operation indicates the
        /// user has indeed been verified. Note that this can be biometric
        /// verification, as well as touch plus PIN, or password. Otherwise it
        /// will be <c>false</c>.
        /// </summary>
        public bool UserVerification { get; private set; }

        /// <summary>
        /// The count the authenticator returns. This should be an increasing
        /// value for each time <c>GetAssertion</c> is called and is returned to
        /// the relying party, which can verify that it is greater than the
        /// previous value (to help thwart authenticator cloning).
        /// </summary>
        public int SignatureCounter { get; private set; }

        /// <summary>
        /// The authenticator's AAGUID. This is an optional value and can be null.
        /// </summary>
        /// <remarks>
        /// When making a credential, this information will be provided, when
        /// getting an assertion, it will not.
        /// </remarks>
        public ReadOnlyMemory<byte>? Aaguid { get; private set; }

        /// <summary>
        /// The CredentialId. This is an optional value and can be null.
        /// </summary>
        /// <remarks>
        /// When making a credential, this information will be provided, when
        /// getting an assertion, it will not.
        /// </remarks>
        public CredentialId? CredentialId { get; private set; }

        /// <summary>
        /// The Credential's public key. This is an optional value and can be null.
        /// </summary>
        /// <remarks>
        /// When making a credential, this information will be provided, when
        /// getting an assertion, it will not.
        /// </remarks>
        public CoseKey? CredentialPublicKey { get; private set; }

        /// <summary>
        /// The list of extensions. This is an optional value and can be null.
        /// </summary>
        /// <remarks>
        /// Each extension is a key/value pair. All keys are strings, but each
        /// extension has its own definition of a value. It could be an int, or
        /// it could be a map containing a string and a boolean,. It is the
        /// caller's responsibility to decode the value.
        /// <para>
        /// For each value, the standard (or the vendor in the case of
        /// vendor-defined extensions) will define the structure of the value.
        /// From that structure the value can be decoded following CBOR rules.
        /// The encoded value is what is stored in this dictionary.
        /// </para>
        /// </remarks>
        public IReadOnlyDictionary<string, byte[]>? Extensions { get; private set; }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private AuthenticatorData()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a new instance of <see cref="AuthenticatorData"/> based on the
        /// given encoding.
        /// </summary>
        /// <remarks>
        /// The overall encoding does not follow any standard encoding scheme but
        /// is defined in the W3C standard, although two of the elements are
        /// Cbor-encoded structures.
        /// <para>
        /// This constructor will copy the input data, not just a reference.
        /// </para>
        /// </remarks>
        /// <param name="encodedData">
        /// The authenticator data, encoded following the definition in the W3C
        /// standard.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The <c>encodedData</c> is not a correct encoding for FIDO2
        /// authenticator data.
        /// </exception>
        public AuthenticatorData(ReadOnlyMemory<byte> encodedData)
        {
            EncodedAuthenticatorData = new ReadOnlyMemory<byte>(encodedData.ToArray());

            RelyingPartyIdHash = EncodedAuthenticatorData.Slice(0, RelyingPartyIdHashLength);
            int offset = RelyingPartyIdHashLength;
            byte flags = EncodedAuthenticatorData.Span[offset];
            UserPresence = (flags & UserPresenceBit) != 0;
            UserVerification = (flags & UserVerificationBit) != 0;
            bool attestedData = (flags & AttestedBit) != 0;
            bool extensions = (flags & ExtensionsBit) != 0;
            offset++;
            SignatureCounter = BinaryPrimitives.ReadInt32BigEndian(
                EncodedAuthenticatorData.Span.Slice(offset, SignCountLength));
            offset += SignCountLength;

            if (attestedData)
            {
                Aaguid = EncodedAuthenticatorData.Slice(offset, AaguidLength);
                offset += AaguidLength;
                int credentialIdLength = BinaryPrimitives.ReadInt16BigEndian(
                    EncodedAuthenticatorData.Span.Slice(offset, CredentialIdLengthLength));
                offset += CredentialIdLengthLength;
                CredentialId = new CredentialId() { Id = EncodedAuthenticatorData.Slice(offset, credentialIdLength) };
                offset += credentialIdLength;
                CredentialPublicKey = CoseKey.Create(EncodedAuthenticatorData[offset..], out int bytesRead);
                offset += bytesRead;
            }
            if (extensions)
            {
                var extensionList = new Dictionary<string, byte[]>();
                var cbor = new CborReader(EncodedAuthenticatorData[offset..], CborConformanceMode.Ctap2Canonical);
                int? entries = cbor.ReadStartMap();
                int count = entries ?? 0;
                while (count > 0)
                {
                    string extensionKey = cbor.ReadTextString();
                    extensionList.Add(extensionKey, cbor.ReadEncodedValue().ToArray());
                    count--;
                }
                cbor.ReadEndMap();

                Extensions = extensionList;
            }
        }

        /// <summary>
        /// Get the value of the "credBlob" extension. This returns the decoded
        /// value.
        /// </summary>
        /// <remarks>
        /// Because this extension is used more often, a dedicated method is
        /// provided as a convenience. There is no need for the caller to
        /// decode the byte array value for the key "credBlob".
        /// <para>
        /// If there is no "credBlob" extension, this method will return an empty
        /// byte array.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A byte array containing the decoded "credBlob" extension.
        /// </returns>
        public byte[] GetCredBlobExtension()
        {
            if (!(Extensions is null))
            {
                if (Extensions.ContainsKey("credBlob"))
                {
                    byte[] encodedValue = Extensions["credBlob"];
                    var cborReader = new CborReader(encodedValue, CborConformanceMode.Ctap2Canonical);
                    return cborReader.ReadByteString();
                }
            }

            return Array.Empty<byte>();
        }
    }
}
