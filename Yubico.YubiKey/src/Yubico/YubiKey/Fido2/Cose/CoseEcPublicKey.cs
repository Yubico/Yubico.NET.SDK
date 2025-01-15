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
using System.Linq;
using System.Security.Cryptography;
using Yubico.YubiKey.Cryptography;
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

        private byte[] _xCoordinate = Array.Empty<byte>();
        private byte[] _yCoordinate = Array.Empty<byte>();
        #pragma warning disable IDE0032
        private CoseEcCurve _curve;
        #pragma warning restore IDE0032

        /// <summary>
        /// Creates a new instance of <see cref="CoseEcPublicKey"/> from the given encoded COSE key.
        /// </summary>
        /// <param name="encodedCoseKey">
        /// The encoded COSE key in CBOR format.
        /// </param>
        /// <returns>
        /// A <see cref="CoseEcPublicKey"/> object initialized with the provided encoded key data.
        /// </returns>
        /// <exception cref="Ctap2DataException">
        /// Thrown if the <paramref name="encodedCoseKey"/> is not a valid EC Public Key encoding.
        /// </exception>
        public static CoseEcPublicKey CreateFromEncodedKey(ReadOnlyMemory<byte> encodedCoseKey) =>
            new CoseEcPublicKey(encodedCoseKey);

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
                ValidateCurve(value);
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
                ValidateLength(value);
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
                ValidateLength(value);
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
        /// An ECC public key is a curve and public point (x and y coordinates). This
        /// constructor expects the length of each coordinate to be at least one
        /// byte and 32 bytes or fewer.
        /// Valid keys are P-256, P-384, and P-521.
        /// Note: Certain keys might not be supported by the YubiKey. 
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
        /// The xCoordinate or yCoordinate is not the correct length, or when the curve is not supported.
        /// </exception>
        public CoseEcPublicKey(CoseEcCurve curve, ReadOnlyMemory<byte> xCoordinate, ReadOnlyMemory<byte> yCoordinate)
        {
            var definition = curve.GetKeyDefinition();
            var coseDefinition = definition.CoseKeyDefinition ??
                throw new ArgumentException(nameof(curve), "Unknown curve");

            Type = CoseKeyType.Ec2;
            Curve = curve;
            XCoordinate = xCoordinate;
            YCoordinate = yCoordinate;
            Algorithm = coseDefinition.AlgorithmIdentifier;
        }

        /// <summary>
        /// Construct a <see cref="CoseEcPublicKey"/> based on the curve and x and y coordinates.
        /// </summary>
        /// <remarks>
        /// An ECC public key is a curve and public point (x and y coordinates). Valid keys are P-256, P-384, and P-521.
        /// Note: Certain keys might not be supported by the YubiKey. 
        /// </remarks>
        /// <param name="curve">
        /// The curve for this public key.
        /// </param>
        /// <param name="algorithm">The algorithm of the key.</param>
        /// <param name="xCoordinate">
        /// The x-coordinate of the public point.
        /// </param>
        /// <param name="yCoordinate">
        /// The y-coordinate of the public point.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The xCoordinate or yCoordinate is not the correct length, or when the curve is not supported.
        /// </exception>
        public CoseEcPublicKey(
            CoseEcCurve curve,
            CoseAlgorithmIdentifier algorithm,
            ReadOnlyMemory<byte> xCoordinate,
            ReadOnlyMemory<byte> yCoordinate)
        {
            Type = CoseKeyType.Ec2;
            Curve = curve;
            XCoordinate = xCoordinate;
            YCoordinate = yCoordinate;
            Algorithm = algorithm;
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
            if (!ecParameters.Curve.IsNamed)
            {
                throw new NotSupportedException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.UnsupportedAlgorithm));
            }

            var definition = KeyDefinitions.Helper.GetKeyDefinitionByOid(ecParameters.Curve.Oid.Value);
            if (definition.CoseKeyDefinition == null)
            {
                throw new NotSupportedException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.UnsupportedAlgorithm));
            }

            Algorithm = definition.CoseKeyDefinition.AlgorithmIdentifier;
            Curve = definition.CoseKeyDefinition.CurveIdentifier;
            Type = definition.CoseKeyDefinition.Type;
            XCoordinate = ecParameters.Q.X;
            YCoordinate = ecParameters.Q.Y;
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
            var definition = KeyDefinitions.Helper.GetKeyDefinition(_curve);

            var ecParams = new ECParameters
            {
                Curve = ECCurve.CreateFromValue(definition.Oid),
                Q = new ECPoint
                {
                    X = _xCoordinate,
                    Y = _yCoordinate
                }
            };

            return ecParams;
        }

        /// <inheritdoc/>
        public override byte[] Encode()
        {
            if (_xCoordinate.Length == 0 ||
                _yCoordinate.Length == 0)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.NoDataToEncode));
            }

            return new CborMapWriter<int>()
                .Entry(TagKeyType, (int)CoseKeyType.Ec2)

                // .Entry(TagAlgorithm, (int)Algorithm) // Should be correct, right? Instead of -25
                .Entry(
                    TagAlgorithm, (int)CoseAlgorithmIdentifier.ECDHwHKDF256) // Should be correct, right? Instead of -25
                .Entry(TagCurve, (int)Curve)
                .Entry(TagX, XCoordinate)
                .Entry(TagY, YCoordinate)
                .Encode();
        }

        private static void ValidateLength(ReadOnlyMemory<byte> value)
        {
            var allowedLengths = KeyDefinitions.Helper.GetEcKeyDefinitions()
                .Where(c => c.CoseKeyDefinition is { Type: CoseKeyType.Ec2 }).Select(d => d.LengthInBytes);

            if (!allowedLengths.Contains(value.Length))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPublicKeyData, nameof(value)));
            }
        }

        private static void ValidateCurve(CoseEcCurve value)
        {
            var allowedEcCurves = KeyDefinitions.Helper.GetEcKeyDefinitions()
                .Where(d => d.CoseKeyDefinition is { Type: CoseKeyType.Ec2 })
                .Select(d => d.CoseKeyDefinition!.CurveIdentifier);

            if (!allowedEcCurves.Contains(value))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.UnsupportedAlgorithm), nameof(value));
            }
        }
    }
}
