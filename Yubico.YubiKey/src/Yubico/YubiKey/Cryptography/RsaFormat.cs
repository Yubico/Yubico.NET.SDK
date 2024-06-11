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
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Cryptography
{
    /// <summary>
    /// This class contains methods that can build and read data formatted for
    /// RSA sign/verify and encryption/decryption operations.
    /// </summary>
    /// <remarks>
    /// Currently this class will format data into only PKCS #1 v1.5 and PKCS #1
    /// v.2 PSS and OAEP constructions. Furthermore, this class will only build
    /// specific subsets of PSS and OAEP.
    /// <para>
    /// Note that there are attacks on RSA decryption unpadding operations. To
    /// learn more about these attacks, whether the YubiKey is vulnerable, and
    /// mitigations, see the
    /// <xref href="UsersManualRsaUnpad">User's Manual entry</xref> on the topic.
    /// </para>
    /// </remarks>
    public static class RsaFormat
    {
        private const int NoMoreData = 1;
        private const int ReadNested = 2;
        private const int ReadNestedNoMoreData = ReadNested + NoMoreData;
        private const int ReadValue = 4;
        private const int ReadValueNoMoreData = ReadValue + NoMoreData;

        private const byte Pkcs1LeadByte = 0;
        private const byte Pkcs1SignByte = 1;
        private const byte Pkcs1EncryptByte = 2;
        private const int Pkcs1Separator = 0;
        private const int Pkcs1SignPadByte = 0xFF;
        private const int Pkcs1MinPadLength = 8;

        private const byte SequenceTag = 0x30;
        private const byte OidTag = 0x06;
        private const byte NullTag = 0x05;
        private const byte OctetTag = 0x04;

        private const byte TrailerField = 0xBC;

        private const int Sha1OidLength = 5;
        private const int Sha2OidLength = 9;
        private const byte Sha256OidByte = 1;
        private const byte Sha384OidByte = 2;
        private const byte Sha512OidByte = 3;

        /// <summary>
        /// Use this value to indicate the key size, in bits, is 1024. The
        /// <c>KeySizeBits</c> values listed in this class are the sizes
        /// supported and provided as a convenience to the user to verify the
        /// supported sizes.
        /// </summary>
        public const int KeySizeBits1024 = 1024;

        /// <summary>
        /// Use this value to indicate the key size, in bits, is 2048. The
        /// <c>KeySizeBits</c> values listed in this class are the sizes
        /// supported and provided as a convenience to the user to verify the
        /// supported sizes.
        /// </summary>
        public const int KeySizeBits2048 = 2048;
        
        /// <summary>
        /// Use this value to indicate the key size, in bits, is 3072. The
        /// <c>KeySizeBits</c> values listed in this class are the sizes
        /// supported and provided as a convenience to the user to verify the
        /// supported sizes.
        /// </summary>
        public const int KeySizeBits3072 = 3072;

        /// <summary>
        /// Use this value to indicate the key size, in bits, is 4096. The
        /// <c>KeySizeBits</c> values listed in this class are the sizes
        /// supported and provided as a convenience to the user to verify the
        /// supported sizes.
        /// </summary>
        public const int KeySizeBits4096 = 4096;


        /// <summary>
        /// Use this value to indicate the digest algorithm is SHA-1.
        /// </summary>
        public const int Sha1 = 1;

        private const int Sha1Length = 20;

        /// <summary>
        /// Use this value to indicate the digest algorithm is SHA-256.
        /// </summary>
        public const int Sha256 = 3;

        private const int Sha256Length = 32;

        /// <summary>
        /// Use this value to indicate the digest algorithm is SHA-384.
        /// </summary>
        public const int Sha384 = 4;

        private const int Sha384Length = 48;

        /// <summary>
        /// Use this value to indicate the digest algorithm is SHA-512.
        /// </summary>
        public const int Sha512 = 5;

        private const int Sha512Length = 64;

        /// <summary>
        /// Build the digest into a PKCS #1 v1.5 formatted block for signing (see
        /// RFC 8017).
        /// </summary>
        /// <remarks>
        /// This method will build a new buffer that is <c>keySizeBits</c> long
        /// and contains the following data.
        /// <code>
        ///   00 || 01 || FF FF ... FF || 00 || DigestInfo(digest)
        /// </code>
        /// The <c>DigestInfo</c> is the DER encoding of the ASN.1 definition
        /// <code>
        ///    DigestInfo ::= SEQUENCE {
        ///        digestAlgorithm   DigestAlgorithm,
        ///        digest            OCTET STRING
        ///    }
        /// </code>
        /// <para>
        /// This method supports only the following digest algorithms. Note that
        /// the length of the <c>digest</c> given must match the
        /// <c>digestAlgorithm</c>, otherwise the method will throw an exception.
        /// <code>
        ///   SHA-1     RsaFormat.Sha1     20 bytes
        ///   SHA-256   RsaFormat.Sha256   32 bytes
        ///   SHA-384   RsaFormat.Sha384   48 bytes
        ///   SHA-512   RsaFormat.Sha512   64 bytes
        /// </code>
        /// </para>
        /// <para>
        /// This method supports only <c>keySizeBits</c> values that are defined
        /// in this class as <c>KeySizeBits-x-</c>, such as
        /// <c>RsaFormat.KeySizeBits1024</c> (x=1024). You can use one of these
        /// values or simply the actual key size in bits. For example, if the key
        /// size in bits is 1024, then either <c>RsaFormat.KeySizeBits1024</c> or
        /// <c>1024</c> are valid input to this method.
        /// </para>
        /// <para>
        /// For example, if the <c>digest</c> is 32 bytes long, the
        /// <c>digestAlgorithm</c> is <c>RsaFormat.Sha256</c> and the
        /// <c>keySizeBits</c> is 1024, the result of this method will look like
        /// the following.
        /// <code>
        ///    00 01 FF FF FF FF FF FF FF FF FF FF FF FF FF FF
        ///    FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF
        ///    FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF
        ///    FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF
        ///    FF FF FF FF FF FF FF FF FF FF FF FF 00 30 31 30
        ///    0d 06 09 60 86 48 01 65 03 04 02 01 05 00 04 20
        ///    01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10
        ///    11 12 13 14 15 16 17 18 19 1A 1B 1C 1D 1E 1F 20
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="digest">
        /// The message digest value to format.
        /// </param>
        /// <param name="digestAlgorithm">
        /// The algorithm used to compute the message digest. It must be one of
        /// the digest algorithms defined in this class: <c>RsaFormat.Sha1</c>,
        /// <c>RsaFormat.Sha256</c>, and so on.
        /// </param>
        /// <param name="keySizeBits">
        /// The size of the key used, in bits. This value must be one of the
        /// <c>RsaFormat.KeySizeBits-x-</c> values.
        /// </param>
        /// <returns>
        /// A new byte array containing the formatted digest.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The digest length does not match the <c>digestAlgorithm</c>, or the
        /// <c>digestAlgorithm</c> is not supported, or the <c>keySizeBits</c> is
        /// not supported.
        /// </exception>
        public static byte[] FormatPkcs1Sign(ReadOnlySpan<byte> digest, int digestAlgorithm, int keySizeBits)
        {
            byte[] buffer = GetKeySizeBuffer(keySizeBits);

            var bufferAsSpan = new Span<byte>(buffer);

            // This method will check digest to verify it is supported, and it
            // will check that digest.Length is correct, so we don't have to
            // check those inputs here.
            int digestInfoLength = BuildDigestInfo(digest, digestAlgorithm, bufferAsSpan);

            int paddingLength = (keySizeBits / 8) - (digestInfoLength + 3);
            bufferAsSpan.Slice(2, paddingLength).Fill(Pkcs1SignPadByte);

            buffer[0] = Pkcs1LeadByte;
            buffer[1] = Pkcs1SignByte;
            buffer[paddingLength + 2] = Pkcs1Separator;

            return buffer;
        }

        /// <summary>
        /// Try to parse the <c>formattedSignature</c> as a PKCS #1 v1.5 block
        /// for verifying (see RFC 8017).
        /// </summary>
        /// <remarks>
        /// This method will extract the message digest algorithm and the message
        /// digest itself from the formatted signature. If it is successful, it
        /// will return <c>true</c>. If it cannot extract the information, it
        /// will return <c>false</c>. The caller will likely decrypt an RSA
        /// signature, then try to parse it as PKCS #1 v1.5. If successful, the
        /// digest is collected. If not, try to parse it as PKCS #1 v2 PSS.
        /// <para>
        /// The method will verify that the first byte is <c>00</c>, the second
        /// byte is <c>01</c>, and that the padding bytes are all <c>FF</c>. It
        /// will then expect to find <c>00</c> and then the DigestInfo.
        /// </para>
        /// <para>
        /// It will read the DigestInfo to determine the algorithm. If the method
        /// recognizes the OID, it will set the output int <c>digestAlgorithm</c>
        /// to one of the supported values: <c>RsaFormat.Sha1</c>, <c>Sha256</c>,
        /// or so on.
        /// </para>
        /// <para>
        /// Finally, the method will return a byte array containing the actual
        /// digest. This will be a new buffer.
        /// </para>
        /// <para>
        /// This method only supports signatures 128 or 256 bytes (1024 or 2048
        /// bits) long.
        /// </para>
        /// <para>
        /// If any element fails (the length of the <c>formattedSignature</c> is
        /// not supported, an expected byte is not there, the OID does not
        /// represent a supported algorithm, the digest is not the proper length,
        /// or so on), the method will return false. If there is an error, the
        /// method might set the output <c>digestAlgorithm</c> to 0 and the
        /// output <c>digest</c> to an empty byte array. However, the algorithm
        /// and digest output arguments might contain the purported algorithm and
        /// digest.
        /// </para>
        /// </remarks>
        /// <param name="formattedSignature">
        /// The data to parse.
        /// </param>
        /// <param name="digestAlgorithm">
        /// An output argument, the method will set it to one of the values
        /// defined in this class representing the algorithm:
        /// <c>RsaFormat.Sha1</c>, and so on.
        /// </param>
        /// <param name="digest">
        /// An output argument, the method will set it to be a new byte array
        /// containing the digest portion of the signature.
        /// </param>
        /// <returns>
        /// <c>True</c> if the method is able to parse, <c>false</c> otherwise.
        /// </returns>
        public static bool TryParsePkcs1Verify(ReadOnlySpan<byte> formattedSignature,
                                               out int digestAlgorithm,
                                               out byte[] digest)
        {
            digestAlgorithm = 0;
            digest = Array.Empty<byte>();

            if (formattedSignature.Length != 128 && formattedSignature.Length != 256)
            {
                return false;
            }

            // We expect to find 00 01 FF ... FF 00
            if (formattedSignature[0] != Pkcs1LeadByte || formattedSignature[1] != Pkcs1SignByte)
            {
                return false;
            }

            // Find the first non-FF byte.
            int index = 2;

            for (; index < formattedSignature.Length; index++)
            {
                if (formattedSignature[index] != Pkcs1SignPadByte)
                {
                    break;
                }
            }

            // Where was the first non-FF byte found? There should be at least 8
            // pad bytes, and because there are 2 leading bytes (00 01), that
            // means we need the index to be at least 10. Make sure there was a
            // non-FF byte, and that the first non-FF byte is 00.
            if (index < Pkcs1MinPadLength + 2 || index >= formattedSignature.Length ||
                formattedSignature[index] != Pkcs1Separator)
            {
                return false;
            }

            // The remaining data should be the DigestInfo.
            //   30 len
            //      30 len
            //         06 len OID
            //         05 00
            //      04 len
            //         digest
            bool isValid;
            byte[] digestInfo = formattedSignature[(index + 1)..].ToArray();

            try
            {
                var tlvReader = new TlvReader(digestInfo);

                isValid = TryReadDer(true, ReadNestedNoMoreData, SequenceTag, tlvReader, out TlvReader infoReader,
                    out _);

                isValid = TryReadDer(isValid, ReadNested, SequenceTag, infoReader, out TlvReader oidReader, out _);
                isValid = TryReadDer(isValid, ReadValue, OidTag, oidReader, out _, out ReadOnlyMemory<byte> oid);

                isValid = TryReadDer(isValid, ReadValueNoMoreData, NullTag, oidReader, out _,
                    out ReadOnlyMemory<byte> oidParams);

                isValid = TryReadDer(isValid, ReadValueNoMoreData, OctetTag, infoReader, out _,
                    out ReadOnlyMemory<byte> digestData);

                isValid = TryParseOid(isValid, oid, oidParams, digestData, out digestAlgorithm);
                digest = digestData.ToArray();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(digestInfo);
            }

            return isValid;
        }

        /// <summary>
        /// Build the digest into a PKCS #1 v2 PSS formatted block for signing (see
        /// RFC 8017).
        /// </summary>
        /// <remarks>
        /// The PSS (probabilistic signature scheme) padding operation has a
        /// number of parameters: hash function, mask generating function, salt
        /// length, and trailer field. This method will use the input
        /// <c>digestAlgorithm</c> as the hash function, MGF1 as the mask
        /// generating function, the digest length as the salt length, and 0xBC
        /// as the trailer field.
        /// <para>
        /// The default hash function is SHA-1, but the standard recommends using
        /// the same hash function in PSS operations as was used to digest the
        /// data to sign. Hence, this method will do so. The caller provides the
        /// digest (the data to format), along with a flag indicating the
        /// algorithm. The algorithm must be one supported by this class:
        /// <c>RsaFormat.Sha1</c>, <c>RsaFormat.Sha256</c>, and so on. Note that
        /// the length of the <c>digest</c> given must match the
        /// <c>digestAlgorithm</c>, otherwise the method will throw an exception.
        /// </para>
        /// <para>
        /// The default salt length is 20, but the standard recommends using the
        /// digest length as the salt length. This method will do that. For
        /// example, if the digest is SHA-256, the salt length will be 32. Note
        /// that the C# PSS implementation (see the
        /// <c>System.Security.Cryptography.RSA</c> class) uses the digest length
        /// as the salt length exclusively, the same as this method.
        /// </para>
        /// <para>
        /// Note that it is not possible to use SHA-512 as the digest algorithm
        /// with PSS and a 1024-bit key. The formatted data will be at least 2
        /// times digest length plus two bytes long. So a PSS-formatted block
        /// with SHA-512 will be at a minimum (2 * 64) + 2 = 130 bytes long. But
        /// with a 1024-bit RSA key, the block is 128 bytes long.
        /// </para>
        /// <para>
        /// This method will use the random number generator and message digest
        /// implementations from <see cref="CryptographyProviders"/>.
        /// </para>
        /// <para>
        /// This method supports only <c>keySizeBits</c> values that are defined
        /// in this class as <c>KeySizeBits-x-</c>, such as
        /// <c>RsaFormat.KeySizeBits1024</c> (x=1024). You can use one of these
        /// values or simply the actual key size in bits. For example, if the key
        /// size in bits is 1024, then either <c>RsaFormat.KeySizeBits1024</c> or
        /// <c>1024</c> are valid input to this method.
        /// </para>
        /// </remarks>
        /// <param name="digest">
        /// The message digest value to format.
        /// </param>
        /// <param name="digestAlgorithm">
        /// The algorithm used to compute the message digest. It must be one of
        /// the digest algorithms defined in this class: <c>RsaFormat.Sha1</c>,
        /// <c>RsaFormat.Sha256</c>, and so on.
        /// </param>
        /// <param name="keySizeBits">
        /// The size of the key used, in bits. This value must be one of the
        /// <c>RsaFormat.KeySizeBits-x-</c> values.
        /// </param>
        /// <returns>
        /// A new byte array containing the formatted digest.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The digest length does not match the <c>digestAlgorithm</c>, or the
        /// <c>digestAlgorithm</c> is not supported, or the <c>keySizeBits</c> is
        /// not supported.
        /// </exception>
        public static byte[] FormatPkcs1Pss(ReadOnlySpan<byte> digest, int digestAlgorithm, int keySizeBits)
        {
            byte[] buffer = GetKeySizeBuffer(keySizeBits);

            var bufferAsSpan = new Span<byte>(buffer);

            using HashAlgorithm digester = GetHashAlgorithm(digestAlgorithm);

            if (digest.Length * 8 != digester.HashSize)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncorrectDigestLength));
            }

            // Compute where the salt and Hash will be.
            //   PS || 01 || salt || H || BC
            // The  PS || 01 || salt  make up DB.
            // The salt is the same length as the digest.
            // The first bufferLen - (saltLen + hashLen + 2) bytes are PS.
            int psLength = buffer.Length - ((2 * digest.Length) + 2);
            int offsetSalt = psLength + 1;
            int offsetHash = offsetSalt + digest.Length;

            // The PS can be 0 bytes long, but it certainly can't be < 0.
            if (psLength < 0)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncorrectRsaKeyLength));
            }

            // Create H which is the digest of M' = 00 ... 00 || digest || salt.
            // that's 8 00 octets, the digest, and 20 bytes of random salt.
            _ = digester.TransformBlock(buffer, 0, 8, null, 0);

            // Now copy the digest into the buffer, just so we can operate on it
            // (we need it in a byte array, not a Span).
            digest.CopyTo(bufferAsSpan[offsetHash..]);
            _ = digester.TransformBlock(buffer, offsetHash, digest.Length, null, 0);

            // Generate the random salt and digest it.
            using RandomNumberGenerator randomObject = CryptographyProviders.RngCreator();
            randomObject.GetBytes(buffer, offsetSalt, digest.Length);
            _ = digester.TransformFinalBlock(buffer, offsetSalt, digest.Length);

            // Place H into its location in the buffer.
            // Also, place the 01 (that comes after PS) and the trailer field.
            Array.Copy(digester.Hash, 0, buffer, offsetHash, digester.Hash.Length);
            buffer[psLength] = 1;
            buffer[^1] = TrailerField;

            // Now compute the mask for DB using MGF1.
            PerformMgf1(buffer, offsetHash, digest.Length, buffer, 0, psLength + digest.Length + 1, digester);

            // Note that at this point, the algorithm calls for making sure the
            // appropriate leading bits are all 0. Because we support only 1024-
            // and 2048-bit blocks, there will be only one leading 0 bit.
            // This is to make sure the result is < modulus.
            buffer[0] &= 0x7F;

            return buffer;
        }

        /// <summary>
        /// Try to parse the <c>formattedSignature</c> as a PKCS #1 v2 PSS block
        /// for verifying (see RFC 8017).
        /// </summary>
        /// <remarks>
        /// Note that this method will not return the digest of the data. In
        /// fact, the caller must supply the digest. The reason is that the
        /// digest of the data (the foundation of the signature) is not part of
        /// the formatted data. Rather, the formatted data contains a value
        /// derived from the digest.
        /// <para>
        /// With earlier RSA padding schemes (such as PKCS #1 v1.5), the signer
        /// digested the message (call this the signer's digest), and formatted
        /// that digest into a block. That block was encrypted using the private
        /// key.
        /// </para>
        /// <para>
        /// Verification required the verifier to digest the message (call this the
        /// verifier's digest), use the public key to decrypt the signature,
        /// parse the decrypted data to extract the digest (the signer's digest),
        /// then compare (memcmp) the two digests. If the signer's and verifier's
        /// digests match, the signature verified.
        /// <code>
        ///   Signer:
        ///     message --> Digest(message) --> signer's digest
        ///     signer's digest --> format --> RSA(privateKey, formattedData) --> signature
        ///     send message + signature
        ///<br/>
        ///   Verifier:
        ///     message --> Digest(message) --> verifier's digest
        ///     signature --> RSA(publicKey, signature) --> formattedDigest
        ///     formattedDigest --> parse --> signer's digest
        ///     compare verifier's digest with signer's digest
        /// </code>
        /// </para>
        /// <para>
        /// In contrast, with PSS the signer digests the message, derives a Hash
        /// value (known as H) from the message digest, and builds a formatted
        /// block using H (and other information used in the derivation process).
        /// That block is encrypted using the private key, producing the
        /// signature.
        /// </para>
        /// <para>
        /// To verify a signature, the verifier digests the message to produce
        /// the digest. Next, decrypt the signature to obtain the formatted data.
        /// Parse that data to obtain H (the signer's H, the value derived from
        /// the signer's digest) along with other information needed to perform
        /// the derivation operation. Next, the verifier performs the same
        /// derivation operation using the verifier's digest and information from
        /// the formatted signature (computing the verifier's H), and compares
        /// that result with the value from the parsed block (the signer's H).
        /// <code>
        ///   Signer:
        ///     message --> Digest(message) --> signer's digest
        ///     signer's digest --> derivation(salt, digest) --> signer's H
        ///     signer's H --> format, includes salt and is masked --> formattedData
        ///     formattedData --> RSA(privateKey, formattedData) --> signature
        ///     send message + signature
        ///<br/>
        ///   Verifier:
        ///     message --> Digest(message) --> verifier's digest
        ///     signature --> RSA(publicKey, signature) --> formattedData
        ///     formattedData --> parse, unmask, extract elements --> salt and signer's H
        ///     verifier's digest --> derivation(salt, digest) --> verifier's H
        ///     compare verifier's H with signer's H
        /// </code>
        /// </para>
        /// <para>
        /// This method will perform the derivation operation, which is why the
        /// caller must supply the digest.
        /// </para>
        /// <para>
        /// This method will parse the formatted block, obtaining the information
        /// necessary to perform the derivation operation. It will use the digest
        /// value provided to derive an H value. It will then compare the H value
        /// it derived with the H value from the formatted block. If they are the
        /// same, it will set the <c>isVerified</c> argument to <c>true</c>. If
        /// not, the method will set that value to <c>false</c>.
        /// </para>
        /// <para>
        /// The method will verify that the parsed data matches the PSS
        /// specifications. If not, then it might not derive the H value and
        /// simply set <c>isVerified</c> to <c>false</c>. For example, the PSS
        /// standard requires the most significant bit of the formatted data to
        /// be 0, and the "unmasked" PS to be all 00 bytes.
        /// </para>
        /// <para>
        /// This method will assume the salt length is the same as the digest
        /// length. Although the salt length does not have to be the same as the
        /// digest length, the standard recommends doing so. If the signature you
        /// are verifying does not use the digest length as the salt length, you
        /// cannot use this method. Note that the C# PSS implementation (see the
        /// <c>System.Security.Cryptography.RSA</c> class) uses the digest length
        /// as the salt length exclusively, the same as this method.
        /// </para>
        /// <para>
        /// This method will use the message digest implementations from
        /// <see cref="CryptographyProviders"/>.
        /// </para>
        /// <para>
        /// The method will also return the "M-prime" value followed by the H
        /// value.
        /// <code>
        ///   M-prime || H
        ///     M-prime is (2 * digest length) + 8 bytes long,
        ///     H is digest length bytes long
        /// </code>
        /// M-prime is the value used to derive the H value. It consists of
        /// <code>
        ///   00 00 00 00 00 00 00 00 || digest || salt
        ///   eight 00 bytes, then the digest, then digest length bytes of salt
        /// </code>
        /// Hence, the <c>mPrimeAndH</c> value returned is
        /// <code>
        ///   00 00 00 00 00 00 00 00 || digest || salt || H
        ///             8 bytes           dLen     dLen    dLen
        ///<br/>
        ///   For example, if the digest is SHA-256, then the
        ///   digest length is 32.
        ///<br/>
        ///   00 00 00 00 00 00 00 00 || digest || salt || H
        ///             8 bytes            32       32     32
        /// </code>
        /// If the caller wishes to compute the H value and make the comparison,
        /// the data is made available. The H value is simply the message digest
        /// of M-prime. For example, if the digest algorithm is SHA-256, then
        /// compute
        /// <code>
        ///   SHA-256(M-Prime)
        ///     This will be
        ///   SHA-256(mPrimeAndH, 0, (2 * digestLength) + 8)
        ///     or
        ///   SHA-256(mPrimeAndH, 0, 72) // offset 0, length 72
        /// </code>
        /// Compare the result with the last digest length bytes of
        /// <c>mPrimeAndH</c>.
        /// </para>
        /// <para>
        /// This method only supports signatures 128 or 256 bytes (1024 or 2048
        /// bits) long.
        /// </para>
        /// <para>
        /// It is possible that the method was not able to parse the block enough
        /// to build the <c>mPrimeAndH</c> output. In that case, the return from
        /// the method will be <c>false</c>, <c>isVerified</c> will be
        /// <c>false</c>, and <c>mPrimeAndH</c> will be an empty byte array. It
        /// is also possible that the method was able to build <c>mPrimeAndH</c>
        /// and yet some parsing operation failed so the return is still
        /// <c>false</c>. That is, looking at only <c>mPrimeAndH</c> will not be
        /// sufficient to know what happened.
        /// </para>
        /// <para>
        /// Note that the return value from this method only indicates whether
        /// it was able to parse or not. The return value does not have anything
        /// to do with whether the signature verifies or not. That is, it is
        /// possible the return from this method is <c>true</c> and the
        /// <c>isVerified</c> output argument is set to false. In this case, the
        /// method was able to parse the signature, it's just that the signature
        /// did not verify. In other words, the method successfully completed its
        /// task, there was no error preventing it from parsing and making the H
        /// value comparison. Its task is to determine if the signature verifies
        /// or not. If it determines that the signature did not verify, the
        /// method successfully completed its task, just as determining the
        /// signature did verify is a successful completion of the task.
        /// </para>
        /// </remarks>
        /// <param name="formattedSignature">
        /// The data to parse.
        /// </param>
        /// <param name="digest">
        /// The computed digest, the digest of the message, the data to verify.
        /// </param>
        /// <param name="digestAlgorithm">
        /// The digest algorithm used to compute the digest, it must be one of
        /// the supported algorithms: <c>RsaFormat.Sha1</c>, and so on.
        /// </param>
        /// <param name="mPrimeAndH">
        /// An output argument, a new byte array containing M-prime concatenated
        /// with the H value extracted from the <c>formattedSignature</c>.
        /// </param>
        /// <param name="isVerified">
        /// An output argument, a boolean reporting whether the signature
        /// verified or not.
        /// </param>
        /// <returns>
        /// <c>True</c> if the method is able to parse, <c>false</c> otherwise.
        /// </returns>
        public static bool TryParsePkcs1Pss(ReadOnlySpan<byte> formattedSignature,
                                            ReadOnlySpan<byte> digest,
                                            int digestAlgorithm,
                                            out byte[] mPrimeAndH,
                                            out bool isVerified)
        {
            mPrimeAndH = Array.Empty<byte>();
            isVerified = false;

            if (formattedSignature.Length != 128 && formattedSignature.Length != 256)
            {
                return false;
            }

            // The most significant bit must be 0.
            if ((formattedSignature[0] & 0x80) != 0)
            {
                return false;
            }

            using HashAlgorithm digester = GetHashAlgorithm(digestAlgorithm);

            if (digest.Length * 8 != digester.HashSize)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncorrectDigestLength));
            }

            if (formattedSignature[^1] != TrailerField)
            {
                return false;
            }

            // Copy the data into a byte[], so we can change the data (unmask) and
            // also pass it as an argument to the digester.
            byte[] buffer = formattedSignature.ToArray();

            try
            {
                // the salt is the same length as the digest.
                // PS || 01 || salt || H || BC
                int psLength = buffer.Length - ((2 * digest.Length) + 2);
                int offsetHash = buffer.Length - (digest.Length + 1);

                if (psLength < 0)
                {
                    return false;
                }

                // Run MGF1 to unmask the PS and salt.
                PerformMgf1(buffer, offsetHash, digest.Length, buffer, 0, psLength + digest.Length + 1, digester);

                // It's possible the most significant bit is set if it had been
                // "manually" removed when signing. So remove it here.
                buffer[0] &= 0x7F;

                // Verify that all PS bytes are 0, and that the byte after PS is 01.
                int index = Array.FindIndex<byte>(buffer, p => p != 0);

                if (index != psLength || buffer[psLength] != 1)
                {
                    return false;
                }

                // The buffer contains
                //  PS || 01 || salt || H || BC
                // Build the mPrimeAndH buffer:
                //  00 00 00 00 00 00 00 00 || digest || salt || H
                int mPrimeLength = (2 * digest.Length) + 8;
                mPrimeAndH = new byte[mPrimeLength + digest.Length];
                var mPrimeAsSpan = new Span<byte>(mPrimeAndH);

                // The new byte[] init all bytes to 0, so the first 8 bytes of
                // mPrimeAndH are already 00
                // Copy the digest.
                digest.CopyTo(mPrimeAsSpan[8..]);

                // Now copy the salt
                Array.Copy(buffer, psLength + 1, mPrimeAndH, digest.Length + 8, digest.Length);

                // Copy the H value the signer computed.
                // We're returning the signer's mPrimeAndH, so the caller can
                // compute their own H and compare if they want.
                Array.Copy(buffer, offsetHash, mPrimeAndH, mPrimeLength, digest.Length);

                // Compute Digest of M-prime and compare it to H.
                digester.Initialize();
                _ = digester.TransformFinalBlock(mPrimeAndH, 0, mPrimeLength);
                var digestAsSpan = new Span<byte>(digester.Hash);

                isVerified = digestAsSpan.SequenceEqual(mPrimeAsSpan[mPrimeLength..]);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(buffer);
            }

            return true;
        }

        /// <summary>
        /// Build the input data into a PKCS #1 v1.5 formatted block for
        /// encryption (see RFC 8017).
        /// </summary>
        /// <remarks>
        /// This method will build a new buffer that is <c>keySizeBits</c> long
        /// and contains the following data.
        /// <code>
        ///   00 || 02 || PS || 00 || input data
        ///
        ///   where PS consists of non-zero random bytes
        ///   that is:
        ///
        ///   00 || 02 || non-zero random bytes || 00 || input data
        /// </code>
        /// <para>
        /// This method supports only <c>keySizeBits</c> values that are defined
        /// in this class as <c>KeySizeBits-x-</c>, such as
        /// <c>RsaFormat.KeySizeBits1024</c> (x=1024). You can use one of these
        /// values or simply the actual key size in bits. For example, if the key
        /// size in bits is 1024, then either <c>RsaFormat.KeySizeBits1024</c> or
        /// <c>1024</c> are valid input to this method.
        /// </para>
        /// <para>
        /// The standard specifies that PS must be at least 8 bytes long. Hence,
        /// for a 1024-bit key, the maximum input data length is 117 bytes. For a
        /// 2048-bit key, the maximum input data length is 245 bytes.
        /// <code>
        ///   1024-bit key:
        ///   128-byte buffer: 00 01 || x1 x2 x3 x4 x5 x6 x7 x8 || 00 || 117 bytes
        ///            128 =     2    +             8            +  1  +  117
        ///<br/>
        ///   2048-bit key:
        ///   256-byte buffer: 00 01 || x1 x2 x3 x4 x5 x6 x7 x8 || 00 || 245 bytes
        ///            256 =     2    +             8            +  1  +  245
        /// </code>
        /// </para>
        /// <para>
        /// This method will use the random number generator from
        /// <see cref="CryptographyProviders"/> to generate the random bytes.
        /// </para>
        /// <para>
        /// For example, if the <c>inputData</c> is 32 bytes long, and the
        /// <c>keySizeBits</c> is 1024, the result of this method will look like
        /// the following.
        /// <code>
        ///    00 01 83 62 10 11 98 03 08 80 90 77 43 61 63 23
        ///    34 86 98 07 36 44 56 56 10 01 33 01 24 07 13 20
        ///    72 39 55 89 50 14 46 82 17 43 55 40 36 92 42 06
        ///    06 18 44 86 29 38 36 67 22 91 40 51 16 40 17 18
        ///    56 14 55 25 26 33 21 24 14 08 45 90 85 93 10 77
        ///    49 22 53 88 08 12 10 47 84 20 48 27 29 7A 14 00
        ///    01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10
        ///    11 12 13 14 15 16 17 18 19 1A 1B 1C 1D 1E 1F 20
        /// </code>
        /// </para>
        /// <para>
        /// Because this method creates a new byte array, and it contains
        /// sensitive data, it is a good idea to overwrite the buffer when done
        /// with it.
        /// <code language="csharp">
        ///   CryptographicOperations.ZeroMemory(formattedData);
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="inputData">
        /// The data to format.
        /// </param>
        /// <param name="keySizeBits">
        /// The size of the key used, in bits. This value must be one of the
        /// <c>RsaFormat.KeySizeBits-x-</c> values.
        /// </param>
        /// <returns>
        /// A new byte array containing the formatted data.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The data length is too long for the key size, or the
        /// <c>keySizeBits</c> is not supported.
        /// </exception>
        public static byte[] FormatPkcs1Encrypt(ReadOnlySpan<byte> inputData, int keySizeBits)
        {
            byte[] buffer = GetKeySizeBuffer(keySizeBits);

            // There must be at least 8 bytes of pad, plus 3 extra bytes, the
            // leading 00 02, then the separator between pad and data: 00. If
            // there's too much data, we can't format.
            if (inputData.Length == 0 || inputData.Length > buffer.Length - (Pkcs1MinPadLength + 3))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidCiphertextLength));
            }

            // Generate non-00 random pad bytes.
            int paddingLength = buffer.Length - (inputData.Length + 3);
            using RandomNumberGenerator randomObject = CryptographyProviders.RngCreator();
            randomObject.GetBytes(buffer, 2, paddingLength);
            int index;

            while ((index = Array.FindIndex<byte>(buffer, 2, paddingLength, p => p == 0)) > 0)
            {
                randomObject.GetBytes(buffer, index, 1);
            }

            buffer[0] = Pkcs1LeadByte;
            buffer[1] = Pkcs1EncryptByte;
            buffer[paddingLength + 2] = Pkcs1Separator;

            Span<byte> bufferAsSpan = new Span<byte>(buffer)[(paddingLength + 3)..];
            inputData.CopyTo(bufferAsSpan);

            return buffer;
        }

        /// <summary>
        /// Try to parse the <c>formattedData</c> as a PKCS #1 v1.5 block that
        /// was the result of decryption (see RFC 8017).
        /// </summary>
        /// <remarks>
        /// This method will extract the data from the formatted data. This is
        /// generally the plaintext (the formatted data is the decrypted block).
        /// If it is successful, it will return <c>true</c>. If it cannot extract
        /// the information, it will return <c>false</c>. The caller will likely
        /// decrypt an RSA block, then try to parse it as PKCS #1 v2 OAEP. If
        /// successful, the data is collected. If not, try to parse it as PKCS #1
        /// v1.5. Note that while unlikely, it is possible for an OAEP block to
        /// look like PKCS #1 v1.5. If you don't know which format was used, it is
        /// best to try OAEP first, and if it fails, then try PKCS #1 v1.5.
        /// <para>
        /// The method will verify that the first byte is <c>00</c>, the second
        /// byte is <c>02</c>, and that there are at least 8 padding bytes. It
        /// will then expect to find <c>00</c> and then the data to return.
        /// </para>
        /// <para>
        /// Finally, the method will return a new byte array containing the
        /// actual data portion.
        /// </para>
        /// <para>
        /// Because this method creates a new byte array, and it contains
        /// sensitive data, it is a good idea to overwrite the buffer when done
        /// with it.
        /// <code language="csharp">
        ///   CryptographicOperations.ZeroMemory(outputData);
        /// </code>
        /// </para>
        /// <para>
        /// This method only supports blocks 128 or 256 bytes (1024 or 2048 bits)
        /// long.
        /// </para>
        /// <para>
        /// If any element fails (the length of the <c>formattedData</c> is
        /// not supported, an expected byte is not there, or so on), the method
        /// will return <c>false</c>. If there is an error, the method might set
        /// the <c>outputData</c> argument to an empty array, or it might contain
        /// the purported data. If the return is <c>false</c> and there is data
        /// in <c>outputData</c>, that data is meaningless.
        /// </para>
        /// </remarks>
        /// <param name="formattedData">
        /// The data to parse.
        /// </param>
        /// <param name="outputData">
        /// An output argument, the method will return a new byte array
        /// containing the unpadded data portion of the block.
        /// </param>
        /// <returns>
        /// <c>True</c> if the method is able to parse, <c>false</c> otherwise.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool TryParsePkcs1Decrypt(ReadOnlySpan<byte> formattedData, out byte[] outputData)
        {
            // Return this buffer if there is any error.
            outputData = Array.Empty<byte>();

            if (formattedData.Length != 128 && formattedData.Length != 256)
            {
                return false;
            }

            // Make all checks, even if a previous one failed, to help avoid
            // timing attacks.

            // We expect to find 00 02 Pad Pad ... Pad 00 data
            // With at least 8 Pad bytes. A pad byte must be non-zero, so search
            // for the first 00 byte.
            byte errorFlag = formattedData[0];
            errorFlag |= (byte)(formattedData[1] ^ Pkcs1EncryptByte);

            // Find the index of the first 00 byte, start at the end so that we
            // check every byte every time (to help avoid timing attacks).
            int index = formattedData.Length - 2;
            int startIndex = 0;

            for (; index >= 2; index--)
            {
                if (formattedData[index] == 0)
                {
                    startIndex = index + 1;
                }
            }

            // If there was no zero byte (startIndex will be 0), or only the last
            // byte was 0 (startIndex will be 0), or if the zero byte does not
            // allow for more than 8 pad bytes (startIndex will be < 10), this is
            // an error.
            if (startIndex < Pkcs1MinPadLength + 2)
            {
                errorFlag |= 1;
            }

            // Copy 32 bytes in any error case.
            if (errorFlag == 0)
            {
                outputData = formattedData[startIndex..].ToArray();
            }

            return errorFlag == 0;
        }

        /// <summary>
        /// Build the input data into a PKCS #1 v2 OAEP formatted block for
        /// encryption (see RFC 8017).
        /// </summary>
        /// <remarks>
        /// The OAEP (Optimal Asymmetric Encryption Padding) operation has a
        /// number of parameters: hash function, mask generating function, and
        /// label (pSource). The caller supplies the <c>digestAlgorithm</c> as
        /// the hash function, this method will use MGF1 as the mask generating
        /// function, and the empty label.
        /// <para>
        /// This method will build a new buffer that is <c>keySizeBits</c> long
        /// and contains the following data.
        /// <code>
        ///   00 || masked seed || masked DB
        /// </code>
        /// </para>
        /// <para>
        /// The seed is simply digest length random bytes. The masked seed is the
        /// same length.
        /// </para>
        /// <para>
        /// The DB (data block) is originally
        /// <code>
        ///   lHash || PS || 01 || input data
        /// </code>
        /// The <c>lHash</c> value is the digest of the label. The standard
        /// specifies the default label is the empty string. This method will
        /// only be able to build an <c>lHash</c> from the default empty string.
        /// The PS (padding string) is all 00 bytes. Its length is
        /// <code>
        ///   length(PS) = block size - (input data length + (2 * digest length) + 2)
        ///<br/>
        ///   For example, if the block size is 128 (1024-bit RSA key), the input
        ///   data is 16 bytes, and the digest algorithm is SHA-256, then
        ///<br/>
        ///   length(PS) = 128 - (16 + (2 * 32) + 2)
        ///              = 128 - 82 = 46
        /// </code>
        /// The standard allows a PS of length 0.
        /// </para>
        /// <para>
        /// Another way to look at this is the maximum input data length based on
        /// the block size and digest length.
        /// <code>
        ///   max input length = block size - ((2 * digestLen) + 2)
        ///<br/>
        ///   For example, if the block size is 128 (1024-bit RSA key), and the
        ///   digest algorithm is SHA-256, then
        ///<br/>
        ///   max input length = 128 - ((2 * 32) + 2)
        ///                    = 128 - 66 = 62.
        ///<br/>
        ///   With a block size of 128 and digest algorithm of SHA-384,
        ///<br/>
        ///   max input length = 128 - ((2 * 48) + 2)
        ///                    = 128 - 98 = 30.
        ///<br/>
        ///   Note that SHA-512 is simply not possible with a block size of 128.
        ///<br/>
        ///   max input length = 128 - ((2 * 64) + 2)
        ///                    = 128 - 130 = -2.
        /// </code>
        /// </para>
        /// <para>
        /// If the input data length and digest algorithm make a block too big
        /// for the <c>keySizeBits</c>, this method will throw an exception.
        /// </para>
        /// <para>
        /// This method supports only <c>keySizeBits</c> values that are defined
        /// in this class as <c>KeySizeBits-x-</c>, such as
        /// <c>RsaFormat.KeySizeBits1024</c> (x=1024). You can use one of these
        /// values or simply the actual key size in bits. For example, if the key
        /// size in bits is 1024, then either <c>RsaFormat.KeySizeBits1024</c> or
        /// <c>1024</c> are valid input to this method.
        /// </para>
        /// <para>
        /// This method will use the random number generator and message digest
        /// implementations from <see cref="CryptographyProviders"/>.
        /// </para>
        /// <para>
        /// Because this method creates a new byte array, and it contains
        /// sensitive data, it is a good idea to overwrite the buffer when done
        /// with it.
        /// <code language="csharp">
        ///   CryptographicOperations.ZeroMemory(formattedData);
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="inputData">
        /// The data to format.
        /// </param>
        /// <param name="digestAlgorithm">
        /// The algorithm to use in the OAEP operations. It must be one of the
        /// digest algorithms defined in this class: <c>RsaFormat.Sha1</c>,
        /// <c>RsaFormat.Sha256</c>, and so on.
        /// </param>
        /// <param name="keySizeBits">
        /// The size of the key used, in bits. This value must be either
        /// <c>1024</c> or <c>2048</c>
        /// </param>
        /// <returns>
        /// A new byte array containing the formatted data.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The data length is too long for the key size, or the
        /// <c>keySizeBits</c> is not supported.
        /// </exception>
        public static byte[] FormatPkcs1Oaep(ReadOnlySpan<byte> inputData, int digestAlgorithm, int keySizeBits)
        {
            byte[] buffer = GetKeySizeBuffer(keySizeBits);

            var bufferAsSpan = new Span<byte>(buffer);

            using HashAlgorithm digester = GetHashAlgorithm(digestAlgorithm);

            int digestLength = digester.HashSize / 8;

            if (inputData.Length == 0 || inputData.Length > buffer.Length - ((2 * digestLength) + 2))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidCiphertextLength));
            }

            // Build the buffer:
            //  00 || seed || lHash || PS || 01 || input data
            // Beginning with lHash is the DB
            //  DB = lHash || PS || 01 || input data
            using RandomNumberGenerator randomObject = CryptographyProviders.RngCreator();

            // seed
            randomObject.GetBytes(buffer, 1, digestLength);

            // lHash = digest of empty string.
            _ = digester.TransformFinalBlock(buffer, 0, 0);
            Array.Copy(digester.Hash, 0, buffer, digestLength + 1, digestLength);

            // 01
            buffer[^(inputData.Length + 1)] = 1;
            inputData.CopyTo(bufferAsSpan[(buffer.Length - inputData.Length)..]);

            // Use the seed to mask the DB.
            PerformMgf1(buffer, 1, digestLength, buffer, digestLength + 1, buffer.Length - (digestLength + 1),
                digester);

            // Use the masked DB to mask the seed.
            PerformMgf1(buffer, digestLength + 1, buffer.Length - (digestLength + 1), buffer, 1, digestLength,
                digester);

            return buffer;
        }

        /// <summary>
        /// Try to parse the <c>formattedData</c> as a PKCS #1 v2 OAEP block that
        /// was the result of decryption (see RFC 8017).
        /// </summary>
        /// <remarks>
        /// This method will extract the data from the formatted data and return
        /// it in a new byte array. This is generally the plaintext (the
        /// formatted data is the decrypted block). If it is successful, it will
        /// return <c>true</c>. If it cannot extract the information, it will
        /// return <c>false</c>.
        /// <para>
        /// The method will verify that the first byte is <c>00</c>, then it will
        /// perform MGF1 using the specified digest algorithm on the masked DB
        /// (data block) to unmask the salt, and then perform MGF1 on the salt to
        /// unmaskk the DB. It will then verify that the lHash and PS are correct.
        /// <code>
        ///   00 || masked salt || masked DB
        ///     MGF1(maskedDB)
        ///   00 || salt || masked DB
        ///     MGF1(salt)
        ///   00 || salt || lHash || PS || 01 || output data
        /// </code>
        /// </para>
        /// <para>
        /// The caller supplies the digest algorithm to use in the MGF1. It must
        /// be one of the supported values: <c>RsaFormat.Sha1</c>, <c>Sha256</c>,
        /// or so on.
        /// </para>
        /// <para>
        /// This method will use the message digest implementations from
        /// <see cref="CryptographyProviders"/>.
        /// </para>
        /// <para>
        /// The <c>lHash</c> value is the digest of the label (pSource). The
        /// standard specifies the default label is the empty string. This method
        /// will only be able to build an <c>lHash</c> from the default empty
        /// string.
        /// </para>
        /// <para>
        /// Finally, the method will return a new byte array containing the
        /// actual data portion.
        /// </para>
        /// <para>
        /// Because this method creates a new byte array, and it contains
        /// sensitive data, it is a good idea to overwrite the buffer when done
        /// with it.
        /// <code language="csharp">
        ///   CryptographicOperations.ZeroMemory(outputData);
        /// </code>
        /// </para>
        /// <para>
        /// This method only supports blocks 128 or 256 bytes (1024 or 2048 bits)
        /// long.
        /// </para>
        /// <para>
        /// If any element fails (the length of the <c>formattedData</c> is
        /// not supported, an expected byte is not there, or so on), the method
        /// will return <c>false</c>. If there is an error, the method might set
        /// the <c>outputData</c> argument to an empty array, or it might contain
        /// the purported data. If the return is <c>false</c> and there is data
        /// in <c>outputData</c>, that data is meaningless.
        /// </para>
        /// </remarks>
        /// <param name="formattedData">
        /// The data to parse.
        /// </param>
        /// <param name="digestAlgorithm">
        /// The algorithm to use in the OAEP operations. It must be one of the
        /// digest algorithms defined in this class: <c>RsaFormat.Sha1</c>,
        /// <c>RsaFormat.Sha256</c>, and so on.
        /// </param>
        /// <param name="outputData">
        /// An output argument, a new buffer containing the data portion of the
        /// block.
        /// </param>
        /// <returns>
        /// <c>True</c> if the method is able to parse, <c>false</c> otherwise.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool TryParsePkcs1Oaep(ReadOnlySpan<byte> formattedData,
                                             int digestAlgorithm,
                                             out byte[] outputData)
        {
            outputData = Array.Empty<byte>();

            if (formattedData.Length == 128)
            {
                if (digestAlgorithm == Sha512)
                {
                    return false;
                }
            }
            else if (formattedData.Length != 256)
            {
                return false;
            }

            using HashAlgorithm digester = GetHashAlgorithm(digestAlgorithm);

            int digestLength = digester.HashSize / 8;

            // Run all checks, even if a previous one failed, to help avoid
            // timing attacks.

            // The most significant byte must be 0.
            int errorCount = (int)formattedData[0];

            // Copy the data into a byte[], so we can change the data (unmask) and
            // also pass it as an argument to the digester.
            byte[] buffer = formattedData.ToArray();

            try
            {
                // Use the masked DB to unmask the seed.
                PerformMgf1(buffer, digestLength + 1, buffer.Length - (digestLength + 1), buffer, 1, digestLength,
                    digester);

                // Use the seed to unmask the DB.
                PerformMgf1(buffer, 1, digestLength, buffer, digestLength + 1, buffer.Length - (digestLength + 1),
                    digester);

                // Verify the DB
                //  block = 00 || salt || DB
                //  DB = lHash || PS || 01 || input data

                // lHash = digest of empty string.
                digester.Initialize();
                _ = digester.TransformFinalBlock(buffer, 0, 0);
                int index = 0;

                for (; index < digestLength; index++)
                {
                    errorCount += (int)(digester.Hash[index] ^ buffer[index + digestLength + 1]);
                }

                // Find the first byte after the PS, make sure it is 01.
                index = (2 * digestLength) + 1;
                index = Array.FindIndex<byte>(buffer, index, buffer.Length - index, p => p != 0);

                // If there is no non-zero byte, or it is the last byte, this is
                // an error.
                if (index < 0)
                {
                    errorCount++;
                }
                else if (buffer[index] != 1)
                {
                    errorCount++;
                }

                // The remaining data is the data to return.
                if (errorCount == 0)
                {
                    outputData = new byte[buffer.Length - (index + 1)];
                    Array.Copy(buffer, index + 1, outputData, 0, buffer.Length - (index + 1));

                    return true;
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(buffer);
            }

            return false;
        }

        // Build the DER of DigestInfo for the given digest, place it at the end
        // of the buffer. Return the length.
        // After this call the buffer will be
        //   buffer.Length - digestInfoLen bytes undisturbed || digestInfo
        // This method assumes that the buffer is either 128 or 256 bytes long,
        // so any input will work. Hence, it doesn't check to make sure the
        // buffer is big enough.
        private static int BuildDigestInfo(ReadOnlySpan<byte> digest, int digestAlgorithm, Span<byte> buffer)
        {
            byte[] oid;
            int digestLength;

            switch (digestAlgorithm)
            {
                case Sha1:
                    oid = new byte[] { 0x2b, 0x0e, 0x03, 0x02, 0x1a };
                    digestLength = Sha1Length;

                    break;

                case Sha256:
                    oid = new byte[] { 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01 };
                    digestLength = Sha256Length;

                    break;

                case Sha384:
                    oid = new byte[] { 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x02 };
                    digestLength = Sha384Length;

                    break;

                case Sha512:
                    oid = new byte[] { 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x03 };
                    digestLength = Sha512Length;

                    break;

                default:
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.UnsupportedAlgorithm));
            }

            // The longest digest length supported is 64 bytes (0x40). We also
            // support only OIDs with NULL parameters. Working backwards, the
            // length of the outer SEQUENCE will be at most 81, which is 0x51,
            // which is a single octet in DER. Hence, we can count the size of
            // the DER encoding, knowing all length octets will be 1 byte long.
            //   30 len
            //      30 len
            //         06 len
            //            OID
            //         05 00
            //      04 digestLength
            //         digest
            int totalLength = 10 + oid.Length + digestLength;
            Span<byte> output = buffer[^totalLength..];

            var tlvWriter = new TlvWriter();

            using (tlvWriter.WriteNestedTlv(SequenceTag))
            {
                using (tlvWriter.WriteNestedTlv(SequenceTag))
                {
                    tlvWriter.WriteValue(OidTag, oid);
                    tlvWriter.WriteValue(NullTag, ReadOnlySpan<byte>.Empty);
                }

                tlvWriter.WriteValue(OctetTag, digest);
            }

            bool isValid = tlvWriter.TryEncode(output, out int outputLength);

            // If the digest.Length is not digestLength, either isValid will be
            // false or the outputLength won't be totalLength. So this is where
            // digest.Length is checked.
            if (isValid == false || outputLength != totalLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncorrectDigestLength));
            }

            return totalLength;
        }

        // Perform one of the TlvReader.TryRead methods.
        // This will check isValid. If it is false, don't do anything, just
        // return false.
        // Then perform the readType, either Nested or Value.
        // Then if the NoMoreData bit is set in readType, verify there is no data
        // left in the reader.
        // If this is Nested, set the output arg newReader to the newly-created
        // TlvReader. If this is Value, set the output arg value to the new
        // ReadOnlyMemory<byte>.
        private static bool TryReadDer(bool isValid,
                                       int readType,
                                       int expectedTag,
                                       TlvReader reader,
                                       out TlvReader newReader,
                                       out ReadOnlyMemory<byte> value)
        {
            value = ReadOnlyMemory<byte>.Empty;
            newReader = new TlvReader(value);
            bool returnValue = isValid;

            if (isValid)
            {
                if ((readType & ReadNested) != 0)
                {
                    returnValue = reader.TryReadNestedTlv(out newReader, expectedTag);
                }
                else
                {
                    returnValue = reader.TryReadValue(out value, expectedTag);
                }

                if (returnValue && (readType & NoMoreData) != 0)
                {
                    // We want to make sure all the data has been read. We
                    // don't want a byte or more to be dangling at then end
                    // of this construction. So if HasData is true, there is
                    // unread data and that is incorrect. So HasData being
                    // true means it is not valid, so set the returnValue to
                    // false. If HasData is false, then the construction is
                    // valid, so set the returnValue to true.
                    returnValue = reader.HasData == false;
                }
            }

            return returnValue;
        }

        // Map the oid to an algorithm.
        // If isValid is false, just return false, don't do anything.
        // Otherwise, make sure the OID is one we support, and if it is, make
        // sure the params are what we support, and make sure the message digest
        // is the correct length.
        private static bool TryParseOid(bool isValid,
                                        ReadOnlyMemory<byte> oid,
                                        ReadOnlyMemory<byte> oidParams,
                                        ReadOnlyMemory<byte> digest,
                                        out int algorithm)
        {
            algorithm = 0;
            int digestLength = Sha1Length;
            bool returnValue = isValid;

            if (isValid)
            {
                byte[] supportedOid = Array.Empty<byte>();

                switch (oid.Length)
                {
                    default:
                        returnValue = false;

                        break;

                    case Sha1OidLength:
                        algorithm = Sha1;
                        supportedOid = new byte[] { 0x2b, 0x0e, 0x03, 0x02, 0x1a };

                        break;

                    case Sha2OidLength:
                        // Multiple algorithms are of length 9, and currently
                        // they all have the same OID, except for the last byte.
                        // So look at the last byte to figure out which specific
                        // algorithm is represented.
                        supportedOid = new byte[] { 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, oid.Span[^1] };

                        switch (oid.Span[^1])
                        {
                            default:
                                returnValue = false;

                                break;

                            case Sha256OidByte:
                                algorithm = Sha256;
                                digestLength = Sha256Length;

                                break;

                            case Sha384OidByte:
                                algorithm = Sha384;
                                digestLength = Sha384Length;

                                break;

                            case Sha512OidByte:
                                algorithm = Sha512;
                                digestLength = Sha512Length;

                                break;
                        }

                        break;
                }

                bool sameOid = oid.Span.SequenceEqual(new Span<byte>(supportedOid));

                if (sameOid == false || digest.Length != digestLength || oidParams.Length != 0)
                {
                    returnValue = false;
                }
            }

            return returnValue;
        }

        // Perform MGF1.
        // MGF 1 is
        //  Digest(seed || counter) ^ target
        // The digest produces digestLength bytes.
        // XOR the resulting bytes with the target (mask the target).
        // If the target is fully masked, we're done.
        // If not, increment the counter, compute another result, and mask.
        // The counter is simply
        //   00 00 00 00
        //   00 00 00 01
        //   00 00 00 02
        //    and so on
        // Because we will be masking for only 1024- or 2048-bit blocks, and the
        // smallest digest algorithm we support is SHA-1, we have an upper limit
        // on the number of iterations as 13. Hence, we know we will never need a
        // counter of
        //   00 00 01 00
        private static void PerformMgf1(byte[] seed,
                                        int offsetSeed,
                                        int seedLength,
                                        byte[] target,
                                        int offsetTarget,
                                        int targetLength,
                                        HashAlgorithm digester)
        {
            int bytesRemaining = targetLength;
            int offset = offsetTarget;
            int digestLength = digester.HashSize / 8;

            byte[] counter = new byte[4];

            while (bytesRemaining > 0)
            {
                int xorCount = bytesRemaining;

                if (digestLength <= bytesRemaining)
                {
                    xorCount = digestLength;
                }

                digester.Initialize();
                _ = digester.TransformBlock(seed, offsetSeed, seedLength, null, 0);
                _ = digester.TransformFinalBlock(counter, 0, 4);

                for (int index = 0; index < xorCount; index++)
                {
                    target[offset + index] ^= digester.Hash[index];
                }

                bytesRemaining -= xorCount;
                offset += xorCount;
                counter[3]++;
            }
        }
        
        private static byte[] GetKeySizeBuffer(int keySizeBits)
        {
            switch(keySizeBits)
            {
                case KeySizeBits1024:
                case KeySizeBits2048:
                case KeySizeBits3072:
                case KeySizeBits4096:
                    return new byte[keySizeBits / 8];
                default:
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.IncorrectRsaKeyLength));
            }
        }

        private static HashAlgorithm GetHashAlgorithm(int digestAlgorithm) =>
            digestAlgorithm switch
            {
                Sha1 => CryptographyProviders.Sha1Creator(),
                Sha256 => CryptographyProviders.Sha256Creator(),
                Sha384 => CryptographyProviders.Sha384Creator(),
                Sha512 => CryptographyProviders.Sha512Creator(),
                _ => throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.UnsupportedAlgorithm)),
            };
    }
}
