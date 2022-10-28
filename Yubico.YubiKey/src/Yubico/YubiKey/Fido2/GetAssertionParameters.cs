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
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// This collects and encodes the information needed to get a FIDO2 assertion.
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
    public class GetAssertionParameters : ICborEncode
    {
        private const int TagRp = 1;
        private const int TagClientDataHash = 2;
        private const int TagAllowList = 3;
        private const int TagExtensions = 4;
        private const int TagOptions = 5;
        private const int TagPinUvAuth = 6;
        private const int TagProtocol = 7;

        private List<CredentialId>? _allowList;
        private Dictionary<string, byte[]>? _extensions;
        private Dictionary<string, bool>? _options;

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
        /// new assertion. This is an optional parameter, so it can be null.
        /// </summary>
        /// <remarks>
        /// To add an entry to the list, call <see cref="AllowCredential"/>.
        /// </remarks>
        public IReadOnlyList<CredentialId>? AllowList => _allowList;

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
        /// The standard lists two option keys: "up" and "uv". Any other option
        /// on a YubiKey will yield an error. In addition, YubiKeys that are not
        /// BIO series will not allow "uv".
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
        /// Add an entry to the allow list.
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
        public void AddExtension(string extensionKey, byte[] encodedValue) => _extensions =
            ParameterHelpers.AddKeyValue<byte[]>(extensionKey, encodedValue, _extensions);

        /// <summary>
        /// Add an entry to the list of options.
        /// </summary>
        /// <remarks>
        /// If the <c>Options</c> list already contains an entry with the given
        /// <c>optionKey</c>, this method will replace it.
        /// <para>
        /// The standard lists two option keys: "up" and "uv". Any other option
        /// on a YubiKey will yield an error. In addition, YubiKeys that are not
        /// BIO series will not allow "uv".
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
                .Entry(TagRp, RelyingParty.Id)
                .Entry(TagClientDataHash, ClientDataHash)
                .OptionalEntry<IReadOnlyList<ICborEncode>>(TagAllowList, CborHelpers.EncodeArrayOfObjects, AllowList)
                .OptionalEntry<Dictionary<string, byte[]>>(TagExtensions, ParameterHelpers.EncodeKeyValues<byte[]>, _extensions)
                .OptionalEntry<Dictionary<string, bool>>(TagOptions, ParameterHelpers.EncodeKeyValues<bool>, _options)
                .OptionalEntry(TagPinUvAuth, PinUvAuthParam)
                .OptionalEntry(TagProtocol, (int?)Protocol)
                .EndMap();

            return cbor.Encode();
        }
   }
}
