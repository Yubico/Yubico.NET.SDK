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
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// The base class for some of the General Authenticate response classes,
    /// containing shared code.
    /// </summary>
    public class AuthenticateResponse : PivResponse, IYubiKeyResponseWithData<byte[]>
    {
        private const byte NestedTag = 0x7C;
        private const byte ResponseTag = 0x82;

        /// <summary>
        /// Constructs an AuthenticateResponse based on a ResponseApdu received from the
        /// YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The object containing the response APDU<br/>returned by the YubiKey.
        /// </param>
        public AuthenticateResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Gets the signature, or decrypted data, or key agreement shared secret
        /// from the YubiKey response.
        /// </summary>
        /// <remarks>
        /// Note that the data returned can be sensitive data. It is a new byte
        /// array, belonging to the caller. The caller should overwrite this
        /// memory as soon as it is no longer needed, using
        /// <c>CryptographicOperations.ZeroMemory</c>.
        /// <para>
        /// The data returned by the YubiKey is encoded as follows.
        /// <code>
        ///   7C L1 { 82 L2 result }
        /// </code>
        /// </para>
        /// <para>
        /// The <c>GetData</c> method returns the decoded data, returning the
        /// result.
        /// </para>
        /// <para>
        /// For an RSA signature, it will appear to be random bytes. There are no
        /// tags, no length octets, just the signature.
        /// </para>
        /// <para>
        /// For an ECC signature, it will be the DER encoding of
        /// <code>
        ///    SEQUENCE {
        ///      r   INTEGER,
        ///      s   INTEGER
        ///    }
        /// </code>
        /// </para>
        /// <para>
        /// If the data returned by the YubiKey is decrypted data, it is returned
        /// as the formatted plaintext.
        /// </para>
        /// <para>
        /// If the data returned by the YubiKey is the key agreement shared
        /// secret, it is the raw secret bytes.
        /// </para>
        /// <para>
        /// Note that if there is no data to return, this method will throw an
        /// exception. Even if the response indicates
        /// <c>AuthenticationRequired</c> (see the <c>Status</c> property), which
        /// means the process was not completed because the wrong or no PIN was
        /// entered, or the YubiKey was not touched within the time period. That
        /// is, it is not an error, the process is simply incomplete.
        /// Nonetheless, in that case the method will throw an exception. Hence,
        /// do not call this method unless you know that <c>Status</c> is
        /// <c>Success</c>.
        /// </para>
        /// </remarks>
        /// <returns>
        /// The signature, decrypted data, or key agreement shared secret as a
        /// byte array.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="YubiKeyResponse.Status"/> is not <see cref="ResponseStatus.Success"/>.
        /// </exception>
        public byte[] GetData() => Status switch
        {
            ResponseStatus.Success => ExtractGeneralAuthenticateResponseData(),
            _ => throw new InvalidOperationException(StatusMessage),
        };

        /// <summary>
        /// Extract the data from a GeneralAuthenticate Response APDU, returning
        /// it as a byte array.
        /// </summary>
        /// <remarks>
        /// This is to be used by the Authenticate Signature, Authenticate
        /// Decrypt, and Authenticate Key Agree classes, not the classes that
        /// authenticate a management key.
        /// <para>
        /// The response APDU for General Authenticate is encoded as follows.
        /// <code>
        ///   7c L1 { 82 L2 data }
        /// </code>
        /// The data is either the signature, the decrypted data, or the key
        /// agree shared secret. This method will decode the encoding, returning
        /// the data as a new byte array.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A new byte array containing the data portion of the encoding.
        /// </returns>
        private byte[] ExtractGeneralAuthenticateResponseData()
        {
            var tlvReader = new TlvReader(ResponseApdu.Data);
            var dataReader = tlvReader.ReadNestedTlv(NestedTag);
            var value = dataReader.ReadValue(ResponseTag);

            return value.ToArray();
        }
    }
}
