// Copyright 2023 Yubico AB
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
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2
{
    // This portion of the GetAssertionParameters class contains code for dealing
    // with certain extensions. These are "convenience" methods, making it easier
    // to deal with common extensions.
    public partial class GetAssertionParameters
    {
        // These are the CBOR tags for the elements that make up the encoded HMAC
        // secret extension. When requesting the hmac-secret, we add the
        // extension with the key/value pair of "hmac-secret"/encoded-salt. The
        // encoded-salt is a map of key/value pairs where each key is an int, or
        // Tag. These are the tags for the encoded-salt.
        private const int TagKeyAgreeKey = 1;
        private const int TagEncryptedSalt = 2;
        private const int TagAuthenticatedSalt = 3;
        private const int TagPinProtocol = 4;

        private const int HmacSecretSaltLength = 32;

        // These are the keys of key/value for the credBlob and hmac-secret
        // extensions.
        private const string KeyCredBlob = "credBlob";
        private const string KeyHmacSecret = "hmac-secret";
        private byte[]? _hmacSecretEncoding;

        private ReadOnlyMemory<byte>? _salt1;
        private ReadOnlyMemory<byte>? _salt2;

        /// <summary>
        ///     Specify that the YubiKey should return the credBlob with the
        ///     assertion. Once this extension is added to this object, it is not
        ///     possible to remove it.
        /// </summary>
        /// <remarks>
        ///     Because this extension is used more often, a dedicated method is
        ///     provided as a convenience. Note that the credBlob extension is
        ///     valid only for discoverable credentials.
        ///     <para>
        ///         If there is no credBlob stored with the credential, then the YubiKey
        ///         will simply not return anything. It is not an error.
        ///     </para>
        ///     <para>
        ///         The credBlob data will be with the assertion returned. It will be in
        ///         the <see cref="GetAssertionData.AuthenticatorData" /> and
        ///         can be retrieved using
        ///         <see cref="AuthenticatorData.GetCredBlobExtension" />
        ///     </para>
        ///     <para>
        ///         Note that there will be a credBlob only if the credential was made
        ///         with the "credBlob" extension. See
        ///         <see cref="MakeCredentialParameters.AddCredBlobExtension" />.
        ///     </para>
        /// </remarks>
        public void RequestCredBlobExtension() => AddExtension(KeyCredBlob, new byte[] { 0xF5 });

        /// <summary>
        ///     Specify that the YubiKey should return the "hmac-secret" with the
        ///     assertion. Provide the salt (or salts) to use, which must be exactly
        ///     32 bytes long. Once this extension is added to this object, it is not
        ///     possible to remove it, although it is possible to "change" the salt
        ///     by calling this method again with a different salt.
        /// </summary>
        /// <remarks>
        ///     Because this extension is used more often, a dedicated method is
        ///     provided as a convenience. Note that the hmac-secret extension is
        ///     valid for both discoverable and non-discoverable credentials.
        ///     <para>
        ///         Note that there will be an hmac-secret only if the credential was made
        ///         with the "hmac-secret" extension. See
        ///         <see cref="MakeCredentialParameters.AddHmacSecretExtension" />.
        ///         If the "hmac-secret" extension was not specified when making the
        ///         credential, then the YubiKey will simply not return anything. It is
        ///         not an error.
        ///     </para>
        ///     <para>
        ///         If you are getting assertions using
        ///         <see cref="Fido2Session.GetAssertions" />, calling this method is
        ///         sufficient, the SDK will take care of everything else needed to get
        ///         the hmac-secret extension.
        ///         <code language="csharp">
        ///        var gaParams = new GetAssertionParameters(relyingParty, clientDataHash);
        ///        gaParams.RequestHmacSecretExtension(salt);
        ///        IReadOnlyList&lt;GetAssertionData&gt; assertions = fido2.GetAssertions(gaParams);
        /// </code>
        ///     </para>
        ///     <para>
        ///         But if you are getting assertions using the
        ///         <see cref="Commands.GetAssertionCommand" />, then you must call
        ///         <see cref="EncodeHmacSecretExtension" /> with an appropriate instance
        ///         of <see cref="PinProtocols.PinUvAuthProtocolBase" /> for which the
        ///         <see cref="PinProtocols.PinUvAuthProtocolBase.Encapsulate" /> method
        ///         has been successfully called. If the HmacSecret extension is not
        ///         encoded, then it will not be sent to the YubiKey, and the value will
        ///         not be returned.
        ///         <code language="csharp">
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
        ///     </para>
        ///     <para>
        ///         The caller supplies a 32-byte salt which will be combined with the
        ///         YubiKey's secret stored with the credential to produce the output. A
        ///         second 32-byte salt is optional, but if provided, the YubiKey will
        ///         build a second result. The standard indicates that the second secret
        ///         is to be used when a secret rolls over.
        ///     </para>
        ///     <para>
        ///         The hmac-secret data will be returned with the assertion. The result
        ///         is returned in the
        ///         <see cref="GetAssertionData.AuthenticatorData" /> and can be
        ///         retrieved using
        ///         <see cref="AuthenticatorData.GetHmacSecretExtension" />
        ///     </para>
        ///     <para>
        ///         References to the salt inputs will be stored in this object. If there
        ///         is already salt data in the object, this method will replace the
        ///         previous references with the new ones. It will also delete any
        ///         encoding (see <see cref="EncodeHmacSecretExtension" />).
        ///     </para>
        ///     <para>
        ///         If an invalid salt is passed in, this method will throw an exception.
        ///         In the unlikely event that you are replacing a salt, and you catch
        ///         the exception and use the <c>GetAssertionParameters</c> object
        ///         anyway, the previous salt which was being replaced will be removed as
        ///         well. If <c>salt1</c> is invalid, both previous salts will be
        ///         removed. For example, if there is a valid "salt1" and "salt2" in the
        ///         object, and you call this method with a valid salt1 and an invalid
        ///         salt2, then the original salt1 would be replaced and the original
        ///         salt2 would be removed. If you catch the exception and get an
        ///         assertion with this parameter object, only one hmac-secret value
        ///         would be returned, one based on <c>salt1</c>.
        ///     </para>
        /// </remarks>
        /// <param name="salt1">
        ///     The salt the YubiKey will use in combination with the stored secret
        ///     to build the resulting value.
        /// </param>
        /// <param name="salt2">
        ///     An optional second salt the YubiKey can use in combination with the
        ///     stored secret to build a second value. If no arg is given, the
        ///     default of null will be used (the YubiKey will not build a second
        ///     result).
        /// </param>
        /// <exception cref="ArgumentException">
        ///     Either <c>salt1</c> is not exactly 32 bytes, or <c>salt2</c> is not
        ///     null and is not exactly 32 bytes.
        /// </exception>
        public void RequestHmacSecretExtension(ReadOnlyMemory<byte> salt1, ReadOnlyMemory<byte>? salt2 = null)
        {
            _hmacSecretEncoding = null;

            // If one or both salts are invalid, we will throw an exception. But
            // we will replace _salt1 with the new salt1 if it is valid.
            //
            // If input salt1 is valid, and input salt2 is valid, we'll set
            // _salt1 to salt1 and _salt2 to salt2.
            //
            // If input salt1 is valid, but input salt2 is not valid, we'll set
            // _salt1 to salt1 and _salt2 to null.
            //
            // If input salt1 is invalid, we'll set _salt1 to null. If salt2 is
            // valid, it doesn't matter, because it is not possible to get an
            // hmac-secret with only salt2, so _salt2 will be null as well.

            if (salt1.Length == HmacSecretSaltLength)
            {
                int s2Len = salt2 is null
                    ? HmacSecretSaltLength
                    : salt2.Value.Length;

                _salt1 = salt1;
                if (s2Len == HmacSecretSaltLength)
                {
                    _salt2 = salt2;
                    return;
                }
            }
            else
            {
                _salt1 = null;
            }

            _salt2 = null;

            throw new ArgumentException(ExceptionMessages.InvalidSaltLength);
        }

        /// <summary>
        ///     Encode the "hmac-secret" extension. This call will be valid only if
        ///     the <see cref="RequestHmacSecretExtension" /> has been called, and the
        ///     <see cref="PinProtocols.PinUvAuthProtocolBase.Encapsulate" /> method
        ///     has been successfully called. The hmac-secret extension must be
        ///     encoded before calling the <see cref="Commands.GetAssertionCommand" />.
        ///     &gt; [!NOTE]
        ///     &gt; If you use <see cref="Fido2Session.GetAssertions" /> to get any
        ///     &gt; assertion, you do not need to call this method.
        /// </summary>
        /// <remarks>
        ///     If you want the hmac-secret extension value returned along with the
        ///     assertion, then call the <c>RequestHmacSecretExtension</c> method
        ///     with a salt. If you will be using the <c>GetAssertionCommand</c> to
        ///     get the assertion, then you must call this method to encode the
        ///     extension. The <c>authProtocol</c> you supply must be an object for
        ///     which the
        ///     <see cref="PinProtocols.PinUvAuthProtocolBase.Encapsulate" /> method
        ///     has been called.
        ///     <para>
        ///         If the <c>RequestHmacSecretExtension</c> method has not been called,
        ///         this method will do nothing. If it has been called, but the
        ///         <c>authProtocol</c> has not been encapsulated, this method will throw
        ///         an exception.
        ///     </para>
        ///     <para>
        ///         The result of this method is stored inside this object. If there is
        ///         already an encoded hmac-secret extension in the object, this method
        ///         will build a new one and replace the old one. This might be necessary
        ///         if a new authenticator public key is retrieved since the last time
        ///         this method has been called.
        ///     </para>
        ///     <para>
        ///         See also the documentation for the
        ///         <see cref="RequestHmacSecretExtension" /> method, which also includes
        ///         some code samples.
        ///     </para>
        /// </remarks>
        /// <param name="authProtocol">
        ///     An instance of one of the subclasses of <c>PinUvAuthProtocolBase</c>,
        ///     for which the <c>Encapsulate</c> method has been called.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The <c>authProtocol</c> arg is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     The Encapsulate method for the <c>authProtocol</c> has not been
        ///     called.
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
            byte[] dataToEncrypt = new byte[2 * HmacSecretSaltLength];
            int dataToEncryptLength = HmacSecretSaltLength;

            _salt1.Value.CopyTo(dataToEncrypt.AsMemory());
            if (!(_salt2 is null))
            {
                _salt2.Value.CopyTo(dataToEncrypt.AsMemory().Slice(HmacSecretSaltLength));
                dataToEncryptLength += HmacSecretSaltLength;
            }

            byte[] encryptedSalt = authProtocol.Encrypt(dataToEncrypt, offset: 0, dataToEncryptLength);

            byte[] authenticatedSalt = authProtocol.Authenticate(encryptedSalt);

            _hmacSecretEncoding = new CborMapWriter<int>()
                .Entry(TagKeyAgreeKey, authProtocol.PlatformPublicKey)
                .Entry(TagEncryptedSalt, encryptedSalt.AsMemory())
                .Entry(TagAuthenticatedSalt, authenticatedSalt.AsMemory())
                .Entry(TagPinProtocol, (int)authProtocol.Protocol)
                .Encode();
        }
    }
}
