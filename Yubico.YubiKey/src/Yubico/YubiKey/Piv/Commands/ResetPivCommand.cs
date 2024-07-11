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
    /// Reset the YubiKey's PIV application
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="ResetPivResponse"/>.
    /// <para>
    /// This will delete all keys and certs in all the asymmetric key slots other
    /// than F9, and set the PIN, PUK, and management key to their default
    /// values. See the User's Manual entry on the PIV PIN, PUK, and management
    /// key for more information on this topic.
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
    /// the user can reset the entire application. All keys in all asymmetric key
    /// slots (other than F9) are deleted. This means those keys are no longer
    /// usable. But that was the case with both the PIN and PUK blocked, so
    /// resetting the application does not make the situation worse. But it does
    /// improve things somewhat, because you can use the PIV application again.
    /// You just need to generate new key pairs.
    /// </para>
    /// <para>
    /// After resetting the PIV application, all the asymmetric key slots (other
    /// than F9) will be empty, and the PIN, PUK, and management key will be the
    /// default values again ("123456", "12345678", and 0x0102030405060708 three
    /// times).
    /// </para>
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
    public sealed class ResetPivCommand : IYubiKeyCommand<ResetPivResponse>
    {
        private const byte ResetPivInstruction = 0xFB;

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs. For this
        /// command it's PIV.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Piv
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Piv;

        /// <summary>
        /// Initializes a new instance of the ResetPivCommand class. This command
        /// has no input.
        /// </summary>
        public ResetPivCommand()
        {
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() =>
            new CommandApdu
            {
                Ins = ResetPivInstruction
            };

        /// <inheritdoc />
        public ResetPivResponse CreateResponseForApdu(ResponseApdu responseApdu) => new ResetPivResponse(responseApdu);
    }
}
