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
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// A FIDO2 credential type consisting of type and algorithm.
    /// </summary>
    /// <remarks>
    /// A credential type specifies what sort of credential is supported or to
    /// be constructed.
    /// <para>
    /// The FIDO2 standard defines a credential type as
    /// <c>PublicKeyCredentialParemeters</c>, which is a "dictionary" consisting
    /// of a <c>type</c> and an <c>alg</c>.
    /// </para>
    /// <para>
    /// Currently only one <c>type</c> is supported: the string "public-key".
    /// However, the standard also allows authenticators to support non-standard
    /// values.
    /// </para>
    /// <para>
    /// The <c>alg</c> is a <c>COSEAlgorithmIdentifier</c>, which is an integer
    /// from the list of values specified in the IANA standard. For example, -7
    /// is the integer for ECDSA with SHA-256.
    /// </para>
    /// <para>
    /// This class holds the type and algorithm, and can encode and decode them
    /// as part of CBOR structures.
    /// </para>
    /// </remarks>
    public class CredentialType : ICborEncode
    {
        private const string TagType = "type";
        private const string TagAlg = "alg";

        /// <summary>
        /// The <c>type</c> component of the credential type.
        /// </summary>
        /// <remarks>
        /// Upon construction, this property will be set to "public-key".
        /// <para>
        /// Currently, the only type specified is the string "public-key". If you
        /// do not want to use any other value, do not set this property.
        /// </para>
        /// <para>
        /// However, the standard also allows authenticators to support
        /// non-standard values. That is, an authenticator must support the
        /// standard type (and may choose to support only the standard type), but
        /// is also allowed to support non-standard types.
        /// </para>
        /// <para>
        /// While using a non-standard value will likely yield an error from the
        /// YubiKey, this class will follow the standard and allow for
        /// non-standard types.
        /// </para>
        /// </remarks>
        public string Type { get; set; }

        /// <summary>
        /// The <c>alg</c> component of the credential type.
        /// </summary>
        /// <remarks>
        /// Currently, the YubiKey supports only ECDSA with SHA-256 for a
        /// credential algorithm. Hence, upon construction, this property will be
        /// set to <c>CoseAlgorithmIdentifier.ES256</c>. It is possible to change
        /// it, although until other algorithms are supported, setting this to a
        /// different algorithm will likely result in the YubiKey returning an
        /// error.
        /// </remarks>
        public CoseAlgorithmIdentifier Algorithm { get; set; }

        /// <summary>
        /// Constructs a new instance of <see cref="CredentialType"/> with the
        /// default type and algorithm.
        /// </summary>
        public CredentialType()
        {
            Type = "public-key";
            Algorithm = CoseAlgorithmIdentifier.ES256;
        }

        /// <summary>
        /// Constructs a new instance of <see cref="CredentialType"/> from
        /// the encoded value.
        /// </summary>
        /// <remarks>
        /// This constructor expects the encoding to follow this template.
        /// <code>
        ///    map {
        ///      "type"        --text string--
        ///      "alg"         --int--
        ///    }
        /// </code>
        /// </remarks>
        /// <param name="encodedCredentialType">
        /// The CBOR-encoded credential type.
        /// </param>
        /// <exception cref="Ctap2DataException">
        /// The <c>encodedCredentialType</c> is not a correct encoding.
        /// </exception>
        public CredentialType(ReadOnlyMemory<byte> encodedCredentialType)
        {
            var cbor = new CborReader(encodedCredentialType, CborConformanceMode.Ctap2Canonical);

            int? entries = cbor.ReadStartMap();
            int count = entries ?? 0;

            try
            {
                while (count > 0)
                {
                    string mapKey = cbor.ReadTextString();

                    switch (mapKey)
                    {
                        default:
                            throw new Ctap2DataException(
                                string.Format(
                                    CultureInfo.CurrentCulture,
                                    ExceptionMessages.Ctap2CborUnexpectedKey, TagType, mapKey));

                        case TagType:
                            Type = cbor.ReadTextString();
                            break;

                        case TagAlg:
                            Algorithm = (CoseAlgorithmIdentifier)cbor.ReadInt32();
                            break;
                    }

                    count--;
                }
            }
            catch (CborContentException cborException)
            {
                throw new Ctap2DataException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.Ctap2CborUnexpectedValue),
                        cborException);
            }

            if ((Type is null) || (Algorithm == CoseAlgorithmIdentifier.None))
            {
                throw new Ctap2DataException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.Ctap2MissingRequiredField));
            }
        }

        /// <inheritdoc/>
        public byte[] CborEncode()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);

            cbor.WriteStartMap(null);
            cbor.WriteTextString(TagType);
            cbor.WriteTextString(Type);
            cbor.WriteTextString(TagAlg);
            cbor.WriteInt32((int)Algorithm);
            cbor.WriteEndMap();

            return cbor.Encode();
        }
    }
}
