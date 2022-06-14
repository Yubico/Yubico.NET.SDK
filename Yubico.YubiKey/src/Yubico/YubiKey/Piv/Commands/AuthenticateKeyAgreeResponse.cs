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
    /// The response to the authenticate: key agree command, containing the
    /// shared secret result of the YubiKey's private key operation.
    /// </summary>
    /// <remarks>
    /// This is the partner Response class to <see cref="AuthenticateKeyAgreeCommand"/>.
    /// <para>
    /// The data returned by <c>GetData</c> is a byte array,
    /// containing the shared secret. The data will be the same size as the key.
    /// That is, for a 256-bit ECC key, the shared secret is 32 bytes, and for
    /// a 384-bit key, the share secret is 48 bytes.
    /// </para>
    /// <para>
    /// The data returned is not formatted, it is simply a byte array. It happens
    /// to be the x coordinate of an ECC point that is the result of an EC scalar
    /// muliplication operation.
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
    /// <code language="csharp">
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   var keyAgreeCommand = new AuthenticateKeyAgreeCommand(pubKeyData, PivSlot.KeyManagement);
    ///   AuthenticateDecryptResponse keyAgreeResponse = connection.SendCommand(keyAgreeCommand);<br/>
    ///   if (keyAgreeResponse.Status != ResponseStatus.Success)
    ///   {
    ///     // handle error
    ///   }
    ///   byte[] sharedSecret = keyAgreeResponse.GetData();
    /// </code>
    /// </remarks>
    public sealed class AuthenticateKeyAgreeResponse : AuthenticateResponse
    {
        /// <summary>
        /// Constructs an AuthenticateKeyAgreeResponse based on a ResponseApdu received from
        /// the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The object containing the response APDU<br/>returned by the YubiKey.
        /// </param>
        public AuthenticateKeyAgreeResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }
    }
}
