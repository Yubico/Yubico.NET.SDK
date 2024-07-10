// Copyright 2021 Yubico AB
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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    ///     The response to the authenticate: decrypt command, containing the
    ///     plaintext result of the YubiKey's private key operation.
    /// </summary>
    /// <remarks>
    ///     This is the partner Response class to <see cref="AuthenticateDecryptCommand" />.
    ///     <para>
    ///         The data returned by <c>GetData</c> is a byte array,
    ///         containing the decrypted data. The data will be the same size as the key.
    ///         That is, for a 1024-bit RSA key, the decrypted data is 128 bytes, and for
    ///         a 2048-bit key, the decrypted data is 256 bytes.
    ///     </para>
    ///     <para>
    ///         The data returned is almost certainly formatted, either using PKCS 1 v.
    ///         1.5 or OAEP. It is the responsibility of the caller to extract the actual
    ///         plaintext from the formatted data. For example, if the data to encrypt
    ///         had originally been 32 bytes (possibly a 256-bit AES key) formatted using
    ///         PKCS 1 v.1.5, and the RSA key is 1024 bits, then the decrypted data will
    ///         look like this:
    ///     </para>
    ///     <code>
    ///   00 02 &lt;93 random, non-zero bytes&gt; 00 &lt;32-byte plaintext&gt;
    /// </code>
    ///     <para>
    ///         OAEP is much more complicated. To learn about this formatting, see RFC 8017.
    ///     </para>
    ///     <para>
    ///         <c>GetData</c> will throw an exception when the <c>Status</c>
    ///         is not <c>Success</c>. This includes when the response indicates
    ///         <c>AuthenticationRequired</c>, which
    ///         means the process was not completed because the wrong or no PIN was
    ///         entered, or the YubiKey was not touched within the time period. That
    ///         is, it is not an error, the process is simply incomplete.
    ///         Nonetheless, in that case the method will throw an exception.
    ///     </para>
    ///     <para>
    ///         Note that whether the PIN and/or touch is required depends on the PIN and
    ///         touch policies specified at the time of generation or import.
    ///     </para>
    ///     <para>
    ///         Example:
    ///     </para>
    ///     <code language="csharp">
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br />
    ///   var decryptCommand = new AuthenticateDecryptCommand(dataToDecrypt, PivSlot.KeyManagement);
    ///   AuthenticateDecryptResponse decryptResponse = connection.SendCommand(decryptCommand);<br />
    ///   if (decryptResponse.Status != ResponseStatus.Success)
    ///   {
    ///     // handle error
    ///   }
    ///   byte[] decryptedData = decryptResponse.GetData();
    /// </code>
    /// </remarks>
    public sealed class AuthenticateDecryptResponse : AuthenticateResponse
    {
        /// <summary>
        ///     Constructs an AuthenticateDecryptResponse based on a ResponseApdu received from
        ///     the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        ///     The object containing the response APDU<br />returned by the YubiKey.
        /// </param>
        public AuthenticateDecryptResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }
    }
}
