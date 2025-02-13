// Copyright 2025 Yubico AB
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

namespace Yubico.YubiKey.Piv
{
    /// <summary>
    /// This class holds a private key. It contains the algorithm and TLV
    /// encoding. Subclasses will contain specific components of the key.
    /// </summary>
    /// <remarks>
    /// Note that this class contains a <c>Clear</c> method. This class will hold
    /// copies of sensitive data (the private key data), and that data should be
    /// overwritten as soon as the object is no longer needed. Note that there
    /// are SDK classes that take in a PivPrivateKey object as input, and copy a
    /// reference. For example, look at
    /// <see cref="Commands.ImportAsymmetricKeyCommand"/>. You want to call the
    /// <c>Clear</c> method, but not before the SDK class is done with it. The
    /// documentation for those classes that copy a reference to the private key
    /// you pass in will describe when it is safe to call the Clear method.
    /// <para>
    /// There are currently two kinds of private keys YubiKey supports: RSA and
    /// ECC. This class is the base class for all supported private keys.
    /// </para>
    /// <para>
    /// When you import a key (see <see cref="Commands.ImportAsymmetricKeyCommand"/>),
    /// you provide the private key as an instance of this class. It will really be
    /// an instance of one of the subclasses. You will likely build an instance
    /// of the subclass and pass it in as <c>PivPrivateKey</c>.
    /// </para>
    /// <para>
    /// You will likely build the subclass using the individual components,
    /// rather than the encoding. But it is possible to build an object from an
    /// encoded private key.
    /// </para>
    /// <para>
    /// The TLV encoding of an RSA private key (Yubico proprietary schema) is
    /// <code>
    ///   01 length prime P || 02 length prime Q ||
    ///   03 length prime p Exponent dP || 04 length prime q Exponent dQ ||
    ///   05 length CRT coefficient
    /// </code>
    /// The TLV encoding of an ECC private key (Yubico proprietary schema) is
    /// <code>
    ///   06 length private value s
    /// </code>
    /// </para>
    /// </remarks>
    public class PivPrivateKey
    {
        private const int primePTag = 0x01;
        private const int primeQTag = 0x02;
        private const int exponentPTag = 0x03;
        private const int exponentQTag = 0x04;
        private const int CoefficientTag = 0x05;
        private const int EccTag = 0x06;

        /// <summary>
        /// The algorithm of the key in this object.
        /// </summary>
        /// <value>
        /// RSA or ECC.
        /// </value>
        public PivAlgorithm Algorithm { get; protected set; }

        protected Memory<byte> EncodedKey { get; set; }

        /// <summary>
        /// Contains the TLV encoding of the private key.
        /// </summary>
        public ReadOnlyMemory<byte> EncodedPrivateKey => EncodedKey;

        /// <summary>
        /// This builds an empty object. The <c>Algorithm</c> is <c>None</c> and
        /// the <c>EncodedPrivateKey</c> is empty.
        /// </summary>
        public PivPrivateKey()
        {
            EncodedKey = Memory<byte>.Empty;
        }

        /// <summary>
        /// Create a new instance of a PivPrivateKey from the given encoded value.
        /// </summary>
        /// <remarks>
        /// This will return an instance of either <c>PivRsaPrivateKey</c> or
        /// <c>PivEccPrivateKey</c>.
        /// </remarks>
        /// <param name="encodedPrivateKey">
        /// The PIV TLV encoding.
        /// </param>
        /// <returns>
        /// An instance of a subclass of <c>PivPrivateKey</c>, the actual key
        /// represented by the encoding.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The key data supplied is not a supported encoding.
        /// </exception>
        public static PivPrivateKey Create(ReadOnlyMemory<byte> encodedPrivateKey)
        {
            byte tag = 0;
            if (encodedPrivateKey.Length > 0)
            {
                tag = encodedPrivateKey.Span[0];
            }

            switch (tag)
            {
                default:
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidPrivateKeyData));

                case EccTag:
                    return PivEccPrivateKey.CreateEccPrivateKey(encodedPrivateKey);

                case primePTag:
                case primeQTag:
                case exponentPTag:
                case exponentQTag:
                case CoefficientTag:
                    return PivRsaPrivateKey.CreateRsaPrivateKey(encodedPrivateKey);
            }
        }

        /// <summary>
        /// Call on the object to clear (overwrite) any sensitive data it is
        /// holding.
        /// </summary>
        public virtual void Clear()
        {
            CryptographicOperations.ZeroMemory(EncodedKey.Span);
            EncodedKey = Memory<byte>.Empty;
        }
    }
}
