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
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Yubico.Core.Logging;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv.Objects
{
    /// <summary>
    /// Use this class to process the CHUID (CardHolder Unique Identifier) data.
    /// </summary>
    /// <remarks>
    /// A CHUID consists of five values:
    /// <list type="bullet">
    /// <item><description>FASC-N (Federal Agency SmartCredential Number)</description></item>
    /// <item><description>GUID (Global Unique Identifier)</description></item>
    /// <item><description>Expiration Date</description></item>
    /// <item><description>Issuer Asymmetric Signature</description></item>
    /// <item><description>LRC (error code)</description></item>
    /// </list>
    /// <para>
    /// For the YubiKey, the FASC-N and Expiration Date are fixed. That is, the
    /// FASC-N and Expiration Date are the same for all YubiKeys.
    /// </para>
    /// <para>
    /// The YubiKey does not use the signature value, and the PIV standard does
    /// not use the LRC. Hence, those two values are "empty".
    /// </para>
    /// <para>
    /// You can set the GUID to any 16-byte value you want, but it is generally a
    /// random value. That is so each YubiKey has a different GUID.
    /// </para>
    /// <para>
    /// You will generally get the current CHUID for a YubiKey using one of the
    /// <c>PivSession.ReadObject</c> methods. Upon manufacture, the CHUID is
    /// "empty", so the <c>CardHolderUniqueId</c> object will be empty as well
    /// (the <see cref="PivDataObject.IsEmpty"/> property will be <c>true</c>).
    /// You can then set the GUID (or have a random GUID generated for you) and
    /// then store the CHUID using the <c>PivSession.WriteObject</c> method.
    /// </para>
    /// <para>
    /// It is also possible the CHUID is already set on the YubiKey. In that
    /// case, call one of the <c>PivSession.ReadObject</c> methods and the
    /// resulting object will have <c>IsEmpty</c> set to <c>false</c> and you can
    /// see the GUID that is on the YubiKey.
    /// </para>
    /// <para>
    /// Finally, you can create a new <c>CardholderUniqueId</c> object by calling
    /// the constructor directly, then set the GUID and call
    /// <c>PivSession.WriteObject</c>. That will, of course, overwrite the CHUID
    /// on the YubiKey, if there is one. Because that might not be something you
    /// want to do, this is the most dangerous option.
    /// </para>
    /// <para>
    /// See also the user's manual entry on
    /// <xref href="UsersManualPivObjects"> PIV data objects</xref>.
    /// </para>
    /// </remarks>
    public sealed class CardholderUniqueId : PivDataObject
    {
        private const int ChuidDefinedDataTag = 0x005FC102;
        private const int GuidLength = 16;
        private const int EncodingTag = 0x53;
        private const int FascNumberTag = 0x30;
        private const int GuidTag = 0x34;
        private const int ExpirationDateTag = 0x35;
        private const string FixedDate = "20300101";
        private const int FixedDateYear = 2030;
        private const int FixedDateMonth = 1;
        private const int FixedDateDay = 1;
        private const int SignatureTag = 0x3E;
        private const int LrcTag = 0xFE;

        private bool _disposed;
        private readonly ILogger _log = Log.GetLogger<CardholderUniqueId>();

        /// <summary>
        /// The "Federal Agency Smart Credential Number" (FASC-N). This is a fixed
        /// 25-byte value for every YubiKey, and is a Non-Federal Issuer number.
        /// </summary>
        public ReadOnlyMemory<byte> FascNumber { get; private set; }

        private readonly byte[] _fascNumber = new byte[] {
            0xd4, 0xe7, 0x39, 0xda, 0x73, 0x9c, 0xed, 0x39, 0xce, 0x73, 0x9d, 0x83, 0x68, 0x58, 0x21, 0x08,
            0x42, 0x10, 0x84, 0x21, 0xc8, 0x42, 0x10, 0xc3, 0xeb
        };

        /// <summary>
        /// The "Global Unique Identifier" (GUID). If there is no CHUID, this is
        /// "empty" (Guid.Length will be 0). This is a 16-byte value.
        /// </summary>
        public ReadOnlyMemory<byte> GuidValue { get; private set; }

        private readonly byte[] _guidValue = new byte[GuidLength];

        /// <summary>
        /// The PIV card's expiration date. This is a fixed value for every
        /// YubiKey: Jan 1, 2030.
        /// </summary>
        public DateTime ExpirationDate { get; private set; }

        /// <summary>
        /// Build a new object. This will not get a CHUID from any YubiKey, it
        /// will only build an "empty" object.
        /// </summary>
        /// <remarks>
        /// To read the CHUID data out of a YubiKey, call the
        /// <see cref="PivSession.ReadObject{PivObject}()"/> method.
        /// </remarks>
        public CardholderUniqueId()
        {
            _log.LogInformation("Create a new instance of CardholderUniqueId.");
            _disposed = false;
            DataTag = ChuidDefinedDataTag;

            IsEmpty = true;
            FascNumber = new ReadOnlyMemory<byte>(_fascNumber);
            GuidValue = new ReadOnlyMemory<byte>(_guidValue);
            ExpirationDate = new DateTime(FixedDateYear, FixedDateMonth, FixedDateDay);
        }

        /// <inheritdoc />
        public override int GetDefinedDataTag() => ChuidDefinedDataTag;

        /// <summary>
        /// Set the Guid with a random, 16-byte value.
        /// </summary>
        /// <remarks>
        /// This method will use the random number generator built by
        /// <see cref="CryptographyProviders"/> to generate 16 random bytes as
        /// the new GUID.
        /// <para>
        /// If there is a GUID value already in this object, this method will
        /// overwrite it.
        /// </para>
        /// </remarks>
        public void SetRandomGuid()
        {
            _log.LogInformation("Set the GUID of CardholderUniqueId with a random value.");
            Clear();

            using (var randomObject = CryptographyProviders.RngCreator())
            {
                randomObject.GetBytes(_guidValue, 0, GuidLength);
            }

            IsEmpty = false;
        }

        /// <summary>
        /// Set the Guid with the given value. If the array is not exactly 16
        /// bytes, this method will throw an exception.
        /// </summary>
        /// <remarks>
        /// If there is a GUID value already in this object, this method will
        /// overwrite it.
        /// </remarks>
        /// <param name="guidValue">
        /// The GUID to use.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The data is not exactly 16 bytes.
        /// </exception>
        public void SetGuid(ReadOnlySpan<byte> guidValue)
        {
            _log.LogInformation("Set the GUID of CardholderUniqueId with a caller-supplied value.");
            if (guidValue.Length != GuidLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPivDataObjectLength));
            }

            Clear();

            var dest = new Span<byte>(_guidValue);
            guidValue.CopyTo(dest);
            IsEmpty = false;
            return;
        }

        /// <inheritdoc />
        public override byte[] Encode()
        {
            _log.LogInformation("Encode CardholderUniqueId.");
            if (IsEmpty)
            {
                return new byte[] { 0x53, 0x00 };
            }

            // We're encoding
            //   53 3B
            //      30 19
            //             d4 e7 39 da 73 9c ed 39 ce 73 9d 83 68 58 21 08
            //             42 10 84 21 c8 42 10 c3 eb
            //      34 10
            //         GUID
            //      35 08
            //         32 30 33 30 30 31 30 31
            //      3e 00
            //      fe 00
            var tlvWriter = new TlvWriter();
            var emptySpan = ReadOnlySpan<byte>.Empty;
            using (tlvWriter.WriteNestedTlv(EncodingTag))
            {
                tlvWriter.WriteValue(FascNumberTag, FascNumber.Span);
                tlvWriter.WriteValue(GuidTag, GuidValue.Span);
                tlvWriter.WriteString(ExpirationDateTag, FixedDate, System.Text.Encoding.ASCII);
                tlvWriter.WriteValue(SignatureTag, emptySpan);
                tlvWriter.WriteValue(LrcTag, emptySpan);
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

            // We're looking for a CHUID that is encoded as
            //   53 3B
            //      30 19
            //             d4 e7 39 da 73 9c ed 39 ce 73 9d 83 68 58 21 08
            //             42 10 84 21 c8 42 10 c3 eb
            //      34 10
            //         <16 random bytes>
            //      35 08
            //         32 30 33 30 30 31 30 31
            //      3e 00
            //      fe 00
            var tlvReader = new TlvReader(encodedData);
            bool isValid = tlvReader.TryReadNestedTlv(out tlvReader, EncodingTag);
            isValid = TryReadFascNumber(isValid, tlvReader);
            isValid = TryReadGuid(isValid, tlvReader);
            isValid = TryReadExpirationDate(isValid, tlvReader);
            isValid = TryReadTrailingElements(isValid, tlvReader);

            // If isValid is true, then we successfully decoded, so the object is
            // not empty (IsEmpty should be set to false). If isValid is false,
            // then the object is empty (IsEmpty should be set to true).
            IsEmpty = !isValid;

            return isValid;
        }

        // We're expecting 30 19 fasc-n with a fixed value.
        // Try to decode and verify the data is as expected.
        // If everything is correct, return true, otherwise, return false.
        // if the input isValid is false, don't bother doing anything, just
        // return false.
        private bool TryReadFascNumber(bool isValid, TlvReader tlvReader)
        {
            if (isValid)
            {
                _log.LogInformation("Decode data into CardholderUniqueId: FascNumber.");
                if (tlvReader.TryReadValue(out var encodedFascn, FascNumberTag))
                {
                    if (MemoryExtensions.SequenceEqual<byte>(encodedFascn.Span, FascNumber.Span))
                    {
                        var dest = new Memory<byte>(_fascNumber);
                        encodedFascn.CopyTo(dest);
                        return true;
                    }
                }
            }

            return false;
        }

        // We're expecting 34 10 guid.
        // Try to decode and verify the data is as expected.
        // If everything is correct, return true, otherwise, return false.
        // if the input isValid is false, don't bother doing anything, just
        // return false.
        private bool TryReadGuid(bool isValid, TlvReader tlvReader)
        {
            if (isValid)
            {
                _log.LogInformation("Decode data into CardholderUniqueId: Guid.");
                if (tlvReader.TryReadValue(out var encodedGuidBytes, GuidTag))
                {
                    if (encodedGuidBytes.Length == GuidLength)
                    {
                        var dest = new Memory<byte>(_guidValue);
                        encodedGuidBytes.CopyTo(dest);
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryReadExpirationDate(bool isValid, TlvReader tlvReader)
        {
            if (isValid)
            {
                _log.LogInformation("Decode data into CardholderUniqueId: ExpirationDate.");
                if (tlvReader.TryReadString(out string theDate, ExpirationDateTag, System.Text.Encoding.ASCII))
                {
                    if (theDate.Equals(FixedDate, StringComparison.Ordinal))
                    {
                        ExpirationDate = new DateTime(FixedDateYear, FixedDateMonth, FixedDateDay);
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryReadTrailingElements(bool isValid, TlvReader tlvReader)
        {
            if (isValid)
            {
                _log.LogInformation("Decode data into CardholderUniqueId: TrailingElements.");
                if (tlvReader.TryReadValue(out var signatureBytes, SignatureTag))
                {
                    if (signatureBytes.Length == 0 && tlvReader.TryReadValue(out var lrc, LrcTag))
                    {
                        if (lrc.Length == 0 && !tlvReader.HasData)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void Clear()
        {
            CryptographicOperations.ZeroMemory(_guidValue);
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
