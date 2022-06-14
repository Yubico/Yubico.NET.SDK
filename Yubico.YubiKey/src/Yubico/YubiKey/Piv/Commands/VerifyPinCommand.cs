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
using System.Collections.Generic;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// Verify the PIV PIN.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="VerifyPinResponse"/>.
    /// <para>
    /// Some operations require the user enter a PIN. Use this class to build a
    /// command to verify the PIN. This will generally be used in conjunction
    /// with other commands that require the PIN. But it is possible to simply
    /// use this command to verify the PIN only.
    /// </para>
    /// <para>
    /// The PIN starts out as a default value: "123456", which in ASCII is the
    /// 6-byte sequence <c>0x31 32 33 34 35 36</c>. Generally, the first thing
    /// done when a YubiKey is initialized for PIV is to change the PIN (along
    /// with the PUK and management key). The PIN must be 6 to 8 bytes.
    /// Ultimately the bytes that make up the PIN can be any binary value, but
    /// are generally input from a keyboard, so are usually made up of ASCII
    /// characters.
    /// </para>
    /// <para>
    /// The PIN you pass in must be 6 to 8 bytes long. If the actual PIN
    /// collected is less than 6  or more than 8 bytes long, it will be invalid.
    /// </para>
    /// <para>
    /// Note that with PIV there is also a PUK (PIN Unblocking Key). This command
    /// cannot verify a PUK.
    /// </para>
    /// <para>
    /// When you pass a PIN to this class (the PIN to verify), the class will
    /// copy a reference to the object passed in, it will not copy the value.
    /// Because of this, you cannot overwrite the PIN until this object is done
    /// with it. It will be safe to overwrite the PIN after calling
    /// <c>connection.SendCommand</c>. See the User's Manual
    /// <xref href="UsersManualSensitive"> entry on sensitive data</xref> for
    /// more information on this topic.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code language="csharp">
    ///   /* This example assumes the application has a method to collect a PIN.
    ///    */
    ///   byte[] pin;<br/>
    ///
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   pin = CollectPin();
    ///   var verifyPinCommand = new VerifyPinCommand(pin);
    ///   VerifyPinResponse verifyPinResponse = connection.SendCommand(verifyPinCommand);<br/>
    ///   if (resetRetryResponse.Status == ResponseStatus.AuthenticationRequired)
    ///   {
    ///     int retryCount = resetRetryResponse.GetData();
    ///     /* report the retry count */
    ///   }
    ///   else if (verifyPinResponse.Status != ResponseStatus.Success)
    ///   {
    ///     // Handle error
    ///   }
    ///
    ///   CryptographicOperations.ZeroMemory(pin)
    /// </code>
    /// </remarks>
    public sealed class VerifyPinCommand : IYubiKeyCommand<VerifyPinResponse>
    {
        private const byte PivVerifyInstruction = 0x20;

        private readonly ReadOnlyMemory<byte> _pin;

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
        // Note that there is no object-initializer constructor. the only
        // constructor input is a secret byte arrays.
        private VerifyPinCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes a new instance of the VerifyPinCommand class which will
        /// use the given PIN.
        /// </summary>
        /// <remarks>
        /// In order to verify a PIN, the caller must supply the PIN. In this
        /// class, the PIN is supplied as <c>ReadOnlyMemory&lt;byte&gt;</c>. It
        /// is possible to pass a <c>byte[]</c>, because it will be automatically
        /// cast.
        /// <para>
        /// This class will copy references to the PIN (not the values. This
        /// means that you can overwrite the PIN in your byte array only after
        /// this class is done with it. It will no longer need the PIN after
        /// calling <c>connection.SendCommand</c>.
        /// </para>
        /// <para>
        /// A PIN is 6 to 8 bytes long.
        /// </para>
        /// </remarks>
        /// <param name="pin">
        /// The bytes that make up the PIN.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The PIN is an invalid length.
        /// </exception>
        public VerifyPinCommand(ReadOnlyMemory<byte> pin)
        {
            if (PivPinUtilities.IsValidPinLength(pin.Length) == false)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPinPukLength));
            }

            _pin = pin;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => new CommandApdu
        {
            Ins = PivVerifyInstruction,
            P2 = (byte)PivSlot.Pin,
            Data = PivPinUtilities.CopySinglePinWithPadding(_pin),
        };

        /// <inheritdoc />
        public VerifyPinResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
          new VerifyPinResponse(responseApdu);
    }
}
