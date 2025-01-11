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
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.Cbor;

namespace Yubico.YubiKey.Fido2.Cose
{
    /// <summary>
    /// A representation of an Elliptic Curve public key in COSE form.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An ECC public key consists of a curve and public key data. In FIDO2, the curve is represented by the
    /// <see cref="CoseAlgorithmIdentifier"/> .
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
    /// </remarks>
    public class CoseEdDsaPublicKey : CoseKey
    {
        private const int TagCurve = -1;
        private const int TagPublicKey = -2;
        private const int Ed25519PublicKeyLength = 32;
        private byte[] _publicKey = Array.Empty<byte>();
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
                if (value != CoseEcCurve.Ed25519) // TODO do validation somewhere else?
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
        /// The public key data.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// On set, the key data is not the correct length.
        /// </exception>
        public ReadOnlyMemory<byte> PublicKey
        {
            get => _publicKey;
            set
            {
                if (value.Length != Ed25519PublicKeyLength)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidPublicKeyData));
                }

                _publicKey = value.ToArray();
            }
        }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private CoseEdDsaPublicKey()
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
        /// <param name="publicKey">
        /// The x-coordinate of the public point.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The <c>encodedPoint</c> is not a correct EC Point encoding.
        /// </exception>
        public CoseEdDsaPublicKey(CoseEcCurve curve, ReadOnlyMemory<byte> publicKey)
        {
            if (curve != CoseEcCurve.Ed25519 || 
                publicKey.Length != Ed25519PublicKeyLength)
            {
                throw new ArgumentException(ExceptionMessages.InvalidPublicKeyData);
            }

            // todo use KeyDefinition Type and Algorithm??
            Curve = curve;
            PublicKey = publicKey;
            Type = CoseKeyType.Okp;
            Algorithm = CoseAlgorithmIdentifier.EdDSA;
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
        public CoseEdDsaPublicKey(ReadOnlyMemory<byte> encodedCoseKey)
        {
            var map = new CborMap<int>(encodedCoseKey);

            Curve = (CoseEcCurve)map.ReadInt32(TagCurve);
            PublicKey = map.ReadByteString(TagPublicKey);
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
        // ReSharper disable once UnusedParameter.Local
        public CoseEdDsaPublicKey(ECParameters ecParameters)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override byte[] Encode()
        {
            if (_publicKey.Length != Ed25519PublicKeyLength)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.NoDataToEncode));
            }

            return new CborMapWriter<int>()
                .Entry(TagKeyType, (int)CoseKeyType.Okp)
                .Entry(TagAlgorithm, (int)CoseAlgorithmIdentifier.EdDSA)
                .Entry(TagCurve, (int)CoseEcCurve.Ed25519)
                .Entry(TagPublicKey, PublicKey)
                .Encode();
        }
    }
}
