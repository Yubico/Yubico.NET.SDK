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
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// This collects and encodes the information needed to get a FIDO2
    /// assertion.
    /// </summary>
    /// <remarks>
    /// There are seven elements that are inputs to a FIDO2 assertion (see section
    /// 6.2 of the FIDO2 standard). Two of them are required and five are
    /// optional.
    /// <para>
    /// When you need to get an assertion, you will collect all the required
    /// along with any optional parameters and build an instance of this class.
    /// Then pass that object to the <c>GetAssertion</c> method or command.
    /// </para>
    /// </remarks>
    public class GetAssertionParameters : AuthenticatorOperationParameters<GetAssertionParameters>
    {
        private const int TagRp = 1;
        private const int TagClientDataHash = 2;
        private const int TagAllowList = 3;
        private const int TagExtensions = 4;
        private const int TagOptions = 5;
        private const int TagPinUvAuth = 6;
        private const int TagProtocol = 7;
        
        private ReadOnlyMemory<byte>? _salt1;
        private ReadOnlyMemory<byte>? _salt2;
        private byte[]? _hmacSecretEncoding;

        private List<CredentialId>? _allowList;

        /// <summary>
        /// The relying party's ID, along with an optional descriptive string.
        /// This is a required element.
        /// </summary>
        public RelyingParty RelyingParty { get; set; }

        /// <summary>
        /// The original <c>clientDataHash</c> that was provided by the client.
        /// It contains the challenge. This is a required element.
        /// </summary>
        public ReadOnlyMemory<byte> ClientDataHash { get; set; }

        /// <summary>
        /// The list of credentialIds for which the authenticator must generate a
        /// new assertion. This is an optional parameter, so it can be null. This
        /// is generally used to specify a non-discoverable credential.
        /// </summary>
        /// <remarks>
        /// To add an entry to the list, call <see cref="AllowCredential"/>.
        /// </remarks>
        public IReadOnlyList<CredentialId>? AllowList => _allowList;

        /// <summary>
        /// The result of calling the PinProtocol's method
        /// <see cref="PinUvAuthProtocolBase.AuthenticateUsingPinToken(byte[],byte[])"/>
        /// using the PIN token as the key and the client data hash as the
        /// message. This is an optional parameter, so it can be null.
        /// &gt; [!NOTE]
        /// &gt; If you get assertions by calling the &gt; <see cref="Fido2Session"/>
        /// &gt; method <see cref="Fido2Session.GetAssertions"/>, &gt; you do not
        /// &gt; need to set this property, the SDK will do so. If you get an
        /// &gt; assertion using the commands, you must set this property.
        /// </summary>
        /// <remarks>
        /// If you are getting assertions using
        /// <see cref="Fido2Session.GetAssertions"/>, you do NOT need to set this
        /// property, the SDK will take care of it. But if you are getting
        /// assertions using the <see cref="Commands.GetAssertionCommand"/>, then
        /// you must set this property.
        /// <para>
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
        /// </para>
        /// </remarks>
        public ReadOnlyMemory<byte>? PinUvAuthParam { get; set; }

        /// <summary>
        /// The protocol chosen by the platform. This is an optional parameter,
        /// so it can be null.
        /// </summary>
        /// <remarks>
        /// If you are getting assertions using
        /// <see cref="Fido2Session.GetAssertions"/>, you do NOT need to set this
        /// property, the SDK will take care of it. But if you are getting
        /// assertions using the <see cref="Commands.GetAssertionCommand"/>, then
        /// you must set this property.
        /// </remarks>
        public PinUvAuthProtocol? Protocol { get; set; }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private GetAssertionParameters()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="GetAssertionParameters"/>.
        /// </summary>
        /// <remarks>
        /// Both the relying party and client data hash are required parameters.
        /// All others are optional.
        /// </remarks>
        /// <param name="relyingParty">
        /// The relying party for which the assertion is to be obtained. This
        /// constructor copies a reference to the input object. This constructor
        /// will copy a reference to the input.
        /// </param>
        /// <param name="clientDataHash">
        /// The client data hash for the current connection. This constructor
        /// will copy a reference to the input.
        /// </param>
        public GetAssertionParameters(RelyingParty relyingParty, ReadOnlyMemory<byte> clientDataHash)
        {
            RelyingParty = relyingParty;
            ClientDataHash = clientDataHash;
        }

        /// <summary>
        /// Add an entry to the allow list. Once a credential is added to the
        /// allow list, it is not possible to remove it.
        /// </summary>
        /// <remarks>
        /// If there is no list yet when this method is called, one will be
        /// created. That is, even if the <see cref="AllowList"/> is null, you
        /// can call the method to add an entry.
        /// </remarks>
        /// <param name="credentialId">
        /// The <c>credentialId</c> to add.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>credentialId</c> arg is null.
        /// </exception>
        public void AllowCredential(CredentialId credentialId) => _allowList =
            ParameterHelpers.AddToList<CredentialId>(credentialId, _allowList);

        /// <inheritdoc/>
        public override byte[] CborEncode()
        {
            if (_hmacSecretEncoding is not null)
            {
                AddExtension(Fido2ExtensionKeys.HmacSecret, _hmacSecretEncoding);
            }

            return new CborMapWriter<int>()
                .Entry(TagRp, RelyingParty.Id)
                .Entry(TagClientDataHash, ClientDataHash)
                .OptionalEntry<IReadOnlyList<ICborEncode>>(TagAllowList, CborHelpers.EncodeArrayOfObjects, AllowList)
                .OptionalEntry(TagExtensions, ParameterHelpers.EncodeKeyValues, Extensions) // TODO verify
                .OptionalEntry(TagOptions, ParameterHelpers.EncodeKeyValues, Options) // TODO verify
                .OptionalEntry(TagPinUvAuth, PinUvAuthParam)
                .OptionalEntry(TagProtocol, (int?)Protocol)
                .Encode();
        }


        /// <summary>
        /// Specify that the YubiKey should return the credBlob with the
        /// assertion. Once this extension is added to this object, it is not
        /// possible to remove it.
        /// </summary>
        /// <remarks>
        /// Because this extension is used more often, a dedicated method is
        /// provided as a convenience. Note that the credBlob extension is
        /// valid only for discoverable credentials.
        /// <para>
        /// If there is no credBlob stored with the credential, then the YubiKey
        /// will simply not return anything. It is not an error.
        /// </para>
        /// <para>
        /// The credBlob data will be with the assertion returned. It will be in
        /// the <see cref="GetAssertionData.AuthenticatorData"/> and
        /// can be retrieved using
        /// <see cref="AuthenticatorData.GetCredBlobExtension"/>
        /// </para>
        /// <para>
        /// Note that there will be a credBlob only if the credential was made
        /// with the "credBlob" extension. See
        /// <see cref="MakeCredentialParameters.AddCredBlobExtension"/>.
        /// </para>
        /// </remarks>
        public void RequestCredBlobExtension() =>
            AddExtension(Fido2ExtensionKeys.CredBlob, true);

        /// <summary>
        /// Requests the third-party payment status of a credential during an assertion.
        /// </summary>
        /// <remarks>
        /// If the credential was created with the third-party payment extension enabled, the authenticator
        /// will return `true` for this extension in the assertion response. Otherwise, it will return `false`.
        /// </remarks>
        public void RequestThirdPartyPayment() =>
            AddExtension(Fido2ExtensionKeys.ThirdPartyPayment, true);

        /// <summary>
        /// Specify that the YubiKey should return the "hmac-secret" with the
        /// assertion. Provide the salt (or salts) to use, which must be exactly
        /// 32 bytes long. Once this extension is added to this object, it is not
        /// possible to remove it, although it is possible to "change" the salt
        /// by calling this method again with a different salt.
        /// </summary>
        /// <remarks>
        /// Because this extension is used more often, a dedicated method is
        /// provided as a convenience. Note that the hmac-secret extension is
        /// valid for both discoverable and non-discoverable credentials.
        /// <para>
        /// Note that there will be an hmac-secret only if the credential was made
        /// with the "hmac-secret" extension. See
        /// <see cref="MakeCredentialParameters.AddHmacSecretExtension"/>.
        /// If the "hmac-secret" extension was not specified when making the
        /// credential, then the YubiKey will simply not return anything. It is
        /// not an error.
        /// </para>
        /// <para>
        /// If you are getting assertions using
        /// <see cref="Fido2Session.GetAssertions"/>, calling this method is
        /// sufficient, the SDK will take care of everything else needed to get
        /// the hmac-secret extension.
        /// <code language="csharp">
        ///        var gaParams = new GetAssertionParameters(relyingParty, clientDataHash);
        ///        gaParams.RequestHmacSecretExtension(salt);
        ///        IReadOnlyList&lt;GetAssertionData&gt; assertions = fido2.GetAssertions(gaParams);
        /// </code>
        /// </para>
        /// <para>
        /// But if you are getting assertions using the
        /// <see cref="Commands.GetAssertionCommand"/>, then you must call
        /// <see cref="EncodeHmacSecretExtension"/> with an appropriate instance
        /// of <see cref="PinProtocols.PinUvAuthProtocolBase"/> for which the
        /// <see cref="PinProtocols.PinUvAuthProtocolBase.Encapsulate"/> method
        /// has been successfully called. If the HmacSecret extension is not
        /// encoded, then it will not be sent to the YubiKey, and the value will
        /// not be returned.
        /// <code language="csharp">
        ///        var pinProtocol = new PinUvAuthProtocolTwo();
        ///
        ///        var keyAgreeCmd = new new GetKeyAgreementCommand(PinProtocol.Protocol);
        ///        GetKeyAgreementResponse keyAgreeRsp = Connection.SendCommand(keyAgreeCmd);
        ///        CoseEcPublicKey authenticatorPublicKey = keyAgreeRsp.GetData();
        ///
        ///        pinProtocol.Encapsulate(authenticatorPublicKey);
        ///
        ///        var getTokenCmd = new GetPinUvAuthTokenUsingPinCommand(
        ///            protocol, currentPin, PinUvAuthTokenPermissions.GetAssertion, null);
        ///        GetPinUvAuthTokenResponse getTokenRsp = Connection.SendCommand(getTokenCmd);
        ///        ReadOnlyMemory&lt;byte&gt; pinToken = getTokenRsp.GetData();
        ///        byte[] pinUvAuthParam = pinProtocol.AuthenticateUsingPinToken(
        ///            pinToken, clientDataHash);
        ///
        ///        var gaParams = new GetAssertionParameters(relyingParty, clientDataHash);
        ///        gaParams.Protocol = protocol.Protocol;
        ///        gaParams.PinUvAuthParam = pinUvAuthParam;
        ///        gaParams.AddOption("up", true);
        ///        gaParams.RequestHmacSecretExtension(salt);
        ///        gaParams.EncodeHmacSecretExtension(pinProtocol);
        ///
        ///        var cmd = new GetAssertionCommand(gaParams);
        ///        GetAssertionResponse rsp = connection.SendCommand(cmd);
        ///        GetAssertionData assertion = rsp.GetData();
        /// </code>
        /// </para>
        /// <para>
        /// The caller supplies a 32-byte salt which will be combined with the
        /// YubiKey's secret stored with the credential to produce the output. A
        /// second 32-byte salt is optional, but if provided, the YubiKey will
        /// build a second result. The standard indicates that the second secret
        /// is to be used when a secret rolls over.
        /// </para>
        /// <para>
        /// The hmac-secret data will be returned with the assertion. The result
        /// is returned in the
        /// <see cref="GetAssertionData.AuthenticatorData"/> and can be
        /// retrieved using
        /// <see cref="AuthenticatorData.GetHmacSecretExtension"/>
        /// </para>
        /// <para>
        /// References to the salt inputs will be stored in this object. If there
        /// is already salt data in the object, this method will replace the
        /// previous references with the new ones. It will also delete any
        /// encoding (see <see cref="EncodeHmacSecretExtension"/>).
        /// </para>
        /// <para>
        /// If an invalid salt is passed in, this method will throw an exception.
        /// In the unlikely event that you are replacing a salt, and you catch
        /// the exception and use the <c>GetAssertionParameters</c> object
        /// anyway, the previous salt which was being replaced will be removed as
        /// well. If <c>salt1</c> is invalid, both previous salts will be
        /// removed. For example, if there is a valid "salt1" and "salt2" in the
        /// object, and you call this method with a valid salt1 and an invalid
        /// salt2, then the original salt1 would be replaced and the original
        /// salt2 would be removed. If you catch the exception and get an
        /// assertion with this parameter object, only one hmac-secret value
        /// would be returned, one based on <c>salt1</c>.
        /// </para>
        /// </remarks>
        /// <param name="salt1">
        /// The salt the YubiKey will use in combination with the stored secret
        /// to build the resulting value.
        /// </param>
        /// <param name="salt2">
        /// An optional second salt the YubiKey can use in combination with the
        /// stored secret to build a second value. If no arg is given, the
        /// default of null will be used (the YubiKey will not build a second
        /// result).
        /// </param>
        /// <exception cref="ArgumentException">
        /// Either <c>salt1</c> is not exactly 32 bytes, or <c>salt2</c> is not
        /// null and is not exactly 32 bytes.
        /// </exception>
        public void RequestHmacSecretExtension(
            ReadOnlyMemory<byte> salt1, ReadOnlyMemory<byte>? salt2 = null)
        {
            if (salt1.Length != HmacSecret.HmacSecretSaltLength)
            {
                throw new ArgumentException(ExceptionMessages.InvalidSaltLength, nameof(salt1));
            }

            if (salt2.HasValue && salt2.Value.Length != HmacSecret.HmacSecretSaltLength)
            {
                throw new ArgumentException(ExceptionMessages.InvalidSaltLength, nameof(salt2));
            }

            _hmacSecretEncoding = null;
            _salt1 = salt1;
            _salt2 = salt2;
        }

        /// <summary>
        /// Encode the "hmac-secret" extension. This call will be valid only if
        /// the <see cref="RequestHmacSecretExtension"/> has been called, and the
        /// <see cref="PinProtocols.PinUvAuthProtocolBase.Encapsulate"/> method
        /// has been successfully called. The hmac-secret extension must be
        /// encoded before calling the <see cref="Commands.GetAssertionCommand"/>.
        /// &gt; [!NOTE]
        /// &gt; If you use <see cref="Fido2Session.GetAssertions"/> to get any
        /// &gt; assertion, you do not need to call this method.
        /// </summary>
        /// <remarks>
        /// If you want the hmac-secret extension value returned along with the
        /// assertion, then call the <c>RequestHmacSecretExtension</c> method
        /// with a salt. If you will be using the <c>GetAssertionCommand</c> to
        /// get the assertion, then you must call this method to encode the
        /// extension. The <c>authProtocol</c> you supply must be an object for
        /// which the
        /// <see cref="PinProtocols.PinUvAuthProtocolBase.Encapsulate"/> method
        /// has been called.
        /// <para>
        /// If the <c>RequestHmacSecretExtension</c> method has not been called,
        /// this method will do nothing. If it has been called, but the
        /// <c>authProtocol</c> has not been encapsulated, this method will throw
        /// an exception.
        /// </para>
        /// <para>
        /// The result of this method is stored inside this object. If there is
        /// already an encoded hmac-secret extension in the object, this method
        /// will build a new one and replace the old one. This might be necessary
        /// if a new authenticator public key is retrieved since the last time
        /// this method has been called.
        /// </para>
        /// <para>
        /// See also the documentation for the
        /// <see cref="RequestHmacSecretExtension"/> method, which also includes
        /// some code samples.
        /// </para>
        /// </remarks>
        /// <param name="authProtocol">
        /// An instance of one of the subclasses of <c>PinUvAuthProtocolBase</c>,
        /// for which the <c>Encapsulate</c> method has been called.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>authProtocol</c> arg is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The Encapsulate method for the <c>authProtocol</c> has not been
        /// called.
        /// </exception>
        public void EncodeHmacSecretExtension(PinUvAuthProtocolBase authProtocol)
        {
            if (_salt1 is null)
            {
                return;
            }

            if (authProtocol is null)
            {
                throw new ArgumentNullException(nameof(authProtocol));
                
            }
            
            if (authProtocol.EncryptionKey is null || authProtocol.PlatformPublicKey is null)
            {
                throw new InvalidOperationException(ExceptionMessages.Fido2NotEncapsulated);
            }

            // The encoded hmac-secret request info is the following
            //   A4
            //      01 platform key-agree key
            //      02 encrypted salt: either E(salt1) or E(salt1 || salt2)
            //      03 authenticate (shared secret, encrypted salt)
            //      04 pin protocol (an int, either 1 or 2)
            // Begin by encrypting the salt or salts.
            byte[] dataToEncrypt = new byte[2 * HmacSecret.HmacSecretSaltLength];
            int dataToEncryptLength = HmacSecret.HmacSecretSaltLength;

            _salt1.Value.CopyTo(dataToEncrypt.AsMemory());
            if (_salt2 is not null)
            {
                _salt2.Value.CopyTo(dataToEncrypt.AsMemory()[HmacSecret.HmacSecretSaltLength..]);
                dataToEncryptLength += HmacSecret.HmacSecretSaltLength;
            }

            byte[] encryptedSalt = authProtocol.Encrypt(dataToEncrypt, 0, dataToEncryptLength);
            byte[] authenticatedSalt = authProtocol.Authenticate(encryptedSalt);

            _hmacSecretEncoding = new CborMapWriter<int>()
                .Entry(HmacSecret.TagKeyAgreeKey, authProtocol.PlatformPublicKey)
                .Entry(HmacSecret.TagEncryptedSalt, encryptedSalt.AsMemory())
                .Entry(HmacSecret.TagAuthenticatedSalt, authenticatedSalt.AsMemory())
                .Entry(HmacSecret.TagPinProtocol, (int)authProtocol.Protocol)
                .Encode();
        }
    }
}
