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
using System.Linq;
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
        private const string KeyHmacSecret = "hmac-secret";
        private const string KeyCredProtect = "credProtect";
        private const string KeyMinPinLength = "minPinLength";

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
        /// <remarks>
        /// Note that a <c>UserEntity</c> is a required element in order to make
        /// a credential. The standard specifies that the <c>UserEntity</c> is
        /// made up of an <c>ID</c>, a <c>Name</c>, and a <c>DisplayName</c>.
        /// The standard also says the <c>Name</c> and <c>DisplayName</c> are
        /// optional. It should be possible to make a credential using a
        /// <c>UserEntity</c> that contains only an <c>ID</c>. However, YubiKeys
        /// prior to version 5.3.0 require a <c>Name</c> in order to make a
        /// credential.
        /// </remarks>
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
        /// <see cref="PinUvAuthProtocolBase.AuthenticateUsingPinToken(byte[],byte[])"/> using
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
        /// Note that a <c>UserEntity</c> is a required element in order to make
        /// a credential. The standard specifies that the <c>UserEntity</c> is
        /// made up of an <c>ID</c>, a <c>Name</c>, and a <c>DisplayName</c>.
        /// The standard also says the <c>Name</c> and <c>DisplayName</c> are
        /// optional. It should be possible to make a credential using a
        /// <c>UserEntity</c> that contains only an <c>ID</c>. However, YubiKeys
        /// prior to version 5.3.0 require a <c>Name</c> in order to make a
        /// credential.
        /// <para>
        /// One of the required elements of the <c>MakeCredential</c> parameters
        /// is a list of supported algorithms (a type and algorithm pair,
        /// <see cref="Algorithms"/>). Hence, you must supply at least one
        /// algorithm, which will be the preferred one. Currently the only
        /// algorithm supported by the YubiKey is the pair
        /// <c>"public-key"/ECDSA with SHA-256</c>. To build this parameters
        /// object with that credential type, use the constructor that adds it be
        /// default. If you want to specify something other than the default, use
        /// this constructor.
        /// </para>
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
        /// Note that a <c>UserEntity</c> is a required element in order to make
        /// a credential. The standard specifies that the <c>UserEntity</c> is
        /// made up of an <c>ID</c>, a <c>Name</c>, and a <c>DisplayName</c>.
        /// The standard also says the <c>Name</c> and <c>DisplayName</c> are
        /// optional. It should be possible to make a credential using a
        /// <c>UserEntity</c> that contains only an <c>ID</c>. However, YubiKeys
        /// prior to version 5.3.0 require a <c>Name</c> in order to make a
        /// credential.
        /// <para>
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
        /// </para>
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
        /// Specify that the YubiKey should return the minimum PIN length with
        /// the credential.
        /// </summary>
        /// <remarks>
        /// Because this extension is used more often, a dedicated method is
        /// provided as a convenience. Note that the minimum PIN length is
        /// visible to only those RPs who have permission. See the documentation
        /// for <see cref="Fido2Session.TrySetPinConfig"/> and the
        /// <xref href="Fido2MinPinLength">User's Manual entry</xref>
        /// on the minimum PIN length.
        /// <para>
        /// When the YubiKey makes the credential, it will be sent to the relying
        /// party. At that point, the relying party can reject it. One reason an
        /// RP might reject a credential is if the minimum PIN length is too
        /// short.
        /// </para>
        /// <para>
        /// If the RP for which the credential is being built is not allowed to
        /// see the minimum PIN length, the YubiKey will simply not return the
        /// minimum PIN length. This is not an error. The credential will be
        /// made, but it will not contain the minimum PIN length.
        /// </para>
        /// <para>
        /// If the minimum PIN length is returned with the credential, it will be
        /// in the <see cref="MakeCredentialData.AuthenticatorData"/> and can be
        /// retrieved using
        /// <see cref="AuthenticatorData.GetMinPinLengthExtension"/>
        /// </para>
        /// <para>
        /// The caller supplies the <c>AuthenticatorInfo</c> for the YubiKey,
        /// obtained by calling the <see cref="Commands.GetInfoCommand"/> or
        /// providing the <see cref="Fido2Session.AuthenticatorInfo"/> property.
        /// This method will determine from the <c>authenticatorInfo</c> whether
        /// the YubiKey supports this extension.
        /// </para>
        /// </remarks>
        /// <param name="authenticatorInfo">
        /// The FIDO2 <c>AuthenticatorInfo</c> for the YubiKey being used.
        /// </param>
        public void AddMinPinLengthExtension(AuthenticatorInfo authenticatorInfo)
        {
            if (authenticatorInfo is null)
            {
                throw new ArgumentNullException(nameof(authenticatorInfo));
            }

            if (!authenticatorInfo.Extensions.Contains<string>(KeyMinPinLength))
            {
                throw new NotSupportedException(ExceptionMessages.NotSupportedByYubiKeyVersion);
            }

            AddExtension(KeyMinPinLength, new byte[] { 0xF5 });
        }

        /// <summary>
        /// Add the "credBlob" extension. Note that the credBlob extension is
        /// valid only for discoverable credentials.
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
        /// <para>
        /// The credBlob data will be returned when the credential is used to get
        /// an assertion. When building the GetAssertion parameters, the caller
        /// must specify that the YubiKey return the credBlob. See
        /// <see cref="GetAssertionParameters.RequestCredBlobExtension"/>. The
        /// assertion returned will contain the credBlob. The data will be
        /// returned in the <see cref="GetAssertionData.AuthenticatorData"/> and
        /// can be retrieved using
        /// <see cref="AuthenticatorData.GetCredBlobExtension"/>
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
                throw new NotSupportedException(ExceptionMessages.NotSupportedByYubiKeyVersion);
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
        /// Add the "hmac-secret" extension, meaning the YubiKey will generate a
        /// secret value to be associated with the credential made. When getting
        /// an assertion, it will be possible to get the secret value. Note that
        /// the hmac-secret extension is valid for both discoverable and
        /// non-discoverable credentials.
        /// </summary>
        /// <remarks>
        /// Because this extension is used more often, a dedicated method is
        /// provided as a convenience. There is no need for the caller to
        /// encode the <c>hmacSecretValue</c>. That is, this is essentially the
        /// same as calling <c>AddExtension</c>, except this method will verify
        /// the YubiKey supports the extension, and encode the value.
        /// <para>
        /// The caller supplies the <c>AuthenticatorInfo</c> for the YubiKey,
        /// obtained by calling the <see cref="Commands.GetInfoCommand"/> or
        /// providing the <see cref="Fido2Session.AuthenticatorInfo"/> property.
        /// </para>
        /// <para>
        /// This method will determine from the <c>authenticatorInfo</c> whether
        /// the YubiKey supports this extension.
        /// </para>
        /// <para>
        /// The hmac-secret data will be returned when the credential is used to
        /// get an assertion. When building the GetAssertion parameters, the
        /// caller must specify that the YubiKey return the hmac-secret. See
        /// <see cref="GetAssertionParameters.RequestHmacSecretExtension"/>. The
        /// assertion returned will contain the hmac-secret output. The result
        /// will be returned in the
        /// <see cref="GetAssertionData.AuthenticatorData"/> and can be
        /// retrieved using
        /// <see cref="AuthenticatorData.GetHmacSecretExtension"/>
        /// </para>
        /// </remarks>
        /// <param name="authenticatorInfo">
        /// The FIDO2 <c>AuthenticatorInfo</c> for the YubiKey being used.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>authenticatorInfo</c> arg is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The YubiKey does not support this extension.
        /// </exception>
        public void AddHmacSecretExtension(AuthenticatorInfo authenticatorInfo)
        {
            if (authenticatorInfo is null)
            {
                throw new ArgumentNullException(nameof(authenticatorInfo));
            }

            if (!authenticatorInfo.Extensions.Contains<string>(KeyHmacSecret))
            {
                throw new NotSupportedException(ExceptionMessages.NotSupportedByYubiKeyVersion);
            }

            AddExtension(KeyHmacSecret, new byte[] { 0xF5 });
        }

        /// <summary>
        /// Add the "credProtect" extension, specifying the protection policy the
        /// YubiKey is to use when getting assertions.
        /// </summary>
        /// <remarks>
        /// Section 12.1 of the FIDO2 CTAP 2.1 standard specifies this extension.
        /// There are two parts: what the relying party communicates to the
        /// client, and what the client communicates to the authenticator. This
        /// class, <c>MakeCredentialParameters</c>, builds the parameters for the
        /// message from the client to the authenticator. Hence, this method will
        /// build the extension in the structure specified by the standard in the
        /// message from the client to the YubiKey.
        /// <para>
        /// Note that the standard specifies that the the message from RP to
        /// client contains the same information as the message from the client
        /// to the authenticator, just in a different format. Furthermore, the
        /// message from the RP to the client contains extra information, namely
        /// a boolean indicating the RP's request on how to handle the case where
        /// the authenticator does not support user verification (UV). That
        /// boolean is not passed down to the YubiKey and it is the
        /// responsibility of the client to handle that logic.
        /// </para>
        /// <para>
        /// Because this extension is used more often, a dedicated method is
        /// provided as a convenience. There is no need for the caller to
        /// encode the <c>credProtectPolicy</c>. That is, this is essentially the
        /// same as calling <c>AddExtension</c>, except this method will verify
        /// the YubiKey supports the extension, verify the data, use the
        /// appropriate <c>>extensionString</c>, and encode the value.
        /// </para>
        /// <para>
        /// The caller supplies the <c>AuthenticatorInfo</c> for the YubiKey,
        /// obtained by calling the <see cref="Commands.GetInfoCommand"/> or
        /// providing the <see cref="Fido2Session.AuthenticatorInfo"/> property.
        /// </para>
        /// <para>
        /// This method will determine from the <c>authenticatorInfo</c> whether
        /// the YubiKey supports this extension, and whether the data provided is
        /// correct for the YubiKey's support for "credProtect".
        /// </para>
        /// <para>
        /// The standard defines three policies:
        /// <code>
        ///    UserVerificationOptional
        ///    UserVerificationOptionalWithCredentialIDList
        ///    UserVerificationRequired
        /// </code>
        /// The SDK provides for one more option: <c>None</c>.
        /// </para>
        /// <para>
        /// The policy <c>UserVerificationOptionalWithCredentialIDList</c> means
        /// that the authenticator may or may not enforce UV if the request for
        /// an assertion is accompanied by a credential ID (see the
        /// <c>allowList</c> in <see cref="GetAssertionParameters"/>). If there
        /// is no credential ID (no <c>allowList</c>), then UV is required to get
        /// an assertion.
        /// </para>
        /// <para>
        /// You can see the "credProtect" policy in the
        /// <c>MakeCredentialData.AuthenticatorData.Extensions</c> property once
        /// the credential has been made. See
        /// <see cref="AuthenticatorData.GetCredProtectExtension"/>.
        /// </para>
        /// <para>
        /// Note that while the "credProtect" policy refers to how the credential
        /// is protected when getting an assertion, the "credProtect" policy is
        /// not returned by the YubiKey in the
        /// <c>GetAssertionData.AuthenticatorData.Extensions</c>.
        /// </para>
        /// <para>
        /// If you pass <c>None</c> as the <c>credProtectPolicy</c>, this method
        /// will do nothing and return. The "credProtect" policy of the
        /// credential will be the YubiKey's default.
        /// </para>
        /// </remarks>
        /// <param name="credProtectPolicy">
        /// The "credProtect" policy the YubiKey is to follow when making the
        /// credential.
        /// </param>
        /// <param name="authenticatorInfo">
        /// The FIDO2 <c>AuthenticatorInfo</c> for the YubiKey being used.
        /// </param>
        /// <param name="enforceCredProtectPolicy">
        /// Determines the behavior taken when the authenticator does not support the
        /// requested credProtect extension. Throws NotSupportedException when true, returns
        /// silently without adding the extension when false.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>authenticatorInfo</c> arg is null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The YubiKey does not support this extension, or the input values were
        /// not correct.
        /// </exception>
        public void AddCredProtectExtension(
            CredProtectPolicy credProtectPolicy,
            AuthenticatorInfo authenticatorInfo,
            bool enforceCredProtectPolicy = true)
        {
            if (credProtectPolicy == CredProtectPolicy.None)
            {
                return;
            }
            if (authenticatorInfo is null)
            {
                throw new ArgumentNullException(nameof(authenticatorInfo));
            }
            if (!authenticatorInfo.Extensions.Contains<string>(KeyCredProtect))
            {
                if (enforceCredProtectPolicy)
                {
                    throw new NotSupportedException(ExceptionMessages.NotSupportedByYubiKeyVersion);
                }

                return;
            }

            // The encoding is key/value where the key is "credProtect" and the
            // value is an unsigned int (major type 0). The only three possible
            // values are 1, 2, or 3, so the encoding is simply 0x01, 02,or 03.
            AddExtension(KeyCredProtect, new byte[] { (byte)credProtectPolicy });
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
            return new CborMapWriter<int>()
                .Entry(TagClientDataHash, ClientDataHash)
                .Entry(TagRp, RelyingParty)
                .Entry(TagUserEntity, UserEntity)
                .Entry(TagAlgorithmsList, EncodeAlgorithms, this)
                .OptionalEntry<IReadOnlyList<ICborEncode>>(TagExcludeList, CborHelpers.EncodeArrayOfObjects, ExcludeList)
                .OptionalEntry<Dictionary<string, byte[]>>(TagExtensions, ParameterHelpers.EncodeKeyValues<byte[]>, _extensions)
                .OptionalEntry<Dictionary<string, bool>>(TagOptions, ParameterHelpers.EncodeKeyValues<bool>, _options)
                .OptionalEntry(TagPinUvAuth, PinUvAuthParam)
                .OptionalEntry(TagProtocol, (int?)Protocol)
                .OptionalEntry(TagEnterpriseAttestation, (int?)EnterpriseAttestation)
                .Encode();
        }

        private byte[] EncodeAlgorithms(MakeCredentialParameters? localData)
        {
            if (localData is null || localData.Algorithms.Count == 0)
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
