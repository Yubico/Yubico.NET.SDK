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
using System.Formats.Cbor;
using Yubico.YubiKey.Fido2.Cbor;

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
    /// authenticator data, and a typed attestation statement.
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
        /// The format-specific attestation statement.
        /// </summary>
        public AttestationStatement Statement { get; private set; } = null!;

        /// <summary>
        /// The raw CBOR encoding of the full attestation statement map
        /// (key 3 in the attestation object).
        /// This is always available after decoding an attestation object.
        /// </summary>
        public ReadOnlyMemory<byte> EncodedAttestationStatement => Statement.Encoded;

        /// <summary>
        /// The raw CBOR encoding of the entire attestation object.
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
        /// The <see cref="Statement"/> property is populated with a format-specific
        /// statement type for statement formats parsed by this SDK. Unknown or
        /// malformed formats are represented by <see cref="UnknownAttestationStatement"/>,
        /// preserving the raw bytes via <see cref="EncodedAttestationStatement"/>.
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
                (string format, ReadOnlyMemory<byte> authenticatorData, ReadOnlyMemory<byte> encodedAttStmt) =
                    DecodeRequiredFields(cborEncoding, out bytesRead);

                Initialize(format, authenticatorData, encodedAttStmt);
                Encoded = cborEncoding[..bytesRead];
            }
            catch (CborContentException cborException)
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info, cborException);
            }
            catch (InvalidCastException invalidCast)
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info, invalidCast);
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

        internal AttestationObject(
            string format,
            ReadOnlyMemory<byte> authenticatorData,
            ReadOnlyMemory<byte> encodedAttestationStatement)
        {
            try
            {
                Initialize(format, authenticatorData, encodedAttestationStatement);
                Encoded = CborEncode();
            }
            catch (CborContentException cborException)
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info, cborException);
            }
            catch (InvalidCastException invalidCast)
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info, invalidCast);
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

        private static (string format, ReadOnlyMemory<byte> authenticatorData, ReadOnlyMemory<byte> encodedAttStmt) DecodeRequiredFields(
            ReadOnlyMemory<byte> cborEncoding,
            out int bytesRead)
        {
            var map = new CborMap<int>(cborEncoding);
            bytesRead = map.BytesRead;

            return (
                map.Contains(KeyFormat)
                    ? map.ReadTextString(KeyFormat)
                    : throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info),
                map.Contains(KeyAuthData)
                    ? map.ReadByteString(KeyAuthData)
                    : throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info),
                map.Contains(KeyAttestationStatement)
                    ? map.ReadEncodedValue(KeyAttestationStatement)
                    : ReadOnlyMemory<byte>.Empty
            );
        }

        private void Initialize(
            string format,
            ReadOnlyMemory<byte> authenticatorData,
            ReadOnlyMemory<byte> encodedAttestationStatement)
        {
            Format = format;
            AuthenticatorData = new AuthenticatorData(authenticatorData);
            Statement = AttestationStatement.FromCbor(format, encodedAttestationStatement);
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
