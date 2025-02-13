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
    /// Get information about the key in a specified slot.
    /// </summary>
    /// <remarks>
    /// The Get Metadata command is available on YubiKey version 5.3 and later.
    /// <para>
    /// The partner Response class is <see cref="GetMetadataResponse"/>.
    /// </para>
    /// <para>
    /// See the User's Manual
    /// <xref href="UsersManualPivCommands#get-metadata"> entry on getting metadata</xref>
    /// for specific information about what information is returned. Different
    /// slots return different sets of data.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code language="csharp">
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   GetMetadataCommand metadataCommand = new GetMetadataCommand(0x9A);
    ///   GetMetadataResponse metadataResponse = connection.SendCommand(metadataCommand);<br/>
    ///   if (metadataResponse.Status == ResponseStatus.Success)
    ///   {
    ///       PivMetadata keyData = metadataResponse.GetData();
    ///   }
    /// </code>
    /// </remarks>
    public sealed class GetMetadataCommand : IYubiKeyCommand<GetMetadataResponse>
    {
        private const byte PivMetadataInstruction = 0xF7;

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs. For this
        /// command it's PIV.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Piv
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Piv;

        // This is needed so we can make the check on the set of the property.
        private byte _slotNumber;

        /// <summary>
        /// The slot for which the metadate is requested.
        /// </summary>
        /// <value>
        /// The slot number, see <see cref="PivSlot"/>
        /// </value>
        /// <exception cref="ArgumentException">
        /// The slot specified is not valid for getting metadata.
        /// </exception>
        public byte SlotNumber
        {
            get => _slotNumber;
            set
            {
                if (PivSlot.IsValidSlotNumber(value) == false)
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
        /// Initializes a new instance of the GetMetadataCommand class. This command
        /// takes the slot number as input.
        /// </summary>
        /// <remarks>
        /// The valid slot numbers are described in the User's Manual entry
        /// <xref href="UsersManualPivSlots"> PIV slots</xref>. There is also the
        /// static class <see cref="PivSlot"/> providing mappings between slot
        /// names and numbers.
        /// <para>
        /// For example, the following two are equivalent.
        /// <code language="csharp">
        /// var getMetadata = new GetMetadataCommand(0x9C);
        /// var getMetadata = new GetMetadataCommand(PivSlot.Signing);
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="slotNumber">
        /// The slot for which the metadata is requested.
        /// </param>
        public GetMetadataCommand(byte slotNumber)
        {
            SlotNumber = slotNumber;
        }

        /// <summary>
        /// Initializes a new instance of the GetMetadataCommand class.
        /// </summary>
        /// <remarks>
        /// This constructor is provided for those developers who want to use the
        /// object initializer pattern. For example:
        /// <code language="csharp">
        ///   var getMetadataCommand = new GetMetadataCommand()
        ///   {
        ///       SlotNumber = PivSlot.Authentication,
        ///   };
        /// </code>
        /// <para>
        /// There is no default slot number, hence, for this command to be valid,
        /// the slot number must be specified. So if you create an object using
        /// this constructor, you must set the SlotNumber property at some time
        /// before using it. Otherwise you will get an exception when you do use
        /// it.
        /// </para>
        /// <para>
        /// The valid slot numbers are described in the User's Manual entry
        /// <xref href="UsersManualPivSlots"> PIV slots</xref>. There is also the
        /// static class <see cref="PivSlot"/> providing mappings between slo/t
        /// names and numbers.
        /// </para>
        /// </remarks>
        public GetMetadataCommand()
        {
            _slotNumber = 0;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            if (PivSlot.IsValidSlotNumber(_slotNumber) == false)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidSlot,
                        _slotNumber));
            }

            return new CommandApdu
            {
                Ins = PivMetadataInstruction,
                P2 = SlotNumber,
            };
        }

        /// <inheritdoc />
        public GetMetadataResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetMetadataResponse(responseApdu, SlotNumber);
    }
}
