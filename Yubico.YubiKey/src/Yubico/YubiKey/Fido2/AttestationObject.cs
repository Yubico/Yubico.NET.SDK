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
    /// (MakeCredential). It contains the attestation format identifier,
    /// authenticator data, and the attestation statement for checking the
    /// attestation signature over the credential creation data.
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
        /// This is null for attestation statement formats not parsed by this SDK.
        /// </summary>
        public CoseAlgorithmIdentifier? AttestationAlgorithm { get; private set; }

        /// <summary>
        /// The attestation signature bytes.
        /// This is null for attestation statement formats not parsed by this SDK.
        /// </summary>
        public ReadOnlyMemory<byte>? AttestationStatement { get; private set; }

        /// <summary>
        /// The list of X.509 certificates from the attestation statement's x5c field.
        /// This is null when certificates are not present or the attestation
        /// statement format is not parsed by this SDK.
        /// The first certificate contains the public key that verifies the attestation signature.
        /// </summary>
        public IReadOnlyList<X509Certificate2>? AttestationCertificates { get; private set; }

        /// <summary>
        /// The raw CBOR encoding of the attestation statement (key 3 in the attestation object).
        /// This is always available after decoding an attestation object.
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
        /// Use this overload when you do not need to know how many bytes were consumed.
        /// For sequential parsing of multiple CBOR objects from a shared buffer, use the overload
        /// with <c>out int bytesRead</c>.
        /// </remarks>
        /// <param name="cborEncoding">The CBOR encoding of the attestation object.</param>
        /// <exception cref="Ctap2DataException">
        /// The cborEncoding is not a well-formed attestation object.
        /// </exception>
        public AttestationObject(ReadOnlyMemory<byte> cborEncoding)
            : this(cborEncoding, out _) { }

        /// <summary>
        /// Constructs a new instance of <see cref="AttestationObject"/> from CBOR-encoded bytes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This constructor decodes an attestation object from CBOR encoding following
        /// the CTAP 2.1 specification (section 6.1.2).
        /// </para>
        /// <para>
        /// For "packed" attestation format, the typed properties
        /// (<see cref="AttestationAlgorithm"/>, <see cref="AttestationStatement"/>,
        /// <see cref="AttestationCertificates"/>) are populated. For all other formats,
        /// those properties remain null and the raw bytes are available via
        /// <see cref="EncodedAttestationStatement"/>.
        /// </para>
        /// </remarks>
        /// <param name="cborEncoding">
        /// The CBOR encoding of the attestation object.
        /// </param>
        /// <param name="bytesRead">
        /// Returns the number of bytes read from the encoding.
        /// </param>
        /// <exception cref="Ctap2DataException">
        /// The cborEncoding is not a well-formed attestation object.
        /// </exception>
        public AttestationObject(ReadOnlyMemory<byte> cborEncoding, out int bytesRead)
        {
            try
            {
                (string format, byte[] authDataBytes, ReadOnlyMemory<byte> encodedAttStmt) =
                    DecodeRequiredFields(cborEncoding, out bytesRead);
                Encoded = cborEncoding[..bytesRead];

                Format = format;
                AuthenticatorData = new AuthenticatorData(authDataBytes);

                if (!encodedAttStmt.IsEmpty)
                {
                    EncodedAttestationStatement = encodedAttStmt;

                    if (Format.Equals(AttestationFormats.Packed, StringComparison.Ordinal))
                    {
                        var attestCborMap = new CborMap<string>(EncodedAttestationStatement);
                        _ = TryParsePackedAttestationStatement(attestCborMap);
                    }
                }
            }
            catch (CborContentException cborException)
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info, cborException);
            }
            catch (InvalidOperationException invalidOp)
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info, invalidOp);
            }
            catch (FormatException formatEx)
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info, formatEx);
            }
        }

        private static (string format, byte[] authDataBytes, ReadOnlyMemory<byte> encodedAttStmt) DecodeRequiredFields(
            ReadOnlyMemory<byte> cborEncoding,
            out int bytesRead)
        {
            var reader = new CborReader(cborEncoding, CborConformanceMode.Ctap2Canonical);
            int? count = reader.ReadStartMap();
            int remaining = count ?? int.MaxValue;

            string? format = null;
            byte[]? authDataBytes = null;
            var encodedAttStmt = ReadOnlyMemory<byte>.Empty;

            for (int i = 0; i < remaining; i++)
            {
                if (reader.PeekState() == CborReaderState.EndMap)
                {
                    break;
                }

                int key = (int)reader.ReadInt64();
                if (key == KeyFormat)
                {
                    format = reader.ReadTextString();
                }
                else if (key == KeyAuthData)
                {
                    authDataBytes = reader.ReadByteString();
                }
                else if (key == KeyAttestationStatement)
                {
                    encodedAttStmt = reader.ReadEncodedValue().ToArray();
                }
                else
                {
                    reader.SkipValue();
                }
            }

            reader.ReadEndMap();
            bytesRead = cborEncoding.Length - reader.BytesRemaining;

            return (
                format ?? throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info),
                authDataBytes ?? throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info),
                encodedAttStmt);
        }

        private bool TryParsePackedAttestationStatement(CborMap<string> attestCborMap)
        {
            try
            {
                if (!attestCborMap.Contains(AlgString) ||
                    !attestCborMap.Contains(SigString) ||
                    attestCborMap.Count > MaxAttestationMapCount ||
                    (attestCborMap.Count == MaxAttestationMapCount && !attestCborMap.Contains(X5cString)))
                {
                    return false;
                }

                CoseAlgorithmIdentifier attestationAlgorithm =
                    (CoseAlgorithmIdentifier)attestCborMap.ReadInt32(AlgString);
                ReadOnlyMemory<byte> attestationStatement = attestCborMap.ReadByteString(SigString);
                IReadOnlyList<X509Certificate2>? attestationCertificates = null;

                if (attestCborMap.Contains(X5cString))
                {
                    var certList = attestCborMap.ReadArray<byte[]>(X5cString);
                    var certificateList = new List<X509Certificate2>(certList.Count);

                    for (int index = 0; index < certList.Count; index++)
                    {
                        certificateList.Add(new X509Certificate2(certList[index]));
                    }

                    attestationCertificates = certificateList;
                }

                AttestationAlgorithm = attestationAlgorithm;
                AttestationStatement = attestationStatement;
                AttestationCertificates = attestationCertificates;
                return true;
            }
            catch (Exception exception) when (
                exception is CborContentException ||
                exception is InvalidCastException ||
                exception is InvalidOperationException ||
                exception is FormatException ||
                exception is System.Security.Cryptography.CryptographicException)
            {
                AttestationAlgorithm = null;
                AttestationStatement = null;
                AttestationCertificates = null;
                return false;
            }
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
