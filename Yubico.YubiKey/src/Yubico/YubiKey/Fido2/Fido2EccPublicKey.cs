// Copyright 2022 Yubico AB
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
using System.Formats.Cbor;
using System.Globalization;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// This class holds an ECC public key.
    /// </summary>
    /// <remarks>
    /// An ECC public key consists of a curve and public point. In FIDO2, the
    /// curve can be represented by the
    /// <see cref="Yubico.YubiKey.Fido2.Cose.CoseAlgorithmIdentifier"/> and the
    /// public point is simply an x-coordinate and a y-coordinate.
    /// <para>
    /// The FIDO2 standard also specifies an encoding of the public key
    /// information. This is called a "COSE_Key", encoded using a CBOR following
    /// a definition found in section 6.5.6 of CTAP 2.1 (under the heading
    /// <c>getPublicKey()</c>).
    /// </para>
    /// <para>
    /// This class has two constructors, one that builds an object from the
    /// <c>CoseAlgorithmIdentifier</c> and public point, and one that builds an
    /// object from the encoded key data. Once you have the object built, you can
    /// get either the encoded public key or each of the components from the
    /// approrpiate properties.
    /// </para>
    /// <para>
    /// The YubiKey's FIDO2 application currently supports only the NIST curve
    /// P-256. Hence, the <see cref="Algorithm"/> will be
    /// <c>CoseAlgorithmIdentifier.ES256</c>, and each of the public point
    /// coordinates will be 32 bytes long.
    /// </para>
    /// </remarks>
    public sealed class Fido2EccPublicKey
    {
        private const int DataMapCount = 5;
        private const int KeyKeyType = 1;
        private const int KeyAlgorithm = 3;
        private const int KeyCurve = -1;
        private const int KeyXCoordinate = -2;
        private const int KeyYCoordinate = -3;

        private const int ExpectedKeyType = 2;
        private const int ExpectedAlgorithm = -25;
        private const int ExpectedCurve = 1;

        // We currently support only one coordinate size.
        private const int P256CoordinateLength = 32;

        private Memory<byte> _xCoordinate;
        private Memory<byte> _yCoordinate;

        /// <summary>
        /// The key's algorithm.
        /// </summary>
        public CoseAlgorithmIdentifier Algorithm { get; private set;  }

        /// <summary>
        /// Contains the x-coordinate of the public point.
        /// </summary>
        public ReadOnlySpan<byte> XCoordinate => _xCoordinate.Span;

        /// <summary>
        /// Contains the y-coordinate of the public point.
        /// </summary>
        public ReadOnlySpan<byte> YCoordinate => _yCoordinate.Span;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private Fido2EccPublicKey()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a new instance of an ECC public key object based on the
        /// given algorithm and public point.
        /// </summary>
        /// <remarks>
        /// The point must be provided as two coordinates, each coordinate of a
        /// length specific to the algorithm. Currently only <c>ES256</c> (NIST
        /// P-256) is supported. Each coordinate for that algorithm is 32 bytes.
        /// If the actual coordinate is shorter than 32 bytes, the caller must
        /// supply it with enough prepended <c>00</c> bytes so that the buffer is
        /// exactly 32 bytes.
        /// <para>
        /// Note that this constructor will copy the coordinates' data (not a
        /// reference), creating its own local copy.
        /// </para>
        /// </remarks>
        /// <param name="algorithm">
        /// The algorithm of the key, namely the curve to use.
        /// </param>
        /// <param name="xCoordinate">
        /// The public point's x-coordinate.
        /// </param>
        /// <param name="yCoordinate">
        /// The public point's y-coordinate.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The format of the public point is not supported.
        /// </exception>
        public Fido2EccPublicKey(CoseAlgorithmIdentifier algorithm, ReadOnlySpan<byte> xCoordinate, ReadOnlySpan<byte> yCoordinate)
        {
            if ((algorithm != CoseAlgorithmIdentifier.ES256)
                || (xCoordinate.Length != P256CoordinateLength)
                || (yCoordinate.Length != P256CoordinateLength))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPublicKeyData));
            }

            Algorithm = algorithm;
            _xCoordinate = new Memory<byte>(xCoordinate.ToArray());
            _yCoordinate = new Memory<byte>(yCoordinate.ToArray());
        }

        /// <summary>
        /// Create a new instance of an ECC public key object based on the
        /// given encoded data.
        /// </summary>
        /// <remarks>
        /// This constructor expects the data to be encoded as follows.
        /// <code>
        ///       map of 5 (a5)
        ///          key of 01
        ///           integer 02 (key type, EC2)
        ///          key of 03
        ///           integer 38 18 (-25, algorithm, ignored)
        ///          key of 20 (-1)
        ///           integer 01 (curve, P-256)
        ///          key of 21 (-2)
        ///           byte array (x-coordinate, 58 20 where 20 is the length = decimal 32)
        ///          key of 22 (-3)
        ///           byte array (y-coordinate, 58 20 where 20 is the length = decimal 32)
        /// </code>
        /// <para>
        /// This constructor will accept an encoding that begins with a map of 1
        /// (first byte = a1) or a map of 5 (first byte = a5)
        /// </para>
        /// <para>
        /// Upon construction, the <see cref="Algorithm"/>,
        /// <see cref="XCoordinate"/>, and <see cref="YCoordinate"/> properties
        /// will be set.
        /// </para>
        /// </remarks>
        /// <param name="cborEncoding">
        /// The encoded public key.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The <c>cborEncoding</c> is not a valid CBOR encoding, or it is not a
        /// correct encoding for a FIDO2 ECC public key.
        /// </exception>
        public Fido2EccPublicKey(ReadOnlyMemory<byte> cborEncoding)
        {
            if (!TryDecodeKey(cborEncoding))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPublicKeyData));
            }
        }

        // Perform the actual decode. If some key or value is incorrect, return
        // false.
        private bool TryDecodeKey(ReadOnlyMemory<byte> cborEncoding)
        {
            try
            {
                var cbor = new CborReader(cborEncoding, CborConformanceMode.Ctap2Canonical);

                int? entries = cbor.ReadStartMap();
                int count = entries ?? 0;

                bool isValid = TryReadInt(count == DataMapCount, cbor, KeyKeyType, ExpectedKeyType);
                isValid = TryReadInt(isValid, cbor, KeyAlgorithm, ExpectedAlgorithm);
                isValid = TryReadInt(isValid, cbor, KeyCurve, ExpectedCurve);
                isValid = TryReadByteArray(isValid, cbor, KeyXCoordinate, P256CoordinateLength, out _xCoordinate);
                isValid = TryReadByteArray(isValid, cbor, KeyYCoordinate, P256CoordinateLength, out _yCoordinate);

                if (isValid)
                {
                    cbor.ReadEndMap();
                    Algorithm = CoseAlgorithmIdentifier.ES256;
                }

                return isValid;
            }
            catch (CborContentException cborException)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidDataEncoding + " " + cborException.Message));
            }
        }

        // Try to read the next key/value pair. Make sure the key is the given
        // mapKey, read the value as a signed int (the major type is either 0 or
        // 1, unsigned or negative) and verify it is the expectedValue.
        // If the incoming isValid is false, don't bother doing anything, just
        // return false.
        // Otherwise, if everything checks out, return true.
        private static bool TryReadInt(bool isValid, CborReader cbor, int mapKey, int expectedValue)
        {
            if (isValid)
            {
                if (cbor.ReadInt32() == mapKey)
                {
                    if (cbor.ReadInt32() == expectedValue)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Try to read the next key/value pair. Make sure the key is the given
        // mapKey, read the value as a Byte String (major type 2).
        // Verify the length is expected, then build a new Memory<byte> with the
        // data from the encoding and set the out arg destination to this data.
        // If the incoming isValid is false, don't bother doing anything, just
        // return false.
        // Otherwise, if everything works out, return true.
        private static bool TryReadByteArray(
            bool isValid,
            CborReader cbor,
            int mapKey,
            int expectedLength,
            out Memory<byte> destination)
        {
            destination = Memory<byte>.Empty;

            if (isValid)
            {
                if (cbor.ReadInt32() == mapKey)
                {
                    byte[] byteStringValue = cbor.ReadByteString();
                    if (byteStringValue.Length == expectedLength)
                    {
                        destination = new Memory<byte>(byteStringValue);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Return a new byte array that is the key data encoded following the
        /// FIDO2/CBOR standard.
        /// </summary>
        /// <returns>
        /// The encoded key.
        /// </returns>
        public ReadOnlyMemory<byte> GetEncodedKey()
        {
            // This encodes the map of 5 things.
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(DataMapCount);
            cbor.WriteInt32(KeyKeyType);
            cbor.WriteInt32(ExpectedKeyType);
            cbor.WriteInt32(KeyAlgorithm);
            cbor.WriteInt32(ExpectedAlgorithm);
            cbor.WriteInt32(KeyCurve);
            cbor.WriteInt32(ExpectedCurve);
            cbor.WriteInt32(KeyXCoordinate);
            cbor.WriteByteString(_xCoordinate.Span);
            cbor.WriteInt32(KeyYCoordinate);
            cbor.WriteByteString(_yCoordinate.Span);
            cbor.WriteEndMap();

            byte[] encoding = cbor.Encode();
            return new ReadOnlyMemory<byte>(encoding);
        }
    }
}
