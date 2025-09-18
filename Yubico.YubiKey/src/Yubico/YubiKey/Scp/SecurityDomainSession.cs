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
using System.Collections.Generic;
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
using DeleteKeyCommand = Yubico.YubiKey.Scp.Commands.DeleteKeyCommand;
using GetDataCommand = Yubico.YubiKey.Scp.Commands.GetDataCommand;

namespace Yubico.YubiKey.Scp;

/// <summary>
///     Create a session for managing the Secure Channel Protocol (SCP) configuration of a YubiKey.
/// </summary>
/// <remarks>
///     See the <xref href="UsersManualScp">User's Manual entry</xref> on SCP.
///     <para>
///         The Security Domain session provides secure communication and key management capabilities
///         through both SCP03 (symmetric) and SCP11 (asymmetric) protocols. This session can be used in two ways:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 For direct SCP management:
///                 <list type="bullet">
///                     <item>
///                         <description>Managing SCP03 symmetric key sets (ENC, MAC, DEK)</description>
///                     </item>
///                     <item>
///                         <description>Managing SCP11 asymmetric keys (EC public/private key pairs)</description>
///                     </item>
///                     <item>
///                         <description>Configuring secure messaging parameters</description>
///                     </item>
///                     <item>
///                         <description>
///                             Example:
///                             <code language="csharp">
/// using (var scp = new SecurityDomainSession(yubiKeyDevice, scpKeyParameters))
/// {
///     // Manage SCP configuration
/// }
/// </code>
///                         </description>
///                     </item>
///                 </list>
///             </description>
///         </item>
///         <item>
///             <description>
///                 As a background security layer:
///                 <list type="bullet">
///                     <item>
///                         <description>
///                             Secures communication (encrypted channel) with other applications (e.g., PIV, OTP,
///                             OATH, YubiHSM)
///                         </description>
///                     </item>
///                     <item>
///                         <description>
///                             Provides authenticity and confidentiality (encrypted channel) for remote
///                             operations
///                         </description>
///                     </item>
///                     <item>
///                         <description>
///                             Example:
///                             <code language="csharp">
/// using (var pivSession = new PivSession(yubiKeyDevice, scpKeyParameters))
/// {
///     // Perform PIV operations with secure messaging
/// }
/// </code>
///                         </description>
///                     </item>
///                 </list>
///             </description>
///         </item>
///     </list>
///     <para>
///         The session supports various key management operations:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Loading and replacing SCP03 key sets</description>
///         </item>
///         <item>
///             <description>Storing EC private keys (NIST P-256)</description>
///         </item>
///         <item>
///             <description>Storing EC public keys (NIST P-256)</description>
///         </item>
///         <item>
///             <description>Managing key certificates and metadata</description>
///         </item>
///         <item>
///             <description>Deleting keys and resetting configurations</description>
///         </item>
///     </list>
///     <para>
///         The constructor will throw an exception if:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>The YubiKey does not support SCP</description>
///         </item>
///         <item>
///             <description>The provided key parameters are incorrect</description>
///         </item>
///     </list>
/// </remarks>
public sealed class SecurityDomainSession : ApplicationSession
{
    /// <summary>
    ///     Create an unauthenticated instance of <see cref="SecurityDomainSession" />, the object that
    ///     manages the security domain on the YubiKey.
    /// </summary>
    /// <remarks>
    ///     Sessions created from this constructor will not be able to perform operations which require authentication
    ///     <para>See GlobalPlatform Technology Card Specification v2.3.1 §11 APDU Command Reference for more information.</para>
    /// </remarks>
    /// <param name="yubiKey">
    ///     The object that represents the actual YubiKey which will perform the
    ///     operations.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     The <c>yubiKey</c> argument is null.
    /// </exception>
    public SecurityDomainSession(IYubiKeyDevice yubiKey)
        : base(Log.GetLogger<SecurityDomainSession>(), yubiKey, YubiKeyApplication.SecurityDomain, null)
    {
    }

    /// <summary>
    ///     Create an instance of <see cref="SecurityDomainSession" />, the object that
    ///     manages the security domain on the YubiKey.
    /// </summary>
    /// <remarks>
    ///     See the <xref href="UsersManualScp">User's Manual entry</xref> on SCP.
    ///     <para>
    ///         See GlobalPlatform Technology Card Specification v2.3.1 §11 APDU Command Reference for more information on
    ///         SCP.
    ///     </para>
    ///     <para>
    ///         Because this class implements <c>IDisposable</c>, use the <c>using</c>
    ///         keyword. For example,
    ///         <code language="csharp">
    ///   if (YubiKeyDevice.TryGetYubiKey(serialNumber, out IYubiKeyDevice yubiKeyDevice))
    ///   {
    ///       using (var scp = new SecurityDomainSession(yubiKeyDevice, Scp03KeyParameters.DefaultKey))
    ///       {
    ///           // Perform SCP operations while authenticated with SCP03
    ///       }
    ///   }
    /// </code>
    ///     </para>
    /// </remarks>
    /// <param name="yubiKey">
    ///     The object that represents the actual YubiKey which will perform the
    ///     operations.
    /// </param>
    /// <param name="scpKeyParameters">
    ///     The shared secret keys that will be used to authenticate the caller
    ///     and encrypt the communications.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     The <c>yubiKey</c> or <c>scpKeys</c> argument is null.
    /// </exception>
    public SecurityDomainSession(IYubiKeyDevice yubiKey, ScpKeyParameters scpKeyParameters)
        : base(Log.GetLogger<SecurityDomainSession>(), yubiKey, YubiKeyApplication.SecurityDomain, scpKeyParameters)
    {
    }

    /// <summary>
    ///     Get the encryptor to encrypt any data for an SCP command.
    ///     <seealso cref="EncryptDataFunc" />
    /// </summary>
    /// <returns>
    ///     An encryptor function that takes the plaintext as a parameter and
    ///     returns the encrypted data.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     If the data encryption key has not been set on the session keys.
    /// </exception>
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
    ///     Puts an SCP03 key set onto the YubiKey using the Security Domain.
    /// </summary>
    /// <param name="keyReference">The key reference identifying where to store the key.</param>
    /// <param name="staticKeys">The new SCP03 key set to store.</param>
    /// <param name="replaceKvn">The key version number to replace, or 0 for a new key (Default value is 0).</param>
    /// <exception cref="ArgumentException">Thrown when the KID is not 0x01 for SCP03 key sets.</exception>
    /// <exception cref="SecureChannelException">
    ///     Thrown when the new key set's checksum failed to verify, or some other SCP related error
    ///     described in the exception message.
    /// </exception>
    public void PutKey(KeyReference keyReference, StaticKeys staticKeys, int replaceKvn = 0)
    {
        Logger.LogInformation("Importing SCP03 key set into KeyReference {KeyReference}", keyReference);

        if (keyReference.Id != ScpKeyIds.Scp03)
        {
            throw new ArgumentException("Key ID (KID) must be 0x01 for SCP03 key sets");
        }

        using var dataStream = new MemoryStream();
        using var dataWriter = new BinaryWriter(dataStream);
        using var expectedKcvStream = new MemoryStream();
        using var expectedKcvWriter = new BinaryWriter(expectedKcvStream);

        // Write KVN
        dataWriter.Write(keyReference.VersionNumber);
        expectedKcvWriter.Write(keyReference.VersionNumber);

        Span<byte> kcvInput = stackalloc byte[16];
        ReadOnlySpan<byte> kvcZeroIv = stackalloc byte[16];
        kcvInput.Fill(1);

        // Process all keys
        foreach (var key in new[]
                 {
                     staticKeys.ChannelEncryptionKey,
                     staticKeys.ChannelMacKey,
                     staticKeys.DataEncryptionKey
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
        byte p2 = (byte)(0x80 | keyReference.Id); // OR with 0x80 indicates that we're sending multiple keys

        var command = new PutKeyCommand((byte)replaceKvn, p2, commandData);
        var response = Connection.SendCommand(command);
        response.ThrowIfFailed("Error when importing key");

        var responseKcvData = response.GetData().Span;
        ReadOnlySpan<byte> expectedKcvData = expectedKcvStream.ToArray().AsSpan();
        ValidateCheckSum(expectedKcvData, responseKcvData);

        Logger.LogInformation("Successfully put static keys for Key Reference: {KeyReference}", keyReference);
    }

    /// <summary>
    ///     Puts an EC private key onto the YubiKey using the Security Domain.
    /// </summary>
    /// <param name="keyReference">The key reference identifying where to store the key.</param>
    /// <param name="privateKeyParameters">The EC private key parameters to store.</param>
    /// <param name="replaceKvn">The key version number to replace, or 0 for a new key (Default value is 0).</param>
    /// <exception cref="ArgumentException">Thrown when the private key is not of type NIST P-256.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no secure session is established.</exception>
    /// <exception cref="SecureChannelException">
    ///     Thrown when the new key set's checksum failed to verify, or some other SCP related error
    ///     described in the exception message.
    /// </exception>
    public void PutKey(KeyReference keyReference, ECPrivateKey privateKeyParameters, int replaceKvn = 0)
    {
        Logger.LogInformation("Importing SCP11 private key into Key Reference: {KeyReference}", keyReference);

        var privateKey = privateKeyParameters.Parameters;
        if (privateKey.Curve.Oid.Value != ECCurve.NamedCurves.nistP256.Oid.Value)
        {
            throw new ArgumentException("Private key must be of type NIST P-256");
        }

        try
        {
            // Prepare the command data
            using var commandDataStream = new MemoryStream();
            using var commandDataWriter = new BinaryWriter(commandDataStream);

            // Write the key version number
            commandDataWriter.Write(keyReference.VersionNumber);

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

            // Write the EC parameters
            var paramsTlv = new TlvObject(EcKeyType, new byte[] { 0x00 }).GetBytes();
            commandDataWriter.Write(paramsTlv.ToArray());
            commandDataWriter.Write((byte)0);

            // Create and send the command
            var command = new PutKeyCommand((byte)replaceKvn, keyReference.Id, commandDataStream.ToArray());
            var response = Connection.SendCommand(command);
            response.ThrowIfFailed("Error when importing key");

            // Get and validate the response
            var responseData = response.GetData();
            Span<byte> expectedResponseData = new[] { keyReference.VersionNumber };
            ValidateCheckSum(responseData.Span, expectedResponseData);

            Logger.LogInformation("Successfully put private key for Key Reference: {KeyReference}", keyReference);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to put private key for Key Reference: {KeyReference}", keyReference);
            throw;
        }
    }

    [Obsolete("Obsolete, use PutKey(KeyReference, ECPrivateKey)", false)]
    public void PutKey(KeyReference keyReference, ECPrivateKeyParameters privateKeyParameters, int replaceKvn = 0) =>
        PutKey(keyReference, privateKeyParameters as ECPrivateKey, replaceKvn);

    /// <summary>
    ///     Puts an EC public key onto the YubiKey using the Security Domain.
    /// </summary>
    /// <param name="keyReference">The key reference identifying where to store the key.</param>
    /// <param name="publicKey">The EC public key parameters to store.</param>
    /// <param name="replaceKvn">The key version number to replace, or 0 for a new key (Default value is 0).</param>
    /// <exception cref="ArgumentException">Thrown when the public key is not of type SECP256R1.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no secure session is established.</exception>
    /// <exception cref="SecureChannelException">
    ///     Thrown when the new key set's checksum failed to verify, or some other SCP related error
    ///     described in the exception message.
    /// </exception>
    public void PutKey(KeyReference keyReference, ECPublicKey publicKey, int replaceKvn = 0)
    {
        Logger.LogInformation("Importing SCP11 public key into KeyReference: {KeyReference}", keyReference);

        var pkParams = publicKey.Parameters;
        if (pkParams.Curve.Oid.Value != ECCurve.NamedCurves.nistP256.Oid.Value)
        {
            throw new ArgumentException("Public key must be of type NIST P-256");
        }

        try
        {
            using var commandDataMs = new MemoryStream();
            using var commandDataWriter = new BinaryWriter(commandDataMs);

            // Write the key version number
            commandDataWriter.Write(keyReference.VersionNumber);

            // Write the EC public key
            var publicKeyTlvData =
                new TlvObject(EcPublicKeyKeyType, publicKey.PublicPoint.Span).GetBytes();

            commandDataWriter.Write(publicKeyTlvData.ToArray());

            // Write the EC parameters
            var paramsTlv = new TlvObject(EcKeyType, new byte[1]).GetBytes();
            commandDataWriter.Write(paramsTlv.ToArray());
            commandDataWriter.Write((byte)0);

            // Create and send the command
            byte[] commandData = commandDataMs.ToArray();
            var command = new PutKeyCommand((byte)replaceKvn, keyReference.Id, commandData);
            var response = Connection.SendCommand(command);
            response.ThrowIfFailed("Error when importing key");

            // Get and validate the response
            var responseData = response.GetData();
            Span<byte> expectedResponseData = new[] { keyReference.VersionNumber };

            ValidateCheckSum(responseData.Span, expectedResponseData);

            Logger.LogInformation("Successfully put public key for KeyReference: {KeyReference}", keyReference);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to put public key for KeyReference: {KeyReference}", keyReference);
            throw;
        }
    }

    [Obsolete("Obsolete, use PutKey(KeyReference, ECPublicKey)", false)]
    public void PutKey(KeyReference keyReference, ECPublicKeyParameters publicKey, int replaceKvn = 0) =>
        PutKey(keyReference, publicKey as ECPublicKey, replaceKvn);

    /// <summary>
    ///     Delete one (or more) keys matching the specified criteria.
    /// </summary>
    /// <remarks>
    ///     All keys matching the given KID (Key ID) and/or KVN (Key Version Number) will be deleted,
    ///     where 0 is treated as a wildcard. For SCP03 keys, they can only be deleted by KVN.
    /// </remarks>
    /// <param name="keyReference">A reference to the key(s) to delete.</param>
    /// <param name="deleteLast">Must be true if deleting the final key, false otherwise.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown when both KID and KVN are 0, or when attempting to delete SCP03 keys by KID.
    /// </exception>
    /// <exception cref="SecureChannelException">
    ///     Thrown when the delete operation fails.
    /// </exception>
    public void DeleteKey(KeyReference keyReference, bool deleteLast = false)
    {
        if (keyReference.Id == 0 && keyReference.VersionNumber == 0)
        {
            throw new ArgumentException("At least one of KID, KVN must be nonzero");
        }

        Logger.LogInformation("Deleting keys (KeyReference: {KeyReference})", keyReference);

        byte kid = keyReference.Id;
        byte kvn = keyReference.VersionNumber;

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

        Logger.LogDebug("Deleting keys matching keys (KeyReference: {KeyReference})", keyReference);

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
        response.ThrowIfFailed("Error deleting key");

        Logger.LogInformation("Keys deleted (KeyReference: {KeyReference})", keyReference);
    }

    /// <summary>
    ///     Generate a new EC key pair for the given key reference.
    /// </summary>
    /// <remarks>
    ///     GlobalPlatform has no command to generate key pairs on the card itself. This is a
    ///     Yubico extension that tries to mimic the format of the GPC PUT KEY
    ///     command.
    /// </remarks>
    /// <param name="keyReference">The KID-KVN pair of the key that should be generated.</param>
    /// <param name="replaceKvn">
    ///     The key version number of the key set that should be replaced, or 0 to generate a new key
    ///     pair.
    /// </param>
    /// <returns>The parameters of the generated key, including the curve and the public point.</returns>
    /// <exception cref="SecureChannelException">Thrown when there was an SCP error, described in the exception message.</exception>
    public ECPublicKey GenerateEcKey(KeyReference keyReference, byte replaceKvn)
    {
        Logger.LogInformation(
            "Generating new key for {KeyReference}{ReplaceMessage}",
            keyReference,
            replaceKvn == 0
                ? string.Empty
                : $", replacing KVN=0x{replaceKvn:X2}");

        // Create tlv data for the command
        var ecParamsTlv = new TlvObject(EcKeyType, new byte[1]).GetBytes();
        byte[] generateEcCommandData = new byte[ecParamsTlv.Length + 1];
        generateEcCommandData[0] = keyReference.VersionNumber;
        ecParamsTlv.CopyTo(generateEcCommandData.AsMemory(1));

        // Create and send the command
        var command = new GenerateEcKeyCommand(replaceKvn, keyReference.Id, generateEcCommandData);
        var response = Connection.SendCommand(command);
        response.ThrowIfFailed("Error generating key");

        // Parse the response, extract the public point
        var tlvReader = new TlvReader(response.GetData());
        var encodedPoint = tlvReader.ReadValue(EcPublicKeyKeyType).Span;

        // Create the ECParameters with the public point
        var ecPublicKey = CreateECPublicKeyFromBytes(encodedPoint);

        Logger.LogInformation("Key generated (KeyReference: {KeyReference})", keyReference);
        return ecPublicKey;
    }

    /// <summary>
    ///     Store the SKI (Subject Key Identifier) for the CA of a given key.
    ///     Requires off-card entity verification.
    /// </summary>
    /// <param name="keyReference">A reference to the key for which to store the CA issuer.</param>
    /// <param name="ski">The Subject Key Identifier to store.</param>
    public void StoreCaIssuer(KeyReference keyReference, ReadOnlyMemory<byte> ski)
    {
        Logger.LogDebug("Storing CA issuer SKI (KeyReference: {KeyReference})", keyReference);

        byte klcc = 0; // Key Loading Card Certificate
        switch (keyReference.Id)
        {
            case ScpKeyIds.Scp11A:
            case ScpKeyIds.Scp11B:
            case ScpKeyIds.Scp11C:
                klcc = 1;
                break;
        }

        // Create and serialize data
        var caIssuerData = new TlvObject(
            ControlReferenceTag, TlvObjects.EncodeList(
                new List<TlvObject>
                {
                    new(0x80, new[] { klcc }),
                    new(0x42, ski.Span),
                    new(KidKvnTag, keyReference.GetBytes.Span)
                }
                )).GetBytes();

        // Send store data command
        StoreData(caIssuerData);

        Logger.LogInformation("CA issuer SKI stored (KeyReference: {KeyReference})", keyReference);
    }

    /// <summary>
    ///     Store a list of certificates associated with the given key reference using the GlobalPlatform STORE DATA command.
    /// </summary>
    /// <param name="keyReference">The key reference associated with the certificates.</param>
    /// <param name="certificates">The certificates to store.</param>
    /// <remarks>
    ///     The certificates will be stored in the order they are provided in the list.
    ///     <para>See GlobalPlatform Technology Card Specification v2.3.1 §11 APDU Command Reference for more information.</para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when certificatedata</exception>
    /// <exception cref="SecureChannelException">Thrown when there was an SCP error, described in the exception message.</exception>
    public void StoreCertificates(KeyReference keyReference, IReadOnlyList<X509Certificate2> certificates)
    {
        Logger.LogDebug("Storing certificate bundle (KeyReference: {KeyReference})", keyReference);

        // Write each certificate to a memory stream
        using var ms = new MemoryStream();
        foreach (var cert in certificates)
        {
            try
            {
                byte[] certTlvEncoded = cert.GetRawCertData(); // ASN.1 DER (TLV) encoded certificate
                ms.Write(certTlvEncoded, 0, certTlvEncoded.Length);
            }
            catch (CryptographicException e)
            {
                throw new ArgumentException("Failed to get encoded version of certificate", e);
            }
        }

        // Create and serialize data
        Memory<byte> certDataEncoded = TlvObjects.EncodeMany(
            new TlvObject(
                ControlReferenceTag, new TlvObject(KidKvnTag, keyReference.GetBytes.Span).GetBytes().Span),
            new TlvObject(CertificateStoreTag, ms.ToArray())
            );

        StoreData(certDataEncoded);

        Logger.LogInformation("Certificate bundle stored (KeyReference: {KeyReference})", keyReference);
    }

    /// <summary>
    ///     Stores an allowlist of certificate serial numbers for a specified key reference using the GlobalPlatform STORE DATA
    ///     command.
    /// </summary>
    /// <remarks>
    ///     This method requires off-card entity verification. If an allowlist is not stored, any
    ///     certificate signed by the CA can be used.
    ///     <para>See GlobalPlatform Technology Card Specification v2.3.1 §11 APDU Command Reference for more information.</para>
    /// </remarks>
    /// <param name="keyReference">A reference to the key for which the allowlist will be stored.</param>
    /// <param name="serials">
    ///     The list of certificate serial numbers (in hexadecimal string format) to be stored in the
    ///     allowlist for the given <see cref="KeyReference" />.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when a serial number cannot be encoded properly.</exception>
    /// <exception cref="SecureChannelException">Thrown when there was an SCP error, described in the exception message.</exception>
    public void StoreAllowlist(KeyReference keyReference, IReadOnlyCollection<string> serials)
    {
        Logger.LogDebug("Storing allow list (KeyReference: {KeyReference})", keyReference);

        using var ms = new MemoryStream();
        foreach (string? serial in serials)
        {
            try
            {
                byte[] serialAsBytes = Base16.DecodeText(serial);
                byte[] serialTlvEncoded = new TlvObject(SerialTag, serialAsBytes).GetBytes().ToArray();
                ms.Write(serialTlvEncoded, 0, serialTlvEncoded.Length);
            }
            catch (CryptographicException e)
            {
                throw new ArgumentException("Failed to get encoded version of certificate", e);
            }
        }

        Memory<byte> serialsDataEncoded = TlvObjects.EncodeMany(
            new TlvObject(
                ControlReferenceTag, new TlvObject(KidKvnTag, keyReference.GetBytes.Span).GetBytes().Span),
            new TlvObject(SerialsAllowListTag, ms.ToArray())
            );

        StoreData(serialsDataEncoded);

        Logger.LogInformation("Allow list stored (KeyReference: {KeyReference})", keyReference);
    }

    /// <summary>
    ///     Clears the allow list for the given <see cref="KeyReference" />
    /// </summary>
    /// <seealso cref="StoreAllowlist" />
    /// <param name="keyReference">The key reference that holds the allow list</param>
    public void ClearAllowList(KeyReference keyReference) => StoreAllowlist(keyReference, Array.Empty<string>());

    /// <summary>
    ///     Stores data in the Security Domain or targeted Application on the YubiKey using the GlobalPlatform STORE DATA
    ///     command.
    /// </summary>
    /// <remarks>
    ///     The STORE DATA command is used to transfer data to either the Security Domain itself or to an Application
    ///     being personalized. The data must be formatted as BER-TLV structures according to ISO 8825.
    ///     <para>
    ///         This implementation:
    ///         - Uses a single block transfer (P1.b8=1 indicating last block)
    ///         - Requires BER-TLV formatted data (P1.b5-b4=10)
    ///         - Does not provide encryption information (P1.b7-b6=00)
    ///     </para>
    ///     <para>See GlobalPlatform Technology Card Specification v2.3.1 §11 APDU Command Reference for more information.</para>
    ///     The <see cref="SecurityDomainSession" /> makes use of this method to store data in the Security Domain. Such as the
    ///     <see cref="StoreCaIssuer" />, <see cref="StoreCertificates" />, <see cref="StoreAllowlist" />, and other data.
    /// </remarks>
    /// <param name="data">
    ///     The data to be stored, which must be formatted as BER-TLV structures according to ISO 8825.
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no secure connection is available or the security context is invalid.
    /// </exception>
    /// <exception cref="SecureChannelException">Thrown when there was an SCP error, described in the exception message.</exception>
    public void StoreData(ReadOnlyMemory<byte> data)
    {
        Logger.LogInformation("Storing data with length:{Length}", data.Length);

        var command = new StoreDataCommand(data);
        var response = Connection.SendCommand(command);
        response.ThrowIfFailed("Error storing data");
    }

    /// <summary>
    ///     Retrieves the key information stored in the YubiKey and returns it in a dictionary format.
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
            var keyReference = new KeyReference(value.Span[0], value.Span[1]);
            var keyComponents = new Dictionary<byte, byte>();

            var currentValue = value.Span[2..];
            while (!currentValue.IsEmpty)
            {
                keyComponents.Add(currentValue[0], currentValue[1]);
                currentValue = currentValue[2..];
            }

            keyInformation.Add(keyReference, keyComponents);
        }

        Logger.LogInformation("Key information retrieved");

        return keyInformation;
    }

    /// <summary>
    ///     Retrieves the certificates associated with the given <paramref name="keyReference" />.
    /// </summary>
    /// <param name="keyReference">The key reference for which the certificates should be retrieved.</param>
    /// <returns>
    ///     A list of X.509 certificates associated with the key reference. The leaf certificate is the last certificate
    ///     in the list
    /// </returns>
    /// <exception cref="SecureChannelException">Thrown when there was an SCP error, described in the exception message.</exception>
    public IReadOnlyList<X509Certificate2> GetCertificates(KeyReference keyReference)
    {
        Logger.LogInformation("Getting certificates for key={KeyReference}", keyReference);

        var nestedTlv = new TlvObject(
            ControlReferenceTag,
            new TlvObject(KidKvnTag, keyReference.GetBytes.Span).GetBytes().Span
            ).GetBytes();

        var certificateTlvData = GetData(CertificateStoreTag, nestedTlv);
        var certificateTlvList = TlvObjects.DecodeList(certificateTlvData.Span);

        Logger.LogInformation("Certificates retrieved (KeyReference: {KeyReference})", keyReference);
        return certificateTlvList
            .Select(tlv => new X509Certificate2(tlv.GetBytes().ToArray()))
            .ToList();
    }

    /// <summary>
    ///     Gets the supported CA identifiers for KLOC and/or KLCC.
    /// </summary>
    /// <param name="kloc">Whether to retrieve Key Loading OCE Certificate (KLOC) identifiers.</param>
    /// <param name="klcc">Whether to retrieve Key Loading Card Certificate (KLCC) identifiers.</param>
    /// <returns>A dictionary of KeyReference and byte arrays representing the CA identifiers.</returns>
    /// <exception cref="ArgumentException">Thrown when both kloc and klcc are false.</exception>
    /// <exception cref="SecureChannelException">Thrown when there was an SCP error, described in the exception message.</exception>
    public IReadOnlyDictionary<KeyReference, ReadOnlyMemory<byte>> GetSupportedCaIdentifiers(bool kloc, bool klcc)
    {
        if (!kloc && !klcc)
        {
            throw new ArgumentException("At least one of kloc and klcc must be true");
        }

        Logger.LogDebug("Getting CA identifiers KLOC={Kloc}, KLCC={Klcc}", kloc, klcc);

        var ms = new MemoryStream();
        if (kloc)
        {
            var response = ExecuteGetDataCommand(CaKlocIdentifiersTag);
            switch (response.Status)
            {
                case ResponseStatus.Success:
                    var klocData = response.GetData();
                    ms.Write(klocData.Span.ToArray(), 0, klocData.Length);
                    break;
                case ResponseStatus.NoData: // A kloc might not be present
                    break;
                default:
                    response.ThrowIfFailed("Error getting kloc data");
                    break;
            }
        }

        if (klcc)
        {
            var response = ExecuteGetDataCommand(CaKlccIdentifiersTag);
            switch (response.Status)
            {
                case ResponseStatus.Success:
                    var klccData = response.GetData();
                    ms.Write(klccData.Span.ToArray(), 0, klccData.Length);
                    break;
                case ResponseStatus.NoData: // A klcc might not be present
                    break;
                default:
                    response.ThrowIfFailed("Error getting klcc data");
                    break;
            }
        }

        var caIdentifiers = new Dictionary<KeyReference, ReadOnlyMemory<byte>>();
        var caTlvObjects = TlvObjects.DecodeList(ms.ToArray()).ToArray().AsSpan();
        while (!caTlvObjects.IsEmpty)
        {
            var caIdentifierTlv = caTlvObjects[0];
            var keyReferenceTlv = caTlvObjects[1];

            var keyReferenceData = keyReferenceTlv.GetBytes().Span;
            var keyReference = new KeyReference(keyReferenceData[0], keyReferenceData[1]);
            caIdentifiers.Add(keyReference, caIdentifierTlv.GetBytes());

            caTlvObjects = caTlvObjects[2..];
        }

        Logger.LogInformation("CA identifiers retrieved");
        return caIdentifiers;
    }

    /// <summary>
    ///     Retrieves the card recognition data from the YubiKey device.
    /// </summary>
    /// <returns>The card recognition data as a byte array.</returns>
    /// <remarks>
    ///     The card recognition data is a TLV (Tag-Length-Value) encoded structure that contains information about the card.
    ///     See GlobalPlatform Technology Card Specification v2.3.1 §H.2 Structure of Card Recognition Data for more
    ///     information.
    /// </remarks>
    public ReadOnlyMemory<byte> GetCardRecognitionData()
    {
        Logger.LogInformation("Getting card recognition data");

        var tlvData = GetData(CardDataTag).Span;
        var cardRecognitionData = TlvObjects.UnpackValue(CardRecognitionDataTag, tlvData);

        Logger.LogInformation("Card recognition data retrieved");

        return cardRecognitionData;
    }

    /// <summary>
    ///     Gets data from the YubiKey associated with the given tag.
    /// </summary>
    /// <param name="tag">The tag of the data to retrieve.</param>
    /// <param name="data">Optional data to send with the command.</param>
    /// <remarks>
    ///     Sessions created from this constructor will not be able to perform operations which require authentication
    ///     <para>See GlobalPlatform Technology Card Specification v2.3.1 §11 APDU Command Reference for more information.</para>
    /// </remarks>
    /// <returns>The encoded tlv data retrieved from the YubiKey.</returns>
    /// <exception cref="SecureChannelException">Thrown when there was an SCP error, described in the exception message.</exception>
    public ReadOnlyMemory<byte> GetData(int tag, ReadOnlyMemory<byte>? data = null)
    {
        Logger.LogInformation("Getting data for tag {Tag}", tag);

        var response = ExecuteGetDataCommand(tag, data);
        response.ThrowIfFailed("Error getting data");

        Logger.LogDebug("Data for tag {Tag} retrieved", tag);
        return response.GetData();
    }

    /// <summary>
    ///     Perform a factory reset of the Security Domain.
    ///     This will remove all keys and associated data, as well as restore the default SCP03 static keys,
    ///     and generate a new (attestable) SCP11b key.
    /// </summary>
    public void Reset()
    {
        Logger.LogInformation("Resetting all SCP keys");

        var keys = GetKeyInformation().Keys;
        foreach (var keyReference in keys) // Reset is done by blocking all available keys
        {
            byte ins;
            var overridenKeyRef = keyReference;

            switch (keyReference.Id)
            {
                case ScpKeyIds.Scp03:
                    // SCP03 uses KID=0, we use KVN=0 to allow deleting the default keys
                    // which have an invalid KVN (0xFF).
                    overridenKeyRef = new KeyReference(0, 0);
                    ins = InitializeUpdateCommand.GpInitializeUpdateIns;
                    break;
                case 0x02:
                case 0x03:
                    continue; // Skip these as they are deleted by 0x01
                case ScpKeyIds.Scp11A:
                case ScpKeyIds.Scp11C:
                    ins = ExternalAuthenticateCommand.GpExternalAuthenticateIns;
                    break;
                case ScpKeyIds.Scp11B:
                    ins = InternalAuthenticateCommand.GpInternalAuthenticateIns;
                    break;
                default: // 0x10, 0x20-0x2F
                    ins = PerformSecurityOperationCommand.GpPerformSecurityOperationIns;
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

    private GetDataCommandResponse ExecuteGetDataCommand(int tag, ReadOnlyMemory<byte>? data = null)
    {
        var command = new GetDataCommand(tag, data);
        return Connection.SendCommand(command);
    }

    private static void ValidateCheckSum(ReadOnlySpan<byte> responseData, ReadOnlySpan<byte> expectedResponseData)
    {
        if (!CryptographicOperations.FixedTimeEquals(responseData, expectedResponseData))
        {
            throw new SecureChannelException(ExceptionMessages.ChecksumError);
        }
    }

    /// <summary>
    ///     Creates an instance from a byte array.
    /// </summary>
    /// <remarks>
    ///     The byte array is expected to be in the format 0x04 || X || Y
    ///     where X and Y are the uncompressed (32 bit) coordinates of the point.
    /// </remarks>
    /// <param name="bytes">The byte array.</param>
    /// <returns>An instance of EcPrivateKeyParameters with the nistP256 curve.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when the byte array is not in the expected format.
    ///     Either the first byte is not 0x04, or the byte array is not 65 bytes long (Key must be of type NIST P-256).
    /// </exception>
    private static ECPublicKey CreateECPublicKeyFromBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes[0] != 0x04)
        {
            throw new ArgumentException("The byte array must start with 0x04", nameof(bytes));
        }

        if (bytes.Length != 65)
        {
            throw new ArgumentException(
                "The byte array must be 65 bytes long (Key must be of type NIST P-256)", nameof(bytes));
        }

        var ecParameters = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = bytes.Slice(
                        1, 32)
                    .ToArray(), // Starts at 1 because the first byte is 0x04, indicating that it is an uncompressed point
                Y = bytes.Slice(33, 32).ToArray()
            }
        };

        return ECPublicKey.CreateFromParameters(ecParameters);
    }

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
    private const byte CardDataTag = 0x66;
    private const byte CardRecognitionDataTag = 0x73;
    private const ushort CertificateStoreTag = 0xBF21;
    private const ushort CaKlocIdentifiersTag = 0xFF33; // Key Loading OCE Certificate
    private const ushort CaKlccIdentifiersTag = 0xFF34; // Key Loading Card Certificate

    #endregion
}
