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
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.Cbor;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// A FIDO2 <c>RelyingParty</c>, consisting of ID and name. This is used when
    /// the FIDO2 standard specifies a <c>PublicKeyCredentialRpEntity</c>.
    /// </summary>
    /// <remarks>
    /// A relying party (RP) can specify its ID, but a client can also build an
    /// RP ID based on the domain of the page its currently communicating with.
    /// In addition, an authenticator or a platform can specify a
    /// "human-readable" name of the RP to display to the user.
    /// <para>
    /// This class holds the RP ID and name, and can encode and decode them as
    /// part of CBOR structures.
    /// </para>
    /// <para>
    /// The FIDO2 standard specifies that when communicating with the
    /// authenticator, the ID is a required element. The W3C standard says the ID
    /// is optional. This seeming contradiction is because the RP is not required
    /// to specify an ID. In that case, the client will build an ID from the
    /// domain it is communicating with. Either way, an ID must be passed to the
    /// authenticator. Hence, when building am instance of RelyingParty, an ID is
    /// required.
    /// </para>
    /// <para>
    /// The W3C standard declares the name a required element, and the FIDO2
    /// standard declares it optional. Because the FIDO2 standard specifically
    /// prescribes authenticator functionality, this class will allow a null name.
    /// </para>
    /// </remarks>
    public class RelyingParty : ICborEncode
    {
        private const string TagId = "id";
        private const string TagName = "name";

        /// <summary>
        /// The <c>id</c> component of the <c>RelyingParty</c>.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The <c>name</c> component of the <c>RelyingParty</c>.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The SHA-256 digest of the <c>RelyingParty.Id</c>.
        /// </summary>
        /// <remarks>
        /// When a <c>RelyingParty</c> object is created, the constructor will
        /// build the <c>RelyingPartyIdHash</c>. It is the digest of the UTF8
        /// byte array version of the string that is the <see cref="Id"/>.
        /// </remarks>
        public ReadOnlyMemory<byte> RelyingPartyIdHash { get; private set; }

        /// <summary>
        /// Constructs a new instance of <see cref="RelyingParty"/>.
        /// </summary>
        /// <param name="id">
        /// The relying party ID.
        /// </param>
        public RelyingParty(string id)
        {
            Id = id;
            ComputeRelyingPartyIdHash();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="RelyingParty"/> based on the
        /// encoded value.
        /// </summary>
        /// <remarks>
        /// This constructor expects the encoding to follow this template.
        /// <code>
        ///    map {
        ///      "id"          --text string--
        ///      "name"        --text string-- (optional)
        ///    }
        /// </code>
        /// </remarks>
        /// <param name="encodedRelyingParty">
        /// The CBOR-encoded relying party info.
        /// </param>
        /// <exception cref="Ctap2DataException">
        /// The <c>encodedRelyingParty</c> is not a correct encoding.
        /// </exception>
        public RelyingParty(ReadOnlyMemory<byte> encodedRelyingParty)
        {
            var cbor = new CborReader(encodedRelyingParty, CborConformanceMode.Ctap2Canonical);

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
                                    ExceptionMessages.Ctap2CborUnexpectedKey, TagId, mapKey));

                        case TagId:
                            Id = cbor.ReadTextString();
                            ComputeRelyingPartyIdHash();
                            break;

                        case TagName:
                            Name = cbor.ReadTextString();
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

            if (Id is null)
            {
                throw new Ctap2DataException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.Ctap2MissingRequiredField));
            }
        }

        /// <summary>
        /// Determine if the <c>candidateHash</c> the same as the computed
        /// <see cref="RelyingPartyIdHash"/> of this object. This is likely used
        /// when searching for a RelyingParty when all you have is the
        /// RelyingPartyIdHash, or when given a RelyingParty and a
        /// RelyingPartyIdHash (e.g. enumerating relying parties), and want to
        /// verify that the given value is correct.
        /// </summary>
        /// <param name="candidateHash">
        /// The purported relying party Id hash
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the candidate matches the hash inside the
        /// object and <c>false</c> otherwise.
        /// </returns>
        public bool IsMatchingRelyingPartyId(ReadOnlyMemory<byte> candidateHash) =>
            MemoryExtensions.SequenceEqual<byte>(candidateHash.Span, RelyingPartyIdHash.Span);

        // Perform the appropriate digest operation to generate the correct value.
        private void ComputeRelyingPartyIdHash()
        {
            using SHA256 digester = CryptographyProviders.Sha256Creator();
            digester.Initialize();
            byte[] utf = Encoding.UTF8.GetBytes(Id);
            byte[] digest = digester.ComputeHash(utf);
            RelyingPartyIdHash = new ReadOnlyMemory<byte>(digest);
        }

        /// <inheritdoc/>
        public byte[] CborEncode()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(null);

            cbor.WriteTextString(TagId);
            cbor.WriteTextString(Id);
            if (!(Name is null))
            {
                cbor.WriteTextString(TagName);
                cbor.WriteTextString(Name);
            }

            cbor.WriteEndMap();
            return cbor.Encode();
        }
    }
}
