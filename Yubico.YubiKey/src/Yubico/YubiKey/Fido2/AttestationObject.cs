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
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Represents a FIDO2 attestation object, which contains attestation format,
    /// authenticator data, and attestation statement.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An attestation object is returned by the authenticator during credential creation
    /// (MakeCredential). It contains the attestation format identifier, authenticator data,
    /// and an attestation statement that can be used to verify the authenticity of the
    /// credential creation process.
    /// </para>
    /// <para>
    /// The CBOR structure is defined in CTAP 2.1 section 6.1.2 as:
    /// <code>
    ///    map {
    ///      1: fmt         (text string)  // Attestation statement format identifier
    ///      2: authData    (byte string)  // Authenticator data
    ///      3: attStmt     (map)          // Attestation statement (format-specific)
    ///    }
    /// </code>
    /// </para>
    /// <para>
    /// This class supports both full parsing with format-specific validation (for known
    /// formats like "packed") and structure-only parsing (for custom or unknown formats).
    /// </para>
    /// </remarks>
    public class AttestationObject : ICborEncode
    {
        private const int KeyFormat = 1;
        private const int KeyAuthData = 2;
        private const int KeyAttestationStatement = 3;

        private const int MaxAttestationMapCount = 3;
        private const string AlgString = "alg";
        private const string SigString = "sig";
        private const string X5cString = "x5c";

        /// <summary>
        /// The attestation statement format identifier (e.g., "packed", "tpm", "android-key").
        /// See <see cref="AttestationFormats"/> for standard format identifiers.
        /// </summary>
        public string Format { get; private set; } = string.Empty;

        /// <summary>
        /// The authenticator data, which includes the relying party ID hash, flags,
        /// signature counter, and optionally the attested credential data.
        /// </summary>
        public AuthenticatorData AuthenticatorData { get; private set; } = null!;

        /// <summary>
        /// The algorithm used to create the attestation statement.
        /// This is null when parseAttestationStatement is false or the format is unknown.
        /// </summary>
        public CoseAlgorithmIdentifier? AttestationAlgorithm { get; private set; }

        /// <summary>
        /// The attestation signature bytes.
        /// This is null when parseAttestationStatement is false or the format is unknown.
        /// </summary>
        public ReadOnlyMemory<byte>? AttestationStatement { get; private set; }

        /// <summary>
        /// The list of X.509 certificates from the attestation statement's x5c field.
        /// This is null when certificates are not present or when parseAttestationStatement is false.
        /// The first certificate contains the public key that verifies the attestation signature.
        /// </summary>
        public IReadOnlyList<X509Certificate2>? AttestationCertificates { get; private set; }

        /// <summary>
        /// The raw CBOR encoding of the attestation statement (key 3 in the attestation object).
        /// This is always available regardless of the parseAttestationStatement flag.
        /// </summary>
        public ReadOnlyMemory<byte> EncodedAttestationStatement { get; private set; }

        /// <summary>
        /// The raw CBOR encoding of the entire attestation object.
        /// This is available when constructed from encoded bytes.
        /// </summary>
        public ReadOnlyMemory<byte> Encoded { get; private set; }

        /// <summary>
        /// Constructs a new instance of <see cref="AttestationObject"/> from CBOR-encoded bytes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This constructor decodes an attestation object from CBOR encoding following
        /// the CTAP 2.1 specification (section 6.1.2).
        /// </para>
        /// <para>
        /// The parseAttestationStatement parameter controls the level of parsing:
        /// </para>
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// true (default): Performs full parsing with format-specific validation.
        /// Currently supports the "packed" format. For "packed" format, this extracts
        /// the algorithm, signature, and optional x5c certificates. Unknown formats
        /// will cause an exception.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// false: Performs structure-only parsing without format-specific validation.
        /// This is useful for custom or unknown attestation formats where you only need
        /// access to the raw attestation statement bytes and authenticator data.
        /// </description>
        /// </item>
        /// </list>
        /// </remarks>
        /// <param name="cborEncoding">
        /// The CBOR encoding of the attestation object.
        /// </param>
        /// <param name="bytesRead">
        /// Returns the number of bytes read from the encoding.
        /// </param>
        /// <param name="parseAttestationStatement">
        /// If true, performs full parsing with format-specific validation.
        /// If false, performs structure-only parsing and stores raw bytes.
        /// Default is true.
        /// </param>
        /// <exception cref="Ctap2DataException">
        /// The cborEncoding is not a valid attestation object, or parseAttestationStatement
        /// is true and the attestation format is unknown or invalid.
        /// </exception>
        public AttestationObject(ReadOnlyMemory<byte> cborEncoding, out int bytesRead, bool parseAttestationStatement = true)
        {
            try
            {
                Encoded = cborEncoding;
                var map = new CborMap<int>(cborEncoding);
                bytesRead = map.BytesRead;

                Format = map.ReadTextString(KeyFormat);
                AuthenticatorData = new AuthenticatorData(map.ReadByteString(KeyAuthData));

                if (map.Contains(KeyAttestationStatement))
                {
                    var attestCborMap = map.ReadMap<string>(KeyAttestationStatement);
                    EncodedAttestationStatement = attestCborMap.Encoded;

                    if (parseAttestationStatement)
                    {
                        if (!ParsePackedAttestationStatement(attestCborMap))
                        {
                            throw new Ctap2DataException(ExceptionMessages.Ctap2UnknownAttestationFormat);
                        }
                    }
                }
            }
            catch (CborContentException cborException)
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info, cborException);
            }
        }

        /// <summary>
        /// Parses the "packed" attestation statement format.
        /// </summary>
        /// <remarks>
        /// The "packed" format is defined in the WebAuthn specification and contains:
        /// <list type="bullet">
        /// <item><description>alg: The COSE algorithm identifier</description></item>
        /// <item><description>sig: The signature bytes</description></item>
        /// <item><description>x5c: Optional array of X.509 certificates</description></item>
        /// </list>
        /// </remarks>
        /// <param name="attestCborMap">
        /// The CBOR map containing the attestation statement.
        /// </param>
        /// <returns>
        /// True if the attestation statement was successfully parsed, false otherwise.
        /// </returns>
        private bool ParsePackedAttestationStatement(CborMap<string> attestCborMap)
        {
            // The attestation statement must be in the "packed" format and contain the expected keys.
            if (!Format.Equals(AttestationFormats.Packed, StringComparison.Ordinal) ||
                !attestCborMap.Contains(AlgString) ||
                !attestCborMap.Contains(SigString) ||
                attestCborMap.Count > MaxAttestationMapCount ||
                (attestCborMap.Count == MaxAttestationMapCount && !attestCborMap.Contains(X5cString)))
            {
                return false;
            }

            AttestationAlgorithm = (CoseAlgorithmIdentifier)attestCborMap.ReadInt32(AlgString);
            AttestationStatement = attestCborMap.ReadByteString(SigString);

            if (attestCborMap.Contains(X5cString))
            {
                var certList = attestCborMap.ReadArray<byte[]>(X5cString);
                var attestationCertificates = new List<X509Certificate2>(certList.Count);

                for (int index = 0; index < certList.Count; index++)
                {
                    attestationCertificates.Add(new X509Certificate2(certList[index]));
                }

                AttestationCertificates = attestationCertificates;
            }

            return true;
        }

        /// <inheritdoc/>
        public byte[] CborEncode()
        {
            if (string.IsNullOrEmpty(Format) || AuthenticatorData == null || EncodedAttestationStatement.IsEmpty)
            {
                throw new InvalidOperationException(
                    "AttestationObject must have Format, AuthenticatorData, and EncodedAttestationStatement set before encoding. " +
                    "Use the decoding constructor to parse a CBOR-encoded attestation object, " +
                    "or set all required properties if building programmatically.");
            }

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);

            cbor.WriteStartMap(null);

            // Key 1: Format
            cbor.WriteInt32(KeyFormat);
            cbor.WriteTextString(Format);

            // Key 2: AuthenticatorData
            cbor.WriteInt32(KeyAuthData);
            cbor.WriteByteString(AuthenticatorData.EncodedAuthenticatorData.Span);

            // Key 3: Attestation Statement
            cbor.WriteInt32(KeyAttestationStatement);
            cbor.WriteEncodedValue(EncodedAttestationStatement.Span);

            cbor.WriteEndMap();

            return cbor.Encode();
        }
    }
}
