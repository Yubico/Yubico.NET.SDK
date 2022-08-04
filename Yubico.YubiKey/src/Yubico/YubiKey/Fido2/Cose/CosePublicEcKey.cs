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
using System.Security.Cryptography;
using Yubico.YubiKey.Fido2.Commands;

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
    /// parameters via the properties on this class. Another constructs a key based on the COSE form enocded in CBOR.
    /// Lastly, there is a constructor that takes in a .NET representation of an EC public key used for interoperating
    /// with the .NET cryptographic library.
    /// </para>
    /// <para>
    /// The YubiKey's FIDO2 application currently only supports the NIST P-256 curve. Thus, the SDK - as of version 1.5.0
    /// - will also only support this curve.
    /// </para>
    /// </remarks>
    public class CosePublicEcKey : CoseKey
    {
        private const long TagCurve = -1;
        private const long TagX = -2;
        private const long TagY = -3;

        private ECParameters _ecParameters;

        /// <summary>
        /// The Elliptic Curve that the key resides on.
        /// </summary>
        public CoseEcCurve Curve
        {
            get => NamedCurveToCoseCurve(_ecParameters.Curve);
            set => CoseCurveToNamedCurve(value);
        }

        /// <summary>
        /// The X-coordinate of the public point.
        /// </summary>
        public ReadOnlyMemory<byte> X
        {
            get => _ecParameters.Q.X;
            set => _ecParameters.Q.X = value.ToArray();
        }

        /// <summary>
        /// The Y-coordinate of the public point.
        /// </summary>
        public ReadOnlyMemory<byte> Y
        {
            get => _ecParameters.Q.Y;
            set => _ecParameters.Q.Y = value.ToArray();
        }

        /// <inheritdoc />
        public CosePublicEcKey(CborMap map) : base(map)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            Curve = (CoseEcCurve)map.ReadUInt64(TagCurve);
            X = map.ReadByteString(TagX);
            Y = map.ReadByteString(TagY);
        }

        /// <summary>
        /// Construct a <see cref="CosePublicEcKey"/> based on .NET elliptic curve parameters.
        /// </summary>
        /// <param name="ecParameters">
        /// An `ECParameters` structure with a specified Curve and a public point Q.
        /// </param>
        public CosePublicEcKey(ECParameters ecParameters)
        {
            _ecParameters = ecParameters;
        }

        /// <summary>
        /// Constructs a new instance of <see cref="CosePublicEcKey"/>.
        /// </summary>
        public CosePublicEcKey()
        {

        }

        /// <summary>
        /// Returns the COSE key as a .NET `ECParameters` structure. Used for interoperating with the .NET crypto library.
        /// </summary>
        /// <returns>
        /// The public key in the form of an `ECParameters` structure.
        /// </returns>
        public ECParameters AsEcParameters() => _ecParameters;

        private static ECCurve CoseCurveToNamedCurve(CoseEcCurve curveId) =>
            curveId switch
            {
                CoseEcCurve.P256 => ECCurve.NamedCurves.nistP256,
                CoseEcCurve.P384 => ECCurve.NamedCurves.nistP384,
                CoseEcCurve.P521 => ECCurve.NamedCurves.nistP521,
                _ => throw new NotSupportedException("Elliptic curve not supported.")
            };

        private static CoseEcCurve NamedCurveToCoseCurve(ECCurve namedCurve) =>
            namedCurve switch
            {
                _ when namedCurve.Oid == ECCurve.NamedCurves.nistP256.Oid => CoseEcCurve.P256,
                _ when namedCurve.Oid == ECCurve.NamedCurves.nistP384.Oid => CoseEcCurve.P384,
                _ when namedCurve.Oid == ECCurve.NamedCurves.nistP521.Oid => CoseEcCurve.P521,
                _ => throw new NotSupportedException("Elliptic curve not supported.")
            };
    }
}
