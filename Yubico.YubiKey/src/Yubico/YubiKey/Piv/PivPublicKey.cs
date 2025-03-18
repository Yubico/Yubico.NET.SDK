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
using Microsoft.Extensions.Logging;
using Yubico.Core.Logging;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv
{
    /// <summary>
    /// This class holds a public key. It contains the algorithm and TLV
    /// encoding. Subclasses will contain specific components of the key.
    /// </summary>
    /// <remarks>
    /// There are currently two kinds of public keys YubiKey supports: RSA and
    /// ECC. This class is the base class for public keys.
    /// <para>
    /// There are also two encoding formats: PIV-defined, and YubiKey-specific.
    /// This class handles both. Whether you have the PIV-defined or
    /// YubiKey-specific encoding, provide that encoding to the static
    /// <c>Create</c> factory method. It will be able to recognize both and build
    /// the appropriate key object.
    /// </para>
    /// <para>
    /// Similarly, if you have the individual components of a public key, but
    /// need to build the encoding, use the appropriate subclass to construct an
    /// object. Then if you need the PIV-defined encoding, get it from the
    /// <c>PivEncodedPublicKey</c> property. If you need the YubiKey-specific
    /// encoding, get it from the <c>YubiKeyEncodedPublicKey</c> property.
    /// </para>
    /// <para>
    /// When you get a public key from one of the Response APDUs (such as
    /// Generate Asymmetric or Get Metadata), it will be an instance of this
    /// class, but will really be an instance of one of the subclasses. You can
    /// know which class it is by either looking at the <c>Algorithm</c> property
    /// or using "is":
    /// <code language="csharp">
    ///   PivPublicKey publicKey = response.GetData();
    ///   if (publicKey is PivRsaPublicKey)
    ///   {
    ///        process RSA key
    ///   }
    /// </code>
    /// </para>
    /// <para>
    /// The TLV encoding of an RSA key (from the PIV standard) is
    /// <code>
    ///   7F49 L1 { 81 length modulus || 82 length public exponent }
    /// </code>
    /// The TLV encoding of an ECC key (from the PIV standard) is
    /// <code>
    ///   7F49 L1 { 86 length public point }
    ///   where the public point is 04 || x-coordinate || y-coordinate
    /// </code>
    /// </para>
    /// <para>
    /// The YubiKey-specific encoding is the same as the PIV encoding, but
    /// without the nested <c>7F49</c> tag.
    /// </para>
    /// </remarks>
    public class PivPublicKey
    {
        protected const int EccTag = 0x86;
        protected const int ModulusTag = 0x81;
        protected const int ExponentTag = 0x82;
        protected const int PublicKeyTag = 0x7F49;
        protected static readonly ILogger Logger = Log.GetLogger<PivPublicKey>();

        /// <summary>
        /// The algorithm of the key in this object.
        /// </summary>
        /// <value>
        /// RSA or ECC.
        /// </value>
        public PivAlgorithm Algorithm { get; protected set; }

        public IPublicKeyParameters KeyParameters { get; protected set; } = null!;
        protected KeyDefinitions.KeyDefinition KeyDefinition { get; set; } = new();
        protected Memory<byte> PivEncodedKey { get; set; }
        protected Memory<byte> YubiKeyEncodedKey { get; set; }
        protected Memory<byte> EncodedKey { get; set; }
        protected ReadOnlyMemory<byte> PublicPoint { get; set; }

        /// <summary>
        /// Contains the PIV TLV encoding of the public key. 
        /// If there is no encoded public key, this will be a buffer of length 0.
        /// </summary>
        public ReadOnlyMemory<byte> PivEncodedPublicKey => PivEncodedKey;

        /// <summary>
        /// Contains the TLV encoding of the public key as represented by the
        /// <c>GET METADATA</c> command. 
        /// If there is no encoded public key, this will be a buffer of length 0.
        /// </summary>
        public ReadOnlyMemory<byte> YubiKeyEncodedPublicKey => YubiKeyEncodedKey;

        /// <summary>
        /// This builds an empty object. The <c>Algorithm</c> is <c>None</c> and
        /// the <c>EncodedPublicKey</c> is empty.
        /// </summary>
        public PivPublicKey()
        {
            PivEncodedKey = Memory<byte>.Empty;
            YubiKeyEncodedKey = Memory<byte>.Empty;
        }

        public static PivPublicKey Create(IPublicKeyParameters keyParameters)
        {
            return keyParameters switch
            {
                RSAPublicKeyParameters rsaKey => PivRsaPublicKey.CreateFromPublicKey(rsaKey),
                ECPublicKeyParameters eccKey => PivEccPublicKey.CreateFromPublicKey(eccKey),
                EDsaPublicKeyParameters eccKey => PivEccPublicKey.CreateFromPublicKey(eccKey),
                ECX25519PublicKeyParameters eccKey => PivEccPublicKey.CreateFromPublicKey(eccKey),
                _ => throw new ArgumentException("Invalid key type", nameof(keyParameters))
            };
        }

        /// <summary>
        /// Create a new instance of a PivPublicKey from the given encoded value.
        /// </summary>
        /// <remarks>
        /// This will return an instance of either <c>PivRsaPublicKey</c> or
        /// <c>PivEccPublicKey</c>.
        /// </remarks>
        /// <param name="encodedPublicKey">
        /// The PIV TLV encoding.
        /// </param>
        /// <param name="algorithm"></param>
        /// <returns>
        /// An instance of a subclass of <c>PivPublicKey</c>, the actual key
        /// represented by the encoding.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The key data supplied is not a supported encoding.
        /// </exception>
        public static PivPublicKey Create(ReadOnlyMemory<byte> encodedPublicKey, PivAlgorithm algorithm)
        {
            if (algorithm.IsEcc())
            {
                return PivEccPublicKey.CreateFromPivEncoding(encodedPublicKey, algorithm);
            }

            if (algorithm.IsRsa())
            {
                return PivRsaPublicKey.CreateFromPivEncoding(encodedPublicKey);
            }

            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPublicKeyData));
        }

        public static PivPublicKey Create(ReadOnlyMemory<byte> encodedPublicKey)
        {
            if (PivRsaPublicKey.CanCreate(encodedPublicKey))
            {
                return PivRsaPublicKey.CreateFromPivEncoding(encodedPublicKey);
            }

            if (PivEccPublicKey.CanCreate(encodedPublicKey))
            {
                return PivEccPublicKey.CreateFromPivEncoding(encodedPublicKey);
            }

            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPublicKeyData));
        }
        
        public KeyDefinitions.KeyDefinition GetKeyDefinition() => KeyDefinition;

        public KeyDefinitions.KeyType GetKeyType() => KeyDefinition.KeyType;

        public ReadOnlyMemory<byte> GetPublicPoint() => PublicPoint;

        public ReadOnlyMemory<byte> ExportSubjectPublicKeyInfo() => EncodedKey;

        internal static bool IsValidEccTag(int peekTag) =>
            peekTag switch
            {
                EccTag => true,
                _ => false
            };

        internal static bool IsValidRsaTag(int peekTag) =>
            peekTag switch
            {
                ModulusTag or
                    ExponentTag
                    => true,
                _ => false
            };
    }
}
