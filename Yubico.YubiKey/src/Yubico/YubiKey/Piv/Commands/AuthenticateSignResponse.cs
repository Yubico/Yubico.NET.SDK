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
    /// The response to the authenticate: sign command, containing the signature
    /// built by the YubiKey.
    /// </summary>
    /// <remarks>
    /// This is the partner Response class to <see cref="AuthenticateSignCommand"/>.
    /// <para>
    /// The data returned by <c>GetData</c> is a <c>byte[]</c>. The caller now
    /// owns that data and can overwrite the buffer when done with it.
    /// </para>
    /// <para>
    /// If the data had been signed by an RSA key, the data will be
    /// random-looking data the same size as the key. That is, for a 1024-bit RSA
    /// key, the signature is 128 bytes, and for a 2048-bit key, the signature is
    /// 256 bytes.
    /// </para>
    /// <para>If the data had been signed by an ECC key, the signature will be the
    /// DER encoding of the following ASN.1 definition.
    /// <code>
    ///    SEQUENCE {
    ///      r   INTEGER,
    ///      s   INTEGER
    ///    }
    /// </code>
    /// Both r and s are the same size as the key, so will be 32 (ECC-P256), or 48
    /// (ECC-P384) bytes long. It is possible that the encoding of r or s will be
    /// one extra bytes (a leading 00 byte), and it can be shorter. For example,
    /// the DER encoding can look like these:
    /// <code>
    ///   30 44
    ///      02 20
    ///         61 0C ... B3       &lt;-- 32 bytes
    ///      02 20
    ///         59 EA ... 52       &lt;-- 32 bytes
    ///   30 64
    ///      02 30
    ///         7f 22 ... 10       &lt;-- 48 bytes
    ///      02 30
    ///         29 F1 ... 41       &lt;-- 48 bytes
    ///   30 65
    ///      02 31
    ///         00 B3 47 ... 9C    &lt;-- 49 bytes
    ///      02 30
    ///         59 2D ... D8       &lt;-- 48 bytes
    /// </code>
    /// </para>
    /// <para>
    /// <c>GetData</c> will throw an exception when the <c>Status</c>
    /// is not <c>Success</c>. This includes when the response indicates
    /// <c>AuthenticationRequired</c>, which
    /// means the process was not completed because the wrong or no PIN was
    /// entered, or the YubiKey was not touched within the time period. That
    /// is, it is not an error, the process is simply incomplete.
    /// Nonetheless, in that case the method will throw an exception.
    /// </para>
    /// <para>
    /// Note that whether the PIN and/or touch is required depends on the PIN and
    /// touch policies specified at the time of generation or import.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    ///   /* This example assumes there is some code that will digest the data. */
    ///   byte[] sha384Digest = DigestDataToSign(SHA384, dataToSign);<br/>
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   var signCommand = new AuthenticateSignCommand(sha384Digest, PivSlot.Signing);
    ///   AuthenticateSignResponse signResponse = connection.SendCommand(signCommand);<br/>
    ///   if (signResponse.Status != ResponseStatus.Success)
    ///   {
    ///     // handle error
    ///   }
    ///   byte[] signature = signResponse.GetData();
    /// </code>
    /// </remarks>
    public sealed class AuthenticateSignResponse : AuthenticateResponse
    {
        /// <summary>
        /// Constructs an AuthenticateSignResponse based on a ResponseApdu received from
        /// the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The object containing the response APDU<br/>returned by the YubiKey.
        /// </param>
        public AuthenticateSignResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }
    }
}
