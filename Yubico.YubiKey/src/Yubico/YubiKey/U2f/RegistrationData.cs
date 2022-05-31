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
using System.Security.Cryptography.X509Certificates;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.U2f
{
    /// <summary>
    /// Represents a single U2F registration.
    /// </summary>
    /// <remarks>
    /// This represents the registration data returned by the YubiKey when registering a new U2F credential. The information
    /// stored in this structure can be sent back to the relying party to store for future validation (authentication)
    /// attempts.
    /// </remarks>
    public class RegistrationData
    {
        private const int EcP256PublicKeyCoordinateLength = 32;
        private const int EncodedEcPublicKeyLength = 1 +  (2 * EcP256PublicKeyCoordinateLength);
        private const int AppIdLength = 32;
        private const int ClientDataHashLength = 32;
        private const int KeyHandleOffset = 1 + AppIdLength + ClientDataHashLength;
        private const int EcPublicKeyTag = 0x04;

        private ECPoint _userPublicKey;

        /// <summary>
        /// The ECDSA public key for this user. Each coordinate must be 32 bytes and the point must be on the P256 curve.
        /// </summary>
        /// <remarks>
        /// This is a public key for ECDSA using the NIST P256 curve and SHA256,
        /// per the FIDO specifications.
        /// </remarks>
        public ECPoint UserPublicKey
        {
            get => _userPublicKey;
            set
            {
                if (value.X.Length != EcP256PublicKeyCoordinateLength)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidPropertyLength,
                            nameof(value), EcP256PublicKeyCoordinateLength, value.X.Length));
                }

                if (value.Y.Length != EcP256PublicKeyCoordinateLength)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidPropertyLength,
                            nameof(value), EcP256PublicKeyCoordinateLength, value.Y.Length));
                }

                _userPublicKey = value;
            }
        }

        /// <summary>
        /// The key handle of the created public key credential.
        /// </summary>
        public ReadOnlyMemory<byte> KeyHandle { get; set; }

        public X509Certificate2 AttestationCertificate { get; set; }

        public ReadOnlyMemory<byte> Signature { get; set; }

        public RegistrationData()
        {
            AttestationCertificate = new X509Certificate2();
        }

        public RegistrationData(
            ECPoint userPublicKey,
            ReadOnlyMemory<byte> keyHandle,
            X509Certificate2 attestationCertificate,
            ReadOnlyMemory<byte> signature)
        {
            UserPublicKey = userPublicKey;
            KeyHandle = keyHandle;
            AttestationCertificate = attestationCertificate;
            Signature = signature;
        }

        /// <summary>
        /// Returns whether the signature in this registration was valid for the given client parameters.
        /// </summary>
        /// <remarks>
        /// Specifically, this checks that the <see cref="Signature"/> property contains
        /// a signature over the registration data using the public key in the <see cref="AttestationCertificate"/>.
        /// </remarks>
        /// <param name="clientDataHash">The original clientDataHash that was provided to create this registration.</param>
        /// <param name="applicationId">The original appId that was provided to create this registration.</param>
        /// <returns></returns>
        public bool VerifySignature(ReadOnlySpan<byte> applicationId, ReadOnlySpan<byte> clientDataHash)
        {
            if (clientDataHash.Length != ClientDataHashLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPropertyLength,
                        nameof(clientDataHash), 32, clientDataHash.Length));
            }

            if (applicationId.Length != AppIdLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPropertyLength,
                        nameof(applicationId), 32, applicationId.Length));
            }

            using ECDsa ecdsa = AttestationCertificate.GetECDsaPublicKey();

            int dataToVerifyLength = 1 + AppIdLength + ClientDataHashLength + KeyHandle.Length + EncodedEcPublicKeyLength;
            byte[] dataToVerify = new byte[dataToVerifyLength];

            applicationId.CopyTo(dataToVerify.AsSpan(1));
            clientDataHash.CopyTo(dataToVerify.AsSpan(1 + AppIdLength));

            KeyHandle.ToArray().CopyTo(dataToVerify.AsSpan(KeyHandleOffset));

            int userPublicKeyOffset = KeyHandleOffset + KeyHandle.Length;
            dataToVerify[userPublicKeyOffset] = EcPublicKeyTag;
            UserPublicKey.X.CopyTo(dataToVerify.AsSpan(userPublicKeyOffset + 1));
            UserPublicKey.Y.CopyTo(dataToVerify.AsSpan(userPublicKeyOffset + EcP256PublicKeyCoordinateLength + 1));

            return ecdsa.VerifyData(dataToVerify, ConvertDerToIeeeP1393(Signature, EcP256PublicKeyCoordinateLength), HashAlgorithmName.SHA256);
        }

        /// <summary>
        /// Converts DER-endcoded ECDSA signatures to the 'raw' IEEE P1393 format expected by the runtime.
        /// </summary>
        // The input will be
        //   30 length
        //      02 length  rValue
        //      02 length  sValue
        // The result will be
        //    rValue || sValue
        // where each value must be exactly valueLength bytes long.
        // If an input value is > valueLength, strip leading byte(s) (it should
        // be no more than one 00 leading byte).
        // If an input value is < valueLength, prepend 00 bytes to the result.
        private static byte[] ConvertDerToIeeeP1393(ReadOnlyMemory<byte> data, int valueLength)
        {
            var tlvReader = new TlvReader(data);
            tlvReader = tlvReader.ReadNestedTlv(0x30);

            byte[] result = new byte[2 * valueLength];

            ReadOnlyMemory<byte> value = tlvReader.ReadValue(0x02);
            CopySignatureValue(value, result, 0, valueLength);

            value = tlvReader.ReadValue(0x02);
            CopySignatureValue(value, result, valueLength, valueLength);

            return result;
        }

        // Copy from source to destination.
        // Copy length bytes.
        // If the source is too long, skip the first byte(s) of source.
        // If the source is too short, skip the first byte(s) of destination.
        // Copy the bytes into destination beginning at offset.
        private static void CopySignatureValue(
            ReadOnlyMemory<byte> source,
            Memory<byte> destination,
            int offset,
            int length)
        {
            int offsetSource = 0;
            int offsetDestination = offset;
            if (source.Length > length)
            {
                offsetSource = source.Length - length;
            }
            else if (source.Length < length)
            {
                offsetDestination += length - source.Length;
            }

            _ = source.Slice(offsetSource).TryCopyTo(destination.Slice(offsetDestination));
        }

    }
}
