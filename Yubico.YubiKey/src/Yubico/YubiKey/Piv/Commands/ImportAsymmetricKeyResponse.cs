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
    ///     The response to the import asymmetric key command, containing the result
    ///     of the import process.
    /// </summary>
    /// <remarks>
    ///     This is the partner Response class to <see cref="ImportAsymmetricKeyCommand" />.
    ///     <para>
    ///         This class does not return any Data (there is no <c>GetData</c> method).
    ///     </para>
    ///     <para>
    ///         To determine the status of the command, examine the <c>Status</c> property.
    ///         <c>ResponseStatus.Success</c> means the command executed correctly. Other
    ///         values represent various errors. For example, <c>ResponseStatus.AuthenticationRequired</c>
    ///         indicates that the user verification (management key) failed, so the command
    ///         was not successful.
    ///     </para>
    ///     <para>
    ///         Example:
    ///     </para>
    ///     <code language="csharp">
    ///   var privateKey = new PivEccPrivateKey(privateValue);
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br />
    ///   var importKeyCommand = new ImportAsymmetricKeyCommand(
    ///       privateKey, PivSlot.Signing, PivPinPolicy.Default, PivTouchPolicy.Default);
    ///   ImportAsymmetricKeyResponse importAsymmetricKeyResponse =
    ///       connection.SendCommand(importAsymmetricKeyCommand);<br />
    ///   if (importAsymmetricKeyResponse.Status != ResponseStatus.Success)
    ///   {
    ///       // Handle error
    ///   }
    ///   privateKey.Clear();
    /// </code>
    /// </remarks>
    public class ImportAsymmetricKeyResponse : PivResponse
    {
        /// <summary>
        ///     Constructs an ImportAsymmetricKeyResponse based on a ResponseApdu
        ///     received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        ///     The object containing the response APDU<br />returned by the YubiKey.
        /// </param>
        public ImportAsymmetricKeyResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }
    }
}
