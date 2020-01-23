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
    /// Put PIV standard information onto the YubiKey.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="PutDataResponse"/>.
    /// <para>
    /// It is possible to put a variety of data elements onto a YubiKey using
    /// this command. Specify which data is being loaded using the
    /// <see cref="PivDataTag"/> enum, provide the encoded data, then see if the
    /// Put succeeded using the <c>PutDataResponse</c>.
    /// </para>
    /// <para>
    /// Note that if the YubiKey already contains information under the given
    /// tag, this command replaces it.
    /// </para>
    /// <para>
    /// Generally, an application will not use this command. Rather, there are
    /// other SDK APIs (higher layers) that load specific information onto a
    /// YubiKey. For example, if you want to put a certificate onto a YubiKey,
    /// there are classes and methods that do so. Under the covers, these APIs
    /// will ultimately call this command. But the application that uses the SDK
    /// can simply make the specific API calls, rather than use Put Data.
    /// </para>
    /// <para>
    /// The reason for this is that the input to Put Data must follow specific
    /// formats. Each tag has its own defined format. That is, there is not one
    /// format, but many. This class will verify that the data to put is encoded
    /// correctly for the tag specified, but it is the responsibility of the
    /// caller to build that encoding. Most applications will not bother with
    /// encoding, but rather let the (higher-layer) SDK APIs take care of it.
    /// </para>
    /// <para>
    /// This command will not allow putting data for all PIV data tags. For
    /// example, the Discovery tag is used to get specific data for applications
    /// to learn about the device with which they are communicating. It is fixed
    /// data. It would inhibit the YubiKey's ability to operate if that value
    /// were changed from what was installed at manufacture. Currently it is not
    /// possible to PUT DATA using the tags <c>Discovery</c>,
    /// <c>BiometricGroupTemplate</c>, and <c>Printed</c>. See also the
    /// documentation for <see cref="PivDataTag"/>.
    /// </para>
    /// <para>
    /// In virtually all real-world applications, this will likely not be a
    /// problem. Generally, the only elements an application will need to PUT are
    /// certificates. If there are cases where data must be put into one of these
    /// tags, there will be a (higher-layer) API to specifically do that.
    /// </para>
    /// <para>
    /// The caller supplies the encoded data. See the User's Manual entry on
    /// <xref href="UsersManualPivCommands#get-data"> GET DATA </xref> for a list
    /// of the encoding definitions for each tag. For example, to put a cert into
    /// one of the slots, you must get the DER encoding of the X.509 cert, then
    /// build the following TLV construction.
    /// <code>
    ///   53 L1
    ///     70 L2
    ///        --X.509 certificate--
    ///     71 01
    ///        00
    ///     FE 00
    /// </code>
    /// </para>
    /// <para>
    /// This class will copy a reference to the data to put, so you should not
    /// clear or alter that input data until this class is done with it, which is
    /// after the call to <c>SendCommand</c>.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    ///   /* This example assumes there is some code that will build an encoded
    ///      certificate from an X509Certificate2 object. */
    ///   byte[] encodedCertificate = PivPutDataEncodeCertificate(certObject);<br/>
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   PutDataCommand putDataCommand = new PutDataCommand(
    ///       PivDataTag.Authentication, encodedCertificate);
    ///   PutDataResponse putDataResponse = connection.SendCommand(putDataCommand);<br/>
    ///   if (getDataResponse.StatusWord != SWConstants.Success)
    ///   {
    ///       /* handle case where the the PUT did not work. */
    ///   }
    /// </code>
    /// </remarks>
    public sealed class PutDataCommand : IYubiKeyCommand<PutDataResponse>
    {
        private const byte PivPutDataInstruction = 0xDB;
        private const byte PivPutDataParameter1 = 0x3F;
        private const byte PivPutDataParameter2 = 0xFF;
        private const int PivPutDataTlvTagLength = 5;
        private const byte PivPutDataTlvTag = 0x5C;
        private const byte PivPutDataTag = 0x53;
        private const int MinimumVendorTag = 0x005F0000;
        private const int MaximumVendorTag = 0x005fffff;

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
        /// The tag specifying where the data will be put.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The data tag specified is not valid for putting data.
        /// </exception>
        public PivDataTag Tag
        {
            get => (PivDataTag)_tag;
            set
            {
                if (value.IsValidTagForPut() == false)
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

        private ReadOnlyMemory<byte> _encodedData;

        /// <summary>
        /// The data that will be put onto the YubiKey.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The data supplied is not valid for the specified data tag.
        /// </exception>
        public ReadOnlyMemory<byte> EncodedData
        {
            get => _encodedData;
            set
            {
                if (Tag.IsValidEncodingForPut(value) == false)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidDataEncoding));
                }
                _encodedData = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <c>PutDataCommand</c> class.
        /// </summary>
        /// <remarks>
        /// This constructor is provided for those developers who want to use the
        /// object initializer pattern. For example:
        /// <code>
        ///   var command = new PutDataCommand()
        ///   {
        ///       Tag = PivDataTag.Authentication;
        ///       EncodedData = GetEncodedCertificate();
        ///   };
        /// </code>
        /// <para>
        /// There is no default Tag or EncodedData, hence, for this command to be
        /// valid, the Tag and EncodedData must be specified. So if you create an
        /// object using this constructor, you must set the Tag and EncodedData
        /// properties at some time before using it. Otherwise you will get an
        /// exception when you do use it.
        /// </para>
        /// </remarks>
        public PutDataCommand()
        {
            _tag = 0;
            _encodedData = Memory<byte>.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <c>PutDataCommand</c> class. This
        /// command takes in a tag indicating which data element to put, and the
        /// encoded data to load onto the YubiKey.
        /// </summary>
        /// <remarks>
        /// Note that this constructor will verify that the data follows the
        /// specified format for the given tag.
        /// </remarks>
        /// <param name="tag">
        /// The tag indicating which data to put.
        /// </param>
        /// <param name="encodedData">
        /// The data to put, encoded as defined int the User's Manual entry for
        /// <xref href="UsersManualPivCommands#get-data"> GET DATA </xref>.
        /// </param>
        public PutDataCommand(PivDataTag tag, ReadOnlyMemory<byte> encodedData)
        {
            Tag = tag;
            EncodedData = encodedData;
        }

        // This constructor is internal so that is is available only to the SDK.
        // This is intended to be used to put the vendor-defined elements, those
        // that are defined as 0x5fffxx. It is also intended to be used to put
        // arbitrary data into any tag.
        // The caller can pass in any int as a tag, and any data. The command
        // will put whatever data it is given. There are limitations. The YubiKey
        // does not support putting data for the tags 7E (Discovery) or 7F61
        // (Biometric Group Template). Furthermore, the only tags it supports are
        // those that are 3 bytes long and have as the first byte 0x5F. In
        // addition, the YubiKey enforces length limitations on the data.
        // Currently we have vendor-defined tags of 0x005fff00, 01, and 10 - 15.
        // The data entered is put onto the YubiKey exactly as given. However,
        // the YubiKey also enforces the rule that the data must be in the form
        // of a TLV with a T of 0x53.
        // For example, if you want to store the data
        //   01 02 03 04
        // in the Iris Images tag (5FC121), you must supply the data as
        //   53 04 01 02 03 04
        //     or looking at this parsed
        //   53 04
        //      01 02 03 04
        // A call to GET DATA will return the data as the TLV 53 L Data.
        internal PutDataCommand(int tag, ReadOnlyMemory<byte> data)
        {
            _tag = tag;

            if ((tag < MinimumVendorTag) || (tag > MaximumVendorTag))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidDataTag,
                        tag));
            }

            if (data.Span[0] != PivPutDataTag)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidDataEncoding));
            }

            _encodedData = data;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => new CommandApdu
        {
            Ins = PivPutDataInstruction,
            P1 = PivPutDataParameter1,
            P2 = PivPutDataParameter2,
            Data = BuildPutDataApduData(),
        };

        // Build the data that is the Data portion of the APDU.
        // The data will be the concatenation of two TLVs:
        //   5C length Tag || Data
        // Note that the full encoded Data is 53 L specificEncoding.
        // It is the responsibility of the caller to encode the Data. The
        // constructors performed some minimal checks on the input data.
        private byte[] BuildPutDataApduData()
        {
            if (_tag == 0)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidDataTag,
                        _tag));
            }

            if (_encodedData.Length == 0)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidDataEncoding));
            }

            byte[] encoding = new byte[PivPutDataTlvTagLength + _encodedData.Length];
            encoding[0] = PivPutDataTlvTag;
            encoding[1] = 3;
            encoding[2] = (byte)(_tag >> 16);
            encoding[3] = (byte)(_tag >> 8);
            encoding[4] = (byte)_tag;
            var encodingMemory = new Memory<byte>(encoding);
            encodingMemory = encodingMemory.Slice(PivPutDataTlvTagLength);
            _encodedData.CopyTo(encodingMemory);

            return encoding;
        }

        /// <inheritdoc />
        public PutDataResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new PutDataResponse(responseApdu);
    }
}
