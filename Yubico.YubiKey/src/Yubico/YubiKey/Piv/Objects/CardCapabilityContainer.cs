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
    /// Use this class to process the CCC (Card Capability Container) data.
    /// </summary>
    /// <remarks>
    /// The PIV standard declares,
    /// <para>
    /// <i>"The Card Capability Container (CCC) is a mandatory data object whose
    /// purpose is to facilitate compatibility of Government Smart Card
    /// Interoperability Specification (GSC-IS) applications with PIV Cards."</i>
    /// </para>
    /// <para>
    /// In other words, it's a holdover from the older smart card specification.
    /// In order to remain compatible with that older spec and with older
    /// applications, it might be necessary to read and write this data object.
    /// </para>
    /// <para>
    /// There are many elements that make up the CCC, but most of them are
    /// ignored by PIV and the YubiKey. Other elements are fixed. Note that the
    /// PIV standard says,
    /// </para>
    /// <para>
    /// <i>"The data model of the PIV Card Application shall be identified by
    /// data model number 0x10. ... The content of the CCC data elements, other
    /// than the data model number, are out of scope for this specification."</i>
    /// </para>
    /// <para>
    /// There is only one element that can be set in this class, namely, the Card
    /// Identifier portion of the Unique Card Identifier. This is a 14-byte
    /// value. With the YubiKey, the caller sets it, or allows the SDK to set it
    /// to random bytes.
    /// </para>
    /// <para>
    /// Upon manufacture, the CCC is "empty", so the
    /// <see cref="PivDataObject.IsEmpty"/> property is <c>true</c>. This object will
    /// be considered empty until the Card Identifier is set. See
    /// <see cref="SetCardId"/> and <see cref="SetRandomCardId"/>.
    /// </para>
    /// <para>
    /// The following list indicates the elements of the CCC that can be found on
    /// a YubiKey.
    /// <list type="bullet">
    /// <item><description>Unique Card Identifier</description></item>
    /// <item><description>Application Identifier (part of the Unique Card ID</description></item>
    /// <item><description>GSC-RID (Registered Application Provider Identifier,
    /// part of the AID)</description></item>
    /// <item><description>Card Identifier (part of the Unique Card ID)</description></item>
    /// <item><description>Manufacturer ID</description></item>
    /// <item><description>Card Type</description></item>
    /// <item><description>Container Version Number</description></item>
    /// <item><description>Grammar Version Number</description></item>
    /// <item><description>PKCS #15 Version Number (for the YubiKey, this is 0x00
    /// indicating PKCS #15 is not supported</description></item>
    /// <item><description>Data Model Number</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class CardCapabilityContainer : PivDataObject
    {
        private const int CccDefinedDataTag = 0x005FC107;
        private const int AidOffset = 0;
        private const int AidLength = 7;
        private const int GscRidOffset = 0;
        private const int GscRidLength = 5;
        private const int CardIdOffset = 7;
        private const int CardIdLength = 14;
        private const int FixedManufacturerId = 0xFF;
        private const int CardTypeJavaCard = 0x02;
        private const byte FixedContainerVersionNumber = 0x21;
        private const byte FixedGrammarVersionNumber = 0x21;
        private const byte FixedPkcs15VersionNumber = 0x00;
        private const byte FixedDataModelNumber = 0x10;
        private const int EncodingTag = 0x53;
        private const int UniqueCardIdTag = 0xF0;
        private const int UniqueCardIdLength = 0x15;
        private const int ContainerVersionTag = 0xF1;
        private const int GrammarVersionTag = 0xF2;
        private const int UnusedTag1 = 0xF3;
        private const int Pkcs15Tag = 0xF4;
        private const int DataModelTag = 0xF5;
        private const int UnusedTag2 = 0xF6;
        private const int UnusedTag3 = 0xF7;
        private const int UnusedTag4 = 0xFA;
        private const int UnusedTag5 = 0xFB;
        private const int UnusedTag6 = 0xFC;
        private const int UnusedTag7 = 0xFD;
        private const int UnusedTag8 = 0xFE;

        private bool _disposed;
        private readonly ILogger _log = Log.GetLogger<CardCapabilityContainer>();

        /// <summary>
        /// The full Unique Card Identifier which consists of the AID || CardID.
        /// </summary>
        public ReadOnlyMemory<byte> UniqueCardIdentifier { get; private set; }

        private readonly byte[] _uniqueCardIdentifier = new byte[] {
            0xA0, 0x00, 0x00, 0x01, 0x16, 0xFF, 0x02,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        /// <summary>
        /// The "AID" (Capabilities Application Identifier), which consists of
        /// the GSC-RID || ManufacturerID || CardType.
        /// </summary>
        public ReadOnlyMemory<byte> ApplicationIdentifier { get; private set; }

        /// <summary>
        /// The "Government Smart Card - Registered Application Provider
        /// Identifier".
        /// </summary>
        public ReadOnlyMemory<byte> GscRid { get; private set; }

        /// <summary>
        /// The actual Card Identifier portion of the Unique Card Identifier.
        /// </summary>
        public ReadOnlyMemory<byte> CardIdentifier { get; private set; }

        /// <summary>
        /// The manufacturer ID is fixed at 0xFF
        /// </summary>
        public int ManufacturerId => FixedManufacturerId;

        /// <summary>
        /// The card type is fixed at JavaCard.
        /// </summary>
        public int CardType => CardTypeJavaCard;

        /// <summary>
        /// The version number of the CCC itself, it is fixed at version 2.1.
        /// </summary>
        public byte ContainerVersionNumber => FixedContainerVersionNumber;

        /// <summary>
        /// The version number of the CCC grammar, it is fixed at version 2.1.
        /// </summary>
        public byte GrammarVersionNumber => FixedGrammarVersionNumber;

        /// <summary>
        /// The version of PKCS #15 the card supports. If the card does not
        /// support PKCS #15, this number is 0x00. For the YubiKey it is fixed at
        /// 0x00.
        /// </summary>
        public byte Pkcs15Version => FixedPkcs15VersionNumber;

        /// <summary>
        /// The number representing the Data Model used by the smart card. For
        /// the YubiKey it is fixed at 0x10 (a PIV requirement).
        /// </summary>
        public byte DataModelNumber => FixedDataModelNumber;

        /// <summary>
        /// Build a new object. This will not get the CCC from from any YubiKey,
        /// it will only build an "empty" object.
        /// </summary>
        /// <remarks>
        /// To read the CCC data out of a YubiKey, call a
        /// <see cref="PivSession.ReadObject{PivObject}(int)"/> method.
        /// </remarks>
        public CardCapabilityContainer()
        {
            _log.LogInformation("Create a new instance of CardCapabilityContainer.");
            _disposed = false;
            DataTag = CccDefinedDataTag;

            IsEmpty = true;
            UniqueCardIdentifier = new ReadOnlyMemory<byte>(_uniqueCardIdentifier);
            ApplicationIdentifier = UniqueCardIdentifier.Slice(AidOffset, AidLength);
            GscRid = UniqueCardIdentifier.Slice(GscRidOffset, GscRidLength);
            CardIdentifier = UniqueCardIdentifier.Slice(CardIdOffset, CardIdLength);
        }

        /// <inheritdoc />
        public override int GetDefinedDataTag() => CccDefinedDataTag;

        /// <summary>
        /// Set the CardId with a random, 14-byte value.
        /// </summary>
        /// <remarks>
        /// This method will use the random number generator built by
        /// <see cref="CryptographyProviders"/> to generate 14 random bytes as
        /// the new CardId.
        /// <para>
        /// If there is a CardId value already in this object, this method will
        /// overwrite it.
        /// </para>
        /// </remarks>
        public void SetRandomCardId()
        {
            _log.LogInformation("Set the CardId of CardCapabilityContainer with a random value.");
            Clear();

            using (var randomObject = CryptographyProviders.RngCreator())
            {
                randomObject.GetBytes(_uniqueCardIdentifier, CardIdOffset, CardIdLength);
            }

            IsEmpty = false;
        }

        /// <summary>
        /// Set the <c>CardIdentifier</c> with the given value. If the array is
        /// not exactly 14 bytes, this method will throw an exception.
        /// </summary>
        /// <remarks>
        /// If there is a CardId value already in this object, this method will
        /// overwrite it.
        /// </remarks>
        /// <param name="cardIdValue">
        /// The CardId to use.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The data is not exactly 14 bytes.
        /// </exception>
        public void SetCardId(ReadOnlySpan<byte> cardIdValue)
        {
            _log.LogInformation("Set the CardId of CardCapabilityContainer with a caller-supplied value.");
            if (cardIdValue.Length != CardIdLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPivDataObjectLength));
            }

            Clear();

            var dest = new Span<byte>(_uniqueCardIdentifier);
            cardIdValue.CopyTo(dest.Slice(CardIdOffset, CardIdLength));
            IsEmpty = false;
            return;
        }

        /// <inheritdoc />
        public override byte[] Encode()
        {
            _log.LogInformation("Encode CardCapabilityContainer.");
            if (IsEmpty)
            {
                return new byte[] { 0x53, 0x00 };
            }

            // We're encoding
            //   53 33
            //      F0 15
            //         A0 00 00 01 16 FF 02
            //         --14 random bytes--
            //      F1 01
            //         21
            //      F2 01
            //         21
            //      F3 00
            //      F4 01
            //         00
            //      F5 01
            //         10
            //      F6 00
            //      F7 00
            //      FA 00
            //      FB 00
            //      FC 00
            //      FD 00
            //      FE 00
            var tlvWriter = new TlvWriter();
            using (tlvWriter.WriteNestedTlv(EncodingTag))
            {
                tlvWriter.WriteValue(UniqueCardIdTag, UniqueCardIdentifier.Span);
                WriteFixedValues(tlvWriter);
            }

            byte[] returnValue = tlvWriter.Encode();
            tlvWriter.Clear();
            return returnValue;
        }

        /// <inheritdoc />
        public override bool TryDecode(ReadOnlyMemory<byte> encodedData)
        {
            _log.LogInformation("Decode data into CardCapabilityContainer.");
            if (encodedData.Length == 0)
            {
                Clear();
                return true;
            }


            // We're looking for a CCC that is encoded as
            //   53 33
            //      F0 15
            //         A0 00 00 01 16 FF 02
            //         --14 random bytes--
            //      F1 01
            //         21
            //      F2 01
            //         21
            //      F3 00
            //      F4 01
            //         00
            //      F5 01
            //         10
            //      F6 00
            //      F7 00
            //      FA 00
            //      FB 00
            //      FC 00
            //      FD 00
            //      FE 00
            var tlvReader = new TlvReader(encodedData);
            bool isValid = tlvReader.TryReadNestedTlv(out tlvReader, EncodingTag);
            isValid = TryReadUniqueId(isValid, tlvReader);
            isValid = TryReadFixedValues(isValid, tlvReader);

            // If isValid is true, then we successfully decoded, so the object is
            // not empty (IsEmpty should be set to false). If isValid is false,
            // then the object is empty (IsEmpty should be set to true).
            IsEmpty = !isValid;

            return isValid;
        }

        // We're expecting F0 15 uniqueID with the first 7 bytes fixed.
        // Try to decode and verify the data is as expected.
        // If everything is correct, return true, otherwise, return false.
        // if the input isValid is false, don't bother doing anything, just
        // return false.
        private bool TryReadUniqueId(bool isValid, TlvReader tlvReader)
        {
            if (isValid)
            {
                _log.LogInformation("Decode data into CardCapabilityContainer: UniqueId.");
                if (tlvReader.TryReadValue(out var encodedUniqueId, UniqueCardIdTag))
                {
                    if (encodedUniqueId.Length == UniqueCardIdLength &&
                        MemoryExtensions.SequenceEqual<byte>(encodedUniqueId.Slice(AidOffset, AidLength).Span, ApplicationIdentifier.Span))
                    {
                        var dest = new Memory<byte>(_uniqueCardIdentifier);
                        encodedUniqueId.CopyTo(dest);
                        return true;
                    }
                }
            }

            return false;
        }

        // We're expecting F1 throug FE (skipping F8 and F9).
        // Each of these is either Fx 01 byte or Fx 00.
        // Try to decode and verify the data is as expected.
        // If everything is correct, return true, otherwise, return false.
        // if the input isValid is false, don't bother doing anything, just
        // return false.
        private bool TryReadFixedValues(bool isValid, TlvReader tlvReader)
        {
            if (!isValid)
            {
                return false;
            }

            _log.LogInformation("Decode data into CardCapabilityContainer: FixedValues.");
            bool readSuccessful = isValid;

            var elementList = GetFixedTupleArray();

            int index = 0;
            while (readSuccessful && index < elementList.Length)
            {
                if (elementList[index].Item2 == 0)
                {
                    readSuccessful = tlvReader.TryReadValue(out var currentValue, elementList[index].Item1) &&
                              currentValue.Length == elementList[index].Item2;
                }
                else
                {
                    readSuccessful = tlvReader.TryReadByte(out byte currentValue, elementList[index].Item1) &&
                        currentValue == elementList[index].Item3;
                }

                index++;
            }

            return readSuccessful;
        }

        private void WriteFixedValues(TlvWriter tlvWriter)
        {
            var elementList = GetFixedTupleArray();
            var emptySpan = ReadOnlySpan<byte>.Empty;

            int index = 0;
            do
            {
                if (elementList[index].Item2 == 0)
                {
                    tlvWriter.WriteValue(elementList[index].Item1, emptySpan);
                }
                else
                {
                    tlvWriter.WriteByte(elementList[index].Item1, elementList[index].Item3);
                }

                index++;
            } while (index < elementList.Length);
        }

        // This array of tuples represents what we'll be encoding or decoding.
        // Item 1 is the tag.
        // Item 2 is the length, it must be either 0 or 1.
        // Item 3 is the value, if the length is 0, this is ignored.
        private Tuple<int, int, byte>[] GetFixedTupleArray() =>
            new Tuple<int, int, byte>[] {
                new Tuple<int, int, byte>(ContainerVersionTag, 1, ContainerVersionNumber),
                new Tuple<int, int, byte>(GrammarVersionTag, 1, GrammarVersionNumber),
                new Tuple<int, int, byte>(UnusedTag1, 0, 0),
                new Tuple<int, int, byte>(Pkcs15Tag, 1, Pkcs15Version),
                new Tuple<int, int, byte>(DataModelTag, 1, DataModelNumber),
                new Tuple<int, int, byte>(UnusedTag2, 0, 0),
                new Tuple<int, int, byte>(UnusedTag3, 0, 0),
                new Tuple<int, int, byte>(UnusedTag4, 0, 0),
                new Tuple<int, int, byte>(UnusedTag5, 0, 0),
                new Tuple<int, int, byte>(UnusedTag6, 0, 0),
                new Tuple<int, int, byte>(UnusedTag7, 0, 0),
                new Tuple<int, int, byte>(UnusedTag8, 0, 0)
            };

        private void Clear()
        {
            var dataAsSpan = new Span<byte>(_uniqueCardIdentifier);
            CryptographicOperations.ZeroMemory(dataAsSpan.Slice(CardIdOffset, CardIdLength));
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
