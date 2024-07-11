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
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// The base class for some of the General Authenticate command classes,
    /// containing shared code.
    /// </summary>
    public abstract class AuthenticateCommand
    {
        private const byte PivAuthenticateInstruction = 0x87;
        private const byte NestedTag = 0x7C;
        private const byte ResponseTag = 0x82;

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs. For this
        /// command it's PIV.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Piv
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Piv;

        /// <summary>
        /// The algorithm of the key used. See <see cref="PivAlgorithm"/>
        /// </summary>
        internal PivAlgorithm Algorithm { get; set; }

        /// <summary>
        /// The tag used for building the data portion of the APDU.
        /// </summary>
        protected byte DataTag { get; set; }

        /// <summary>
        /// The data following the <c>DataTag</c>.
        /// </summary>
        protected ReadOnlyMemory<byte> Data { get; set; }

        // This is needed so we can make the check on the set of the property.
        private byte _slotNumber;

        /// <summary>
        /// The slot holding the key to use.
        /// </summary>
        /// <value>
        /// The slot number, see <see cref="PivSlot"/>
        /// </value>
        /// <exception cref="ArgumentException">
        /// The slot specified is not valid for public key operations.
        /// </exception>
        public byte SlotNumber
        {
            get => _slotNumber;
            set
            {
                if (PivSlot.IsValidSlotNumberForSigning(value) == false)
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

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() =>
            new CommandApdu
            {
                Ins = PivAuthenticateInstruction,
                P1 = (byte)Algorithm,
                P2 = SlotNumber,
                Data = BuildGeneralAuthenticateApduData(),
            };

        /// <summary>
        /// Build a byte array contains the data portion of the APDU.
        /// </summary>
        /// <remarks>
        /// This will build the construction:
        /// <code>
        ///   7C len 82 00 DataTag len data
        /// </code>
        /// This construction works for the <c>AuthenticateSignCommand</c>, as
        /// well as the <c>AuthenticateDecryptCommand</c> and the
        /// <c>AuthenticateKeyAgreeCommand</c>. The only difference between the
        /// three commands is that the dataTag for Key Agree is 0x85.
        /// <para>
        /// PIV lists a GENERAL AUTHENTICATE command that can do four things:
        /// Sign, Decrypt, Key Agree, and authenticate the management key. This
        /// one method builds the APDU data for Sign, Decrypt, and Key Agree. Do
        /// not use this method for building APDU data for authenticating the
        /// management key.
        /// </para>
        /// </remarks>
        /// <returns>
        /// The APDU data as a byte array.
        /// </returns>
        private byte[] BuildGeneralAuthenticateApduData()
        {
            var tlvWriter = new TlvWriter();
            using (tlvWriter.WriteNestedTlv(NestedTag))
            {
                tlvWriter.WriteValue(ResponseTag, null);
                tlvWriter.WriteValue(DataTag, Data.Span);
            }

            return tlvWriter.Encode();
        }
    }
}
