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
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.Cose;
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
        private const int TagAlgorithmsList = 4;
        private const int TagExcludeList = 5;
        private const int TagExtensions = 6;
        private const int TagOptions = 7;
        private const int TagPinUvAuth = 8;
        private const int TagProtocol = 9;
        private const int TagEnterpriseAttestation = 10;
        private const string KeyCredBlob = "credBlob";

        private readonly List<Tuple<string, CoseAlgorithmIdentifier>> _algorithms = new List<Tuple<string, CoseAlgorithmIdentifier>>();
        private List<CredentialId>? _excludeList;
        private Dictionary<string, byte[]>? _extensions;
        private Dictionary<string, bool>? _options;

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
        /// The list of supported algorithms for credential generation. This is
        /// the "pubKeyCredParams" in the standard (FIDO2 section 6.1).
        /// </summary>
        /// <remarks>
        /// Each entry in the list is a type and algorithm. Neither the type nor
        /// algorithm are guaranteed to be unique, although each combination is.
        /// Currently, the only type defined is "public-key". The only algorithm
        /// the YubiKey supports is ECDSA with SHA-256 using the NIST P-256
        /// curve. This is the pair
        /// "public-key"/<c>CoseAlgorithmIdentifier.ES256</c>.
        /// <para>
        /// To add an entry to the list, call <see cref="AddAlgorithm"/>.
        /// </para>
        /// </remarks>
        public IReadOnlyList<Tuple<string, CoseAlgorithmIdentifier>> Algorithms => _algorithms;

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
        /// <remarks>
        /// To add options, call <see cref="AddOption"/>.
        /// </remarks>
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
        /// The protocol chosen by the platform. This is an optional parameter,
        /// so it can be null.
        /// </summary>
        public PinUvAuthProtocol? Protocol { get; set; }

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
        public EnterpriseAttestation? EnterpriseAttestation { get; set; }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private MakeCredentialParameters()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="MakeCredentialParameters"/>
        /// setting the preferred algorithm to something other than the default.
        /// </summary>
        /// <remarks>
        /// One of the required elements of the <c>MakeCredential</c> parameters
        /// is a list of supported algorithms (a type and algorithm pair,
        /// <see cref="Algorithms"/>). Hence, you must supply at least one
        /// algorithm, which will be the preferred one. Currently the only
        /// algorithm supported by the YubiKey is the pair
        /// <c>"public-key"/ECDSA with SHA-256</c>. To build this parameters
        /// object with that credential type, use the constructor that adds it be
        /// default. If you want to specify something other than the default, use
        /// this constructor.
        /// <para>
        /// It is possible to add more types later using the method
        /// <see cref="AddAlgorithm"/>.
        /// </para>
        /// </remarks>
        /// <param name="relyingParty">
        /// The relying party for which the credential is to be created. This
        /// constructor copies a reference to the input object.
        /// </param>
        /// <param name="userEntity">
        /// The user for which the credential is to be created. This constructor
        /// copies a reference to the input object.
        /// </param>
        /// <param name="algorithmType">
        /// The type of type and algorithm that is the caller's preferred choice.
        /// </param>
        /// <param name="algorithm">
        /// The algorithm of type and algorithm that is the caller's preferred
        /// choice.
        /// </param>
        public MakeCredentialParameters(
            RelyingParty relyingParty,
            UserEntity userEntity,
            string algorithmType,
            CoseAlgorithmIdentifier algorithm)
        {
            RelyingParty = relyingParty;
            UserEntity = userEntity;
            AddAlgorithm(algorithmType, algorithm);
        }

        /// <summary>
        /// Constructs a new instance of <see cref="MakeCredentialParameters"/>
        /// using the default preferred algorithm
        /// </summary>
        /// <remarks>
        /// One of the required elements of the <c>MakeCredential</c> parameters
        /// is a list of supported algorithms (a type and algorithm pair,
        /// <see cref="Algorithms"/>). Hence, you must supply at least one
        /// algorithm, which will be the preferred one. Currently the only
        /// algorithm supported by the YubiKey is the pair
        /// <c>"public-key"/ECDSA with SHA-256</c>. To build this parameters
        /// object with that credential type, use this constructor, which will
        /// add it by default. If you want to specify something other than the
        /// default, use the constructor that takes in an <c>algorithmType</c>
        /// and <c>algorithm</c>.
        /// <para>
        /// It is possible to add more types later using the method
        /// <see cref="AddAlgorithm"/>.
        /// </para>
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
            AddAlgorithm(ParameterHelpers.DefaultAlgType, ParameterHelpers.DefaultAlg);
        }

        /// <summary>
        /// Add an entry to <see cref="Algorithms"/>.
        /// </summary>
        public void AddAlgorithm(string algorithmType, CoseAlgorithmIdentifier algorithm) =>
            _algorithms.Add(new Tuple<string, CoseAlgorithmIdentifier>(algorithmType, algorithm));

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
        public void ExcludeCredential(CredentialId credentialId) => _excludeList =
            ParameterHelpers.AddToList<CredentialId>(credentialId, _excludeList);

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
        /// value. This method copies a reference to the byte array value.
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
        public void AddExtension(string extensionKey, byte[] encodedValue) => _extensions =
            ParameterHelpers.AddKeyValue<byte[]>(extensionKey, encodedValue, _extensions);

        /// <summary>
        /// Add the "credBlob" extension.
        /// </summary>
        /// <remarks>
        /// Because this extension is used more often, a dedicated method is
        /// provided as a convenience. There is no need for the caller to
        /// encode the <c>credBlobValue</c>. That is, this is essentially the
        /// same as calling <c>AddExtension</c>, except this method will verify
        /// the YubiKey supports the extension, verify the data length, use the
        /// appropriate <c>extensionString</c>, and encode the value.
        /// <para>
        /// The caller supplies the <c>AuthenticatorInfo</c> for the YubiKey,
        /// obtained by calling the <see cref="Commands.GetInfoCommand"/> or
        /// providing the <see cref="Fido2Session.AuthenticatorInfo"/> property.
        /// </para>
        /// <para>
        /// This method will determine from the <c>authenticatorInfo</c> whether
        /// the YubiKey supports this extension, and whether the data provided is
        /// within the YubiKey's range for "credBlob". The standard specifies
        /// that the maximum credBlob length is at least 32 bytes. The
        /// <c>AuthenticatorInfo</c> contains the property
        /// <c>MaximumCredentialBlobLength</c>, which is the length the YubiKey
        /// supports. If the YubiKey does not support the "credBlob" extension,
        /// or the data is too long, this method will throw an exception.
        /// </para>
        /// <para>
        /// The caller supplies the un-encoded <c>credBlobValue</c>. This method
        /// will encode it.
        /// </para>
        /// </remarks>
        /// <param name="credBlobValue">
        /// The data to add as the "credBlob" extension.
        /// </param>
        /// <param name="authenticatorInfo">
        /// The FIDO2 <c>AuthenticatorInfo</c> for the YubiKey being used.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>credBlobValue</c> or <c>authenticatorInfo</c> arg is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The YubiKey does not support this extension, or the value's length
        /// was invalid.
        /// </exception>
        public void AddCredBlobExtension(byte[] credBlobValue, AuthenticatorInfo authenticatorInfo)
        {
            if (authenticatorInfo is null)
            {
                throw new ArgumentNullException(nameof(authenticatorInfo));
            }
            if (credBlobValue is null)
            {
                throw new ArgumentNullException(nameof(credBlobValue));
            }

            if (!authenticatorInfo.Extensions.Contains<string>(KeyCredBlob))
            {
                throw new ArgumentException(ExceptionMessages.NotSupportedByYubiKeyVersion);
            }
            if (credBlobValue.Length > authenticatorInfo.MaximumCredentialBlobLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidDataLength,
                        0, authenticatorInfo.MaximumCredentialBlobLength, credBlobValue.Length));
            }

            var cborWriter = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cborWriter.WriteByteString(credBlobValue);
            byte[] encodedValue = cborWriter.Encode();

            AddExtension(KeyCredBlob, encodedValue);
        }

        /// <summary>
        /// Add an entry to the list of options.
        /// </summary>
        /// <remarks>
        /// If the <c>Options</c> list already contains an entry with the given
        /// <c>optionKey</c>, this method will replace it.
        /// <para>
        /// Note that the standard specifies valid option keys. Currently they
        /// are "rk", "up", and "uv". This method will accept any key given and
        /// pass it to the YubiKey. If an invalid key is used, the YubiKey will
        /// return an error.
        /// </para>
        /// </remarks>
        /// <param name="optionKey">
        /// The option to add. This is the key of the option key/value pair.
        /// </param>
        /// <param name="optionValue">
        /// The value this option will possess.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>optionKey</c> arg is null.
        /// </exception>
        public void AddOption(string optionKey, bool optionValue) => _options =
            ParameterHelpers.AddKeyValue<bool>(optionKey, optionValue, _options);

        /// <inheritdoc/>
        public byte[] CborEncode()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);

            CborHelpers.BeginMap<int>(cbor)
                .Entry(TagClientDataHash, ClientDataHash)
                .Entry(TagRp, RelyingParty)
                .Entry(TagUserEntity, UserEntity)
                .Entry(TagAlgorithmsList, EncodeAlgorithms, this)
                .OptionalEntry<IReadOnlyList<ICborEncode>>(TagExcludeList, CborHelpers.EncodeArrayOfObjects, ExcludeList)
                .OptionalEntry<Dictionary<string,byte[]>>(TagExtensions, ParameterHelpers.EncodeKeyValues<byte[]>, _extensions)
                .OptionalEntry<Dictionary<string,bool>>(TagOptions, ParameterHelpers.EncodeKeyValues<bool>, _options)
                .OptionalEntry(TagPinUvAuth, PinUvAuthParam)
                .OptionalEntry(TagProtocol, (int?)Protocol)
                .OptionalEntry(TagEnterpriseAttestation, (int?)EnterpriseAttestation)
                .EndMap();

            return cbor.Encode();
        }

        private byte[] EncodeAlgorithms(MakeCredentialParameters? localData)
        {
            if ((localData is null) || (localData.Algorithms.Count == 0))
            {
                return Array.Empty<byte>();
            }

            int count = localData.Algorithms.Count;

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartArray(count);

            for (int index = 0; index < count; index++)
            {
                string algType = localData.Algorithms[index].Item1;
                int alg = (int)localData.Algorithms[index].Item2;

                CborHelpers.BeginMap<string>(cbor)
                    .Entry(ParameterHelpers.TagType, algType)
                    .Entry(ParameterHelpers.TagAlg, alg)
                    .EndMap();
            }
            cbor.WriteEndArray();

            return cbor.Encode();
        }
    }
}
