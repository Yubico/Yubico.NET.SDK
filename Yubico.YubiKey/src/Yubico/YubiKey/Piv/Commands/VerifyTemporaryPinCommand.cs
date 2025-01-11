// Copyright 2024 Yubico AB
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
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// Verify the PIV Bio temporary PIN.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="VerifyTemporaryPinResponse"/>.
    /// <para>
    /// When using biometric verification, clients can request a temporary PIN
    /// by calling <see cref="VerifyUvCommand"/> with requestTemporaryPin=true.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code language="csharp">
    ///   /* This example assumes the application has a method to collect a PIN.
    ///   */
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   /* request temporary PIN */
    ///   var verifyUvCommand = new VerifyUvCommand(true, false);<br/>
    ///   /* a biometric verification will be performed */
    ///   var verifyUvResponse = connection.SendCommand(verifyUvCommand);
    ///   if (verifyUvResponse.Status == ResponseStatus.Success) 
    ///   {
    ///     var temporaryPin = verifyUvResponse.GetData();<br/>
    ///     /* using temporary PIN will not request biometric verification */
    ///     var verifyTemporaryPinCommand = new VerifyTemporaryPin(temporaryPin);
    ///     var verifyResponse = connection.SendCommand(verifyTemporaryPinCommand);
    ///     if (verifyResponse == ResponseStatus.Success) 
    ///     {
    ///         /* session is authenticated */
    ///     }
    ///   }
    /// </code>
    /// </remarks>
    public sealed class VerifyTemporaryPinCommand : IYubiKeyCommand<VerifyTemporaryPinResponse>
    {
        private const byte PivVerifyInstruction = 0x20;
        private const byte OnCardComparisonAuthenticationSlot = 0x96;
        private const int TemporaryPinLength = 16;

        private readonly ReadOnlyMemory<byte> _temporaryPin;

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
        private VerifyTemporaryPinCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes a new instance of the VerifyTemporaryPinCommand class which will
        /// use the given temporary PIN.
        /// </summary>
        /// <remarks>
        /// In order to verify a temporary PIN, the caller must supply the temporary PIN.
        /// In this class, the temporary PIN is supplied as <c>ReadOnlyMemory&lt;byte&gt;</c>.
        /// It is possible to pass a <c>byte[]</c>, because it will be automatically cast.
        /// <para>
        /// This class will copy references to the temporary PIN (not the values. This
        /// means that you can overwrite the temporary PIN in your byte array only after
        /// this class is done with it. It will no longer need the temporary PIN after
        /// calling <c>connection.SendCommand</c>.
        /// </para>
        /// <para>
        /// A temporary PIN is 16 bytes long.
        /// </para>
        /// </remarks>
        /// <param name="temporaryPin">
        /// The temporary Pin.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The PIN is an invalid length.
        /// </exception>
        public VerifyTemporaryPinCommand(ReadOnlyMemory<byte> temporaryPin)
        {
            if (temporaryPin.Length != TemporaryPinLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidTemporaryPinLength));
            }

            _temporaryPin = temporaryPin;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            const byte VerifyTemporaryPinTag = 0x01;
            var tlvWriter = new TlvWriter();
            tlvWriter.WriteValue(VerifyTemporaryPinTag, _temporaryPin.Span);
            ReadOnlyMemory<byte> data = tlvWriter.Encode();
            return new CommandApdu
            {
                Ins = PivVerifyInstruction,
                P2 = OnCardComparisonAuthenticationSlot,
                Data = data
            };
        }

        /// <inheritdoc />
        public VerifyTemporaryPinResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
          new VerifyTemporaryPinResponse(responseApdu);
    }
}
