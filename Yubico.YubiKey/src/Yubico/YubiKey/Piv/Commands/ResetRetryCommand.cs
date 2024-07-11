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
    /// Reset the PIN, using the PUK (PIN Unblocking Key).
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="ResetRetryResponse"/>.
    /// <para>
    /// This command is what the PUK is for. You can change the PUK, or reset the
    /// retry count on a PUK, but the only really useful operation you can do
    /// with the PUK is to reset a PIN.
    /// </para>
    /// <para>
    /// The PIN starts out as a default value: "123456", which in ASCII is the
    /// 6-byte sequence <c>0x31 32 33 34 35 36</c>. The PUK (PIN Unblocking Key)
    /// starts out as a default value as well: "12345678", which in ASCII is the
    /// 8-byte sequence <c>0x31 32 33 34 35 36 37 38</c>. Generally, the first
    /// thing done when a YubiKey is initialized for PIV is to change the PIN and
    /// PUK (along with the management key). The PIN and PUK must each be 6 to 8
    /// bytes. Ultimately the bytes that make up the PIN or PUK can be any binary
    /// value, but are generally input from a keyboard, so are usually made up of
    /// ASCII characters.
    /// </para>
    /// <para>
    /// If the user forgets the PIN, or if an incorrect PIN value has been
    /// entered too many times in a row (exhausted the retry count), it is
    /// possible to reset the PIN if the PUK is known.
    /// </para>
    /// <para>
    /// When you pass the PIN and PUK to this class, it will copy a reference to
    /// the object passed in, it will not copy the value. Because of this, you
    /// cannot overwrite the PIN and PUK until this object is done with it. It
    /// will be safe to overwrite the PIN and PUK after calling
    /// <c>connection.SendCommand</c>. See the User's Manual
    /// <xref href="UsersManualSensitive"> entry on sensitive data</xref> for
    /// more information on this topic.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code language="csharp">
    ///   using System.Security.Cryptography;<br/>
    ///   /* This example assumes the application has a method to collect a
    ///    * PIN/PUK.
    ///    */
    ///   byte[] puk;
    ///   byte[] newPin;<br/>
    ///
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   puk = CollectPuk();
    ///   newPin = CollectNewPin();
    ///   var resetRetryCommand = new ResetRetryCommand(puk, newPin);
    ///   ResetRetryResponse resetRetryResponse = connection.SendCommand(resetRetryCommand);<br/>
    ///   if (resetRetryResponse.Status != ResponseStatus.Success)
    ///   {
    ///     if (resetRetryResponse.Status == ResponseStatus.AuthenticationRequired)
    ///     {
    ///         int retryCount = resetRetryResponse.GetData();
    ///         /* report the retry count */
    ///     }
    ///     else
    ///     {
    ///         // Handle error
    ///     }
    ///   }
    ///
    ///   CryptographicOperations.ZeroMemory(puk);
    ///   CryptographicOperations.ZeroMemory(newPin);
    /// </code>
    /// </remarks>
    public sealed class ResetRetryCommand : IYubiKeyCommand<ResetRetryResponse>
    {
        private const byte PivResetRetryInstruction = 0x2C;

        private readonly ReadOnlyMemory<byte> _newPin;

        private readonly ReadOnlyMemory<byte> _puk;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        // Note that there is no object-initializer constructor. All the
        // constructor args are secret byte arrays.
        // ReSharper disable once UnusedMember.Local
        private ResetRetryCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a new Command object to reset the PIN using the PUK (PIN
        /// Unblocking Key).
        /// </summary>
        /// <remarks>
        /// To reset the PIN is to set the PIN to a new value, even if you don't
        /// know what the old value is. This is possible if you know what the PUK
        /// is. This command is similar to <see cref="ChangeReferenceDataCommand"/>.
        /// That command changes the PIN if you know the current PIN.
        /// <para>
        /// In order to reset the PIN, the caller must supply the PUK and the new
        /// PIN. In this class, the PINs and PUKs are supplied as
        /// <c>ReadOnlyMemory&lt;byte&gt;</c>. It is possible to pass a
        /// <c>byte[]</c>, because it will be automatically cast.
        /// </para>
        /// <para>
        /// This class will copy references to the PIN and PUK (not the values).
        /// This means that you can overwrite the PIN and PUK in your
        /// byte arrays only after this class is done with it. It will no longer
        /// need the PIN or PUK after calling <c>connection.SendCommand</c>.
        /// </para>
        /// <para>
        /// Both the PIN and PUK are 6 to 8 bytes long.
        /// </para>
        /// </remarks>
        /// <param name="puk">
        /// The current PUK.
        /// </param>
        /// <param name="newPin">
        /// The new PIN.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The PIN or PUK is an invalid length.
        /// </exception>
        public ResetRetryCommand(ReadOnlyMemory<byte> puk, ReadOnlyMemory<byte> newPin)
        {
            if (PivPinUtilities.IsValidPinLength(puk.Length) == false
                || PivPinUtilities.IsValidPinLength(newPin.Length) == false)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPinPukLength));
            }

            _puk = puk;
            _newPin = newPin;
        }

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs. For this
        /// command it's PIV.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Piv
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Piv;

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() =>
            new CommandApdu
            {
                Ins = PivResetRetryInstruction,
                P2 = PivSlot.Pin,
                Data = PivPinUtilities.CopyTwoPinsWithPadding(_puk, _newPin)
            };

        /// <inheritdoc />
        public ResetRetryResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ResetRetryResponse(responseApdu);
    }
}
