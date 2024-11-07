﻿// Copyright 2023 Yubico AB
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Yubico.Core.Buffers;
using Yubico.Core.Iso7816;
using Yubico.Core.Logging;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Scp.Commands;
using Yubico.YubiKey.Scp03;

namespace Yubico.YubiKey.Scp
{
    /// <summary>
    /// Create a session for managing the SCP configuration of a YubiKey.
    /// </summary>
    /// <remarks>
    /// See the <xref href="UsersManualScp">User's Manual entry</xref> on SCP.
    /// <para>
    /// Usually, you use SCP "in the background" to secure the communication
    /// with another application. For example, when you want to perform PIV
    /// operations, but need to send the commands to and get the responses from
    /// the YubiKey securely (such as sending commands remotely where
    /// authenticity and confidentiality are required), you use SCP.
    /// <code language="csharp">
    ///   if (YubiKeyDevice.TryGetYubiKey(serialNumber, out IYubiKeyDevice yubiKeyDevice))
    ///   {
    ///       using (var pivSession = new PivSession(scpDevice, scpKeys))
    ///       {
    ///         . . .
    ///       }
    ///   }
    /// </code>
    /// </para>
    /// <para>
    /// However, there are times you need to manage the configuration of SCP
    /// directly, not as simply the security layer for a PIV or other
    /// applications. The most common operations are loading and deleting SCP
    /// key sets on the YubiKey.
    /// </para>
    /// <para>
    /// For the SCP configuration management operations, use the
    /// <c>ScpSession</c> class.
    /// </para>
    /// <para>
    /// Once you have the YubiKey to use, you will build an instance of this
    /// <c>ScpSession</c> class to represent the SCP on the hardware.
    /// Because this class implements <c>IDisposable</c>, use the <c>using</c>
    /// keyword. For example,
    /// <code language="csharp">
    ///   if (YubiKeyDevice.TryGetYubiKey(serialNumber, out IYubiKeyDevice yubiKeyDevice))
    ///   {
    ///       var scpKeys = new StaticKeys();
    ///       using (var scp = new ScpSession(yubiKeyDevice, scpKeys))
    ///       {
    ///           // Perform SCP operations.
    ///       }
    ///   }
    /// </code>
    /// </para>
    /// <para>
    /// If the YubiKey does not support SCP, the constructor will throw an
    /// exception.
    /// </para>
    /// <para>
    /// If the StaticKeys provided are not correct, the constructor will throw an
    /// exception.
    /// </para>
    /// </remarks>
    public sealed class SecurityDomainSession : ApplicationSession
    {
        #region Tags
        private const byte EcKeyType = 0xF0;
        private const byte EcPublicKeyKeyType = 0xB0;
        private const byte EcPrivateKeyKeyType = 0xB1;
        private const byte AesKeyType = 0x88;
        private const byte ControlReferenceTag = 0xA6;
        private const byte KidKvnTag = 0x83;
        private const byte KeyInformationTag = 0xE0;
        private const byte SerialsAllowListTag = 0x70;
        private const byte SerialTag = 0x93;
        private const byte CardRecognitionDataTag = 0x66;
        private const ushort CertificateStoreTag = 0xBF21;
        private const ushort CaKlocIdentifiersTag = 0xFF33; // Key Loading OCE Certificate
        private const ushort CaKlccIdentifiersTag = 0xFF34; // Key Loading Card Certificate
        #endregion

        private EncryptDataFunc EncryptData
        {
            get
            {
                if (Connection is IScpYubiKeyConnection scpConnection)
                {
                    return scpConnection.EncryptDataFunc;
                }

                throw new InvalidOperationException("No secure connection initialized.");
            }
        }

        /// <summary>
        /// Create an unauthenticated instance of <see cref="SecurityDomainSession"/>, the object that
        /// represents SCP on the YubiKey.
        /// </summary>
        /// <remarks>Sessions created from this constructor will not be able to perform operations which require authentication
        /// <para>See GlobalPlatform Technology Card Specification v2.3.1 §11 APDU Command Reference for more information.</para>
        /// </remarks>
        /// <param name="yubiKey">
        /// The object that represents the actual YubiKey which will perform the
        /// operations.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>yubiKey</c> argument is null.
        /// </exception>
        public SecurityDomainSession(IYubiKeyDevice yubiKey)
            : base(Log.GetLogger<SecurityDomainSession>(), yubiKey, YubiKeyApplication.SecurityDomain, null)
        {
        }

        /// <summary>
        /// Create an instance of <see cref="SecurityDomainSession"/>, the object that
        /// represents SCP on the YubiKey.
        /// </summary>
        /// <remarks>
        /// See the <xref href="UsersManualScp">User's Manual entry</xref> on SCP.
        /// <para>See GlobalPlatform Technology Card Specification v2.3.1 §11 APDU Command Reference for more information on SCP.</para>
        /// <para>
        /// Because this class implements <c>IDisposable</c>, use the <c>using</c>
        /// keyword. For example,
        /// <code language="csharp">
        ///   if (YubiKeyDevice.TryGetYubiKey(serialNumber, out IYubiKeyDevice yubiKeyDevice))
        ///   {
        ///       using (var scp = new SecurityDomainSession(yubiKeyDevice, Scp03KeyParameters.DefaultKey))
        ///       {
        ///           // Perform SCP operations while authenticated with SCP03
        ///       }
        ///   }
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="yubiKey">
        /// The object that represents the actual YubiKey which will perform the
        /// operations.
        /// </param>
        /// <param name="scpKeyParameters">
        /// The shared secret keys that will be used to authenticate the caller
        /// and encrypt the communications. This constructor will make a deep
        /// copy of the keys, it will not copy a reference to the object. //TODO Deep copy
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>yubiKey</c> or <c>scpKeys</c> argument is null.
        /// </exception>
        public SecurityDomainSession(IYubiKeyDevice yubiKey, ScpKeyParameters scpKeyParameters)
            : base(Log.GetLogger<SecurityDomainSession>(), yubiKey, YubiKeyApplication.SecurityDomain, scpKeyParameters)
        {
        }

        /// <summary>
        /// Puts an SCP03 key set onto the YubiKey using the Security Domain.
        /// </summary>
        /// <param name="keyRef">The key reference identifying where to store the key.</param>
        /// <param name="newKeySet">The new SCP03 key set to store.</param>
        /// <param name="replaceKvn">The key version number to replace, or 0 for a new key.</param>
        /// <exception cref="ArgumentException">Thrown when the KID is not 0x01 for SCP03 key sets.</exception>
        /// <exception cref="SecureChannelException">Thrown when the new key set's checksum failed to verify, or some other SCP related error
        /// described in the exception message.</exception>
        public void PutKey(KeyReference keyRef, StaticKeys newKeySet, int replaceKvn)
        {
            Logger.LogInformation("Importing SCP03 key set into KeyRef {KeyRef}", keyRef);

            if (keyRef.Id != ScpKid.Scp03)
            {
                throw new ArgumentException("KID must be 0x01 for SCP03 key sets");
            }

            using var dataStream = new MemoryStream();
            using var dataWriter = new BinaryWriter(dataStream);
            using var expectedKcvStream = new MemoryStream();
            using var expectedKcvWriter = new BinaryWriter(expectedKcvStream);

            // Write KVN
            dataWriter.Write(keyRef.VersionNumber);
            expectedKcvWriter.Write(keyRef.VersionNumber);

            Span<byte> kcvInput = stackalloc byte[16];
            ReadOnlySpan<byte> kvcZeroIv = stackalloc byte[16];
            kcvInput.Fill(1);

            // Process all keys
            foreach (var key in new[]
                     {
                         newKeySet.ChannelEncryptionKey,
                         newKeySet.ChannelMacKey,
                         newKeySet.DataEncryptionKey
                     })
            {
                // Key check value (KCV) is first 3 bytes of encrypted test vector
                var kcv = AesUtilities.AesCbcEncrypt(
                    key.Span,
                    kvcZeroIv,
                    kcvInput)[..3];

                // Encrypt the key using session encryptor
                var encryptedKey = EncryptData(key);

                // Write key structure
                var tlvData = new TlvObject(AesKeyType, encryptedKey.Span.ToArray()).GetBytes();
                dataWriter.Write(tlvData.ToArray());

                // Write KCV
                byte[] kcvData = kcv.ToArray();
                dataWriter.Write((byte)kcvData.Length);
                dataWriter.Write(kcvData);

                // Add KCV to expected response
                expectedKcvWriter.Write(kcvData);
            }

            ReadOnlyMemory<byte> commandData = dataStream.ToArray().AsMemory();
            byte p2 = (byte)(0x80 | keyRef.Id); // OR with 0x80 indicates that we're sending multiple keys

            var command = new PutKeyCommand((byte)replaceKvn, p2, commandData);
            var response = Connection.SendCommand(command);
            ThrowIfFailed(response);

            var responseKcvData = response.GetData().Span;
            ReadOnlySpan<byte> expectedKcvData = expectedKcvStream.ToArray().AsSpan();
            ValidateCheckSum(expectedKcvData, responseKcvData);

            Logger.LogInformation("Successsfully put static keys for KeyRef {KeyRef}", keyRef);
        }

        /// <summary>
        /// Puts an ECC private key onto the YubiKey using the Security Domain.
        /// </summary>
        /// <param name="keyRef">The key reference identifying where to store the key.</param>
        /// <param name="privateKeyParameters">The ECC private key parameters to store.</param>
        /// <param name="replaceKvn">The key version number to replace, or 0 for a new key.</param>
        /// <exception cref="ArgumentException">Thrown when the private key is not of type SECP256R1.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no secure session is established.</exception>
        /// <exception cref="SecureChannelException">Thrown when the new key set's checksum failed to verify, or some other SCP related error
        /// described in the exception message.</exception>
        public void PutKey(KeyReference keyRef, ECPrivateKeyParameters privateKeyParameters, int replaceKvn)
        {
            Logger.LogInformation("Importing SCP11 private key into KeyRef {KeyRef}", keyRef);

            var privateKey = privateKeyParameters.Parameters;
            if (privateKey.Curve.Oid.Value != ECCurve.NamedCurves.nistP256.Oid.Value)
            {
                throw new ArgumentException("Private key must be of type SECP256R1");
            }

            try
            {
                // Prepare the command data
                using var commandDataStream = new MemoryStream();
                using var commandDataWriter = new BinaryWriter(commandDataStream);

                // Write the key version number
                commandDataWriter.Write(keyRef.VersionNumber);

                // Convert the private key to bytes and encrypt it
                var privateKeyBytes = privateKey.D.AsMemory();
                try
                {
                    // Must be encrypted with the active sessions data encryption key
                    var encryptedKey = EncryptData(privateKeyBytes);
                    var privateKeyTlv = new TlvObject(EcPrivateKeyKeyType, encryptedKey.Span).GetBytes();
                    commandDataWriter.Write(privateKeyTlv.ToArray());
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(privateKeyBytes.Span);
                }

                // Write the ECC parameters
                var paramsTlv = new TlvObject(EcKeyType, new byte[] { 0x00 }).GetBytes();
                commandDataWriter.Write(paramsTlv.ToArray());
                commandDataWriter.Write((byte)0);

                // Create and send the command
                var command = new PutKeyCommand((byte)replaceKvn, keyRef.Id, commandDataStream.ToArray());
                var response = Connection.SendCommand(command);
                ThrowIfFailed(response);

                // Get and validate the response
                var responseData = response.GetData();
                Span<byte> expectedResponseData = new[] { keyRef.VersionNumber };
                ValidateCheckSum(responseData.Span, expectedResponseData);

                Logger.LogInformation("Successsfully put private key for KeyRef {KeyRef}", keyRef);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to put private key for KeyRef {KeyRef}", keyRef);
                throw;
            }
        }

        /// <summary>
        /// Puts an ECC public key onto the YubiKey using the Security Domain.
        /// </summary>
        /// <param name="keyRef">The key reference identifying where to store the key.</param>
        /// <param name="publicKeyParameters">The ECC public key parameters to store.</param>
        /// <param name="replaceKvn">The key version number to replace, or 0 for a new key.</param>
        /// <exception cref="ArgumentException">Thrown when the public key is not of type SECP256R1.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no secure session is established.</exception>
        /// <exception cref="SecureChannelException">Thrown when the new key set's checksum failed to verify, or some other SCP related error
        /// described in the exception message.</exception>
        public void PutKey(KeyReference keyRef, ECPublicKeyParameters publicKeyParameters, int replaceKvn)
        {
            Logger.LogInformation("Importing SCP11 public key into KeyRef {KeyRef}", keyRef);

            var pkParams = publicKeyParameters.Parameters;
            if (pkParams.Curve.Oid.Value != ECCurve.NamedCurves.nistP256.Oid.Value)
            {
                throw new ArgumentException("Private key must be of type SECP256R1");
            }

            try
            {
                using var commandDataMs = new MemoryStream();
                using var commandDataWriter = new BinaryWriter(commandDataMs);

                // Write the key version number
                commandDataWriter.Write(keyRef.VersionNumber);

                // Write the ECC public key
                byte[] formatIdentifier = { 0x4 }; // Uncompressed point
                var publicKeyRawData =
                    formatIdentifier
                        .Concat(pkParams.Q.X)
                        .Concat(pkParams.Q.Y).ToArray().AsSpan();

                byte[] publicKeyTlvData = new TlvObject(EcPublicKeyKeyType, publicKeyRawData).GetBytes().ToArray();
                commandDataWriter.Write(publicKeyTlvData);

                // Write the ECC parameters
                var paramsTlv = new TlvObject(EcKeyType, new byte[] { 0 }).GetBytes();
                commandDataWriter.Write(paramsTlv.ToArray());
                commandDataWriter.Write((byte)0);

                // Create and send the command
                byte[] commandData = commandDataMs.ToArray();
                var command = new PutKeyCommand((byte)replaceKvn, keyRef.Id, commandData);
                var response = Connection.SendCommand(command);
                ThrowIfFailed(response);

                // Get and validate the response
                var responseData = response.GetData();
                Span<byte> expectedResponseData = new[] { keyRef.VersionNumber };

                ValidateCheckSum(responseData.Span, expectedResponseData);

                Logger.LogInformation("Successsfully put public key for KeyRef {KeyRef}", keyRef);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to put public key for KeyRef {KeyRef}", keyRef);
                throw;
            }
        }

        // /// <summary>
        // /// Delete the key set with the given <c>keyVersionNumber</c>. If the key
        // /// set to delete is the last SCP key set on the YubiKey, pass
        // /// <c>true</c> as the <c>isLastKey</c> arg.
        // /// </summary>
        // /// <remarks>
        // /// The key set used to create the SCP session cannot be the key set to
        // /// be deleted, unless both of the other key sets have been deleted, and
        // /// you pass <c>true</c> for <c>isLastKey</c>. In this case, the key will
        // /// be deleted but the SCP application on the YubiKey will be reset
        // /// with the default key.
        // /// </remarks>
        // /// <param name="keyVersionNumber">
        // /// The number specifying which key set to delete.
        // /// </param>
        // /// <param name="isLastKey">
        // /// If this key set is the last SCP key set on the YubiKey, pass
        // /// <c>true</c>, otherwise, pass <c>false</c>. This arg has a default of
        // /// <c>false</c> so if no argument is given, it will be <c>false</c>.
        // /// </param>
        // /// <exception cref="SecureChannelException">Thrown when there was an SCP error, described in the exception message.</exception>
        // public void DeleteKeySet(byte keyVersionNumber, bool isLastKey = false)
        // {
        //     _log.LogInformation("Deleting an SCP key set from a YubiKey.");
        //     var command = new DeleteKeyCommand(keyVersionNumber, isLastKey);
        //     var response = Connection.SendCommand(command);
        //     ThrowIfFailed(response);
        //     _log.LogInformation("Successfully deleted {{KeyVersionNumber}}", keyVersionNumber);
        // }

        /// <summary>
        /// Delete one (or more) keys matching the specified criteria.
        /// </summary>
        /// <remarks>
        /// All keys matching the given KID (Key ID) and/or KVN (Key Version Number) will be deleted,
        /// where 0 is treated as a wildcard. For SCP03 keys, they can only be deleted by KVN.
        /// </remarks>
        /// <param name="keyRef">A reference to the key(s) to delete.</param>
        /// <param name="deleteLast">Must be true if deleting the final key, false otherwise.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when both KID and KVN are 0, or when attempting to delete SCP03 keys by KID.
        /// </exception>
        /// <exception cref="SecureChannelException">
        /// Thrown when the delete operation fails.
        /// </exception>
        public void DeleteKeySet(KeyReference keyRef, bool deleteLast = false)
        {
            if (keyRef.Id == 0 && keyRef.VersionNumber == 0)
            {
                throw new ArgumentException("At least one of KID, KVN must be nonzero");
            }

            byte kid = keyRef.Id;
            byte kvn = keyRef.VersionNumber;

            // Special handling for SCP03 keys (1, 2, 3)
            if (kid == 1 || kid == 2 || kid == 3)
            {
                if (kvn != 0)
                {
                    kid = 0;
                }
                else
                {
                    throw new ArgumentException("SCP03 keys can only be deleted by KVN");
                }
            }

            Logger.LogDebug("Deleting keys matching KeyRef {KeyRef}", keyRef);

            // Build TLV list for command data
            var tlvList = new List<TlvObject>();
            if (kid != 0)
            {
                tlvList.Add(new TlvObject(0xD0, new[] { kid }));
            }

            if (kvn != 0)
            {
                tlvList.Add(new TlvObject(0xD2, new[] { kvn }));
            }

            byte[] data = TlvObjects.EncodeList(tlvList);
            var command = new DeleteKeyCommand(data, deleteLast);
            var response = Connection.SendCommand(command);

            ThrowIfFailed(response);

            Logger.LogInformation("Keys deleted. KeyRef: ({KeyReference})", keyRef);
        }

        /// <summary>
        /// Generate a new EC key pair for the given key reference.
        /// </summary>
        /// <remarks>
        /// GlobalPlatform has no command to generate key pairs on the card itself. This is a
        /// Yubico extension that tries to mimic the format of the GPC PUT KEY
        /// command.
        /// </remarks>
        /// <param name="keyRef">The KID-KVN pair of the key that should be generated.</param>
        /// <param name="replaceKvn">The key version number of the key set that should be replaced, or 0 to generate a new key pair.</param>
        /// <returns>The parameters of the generated key, including the curve and the public point.</returns>
        /// <exception cref="SecureChannelException">Thrown when there was an SCP error, described in the exception message.</exception>
        public ECPublicKeyParameters GenerateEcKey(KeyReference keyRef, byte replaceKvn)
        {
            Logger.LogInformation(
                "Generating new key for {KeyRef}{ReplaceMessage}",
                keyRef,
                replaceKvn == 0
                    ? string.Empty
                    : $", replacing KVN=0x{replaceKvn:X2}");

            // Create tlv data for the command
            var paramsTlv = new TlvObject(EcKeyType, new byte[] { 0 }).GetBytes();
            byte[] commandData = new byte[paramsTlv.Length + 1];
            commandData[0] = keyRef.VersionNumber;
            paramsTlv.CopyTo(commandData.AsMemory(1));

            // Create and send the command
            var command = new GenerateEcKeyCommand(replaceKvn, keyRef.Id, commandData);
            var response = Connection.SendCommand(command);
            ThrowIfFailed(response);

            // Parse the response, extract the public point
            var tlvReader = new TlvReader(response.GetData());
            var encodedPoint = tlvReader.ReadValue(EcPublicKeyKeyType).Span;

            // Create the ECParameters with the public point
            var eccPublicKey = encodedPoint.CreateEcPublicKeyFromBytes();
            return eccPublicKey;
        }

        /// <summary>
        /// Store the SKI (Subject Key Identifier) for the CA of a given key.
        /// Requires off-card entity verification.
        /// </summary>
        /// <param name="keyRef">A reference to the key for which to store the CA issuer.</param>
        /// <param name="ski">The Subject Key Identifier to store.</param>
        public void StoreCaIssuer(KeyReference keyRef, ReadOnlyMemory<byte> ski)
        {
            Logger.LogDebug("Storing CA issuer SKI for {KeyRef}", keyRef);

            byte klcc = 0; // Key Loading Card Certificate
            switch (keyRef.Id)
            {
                case ScpKid.Scp11a:
                case ScpKid.Scp11b:
                case ScpKid.Scp11c:
                    klcc = 1;
                    break;
            }

            // Create and serialize data
            var data = new TlvObject(
                ControlReferenceTag, TlvObjects.EncodeList(
                    new List<TlvObject>
                    {
                        new TlvObject(0x80, new[] { klcc }),
                        new TlvObject(0x42, ski.Span),
                        new TlvObject(0x83, keyRef.GetBytes)
                    }
                    )).GetBytes();

            // Send store data command
            StoreData(data);

            Logger.LogInformation("CA issuer SKI stored");
        }

        /// <summary>
        /// Store a list of certificates associated with the given key reference. //TODO should document limitations of this command as well as other storedata command
        /// </summary>
        /// <param name="keyRef">The key reference associated with the certificates.</param>
        /// <param name="certificates">The certificates to store.</param>
        /// <remarks>
        /// The certificates will be stored in the order they are provided in the list.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when certificatedata</exception>
        /// <exception cref="SecureChannelException">Thrown when there was an SCP error, described in the exception message.</exception>
        public void StoreCertificates(KeyReference keyRef, IReadOnlyList<X509Certificate2> certificates)
        {
            Logger.LogDebug("Storing certificate bundle for {KeyRef}", keyRef);

            // Write each certificate to a memory stream
            using var certDataMs = new MemoryStream();
            foreach (var cert in certificates)
            {
                try
                {
                    byte[] certTlvEncoded = cert.GetRawCertData(); // ASN.1 DER (TLV) encoded certificate
                    certDataMs.Write(certTlvEncoded, 0, certTlvEncoded.Length);
                }
                catch (CryptographicException e)
                {
                    throw new ArgumentException("Failed to get encoded version of certificate", e);
                }
            }

            // Create and serialize data
            Memory<byte> certDataEncoded = TlvObjects.EncodeMany(
                new TlvObject(ControlReferenceTag, new TlvObject(KidKvnTag, keyRef.GetBytes).GetBytes().Span),
                new TlvObject(CertificateStoreTag, certDataMs.ToArray())
                );

            StoreData(certDataEncoded);

            Logger.LogInformation("Certificate bundle stored");
        }

        /// <summary>
        /// Stores an allowlist of certificate serial numbers for a specified key reference.
        /// </summary>
        /// <remarks>
        /// This method requires off-card entity verification. If an allowlist is not stored, any
        /// certificate signed by the CA can be used.
        /// </remarks>
        /// <param name="keyRef">A reference to the key for which the allowlist will be stored.</param>
        /// <param name="serials">The list of certificate serial numbers (in hexadecimal string format) to be stored in the allowlist for the given <see cref="KeyReference"/>.</param>
        /// <exception cref="ArgumentException">Thrown when a serial number cannot be encoded properly.</exception>
        /// <exception cref="SecureChannelException">Thrown when there was an SCP error, described in the exception message.</exception>
        public void StoreAllowlist(KeyReference keyRef, IReadOnlyCollection<string> serials)
        {
            Logger.LogDebug("Storing allow list for {KeyRef}", keyRef);

            using var serialDataMs = new MemoryStream();
            foreach (string? serial in serials)
            {
                try
                {
                    byte[] serialAsBytes = Base16.DecodeText(serial);
                    byte[] serialTlvEncoded = new TlvObject(SerialTag, serialAsBytes).GetBytes().ToArray();
                    serialDataMs.Write(serialTlvEncoded, 0, serialTlvEncoded.Length);
                }
                catch (CryptographicException e)
                {
                    throw new ArgumentException("Failed to get encoded version of certificate", e);
                }
            }

            Memory<byte> serialsDataEncoded = TlvObjects.EncodeMany(
                new TlvObject(ControlReferenceTag, new TlvObject(KidKvnTag, keyRef.GetBytes).GetBytes().Span),
                new TlvObject(SerialsAllowListTag, serialDataMs.ToArray())
                );

            StoreData(serialsDataEncoded);

            Logger.LogInformation("Certificate bundle stored");
        }

        /// <summary>
        /// Clears the allow list for the given <see cref="KeyReference"/>
        /// </summary>
        /// <seealso cref="StoreAllowlist"/>
        /// <param name="keyRef">The key reference that holds the allow list</param>
        public void ClearAllowList(KeyReference keyRef) => StoreAllowlist(keyRef, Array.Empty<string>());

        /// <summary>
        /// Stores data in the Security Domain or targeted Application on the YubiKey using the GlobalPlatform STORE DATA command.
        /// </summary>
        /// <remarks>
        /// The STORE DATA command is used to transfer data to either the Security Domain itself or to an Application 
        /// being personalized. The data must be formatted as BER-TLV structures according to ISO 8825.
        /// <para>
        /// This implementation:
        /// - Uses a single block transfer (P1.b8=1 indicating last block)
        /// - Requires BER-TLV formatted data (P1.b5-b4=10)
        /// - Does not provide encryption information (P1.b7-b6=00)
        /// </para>
        /// <para>
        /// Note that this command's behavior depends on the current security context:
        /// - Outside a personalization session: Data is processed by the Security Domain
        /// - During personalization (after INSTALL [for personalization]): Data is forwarded to the target Application
        /// </para>
        /// <para>See GlobalPlatform Technology Card Specification v2.3.1 §11 APDU Command Reference for more information.</para>
        /// </remarks>
        /// <param name="data">
        /// The data to be stored, which must be formatted as BER-TLV structures according to ISO 8825.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no secure connection is available or the security context is invalid.
        /// </exception>
        /// <exception cref="SecureChannelException">Thrown when there was an SCP error, described in the exception message.</exception>
        public void StoreData(ReadOnlyMemory<byte> data) // TODO make test
        {
            Logger.LogInformation("Storing data with length:{Length}", data.Length);

            var command = new StoreDataCommand(data);
            var response = Connection.SendCommand(command);
            ThrowIfFailed(response);
        }

        /// <summary>
        /// Retrieves the key information stored in the YubiKey and returns it in a dictionary format.
        /// </summary>
        /// <returns>A read only dictionary containing the KeyReference as the key and a dictionary of key components as the value.</returns>
        /// <exception cref="SecureChannelException">Thrown when there was an SCP error, described in the exception message.</exception>
        public IReadOnlyDictionary<KeyReference, Dictionary<byte, byte>> GetKeyInformation()
        {
            Logger.LogInformation("Getting key information");

            var keyInformation = new Dictionary<KeyReference, Dictionary<byte, byte>>();

            var getDataResult = GetData(KeyInformationTag);
            var tlvList = TlvObjects.DecodeList(getDataResult.Span);
            foreach (var tlvObject in tlvList)
            {
                var value = TlvObjects.UnpackValue(0xC0, tlvObject.GetBytes().Span);
                var keyRef = new KeyReference(value.Span[0], value.Span[1]);
                var keyComponents = new Dictionary<byte, byte>();

                // Iterate while there are more key components, each component is 2 bytes, so take 2 bytes at a time
                while (!(value = value[2..]).IsEmpty)
                {
                    keyComponents.Add(value.Span[0], value.Span[1]);
                }

                keyInformation.Add(keyRef, keyComponents);
            }

            return keyInformation;
        }

        /// <summary>
        /// Retrieves the certificates associated with the given <paramref name="keyReference"/>.
        /// </summary>
        /// <param name="keyReference">The key reference for which the certificates should be retrieved.</param>
        /// <returns>A list of X.509 certificates associated with the key reference.</returns>
        /// <exception cref="SecureChannelException">Thrown when there was an SCP error, described in the exception message.</exception>
        public IReadOnlyList<X509Certificate2> GetCertificates(KeyReference keyReference)
        {
            Logger.LogInformation("Getting certificates for key={KeyRef}", keyReference);

            var nestedTlv = new TlvObject(
                ControlReferenceTag,
                new TlvObject(KidKvnTag, keyReference.GetBytes).GetBytes().Span
                ).GetBytes();

            var certificateTlvData = GetData(CertificateStoreTag, nestedTlv);
            var certificateTlvList = TlvObjects.DecodeList(certificateTlvData.Span);

            return certificateTlvList
                .Select(tlv => new X509Certificate2(tlv.GetBytes().ToArray()))
                .ToList();
        }

        /// <summary>
        /// Gets the supported CA identifiers for KLOC and/or KLCC.
        /// </summary>
        /// <param name="kloc">Whether to retrieve Key Loading OCE Certificate (KLOC) identifiers.</param>
        /// <param name="klcc">Whether to retrieve Key Loading Card Certificate (KLCC) identifiers.</param>
        /// <returns>A dictionary of KeyReference and byte arrays representing the CA identifiers.</returns>
        /// <exception cref="ArgumentException">Thrown when both kloc and klcc are false.</exception>
        /// <exception cref="SecureChannelException">Thrown when there was an SCP error, described in the exception message.</exception>
        public IReadOnlyDictionary<KeyReference, ReadOnlyMemory<byte>>
            GetSupportedCaIdentifiers(bool kloc, bool klcc) // TODO make test
        {
            if (!kloc && !klcc)
            {
                throw new ArgumentException("At least one of kloc and klcc must be true");
            }

            Logger.LogDebug("Getting CA identifiers KLOC={Kloc}, KLCC={Klcc}", kloc, klcc);

            var dataMs = new MemoryStream();

            if (kloc)
            {
                try
                {
                    var klocData = GetData(CaKlocIdentifiersTag);
                    dataMs.Write(klocData.Span.ToArray(), 0, klocData.Length);
                }
                catch
                    (SecureChannelException) //when (/*e.StatusWord == SWConstants.ReferencedDataNotFound*/) TODO how get response status?
                {
                    // Ignore this specific exception
                }
            }

            if (klcc)
            {
                try
                {
                    var klccData = GetData(CaKlccIdentifiersTag);
                    dataMs.Write(klccData.Span.ToArray(), 0, klccData.Length);
                }
                catch (SecureChannelException) //when (/*e.StatusWord == SWConstants.ReferencedDataNotFound*/) TODO
                {
                    // Ignore this specific exception
                }
            }

            var tlvs = TlvObjects.DecodeList(dataMs.ToArray());
            var identifiers = new Dictionary<KeyReference, ReadOnlyMemory<byte>>();

            var tlvsSpan = tlvs.ToArray().AsSpan();
            while (!tlvsSpan.IsEmpty)
            {
                var current = tlvsSpan[0];
                var next = tlvsSpan[1];

                var refData = next.GetBytes().Span;
                var keyRef = new KeyReference(refData[0], refData[1]);
                identifiers[keyRef] = current.GetBytes();

                tlvsSpan = tlvsSpan[..2];
            }

            return identifiers;
        }

        public Memory<byte> GetCardRecognitionData() // TODO Ask Dain // TODO make test
        {
            Logger.LogInformation("Getting card recognition deta");

            var tlvData = GetData(CardRecognitionDataTag).Span;
            var cardRecognitionData = TlvObjects.UnpackValue(0x73, tlvData);

            return cardRecognitionData;
        }

        /// <summary>
        /// Gets data from the YubiKey associated with the given tag.
        /// </summary>
        /// <param name="tag">The tag of the data to retrieve.</param>
        /// <param name="data">Optional data to send with the command.</param>
        /// <remarks>Sessions created from this constructor will not be able to perform operations which require authentication
        /// <para>See GlobalPlatform Technology Card Specification v2.3.1 §11 APDU Command Reference for more information.</para>
        /// </remarks>
        /// <returns>The encoded tlv data retrieved from the YubiKey.</returns>
        /// <exception cref="SecureChannelException">Thrown when there was an SCP error, described in the exception message.</exception>
        public ReadOnlyMemory<byte> GetData(int tag, ReadOnlyMemory<byte>? data = null)
        {
            var command = new GetDataCommand(tag, data);
            var response = Connection.SendCommand(command);
            ThrowIfFailed(response);

            return response.GetData();
        }

        /// <summary>
        /// Perform a factory reset of the Security Domain.
        /// This will remove all keys and associated data, as well as restore the default SCP03 static keys,
        /// and generate a new (attestable) SCP11b key.
        /// </summary>
        public void Reset()
        {
            Logger.LogInformation("Resetting all SCP keys");

            var keys = GetKeyInformation().Keys;
            foreach (var keyRef in keys) // Reset is done by blocking all available keys
            {
                byte ins;
                var overridenKeyRef = keyRef;

                switch (keyRef.Id)
                {
                    case ScpKid.Scp03:
                        // SCP03 uses KID=0, we use KVN=0 to allow deleting the default keys
                        // which have an invalid KVN (0xff).
                        overridenKeyRef = new KeyReference(0, 0);
                        ins = InitializeUpdateCommand.GpInitializeUpdateIns;
                        break;
                    case 0x02:
                    case 0x03:
                        continue; // Skip these as they are deleted by 0x01
                    case ScpKid.Scp11a:
                    case ScpKid.Scp11c:
                        ins = ExternalAuthenticateCommand.GpExternalAuthenticateIns;
                        break;
                    case ScpKid.Scp11b:
                        ins = InternalAuthenticateCommand.GpInternalAuthenticateIns;
                        break;
                    default: // 0x10, 0x20-0x2F
                        ins = SecurityOperationCommand.GpPerformSecurityOperationIns;
                        break;
                }

                // Keys have 65 attempts before blocking (and thus removal)
                for (int i = 0; i < 65; i++)
                {
                    var result = Connection.SendCommand(
                        new ResetCommand(ins, overridenKeyRef.VersionNumber, overridenKeyRef.Id, new byte[8]));

                    switch (result.StatusWord)
                    {
                        case SWConstants.AuthenticationMethodBlocked:
                        case SWConstants.SecurityStatusNotSatisfied:
                            i = 65;
                            break;
                        case SWConstants.InvalidCommandDataParameter:
                            continue;
                        case SWConstants.Success:
                            continue;

                        default: continue;
                    }
                }
            }

            Logger.LogInformation("SCP keys reset");
        }

        private static void ValidateCheckSum(ReadOnlySpan<byte> responseData, ReadOnlySpan<byte> expectedResponseData)
        {
            if (!CryptographicOperations.FixedTimeEquals(responseData, expectedResponseData))
            {
                throw new SecureChannelException(ExceptionMessages.ChecksumError);
            }
        }

        private static void ThrowIfFailed(ScpResponse response)
        {
            if (response.Status != ResponseStatus.Success)
            {
                throw new SecureChannelException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiKeyOperationFailed,
                        response.StatusMessage));
            }
        }
    }
}
