// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Source: https://github.com/dotnet/runtime/tree/v5.0.0-preview.7.20364.11/src/libraries/System.Formats.Cbor/src/System/Formats/Cbor

using System;
using System.Globalization;
using System.Buffers.Binary;
using System.Linq;

namespace System.Formats.Cbor
{
    public partial class CborWriter
    {
        // Implements major type 7 encoding per https://tools.ietf.org/html/rfc7049#section-2.1

        /// <summary>
        ///   Writes a single-precision floating point number (major type 7).
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">
        ///   Writing a new value exceeds the definite length of the parent data item. -or-
        ///   The major type of the encoded value is not permitted in the parent data item. -or-
        ///   The written data is not accepted under the current conformance mode
        /// </exception>
        public void WriteSingle(float value)
        {
            EnsureWriteCapacity(5);
            WriteInitialByte(new CborInitialByte(CborMajorType.Simple, CborAdditionalInfo.Additional32BitData));
            byte[] valueBytes = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                valueBytes = valueBytes.Reverse().ToArray();
            }

            valueBytes.CopyTo(_buffer.AsSpan(BytesWritten));
            BytesWritten += 4;
            AdvanceDataItemCounters();
        }

        /// <summary>
        ///   Writes a double-precision floating point number (major type 7).
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">
        ///   Writing a new value exceeds the definite length of the parent data item. -or-
        ///   The major type of the encoded value is not permitted in the parent data item. -or-
        ///   The written data is not accepted under the current conformance mode
        /// </exception>
        public void WriteDouble(double value)
        {
            EnsureWriteCapacity(9);
            WriteInitialByte(new CborInitialByte(CborMajorType.Simple, CborAdditionalInfo.Additional64BitData));
            byte[] valueBytes = BitConverter.GetBytes(value);
            
            if (BitConverter.IsLittleEndian)
            {
                valueBytes = valueBytes.Reverse().ToArray();
            }

            valueBytes.CopyTo(_buffer.AsSpan(BytesWritten));
            BytesWritten += 8;
            AdvanceDataItemCounters();
        }

        /// <summary>
        ///   Writes a boolean value (major type 7).
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">
        ///   Writing a new value exceeds the definite length of the parent data item. -or-
        ///   The major type of the encoded value is not permitted in the parent data item. -or-
        ///   The written data is not accepted under the current conformance mode
        /// </exception>
        public void WriteBoolean(bool value) => WriteSimpleValue(value ? CborSimpleValue.True : CborSimpleValue.False);

        /// <summary>
        ///   Writes a null value (major type 7).
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///   Writing a new value exceeds the definite length of the parent data item. -or-
        ///   The major type of the encoded value is not permitted in the parent data item. -or-
        ///   The written data is not accepted under the current conformance mode
        /// </exception>
        public void WriteNull() => WriteSimpleValue(CborSimpleValue.Null);

        /// <summary>
        ///   Writes a simple value encoding (major type 7).
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="value"/> parameter is in the invalid 24-31 range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   Writing a new value exceeds the definite length of the parent data item. -or-
        ///   The major type of the encoded value is not permitted in the parent data item. -or-
        ///   The written data is not accepted under the current conformance mode
        /// </exception>
        public void WriteSimpleValue(CborSimpleValue value)
        {
            if (value < (CborSimpleValue)CborAdditionalInfo.Additional8BitData)
            {
                EnsureWriteCapacity(1);
                WriteInitialByte(new CborInitialByte(CborMajorType.Simple, (CborAdditionalInfo)value));
            }
            else if (value <= (CborSimpleValue)CborAdditionalInfo.IndefiniteLength &&
                     CborConformanceModeHelpers.RequireCanonicalSimpleValueEncodings(ConformanceMode))
            {
                throw new ArgumentOutOfRangeException(string.Format(CultureInfo.CurrentCulture, CborExceptionMessages.Cbor_ConformanceMode_InvalidSimpleValueEncoding, ConformanceMode));
            }
            else
            {
                EnsureWriteCapacity(2);
                WriteInitialByte(new CborInitialByte(CborMajorType.Simple, CborAdditionalInfo.Additional8BitData));
                _buffer[BytesWritten++] = (byte)value;
            }

            AdvanceDataItemCounters();
        }
    }
}
