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
using System.Globalization;
using System.Security.Cryptography;
using Yubico.YubiKey.Fido2.Cbor;

namespace Yubico.YubiKey.Fido2.Cose
{
    /// <summary>
    /// A representation of an Elliptic Curve public key in COSE form.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An ECC public key consists of a curve and public point. In FIDO2, the curve is represented by the
    /// <see cref="CoseAlgorithmIdentifier"/> and the public point is simply an x-coordinate and a y-coordinate.
    /// </para>
    /// <para>
    /// The FIDO2 standard also specifies an encoding of the public key information. It uses the representation defined
    /// in RFC8152: CBOR Object Signing and Encryption (COSE) standard. Supplementary information can be found in
    /// section 6.5.6 of the CTAP2.1 specification (under the heading `getPublicKey()`).
    /// </para>
    /// <para>
    /// This class has multiple constructors. One constructs an empty object and allows the caller to set the key
    /// parameters via the properties on this class. Another constructs a key based on the COSE form encoded in CBOR.
    /// Lastly, there is a constructor that takes in a .NET representation of an EC public key used for interoperating
    /// with the .NET cryptographic library.
    /// </para>
    /// <para>
    /// The YubiKey's FIDO2 application currently only supports the NIST P-256 curve. Thus, the SDK - as of version 1.5.0
    /// - will also only support this curve.
    /// </para>
    /// </remarks>
    public class CoseEcPublicKey : CoseKey
    {
        private const int TagCurve = -1;
        private const int TagX = -2;
        private const int TagY = -3;

        private const string P256Oid = "1.2.840.10045.3.1.7";

        // We currently support only one coordinate size
        private const int P256CoordinateLength = 32;

        private byte[] _xCoordinate = Array.Empty<byte>();
        private byte[] _yCoordinate = Array.Empty<byte>();
        private CoseEcCurve _curve;

        /// <summary>
        /// The Elliptic Curve that the key resides on.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// On set, the curve specified is not supported.
        /// </exception>
        public CoseEcCurve Curve
        {
            get => _curve;
            set
            {
                if (value != CoseEcCurve.P256)
                {
                    throw new NotSupportedException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.UnsupportedAlgorithm));
                }
                _curve = value;
            }
        }

        /// <summary>
        /// The X-coordinate of the public point.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// On set, the coordinate in not the correct length.
        /// </exception>
        public ReadOnlyMemory<byte> XCoordinate
        {
            get => _xCoordinate;
            set
            {
                if (value.Length != P256CoordinateLength)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidPublicKeyData));
                }

                _xCoordinate = value.ToArray();
            }
        }

        /// <summary>
        /// The Y-coordinate of the public point.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// On set, the coordinate in not the correct length.
        /// </exception>
        public ReadOnlyMemory<byte> YCoordinate
        {
            get => _yCoordinate;
            set
            {
                if (value.Length != P256CoordinateLength)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidPublicKeyData));
                }

                _yCoordinate = value.ToArray();
            }
        }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private CoseEcPublicKey()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Construct a <see cref="CoseEcPublicKey"/> based on the curve and
        /// point.
        /// </summary>
        /// <remarks>
        /// An ECC public key is a curve and public point. This class supports
        /// only one curve: NIST P-256 (<c>CoseEcCurve.P256</c>). This
        /// constructor expects the length of each coordinate to be at least one
        /// byte and 32 bytes or fewer.
        /// </remarks>
        /// <param name="curve">
        /// The curve for this public key.
        /// </param>
        /// <param name="xCoordinate">
        /// The x-coordinate of the public point.
        /// </param>
        /// <param name="yCoordinate">
        /// The y-coordinate of the public point.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The <c>encodedPoint</c> is not a correct EC Point encoding.
        /// </exception>
        public CoseEcPublicKey(CoseEcCurve curve, ReadOnlyMemory<byte> xCoordinate, ReadOnlyMemory<byte> yCoordinate)
        {
            if (curve != CoseEcCurve.P256 || xCoordinate.Length == 0 || xCoordinate.Length > P256CoordinateLength
                || yCoordinate.Length == 0 || yCoordinate.Length > P256CoordinateLength)
            {
                throw new ArgumentException(ExceptionMessages.InvalidPublicKeyData);
            }

            Curve = CoseEcCurve.P256;
            XCoordinate = xCoordinate;
            YCoordinate = yCoordinate;
            Type = CoseKeyType.Ec2;
            Algorithm = CoseAlgorithmIdentifier.ES256;
        }

        /// <summary>
        /// Construct a <see cref="CoseEcPublicKey"/> based on the CBOR encoding
        /// of a <c>COSE_Key</c>.
        /// </summary>
        /// <param name="encodedCoseKey">
        /// The CBOR encoding.
        /// </param>
        /// <exception cref="Ctap2DataException">
        /// The <c>encodedCoseKey</c> is not a correct EC Public Key encoding.
        /// </exception>
        public CoseEcPublicKey(ReadOnlyMemory<byte> encodedCoseKey)
        {
            var map = new CborMap<int>(encodedCoseKey);

            Curve = (CoseEcCurve)map.ReadInt32(TagCurve);
            XCoordinate = map.ReadByteString(TagX);
            YCoordinate = map.ReadByteString(TagY);
            Type = (CoseKeyType)map.ReadInt32(TagKeyType);
            Algorithm = (CoseAlgorithmIdentifier)map.ReadInt32(TagAlgorithm);
        }

        /// <summary>
        /// Construct a <see cref="CoseEcPublicKey"/> based on .NET elliptic curve parameters.
        /// </summary>
        /// <param name="ecParameters">
        /// An `ECParameters` structure with a specified Curve and a public point Q.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The <c>ECParameters</c> object does not contain a valid curve and
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The parameters/public key specified is not supported.
        /// </exception>
        public CoseEcPublicKey(ECParameters ecParameters)
        {
            if (ecParameters.Curve.IsNamed)
            {
                if (ecParameters.Curve.Oid.Value.Equals(P256Oid, StringComparison.Ordinal))
                {
                    Algorithm = CoseAlgorithmIdentifier.ES256;
                    Type = CoseKeyType.Ec2;
                    Curve = CoseEcCurve.P256;
                    XCoordinate = ecParameters.Q.X;
                    YCoordinate = ecParameters.Q.Y;

                    return;
                }
            }

            throw new NotSupportedException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.UnsupportedAlgorithm));
        }

        /// <summary>
        /// Returns the COSE key as a new .NET <c>ECParameters</c> structure. Used
        /// for interoperating with the .NET crypto library.
        /// </summary>
        /// <returns>
        /// The public key in the form of an <c>ECParameters</c> structure.
        /// </returns>
        public ECParameters ToEcParameters()
        {
            var ecParams = new ECParameters
            {
                Curve = ECCurve.CreateFromValue(P256Oid)
            };

            ecParams.Q.X = _xCoordinate;
            ecParams.Q.Y = _yCoordinate;

            return ecParams;
        }

        /// <inheritdoc/>
        public override byte[] Encode()
        {
            if (_xCoordinate.Length != P256CoordinateLength || _yCoordinate.Length != P256CoordinateLength)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.NoDataToEncode));
            }

            // This encodes the map of 5 things.
            // The standard specifies that the Algorithm is -25, ECDH with
            // HKDF256.
            return new CborMapWriter<int>()
                .Entry(TagKeyType, (int)CoseKeyType.Ec2)
                .Entry(TagAlgorithm, (int)CoseAlgorithmIdentifier.ECDHwHKDF256)
                .Entry(TagCurve, (int)CoseEcCurve.P256)
                .Entry(TagX, XCoordinate)
                .Entry(TagY, YCoordinate)
                .Encode();
        }
    }
}
