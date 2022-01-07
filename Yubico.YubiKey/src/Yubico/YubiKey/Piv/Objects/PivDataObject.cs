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

namespace Yubico.YubiKey.Piv.Objects
{
    /// <summary>
    /// This abstract class defines the basic properties of a PIV Application
    /// Data Object.
    /// </summary>
    /// <remarks>
    /// Generally you will use one of the <see cref="PivSession.ReadObject"/> methods to
    /// get the specified data out of a YubiKey. The formatted data will be
    /// parsed and the resulting object will present the data in a more readable
    /// form. You can then update the data and call
    /// <see cref="PivSession.WriteObject"/>.
    /// <para>
    /// Note that if there is no data on the YubiKey stored under the given
    /// object, then after calling <c>ReadObject</c>, the resulting PivDataObject
    /// will be "empty".
    /// </para>
    /// <para>
    /// You can also create a new instance of a PivDataObject (call the
    /// constructor directly, rather than getting the YubiKey's contents), set
    /// it, and store it. However, when you store data (by calling
    /// <c>WriteObject</c>), you overwrite any data already there. Hence, you
    /// will likely want to get any data out first, to decide whether you want to
    /// change anything, rather than overwriting any possible contents sight
    /// unseen.
    /// </para>
    /// <para>
    /// This class (and all its subclasses) implements <c>IDisposable</c> because
    /// the data might be sensitive. Upon disposal, any stored data is
    /// overwritten.
    /// </para>
    /// </remarks>
    public abstract class PivDataObject : IDisposable
    {
        private const int MinPivDataTag = 0x005FC102;
        private const int MaxPivDataTag = 0x005FC123;
        private const int MinRetiredDataTag = 0x005FC10D;
        private const int MaxRetiredDataTag = 0x005FC120;
        private const int MinVendorDataTag = 0x005FFF02;
        private const int MaxVendorDataTag = 0x005FFFFF;
        private const int AttestCertDataTag = 0x005FFF01;
        private const int UnusedDataTag = 0x005FC104;
        private const int AuthCertDataTag = 0x005FC105;
        private const int SigCertDataTag = 0x005FC10A;
        private const int MgmtCertDataTag = 0x005FC10B;

        /// <summary>
        /// Indicates whether there is any data or not. If this is true, then the
        /// object is empty and the contents of any property are meaningless.
        /// </summary>
        public bool IsEmpty { get; protected set; }

        /// <summary>
        /// The value used to specify the storage location.
        /// </summary>
        /// <remarks>
        /// Where, on the YubiKey, data is stored is determined by the
        /// <c>DataTag</c>. It is a number such as <c>0x005fC102</c> or
        /// <c>0x005FFF00</c>.
        /// <para>
        /// There are some tag values defined by the PIV standard, and there are
        /// others defined by Yubico (see the User's Manual entry on
        /// <xref href="UsersManualPivCommands#get-data"> GET DATA</xref> and
        /// <xref href="UsersManualPivCommands#get-and-put-vendor-data"> GET vendor data</xref>).
        /// </para>
        /// <para>
        /// When you instantiate an object that is a subclass of this abstract
        /// class, this property will be set with the defined (or sometimes it's
        /// called the default) <c>DataTag</c>. However, it is possible to change
        /// that tag. See <see cref="SetDataTag"/>. This is not recommended, but
        /// it is possible because there are some applications that have a use
        /// case for such a change.
        /// </para>
        /// </remarks>
        public int DataTag { get; protected set; }

        /// <summary>
        /// Set the <c>DataTag</c> to the new value, using this new value as an
        /// alternate to the defined.
        /// </summary>
        /// <remarks>
        /// Changing the <c>DataTag</c> means storing the data under an alternate
        /// tag. That is, there are specific tags defined for specific data
        /// constructions. For example, there is a tag for CHUID
        /// (<c>0x005FC102</c>), and specific data formatted following a specific
        /// TLV construction. However, if you want to store CHUID data under an
        /// alternate tag (it will still be the CHUID data formatted following
        /// the CHUID definition), you can set the <c>DataTag</c>.
        /// <para>
        /// You will likely never have a use case in your application for using
        /// an alternate <c>DataTag</c> but this feature is available for those
        /// rare cases when it can be useful.
        /// </para>
        /// <para>
        /// Note that it can be dangerous to change the <c>DataTag</c> as well,
        /// because some tags require the PIN to read and others do not. For
        /// example, if you store some sensitive data in the PRINTED storage
        /// area, PIN verification is required to retrieve it. But suppose you
        /// change the <c>DataTag</c> to, say, SECURITY (<c>0x005FC106</c>). That
        /// storage area does not require the PIN to retrieve the data.
        /// </para>
        /// <para>
        /// It is also possible some other application wants to use a storage
        /// area for its intended purpose, but using an alternate <c>DataTag</c>
        /// might overwrite existing data, or the other application ecpects to be
        /// able to read data in a particular format but cannot.
        /// </para>
        /// <para>
        /// It is not possible to change the <c>DataTag</c> to just any integer
        /// value. The new tag must either be an existing defined tag (see the
        /// User's Manual entries on
        /// <xref href="UsersManualPivCommands#get-data"> GET DATA</xref> and
        /// <xref href="UsersManualPivCommands#get-and-put-vendor-data"> GET vendor data</xref>),
        /// that is, a number between <c>0x005FC100</c> and <c>0x005FC123</c> or
        /// a value between <c>0x005FFF00</c> and <c>0x005FFFFF</c> (inclusive).
        /// In addition, there are some values still not allowed. For example,
        /// the YubiKey does not allow storing data in the DISCOVERY storage area
        /// (<c>DataTag = 0x7E</c>). Also, only certificates are allowed in the
        /// ATTESTATION CERT storage area (<c>DataTag = 0x005FFF01</c>) and there
        /// is a <c>PivSession</c> method dedicated to this data object (see
        /// <see cref="PivSession.ReplaceAttestationKeyAndCertificate"/>) for
        /// this operation. Hence, this method will not allow using that data tag.
        /// </para>
        /// <para>
        /// These are the data tags that this method will not allow.
        /// <list type="bullet">
        /// <item><description><c>0x0000007E</c></description></item>
        /// <item><description><c>0x00007F61</c></description></item>
        /// <item><description><c>0x005FC101</c></description></item>
        /// <item><description><c>0x005FC104</c></description></item>
        /// <item><description><c>0x005FC105</c></description></item>
        /// <item><description><c>0x005FC10A</c></description></item>
        /// <item><description><c>0x005FC10B</c></description></item>
        /// <item><description><c>0x005FC10D through 0x005FC120</c></description></item>
        /// <item><description><c>0x005FFF01</c></description></item>
        /// </list>
        /// </para>
        /// <para>
        /// If you change the <c>DataTag</c>, then the data specified in the
        /// object, including its format, will be stored under a different tag.
        /// For example, if you build a <see cref="CardholderUniqueId"/> object
        /// and leave the <c>DataTag</c> alone, then when you store the data it
        /// will be stored in the YubiKey's CHUID storage area. But if you build
        /// the object and then change the <c>DataTag</c> to, say,
        /// <c>0x005FC103</c> (Fingerprints), when you store the data, it will be
        /// the CHUID data formatted according to the PIV specification for
        /// CHUID, but stored in the Fingerprints storage area.
        /// </para>
        /// </remarks>
        /// <param name="newDataTag">
        /// The alternate data tag under which the data is to be stored or
        /// retrieved.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The <c>newDataTag</c> in not allowed to be an alternate tag.
        /// </exception>
        public void SetDataTag(int newDataTag)
        {
            if (!IsValidAlternateTag(newDataTag))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.CannotUseDataTagAsAlternate,
                        newDataTag));
            }

            DataTag = newDataTag;
        }

        // Is the given dataTag valid as an alternate?
        private static bool IsValidAlternateTag(int dataTag)
        {
            if ((dataTag < MinPivDataTag) || (dataTag > MaxVendorDataTag))
            {
                return false;
            }
            if ((dataTag > MaxPivDataTag) && (dataTag < MinVendorDataTag))
            {
                return false;
            }
            if ((dataTag >= MinRetiredDataTag) && (dataTag <= MaxRetiredDataTag))
            {
                return false;
            }
            if ((dataTag == AttestCertDataTag) ||
                (dataTag == UnusedDataTag) ||
                (dataTag == AuthCertDataTag) ||
                (dataTag == SigCertDataTag) ||
                (dataTag == MgmtCertDataTag))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get the defined data tag. This is the data tag that the PIV
        /// standard or Yubico defines to specify the given data object.
        /// </summary>
        /// <remarks>
        /// This is also called the default data tag. This method will always
        /// return the defined tag, regardless of what the <c>DataTag</c>
        /// property returns. That is, if you call <c>SetDataTag</c> to change
        /// the tag under which the data is to be stored, the <c>DataTag</c>
        /// property will reflect that change. But this method will still return
        /// the original, defined tag.
        /// </remarks>
        /// <returns>
        /// The data tag defined for the data object.
        /// </returns>
        public abstract int GetDefinedDataTag();

        /// <summary>
        /// Build the encoding of the data.
        /// </summary>
        /// <remarks>
        /// Each data object has a defined format. See the User's Manual entry on
        /// <xref href="UsersManualPivCommands#get-data"> GET DATA</xref> and
        /// <xref href="UsersManualPivCommands#get-and-put-vendor-data"> GET vendor data</xref>
        /// for descriptions of the formats. This method will build a new byte
        /// array containing the data set in the object. This data will generally
        /// then be stored on the YubiKey.
        /// <para>
        /// If the object is empty (<c>IsEmpty</c> is <c>true</c>), then this
        /// method will throw an exception.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A new byte array containing the encoded data object.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The object is empty (there is no data to encode).
        /// </exception>
        public abstract byte[] Encode();

        /// <summary>
        /// Decode the data given according to the format specified for the data
        /// object.
        /// </summary>
        /// <remarks>
        /// This will parse the encoding and set local properties with the data.
        /// The <c>encodedData</c> generally was retrieved from the YubiKey.
        /// <para>
        /// This will replace any data in the object.
        /// </para>
        /// <para>
        /// If there is no data (<c>encodedData.Length</c> is 0) this method will
        /// set the object to the empty state (<c>IsEmpty</c> will be <c>true</c>
        /// and the contents of any data properties will be meaningless).
        /// </para>
        /// <para>
        /// If the input is not encoded as expected, this method will throw an
        /// exception. This includes the fixed values. That is, there are some
        /// values in some data objects that are fixed for every YubiKey, and
        /// this method will expect the contents of the <c>encodedData</c> to
        /// contain those fixed values.
        /// </para>
        /// </remarks>
        /// <param name="encodedData">
        /// The data to parse.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The data is not properly encoded for the data object.
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
        /// Try to decode the data given according to the format specified for
        /// the data object. If successful, return <c>true</c>, otherwise, return
        /// <c>false</c>.
        /// </summary>
        /// <remarks>
        /// This will parse the encoding and set local properties with the data.
        /// The <c>encodedData</c> generally was retrieved from the YubiKey.
        /// <para>
        /// This will replace any data in the object.
        /// </para>
        /// <para>
        /// If there is no data (<c>encodedData.Length</c> is 0) this method will
        /// set the object to the empty state (<c>IsEmpty</c> will be <c>true</c>
        /// and the contents of any data properties will be meaningless) and
        /// return <c>true</c>.
        /// </para>
        /// <para>
        /// If the input is not encoded as expected, this method will set the
        /// object to the empty state and return <c>false</c>. This includes the
        /// fixed values. That is, there are some values in some data objects
        /// that are fixed for every YubiKey, and this method will expect the
        /// contents of the <c>encodedData</c> to contain those fixed values.
        /// </para>
        /// </remarks>
        /// <param name="encodedData">
        /// The data to parse.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the method successfully decodes,
        /// <c>false</c> otherwise.
        /// </returns>
        public abstract bool TryDecode(ReadOnlyMemory<byte> encodedData);

        /// <summary>
        /// Releases any unmanaged resources and overwrites any sensitive data.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases any unmanaged resources and overwrites any sensitive data.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
