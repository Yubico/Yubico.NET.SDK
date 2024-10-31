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
    internal class Scp11State : ScpState
    {
        private const int ReceiptTag = 0x86;
        private const int EckaTag = 0x5F49;
        private const int KeyAgreementTag = 0xA6;

        public Scp11State(SessionKeys sessionKeys, Memory<byte> receipt)
            : base(sessionKeys, receipt)
        {
        }

        internal static Scp11State CreateScpState(
            IApduTransform pipeline,
            Scp11KeyParameters keyParameters)
        {
            // Perform Security Operation, if needed (for Scp11a and Scp11c)
            if (keyParameters.KeyReference.Id == ScpKid.Scp11a || keyParameters.KeyReference.Id == ScpKid.Scp11c)
            {
                PerformSecurityOperation(pipeline, keyParameters);
            }

            var securityDomainPublicKey = keyParameters.SecurityDomainEllipticCurveKeyAgreementKeyPublicKey;
            var securityDomainPublicKeyCurve = securityDomainPublicKey.Curve;

            // Generate a public and private key using the supplied curve
            var ekpOceEcka = CryptographyProviders.EcdhPrimitivesCreator()
                .GenerateKeyPair(securityDomainPublicKeyCurve);

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

            var hostAuthenticateTlvEncodedData = TlvObjects.EncodeMany(
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

            var authenticateCommand = keyParameters.KeyReference.Id == ScpKid.Scp11b
                ? new InternalAuthenticateCommand(
                    keyParameters.KeyReference.VersionNumber, keyParameters.KeyReference.Id,
                    hostAuthenticateTlvEncodedData) as IYubiKeyCommand<ScpResponse>
                : new ExternalAuthenticateCommand(
                    keyParameters.KeyReference.VersionNumber, keyParameters.KeyReference.Id,
                    hostAuthenticateTlvEncodedData) as IYubiKeyCommand<ScpResponse>;

            var authenticateResponseApdu = pipeline.Invoke(
                authenticateCommand.CreateCommandApdu(), authenticateCommand.GetType(), typeof(ScpResponse));

            var authenticateResponse = authenticateCommand.CreateResponseForApdu(authenticateResponseApdu);
            authenticateResponse.ThrowIfFailed(
                $"Error when performing {authenticateCommand.GetType().Name}: {authenticateResponse.StatusMessage}");

            var authenticateResponseTlvs = TlvObjects.DecodeList(authenticateResponseApdu.Data.Span);

            var epkSdEckaTlv = authenticateResponseTlvs[0];
            var epkSdEckaTlvEncodedData = epkSdEckaTlv.GetBytes();
            var sdReceipt = TlvObjects.UnpackValue(
                ReceiptTag,
                authenticateResponseTlvs[1].GetBytes().Span); // Yubikey X963KDF Receipt to match with our own X963KDF

            var skOceEcka =
                keyParameters
                    .OffCardEntityEllipticCurveAgreementPrivateKey ?? // If set, we will use this for SCP11A and SCP11C. 
                ekpOceEcka; // Otherwise, just use the newly created ephemeral key for SCP11b.

            var (encryptionKey, macKey, rMacKey, dekKey)
                = GetX963KDFKeyAgreementKeys(
                    skOceEcka.Curve,
                    securityDomainPublicKey,
                    ekpOceEcka,
                    skOceEcka,
                    sdReceipt,
                    epkSdEckaTlvEncodedData,
                    hostAuthenticateTlvEncodedData,
                    keyUsage,
                    keyType,
                    keyLen);

            var sessionKeys = new SessionKeys(
                macKey,
                encryptionKey,
                rMacKey,
                dekKey
                );

            return new Scp11State(sessionKeys, sdReceipt.ToArray());
        }

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
            }.All(c => c.Oid == curve.Oid);

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
            byte[] encryptionKey = keys[0];
            byte[] macKey = keys[1];
            byte[] rmacKey = keys[2];
            byte[] dekKey = keys[3];

            // Do AES CMAC 
            using var cmacObj = CryptographyProviders.CmacPrimitivesCreator(CmacBlockCipherAlgorithm.Aes128);
            Span<byte> oceReceipt = stackalloc byte[16]; // Our generated receipt
            cmacObj.CmacInit(encryptionKey);
            cmacObj.CmacUpdate(keyAgreementData);
            cmacObj.CmacFinal(oceReceipt);

            if (!CryptographicOperations.FixedTimeEquals(
                    oceReceipt, sdReceipt.Span)) // Needs to match with the receipt generated by the Yubikey
            {
                throw new SecureChannelException(ExceptionMessages.KeyAgreementReceiptMissmatch);
            }

            return (encryptionKey, macKey, rmacKey, dekKey);
        }

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
        /// Global Platform Secure Channel Protocol 11 Card Specification v2.3 – Amendment F § 7.1.1
        /// </summary>
        private static byte GetScpIdentifierByte(KeyReference keyReference) =>
            keyReference.Id switch
            {
                ScpKid.Scp11a => 0b01,
                ScpKid.Scp11b => 0b00,
                ScpKid.Scp11c => 0b11,
                _ => throw new ArgumentException("Invalid SCP11 KID")
            };

        private static void PerformSecurityOperation(IApduTransform pipeline, Scp11KeyParameters keyParams)
        {
            // GPC v2.3 Amendment F (SCP11) v1.4 §7.5
            if (keyParams.OffCardEntityEllipticCurveAgreementPrivateKey == null)
            {
                throw new ArgumentNullException(
                    nameof(keyParams.OffCardEntityEllipticCurveAgreementPrivateKey),
                    "SCP11a and SCP11c require a private key");
            }

            int n = keyParams.Certificates.Count - 1;
            if (n < 0)
            {
                throw new ArgumentException(
                    "SCP11a and SCP11c require a certificate chain", nameof(keyParams.Certificates));
            }

            var oceRef = keyParams.OffCardEntityKeyReference ?? new KeyReference(0, 0);
            for (int i = 0; i <= n; i++)
            {
                byte[] certificates = keyParams.Certificates[i].RawData;
                byte oceRefPadded = (byte)(oceRef.Id | (i < n
                    ? 0b10000000
                    : 0x00)); // Is this a good name?

                var securityOperationCommand = new SecurityOperationCommand(
                    oceRef.VersionNumber,
                    oceRefPadded,
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

        private static byte[] MergeArrays(params ReadOnlyMemory<byte>[] values)
        {
            using var memoryStream = new MemoryStream();
            foreach (var bytes in values)
            {
#if NETSTANDARD2_1_OR_GREATER
                memoryStream.Write(bytes.Span);
#else
                memoryStream.Write(bytes.Span.ToArray(), 0, bytes.Length);
#endif
            }

            return memoryStream.ToArray();
        }
    }
}
