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
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    ///     Contains the data returned by the YubiKey after making a credential.
    /// </summary>
    /// <remarks>
    ///     When a new credential is made, the YubiKey returns data about that
    ///     credential, including attestation information. There are several elements
    ///     in this data and this structure contains those elements.
    /// </remarks>
    public class MakeCredentialData
    {
        private const int KeyFormat = 1;
        private const int KeyAuthData = 2;
        private const int KeyAttestationStatement = 3;
        private const int KeyEnterpriseAttestation = 4;
        private const int KeyLargeBlob = 5;

        private const int MaxAttestationMapCount = 3;

        private const string PackedString = "packed";
        private const string AlgString = "alg";
        private const string SigString = "sig";
        private const string X5cString = "x5c";

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private MakeCredentialData()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Build a new instance of <see cref="MakeCredentialData" /> based on the
        ///     given CBOR encoding.
        /// </summary>
        /// <remarks>
        ///     The encoding must follow the definition of
        ///     <c>authenticatorMakeCredential response structure</c> in section
        ///     6.1.2 of the CTAP 2.1 standard.
        /// </remarks>
        /// <param name="cborEncoding">
        ///     The credential data, encoded following the CTAP 2.1 and CBOR (RFC
        ///     8949) standards.
        /// </param>
        /// <exception cref="Ctap2DataException">
        ///     The <c>cborEncoding</c> is not a valid CBOR encoding, or it is not a
        ///     correct encoding for FIDO2 credential data.
        /// </exception>
        public MakeCredentialData(ReadOnlyMemory<byte> cborEncoding)
        {
            try
            {
                var map = new CborMap<int>(cborEncoding);

                Format = map.ReadTextString(KeyFormat);
                AuthenticatorData = new AuthenticatorData(map.ReadByteString(KeyAuthData));
                if (!(AuthenticatorData.CredentialPublicKey is CoseEcPublicKey)
                    || AuthenticatorData.CredentialPublicKey.Type != CoseKeyType.Ec2
                    || !map.Contains(KeyAttestationStatement)
                    || !ReadAttestation(map))
                {
                    throw new Ctap2DataException(ExceptionMessages.Ctap2UnknownAttestationFormat);
                }

                if (map.Contains(KeyEnterpriseAttestation))
                {
                    EnterpriseAttestation = map.ReadBoolean(KeyEnterpriseAttestation);
                }

                if (map.Contains(KeyLargeBlob))
                {
                    LargeBlobKey = map.ReadByteString(KeyLargeBlob);
                }
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
        ///     The attestation statement format identifier.
        /// </summary>
        public string Format { get; }

        /// <summary>
        ///     The object that contains both the encoded authenticator data, which
        ///     is to be used in verifying the attestation statement, and the decoded
        ///     elements, including the credential itself, a public key.
        /// </summary>
        /// <remarks>
        ///     Save the public key in this object and use it to verify assertions
        ///     returned by calling <c>GetAssertion</c>.
        /// </remarks>
        public AuthenticatorData AuthenticatorData { get; }

        /// <summary>
        ///     The list of extensions. This is an optional value and can be null.
        /// </summary>
        /// <remarks>
        ///     Each extension is a key/value pair. All keys are strings, but each
        ///     extension has its own definition of a value. It could be an int, or
        ///     it could be a map containing a string and a boolean,. It is the
        ///     caller's responsibility to decode the value.
        ///     <para>
        ///         For each value, the standard (or the vendor in the case of
        ///         vendor-defined extensions) will define the structure of the value.
        ///         From that structure the value can be decoded following CBOR rules.
        ///         The encoded value is what is stored in this dictionary.
        ///     </para>
        /// </remarks>
        public IReadOnlyDictionary<string, byte[]>? Extensions { get; }

        /// <summary>
        ///     The algorithm used to create the attestation statement.
        /// </summary>
        public CoseAlgorithmIdentifier AttestationAlgorithm { get; private set; }

        /// <summary>
        ///     The signature that is the attestation statement, which can be used to
        ///     verify that the public key credential was generated by the YubiKey.
        ///     This is an optional element so it can be null.
        /// </summary>
        /// <remarks>
        ///     Use the public key in the zero'th element of the
        ///     <see cref="AttestationCertificates" /> to verify this signature. If no
        ///     attestation certificate is provided, the authenticator assumes the
        ///     entity that must verify the signature will have access to the
        ///     appropriate cert.
        ///     <para>
        ///         The data to verify is the <see cref="AuthenticatorData" />
        ///         concatenated with the client data hash (from the
        ///         <see cref="MakeCredentialParameters" />).
        ///     </para>
        /// </remarks>
        public ReadOnlyMemory<byte> AttestationStatement { get; private set; }

        /// <summary>
        ///     The encoded CBOR map that describes the attestation statement.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The other members of this class make it easy to access the individual
        ///         elements of the attestation statement and supporting structures. This
        ///         property returns the raw, CBOR encoded attestation statement returned
        ///         by the YubiKey. This is useful if you are implementing or interoperating
        ///         with the WebAuthn data types. It is often easier to copy this field
        ///         over in its encoded form rather than using the parsed properties.
        ///     </para>
        ///     <para>
        ///         For example: the WebAuthn MakeCredential operation expects an "attestation
        ///         object" be returned. This is a CBOR map containing the "format", "attStmt",
        ///         and "authData" - the keys given in string form. The "authData" is the CBOR
        ///         encoded <see cref="AuthenticatorData" /> further encoded in Base64URL. The
        ///         "attStmt" is the CBOR map that contains the <see cref="AttestationAlgorithm" />,
        ///         <see cref="AttestationStatement" />, and <see cref="AttestationCertificates" />.
        ///     </para>
        ///     <para>
        ///         Rather than reconstructing the CBOR map, we provide it here for you, already
        ///         in encoded form.
        ///     </para>
        /// </remarks>
        public ReadOnlyMemory<byte> EncodedAttestationStatement { get; private set; }

        /// <summary>
        ///     This array contains the certificate for the public key that can be
        ///     used to verify that the attestation statement, and possibly CA
        ///     certificates that chain to a root. This is an optional element so it
        ///     can be null.
        /// </summary>
        /// <remarks>
        ///     The first cert in this list (<c>AttestationCertificates[0]</c>) will
        ///     be the certificate that contains the public key that will verify the
        ///     <see cref="AttestationStatement" />. The data to verify is the
        ///     <see cref="AuthenticatorData" /> concatenated with the client data
        ///     hash (from the <see cref="MakeCredentialParameters" />).
        /// </remarks>
        public IReadOnlyList<X509Certificate2>? AttestationCertificates { get; private set; }

        /// <summary>
        ///     Indicates whether an enterprise attestation was returned. This is an
        ///     optional value, so if the YubiKey did not return this element, the
        ///     property will be null.
        /// </summary>
        /// <remarks>
        ///     If there is no enterprise attestation entry in the response (this
        ///     property is null), or if there was (this property is not null) and it
        ///     is <c>false</c>, then there was no enterprise attestation statement
        ///     returned. If there was an entry (this property is not null) and the
        ///     value is <c>true</c>, then there was an enterprise attestation
        ///     statement returned.
        /// </remarks>
        public bool? EnterpriseAttestation { get; private set; }

        /// <summary>
        ///     If this is not null, it is the large blob key (see section 12.3 of
        ///     the CTAP2 standard). This is an optional element so it can be null.
        /// </summary>
        public ReadOnlyMemory<byte>? LargeBlobKey { get; private set; }

        // We're expecting a Format of "packed", which means the data in the
        // key/value pair for KeyAttestationStatement is a map with 2 or 3
        // elements:
        //    "alg"/-7
        //    "sig"/byte array
        //       and possibly
        //    "x5c"/array of certs.
        // The byte array is the DER encoding of the ECDSA signature.
        // If everything works, return true. Otherwise, return false.
        private bool ReadAttestation(CborMap<int> map)
        {
            CborMap<string> attest = map.ReadMap<string>(KeyAttestationStatement);
            EncodedAttestationStatement = attest.Encoded;
            if (!Format.Equals(PackedString, StringComparison.Ordinal)
                || !attest.Contains(AlgString) || !attest.Contains(SigString)
                || attest.Count > MaxAttestationMapCount
                || attest.Count == MaxAttestationMapCount && !attest.Contains(X5cString))
            {
                return false;
            }

            AttestationAlgorithm = (CoseAlgorithmIdentifier)attest.ReadInt32(AlgString);
            AttestationStatement = attest.ReadByteString(SigString);

            if (attest.Contains(X5cString))
            {
                IReadOnlyList<byte[]> certList = attest.ReadArray<byte[]>(X5cString);
                var attestationCertificates = new List<X509Certificate2>(certList.Count);

                for (int index = 0; index < certList.Count; index++)
                {
                    attestationCertificates.Add(new X509Certificate2(certList[index]));
                }

                AttestationCertificates = attestationCertificates;
            }

            return true;
        }

        /// <summary>
        ///     Use the zero'th public key in the
        ///     <see cref="AttestationCertificates" /> list to verify the
        ///     <c>AuthenticatorData</c> and client data hash using the signature
        ///     that is the <see cref="AttestationStatement" />.
        /// </summary>
        /// <remarks>
        ///     If the signature verifies, this method will return <c>true</c>, and
        ///     if it does not verify, it will return <c>false</c>. If there are no
        ///     certificates in the list, this method will throw an exception.
        /// </remarks>
        /// <param name="clientDataHash">
        ///     The client data hash sent to the YubiKey to make the credential.
        /// </param>
        /// <returns>
        ///     A boolean, <c>true</c> if the attestation statement (the signature)
        ///     verifies, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     There is no cert in the attestation certificate list.
        /// </exception>
        public bool VerifyAttestation(ReadOnlyMemory<byte> clientDataHash)
        {
            if (AttestationCertificates is null || AttestationCertificates.Count == 0)
            {
                throw new InvalidOperationException(ExceptionMessages.MissingCtap2Data);
            }

            using SHA256 digester = CryptographyProviders.Sha256Creator();
            _ = digester.TransformBlock(
                AuthenticatorData.EncodedAuthenticatorData.ToArray(), inputOffset: 0,
                AuthenticatorData.EncodedAuthenticatorData.Length, outputBuffer: null, outputOffset: 0);

            _ = digester.TransformFinalBlock(clientDataHash.ToArray(), inputOffset: 0, clientDataHash.Length);

            using var ecdsaVfy = new EcdsaVerify(AttestationCertificates[0]);
            return ecdsaVfy.VerifyDigestedData(digester.Hash, AttestationStatement.ToArray());
        }
    }
}
