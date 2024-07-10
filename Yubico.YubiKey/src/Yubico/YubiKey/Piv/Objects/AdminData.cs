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
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using Yubico.Core.Logging;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Piv.Objects
{
    /// <summary>
    ///     Use this class to process the Admin Data.
    /// </summary>
    /// <remarks>
    ///     Admin consists of three values:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Bit field: PUK blocked? Mgmt Key stored in protected
    ///                 area? (optional)
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>Salt (optional)</description>
    ///         </item>
    ///         <item>
    ///             <description>PIN last updated (optional)</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         The <c>AdminData</c> is used to store information about "PIN-only" modes
    ///         of a YubiKey. See the User's Manual entry on setting the YubiKey to be
    ///         <xref href="UsersManualPinPukMgmtKey#pin-only"> PIN only</xref>.
    ///     </para>
    ///     <para>
    ///         If the YubiKey is PIN-derived, the PUK should be blocked, and there will
    ///         be a salt. Hence, the <c>PukBlocked</c> property should be <c>true</c>
    ///         and the <c>Salt</c> should contain the salt used to derive the management
    ///         key.
    ///     </para>
    ///     <para>
    ///         If the YubiKey is PIN-protected, the PUK should be blocked, so both the
    ///         <c>PinProtected</c> and <c>PukBlocked</c> properties should be
    ///         <c>true</c>.
    ///     </para>
    ///     <para>
    ///         Note that the YubiKey will not "automatically" set the Admin Data to the
    ///         appropriate values if the management key is set to one of the PIN-only
    ///         modes. That is the responsibility of the code that sets the PIN-only
    ///         mode. In other words, if you write code that sets a YubiKey to one of the
    ///         PIN-only modes, then you must also write code to correctly set the Admin
    ///         Data. The <c>PivSession</c> methods that set a YubiKey to PIN-only will
    ///         store the appropriate Admin Data information, so you should call those
    ///         methods to set a YubiKey to PIN-only, rather than writing the code
    ///         yourself.
    ///     </para>
    ///     <para>
    ///         The salt is used by the code that computes a PIN-derived management key.
    ///         The management key is derived from the PIN and salt. It must be exactly
    ///         16 bytes. This class will accept either no salt (mgmt key is not
    ///         PIN-derived) or a 16-byte salt. If you want to use the Admin Data storage
    ///         area to store something other than a 16-byte salt, you will have to write
    ///         your own implementation.
    ///     </para>
    ///     <para>
    ///         The PIN last updated element is the date the PIN was changed. It is not
    ///         mandatory to set this value when the PIN is changed, but the SDK code
    ///         that changes the PIN will check the ADMIN DATA. If the YubiKey contains
    ///         ADMIN DATA, the SDK will update the time when the PIN is changed. If
    ///         there is no ADMIN DATA, the SDK will not create ADMIN DATA when the PIN
    ///         is changed.
    ///     </para>
    ///     <para>
    ///         Upon instantiation of this class, it is empty. If you set any of the
    ///         properties (<see cref="PukBlocked" />, (<see cref="PinProtected" />,
    ///         <see cref="Salt" />, and <see cref="PinLastUpdated" />), the object will no
    ///         longer be empty. That is the case even if you set the <c>PukBlocked</c>
    ///         and/or the <c>PinProtected</c> to <c>false</c>, or the other two to null.
    ///         In this case, the encoding of the Admin Data is
    ///         <code>
    ///    80 03
    ///       81 01
    ///          00
    ///    The salt and PinLastUpdated are optional, so
    ///    they are not encoded when absent. The bit field
    ///    is also optional, so it could be absent, but
    ///    this class exercises the option and writes it.
    /// </code>
    ///     </para>
    ///     <para>
    ///         If an object is not empty, you can call the
    ///         <see cref="PivSession.WriteObject" /> method, which will call the
    ///         <see cref="PivDataObject.Encode" /> method. This class will encode
    ///         whatever data it is given, even if it is "wrong". For example, if a
    ///         management key is PIN-derived, then the PUK blocked bit and the
    ///         <c>Salt</c> should be set. However, if, for example, the PUK
    ///         blocked bit is set, but not the <c>Salt</c>, this class will encode
    ///         anyway. It will generate an encoding, not throw an exception. It is the
    ///         responsibility of the caller to make sure the data in an object is
    ///         correct for the situation.
    ///     </para>
    /// </remarks>
    public sealed class AdminData : PivDataObject
    {
        private const int AdminDataDefinedDataTag = 0x005FFF00;
        private const byte PukBlockedBit = 1;
        private const byte PinProtectedBit = 2;
        private const int SaltLength = 16;
        private const int EncodingTag = 0x53;
        private const int AdminDataTag = 0x80;
        private const int BitFieldTag = 0x81;
        private const int SaltTag = 0x82;
        private const int DateTag = 0x83;
        private const byte BitFieldRead = 1;
        private const byte SaltRead = 2;
        private const byte DateRead = 4;
        private readonly Logger _log = Log.GetLogger();
        private readonly Memory<byte> _salt;

        private readonly byte[] _saltBuffer = new byte[SaltLength];

        // Set the PukBlockedBit if PukBlocked is true.
        // Set the PinProtectedBit if PinProtected is true.
        private byte _adminDataBitField;

        private bool _disposed;

        private DateTime? _pinLastUpdated;

        /// <summary>
        ///     Build a new object. This will not get the Admin Data from any
        ///     YubiKey, it will only build an "empty" object.
        /// </summary>
        /// <remarks>
        ///     To read the Admin Data out of a YubiKey, call the
        ///     <see cref="PivSession.ReadObject{PivObject}()" /> method.
        /// </remarks>
        public AdminData()
        {
            _log.LogInformation("Create a new instance of AdminData.");
            _disposed = false;
            DataTag = AdminDataDefinedDataTag;
            PukBlocked = false;
            PinProtected = false;
            _salt = new Memory<byte>(_saltBuffer);
            IsEmpty = true;
        }

        /// <summary>
        ///     Set this to <c>true</c> if the PUK is blocked. If you set a YubiKey
        ///     to PIN-only, then the PUK should be blocked.
        /// </summary>
        public bool PukBlocked
        {
            get => (_adminDataBitField & PukBlockedBit) != 0;
            set
            {
                // If true, make sure the bit is set.
                // If false, make sure the bit is clear.
                _adminDataBitField |= PukBlockedBit;
                if (!value)
                {
                    _adminDataBitField ^= PukBlockedBit;
                }

                IsEmpty = false;
            }
        }

        /// <summary>
        ///     Set this to <c>true</c> if the YubiKey's management key is
        ///     PIN-protected. If you set a YubiKey to PIN-protected, then the PUK
        ///     should be blocked as well.
        /// </summary>
        public bool PinProtected
        {
            get => (_adminDataBitField & PinProtectedBit) != 0;
            set
            {
                // If true, make sure the bit is set.
                // If false, make sure the bit is clear.
                _adminDataBitField |= PinProtectedBit;
                if (!value)
                {
                    _adminDataBitField ^= PinProtectedBit;
                }

                IsEmpty = false;
            }
        }

        /// <summary>
        ///     The salt used to derive the management key. If there is no salt, this
        ///     will be null.
        /// </summary>
        public ReadOnlyMemory<byte>? Salt { get; private set; }

        /// <summary>
        ///     The date the PIN was last updated. If this is not being used, it will
        ///     be null.
        /// </summary>
        /// <remarks>
        ///     To set this property to the current time, use <c>DateTime.UtcNow</c>.
        ///     <code language="csharp">
        ///    var adminData = new AdminData();
        ///    adminData.PinLastUpdated = DateTime.UtcNow;
        /// </code>
        ///     It is possible to set the time to any time at all (Jan. 1, 2000, if
        ///     you want), but it is likely that you will never need to set it to
        ///     anything other than the current time.
        ///     <para>
        ///         If you get the ADMIN DATA out of a YubiKey, it will be encoded. This
        ///         class will decode it and set this property to the time specified in
        ///         the encoding. It is possible the date is not encoded, in which case
        ///         this will be null.
        ///     </para>
        ///     <para>
        ///         Upon construction, the PinLastUpdated is null. If you leave it null
        ///         or set it to null, then when the data is encoded, no
        ///         <c>PinLastUpdated</c> value will be included in the encoding.
        ///     </para>
        /// </remarks>
        public DateTime? PinLastUpdated
        {
            get => _pinLastUpdated;
            set
            {
                _pinLastUpdated = value;
                IsEmpty = false;
            }
        }

        /// <inheritdoc />
        public override int GetDefinedDataTag() => AdminDataDefinedDataTag;

        /// <summary>
        ///     Set the <c>Salt</c> property with the given value. If the input
        ///     argument <c>Length</c> is 0, this will set the <c>Salt</c> to be
        ///     null. Otherwise, it must be exactly 16 bytes. If not, this method
        ///     will throw an exception.
        /// </summary>
        /// <remarks>
        ///     This method will copy the input salt data, it will not copy a
        ///     reference.
        ///     <para>
        ///         If there is a salt value already in this object, this method will
        ///         overwrite it.
        ///     </para>
        ///     <para>
        ///         If the input <c>salt</c> argument is null or the <c>Length</c> is 0,
        ///         this method will set the <c>Salt</c> property to null. The object
        ///         will not be empty (<c>IsEmpty</c>) will be <c>false</c>), but there
        ///         will be no salt.
        ///     </para>
        /// </remarks>
        /// <param name="salt">
        ///     The salt to use.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     The data, if there is any, is not exactly 16 bytes.
        /// </exception>
        public void SetSalt(ReadOnlyMemory<byte> salt)
        {
            _log.LogInformation("Set the Salt of AdminData with a caller-supplied value.");

            IsEmpty = false;

            if (salt.Length == SaltLength)
            {
                salt.CopyTo(_salt);
                Salt = _salt;
                return;
            }

            Salt = null;

            if (salt.Length != 0)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPivDataObjectLength));
            }
        }

        /// <inheritdoc />
        public override byte[] Encode()
        {
            _log.LogInformation("Encode AdminData.");
            if (IsEmpty)
            {
                return new byte[] { 0x53, 0x00 };
            }

            // We're encoding
            //   53 len
            //      80 len
            //         81 01  (optional)
            //            --bit field--
            //         82 10  (optional)
            //            --salt--
            //         83 04  (optional)
            //            --PIN last updated--
            // If the bit field is unset, we're still going to encode it, even
            // though it is optional
            // If the Salt is null, we're going to leave it out of the encoding.
            // If PinLastUpdated is null, we're going to leave it out of the
            // encoding.
            var tlvWriter = new TlvWriter();
            using (tlvWriter.WriteNestedTlv(EncodingTag))
            {
                using (tlvWriter.WriteNestedTlv(AdminDataTag))
                {
                    tlvWriter.WriteByte(BitFieldTag, _adminDataBitField);
                    if (!(Salt is null))
                    {
                        tlvWriter.WriteValue(SaltTag, _salt.Span);
                    }

                    if (!(PinLastUpdated is null))
                    {
                        long unixTimeSeconds = new DateTimeOffset((DateTime)PinLastUpdated).ToUnixTimeSeconds();
                        byte[] timeValue = TimeAsLittleEndianArray(unixTimeSeconds);
                        tlvWriter.WriteValue(DateTag, timeValue);
                    }
                }
            }

            byte[] returnValue = tlvWriter.Encode();
            tlvWriter.Clear();
            return returnValue;
        }

        /// <inheritdoc />
        public override bool TryDecode(ReadOnlyMemory<byte> encodedData)
        {
            _log.LogInformation("Try to decode into AdminData.");

            Clear();
            if (encodedData.Length == 0)
            {
                return true;
            }

            // We're looking for Admin data that is encoded as
            //   53 len
            //      80 len
            //         81 01  (optional)
            //            --bit field--
            //         82 10  (optional)
            //            --salt--
            //         83 04  (optional)
            //            --PIN last updated--
            // The elementsRead is a bit field. If the BitField has been read,
            // set 1. If the Salt has been read set 2. If the Date has been read,
            // set 4. If something is set twice, isValid is false.
            byte elementsRead = 0;
            var tlvReader = new TlvReader(encodedData);
            bool isValid = tlvReader.TryReadNestedTlv(out tlvReader, EncodingTag);
            if (isValid)
            {
                isValid = tlvReader.TryReadNestedTlv(out tlvReader, AdminDataTag);
            }

            while (tlvReader.HasData)
            {
                int nextTag = tlvReader.PeekTag();
                isValid = nextTag switch
                {
                    BitFieldTag => ReadBitField(tlvReader, ref elementsRead),
                    SaltTag => ReadSalt(tlvReader, ref elementsRead),
                    DateTag => ReadDate(tlvReader, ref elementsRead),
                    _ => false
                };

                if (!isValid)
                {
                    Clear();
                    break;
                }
            }

            // If isValid is true, then we successfully decoded, so the object is
            // not empty (IsEmpty should be set to false). If isValid is false,
            // then the object is empty (IsEmpty should be set to true).
            IsEmpty = !isValid;

            return isValid;
        }

        // The tlvReader has determined that the next byte is the BitField tag,
        // so read the data and set the AdminDataState.
        // If this is not encoded properly, return false. If encoded properly but
        // the data is invalid, throw an exception.
        private bool ReadBitField(TlvReader tlvReader, ref byte elementsRead)
        {
            _log.LogInformation("Decode AdminData bit field.");

            // Do not read two BitField elements.
            // If we have read this before, the XOR will clear the bit.
            elementsRead ^= BitFieldRead;

            bool isValid = tlvReader.TryReadByte(out byte bitField, BitFieldTag);
            if (isValid)
            {
                PukBlocked = (bitField & PukBlockedBit) != 0;
                PinProtected = (bitField & PinProtectedBit) != 0;

                isValid = (elementsRead & BitFieldRead) != 0;
            }

            return isValid;
        }

        // The tlvReader has determined that the next byte is the Salt tag, so
        // read the data and set the Salt.
        // If this is not encoded properly, return false. If encoded properly but
        // the data is invalid, throw an exception.
        private bool ReadSalt(TlvReader tlvReader, ref byte elementsRead)
        {
            _log.LogInformation("Decode AdminData salt.");

            // Do not read two Salt elements.
            // If we have read this before, the XOR will clear the bit.
            elementsRead ^= SaltRead;

            bool isValid = tlvReader.TryReadValue(out ReadOnlyMemory<byte> salt, SaltTag);
            if (isValid)
            {
                if (salt.Length == 0)
                {
                    Salt = null;
                    return true;
                }

                if (salt.Length != SaltLength)
                {
                    return false;
                }

                salt.CopyTo(_salt);
                Salt = _salt;
                isValid = (elementsRead & SaltRead) != 0;
            }

            return isValid;
        }

        // The tlvReader has determined that the next byte is the Date tag, so
        // read the data and set PinLastUpdated.
        // If this is not encoded properly, return false. If encoded properly but
        // the data is invalid, throw an exception.
        private bool ReadDate(TlvReader tlvReader, ref byte elementsRead)
        {
            _log.LogInformation("Decode AdminData time.");

            // Do not read two Date elements.
            // If we have read this before, the XOR will clear the bit.
            elementsRead ^= DateRead;

            // Make sure the value is no more than 8 bytes.
            // Also, if the length is 0, there is no date, we'll want the
            // property to be null. It was set to null when we called Clear
            // before decoding.
            bool isValid = tlvReader.TryReadValue(out ReadOnlyMemory<byte> theTime, DateTag);
            isValid = isValid && theTime.Length <= 8;

            if (isValid && theTime.Length > 0)
            {
                var cpyObj = new Memory<byte>(new byte[8]);
                theTime.CopyTo(cpyObj);
                long unixTimeSeconds = BinaryPrimitives.ReadInt64LittleEndian(cpyObj.Span);
                PinLastUpdated = DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds).UtcDateTime;

                isValid = (elementsRead & DateRead) != 0;
            }

            return isValid;
        }

        private static byte[] TimeAsLittleEndianArray(long unixTime)
        {
            byte[] buffer = new byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, (ulong)unixTime);
            int index = Array.FindLastIndex(buffer, element => element != 0);

            Array.Resize(
                ref buffer, index < 0
                    ? 1
                    : index + 1);

            return buffer;
        }

        private void Clear()
        {
            CryptographicOperations.ZeroMemory(_salt.Span);
            PukBlocked = false;
            PinProtected = false;
            Salt = null;
            PinLastUpdated = null;
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
