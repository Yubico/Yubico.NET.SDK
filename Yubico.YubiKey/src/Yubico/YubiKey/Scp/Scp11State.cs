// Copyright 2024 Yubico AB
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Yubico.Core.Cryptography;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Scp.Commands;

namespace Yubico.YubiKey.Scp
{
    /// <summary>
    /// Manages the state for Secure Channel Protocol 11 (SCP11) communication with a YubiKey.
    /// This class handles key agreement, authentication, and secure messaging between the host
    /// and the YubiKey using AES-128 encryption and MAC operations. It supports different SCP11
    /// variants (11a, 11b, 11c) for establishing secure channels with PIV and other smart card
    /// applications.
    /// </summary>
    internal class Scp11State : ScpState
    {
        private const int ReceiptTag = 0x86;
        private const int EckaTag = 0x5F49;
        private const int KeyAgreementTag = 0xA6;

        /// <summary>
        /// Initializes a new instance of the <see cref="Scp11State"/> class with the specified session keys and receipt.
        /// </summary>
        /// <param name="sessionKeys">The session keys for secure channel communication.</param>
        /// <param name="receipt">The receipt data used for verification.</param>
        public Scp11State(SessionKeys sessionKeys, Memory<byte> receipt)
            : base(sessionKeys, receipt)
        {
        }

        /// <summary>
        /// Creates a new SCP11 secure channel state by performing key agreement and authentication with the YubiKey.
        /// </summary>
        /// <param name="pipeline">The APDU pipeline for communication with the YubiKey.</param>
        /// <param name="keyParameters">The key parameters required for SCP11 authentication.</param>
        /// <returns>A new instance of <see cref="Scp11State"/> configured for secure channel communication.</returns>
        /// <exception cref="ArgumentException">Thrown when key parameters are invalid or incompatible.</exception>
        /// <exception cref="SecureChannelException">Thrown when secure channel establishment fails.</exception>
        internal static Scp11State CreateScpState(
            IApduTransform pipeline,
            Scp11KeyParameters keyParameters)
        {
            // Perform Security Operation, if needed (for Scp11a and Scp11c)
            if (keyParameters.KeyReference.Id == ScpKeyIds.Scp11A || keyParameters.KeyReference.Id == ScpKeyIds.Scp11C)
            {
                PerformSecurityOperation(pipeline, keyParameters);
            }

            var sdEckaPkParams = keyParameters.PkSdEcka.Parameters;

            // Generate a public and private key using the supplied curve
            var ekpOceEcka = CryptographyProviders.EcdhPrimitivesCreator()
                .GenerateKeyPair(sdEckaPkParams.Curve);

            // Create an encoded point of the ephemeral public key to send to the Yubikey
            byte[] ephemeralPublicKeyEncodedPointOceEcka = new byte[65];
            ephemeralPublicKeyEncodedPointOceEcka[0] = 0x04; // Coding Identifier Byte
            ekpOceEcka.Q.X.CopyTo(ephemeralPublicKeyEncodedPointOceEcka, 1);
            ekpOceEcka.Q.Y.CopyTo(ephemeralPublicKeyEncodedPointOceEcka, 33);

            // GPC v2.3 Amendment F (SCP11) v1.4 §7.6.2.3
            byte[] keyUsage = { 0x3C }; // AUTHENTICATED | C_MAC | C_DECRYPTION | R_MAC | R_ENCRYPTION
            byte[] keyType = { 0x88 }; // AES
            byte[] keyLen = { 16 }; // 128-bit
            byte[] keyIdentifier = { 0x11, GetScpIdentifierByte(keyParameters.KeyReference) };

            // Construct the host authentication data (payload)
            byte[] hostAuthenticateTlvEncodedData = TlvObjects.EncodeMany(
                new TlvObject(
                    KeyAgreementTag,
                    TlvObjects.EncodeMany(
                        new TlvObject(0x90, keyIdentifier),
                        new TlvObject(0x95, keyUsage),
                        new TlvObject(0x80, keyType),
                        new TlvObject(0x81, keyLen)
                        )),
                new TlvObject(EckaTag, ephemeralPublicKeyEncodedPointOceEcka)
                );

            // Construct the host authentication command
            var authenticateCommand = keyParameters.KeyReference.Id == ScpKeyIds.Scp11B
                ? new InternalAuthenticateCommand(
                    keyParameters.KeyReference.VersionNumber, keyParameters.KeyReference.Id,
                    hostAuthenticateTlvEncodedData) as IYubiKeyCommand<ScpResponse>
                : new ExternalAuthenticateCommand(
                    keyParameters.KeyReference.VersionNumber, keyParameters.KeyReference.Id,
                    hostAuthenticateTlvEncodedData) as IYubiKeyCommand<ScpResponse>;
            
            // Issue the host authentication command
            var authenticateResponseApdu = pipeline.Invoke(
                authenticateCommand.CreateCommandApdu(), authenticateCommand.GetType(), typeof(ScpResponse));

            var authenticateResponse = authenticateCommand.CreateResponseForApdu(authenticateResponseApdu);
            authenticateResponse.ThrowIfFailed(
                $"Error when performing {authenticateCommand.GetType().Name}: {authenticateResponse.StatusMessage}");

            // Decode the response as a TLV list
            var authenticateResponseTlvs = TlvObjects.DecodeList(authenticateResponseApdu.Data.Span);

            // Extract the ephemeral public key from the response
            var epkSdEckaTlv = authenticateResponseTlvs[0];
            var epkSdEckaTlvEncodedData = epkSdEckaTlv.GetBytes();
            var sdReceipt = TlvObjects.UnpackValue(
                ReceiptTag,
                authenticateResponseTlvs[1].GetBytes().Span); // Yubikey X963KDF Receipt to match with our own X963KDF

            // Decide which key to use for key agreement
            var skOceEcka =
                keyParameters.SkOceEcka?.Parameters ?? // If set, we will use this for SCP11A and SCP11C. 
                ekpOceEcka; // Otherwise, just use the newly created ephemeral key for SCP11b.

            // Perform key agreement
            var (encryptionKey, macKey, rMacKey, dekKey)
                = GetX963KDFKeyAgreementKeys(
                    skOceEcka.Curve,
                    sdEckaPkParams,
                    ekpOceEcka,
                    skOceEcka,
                    sdReceipt,
                    epkSdEckaTlvEncodedData,
                    hostAuthenticateTlvEncodedData,
                    keyUsage,
                    keyType,
                    keyLen);
                    
            // Create the session keys
            var sessionKeys = new SessionKeys(
                macKey,
                encryptionKey,
                rMacKey,
                dekKey
                );

            return new Scp11State(sessionKeys, sdReceipt.ToArray());
        }

        /// <summary>
        /// Performs X9.63 Key Derivation Function (KDF) to generate session keys for the secure channel.
        /// </summary>
        /// <param name="curve">The elliptic curve used for key agreement.</param>
        /// <param name="pkSdEcka">The YubiKey's public key parameters.</param>
        /// <param name="eskOceEcka">The host's ephemeral key pair parameters.</param>
        /// <param name="skOceEcka">The host's static key pair parameters.</param>
        /// <param name="sdReceipt">The receipt computed by the YubiKey.</param>
        /// <param name="epkSdEckaTlvEncodedData">The YubiKey's ephemeral public key in TLV format.</param>
        /// <param name="hostAuthenticateTlvEncodedData">The host authentication data in TLV format.</param>
        /// <param name="keyUsage">The intended usage of the derived keys.</param>
        /// <param name="keyType">The type of keys to be derived.</param>
        /// <param name="keyLen">The length of keys to be derived.</param>
        /// <returns>A tuple containing the encryption, MAC, R-MAC, and DEK keys.</returns>
        /// <exception cref="ArgumentException">Thrown when the curves of the provided keys do not match.</exception>
        /// <exception cref="SecureChannelException">Thrown when key agreement receipt verification fails.</exception>
        private static (Memory<byte> encryptionKey, Memory<byte> macKey, Memory<byte> rMacKey, Memory<byte> dekKey)
            GetX963KDFKeyAgreementKeys(
            ECCurve curve, // The curve being used for the key agreement
            ECParameters pkSdEcka, // Yubikey Public Key
            ECParameters eskOceEcka, // Host Ephemeral Private Key
            ECParameters skOceEcka, // Host Private Key
            ReadOnlyMemory<byte> sdReceipt, // The receipt computed on the Yubikey
            ReadOnlyMemory<byte> epkSdEckaTlvEncodedData, // Yubikey Ephemeral Public Key as Tlv Raw Data
            ReadOnlyMemory<byte> hostAuthenticateTlvEncodedData,
            ReadOnlyMemory<byte> keyUsage, // The shared key usage
            ReadOnlyMemory<byte> keyType, // The shared key type
            ReadOnlyMemory<byte> keyLen) // The shared key length
        {
            bool allKeysAreSameCurve = new[]
            {
                pkSdEcka.Curve, // Yubikey Public Key
                eskOceEcka.Curve, // Host Ephemeral Private Key
                skOceEcka.Curve // Host Private Key
            }.All(c => c.Oid.Value == curve.Oid.Value);

            if (!allKeysAreSameCurve)
            {
                throw new ArgumentException("All curves must be the same");
            }

            // Compute key agreement for:
            // Yubikey Ephemeral Public Key + Host Ephemeral Private Key
            var ecdhObject = CryptographyProviders.EcdhPrimitivesCreator();
            var epkSdEcka = ExtractPublicKeyEcParameters(
                epkSdEckaTlvEncodedData, skOceEcka.Curve); // Yubikey Ephemeral Public Key 

            byte[] keyAgreementFirst = ecdhObject.ComputeSharedSecret(epkSdEcka, eskOceEcka.D);

            // Compute key agreement for:
            // Yubikey Public Key + Host Private Key
            byte[] keyAgreementSecond = ecdhObject.ComputeSharedSecret(pkSdEcka, skOceEcka.D);

            byte[] keyMaterial = MergeArrays(keyAgreementFirst, keyAgreementSecond);
            byte[] keyAgreementData = MergeArrays(hostAuthenticateTlvEncodedData, epkSdEckaTlvEncodedData);
            byte[] sharedInfo = MergeArrays(keyUsage, keyType, keyLen);

            const int keyCount = 4;
            var keys = new List<byte[]>(keyCount);
            byte counter = 1;
            for (int i = 0; i <= keyCount; i++)
            {
                using var hash = CryptographyProviders.Sha256Creator();

                _ = hash.TransformBlock(keyMaterial, 0, keyMaterial.Length, null, 0);
                _ = hash.TransformBlock(new byte[] { 0, 0, 0, counter }, 0, 4, null, 0);
                _ = hash.TransformFinalBlock(sharedInfo, 0, sharedInfo.Length);

                Span<byte> digest = hash.Hash;
                keys.Add(digest[..16].ToArray());
                keys.Add(digest[16..].ToArray());

                ++counter;
                CryptographicOperations.ZeroMemory(digest);
            }

            // Get keys
            byte[] receiptVerificationKey = keys[0];
            byte[] encryptionKey = keys[1];
            byte[] macKey = keys[2];
            byte[] rmacKey = keys[3];
            byte[] dekKey = keys[4];

            // Do AES CMAC 
            using var cmacObj = CryptographyProviders.CmacPrimitivesCreator(CmacBlockCipherAlgorithm.Aes128);
            
            Span<byte> oceReceipt = stackalloc byte[16]; 
            cmacObj.CmacInit(receiptVerificationKey);
            cmacObj.CmacUpdate(keyAgreementData);
            cmacObj.CmacFinal(oceReceipt); // Our generated receipt

            if (!CryptographicOperations.FixedTimeEquals(
                    oceReceipt, sdReceipt.Span)) // Needs to match with the receipt generated by the Yubikey
            {
                throw new SecureChannelException(ExceptionMessages.KeyAgreementReceiptMissmatch);
            }

            return (encryptionKey, macKey, rmacKey, dekKey);
        }

        /// <summary>
        /// Extracts EC public key parameters from TLV-encoded data.
        /// </summary>
        /// <param name="epkSdEckaTlv">The TLV-encoded public key data.</param>
        /// <param name="curve">The elliptic curve parameters.</param>
        /// <returns>The extracted EC parameters containing the public key coordinates.</returns>
        private static ECParameters ExtractPublicKeyEcParameters(ReadOnlyMemory<byte> epkSdEckaTlv, ECCurve curve)
        {
            var epkSdEckaEncodedPoint = TlvObjects.UnpackValue(EckaTag, epkSdEckaTlv.Span);
            var epkSdEcka = new ECParameters
            {
                Curve = curve,
                Q = new ECPoint
                {
                    X = epkSdEckaEncodedPoint.Span[1..33].ToArray(),
                    Y = epkSdEckaEncodedPoint.Span[33..].ToArray()
                }
            };

            return epkSdEcka;
        }

        /// <summary>
        /// Gets the standardized SCP identifier for the given key reference.
        /// As defined in Global Platform Secure Channel Protocol 11 Card Specification v2.3 – Amendment F § 7.1.1
        /// </summary>
        /// <param name="keyReference">The key reference to get the identifier for.</param>
        /// <returns>The SCP identifier byte.</returns>
        /// <exception cref="ArgumentException">Thrown when the key reference ID is not a valid SCP11 KID.</exception>
        private static byte GetScpIdentifierByte(KeyReference keyReference) =>
            keyReference.Id switch
            {
                ScpKeyIds.Scp11A => 0b01,
                ScpKeyIds.Scp11B => 0b00,
                ScpKeyIds.Scp11C => 0b11,
                _ => throw new ArgumentException("Invalid SCP11 KID")
            };

        /// <summary>
        /// Performs the Security Operation command sequence required for SCP11a and SCP11c authentication.
        /// </summary>
        /// <param name="pipeline">The APDU pipeline for communication with the YubiKey.</param>
        /// <param name="keyParams">The key parameters containing certificates and references.</param>
        /// <exception cref="ArgumentNullException">Thrown when required key parameters are missing.</exception>
        /// <exception cref="ArgumentException">Thrown when required certificates are missing.</exception>
        /// <exception cref="SecureChannelException">Thrown when the security operation fails.</exception>
        private static void PerformSecurityOperation(IApduTransform pipeline, Scp11KeyParameters keyParams)
        {
            // GPC v2.3 Amendment F (SCP11) v1.4 §7.5
            if (keyParams.SkOceEcka == null)
            {
                throw new ArgumentNullException(
                    nameof(keyParams.SkOceEcka),
                    "SCP11a and SCP11c require a private key");
            }

            if (keyParams.OceCertificates == null || keyParams.OceCertificates.Count == 0)
            {
                throw new ArgumentException(
                    "SCP11a and SCP11c require a certificate chain", nameof(keyParams.OceCertificates));
            }

            int n = keyParams.OceCertificates.Count - 1;
            var oceRef = keyParams.OceKeyReference ?? new KeyReference(0, 0);
            for (int i = 0; i <= n; i++)
            {
                byte[] certificates = keyParams.OceCertificates[i].RawData;
                byte oceRefInput = (byte)(oceRef.Id | (i < n
                    ? 0x80
                    : 0x00)); // Append 0x80 if more certificates remain to be sent

                var securityOperationCommand = new SecurityOperationCommand(
                    oceRef.VersionNumber,
                    oceRefInput,
                    certificates);

                // Send payload
                var responseSecurityOperation = pipeline.Invoke(
                    securityOperationCommand.CreateCommandApdu(),
                    typeof(SecurityOperationCommand),
                    typeof(SecurityOperationResponse));

                if (responseSecurityOperation.SW != SWConstants.Success)
                {
                    throw new SecureChannelException(
                        $"Security operation failed. Status: {responseSecurityOperation.SW:X4}");
                }
            }
        }

        /// <summary>
        /// Combines multiple byte arrays into a single array.
        /// </summary>
        /// <param name="values">The arrays to merge.</param>
        /// <returns>A new array containing all input arrays concatenated in sequence.</returns>
        private static byte[] MergeArrays(params ReadOnlyMemory<byte>[] values)
        {
            using var memoryStream = new MemoryStream();
            foreach (var bytes in values)
            {
                memoryStream.Write(bytes.Span.ToArray(), 0, bytes.Length);
            }

            return memoryStream.ToArray();
        }
    }
}
