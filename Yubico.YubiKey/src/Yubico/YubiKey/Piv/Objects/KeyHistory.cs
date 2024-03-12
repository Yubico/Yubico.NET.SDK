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
using System.Text;
using Yubico.Core.Logging;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Piv.Objects
{
    /// <summary>
    /// Use this class to process the Key History data.
    /// </summary>
    /// <remarks>
    /// A Key History consists of three values:
    /// <list type="bullet">
    /// <item><description>Number of keys with on-card certificates</description></item>
    /// <item><description>Number of keys with off-card certificates</description></item>
    /// <item><description>Off-card certificate URL (if off-card or on-card certs
    /// value is greater than zero)</description></item>
    /// </list>
    /// <para>
    /// The YubiKey will not automatically set the number of on-card certs value.
    /// For example, suppose you call the method
    /// <see cref="PivSession.ImportCertificate"/> for a slot that has no cert.
    /// There is now one more key with an on-card cert. However, the YubiKey will
    /// not increment the value in the Key History storage area. If you want the
    /// Key History to reflect the number of keys with certs on the card, you
    /// must set this data object yourself.
    /// </para>
    /// <para>
    /// The Off-card certificate URL is where the off-card certs can be found.
    /// This should be set if the number of off-card certs is greater than zero.
    /// If there are no off-card certs, this is generally null. However, the PIV
    /// standard allows for a non-null URL if either or both the number of
    /// on-card and off-card certs is not zero. That is, if the number of
    /// off-card certs is zero, but the number of on-card certs is not zero, then
    /// it is permissible to have an off-card cert URL.
    /// </para>
    /// <para>
    /// This class will not check to make sure the values you set for the numbers
    /// of certificates matches the YubiKey contents. For example, suppose you
    /// have a YubiKey with only four private keys. Hence, the maximum
    /// <c>OnCardCertificates</c> is four. But there is nothing stopping you from
    /// creating a <c>KeyHistory</c> object and setting <c>OnCardCertificates</c>
    /// to 20, 30, or even 255.
    /// </para>
    /// <para>
    /// If you create an instance of <c>KeyHistory</c>, it will be empty
    /// (<c>IsEmpty</c> will be <c>true</c>). Once you set one of the properties
    /// (<c>OnCardCertificates</c> or <c>OffCardCertificates</c>), the object
    /// will no longer be empty, even if you set those values to zero. If a
    /// <c>PivDataObject</c> is empty, the <c>PivSession.WriteObject</c> method
    /// will not write anything to the YubiKey. If the Data Object is not empty,
    /// the <c>WriteObject</c> method will write to the YubiKey. So if you want
    /// to write a Key History to the YubiKey that contains the information that
    /// there are no certs and no URL, then create a new <c>KeyHistory</c> object,
    /// set one of the properties to zero, and call the <c>Write</c> method.
    /// </para>
    /// <para>
    /// If you create a new <c>KeyHistory</c> object by calling the constructor
    /// directly, then set the properties and call <c>PivSession.WriteObject</c>,
    /// that will, of course, overwrite the Key History on the YubiKey, if there
    /// is one. Because that might not be something you want to do, this is the
    /// most dangerous option.
    /// </para>
    /// <para>
    /// See also the user's manual entry on
    /// <xref href="UsersManualPivObjects"> PIV data objects</xref>.
    /// </para>
    /// </remarks>
    public sealed class KeyHistory : PivDataObject
    {
        private const int KeyHistoryDefinedDataTag = 0x005FC10C;
        private const int MaximumUrlLength = 118;
        private const int EncodingTag = 0x53;
        private const int OnCardTag = 0xC1;
        private const int OffCardTag = 0xC2;
        private const int UrlTag = 0xF3;
        private const int UnusedTag = 0xFE;

        private bool _disposed;
        private readonly Logger _log = Log.GetLogger();

        /// <summary>
        /// Number of Keys with On-Card Certificates. If you set this to zero,
        /// and the <c>OffCardCertificates</c> property is also zero, the
        /// <c>OffCardCertificateUrl</c> property will automatically be set to
        /// null.
        /// </summary>
        public byte OnCardCertificates
        {
            get => _onCardCerts;
            set
            {
                _onCardCerts = value;
                SetOffCardUrlNullIfZeroCerts();
                IsEmpty = false;
            }
        }

        private byte _onCardCerts;

        /// <summary>
        /// Number of Keys with Off-Card Certificates. If you set this to zero,
        /// and the <c>OnCardCertificates</c> property is also zero, the
        /// <c>OffCardCertificateUrl</c> property will automatically be set to
        /// null.
        /// </summary>
        public byte OffCardCertificates
        {
            get => _offCardCerts;
            set
            {
                _offCardCerts = value;
                SetOffCardUrlNullIfZeroCerts();
                IsEmpty = false;
            }
        }

        private byte _offCardCerts;

        /// <summary>
        /// The URL where the Off-Card Certificates can be found. If there are no
        /// On-Card or Off-Card Certs, it can only be set to null.
        /// </summary>
        /// <remarks>
        /// This class will use the <c>AbsoluteUri</c> property of the <c>Uri</c>
        /// class. Furthermore, it will "convert" it to bytes by using the UTF8
        /// encoding. That is, you will build the <c>Uri</c> object, and when this
        /// class builds the encoded <c>KeyHistory</c>, it will extract the
        /// <c>AbsoluteUri</c> property and convert it into a byte array made up
        /// of the UTF8 encoding for the off-card cert URL portion.
        /// <para>
        /// The PIV standard specifies that this value be made up of 118 bytes or
        /// fewer. If the UTF8 encoding of the <c>AbsoluteUri</c> is greater than
        /// 118, this class will throw an exception.
        /// </para>
        /// If this property is set to something other than null, and the
        /// <c>OnCardCertificates</c> and <c>OffCardCertificates</c> properties
        /// are both zero, a call to <c>Encode</c> will throw an exception.
        /// </remarks>
        public Uri? OffCardCertificateUrl
        {
            get => _offCardCertUrl;
            set => SetOffCardCertUrl(value);
        }

        private Uri? _offCardCertUrl;
        private byte[]? _urlBytes;

        /// <summary>
        /// Build a new object. This will not get the Key History from any
        /// YubiKey, it will only build an "empty" object.
        /// </summary>
        /// <remarks>
        /// To read the Key History data out of a YubiKey, call the
        /// <see cref="PivSession.ReadObject{PivObject}()"/> method.
        /// </remarks>
        public KeyHistory()
        {
            _log.LogInformation("Create a new instance of KeyHistory.");
            _disposed = false;
            DataTag = KeyHistoryDefinedDataTag;
            IsEmpty = true;
        }

        /// <inheritdoc />
        public override int GetDefinedDataTag() => KeyHistoryDefinedDataTag;

        /// <inheritdoc />
        public override byte[] Encode()
        {
            _log.LogInformation("Encode KeyHistory.");
            if (IsEmpty)
            {
                return new byte[] { 0x53, 0x00 };
            }

            if (_onCardCerts == 0 && _offCardCerts == 0 && !(OffCardCertificateUrl is null))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPivDataObjectValue));
            }

            // We're encoding
            //   53 len
            //      C1 01
            //         --number of keys with on card certs--
            //      C2 01
            //         --number of keys with off card certs--
            //      F3 len
            //         off card cert URL
            //      FE 00
            var tlvWriter = new TlvWriter();
            using (tlvWriter.WriteNestedTlv(EncodingTag))
            {
                tlvWriter.WriteByte(OnCardTag, _onCardCerts);
                tlvWriter.WriteByte(OffCardTag, _offCardCerts);
                if (_urlBytes is null)
                {
                    tlvWriter.WriteValue(UrlTag, ReadOnlySpan<byte>.Empty);
                }
                else
                {
                    tlvWriter.WriteValue(UrlTag, _urlBytes);
                }
                tlvWriter.WriteValue(UnusedTag, ReadOnlySpan<byte>.Empty);
            }

            byte[] returnValue = tlvWriter.Encode();
            tlvWriter.Clear();
            return returnValue;
        }

        /// <inheritdoc />
        public override bool TryDecode(ReadOnlyMemory<byte> encodedData)
        {
            _log.LogInformation("Decode data into CardholderUniqueId.");

            Clear();
            if (encodedData.Length == 0)
            {
                return true;
            }

            // We're looking for a KeyHistory that is encoded as
            //   53 len
            //      C1 01
            //         --number of keys with on card certs--
            //      C2 01
            //         --number of keys with off card certs--
            //      F3 len
            //         off card cert URL
            //      FE 00
            byte onCard = 0;
            byte offCard = 0;
            ReadOnlyMemory<byte> offCardUrl = ReadOnlyMemory<byte>.Empty;
            var tlvReader = new TlvReader(encodedData);
            bool isValid = tlvReader.TryReadNestedTlv(out tlvReader, EncodingTag);
            if (isValid)
            {
                isValid = tlvReader.TryReadByte(out onCard, OnCardTag);
            }
            if (isValid)
            {
                isValid = tlvReader.TryReadByte(out offCard, OffCardTag);
            }
            if (isValid)
            {
                isValid = tlvReader.TryReadValue(out offCardUrl, UrlTag);
            }
            if (isValid)
            {
                isValid = tlvReader.TryReadValue(out ReadOnlyMemory<byte> unusedData, UnusedTag);
                if (isValid)
                {
                    isValid = unusedData.Length == 0;
                }
            }

            if (isValid)
            {
                OnCardCertificates = onCard;
                OffCardCertificates = offCard;

                if (offCardUrl.Length != 0)
                {
                    string urlString = new string(Encoding.UTF8.GetChars(offCardUrl.ToArray()));
                    OffCardCertificateUrl = new Uri(urlString);
                }
            }

            return isValid;
        }

        // If the urlValue is null, just set _offCardCertUrl to null. Otherwise,
        // determine if it is permissible to set the URL. If it is, verify the
        // input value is within the required limits.
        private void SetOffCardCertUrl(Uri? urlValue)
        {
            _urlBytes = null;
            if (!(urlValue is null))
            {
                _urlBytes = Encoding.UTF8.GetBytes(urlValue.AbsoluteUri);
                if (_urlBytes.Length > MaximumUrlLength)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidPivDataObjectLength));
                }
            }

            _offCardCertUrl = urlValue;
            IsEmpty = false;
        }

        // If the _onCardCerts and _offCardCerts values are both zero, then set
        // the URL to null. Otherwise, leave it alone.
        private void SetOffCardUrlNullIfZeroCerts()
        {
            if (_onCardCerts == 0 && _offCardCerts == 0)
            {
                _offCardCertUrl = null;
                _urlBytes = null;
            }
        }

        private void Clear()
        {
            _onCardCerts = 0;
            _offCardCerts = 0;
            _offCardCertUrl = null;
            _urlBytes = null;
            IsEmpty = true;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                Clear();
            }

            base.Dispose(disposing);
            _disposed = true;
        }
    }
}
