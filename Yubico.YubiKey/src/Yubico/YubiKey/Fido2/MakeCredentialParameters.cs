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
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// This collects and encodes the information needed to make a FIDO2
    /// credential.
    /// </summary>
    /// <remarks>
    /// There are ten elements that are inputs to a FIDO2 credential (see section
    /// 6.1 of the FIDO2 standard). Four of them are required and six are
    /// optional.
    /// <para>
    /// When you need to make a new credential, you will collect all the required
    /// along with any optional parameters and build an instance of this class.
    /// Then pass that object to the <c>MakeCredential</c> method or command.
    /// </para>
    /// </remarks>
    public class MakeCredentialParameters : ICborEncode
    {
        private const int TagClientDataHash = 1;
        private const int TagRp = 2;
        private const int TagUserEntity = 3;
        private const int TagCredentialTypeList = 4;
        private const int TagExcludeList = 5;
        private const int TagExtensions = 6;
        private const int TagOptions = 7;
        private const int TagPinUvAuth = 8;
        private const int TagProtocol = 9;
        private const int TagEnterpriseAttestation = 10;

        private const string OptionRk = "rk";
        private const string OptionUp = "up";
        private const string OptionUv = "uv";

        private List<CredentialId>? _excludeList;
        private Dictionary<string, byte[]>? _extensions;
        private Dictionary<string, bool>? _options;
        private readonly List<CredentialType> _credentialTypes = new List<CredentialType>();

        /// <summary>
        /// The original <c>clientDataHash</c> that was provided by the client.
        /// It contains the challenge. This is a required element.
        /// </summary>
        public ReadOnlyMemory<byte> ClientDataHash { get; set; }

        /// <summary>
        /// The relying party's ID, along with an optional descriptive string.
        /// This is a required element.
        /// </summary>
        public RelyingParty RelyingParty { get; set; }

        /// <summary>
        /// The user's ID, along with optional descriptive strings. This is a
        /// required element.
        /// </summary>
        public UserEntity UserEntity { get; set; }

        /// <summary>
        /// The list of supported credential types and algorithms. The YubiKey
        /// will select one from the list based on what it supports. This is a
        /// required element.
        /// </summary>
        /// <remarks>
        /// To add an entry to the list, call <see cref="AddCredentialType"/>.
        /// <para>
        /// Currently only credential parameters of the pair
        /// "public-key"/ECDSA with SHA-256 is supported, so it is sufficent to
        /// use the constructor that automatically adds the default credential
        /// type. If this constructor is used and no other credential type is
        /// needed, there is no need to call <c>AddCredentialType</c>.
        /// </para>
        /// </remarks>
        public IReadOnlyList<CredentialType> CredentialTypes => _credentialTypes;

        /// <summary>
        /// The protocol chosen by the platform. This is an optional parameter,
        /// so it can be null.
        /// </summary>
        public PinUvAuthProtocol? Protocol { get; set; }

        /// <summary>
        /// The list of credentialIds for which the authenticator should not
        /// create a new credential. This is an optional parameter, so it can
        /// be null.
        /// </summary>
        /// <remarks>
        /// To add an entry to the list, call <see cref="ExcludeCredential"/>.
        /// </remarks>
        public IReadOnlyList<CredentialId>? ExcludeList => _excludeList;

        /// <summary>
        /// The list of extensions. This is an optional parameter, so it can be
        /// null.
        /// </summary>
        /// <remarks>
        /// To add an entry to the list, call <see cref="AddExtension"/>.
        /// <para>
        /// Each extension is a key/value pair. All keys are strings, but each
        /// extension has its own definition of a value. It could be an int, or
        /// it could be a map containing a string and a boolean,. It is the
        /// caller's responsibility to encode the value.
        /// </para>
        /// <para>
        /// For each value, the standard (or the vendor in the case of
        /// vendor-defined extensions) will define the structure of the value.
        /// From that structure the value can be encoded following CBOR rules.
        /// The result of the encoding the value is what is stored in this
        /// dictionary.
        /// </para>
        /// </remarks>
        public IReadOnlyDictionary<string, byte[]>? Extensions => _extensions;

        /// <summary>
        /// The list of authenticator options. Each standard-defined option is a
        /// key/value pair, where the key is a string and the value is a boolean.
        /// This is an optional parameter, so it can be null.
        /// </summary>
        public IReadOnlyDictionary<string, bool>? Options => _options;

        /// <summary>
        /// The result of calling the PinProtocol's method
        /// <see cref="PinUvAuthProtocolBase.AuthenticateUsingPinToken"/> using
        /// the PIN token as the key and the client data hash as the message.
        /// This is an optional parameter, so it can be null.
        /// </summary>
        /// <remarks>
        /// In order to obtain the <c>pinUvAuthParam</c>, choose a protocol and
        /// build the appropriate <see cref="PinUvAuthProtocolBase"/> object.
        /// Obtain the YubiKey's Key Agreement public key and call the protocol
        /// object's <c>Encapsulate</c> method. Next obtain the PIN token.
        /// Finally, call the protocol object's
        /// <c>AuthenticateUsingPinToken(byte[], byte[])</c> method using the
        /// <c>ClientDataHash</c> as the message to authenticate. Note that the
        /// first argument in this call is the PIN token, which is an encrypted
        /// value. Do not decrypt the PIN token. The result of that
        /// authentication operation is the <c>PinUvAuthParam</c>
        /// </remarks>
        public ReadOnlyMemory<byte>? PinUvAuthParam { get; set; }

        /// <summary>
        /// Specifies whether an enterprise attestation is to be returned along
        /// with the credential, and if so, which kind. This is an optional
        /// parameter, so it is can be null.
        /// </summary>
        /// <remarks>
        /// See also the documentation for <see cref="EnterpriseAttestation"/>.
        /// <para>
        /// Not all authenticators support enterprise attestation (check the
        /// <c>Options</c> property of <see cref="AuthenticatorInfo"/>). If a
        /// YubiKey does not support this option, setting this property (even
        /// setting it to <c>None</c>) will generate an error return.
        /// </para>
        /// <para>
        /// Furthermore, if an authenticator supports only vendor-facilitated
        /// attestation, the standard allows treating a request for
        /// platform-managed attestation as a request for vendor-facilitated.
        /// </para>
        /// </remarks>
        public EnterpriseAttestation? Attestation { get; set; }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private MakeCredentialParameters()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="MakeCredentialParameters"/>.
        /// </summary>
        /// <remarks>
        /// One of the required elements of the <c>MakeCredential</c> parameters
        /// is a list of supported credential types
        /// (<see cref="CredentialTypes"/>). Hence, you must supply at least
        /// one credential type, and that one will be the preferred type.
        /// Currently the only credential type supported by the YubiKey is the
        /// pair <c>"public-key"/ECDSA with SHA-256</c>. To build this parameters
        /// object with that credential type, use the constructor that adds it be
        /// default, or pass in an instance of <see cref="CredentialType"/>
        /// built using the default constructor. That is,
        /// <code>
        ///    var params = new MakeCredentialParameters(
        ///        relyingParty, userEntity, new CredentialType());
        /// </code>
        /// It is possible to add more types later using the method
        /// <see cref="AddCredentialType"/>.
        /// </remarks>
        /// <param name="relyingParty">
        /// The relying party for which the credential is to be created. This
        /// constructor copies a reference to the input object.
        /// </param>
        /// <param name="userEntity">
        /// The user for which the credential is to be created. This constructor
        /// copies a reference to the input object.
        /// </param>
        /// <param name="preferredCredentialType">
        /// The credential type (type and algorithm) that is the caller's first
        /// choice.
        /// </param>
        public MakeCredentialParameters(RelyingParty relyingParty, UserEntity userEntity, CredentialType preferredCredentialType)
        {
            RelyingParty = relyingParty;
            UserEntity = userEntity;
            AddCredentialType(preferredCredentialType);
        }

        /// <summary>
        /// Constructs a new instance of <see cref="MakeCredentialParameters"/>
        /// using the default credential type.
        /// </summary>
        /// <remarks>
        /// One of the required elements of the <c>MakeCredential</c> parameters
        /// is a list of supported credential types
        /// (<see cref="CredentialTypes"/>). Hence, you must supply at least
        /// one credential type, and that one will be the preferred type.
        /// Currently the only credential type supported by the YubiKey is the
        /// pair <c>"public-key"/ECDSA with SHA-256</c>. This constructor will
        /// add that credential. If you do not want this credential type, then
        /// use the other constructor that takes in a
        /// <see cref="CredentialType"/>. It is possible to add more types later
        /// using the method <see cref="AddCredentialType"/>.
        /// </remarks>
        /// <param name="relyingParty">
        /// The relying party for which the credential is to be created. This
        /// constructor copies a reference to the input object.
        /// </param>
        /// <param name="userEntity">
        /// The user for which the credential is to be created. This constructor
        /// copies a reference to the input object.
        /// </param>
        public MakeCredentialParameters(RelyingParty relyingParty, UserEntity userEntity)
        {
            RelyingParty = relyingParty;
            UserEntity = userEntity;
            AddCredentialType(new CredentialType());
        }

        /// <summary>
        /// Constructs a new instance of <see cref="MakeCredentialParameters"/>
        /// from the encoded value.
        /// </summary>
         /// <remarks>
        /// This constructor expects the encoding to follow this template.
        /// <code>
        ///    map {
        ///      01  --byte string--
        ///      02  --map--
        ///      03  --map--
        ///      04  --array of CredentialType--
        ///      05  --array of CredentialId-- (Optional)
        ///      07  --map-- (Optional)
        ///      08  --byte string-- (Optional)
        ///      09  --int-- (Optional)
        ///      0A  --int-- (Optional)
        ///    }
        /// </code>
        /// </remarks>
        /// <param name="encodedParameters">
        /// The CBOR-encoded <c>MakeCredentialParameters</c>.
        /// </param>
        /// <exception cref="Ctap2DataException">
        /// The <c>encodedParameters</c> is not a correct encoding.
        /// </exception>
        public MakeCredentialParameters(ReadOnlyMemory<byte> encodedParameters)
        {
            var cbor = new CborReader(encodedParameters, CborConformanceMode.Ctap2Canonical);
            int? entries = cbor.ReadStartMap();
            int count = entries ?? 0;

            while (count > 0)
            {
                int mapKey = (int)cbor.ReadInt32();

                switch (mapKey)
                {
                    default:
                        throw new Ctap2DataException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                ExceptionMessages.Ctap2CborUnexpectedKey, TagClientDataHash, mapKey));

                    case TagClientDataHash:
                        ClientDataHash = new ReadOnlyMemory<byte>(cbor.ReadByteString());
                        break;

                    case TagRp:
                        RelyingParty = new RelyingParty(cbor.ReadEncodedValue());
                        break;

                    case TagUserEntity:
                        UserEntity = new UserEntity(cbor.ReadEncodedValue());
                        break;

                    case TagCredentialTypeList:
                        entries = cbor.ReadStartArray();
                        int tCount = entries ?? 0;

                        while (tCount > 0)
                        {
                            AddCredentialType(new CredentialType(cbor.ReadEncodedValue()));
                            tCount--;
                        }

                        cbor.ReadEndArray();
                        break;

                    case TagExcludeList:
                        entries = cbor.ReadStartArray();
                        int eCount = entries ?? 0;

                        while (eCount > 0)
                        {
                            ExcludeCredential(new CredentialId(cbor.ReadEncodedValue()));
                            eCount--;
                        }

                        cbor.ReadEndArray();
                        break;

                    case TagExtensions:
                        entries = cbor.ReadStartMap();
                        int xCount = entries ?? 0;

                        while (xCount > 0)
                        {
                            string extensionKey = cbor.ReadTextString();
                            AddExtension(extensionKey, cbor.ReadEncodedValue().ToArray());
                            xCount--;
                        }

                        cbor.ReadEndMap();
                        break;

                    case TagOptions:
                        entries = cbor.ReadStartMap();
                        int oCount = entries ?? 0;

                        while (oCount > 0)
                        {
                            string optionKey = cbor.ReadTextString();
                            AddOption(optionKey, cbor.ReadBoolean());
                            oCount--;
                        }

                        cbor.ReadEndMap();
                        break;

                    case TagPinUvAuth:
                        PinUvAuthParam = new ReadOnlyMemory<byte>(cbor.ReadByteString());
                        break;

                    case TagProtocol:
                        Protocol = (PinUvAuthProtocol)cbor.ReadInt32();
                        break;

                    case TagEnterpriseAttestation:
                        Attestation = (EnterpriseAttestation)cbor.ReadInt32();
                        break;
                }

                count--;
            }

            cbor.ReadEndMap();

            if ((ClientDataHash.Length == 0)
                || (RelyingParty is null)
                || (UserEntity is null)
                || (CredentialTypes.Count == 0))
            {
                throw new Ctap2DataException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.Ctap2MissingRequiredField));
            }
        }

        /// <summary>
        /// Add an entry to the credential parameters list.
        /// </summary>
        /// <remarks>
        /// Currently only credential parameters of the pair "public-key"/ECDSA
        /// with SHA-256 is supported, so it is sufficent to call the constructor
        /// that adds this one by default.
        /// </remarks>
        /// <param name="credentialType">
        /// The <c>credentialType</c> object to add.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>credentialParamters</c> arg is null.
        /// </exception>
        public void AddCredentialType(CredentialType credentialType)
        {
            if (credentialType is null)
            {
                throw new ArgumentNullException(nameof(credentialType));
            }

            _credentialTypes.Add(credentialType);
        }

        /// <summary>
        /// Add an entry to the exclude list.
        /// </summary>
        /// <remarks>
        /// If there is no list yet when this method is called, one will be
        /// created. That is, even if the <see cref="ExcludeList"/> is null, you
        /// can call the method to add an entry.
        /// </remarks>
        /// <param name="credentialId">
        /// The <c>credentialId</c> to add.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>credentialId</c> arg is null.
        /// </exception>
        public void ExcludeCredential(CredentialId credentialId)
        {
            if (credentialId is null)
            {
                throw new ArgumentNullException(nameof(credentialId));
            }

            (_excludeList ??= new List<CredentialId>()).Add(credentialId);
        }

        /// <summary>
        /// Add an entry to the extensions list.
        /// </summary>
        /// <remarks>
        /// If there is no list yet when this method is called, one will be
        /// created. That is, even if the <see cref="Extensions"/> is null, you
        /// can call the method to add an entry.
        /// <para>
        /// Each extension is a key/value pair. For each extension the key is a
        /// string (such as "credProtect" or "hmac-secret"). However, each value
        /// is different. There will be a definition of the value that
        /// accompanies each key. It will be possible to encode that definition
        /// using the rules of CBOR. The caller supplies the key and the encoded
        /// value.
        /// </para>
        /// </remarks>
        /// <param name="extensionKey">
        /// The key of key/value to add.
        /// </param>
        /// <param name="encodedValue">
        /// The CBOR-encoded value of key/value to add.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>extensionKey</c> or <c>encodedValue</c> arg is null.
        /// </exception>
        public void AddExtension(string extensionKey, byte[] encodedValue)
        {
            if (extensionKey is null)
            {
                throw new ArgumentNullException(nameof(extensionKey));
            }
            if (encodedValue is null)
            {
                throw new ArgumentNullException(nameof(encodedValue));
            }

            (_extensions ??= new Dictionary<string, byte[]>()).Add(extensionKey, encodedValue);
        }

        /// <summary>
        /// Add an entry to the list of options.
        /// </summary>
        /// <remarks>
        /// If the <c>Options</c> list already contains an entry with the given
        /// <c>optionKey</c>, this method will replace it.
        /// </remarks>
        /// <param name="optionKey">
        /// The option to add. This is the key of the option key/value pair.
        /// </param>
        /// <param name="value">
        /// The value this option will possess.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The <c>optionKey</c> arg is not a valid key defined in the
        /// standard.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// The <c>optionKey</c> arg is null.
        /// </exception>
        public void AddOption(string optionKey, bool value)
        {
            if (optionKey is null)
            {
                throw new ArgumentNullException(nameof(optionKey));
            }

            if (!IsValidOption(optionKey))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidCtap2Data));
            }

            _options ??= new Dictionary<string, bool>();
            // If the option already exists, relpace what is in the dictionary
            // with this one.
            // This will add a new entry if there is no entry associated with the
            // given optionKey.
            _options[optionKey] = value;
        }

        // Check to see if the given optionKey is one from the standard.
        private static bool IsValidOption(string optionKey)
        {
            return optionKey.Equals(OptionRk, StringComparison.Ordinal)
                || optionKey.Equals(OptionUp, StringComparison.Ordinal)
                || optionKey.Equals(OptionUv, StringComparison.Ordinal);
        }

        // Implements CborHelper.CborEncodeDelegate.
        // Encode each of the entries as an array.
        // If there are no entries in the list, this method will write out an
        // array of 0 elements. So if you want "no entries" to mean "don't write
        // anything", don't call this method.
        private static byte[] EncodeArrayOfObjects(object? localData)
        {
            if ((!(localData is IReadOnlyList<ICborEncode> entryList)) || (entryList.Count == 0))
            {
                return Array.Empty<byte>();
            }

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);

            cbor.WriteStartArray(entryList.Count);
            foreach (ICborEncode cborEncode in entryList)
            {
                cbor.WriteEncodedValue(cborEncode.CborEncode());
            }
            cbor.WriteEndArray();

            return cbor.Encode();
        }

        // Implements CborHelper.CborEncodeDelegate.
        private byte[] EncodeExtensions(object? localData)
        {
            if ((_extensions is null) || !(localData is null))
            {
                return Array.Empty<byte>();
            }

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);

            cbor.WriteStartMap(null);
            foreach (KeyValuePair<string, byte[]> entry in _extensions)
            {
                cbor.WriteTextString(entry.Key);
                cbor.WriteEncodedValue(entry.Value);
            }
            cbor.WriteEndMap();

            return cbor.Encode();
        }

        // Implements CborHelper.CborEncodeDelegate.
        private byte[] EncodeOptions(object? localData)
        {
            if ((_options is null) || !(localData is null))
            {
                return Array.Empty<byte>();
            }

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);

            cbor.WriteStartMap(null);
            foreach (KeyValuePair<string, bool> entry in _options)
            {
                cbor.WriteTextString(entry.Key);
                cbor.WriteBoolean(entry.Value);
            }
            cbor.WriteEndMap();

            return cbor.Encode();
        }

        /// <inheritdoc/>
        public byte[] CborEncode()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);

            CborHelpers.BeginMap<long>(cbor)
                .Entry(TagClientDataHash, ClientDataHash)
                .Entry(TagRp, RelyingParty)
                .Entry(TagUserEntity, UserEntity)
                .Entry(TagCredentialTypeList, EncodeArrayOfObjects, CredentialTypes)
                .OptionalEntry(TagExcludeList, EncodeArrayOfObjects, ExcludeList)
                .OptionalEntry(TagExtensions, EncodeExtensions, null)
                .OptionalEntry(TagOptions, EncodeOptions, null)
                .OptionalEntry(TagPinUvAuth, PinUvAuthParam)
                .OptionalEntry(TagProtocol, (long?)Protocol)
                .OptionalEntry(TagEnterpriseAttestation, (long?)Attestation)
                .EndMap();

            return cbor.Encode();
        }
    }
}
