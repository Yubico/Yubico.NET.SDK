// Copyright 2025 Yubico AB
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
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Contains the data returned by the YubiKey after making a credential.
    /// </summary>
    /// <remarks>
    /// This includes the CTAP attestation object and CTAP-specific optional
    /// response fields such as enterprise attestation, large blob key, and
    /// unsigned extension outputs.
    /// </remarks>
    public class MakeCredentialData
    {
        private const int KeyFormat = 1;
        private const int KeyAuthData = 2;
        private const int KeyAttestationStatement = 3;
        private const int KeyEnterpriseAttestation = 4;
        private const int KeyLargeBlob = 5;
        private const int KeyUnsignedExtensionOutputs = 6;

        /// <summary>
        /// The parsed attestation object containing the format, authenticator data,
        /// and attestation statement.
        /// </summary>
        public AttestationObject AttestationObject { get; }

        /// <summary>
        /// The attestation statement format identifier.
        /// See <see cref="AttestationFormats"/> for standard format identifiers.
        /// </summary>
        public string Format { get; }

        /// <summary>
        /// The object that contains both the encoded authenticator data, which
        /// is to be used in verifying the attestation statement, and the decoded
        /// elements, including the credential itself, a public key.
        /// </summary>
        /// <remarks>
        /// Save the public key in this object and use it to verify assertions
        /// returned by calling <c>GetAssertion</c>.
        /// </remarks>
        public AuthenticatorData AuthenticatorData { get; }

        /// <summary>
        /// The algorithm used to create the attestation statement.
        /// </summary>
        public CoseAlgorithmIdentifier AttestationAlgorithm { get; private set; }

        /// <summary>
        /// The attestation signature bytes from the parsed attestation statement's
        /// <c>sig</c> field.
        /// </summary>
        /// <remarks>
        /// This property is populated for attestation statement formats parsed by
        /// this SDK. Use <see cref="EncodedAttestationStatement"/> when you need
        /// the full CBOR-encoded attestation statement map.
        /// <para>
        /// For packed attestation statements, the data to verify is the
        /// <see cref="AuthenticatorData"/> concatenated with the client data
        /// hash (from the <see cref="MakeCredentialParameters"/>).
        /// </para>
        /// </remarks>
        public ReadOnlyMemory<byte> AttestationSignature { get; private set; }

        /// <summary>
        /// The attestation signature bytes from the parsed attestation statement's
        /// <c>sig</c> field.
        /// </summary>
        /// <remarks>
        /// This property is retained for source compatibility. Despite its name,
        /// it does not contain the full attestation statement. Use
        /// <see cref="AttestationSignature"/> for the signature bytes, or
        /// <see cref="EncodedAttestationStatement"/> for the full CBOR-encoded
        /// attestation statement map.
        /// </remarks>
        [Obsolete("Use AttestationSignature for the signature bytes, or EncodedAttestationStatement for the full attestation statement.", false)]
        public ReadOnlyMemory<byte> AttestationStatement => AttestationSignature;

        /// <summary>
        /// The raw CBOR-encoded full attestation statement map from the
        /// <c>attStmt</c> field.
        /// </summary>
        /// <remarks>
        /// This contains the complete format-specific attestation statement, not
        /// only the <c>sig</c> field. Use <see cref="AttestationSignature"/> when
        /// only the signature bytes are needed.
        /// </remarks>
        public ReadOnlyMemory<byte> EncodedAttestationStatement { get; private set; }

        /// <summary>
        /// This list contains the certificates from the attestation statement's
        /// x5c field. This is an optional element so it can be null.
        /// </summary>
        /// <remarks>
        /// The first cert in this list (<c>AttestationCertificates[0]</c>) will
        /// be the certificate that contains the public key used to verify the
        /// <see cref="AttestationSignature"/>. For packed attestation statements,
        /// the data to verify is the <see cref="AuthenticatorData"/> concatenated
        /// with the client data hash (from the <see cref="MakeCredentialParameters"/>).
        /// </remarks>
        public IReadOnlyList<X509Certificate2>? AttestationCertificates { get; private set; }

        /// <summary>
        /// Indicates whether an enterprise attestation was returned.
        /// </summary>
        /// <remarks>
        /// A value of <c>true</c> means enterprise attestation was returned.
        /// A value of <c>false</c> or <c>null</c> means enterprise attestation
        /// was not returned. The value is <c>null</c> when the response omits
        /// the optional enterprise attestation field.
        /// </remarks>
        public bool? EnterpriseAttestation { get; private set; }

        /// <summary>
        /// If this is not null, it is the large blob key (see section 12.3 of
        /// the CTAP2 standard). This is an optional element so it can be null.
        /// </summary>
        public ReadOnlyMemory<byte>? LargeBlobKey { get; private set; }

        /// <summary>
        /// Gets the unsigned extension outputs returned by the authenticator, if any.
        /// </summary>
        /// <remarks>
        /// This dictionary contains extension outputs that are not included in the
        /// signed authenticator data.
        /// </remarks>
        public IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? UnsignedExtensionOutputs { get; private set; }

        /// <summary>
        /// The raw CBOR-encoded MakeCredential response from the YubiKey.
        /// </summary>
        public ReadOnlyMemory<byte> RawData { get; }

        private MakeCredentialData()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a new instance of <see cref="MakeCredentialData"/> based on the
        /// given CBOR encoding.
        /// </summary>
        /// <remarks>
        /// The encoding must follow the CTAP
        /// <see href="https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#authenticatormakecredential-response-structure">authenticatorMakeCredential response structure</see>.
        /// </remarks>
        /// <param name="cborEncoding">
        /// The credential data, encoded following the CTAP and CBOR (RFC
        /// 8949) standards.
        /// </param>
        /// <exception cref="Ctap2DataException">
        /// The <c>cborEncoding</c> is not a valid CBOR encoding, or it is not a
        /// correct encoding for FIDO2 credential data.
        /// </exception>
        public MakeCredentialData(ReadOnlyMemory<byte> cborEncoding)
        {
            RawData = cborEncoding;
            var map = new CborMap<int>(RawData);

            try
            {
                AttestationObject = ReadAttestationObject(map);

                Format = AttestationObject.Format;
                AuthenticatorData = AttestationObject.AuthenticatorData;
                var packedStatement = AttestationObject.Statement as PackedAttestationStatement ??
                    throw new Ctap2DataException(ExceptionMessages.Ctap2MissingRequiredField);
                AttestationAlgorithm = packedStatement.Algorithm;
                AttestationSignature = packedStatement.Signature;
                EncodedAttestationStatement = AttestationObject.EncodedAttestationStatement;
                AttestationCertificates = packedStatement.Certificates;

                if (map.Contains(KeyEnterpriseAttestation))
                {
                    EnterpriseAttestation = map.ReadBoolean(KeyEnterpriseAttestation);
                }

                if (map.Contains(KeyLargeBlob))
                {
                    LargeBlobKey = map.ReadByteString(KeyLargeBlob);
                }

                if (map.Contains(KeyUnsignedExtensionOutputs))
                {
                    UnsignedExtensionOutputs = ParseUnsignedExtensionOutputs(
                        map.ReadEncodedValue(KeyUnsignedExtensionOutputs));
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

        private static IReadOnlyDictionary<string, ReadOnlyMemory<byte>> ParseUnsignedExtensionOutputs(
            ReadOnlyMemory<byte> encodedMap)
        {
            var cborMap = new CborMap<string>(encodedMap);
            var result = new Dictionary<string, ReadOnlyMemory<byte>>(cborMap.Count, StringComparer.Ordinal);
            foreach (string extensionKey in cborMap.Keys)
            {
                result[extensionKey] = cborMap.ReadEncodedValue(extensionKey);
            }

            return result;
        }

        private static AttestationObject ReadAttestationObject(CborMap<int> fullResponse)
        {
            foreach (int requiredKey in new[] { KeyFormat, KeyAuthData, KeyAttestationStatement })
            {
                if (!fullResponse.Contains(requiredKey))
                {
                    throw new Ctap2DataException(ExceptionMessages.Ctap2MissingRequiredField);
                }
            }

            return new AttestationObject(
                fullResponse.ReadTextString(KeyFormat),
                fullResponse.ReadByteString(KeyAuthData),
                fullResponse.ReadEncodedValue(KeyAttestationStatement));
        }

        /// <summary>
        /// Use the zero'th public key in the
        /// <see cref="AttestationCertificates"/> list to verify the
        /// packed attestation statement signature over the
        /// <c>AuthenticatorData</c> and client data hash.
        /// </summary>
        /// <remarks>
        /// This verifies only the correctness of the attestation signature. It
        /// does not establish whether the attestation certificate is trusted.
        /// Trust validation requires application-provided trust roots and
        /// certificate path validation, which this method does not perform.
        /// Applications should perform certificate path validation of
        /// <see cref="AttestationCertificates"/> externally.
        /// If there are no certificates in the list, this method will throw an
        /// exception.
        /// </remarks>
        /// <param name="clientDataHash">
        /// The client data hash sent to the YubiKey to make the credential.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the attestation statement signature is
        /// correct, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// There is no cert in the attestation certificate list.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The attestation algorithm is not ES256, or the attestation certificate
        /// does not contain an ECDSA public key.
        /// </exception>
        public bool VerifyAttestation(ReadOnlyMemory<byte> clientDataHash)
        {
            if (AttestationAlgorithm != CoseAlgorithmIdentifier.ES256)
            {
                throw new NotSupportedException(
                    "Only packed ES256 attestation verification is supported.");
            }

            if (AttestationCertificates is null || AttestationCertificates.Count == 0)
            {
                throw new InvalidOperationException(ExceptionMessages.MissingCtap2Data);
            }

            using var digester = CryptographyProviders.Sha256Creator();
            _ = digester.TransformBlock(
                AuthenticatorData.EncodedAuthenticatorData.ToArray(), 0,
                AuthenticatorData.EncodedAuthenticatorData.Length, null, 0);
            _ = digester.TransformFinalBlock(clientDataHash.ToArray(), 0, clientDataHash.Length);

            var attestationPublicKey = AttestationCertificates[0].GetECDsaPublicKey() ??
                throw new NotSupportedException(
                    "Only ECDSA attestation certificates are supported.");

            using var ecdsaVfy = new EcdsaVerify(attestationPublicKey);
            return ecdsaVfy.VerifyDigestedData(digester.Hash, AttestationSignature.ToArray());
        }
    }
}
