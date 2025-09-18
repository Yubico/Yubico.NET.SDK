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

namespace Yubico.YubiKey.Piv.Commands;

/// <summary>
///     Get a Data Object from the YubiKey.
/// </summary>
/// <remarks>
///     The partner Response class is <see cref="GetDataResponse" />.
///     <para>
///         See also the User's Manual entries on
///         <xref href="UsersManualPivGetAndPutData"> Get and Put Data</xref> and
///         <xref href="UsersManualPivObjects"> PIV objects</xref>, along with the
///         documentation for the <see cref="PutDataCommand" />.
///     </para>
///     <para>
///         Note that for some Data Objects there are higher-level APIs that are
///         easier to use. An application that needs to retrieve information often
///         will not need to use this command. For example, if you want to get a
///         certificate from a YubiKey, use <see cref="PivSession.GetCertificate" />.
///         Or if you want to store/retrieve Key History, use
///         <see cref="PivSession.ReadObject{T}()" /> and <see cref="PivSession.WriteObject" />
///         along with the <see cref="Piv.Objects.KeyHistory" /> class. Under the
///         covers, these APIs will ultimately call this command. But the application
///         that uses the SDK can simply make the specific API calls, rather than use
///         Get Data.
///     </para>
///     <para>
///         There are a number of ways to use this command. The old, obsolete way is
///         to provide the DataTag using the <c>PivDataTag</c> enum. The constructor
///         <c>GetDataCommand(PivDataTag)</c> and the property <c>Tag</c> require
///         using PIV-defined DataTags. This constructor and that property are marked
///         "Obsolete" and will be removed from the SDK in the future. However, it
///         will still be possible to get the same functionality using the updated
///         API.
///     </para>
///     <para>
///         The API you should use are the constructors <c>GetDataCommand()</c>, and
///         <c>GetDataCommand(int)</c>, along with the property <c>DataTag</c>. Using
///         these will allow you to use any DataTag (not just those defined by PIV).
///     </para>
///     <para>
///         While you can retrieve any data under a PIV-defined DataTag, if you want to
///         use only PIV-defined DataTags, you can use the <c>PivDataTag</c> class.
///         For example,
///         <code language="csharp">
///    // Retrieve IrisImages
///    var getCmd = new GetDataCommand((int)PivDataTag.IrisImages);
///    GetDataResponse getRsp = connection.SendCommand(getCmd);
///    ReadOnlyMemory&lt;byte&gt; encodedData = getRsp.GetData();
///    if (!PivDataTag.IrisImages.IsValidEncodingForPut(encodedData))
///    {
///        // handle error case.
///    }
/// </code>
///     </para>
///     <para>
///         Note that when you set an object with the DataTag using either the old
///         constructor/property or the new versions, when you get it (using either
///         old or new), you are getting the same thing. For example,
///         <code language="csharp">
///     // Use the old, obsolete API to set the tag.
///     var getCmd = new GetDataCommand()
///     {
///         Tag = PivDataTag.KeyHistory,
///     }<br />
///     PivDataTag pivDataTag = getCmd.Tag;
///     int dataTag = getCmd.DataTag;
///     // At this point pivDataTag will equal PivDataTag.KeyHistory = 0x005FC10C
///     // dataTag will equal 0x005FC10C
///     // Even though the code used the old API to set the Tag property
///     // the new API DataTag property will return the same value.
/// </code>
///     </para>
///     <para>
///         The data returned will begin with the tag <c>0x53</c>. For example,
///         <code>
///    Suppose the data is
///      04 02 55 44 02 01 7F
///    It will be returned by the GetDataCommand as
///      53 07
///         04 02 55 44 02 01 7F
/// </code>
///     </para>
///     <para>
///         Example:
///     </para>
///     <code language="csharp">
///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br />
///   GetDataCommand getDataCommand = new GetDataCommand((int)PivDataTag.Chuid);
///   GetDataResponse getDataResponse = connection.SendCommand(getDataCommand);<br />
///   if (getDataResponse.StatusWord == SWConstants.Success)
///   {
///       ReadOnlyMemory&lt;byte&gt; getChuid = getDataResponse.GetData();
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

    private int _tag;

    /// <summary>
    ///     Initializes a new instance of the GetDataCommand class.
    /// </summary>
    /// <remarks>
    ///     This constructor is provided for those developers who want to use the
    ///     object initializer pattern. For example:
    ///     <code language="csharp">
    ///   var getDataCommand = new GetDataCommand()
    ///   {
    ///       DataTag = (int)PivDataTag.Authentication,
    ///   };
    /// </code>
    ///     <para>
    ///         There is no default data tag, hence, for this command to be valid,
    ///         the tag must be specified. So if you create an object using this
    ///         constructor, you must set the <c>DataTag</c> property at some time
    ///         before using it. Otherwise you will get an exception when you do use
    ///         it.
    ///     </para>
    ///     <para>
    ///         The valid data tags are numbers from <c>0x005F0000</c> to
    ///         <c>0x005FFFFF</c> inclusive, along with <c>0x0000007E</c> and
    ///         <c>0x00007F61</c>.
    ///     </para>
    /// </remarks>
    public GetDataCommand()
    {
        _tag = 0;
    }

    /// <summary>
    ///     &gt; [!WARNING]
    ///     &gt; This constructor is obsolete, use <c>GetDataCommand()</c> or
    ///     &gt; <c>GetDataCommand(int)</c> instead.
    ///     Initializes a new instance of the <c>GetDataCommand</c> class. This
    ///     command takes in a tag indicating which data element to get.
    /// </summary>
    /// <remarks>
    ///     Note that this constructor is Obsolete.
    /// </remarks>
    /// <param name="tag">
    ///     The tag indicating which data to get.
    /// </param>
    [ObsoleteAttribute("This constructor is obsolete. Use GetDataCommand(int) instead", false)]
    public GetDataCommand(PivDataTag tag)
    {
        Tag = tag;
    }

    /// <summary>
    ///     Initializes a new instance of the <c>GetDataCommand</c> class.
    /// </summary>
    /// <remarks>
    ///     Note that this constructor requires using a DataTag that is a number
    ///     from <c>0x005F0000</c> to <c>0x005FFFFF</c> inclusive, along with
    ///     <c>0x0000007E</c> and <c>0x00007F61</c>.
    /// </remarks>
    /// <param name="dataTag">
    ///     The DataTag indicating from where the data will be retrieved.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     The DataTag specified is not a number between <c>0x005F0000</c> and
    ///     <c>0x005FFFFF</c> (inclusive), or <c>0x0000007E</c> or
    ///     <c>0x00007F61</c>.
    /// </exception>
    public GetDataCommand(int dataTag)
    {
        DataTag = dataTag;
    }

    /// <summary>
    ///     The tag specifying from where the data is to be retrieved.
    /// </summary>
    /// <exception cref="ArgumentException">
    ///     The DataTag specified is not a number between <c>0x005F0000</c> and
    ///     <c>0x005FFFFF</c> (inclusive), or 0x0000007E or 0x00007F61.
    /// </exception>
    public int DataTag
    {
        get => _tag;
        set
        {
            if (value < MinimumVendorTag || value > MaximumVendorTag)
            {
                if (value != DiscoveryTag && value != BiometricGroupTemplateTag)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidDataTag,
                            value));
                }
            }

            _tag = value;
        }
    }

    /// <summary>
    ///     &gt; [!WARNING]
    ///     &gt; This property is obsolete, use <c>DataTag</c> instead.
    ///     The tag specifying which data to get.
    /// </summary>
    /// <exception cref="ArgumentException">
    ///     The data tag specified is not valid for getting data.
    /// </exception>
    [ObsoleteAttribute("This property is obsolete. Use DataTag instead", false)]
    public PivDataTag Tag
    {
        get => (PivDataTag)_tag;
        set
        {
            if (!Enum.IsDefined(typeof(PivDataTag), value)
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

    #region IYubiKeyCommand<GetDataResponse> Members

    /// <summary>
    ///     Gets the YubiKeyApplication to which this command belongs. For this
    ///     command it's PIV.
    /// </summary>
    /// <value>
    ///     YubiKeyApplication.Piv
    /// </value>
    public YubiKeyApplication Application => YubiKeyApplication.Piv;

    /// <inheritdoc />
    public CommandApdu CreateCommandApdu() =>
        new()
        {
            Ins = PivGetDataInstruction,
            P1 = PivGetDataParameter1,
            P2 = PivGetDataParameter2,
            Data = BuildGetDataApduData()
        };

    /// <inheritdoc />
    public GetDataResponse CreateResponseForApdu(ResponseApdu responseApdu) => new(responseApdu);

    #endregion

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
            _ => new byte[] { PivGetDataTlvTag, 0x03, (byte)(_tag >> 16), (byte)(_tag >> 8), (byte)_tag }
        };
    }
}
