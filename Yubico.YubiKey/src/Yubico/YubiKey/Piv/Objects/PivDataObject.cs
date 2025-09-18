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

namespace Yubico.YubiKey.Piv.Objects;

/// <summary>
///     This abstract class defines the basic properties of a PIV Application
///     Data Object.
/// </summary>
/// <remarks>
///     Generally you will use one of the <see cref="PivSession.ReadObject{T}()" /> methods to
///     get the specified data out of a YubiKey. The formatted data will be
///     parsed and the resulting object will present the data in a more readable
///     form. You can then update the data and call
///     <see cref="PivSession.WriteObject" />.
///     <para>
///         Note that if there is no data on the YubiKey stored under the given
///         object, then after calling <c>ReadObject</c>, the resulting
///         <c>PivDataObject</c> will be "empty" (<see cref="IsEmpty" />)
///     </para>
///     <para>
///         You can also create a new instance of a <c>PivDataObject</c> (call the
///         constructor directly, rather than getting the YubiKey's contents), set
///         it, and store it. However, when you store data (by calling
///         <c>WriteObject</c>), you overwrite any data already there. Hence, you
///         will likely want to get any data out first, to decide whether you want to
///         change anything, rather than overwriting any possible contents sight
///         unseen.
///     </para>
///     <para>
///         This class (and each subclass) implements <c>IDisposable</c> because the
///         data might be sensitive. Upon disposal, any stored data is overwritten.
///     </para>
///     <para>
///         See also the user's manual entry on
///         <xref href="UsersManualPivObjects"> PIV data objects</xref>.
///     </para>
/// </remarks>
public abstract class PivDataObject : IDisposable
{
    private const int MinVendorDataTag = 0x005F0000;
    private const int MaxVendorDataTag = 0x005FFFFF;
    private const int MinPivDataTag = 0x005FC101;
    private const int MaxPivDataTag = 0x005FC123;
    private const int MinYubicoDataTag = 0x005FFF00;
    private const int MaxYubicoDataTag = 0x005FFF15;

    private int _dataTag;

    /// <summary>
    ///     Indicates whether there is any data or not. If this is true, then
    ///     the contents of any property are meaningless.
    /// </summary>
    /// <remarks>
    ///     Note that is it possible for some Data Objects to contain data, but
    ///     all that data is "default" or "nothing". For example, the
    ///     <see cref="KeyHistory" /> class contains numbers of certs and a URL.
    ///     It is possible a YubiKey contains and encoded Key History in the Key
    ///     History data location, but that data includes no certs and no URL.
    ///     <para>
    ///         Suppose you build a <c>KeyHistory</c> object using the
    ///         <see cref="PivSession.ReadObject{PivObject}()" /> method, and the
    ///         YubiKey contains data in the Key History storage area, but that data
    ///         indicates there are no certs and no URL. The resulting object will
    ///         not be empty (the <c>IsEmpty</c> field will be <c>false</c>).
    ///         However, the properties describing the contents will be zero and
    ///         NULL.
    ///     </para>
    ///     <para>
    ///         If you build the <c>KeyHistory</c> object using the constructor, it
    ///         will begin as empty, but if you set any properties, even to zero or
    ///         null, the object will become not empty.
    ///     </para>
    /// </remarks>
    public bool IsEmpty { get; protected set; }

    /// <summary>
    ///     The value used to specify the storage location.
    /// </summary>
    /// <remarks>
    ///     Where, on the YubiKey, data is stored is determined by the
    ///     <c>DataTag</c>. It is a number such as <c>0x005fC102</c> or
    ///     <c>0x005FFF00</c>.
    ///     <para>
    ///         There are some tag values defined by the PIV standard, and there are
    ///         others defined by Yubico (see the User's Manual entry on
    ///         <xref href="UsersManualPivCommands#getdatatable"> GET DATA</xref> and
    ///         <xref href="UsersManualPivCommands#getvendordatatable"> GET vendor data</xref>).
    ///         In addition, some numbers are accepted by a YubiKey even though no
    ///         one has defined their use or contents. These are the numbers
    ///         <c>0x005F0000</c> through <c>0x005FFFFF</c> (inclusive) not already
    ///         specified.
    ///     </para>
    ///     <para>
    ///         When you instantiate an object that is a subclass of this abstract
    ///         class, this property will be set with the defined (or sometimes it's
    ///         called the default) <c>DataTag</c>. However, it is possible to change
    ///         that tag. See the User's manual entry on
    ///         <xref href="UsersManualPivObjects#using-an-alternate-datatag"> PIV data objects</xref>
    ///         for more information on what valid data tags are possible. If you try
    ///         to change to an unsupported tag, the SDK will throw an exception.
    ///     </para>
    ///     <para>
    ///         Note that changing the <c>DataTag</c> is not recommended, but it is
    ///         possible because there are some applications that have a use case for
    ///         such a feature. See the User's Manual entry on
    ///         <xref href="UsersManualPivObjects#using-an-alternate-datatag"> PIV data objects</xref>.
    ///         for a more detailed description of this topic.
    ///     </para>
    /// </remarks>
    public int DataTag
    {
        get => _dataTag;
        set
        {
            if (!IsValidAlternateTag(value))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.CannotUseDataTagAsAlternate,
                        value));
            }

            _dataTag = value;
        }
    }

    #region IDisposable Members

    /// <summary>
    ///     Releases any unmanaged resources and overwrites any sensitive data.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion

    /// <summary>
    ///     Is the given tag valid as an alternate?
    /// </summary>
    /// <param name="dataTag">
    ///     The data tag the caller wants to use as an alternate.
    /// </param>
    /// <returns>
    ///     A boolean, <c>true</c> is the given tag can be used as an alternate,
    ///     <c>false</c> otherwise.
    /// </returns>
    protected virtual bool IsValidAlternateTag(int dataTag)
    {
        if (dataTag != GetDefinedDataTag())
        {
            if (dataTag < MinVendorDataTag || dataTag > MaxVendorDataTag
                || (dataTag >= MinPivDataTag && dataTag <= MaxPivDataTag)
                || (dataTag >= MinYubicoDataTag && dataTag <= MaxYubicoDataTag))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Get the defined data tag. This is the data tag that the PIV
    ///     standard or Yubico defines to specify the given data object.
    /// </summary>
    /// <remarks>
    ///     This is also called the default data tag. This method will always
    ///     return the defined tag, regardless of what the <c>DataTag</c>
    ///     property returns. That is, even if you change the <c>DataTag</c> this
    ///     method will still return the original, defined tag.
    /// </remarks>
    /// <returns>
    ///     The data tag defined for the data object.
    /// </returns>
    public abstract int GetDefinedDataTag();

    /// <summary>
    ///     Build the encoding of the data.
    /// </summary>
    /// <remarks>
    ///     Each data object has a defined format. See the User's Manual entry on
    ///     <xref href="UsersManualPivCommands#getdatatable"> GET DATA</xref> and
    ///     <xref href="UsersManualPivCommands#getvendordatatable"> GET vendor data</xref>
    ///     for descriptions of the formats. This method will build a new byte
    ///     array containing the data set in the object. This data will generally
    ///     then be stored on the YubiKey.
    ///     <para>
    ///         Note that this method returns a new byte array, not a reference to an
    ///         array inside the object. If this array contains any sensitive data,
    ///         make sure you overwrite it when done with it.
    ///     </para>
    ///     <para>
    ///         If the object is empty (<c>IsEmpty</c> is <c>true</c>), then this
    ///         method will return the encoding of no data, which is <c>0x53 00</c>.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     A new byte array containing the encoded data object.
    /// </returns>
    public abstract byte[] Encode();

    /// <summary>
    ///     Decode the data given according to the format specified for the data
    ///     object.
    /// </summary>
    /// <remarks>
    ///     This will parse the encoding and set local properties with the data.
    ///     The <c>encodedData</c> generally was retrieved from the YubiKey.
    ///     <para>
    ///         This will replace any data in the object.
    ///     </para>
    ///     <para>
    ///         If there is no data (<c>encodedData.Length</c> is 0) this method will
    ///         set the object to the empty state (<c>IsEmpty</c> will be <c>true</c>
    ///         and the contents of any data properties will be meaningless).
    ///     </para>
    ///     <para>
    ///         If the input is not encoded as expected, this method will throw an
    ///         exception. This includes the fixed values. That is, there are some
    ///         values in some data objects that are fixed for every YubiKey, and
    ///         this method will expect the contents of the <c>encodedData</c> to
    ///         contain those fixed values.
    ///     </para>
    /// </remarks>
    /// <param name="encodedData">
    ///     The data to parse.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     The data is not properly encoded for the data object.
    /// </exception>
    public void Decode(ReadOnlyMemory<byte> encodedData)
    {
        if (!TryDecode(encodedData))
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidDataEncoding));
        }
    }

    /// <summary>
    ///     Try to decode the data given according to the format specified for
    ///     the data object. If successful, return <c>true</c>, otherwise, return
    ///     <c>false</c>.
    /// </summary>
    /// <remarks>
    ///     This will parse the encoding and set local properties with the data.
    ///     The <c>encodedData</c> generally was retrieved from the YubiKey.
    ///     <para>
    ///         This will replace any data in the object.
    ///     </para>
    ///     <para>
    ///         If there is no data (<c>encodedData.Length</c> is 0) this method will
    ///         set the object to the empty state (<c>IsEmpty</c> will be <c>true</c>
    ///         and the contents of any data properties will be meaningless) and
    ///         return <c>true</c>.
    ///     </para>
    ///     <para>
    ///         If the input is not encoded as expected, this method will set the
    ///         object to the empty state and return <c>false</c>. This includes the
    ///         fixed values. That is, there are some values in some data objects
    ///         that are fixed for every YubiKey, and this method will expect the
    ///         contents of the <c>encodedData</c> to contain those fixed values.
    ///     </para>
    ///     <para>
    ///         If the input is encoded as expected, yet the data in that encoding is
    ///         invalid (e.g. some element is not the correct length), this method
    ///         will return <c>false</c>.
    ///     </para>
    /// </remarks>
    /// <param name="encodedData">
    ///     The data to parse.
    /// </param>
    /// <returns>
    ///     A boolean, <c>true</c> if the method successfully decodes,
    ///     <c>false</c> otherwise.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     The data is not properly encoded for the data object.
    /// </exception>
    public abstract bool TryDecode(ReadOnlyMemory<byte> encodedData);

    /// <summary>
    ///     Releases any unmanaged resources and overwrites any sensitive data.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
    }
}
