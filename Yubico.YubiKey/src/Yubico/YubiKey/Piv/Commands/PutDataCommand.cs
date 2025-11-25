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
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// Put a Data Object onto the YubiKey in a storage location.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="PutDataResponse"/>.
    /// <para>
    /// See also the User's Manual entries on
    /// <xref href="UsersManualPivGetAndPutData"> Get and Put Data</xref> and
    /// <xref href="UsersManualPivObjects"> PIV objects</xref>.
    /// </para>
    /// <para>
    /// A Data Object is a DataTag/Data pair. Think of it as a "key-value pair",
    /// with the DataTag a key and the Data a value. The DataTag is simply a
    /// number and associated with each is a definition of data elements and how
    /// they are encoded.
    /// </para>
    /// <para>
    /// The PIV standard specifies a set of Data Objects. See a description of
    /// each DataTag and the associated Data in the User's Manual entry on
    /// <xref href="UsersManualPivCommands#getdatatable"> the GET DATA command</xref>.
    /// Yubico specifies some non-standard DataTags as well. See descriptions in
    /// the User's Manual entry on
    /// <xref href="UsersManualPivCommands#getvendordatatable"> Get and Put vendor data</xref>.
    /// The YubiKey also allows an application to use undefined DataTags as well.
    /// See the See the User's Manual entry on
    /// <xref href="UsersManualPivObjects#datatagtable3"> Data Objects</xref> for
    /// a table describing the undefined DataTags.
    /// </para>
    /// <para>
    /// The caller supplies the DataTag and the data, this command will store
    /// that data on the YubiKey under the DataTag. If the YubiKey already
    /// contains information under the given DataTag, this command replaces it.
    /// </para>
    /// <para>
    /// Note that for some Data Objects there are higher-level APIs that are
    /// easier to use. An application that needs to store information often will
    /// not need to use this command. For example, if you want to put a
    /// certificate onto a YubiKey, use <see cref="PivSession.ImportCertificate"/>.
    /// Or if you want to store/retrieve Key History, use
    /// <see cref="PivSession.ReadObject{T}()"/> and <see cref="PivSession.WriteObject"/>
    /// along with the <see cref="Piv.Objects.KeyHistory"/> class. Under the
    /// covers, these APIs will ultimately call this command. But the application
    /// that uses the SDK can simply make the specific API calls, rather than use
    /// Put Data.
    /// </para>
    /// <para>
    /// There are a number of ways to use this command. The old, obsolete way is
    /// to provide the DataTag using the <c>PivDataTag</c> enum, along with data
    /// encoded as defined by PIV. The constructor
    /// <c>PutDataCommand(PivDataTag, ReadOnlyMemory&lt;byte&gt;)</c> and the
    /// properties <c>Tag</c> and <c>EncodedData</c> require using PIV-defined
    /// DataTags with PIV-defined encoded data only. This constructor and these
    /// properties are marked "Obsolete" and will be removed from the SDK in the
    /// future. However, it will still be possible to get the same functionality
    /// using the updated API.
    /// </para>
    /// <para>
    /// The API you should use are the constructors <c>PutDataCommand()</c>, and
    /// <c>PutDataCommand(int, ReadOnlyMemory&lt;byte&gt;)</c>c>, along with the
    /// properties <c>DataTag</c> and <c>Data</c>. Using these will allow you to
    /// use any DataTag (not just those defined by PIV) and any data (not just
    /// the data and encoding specified by PIV). There are two restrictions on
    /// the data. One, each storage area on the YubiKey can only store up to
    /// 2,800 bytes, and two, the data must be encoded as
    /// <code>
    ///     53 length
    ///        data
    ///   For example, to store 04 02 55 44 02 01 7F
    ///   the data must be provided as
    ///     53 07
    ///        04 02 55 44 02 01 7F
    /// </code>
    /// </para>
    /// <para>
    /// While you can store any data under a PIV-defined DataTag, if you want to
    /// use only PIV-defined DataTags, and want to verify that the encoding
    /// follows the PIV standard (as happens using the old, obsolete API), you
    /// can use the <c>PivDataTag</c> class. For example,
    /// <code language="csharp">
    ///    // Store IrisImages
    ///    if (PivDataTag.IrisImages.IsValidEncodingForPut(encodedDataToStore))
    ///    {
    ///        var putCmd = new PutDataCommand(
    ///            (int)PivDataTag.IrisImages, encodedDataToStore);
    ///    }
    /// </code>
    /// </para>
    /// <para>
    /// Note that even though it is possible, using this command, to store any
    /// data under a PIV-defined DataTag, it is not recommended. If you have some
    /// other data you would like to store, you can use an "undefined" DataTag.
    /// For example, the YubiKey will store data under the number
    /// <c>0x005F0101</c>. That is a number not used by PIV and not used by
    /// Yubico. But the YubiKey will accept it. So if you have non-PIV data you
    /// want to store, rather than use a PIV-defined number, just use one of the
    /// undefined values. See the User's Manual entry on
    /// <xref href="UsersManualPivObjects#datatagtables"> Data Objects</xref>
    /// for tables describing possible DataTags.
    /// </para>
    /// <para>
    /// There is probably only one reason to store non-PIV data under a
    /// PIV-defined DataTag, and that is to require the PIN to retrieve. There
    /// are four PIV-defined DataTags for which the PIN is required to retrieve
    /// the data: Printed, Iris, FacialImages, and Fingerprints. Data stored
    /// under any other DataTag can be retrieved by anyone with access to the
    /// YubiKey. If you want to store some data and want it to be accessible only
    /// with PIN verification, then you must store it under one of those
    /// DataTags. However, Yubico already stores data under the Printed DataTag.
    /// That data is needed to configure the YubiKey in "PIN-Protected mode".
    /// Hence, it would be a good idea to stay away from that DataTag.
    /// </para>
    /// <para>
    /// This command will not allow putting data for all PIV data tags. The
    /// Discovery and BITGT DataTags will not be accepted. The Discovery Data
    /// object is fixed data and it would inhibit the YubiKey's ability to
    /// operate if that value were changed from what was installed at
    /// manufacture. In virtually all real-world applications, this will likely
    /// not be a problem.
    /// </para>
    /// <para>
    /// Note that when you set an object with the DataTag or Data using either
    /// the old constructor/properties or the new versions, when you get it
    /// (using either old or new), you are getting the same thing. For example,
    /// <code language="csharp">
    ///     // Use the old, obsolete API to set the tag and data.
    ///     var putCmd = new PutDataCommand()
    ///     {
    ///         Tag = PivDataTag.KeyHistory,
    ///         EncodedData = encodedKeyHistory,
    ///     }<br/>
    ///     PivDataTag pivDataTag = putCmd.Tag;
    ///     int dataTag = putCmd.DataTag;
    ///     // At this point pivDataTag will equal PivDataTag.KeyHistory = 0x005FC10C
    ///     // dataTag will equal 0x005FC10C
    ///     // Even though the code used the old API to set the Tag property
    ///     // the new API DataTag property will return the same value.
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
    /// <code language="csharp">
    ///   /* This example assumes there is some code that will build an encoded
    ///      certificate from an X509Certificate2 object. */
    ///   byte[] encodedCertificate = PivPutDataEncodeCertificate(certObject);<br/>
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   PutDataCommand putDataCommand = new PutDataCommand(
    ///       (int)PivDataTag.Authentication, encodedCertificate);
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
        private ReadOnlyMemory<byte> _encodedData;

        /// <summary>
        /// The tag specifying where the data will be put.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The DataTag specified is not a number between <c>0x005F0000</c> and
        /// <c>0x005FFFFF</c> (inclusive), or <c>0x00007F61</c>.
        /// </exception>
        public int DataTag
        {
            get => _tag;
            set
            {
                if ((value < MinimumVendorTag || value > MaximumVendorTag) && value != BiometricGroupTemplateTag)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidDataTag,
                            value));
                }
                _tag = value;
            }
        }

        /// <summary>
        /// The data that will be put onto the YubiKey.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The data supplied is not encoded <c>53 length data</c>.
        /// </exception>
        public ReadOnlyMemory<byte> Data
        {
            get => _encodedData;
            set
            {
                if (!IsDataEncoded(value))
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
        /// &gt; [!WARNING]
        /// &gt; This property is obsolete, use <c>DataTag</c> instead.
        ///
        /// The tag specifying where the data will be put.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The data tag specified is not valid for putting data.
        /// </exception>
        [ObsoleteAttribute("This property is obsolete. Use DataTag instead", false)]
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

        /// <summary>
        /// &gt; [!WARNING]
        /// &gt; This property is obsolete, use <c>Data</c> instead.
        ///
        /// The data that will be put onto the YubiKey.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The data supplied is not valid for the specified data tag.
        /// </exception>
        [ObsoleteAttribute("This property is obsolete. Use Data instead", false)]
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
        /// <code language="csharp">
        ///   var command = new PutDataCommand()
        ///   {
        ///       DataTag = (int)PivDataTag.Authentication;
        ///       Data = GetEncodedCertificate();
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
        /// &gt; [!WARNING]
        /// &gt; This constructor is obsolete, use <c>PutDataCommand()</c> or
        /// &gt; <c>PutDataCommand(int, ReadOnlyMemory&lt;byte&gt;)</c> instead.
        ///
        /// Initializes a new instance of the <c>PutDataCommand</c> class. This
        /// command takes in a tag indicating which data element to put, and the
        /// encoded data to load onto the YubiKey.
        /// </summary>
        /// <remarks>
        /// Note that this constructor requires using a PIV-defined DataTag and
        /// will verify that the data follows the PIV-specified format for the
        /// given DataTag. To use any DataTag (PIV-defined, Yubico-defined, or
        /// one of the allowed undefined values), and/or any encoded data, use
        /// one of the other constructors.
        /// </remarks>
        /// <param name="tag">
        /// The tag indicating which data to put.
        /// </param>
        /// <param name="encodedData">
        /// The data to put, encoded as defined int the User's Manual entry for
        /// <xref href="UsersManualPivCommands#get-data"> GET DATA </xref>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The encodedData is not as specified for the given <c>tag</c>.
        /// </exception>
        [ObsoleteAttribute("This constructor is obsolete. Use PutDataCommand(int, ReadOnlyMemory<byte>) instead", false)]
        public PutDataCommand(PivDataTag tag, ReadOnlyMemory<byte> encodedData)
        {
            Tag = tag;
            EncodedData = encodedData;
        }

        /// <summary>
        /// Initializes a new instance of the <c>PutDataCommand</c> class.
        /// </summary>
        /// <remarks>
        /// Note that this constructor requires using a DataTag that is a number
        /// from <c>0x005F0000</c> to <c>0x005FFFFF</c> inclusive. The data can
        /// be anything but it must be encoded as <c>53 length data</c>
        /// <para>
        /// To encode, you can use the <c>TlvWriter</c> class.
        /// <code language="csharp">
        ///    var tlvWriter = new TlvWriter();
        ///    tlvWriter.WriteValue(0x53, dataToStore);
        ///    byte[] encoding = tlvWriter.Encode();
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="dataTag">
        /// The DataTag indicating where the data will be stored.
        /// </param>
        /// <param name="data">
        /// The data to put, encoded as <c>53 length data</c>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The DataTag specified is not a number between <c>0x005F0000</c> and
        /// <c>0x005FFFFF</c> (inclusive), or <c>0x00007F61</c>.
        /// </exception>
        public PutDataCommand(int dataTag, ReadOnlyMemory<byte> data)
        {
            DataTag = dataTag;
            Data = data;
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
            encodingMemory = encodingMemory[PivPutDataTlvTagLength..];
            _encodedData.CopyTo(encodingMemory);

            return encoding;
        }

        // Verify that the data is
        //   53 len
        //      data
        // If there is no 53 tag, return false.
        // If the length is not correct, return false.
        // For example, these are all incorrect and return false
        //   71 02 01 02
        //   53 03 01 02
        //   53 03 01 02 03 04
        private static bool IsDataEncoded(ReadOnlyMemory<byte> encoding)
        {
            if (encoding.Length == 0)
            {
                return false;
            }

            var tlvReader = new TlvReader(encoding);

            if (tlvReader.PeekTag() == PivPutDataTag &&
                tlvReader.TryReadValue(out _, PivPutDataTag) &&
                !tlvReader.HasData)
            {
                return true;
            }
            
            return false;
        }

        /// <inheritdoc />
        public PutDataResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new PutDataResponse(responseApdu);
    }
}
