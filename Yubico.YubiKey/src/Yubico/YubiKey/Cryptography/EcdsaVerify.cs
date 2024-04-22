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
using System.Security.Cryptography.X509Certificates;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.Cryptography
{
    /// <summary>
    /// This class can verify the ECDSA signature using different types of public
    /// keys.
    /// </summary>
    /// <remarks>
    /// This class will use the default <c>System.Security.Cryptography.ECDsa</c>
    /// implementation to verify a signature. In order to do so, it must be able
    /// to "convert" the public key and the signature into the formats required
    /// by that class.
    /// <para>
    /// The <c>ECDsa</c> class is disposable, which is why this class is as well.
    /// </para>
    /// <para>
    /// Converting the key into the appropriate format is done by the
    /// constructor. The public key you will use to verify the signature will
    /// likely be in one of these formats:
    /// <code>
    ///    PivPublicKey
    ///    CoseKey
    ///    X509Certificate2
    ///    ECDsa
    ///    encoded point (0x04 || x-coordinate || y-coordinate)
    /// </code>
    /// Create an instance of this class with the key you have. Then examine the
    /// <see cref="ECDsa"/> property. You will see your key is now loaded in the
    /// format needed. For example, suppose you have an object that contains
    /// information about a PIV slot, and the <c>PublicKey</c> property is a
    /// <see cref="PivPublicKey"/> containing the public key partner to the
    /// private key in that slot.
    /// <code>
    ///   using var ecdsaVerifier = new EcdsaVerify(slotContents[index].PublicKey);
    /// </code>
    /// </para>
    /// <para>
    /// The <c>ECDsa</c> class also requires the signature to be in a specific
    /// format that is not standard. Most standards specify that an ECDSA
    /// signature is formatted following the BER encoding of the following ASN.1
    /// definition:
    /// <code>
    ///    Ecdsa-Sig-Value  ::=  SEQUENCE  {
    ///      r     INTEGER,
    ///      s     INTEGER  }
    /// </code>
    /// However, the <c>ECDsa</c> class expects the signature to be represented
    /// as the concatenation of <c>r</c> and <c>s</c> where each value is exactly
    /// the curve size. For example, the size of the NIST curve P-256 is 256
    /// bits, which is 32 bytes. Hence, the signature <c>r||s</c> must be 64
    /// bytes, both <c>r</c> and <c>s</c> must be exactly 32 bytes. If a value is
    /// shorter than 32 bytes, it is prepended with <c>00</c> bytes.
    /// </para>
    /// <para>
    /// The verification methods will convert a standard signature into the
    /// format the <c>ECDsa</c> class needs. They can also verify signatures that
    /// are formatted as <c>r||s</c>.
    /// </para>
    /// <para>
    /// The YubiKey returns an ECDSA signature following the standard, namely the
    /// BER encoding.
    /// </para>
    /// <para>
    /// This class can verify signatures for P-256 and P-384 only.
    /// </para>
    /// <para>
    /// Each of the verify methods will return a boolean, indicating whether the
    /// signature verifies or not. If a signature does not verify, that is not an
    /// error. The methods will throw exceptions if they encounter bad data, such
    /// as x- or y-coordinates that do not fit the specified curve.
    /// </para>
    /// <para>
    /// For example,
    /// <code>
    ///    using var ecdsaVfy = new EcdsaVerify(authData.CredentialPublicKey);
    ///    bool isVerified = ecdsaVerify.VerifyDigest(digest, signature);
    /// </code>
    /// <code>
    ///    using var ecdsaVfy = new EcdsaVerify(AttestationCert);
    ///    bool isVerified = ecdsaVerify.VerifyData(dataToVerify, signature, false);
    /// </code>
    /// </para>
    /// </remarks>
    public class EcdsaVerify : IDisposable
    {
        private const int P256EncodedPointLength = 65;
        private const int P384EncodedPointLength = 97;
        private const int MinEncodedPointLength = 65;
        private const int P256KeySize = 256;
        private const int P384KeySize = 384;
        private const string OidP256 = "1.2.840.10045.3.1.7";
        private const string OidP384 = "1.3.132.0.34";

        private const byte EncodedPointTag = 4;
        private const int SequenceTag = 0x30;
        private const int IntegerTag = 0x02;

        private bool _disposed;

        /// <summary>
        /// The object built that will perform the verification operation.
        /// </summary>
        /// <remarks>
        /// This must be P-256 or P-384, and contain valid coordinates.
        /// </remarks>
        public ECDsa ECDsa { get; private set; }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private EcdsaVerify()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create an instance of the <see cref="EcdsaVerify"/> class using the
        /// ECDsa object that contains the public key.
        /// </summary>
        /// <remarks>
        /// This supports only NIST P-256 and P-384 curves.
        /// </remarks>
        /// <param name="ecdsa">
        /// The public key to use to verify. This constructor will copy a
        /// reference to this object.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The ecdsa argument is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The key is not for a supported algorithm or curve, or is malformed.
        /// </exception>
        public EcdsaVerify(ECDsa ecdsa)
        {
            if (ecdsa is null)
            {
                throw new ArgumentNullException(nameof(ecdsa));
            }

            ECDsa = CheckECDsa(ecdsa);
        }

        /// <summary>
        /// Create an instance of the <see cref="EcdsaVerify"/> class using the
        /// PIV ECC public key.
        /// </summary>
        /// <remarks>
        /// This supports only NIST P-256 and P-384 curves.
        /// </remarks>
        /// <param name="pivPublicKey">
        /// The public key to use to verify.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The pivPublicKey argument is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The key is not for a supported algorithm or curve, or is malformed.
        /// </exception>
        public EcdsaVerify(PivPublicKey pivPublicKey)
        {
            if (pivPublicKey is null)
            {
                throw new ArgumentNullException(nameof(pivPublicKey));
            }

            ReadOnlySpan<byte> pubPoint = pivPublicKey is PivEccPublicKey eccKey
                ? eccKey.PublicPoint : ReadOnlySpan<byte>.Empty;

            ECDsa = ConvertPublicKey(pubPoint.ToArray());
        }

        /// <summary>
        /// Create an instance of the <see cref="EcdsaVerify"/> class using the
        /// COSE EC public key.
        /// </summary>
        /// <remarks>
        /// This supports only NIST P-256 and P-384 curves.
        /// </remarks>
        /// <param name="coseKey">
        /// The public key to use to verify.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The coseKey is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The key is not for a supported algorithm or curve, or is malformed.
        /// </exception>
        public EcdsaVerify(CoseKey coseKey)
        {
            if (coseKey is null)
            {
                throw new ArgumentNullException(nameof(coseKey));
            }

            string oid = coseKey.Algorithm switch
            {
                CoseAlgorithmIdentifier.ES256 => OidP256,
                CoseAlgorithmIdentifier.ECDHwHKDF256 => OidP256,
                CoseAlgorithmIdentifier.ES384 => OidP384,
                _ => "",
            };

            byte[] xCoordinate = Array.Empty<byte>();
            byte[] yCoordinate = Array.Empty<byte>();
            if (coseKey is CoseEcPublicKey ecKey)
            {
                xCoordinate = ecKey.XCoordinate.ToArray();
                yCoordinate = ecKey.YCoordinate.ToArray();
            }

            ECDsa = ConvertPublicKey(oid, xCoordinate, yCoordinate);
        }

        /// <summary>
        /// Create an instance of the <see cref="EcdsaVerify"/> class using the
        /// encoded point.
        /// </summary>
        /// <remarks>
        /// This supports only NIST P-256 and P-384 curves and only supports the
        /// uncompressed encoded point: <c>04||x-coordinate||y-coordinate</c>
        /// where both coordinates are the curve size (each coordinate is 32 bytes
        /// for P-256 and 48 bytes for P-384), prepended with 00 bytes if
        /// necessary.
        /// </remarks>
        /// <param name="encodedEccPoint">
        /// The public key to use to verify.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The coseKey is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The key is not for a supported algorithm or curve, or is malformed.
        /// </exception>
        public EcdsaVerify(ReadOnlyMemory<byte> encodedEccPoint)
        {
            ECDsa = ConvertPublicKey(encodedEccPoint);
        }

        /// <summary>
        /// Create an instance of the <see cref="EcdsaVerify"/> class using the
        /// given certificate.
        /// </summary>
        /// <remarks>
        /// This supports only NIST P-256 and P-384 curves.
        /// </remarks>
        /// <param name="certificate">
        /// The certificate containing the public key to use to verify.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The certificate argument is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The key is not for a supported algorithm or curve, or is malformed.
        /// </exception>
        public EcdsaVerify(X509Certificate2 certificate)
        {
            ECDsa = CheckECDsa(certificate.GetECDsaPublicKey());
        }

        /// <summary>
        /// Verify the <c>signature</c> using the  <c>dataToVerify</c>. This
        /// method will digest the <c>dataToVerify</c> using SHA-256 if the
        /// public key is P-256 and SHA-384 if the public key is P-384, and then
        /// verify the signature using the digest.
        /// </summary>
        /// <remarks>
        /// If the signature is the standard BER encoding, then pass <c>true</c>
        /// for <c>isStandardSignature</c>. That argument defaults to <c>true</c>
        /// so if the signature is formatted in the standard way, you can call
        /// this method with that argument missing. If the signature is the
        /// concatenation of <c>r</c> and <c>s</c>, pass <c>false</c> for
        /// <c>isStandardSignature</c>.
        /// </remarks>
        /// <param name="dataToVerify">
        /// The data data to verify. To verify an ECDSA signature, this method
        /// will digest the data using SHA-256 or SHA-384, depending on the
        /// public key's curve.
        /// </param>
        /// <param name="signature">
        /// The signature to verify.
        /// </param>
        /// <param name="isStandardSignature">
        /// <c>true</c> if the signature is formatted as the BER encoding
        /// specified by most standards, or <c>false</c> if the signature is
        /// formatted as the concatenation of <c>r</c> and <c>s</c>.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the signature verifies, <c>false</c> if it
        /// does not.
        /// </returns>
        public bool VerifyData(
            byte[] dataToVerify,
            byte[] signature,
            bool isStandardSignature = true)
        {
            HashAlgorithm digester = ECDsa.KeySize switch
            {
                P256KeySize => CryptographyProviders.Sha256Creator(),
                P384KeySize => CryptographyProviders.Sha384Creator(),
                _ => throw new ArgumentException(ExceptionMessages.UnsupportedAlgorithm),
            };

            return VerifyDigestedData(digester.ComputeHash(dataToVerify), signature, isStandardSignature);
        }

        /// <summary>
        /// Verify the <c>signature</c> using the <c>digestToVerify</c>.
        /// </summary>
        /// <remarks>
        /// If the signature is the standard BER encoding, then pass <c>true</c>
        /// for <c>isStandardSignature</c>. That argument defaults to <c>true</c>
        /// so if the signature is formatted in the standard way, you can call
        /// this method with that argument missing. If the signature is the
        /// concatenation of <c>r</c> and <c>s</c>, pass <c>false</c> for
        /// <c>isStandardSignature</c>.
        /// </remarks>
        /// <param name="digestToVerify">
        /// The digest of the data to verify.
        /// </param>
        /// <param name="signature">
        /// The signature to verify.
        /// </param>
        /// <param name="isStandardSignature">
        /// <c>true</c> if the signature is formatted as the BER encoding
        /// specified by most standards, or <c>false</c> if the signature is
        /// formatted as the concatenation of <c>r</c> and <c>s</c>.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the signature verifies, <c>false</c> if it
        /// does not.
        /// </returns>
        public bool VerifyDigestedData(
            byte[] digestToVerify,
            byte[] signature,
            bool isStandardSignature = true)
        {
            byte[] sig = isStandardSignature ? ConvertSignature(signature, ECDsa.KeySize) : signature;
            return ECDsa.VerifyHash(digestToVerify, sig);
        }

        private static ECDsa ConvertPublicKey(ReadOnlyMemory<byte> encodedEccPoint)
        {
            string oid = "";
            byte[] xCoordinate = Array.Empty<byte>();
            byte[] yCoordinate = Array.Empty<byte>();

            if (encodedEccPoint.Length >= MinEncodedPointLength && encodedEccPoint.Span[0] == EncodedPointTag)
            {
                int coordLength = (encodedEccPoint.Length - 1) / 2;
                xCoordinate = encodedEccPoint.Slice(1, coordLength).ToArray();
                yCoordinate = encodedEccPoint.Slice(1 + coordLength, coordLength).ToArray();

                oid = encodedEccPoint.Length switch
                {
                    P256EncodedPointLength => OidP256,
                    P384EncodedPointLength => OidP384,
                    _ => "",
                };
            }

            return ConvertPublicKey(oid, xCoordinate, yCoordinate);
        }

        private static ECDsa ConvertPublicKey(string oid, byte[] xCoordinate, byte[] yCoordinate)
        {
            if (!string.IsNullOrEmpty(oid))
            {
                var eccCurve = ECCurve.CreateFromValue(oid);
                var eccParams = new ECParameters
                {
                    Curve = (ECCurve)eccCurve
                };

                eccParams.Q.X = xCoordinate;
                eccParams.Q.Y = yCoordinate;

                return CheckECDsa(ECDsa.Create(eccParams));
            }

            throw new ArgumentException(ExceptionMessages.UnsupportedAlgorithm);
        }

        private static ECDsa CheckECDsa(ECDsa toCheck)
        {
            ECParameters eccParams = toCheck.ExportParameters(false);

            int coordinateLength = eccParams.Curve.Oid.Value switch
            {
                OidP256 => (P256EncodedPointLength - 1) / 2,
                OidP384 => (P384EncodedPointLength - 1) / 2,
                _ => -1,
            };

            if (eccParams.Q.X.Length > 0 && eccParams.Q.X.Length <= coordinateLength)
            {
                if (eccParams.Q.Y.Length > 0 && eccParams.Q.Y.Length <= coordinateLength)
                {
                    return toCheck;
                }
            }

            throw new ArgumentException(ExceptionMessages.UnsupportedAlgorithm);
        }


        // Convert the signature from standard to the concatenation of r and s.
        private static byte[] ConvertSignature(byte[] signature, int publicKeyBitSize)
        {
            int coordinateLength = publicKeyBitSize / 8;
            byte[] convertedSignature = new byte[2 * coordinateLength];
            var signatureMemory = new Memory<byte>(convertedSignature);

            var tlvReader = new TlvReader(signature);
            if (tlvReader.TryReadNestedTlv(out tlvReader, SequenceTag))
            {
                if (TryCopyNextInteger(tlvReader, signatureMemory, coordinateLength))
                {
                    if (TryCopyNextInteger(tlvReader, signatureMemory[coordinateLength..], coordinateLength))
                    {
                        return convertedSignature;
                    }
                }
            }

            throw new ArgumentException(ExceptionMessages.UnsupportedAlgorithm);
        }

        // Decode the next value in tlvReader, then copy the result into
        // signatureValue.
        // Copy exactly coordinateLength bytes.
        // The decoded value might have a leading 00 byte. It is safe to ignore
        // it.
        // If the tag is wrong, return false.
        // If the number of non-zero bytes is < CoordinateLength, prepend 00
        // bytes in the output.
        // If the number of non-zero bytes is > CoordinateLength, return false.
        private static bool TryCopyNextInteger(TlvReader tlvReader, Memory<byte> signatureValue, int coordinateLength)
        {
            if (tlvReader.TryReadValue(out ReadOnlyMemory<byte> rsValue, IntegerTag))
            {
                // strip any leading 00 bytes.
                int length = rsValue.Length;
                int index = 0;
                while (length > 0)
                {
                    if (rsValue.Span[index] != 0)
                    {
                        break;
                    }

                    index++;
                    length--;
                }

                // If we still have data and it is not too long, copy
                if (length > 0 && length <= coordinateLength)
                {
                    rsValue[index..].CopyTo(signatureValue[(coordinateLength - length)..]);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Clean up
        /// </summary>
        /// <param name="disposing">
        /// Disposing or called from elsewhere.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    ECDsa.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
