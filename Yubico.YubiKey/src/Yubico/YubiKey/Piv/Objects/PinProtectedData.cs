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
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Yubico.Core.Logging;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Piv.Objects
{
    /// <summary>
    /// Use this class to process a specific set of PIN-protected data stored in
    /// the PRINTED data object.
    /// </summary>
    /// <remarks>
    /// Some Data Objects are retrievable only in a session where the PIN has
    /// been verified. Hence, that data is PIN-protected. This class will be able
    /// to process data stored in one such Data Object. The data this class can
    /// process is specified by its properties.
    /// <para>
    /// See the User's Manual entry on
    /// <xref href="UsersManualPivObjects#pinprotecteddata"> PIV data objects</xref>
    /// for a description of the details of how this class works.
    /// </para>
    /// <para>
    /// This class specifies the <c>DefinedDataTag</c> to be <c>0x005FC109</c>
    /// which is the data tag for the PRINTED storage area. The reason is that
    /// the PRINTED area requires the PIN to read. The data is stored in this
    /// object and when it is needed, simply retrieve it and use it. In order to
    /// retrieve, though, PIN verification is required, so in this way the data
    /// is PIN-protected.
    /// </para>
    /// <para>
    /// This class does not allow changing the <c>DataTag</c>. That is, it is
    /// possible to store the data in this set only in the PRINTED area.
    /// </para>
    /// <para>
    /// This class is different from other <c>PivDataObjects</c>. Most such
    /// classes store information encoded as the PIV standard defines it.
    /// However, this class stores the elements specified by
    /// <c>PinProtectedDataType</c> following a definition that is not the PIV
    /// standard for PRINTED.
    /// <para>
    /// Note that this object can accept or decode only elements for which there
    /// is a property.
    /// </para>
    /// </para>
    /// </remarks>
    public sealed class PinProtectedData : PivDataObject
    {
        private const int PinProtectedDefinedDataTag = 0x005FC109;
        private const int MaxKeyLength = 32;
        private const int EncodingTag = 0x53;
        private const int PinProtectedTag = 0x88;
        private const int MgmtKeyTag = 0x89;

        private bool _disposed;
        private readonly ILogger _log = Log.GetLogger<PinProtectedData>();

        /// <summary>
        /// The management key that will be PIN-protected. If there is no
        /// management key, this will be null.
        /// </summary>
        public ReadOnlyMemory<byte>? ManagementKey { get; private set; }

        private readonly byte[] _keyBuffer = new byte[MaxKeyLength];
        private readonly Memory<byte> _mgmtKey;

        private readonly int[] _validKeyLengths = new int[] { 16, 24, 32 };

        /// <summary>
        /// Build a new object. This will not get the PIN-protected data from the
        /// YubiKey, it will only build an "empty" object.
        /// </summary>
        public PinProtectedData()
        {
            _log.LogInformation("Create a new instance of PinProtectedData.");
            _disposed = false;
            DataTag = PinProtectedDefinedDataTag;
            _mgmtKey = new Memory<byte>(_keyBuffer);
            ManagementKey = null;
            IsEmpty = true;
        }

        /// <inheritdoc />
        public override int GetDefinedDataTag() => PinProtectedDefinedDataTag;

        /// <summary>
        /// Override the base class. This class does not allow alternate DataTags.
        /// The only allowed tag is the defined.
        /// </summary>
        /// <param name="dataTag">
        /// The data tag the caller wants to use as an alternate.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> is the given tag can be used as an alternate,
        /// <c>false</c> otherwise.
        /// </returns>
        protected override bool IsValidAlternateTag(int dataTag) => dataTag == PinProtectedDefinedDataTag;

        /// <summary>
        /// Set the <c>ManagementKey</c> property with the specified value.
        /// </summary>
        /// <remarks>
        /// The caller supplies an argument of Length zero, 16, 24, or 32. Any
        /// other input will cause an exception.
        /// <para>
        /// An empty array (Length = zero) means there is no management key
        /// stored in the PRINTED object on the given YubiKey. A caller can set
        /// the management key to empty in order to "convert" a YubiKey from
        /// PIN-protected to normal (the application/user must supply the
        /// management key for authentication).
        /// </para>
        /// <para>
        /// If there is a management key already in this object, this method will
        /// overwrite it.
        /// </para>
        /// <para>
        /// This method will copy the data, it will not copy a reference.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// The data Length is not 0, 16, 24, or 32 bytes.
        /// </exception>
        public void SetManagementKey(ReadOnlyMemory<byte> managementKey)
        {
            _log.LogInformation("Set ManagementKey in PinProtectedManagementKey.");
            IsEmpty = false;
            if (IsValidKeyLength(managementKey.Length))
            {
                managementKey.CopyTo(_mgmtKey);
                ManagementKey = _mgmtKey.Slice(0, managementKey.Length);
                return;
            }

            ManagementKey = null;
            if (managementKey.Length != 0)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPivDataObjectLength));
            }
        }

        private bool IsValidKeyLength(int keyLength)
        {
            for (int index = 0; index < _validKeyLengths.Length; index++)
            {
                if (keyLength == _validKeyLengths[index])
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public override byte[] Encode()
        {
            _log.LogInformation("Encode AdminData.");
            if (IsEmpty)
            {
                return new byte[] { 0x53, 0x00 };
            }

            // If there is a management key, we're encoding
            //   53 1C
            //      88 1A              (or 12 or 22)
            //         89 18           (or 10 or 20)
            //            <24 bytes>   (or 16 or 32 bytes)
            // If there is no management key, we're encoding
            //   53 02
            //      88 00
            var tlvWriter = new TlvWriter();
            using (tlvWriter.WriteNestedTlv(EncodingTag))
            {
                using (tlvWriter.WriteNestedTlv(PinProtectedTag))
                {
                    if (ManagementKey.HasValue)
                    {
                        tlvWriter.WriteValue(MgmtKeyTag, ManagementKey.Value.Span);
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
            _log.LogInformation("Decode into PinProtectedManagementKey.");

            Clear();
            if (encodedData.Length == 0)
            {
                return true;
            }

            // We're looking for PIN-protected data that is encoded as
            //   53 02 88 00
            // or
            //   53 1C
            //      88 1A                       (or 12 or 22)
            //         89 18                    (or 10 or 20)
            //            --management key--
            var managementKey = ReadOnlyMemory<byte>.Empty;
            var tlvReader = new TlvReader(encodedData);

            bool isValid = true;
            int count = 0;
            while (isValid && tlvReader.HasData)
            {
                isValid = count switch
                {
                    0 => tlvReader.TryReadNestedTlv(out tlvReader, EncodingTag),
                    1 => tlvReader.TryReadNestedTlv(out tlvReader, PinProtectedTag),
                    2 => tlvReader.TryReadValue(out managementKey, MgmtKeyTag),
                    _ => false,
                };

                count++;
            }

            if (IsValidKeyLength(managementKey.Length))
            {
                managementKey.CopyTo(_mgmtKey);
                ManagementKey = _mgmtKey.Slice(0, managementKey.Length);
            }
            else
            {
                if (managementKey.Length != 0)
                {
                    isValid = false;
                }
            }

            if (isValid)
            {
                IsEmpty = false;
            }

            return isValid;
        }

        private void Clear()
        {
            CryptographicOperations.ZeroMemory(_mgmtKey.Span);
            ManagementKey = null;
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
