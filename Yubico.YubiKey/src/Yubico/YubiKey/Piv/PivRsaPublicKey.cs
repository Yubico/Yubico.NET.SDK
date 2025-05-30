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
using Yubico.Core.Tlv;
using Yubico.YubiKey.Piv.Converters;

namespace Yubico.YubiKey.Piv
{
    /// <summary>
    /// This class holds an RSA public key.
    /// </summary>
    /// <remarks>
    /// An RSA public key consists of a modulus and public exponent. To build an
    /// RSA key object from the modulus and public exponent, use this class's
    /// constructor.
    /// <para>
    /// Once you have the object built, you can get the encoded public key from
    /// either the <c>PivEncodedPublicKey</c> or <c>YubiKeyEncodedPublicKey</c>
    /// property.
    /// </para>
    /// <para>If you have an encoded public key, and want to build an object, use
    /// the static factory <c>Create</c> method in the base class
    /// <see cref="PivPublicKey"/>.
    /// </para>
    /// <para>
    /// The YubiKey supports 1024-bit, 2048-bit, 3072-bit, and 4096-bit RSA keys, which means
    /// that the modulus must be exactly 128, 256, 384, or 512 bytes long, respectively.
    /// </para>
    /// <para>
    /// The YubiKey supports only F4 (0x010001 = decimal 65,537) as the public
    /// exponent. Note that if you have the public exponent as an int, you can
    /// convert it to a byte array by using <c>BinaryPrimitives</c>.
    /// <code language="csharp">
    ///   var exponentAsArray = new byte[4];
    ///   BinaryPrimitives.WriteInt32BigEndian(exponentAsArray, exponentAsInt);
    /// </code>
    /// Unlike the modulus, leading 00 bytes in the public exponent are ignored.
    /// If you want to examine the public exponent as an int, you can either
    /// know that if the class exists, the input was F4 (<c>0x00010001</c>), or
    /// use the <c>BinaryPrimitives.ReadInt32BigEndian</c> method.
    /// </para>
    /// <para>
    /// You can build an object from either the encoded public key (using the
    /// <c>PivPublicKey.Create</c> static factory method), and then examine the
    /// modulus and public exponent, or you can build an object from the modulus
    /// and public exponent, then examine the encoding.
    /// </para>
    /// </remarks>
    [Obsolete("Usage of PivEccPublic/PivEccPrivateKey PivRsaPublic/PivRsaPrivateKey is deprecated. Use implementations of ECPublicKey, ECPrivateKey and RSAPublicKey, RSAPrivateKey instead", false)]
    public sealed class PivRsaPublicKey : PivPublicKey
    {
        private readonly byte[] _exponentF4 = { 0x01, 0x00, 0x01 };
        private Memory<byte> _modulus;
        private Memory<byte> _publicExponent;

        // The default constructor. We don't want it to be used by anyone outside
        // this class.
        private PivRsaPublicKey()
        {
        }

        /// <summary>
        /// Create a new instance of an RSA public key object based on the
        /// given modulus and public exponent.
        /// </summary>
        /// <param name="modulus">
        /// The modulus as a canonical byte array. It must be 128 bytes (1024
        /// bits) or 256 bytes (2048 bits) long.
        /// </param>
        /// <param name="publicExponent">
        /// The public exponent as a canonical byte array. Note that the YubiKey
        /// supports only <c>0x01 0x00 0x01</c> (aka F4) as a public exponent.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The key data supplied is not supported.
        /// </exception>
        public PivRsaPublicKey(ReadOnlySpan<byte> modulus, ReadOnlySpan<byte> publicExponent)
        {
            if (LoadRsaPublicKey(modulus, publicExponent) == false)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPublicKeyData));
            }
        }

        /// <summary>
        /// Contains the modulus portion of the RSA public key.
        /// </summary>
        public ReadOnlySpan<byte> Modulus => _modulus.Span;

        /// <summary>
        /// Contains the public exponent portion of the RSA public key.
        /// </summary>
        public ReadOnlySpan<byte> PublicExponent => _publicExponent.Span;

        internal static bool TryCreate(
            out PivPublicKey publicKeyObject,
            ReadOnlyMemory<byte> encodedPublicKey)
        {
            var pivRsaPublicKey = new PivRsaPublicKey();
            publicKeyObject = pivRsaPublicKey;

            try
            {
                var (modulus, exponent) = PivEncodingReader.GetPublicRSAValues(encodedPublicKey);
                return pivRsaPublicKey.LoadRsaPublicKey(
                    modulus.Span,
                    exponent.Span);
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (TlvException)
            {
                return false;
            }
        }

        // Load the modulus and exponent and build the encoded key.
        // This method will verify that this class supports the public key given.
        // If successful, return true.
        // If the key given is not supported or the key could not be loaded,
        // return false.
        private bool LoadRsaPublicKey(ReadOnlySpan<byte> modulus, ReadOnlySpan<byte> publicExponent)
        {
            int keySize = modulus.Length * 8;
            switch (keySize)
            {
                case 1024:
                    Algorithm = PivAlgorithm.Rsa1024;
                    break;
                case 2048:
                    Algorithm = PivAlgorithm.Rsa2048;
                    break;
                case 3072:
                    Algorithm = PivAlgorithm.Rsa3072;
                    break;
                case 4096:
                    Algorithm = PivAlgorithm.Rsa4096;
                    break;
                default:
                    return false;
            }

            // Make sure the most significant bit of the modulus is positive
            if ((modulus[0] & 0x80) == 0)
            {
                return false;
            }

            if (IsExponentF4(publicExponent) == false)
            {
                return false;
            }

            var tlvWriter = new TlvWriter();
            using (tlvWriter.WriteNestedTlv(PivConstants.PublicKeyTag))
            {
                tlvWriter.WriteValue(PivConstants.PublicRSAModulusTag, modulus);
                tlvWriter.WriteValue(PivConstants.PublicRSAExponentTag, _exponentF4);
            }

            PivEncodedKey = tlvWriter.Encode();

            // Since the public key is nested within the TLV structure, 
            // we must offset by {keyOffsetIndex} to access the public key.
            // The keyOffsetIndex is 4 or 5 for the RSA key sizes we support.
            // The offset of 4 is correct for up to 128 bytes of data (size of RSA1024)
            // The offset of 5 is correct for up to 64 KiB of data - large enough to accomodate any existing larger RSA key sizes.
            int keyOffsetIndex = Algorithm == PivAlgorithm.Rsa1024
                ? 4
                : 5;

            YubiKeyEncodedKey = PivEncodedKey[keyOffsetIndex..];

            _modulus = new Memory<byte>(modulus.ToArray());
            _publicExponent = new Memory<byte>(_exponentF4);

            return true;
        }

        // Is the given exponent 01 00 01?
        // This will allow leading 00 bytes, such as 00 01 00 01.
        private bool IsExponentF4(ReadOnlySpan<byte> exponent)
        {
            const int ValidExponentLength = 3;
            if (exponent.Length < ValidExponentLength)
            {
                return false;
            }

            int index = 0;

            while (exponent.Length - index > ValidExponentLength)
            {
                if (exponent[index] != 0)
                {
                    return false;
                }

                index++;
            }

            return exponent.EndsWith<byte>(_exponentF4);
        }
    }
}
