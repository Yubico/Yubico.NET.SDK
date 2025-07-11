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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// The response to verifying the temporary PIN.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the partner Response class to <see cref="VerifyTemporaryPinCommand"/>.
    /// </para>
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    ///
    /// <item>
    /// <term><see cref="ResponseStatus.Success"/></term>
    /// <description>The temporary PIN was valid and verified successfully. GetData returns <c>null</c>.</description>
    /// </item>
    ///
    /// <item>
    /// <term><see cref="ResponseStatus.AuthenticationRequired"/></term>
    /// <description>The temporary PIN was not valid. The temporary PIN is cleared from the YubiKey, and
    /// the session is not authenticated.</description>
    /// </item>
    /// </list>
    ///
    /// <para>
    /// Example:
    /// </para>
    /// <code language="csharp">
    ///   /* This example assumes the application has obtained a temporary PIN by calling
    ///    * VerifyUvCommand(true, false).
    ///    */
    ///   byte[] temporaryPin = ...;<br/>
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   var command = new VerifyTemporaryPinCommand(temporaryPin);
    ///   VerifyTemporaryPinResponse response = connection.SendCommand(command);<br/>
    ///   if (response.Status == ResponseStatus.Success)
    ///   {
    ///     /* session is authenticated */
    ///   }
    ///   else 
    ///   {
    ///     /* authentication failed, application has to retry */
    ///   }
    /// </code>
    /// </remarks>
    public sealed class VerifyTemporaryPinResponse : PivResponse
    {
        /// <summary>
        /// Constructs a VerifyTemporaryPinResponse based on a ResponseApdu received from
        /// the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The object containing the response APDU<br/>returned by the YubiKey.
        /// </param>
        public VerifyTemporaryPinResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }
    }
}
