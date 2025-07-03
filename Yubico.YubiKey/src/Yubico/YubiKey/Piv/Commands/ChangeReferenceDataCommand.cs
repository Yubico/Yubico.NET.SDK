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

using System;
using System.Globalization;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// Change the PIN or PUK.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="ChangeReferenceDataResponse"/>.
    /// <para>
    /// The PIN starts out as a default value: "123456", which in ASCII is the
    /// 6-byte sequence <c>0x31 32 33 34 35 36</c>. The PUK (PIN Unblocking Key)
    /// starts out as a default value as well: "12345678", which in ASCII is the
    /// 8-byte sequence <c>0x31 32 33 34 35 36 37 38</c>. Generally, the first
    /// thing done when a YubiKey is initialized for PIV is to change the PIN and
    /// PUK (along with the management key). 
    /// The PIN and PUK are both allowed to be 6 to 8 characters/bytes. The PIN can be any ASCII character. For YubiKeys with firmware versions prior to 5.7, the PUK is allowed to be any character in the <c>0x00</c> - <c>0xFF</c> range for a total length of 6-8 bytes. For YubiKeys with firmware version 5.7 and above, the PUK is allowed to be any character in the <c>0x00</c> - <c>0x7F</c> range for a total length of 6-8 Unicode code points.
    /// Since the PIN and PUK are generally input from a keyboard, they are usually made up of ASCII
    /// characters.
    /// </para>
    /// <para>
    /// When you pass a PIN or PUK to this class (the PIN or PUK to change, along
    /// with the new value), the class will copy a reference to the object passed
    /// in, it will not copy the value. Because of this, you cannot overwrite the
    /// PIN until this object is done with it. It will be safe to overwrite the
    /// PIN after calling <c>connection.SendCommand</c>. See the User's Manual
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
    ///   byte[] oldPuk;
    ///   byte[] newPuk;<br/>
    ///
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   oldPuk = CollectPuk();
    ///   newPuk = CollectNewPuk();
    ///   var changeReferenceDataCommand =
    ///       new ChangeReferenceDataCommand(PivSlot.Puk, oldPuk, newPuk);
    ///   ChangeReferenceDataResponse changeReferenceDataResponse =
    ///       connection.SendCommand(changeReferenceDataCommand);<br/>
    ///   if (changeReferenceDataResponse.Status != ResponseStatus.Success)
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
    ///   CryptographicOperations.ZeroMemory(newPuk);
    /// </code>
    /// <para>
    /// Note: YubiKey Bio Multi-protocol Edition (MPE) keys do not have a PUK. 
    /// </para>
    /// </remarks>
    public sealed class ChangeReferenceDataCommand : IYubiKeyCommand<ChangeReferenceDataResponse>
    {
        private const byte PivChangeReferenceInstruction = 0x24;

        // This is needed so we can make the check on the set of the property.
        private byte _slotNumber;

        private readonly ReadOnlyMemory<byte> _currentValue;

        private readonly ReadOnlyMemory<byte> _newValue;

        /// <summary>
        /// The slot for the PIN or PUK.
        /// </summary>
        /// <value>
        /// The slot number, see <see cref="PivSlot"/>
        /// </value>
        /// <exception cref="ArgumentException">
        /// The slot specified is not valid for changing reference data.
        /// </exception>
        public byte SlotNumber
        {
            get => _slotNumber;
            set
            {
                if (value != PivSlot.Pin && value != PivSlot.Puk)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidSlot,
                            value));
                }
                _slotNumber = value;
            }
        }

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs. For this
        /// command it's PIV.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Piv
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Piv;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        // Note that there is no object-initializer constructor. All the
        // constructor inputs have no default or are secret byte arrays.
        private ChangeReferenceDataCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a new Command object to "change reference data", which means to
        /// change a PIN or PUK.
        /// </summary>
        /// <remarks>
        /// In order to change a PIN or PUK, the caller must supply the old and
        /// new PIN or PUK. In this class, the PINs and PUKs are supplied as
        /// <c>ReadOnlyMemory&lt;byte&gt;</c>. It is possible to pass a
        /// <c>byte[]</c>, because it will be automatically cast.
        /// <para>
        /// This class will copy references to the PINs and PUKs (not the values).
        /// This means that you can overwrite the PIN or PUK in your byte array
        /// only after this class is done with it. It will no longer need the PIN
        /// or PUK after calling <c>connection.SendCommand</c>.
        /// </para>
        /// <para>
        /// The PIN and PUK are both allowed to be 6 to 8 characters/bytes. The PIN can be any ASCII character. For YubiKeys with firmware versions prior to 5.7, the PUK is allowed to be any character in the <c>0x00</c> - <c>0xFF</c> range for a total length of 6-8 bytes. For YubiKeys with firmware version 5.7 and above, the PUK is allowed to be any character in the <c>0x00</c> - <c>0x7F</c> range for a total length of 6-8 Unicode code points.
        /// </para>
        /// </remarks>
        /// <param name="slotNumber">
        /// Which element to change, the PIN or PUK. Use <c>PivSlot.Pin</c> or
        /// <c>PivSlot.Puk</c>
        /// </param>
        /// <param name="currentValue">
        /// The current PIN or PUK, the value to change.
        /// </param>
        /// <param name="newValue">
        /// The new PIN or PUK.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The PIN or PUK is an incorrect length.
        /// </exception>
        public ChangeReferenceDataCommand(byte slotNumber, ReadOnlyMemory<byte> currentValue, ReadOnlyMemory<byte> newValue)
        {
            SlotNumber = slotNumber;

            if (PivPinUtilities.IsValidPinLength(currentValue.Length) == false
             || PivPinUtilities.IsValidPinLength(newValue.Length) == false)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPinPukLength));
            }

            _currentValue = currentValue;
            _newValue = newValue;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => new CommandApdu
        {
            Ins = PivChangeReferenceInstruction,
            P2 = SlotNumber,
            Data = PivPinUtilities.CopyTwoPinsWithPadding(_currentValue, _newValue),
        };

        /// <inheritdoc />
        public ChangeReferenceDataResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
          new ChangeReferenceDataResponse(responseApdu);
    }
}
