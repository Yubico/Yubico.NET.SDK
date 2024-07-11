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
    ///     The response to the setting the number of retries for the PIN or PUK.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is the partner Response class to <see cref="SetPinRetriesCommand" />.
    ///     </para>
    ///     <para>
    ///         To determine the result of the command, look at the
    ///         <see cref="YubiKeyResponse.Status" />. If <c>Status</c> is not
    ///         one of the following values then an error has occurred.
    ///     </para>
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Status</term>
    ///             <description>Description</description>
    ///         </listheader>
    ///         <item>
    ///             <term>
    ///                 <see cref="ResponseStatus.Success" />
    ///             </term>
    ///             <description>The retry count successfully set.</description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <see cref="ResponseStatus.AuthenticationRequired" />
    ///             </term>
    ///             <description>The Management key or PIN did not verify.</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         Example:
    ///     </para>
    ///     <code language="csharp">
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br />
    ///   var setPinRetriesCommand = new SetPinRetriesCommand (5, 5);
    ///   SetPinRetriesResponse setPinRetriesResponse =
    ///       connection.SendCommand(setPinRetriesCommand);<br />
    ///   if (setPinRetriesResponse.Status != ResponseStatus.Success)
    ///   {
    ///     // Handle error
    ///   }
    /// </code>
    /// </remarks>
    public sealed class SetPinRetriesResponse : PivResponse
    {
        /// <summary>
        ///     Constructs a SetPinRetriesResponse based on a ResponseApdu received from
        ///     the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        ///     The object containing the response APDU<br />returned by the YubiKey.
        /// </param>
        public SetPinRetriesResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }
    }
}
