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

using System;
using System.Globalization;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// Set the number of retries for the PIN and PUK.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="SetPinRetriesResponse"/>.
    /// <para>
    /// Note that this command will reset the PIN and PUK to their default
    /// values ("123456" for the PIN and "12345678" for the PUK), as well as
    /// changing the retry count. You will likely want to follow up this command
    /// with a call to <see cref="ChangeReferenceDataCommand"/>
    /// </para>
    /// <para>
    /// In order to set the retry count, you must authenticate the management key
    /// and verify the PIN. Those two elements are not part of this command. See
    /// the User's Manual entry on
    /// <xref href="UsersManualPivAccessControl"> PIV commands access control</xref>
    /// For information on how to provide authentication for a command that does
    /// not include the authentication information in the command.
    ///</para>
    /// <para>
    /// The number of retries refers to how many times in a row the wrong value
    /// can be entered until the element is blocked. For example, suppose the PIN
    /// retry count is three. If you perform an operation or command that
    /// requires the PIN, and you provide the wrong PIN, the operation or command
    /// will not succeed. The retry count will drop to two. If you enter the
    /// wrong PIN two more times, the PIN is blocked. Any operation or command
    /// that requires the PIN will not work, even if you supply the correct PIN.
    /// </para>
    /// <para>
    /// The YubiKey is manufactured with the default PIN and PUK counts of 3.
    /// </para>
    /// <para>
    /// Note that if a PIN is blocked, it is possible to unblock it using the PUK
    /// and the <see cref="ResetRetryCommand"/>. If that command is performed
    /// with the wrong PUK, the retry count for the PUK will be decremented.
    /// After too many wrong PUKs, it can also be blocked. In that case, the only
    /// possible recovery is to reset the entire PIV application.
    /// </para>
    /// <para>
    /// The Set Retries command will set the retry count for both the PIN and
    /// PUK. If you want to reset the retry count for one, not the other, you
    /// still have to set the count for both.
    /// </para>
    /// <para>
    /// The retry count must be a value from 1 to 255. Note that if you set the
    /// retry count to one, that means that after one wrong entry, the PIN or PUK
    /// is blocked.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code language="csharp">
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   var setPinRetriesCommand = new SetPinRetriesCommand (5, 5);
    ///   SetPinRetriesResponse setPinRetriesResponse =
    ///       connection.SendCommand(setPinRetriesCommand);<br/>
    ///   if (setPinRetriesResponse.Status != ResponseStatus.Success)
    ///   {
    ///     // Handle error
    ///   }
    /// </code>
    /// </remarks>
    public sealed class SetPinRetriesCommand : IYubiKeyCommand<SetPinRetriesResponse>
    {
        private const byte PivSetPinRetriesInstruction = 0xFA;

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs. For this
        /// command it's PIV.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Piv
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Piv;

        private byte _pinRetryCount;
        private byte _pukRetryCount;

        private const byte DefaultPinRetryCount = 3;
        private const byte DefaultPukRetryCount = 3;

        /// <summary>
        /// The number of retries before the PIN will be blocked.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The PIN retry count is invalid.
        /// </exception>
        public byte PinRetryCount
        {
            get => _pinRetryCount;
            set
            {
                if (value == 0)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidPinPukRetryCount));
                }
                _pinRetryCount = value;
            }
        }

        /// <summary>
        /// The number of retries before the PUK will be blocked.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The PUK retry count is invalid.
        /// </exception>
        public byte PukRetryCount
        {
            get => _pukRetryCount;
            set
            {
                if (value == 0)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidPinPukRetryCount));
                }
                _pukRetryCount = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the SetPinRetriesCommand class. This command
        /// takes the PIN and PUK retry counts as input.
        /// </summary>
        /// <remarks>
        /// The retry count must be a value from 1 to 255 (inclusive).
        /// </remarks>
        /// <param name="pinRetryCount">
        /// The new number of retries for the PIN (minimum 1, maximum 255).
        /// </param>
        /// <param name="pukRetryCount">
        /// The new number of retries for the PUK (minimum 1, maximum 255).
        /// </param>
        public SetPinRetriesCommand(byte pinRetryCount, byte pukRetryCount)
        {
            PinRetryCount = pinRetryCount;
            PukRetryCount = pukRetryCount;
        }

        /// <summary>
        /// Initializes a new instance of the <c>SetPinRetriesCommand</c> class.
        /// This command will set the <c>PinRetryCount</c> and
        /// <c>PukRetryCount</c> to the default count of 3.
        /// </summary>
        /// <remarks>
        /// This constructor is provided for those developers who want to use the
        /// object initializer pattern. For example:
        /// <code language="csharp">
        ///   var command = new SetPinRetriesCommand()
        ///   {
        ///       PinRetryCount = 5,
        ///       PukRetryCount = 2,
        ///   };
        /// </code>
        /// </remarks>
        public SetPinRetriesCommand()
        {
            PinRetryCount = DefaultPinRetryCount;
            PukRetryCount = DefaultPukRetryCount;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => new CommandApdu
        {
            Ins = PivSetPinRetriesInstruction,
            P1 = _pinRetryCount,
            P2 = _pukRetryCount,
        };

        /// <inheritdoc />
        public SetPinRetriesResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
          new SetPinRetriesResponse(responseApdu);
    }
}
