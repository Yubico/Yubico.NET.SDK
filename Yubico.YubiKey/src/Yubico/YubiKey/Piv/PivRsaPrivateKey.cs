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
using Yubico.Core.Cryptography;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Piv
{
    /// <summary>
    /// This class holds an RSA private key.
    /// </summary>
    /// <remarks>
    /// At its foundation, an RSA private key consists of a modulus and private
    /// exponent. However, to improve performance, the primeP, primeQ, exponentP,
    /// exponentQ, and coefficient can be used to make the computations. These
    /// are the elements of the Chinese Remainder Theorem method of computing RSA
    /// private key operations. These are the elements needed by the YubiKey.
    /// There are several ways to create an encoded key, however, this class only
    /// supports the encoding scheme specified by Yubico.
    /// <code>
    ///   TLV || TLV || TLV || TLV || TLV
    ///   01 length prime P || 02 length prime Q ||
    ///   03 length prime p Exponent dP || 04 length prime q Exponent dQ ||
    /// </code>
    /// <para>
    /// The YubiKey supports 1024-bit, 2048-bit, 3072-bit, and 4096-bit RSA keys. Each element in
    /// the private key will be half that size. So for a 1024-bit RSA key pair,
    /// the CRT components are each 512 bits (64 bytes) long, for a 2048-bit
    /// RSA key pair, the CRT components are each 1024 bits (128 bytes) long, for a 3072-bit
    /// RSA key pair, the CRT components are each 1536 bits (192 bytes) long, and for a 4096-bit
    /// RSA key pair, the CRT components are each 2048 bits (256 bytes) long.
    /// </para>
    /// <para>
    /// You can build an object from either the encoded private key, and then
    /// examine each component, or you can build an object from the components,
    /// then then examine the encoding.
    /// </para>
    /// </remarks>
    public sealed class PivRsaPrivateKey : PivPrivateKey
    {
        private const int PrimePTag = 0x01;
        private const int PrimeQTag = 0x02;
        private const int ExponentPTag = 0x03;
        private const int ExponentQTag = 0x04;
        private const int CoefficientTag = 0x05;
        private const int Rsa1024CrtBlockSize = 64;
        private const int Rsa2048CrtBlockSize = 128;
        private const int Rsa3072CrtBlockSize = 192;
        private const int Rsa4096CrtBlockSize = 256;
        private const int CrtComponentCount = 5;

        private Memory<byte> _primeP;

        /// <summary>
        /// Contains the prime p portion of the RSA private key.
        /// </summary>
        public ReadOnlySpan<byte> PrimeP => _primeP.Span;

        private Memory<byte> _primeQ;

        /// <summary>
        /// Contains the prime q portion of the RSA private key.
        /// </summary>
        public ReadOnlySpan<byte> PrimeQ => _primeQ.Span;

        private Memory<byte> _exponentP;

        /// <summary>
        /// Contains the exponent p portion of the RSA private key.
        /// </summary>
        public ReadOnlySpan<byte> ExponentP => _exponentP.Span;

        private Memory<byte> _exponentQ;

        /// <summary>
        /// Contains the exponent q portion of the RSA private key.
        /// </summary>
        public ReadOnlySpan<byte> ExponentQ => _exponentQ.Span;

        private Memory<byte> _coefficient;

        /// <summary>
        /// Contains the coefficient portion of the RSA private key.
        /// </summary>
        public ReadOnlySpan<byte> Coefficient => _coefficient.Span;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private PivRsaPrivateKey()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a new instance of an RSA private key object based on the
        /// given CRT components.
        /// </summary>
        /// <param name="primeP">
        /// The prime p as a canonical byte array. It must be 64 bytes (512
        /// bits) or 128 bytes (1024 bits) long.
        /// </param>
        /// <param name="primeQ">
        /// The prime q as a canonical byte array. It must be 64 bytes (512
        /// bits) or 128 bytes (1024 bits) long.
        /// </param>
        /// <param name="exponentP">
        /// The exponent p as a canonical byte array. It must be 64 bytes (512
        /// bits) or 128 bytes (1024 bits) long.
        /// </param>
        /// <param name="exponentQ">
        /// The exponent q as a canonical byte array. It must be 64 bytes (512
        /// bits) or 128 bytes (1024 bits) long.
        /// </param>
        /// <param name="coefficient">
        /// The CRT coefficient as a canonical byte array. It must be 64 bytes
        /// (512 bits) or 128 bytes (1024 bits) long.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The key data supplied is not a supported length.
        /// </exception>
        public PivRsaPrivateKey(
            ReadOnlySpan<byte> primeP,
            ReadOnlySpan<byte> primeQ,
            ReadOnlySpan<byte> exponentP,
            ReadOnlySpan<byte> exponentQ,
            ReadOnlySpan<byte> coefficient)
        {
            Algorithm = primeP.Length switch
            {
                Rsa1024CrtBlockSize => PivAlgorithm.Rsa1024,
                Rsa2048CrtBlockSize => PivAlgorithm.Rsa2048,
                Rsa3072CrtBlockSize => PivAlgorithm.Rsa3072,
                Rsa4096CrtBlockSize => PivAlgorithm.Rsa4096,
                _ => throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPrivateKeyData)),
            };

            if (primeQ.Length != primeP.Length || exponentP.Length != primeP.Length
                || exponentQ.Length != primeP.Length || coefficient.Length != primeP.Length)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPrivateKeyData));
            }

            var tlvWriter = new TlvWriter();
            tlvWriter.WriteValue(PrimePTag, primeP);
            tlvWriter.WriteValue(PrimeQTag, primeQ);
            tlvWriter.WriteValue(ExponentPTag, exponentP);
            tlvWriter.WriteValue(ExponentQTag, exponentQ);
            tlvWriter.WriteValue(CoefficientTag, coefficient);
            EncodedKey = tlvWriter.Encode();

            _primeP = new Memory<byte>(primeP.ToArray());
            _primeQ = new Memory<byte>(primeQ.ToArray());
            _exponentP = new Memory<byte>(exponentP.ToArray());
            _exponentQ = new Memory<byte>(exponentQ.ToArray());
            _coefficient = new Memory<byte>(coefficient.ToArray());
        }

        /// <summary>
        /// Create a new instance of an RSA private key object based on the
        /// encoding.
        /// </summary>
        /// <param name="encodedPrivateKey">
        /// The PIV TLV encoding.
        /// </param>
        /// <returns>
        /// A new instance of a PivRsaPrivateKey object based on the encoding.
        /// </returns>
        public static PivRsaPrivateKey CreateRsaPrivateKey(ReadOnlyMemory<byte> encodedPrivateKey)
        {
            var tlvReader = new TlvReader(encodedPrivateKey);
            var valueArray = new ReadOnlyMemory<byte>[CrtComponentCount];

            int index = 0;
            for (; index < CrtComponentCount; index++)
            {
                valueArray[index] = ReadOnlyMemory<byte>.Empty;
            }

            index = 0;
            while (index < CrtComponentCount)
            {
                if (tlvReader.HasData == false)
                {
                    break;
                }

                int tag = tlvReader.PeekTag();
                var temp = tlvReader.ReadValue(tag);
                if (tag <= 0 || tag > CrtComponentCount)
                {
                    continue;
                }
                if (valueArray[tag - 1].IsEmpty == false)
                {
                    continue;
                }

                index++;
                valueArray[tag - 1] = temp;
            }

            return new PivRsaPrivateKey(
                valueArray[PrimePTag - 1].Span,
                valueArray[PrimeQTag - 1].Span,
                valueArray[ExponentPTag - 1].Span,
                valueArray[ExponentQTag - 1].Span,
                valueArray[CoefficientTag - 1].Span);
        }

        /// <inheritdoc />
        public override void Clear()
        {
            CryptographicOperations.ZeroMemory(_primeP.Span);
            _primeP = Memory<byte>.Empty;

            CryptographicOperations.ZeroMemory(_primeQ.Span);
            _primeQ = Memory<byte>.Empty;

            CryptographicOperations.ZeroMemory(_exponentP.Span);
            _exponentP = Memory<byte>.Empty;

            CryptographicOperations.ZeroMemory(_exponentQ.Span);
            _exponentQ = Memory<byte>.Empty;

            CryptographicOperations.ZeroMemory(_coefficient.Span);
            _coefficient = Memory<byte>.Empty;

            base.Clear();
        }
    }
}
