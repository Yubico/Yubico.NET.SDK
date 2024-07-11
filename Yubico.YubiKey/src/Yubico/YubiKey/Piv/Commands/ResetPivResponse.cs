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
    /// The response to the resetting the PIV application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the partner Response class to <see cref="ResetPivCommand"/>.
    /// </para>
    /// <para>
    /// The PIV application can be reset only if both the PIN and PUK are
    /// blocked. That is, if an incorrect PIN has been entered retry count times
    /// in a row, it will be blocked. To unblock it, use the PUK (PIN Unblocking
    /// Key) with the <see cref="ResetRetryCommand"/>. If the incorrect PUK is
    /// used retry count times in a row, it will be blocked. If both are blocked,
    /// there are very few things the PIV application can do on the YubiKey any
    /// more.
    /// </para>
    /// <para>
    /// At this point, because the YubiKey's PIV application is no longer useful,
    /// the user can reset the entire application. All keys in all slots are
    /// deleted. This means those keys are no longer usable. But that was the
    /// case with both the PIN and PUK blocked, so resetting the application does
    /// not make the situation worse. But it does improve things somewhat,
    /// because you can use the PIV application again. You just need to generate
    /// new key pairs.
    /// </para>
    /// <para>
    /// After resetting the PIV application, all the asymmetric key slots (other
    /// than F9) will be empty, and the PIN, PUK, and management key will be the
    /// default values again ("123456", "12345678", and 0x0102030405060708 three
    /// times).
    /// </para>
    /// <para>
    /// To determine the result of the command, look at the
    /// <see cref="YubiKeyResponse.Status"/>. If <c>Status</c> is not
    /// one of the following values then an error has occurred.
    /// </para>
    /// <list type="table">
    /// <listheader>
    ///  <term>Status</term>
    ///  <description>Description</description>
    /// </listheader>
    ///
    /// <item>
    ///  <term><see cref="ResponseStatus.Success"/></term>
    ///  <description>The PIV application was reset successfully.</description>
    /// </item>
    ///
    /// <item>
    ///  <term><see cref="ResponseStatus.ConditionsNotSatisfied"/></term>
    ///  <description>The PIN and/or PUK was not blocked, preventing the PIV application
    ///  from being reset.</description>
    /// </item>
    /// </list>
    ///
    /// <para>
    /// Example:
    /// </para>
    /// <code language="csharp">
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);
    ///   Command resetPivCmd = new ResetPivCommand();
    ///   ResetPivResponse resetPivRsp = connection.SendCommand(resetPivCmd);<b/>
    ///   if (resetPivResponse.Status != ResponseStatus.Success)
    ///   {
    ///       // Handle error
    ///   }
    /// </code>
    /// </remarks>
    public sealed class ResetPivResponse : PivResponse
    {
        /// <summary>
        /// Constructs a ResetPivResponse based on a ResponseApdu received from
        /// the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The object containing the response APDU<br/>returned by the YubiKey.
        /// </param>
        public ResetPivResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }
    }
}
