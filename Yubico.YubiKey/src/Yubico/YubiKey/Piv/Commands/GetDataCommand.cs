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
using System.Collections.Generic;
using System.Buffers.Binary;
using System.Linq;
using System.Globalization;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// Get PIV standard information from the YubiKey.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="GetDataResponse"/>.
    /// <para>
    /// It is possible to get a variety of data elements from a YubiKey using
    /// this command. Specify which data is requested using the
    /// <see cref="PivDataTag"/> enum, then examine the return from the
    /// <c>GetDataResponse</c>.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   GetDataCommand getDataCommand = new GetDataCommand(PivDataTag.Chuid);
    ///   GetDataResponse getDataResponse = connection.SendCommand(getDataCommand);<br/>
    ///   if (getDataResponse.StatusWord == SWConstants.Success)
    ///   {
    ///       byte[] getChuid = getDataResponse.GetData();
    ///   }
    /// </code>
    /// </remarks>
    public sealed class GetDataCommand : IYubiKeyCommand<GetDataResponse>
    {
        private const byte PivGetDataInstruction = 0xCB;
        private const byte PivGetDataParameter1 = 0x3F;
        private const byte PivGetDataParameter2 = 0xFF;
        private const byte PivGetDataTlvTag = 0x5C;
        private const int MinimumVendorTag = 0x005F0000;
        private const int MaximumVendorTag = 0x00ffffff;
        private const int DiscoveryTag = 0x0000007E;
        private const int BiometricGroupTemplateTag = 0x00007F61;

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs. For this
        /// command it's PIV.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Piv
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Piv;

        private int _tag;

        /// <summary>
        /// The tag specifying which data to get.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The data tag specified is not valid for getting data.
        /// </exception>
        public PivDataTag Tag
        {
            get => (PivDataTag)_tag;
            set
            {
                if (Enum.IsDefined(typeof(PivDataTag), value) == false
                    || value == PivDataTag.Unknown)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidDataTag,
                            value));
                }
                _tag = (int)value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the GetDataCommand class.
        /// </summary>
        /// <remarks>
        /// This constructor is provided for those developers who want to use the
        /// object initializer pattern. For example:
        /// <code>
        ///   var getDataCommand = new GetDataCommand()
        ///   {
        ///       Tag = PivDataTag.Authentication,
        ///   };
        /// </code>
        /// <para>
        /// There is no default data tag, hence, for this command to be valid,
        /// the tag must be specified. So if you create an object using this
        /// constructor, you must set the Tag property at some time before using
        /// it. Otherwise you will get an exception when you do use it.
        /// </para>
        /// <para>
        /// The valid data tags are in the enum <c>PivDataTag</c>.
        /// </para>
        /// </remarks>
        public GetDataCommand()
        {
            _tag = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <c>GetDataCommand</c> class. This
        /// command takes in a tag indicating which data element to get.
        /// </summary>
        /// <param name="tag">
        /// The tag indicating which data to get.
        /// </param>
        public GetDataCommand(PivDataTag tag)
        {
            Tag = tag;
        }

        // This constructor is internal so that is is available only to the SDK.
        // This is intended to be used to get the vendor-defined elements, those
        // that are defined as 0x5fffxx.
        // The caller can pass in any int as a tag, and the command will get that
        // data object. There are limitations. If the tag is one byte, it still
        // must be 0x7E (Discovery), if it is two bytes, it still must be 0x7f61
        // (Biometric Group Template), and if not those values, it must be 3
        // bytes. Note that 0 is not a valid tag for this class.
        // Currently we have vendor-defined tags of 0x005fff00, 01, and 10 - 15.
        internal GetDataCommand(int tag)
        {
            _tag = tag;

            if (tag < MinimumVendorTag)
            {
                if ((tag != DiscoveryTag) && (tag != BiometricGroupTemplateTag))
                {
                    _tag = 0;
                }
            }
            if (tag > MaximumVendorTag)
            {
                _tag = 0;
            }

            if (_tag == 0)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidDataTag,
                        tag));
            }
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => new CommandApdu
        {
            Ins = PivGetDataInstruction,
            P1 = PivGetDataParameter1,
            P2 = PivGetDataParameter2,
            Data = BuildGetDataApduData(),
        };

        // Build the data that is the Data portion of the APDU.
        // This will be TLV 5C length Tag
        private byte[] BuildGetDataApduData()
        {
            return _tag switch
            {
                0 => throw new InvalidOperationException(
                         string.Format(
                             CultureInfo.CurrentCulture,
                             ExceptionMessages.InvalidDataTag,
                             _tag)),
                DiscoveryTag => new byte[] { PivGetDataTlvTag, 0x01, (byte)_tag },
                BiometricGroupTemplateTag => new byte[] { PivGetDataTlvTag, 0x02, (byte)(_tag >> 8), (byte)_tag },
                _ => new byte[] { PivGetDataTlvTag, 0x03, (byte)(_tag >> 16), (byte)(_tag >> 8), (byte)_tag },
            };
        }

        /// <inheritdoc />
        public GetDataResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetDataResponse(responseApdu);
    }
}
