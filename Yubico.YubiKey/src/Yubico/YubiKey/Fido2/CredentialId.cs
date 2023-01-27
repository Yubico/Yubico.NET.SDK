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

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// A FIDO2 <c>credentialId</c>, consisting of type, ID, and transports.
    /// </summary>
    /// <remarks>
    /// A credential ID is how credentials can be identified. That is, there
    /// should be a one-to-one correspondence between credentials and
    /// <c>credentialIds</c>. When you make a new credential, the YubiKey will
    /// build a <c>credentialId</c> and store the credential against this value.
    /// Later on, you can enumerate the credentials on a YubiKey, which will
    /// return each <c>credentialId</c>.
    /// <para>
    /// The FIDO2 standard defines a "credentialId" as a
    /// <c>PublicKeyCredentialDescriptor</c>, which is defined in the W3C
    /// standard. The W3C standard defines a <c>PublicKeyCredentialDescriptor</c>
    /// as a "dictionary" consisting of a <c>type</c>, <c>id</c>, and an optional
    /// sequence of <c>transports</c>. The W3C standard further defines the
    /// <c>id</c> as a "Credential ID". That is, there is a "credentialId" in
    /// FIDO2 and a "Credential ID" in W3C, however, they are not the same thing.
    /// This class is a FIDO2 "credentialId".
    /// </para>
    /// <para>
    /// Currently only one <c>type</c> is supported: the string "public-key".
    /// However, the standard also allows authenticators to support non-standard
    /// values.
    /// </para>
    /// <para>
    /// The <c>id</c> is a byte array. It can be random (at least 16 bytes long),
    /// or it can be encrypted identifying data.
    /// </para>
    /// <para>
    /// The transports are defined as a sequence (list) of supported strings
    /// describing transport methods. Currently, a list of transports will be a
    /// subset of the following strings: "usb", "nfc", "ble", "hybrid", and
    /// "internal".
    /// </para>
    /// <para>
    /// The two or three elements that make up a <c>credentialId</c> can be
    /// CBOR-encoded into a single byte array. For example, when a YubiKey
    /// returns a <c>credentialId</c> (e.g. when enumerating), it is encoded. To
    /// decode the value into its component parts, use this class.
    /// </para>
    /// </remarks>
    public class CredentialId : ICborEncode
    {
        private const string TagType = "type";
        private const string TagId = "id";
        private const string TagTransports = "transports";

        private List<string>? _transports;

        /// <summary>
        /// The <c>type</c> component of the <c>credentialId</c>.
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
        /// standard type, and is allowed to support only the standard type, but
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
        /// The <c>id</c> component of the <c>credentialId</c>.
        /// </summary>
        public ReadOnlyMemory<byte> Id { get; set; }

        /// <summary>
        /// The <c>transports</c> component of the <c>credentialId</c>. This is
        /// an optional parameter, so it can be null.
        /// </summary>
        /// <remarks>
        /// The <c>transports</c> component of the <c>credentialId</c> can
        /// contain more than one transport.  To add an entry to the list, call
        /// <see cref="AddTransport"/>.
        /// <para>
        /// The standard defines some strings, and allows for vendor- or
        /// application-defined values as well. The standard-defined strings are
        /// in the class <see cref="AuthenticatorTransports"/>.
        /// </para>
        /// </remarks>
        public IReadOnlyList<string>? Transports => _transports;

        /// <summary>
        /// Constructs a new instance of <see cref="CredentialId"/>.
        /// </summary>
        public CredentialId()
        {
            Type = "public-key";
        }

        /// <summary>
        /// Constructs a new instance of <see cref="CredentialId"/> from the
        /// <c>encodedCredentialId</c>.
        /// </summary>
        /// <remarks>
        /// This constructor expects the encoding to follow this Cbor template.
        /// <code>
        ///    map {
        ///      "type"        --text string--
        ///      "id"          --text string--
        ///      "transports"  --array of strings-- (optional)
        ///    }
        /// </code>
        /// </remarks>
        /// <param name="encodedCredentialId">
        /// The Cbor encoding of the credential ID.
        /// </param>
        /// <param name="bytesRead">
        /// The constructor will return the number of bytes read.
        /// </param>
        /// <exception cref="Ctap2DataException">
        /// The <c>encodedCredentialId</c> is not a correct encoding.
        /// </exception>
        public CredentialId(ReadOnlyMemory<byte> encodedCredentialId, out int bytesRead)
        {
            var cborMap = new CborMap<string>(encodedCredentialId);
            Type = cborMap.ReadTextString(TagType);
            Id = cborMap.ReadByteString(TagId);
            if (cborMap.Contains(TagTransports))
            {
                IReadOnlyList<string> transportArray = cborMap.ReadArray<string>(TagTransports);
                foreach (string entry in transportArray)
                {
                    AddTransport(entry);
                }
            }
            bytesRead = cborMap.BytesRead;
        }

        /// <summary>
        /// Add an entry to the list of transports.
        /// </summary>
        /// <remarks>
        /// If there is no list yet when this method is called, one will be
        /// created. That is, even if the <see cref="Transports"/> property is
        /// null, you can call the method to add an entry.
        /// <para>
        /// The standard defines some specific strings to use with some
        /// transports. These specific strings are defined in the
        /// <see cref="AuthenticatorTransports"/> static class. For example, to
        /// add the USB transport, call
        /// <code>
        ///    credentialId.AddTransport(AuthenticatorTransports.Usb);
        /// </code>
        /// </para>
        /// <para>
        /// The standard also specifies that is is permissible to add
        /// non-standard transports.
        /// </para>
        /// </remarks>
        /// <param name="transport">
        /// The transport to add.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>transport</c> arg is null.
        /// </exception>
        public void AddTransport(string transport)
        {
            if (transport is null)
            {
                throw new ArgumentNullException(nameof(transport));
            }

            _transports ??= new List<string>();
            // If the transport is already in the list, don't add it again.
            if (!_transports.Contains(transport))
            {
                _transports.Add(transport);
            }
         }

        /// <inheritdoc/>
        public byte[] CborEncode()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);

            cbor.WriteStartMap(null);
            cbor.WriteTextString(TagType);
            cbor.WriteTextString(Type);
            cbor.WriteTextString(TagId);
            cbor.WriteByteString(Id.Span);
            if (!(Transports is null))
            {
                cbor.WriteTextString(TagTransports);
                cbor.WriteEncodedValue(CborHelpers.EncodeStringArray(Transports));
            }
            cbor.WriteEndMap();

            return cbor.Encode();
        }
    }
}
