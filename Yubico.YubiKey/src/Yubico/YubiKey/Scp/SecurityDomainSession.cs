// Copyright 2023 Yubico AB
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
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
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
    public sealed class SecurityDomainSession : IDisposable
    {
        private const byte KeyTypeEccPrivateKeyTag = 0xB1;
        private const byte KeyTypeEccKeyParamsTag = 0xF0;
        private const byte KeyTypeEccPublicKeyTag = 0xB0;
        private const byte KeyTypeAesTag = 0x88;

        private readonly IYubiKeyDevice _yubiKey;
        private readonly ILogger _log = Log.GetLogger<SecurityDomainSession>();
        private bool _disposed;

        /// <summary>
        /// The object that represents the connection to the YubiKey. Most
        /// applications will ignore this, but it can be used to call Commands
        /// directly.
        /// </summary>
        private IScpYubiKeyConnection? Connection { get; }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private SecurityDomainSession()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create an instance of <see cref="SecurityDomainSession"/>, the object that
        /// represents SCP on the YubiKey.
        /// </summary>
        /// <remarks>
        /// See the <xref href="UsersManualScp">User's Manual entry</xref> on SCP.
        /// <para>
        /// Because this class implements <c>IDisposable</c>, use the <c>using</c>
        /// keyword. For example,
        /// <code language="csharp">
        ///   if (YubiKeyDevice.TryGetYubiKey(serialNumber, out IYubiKeyDevice yubiKeyDevice))
        ///   {
        ///       var staticKeys = new StaticKeys();
        ///       // Note that you do not need to call the "WithScp" method when
        ///       // using the ScpSession class.
        ///       using (var scp = new ScpSession(yubiKeyDevice, staticKeys))
        ///       {
        ///           // Perform SCP operations.
        ///       }
        ///   }
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="yubiKey">
        /// The object that represents the actual YubiKey which will perform the
        /// operations.
        /// </param>
        /// <param name="scpKeys">
        /// The shared secret keys that will be used to authenticate the caller
        /// and encrypt the communications. This constructor will make a deep
        /// copy of the keys, it will not copy a reference to the object.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>yubiKey</c> or <c>scpKeys</c> argument is null.
        /// </exception>
        public SecurityDomainSession(IYubiKeyDevice yubiKey, ScpKeyParameters scpKeys)
        {
            _log.LogInformation("Create a new instance of ScpSession.");

            if (yubiKey is null)
            {
                throw new ArgumentNullException(nameof(yubiKey));
            }

            if (scpKeys is null)
            {
                throw new ArgumentNullException(nameof(scpKeys));
            }

            _yubiKey = yubiKey;
            Connection = yubiKey.ConnectScp(YubiKeyApplication.SecurityDomain, scpKeys);
        }

        /// <summary>
        /// Create an unauthenticated instance of <see cref="SecurityDomainSession"/>, the object that
        /// represents SCP on the YubiKey.
        /// </summary>
        /// <remarks>Sessions created from this constructor will not be able to perform operations which require authentication</remarks>
        /// <param name="yubiKey">
        /// The object that represents the actual YubiKey which will perform the
        /// operations.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>yubiKey</c> or <c>scpKeys</c> argument is null.
        /// </exception>
        public SecurityDomainSession(IYubiKeyDevice yubiKey)
        {
            _log.LogInformation("Create a new instance of ScpSession.");
            _yubiKey = yubiKey ?? throw new ArgumentNullException(nameof(yubiKey));
        }

        /// <summary>
        /// Puts an SCP03 key set onto the YubiKey using the Security Domain.
        /// </summary>
        /// <param name="keyRef">The key reference identifying where to store the key.</param>
        /// <param name="newKeySet">The new SCP03 key set to store.</param>
        /// <param name="replaceKvn">The key version number to replace, or 0 for a new key.</param>
        /// <exception cref="ArgumentException">Thrown when the KID is not 0x01 for SCP03 key sets.</exception>
        /// <exception cref="SecureChannelException">Thrown when the new key set's checksum failed to verify, or some other error
        /// described in the exception message.</exception>
        public void PutKeySet(KeyReference keyRef, StaticKeys newKeySet, int replaceKvn)
        {
            _log.LogInformation("Importing SCP03 key set into KeyRef {KeyRef}", keyRef);

            if (keyRef.Id != ScpKid.Scp03)
            {
                throw new ArgumentException("KID must be 0x01 for SCP03 key sets");
            }

            var connection = Connection ?? throw new InvalidOperationException("No connection initialized");
            var encryptor = connection.DataEncryptor ?? throw new InvalidOperationException("No session DEK available");

            using var dataStream = new MemoryStream();
            using var dataWriter = new BinaryWriter(dataStream);
            using var expectedKcvStream = new MemoryStream();
            using var expectedKcvWriter = new BinaryWriter(expectedKcvStream);

            // Write KVN
            dataWriter.Write(keyRef.VersionNumber);
            expectedKcvWriter.Write(keyRef.VersionNumber);

            Span<byte> kcvInput = stackalloc byte[16] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            ReadOnlySpan<byte> kvcZeroIv = stackalloc byte[16];

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
                var encryptedKey = encryptor(key);

                // Write key structure
                var tlvData = new TlvObject(KeyTypeAesTag, encryptedKey.Span.ToArray()).GetBytes();
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
            var response = connection.SendCommand(command);
            if (response.Status != ResponseStatus.Success)
            {
                throw new SecureChannelException(
                    string.Format(
                        CultureInfo.CurrentCulture, ExceptionMessages.YubiKeyOperationFailed, response.StatusMessage));
            }

            var responseKcvData = response.GetData().Span;
            ReadOnlySpan<byte> expectedKcvData = expectedKcvStream.ToArray().AsSpan();
            if (!CryptographicOperations.FixedTimeEquals(responseKcvData, expectedKcvData))
            {
                throw new SecureChannelException(ExceptionMessages.ChecksumError);
            }
        }

        /// <summary>
        /// Puts an ECC private key onto the YubiKey using the Security Domain.
        /// </summary>
        /// <param name="keyRef">The key reference identifying where to store the key.</param>
        /// <param name="secretKey">The ECC private key parameters to store.</param>
        /// <param name="replaceKvn">The key version number to replace, or 0 for a new key.</param>
        /// <exception cref="ArgumentException">Thrown when the private key is not of type SECP256R1.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no secure session is established.</exception>
        public void PutKeySet(KeyReference keyRef, ECParameters secretKey, int replaceKvn)
        {
            _log.LogInformation("Importing SCP11 private key into KeyRef {KeyRef}", keyRef);

            var connection = Connection ?? throw new InvalidOperationException(
                "No secure session established. Connection required for key import.");

            var encryptor = connection.DataEncryptor ?? throw new InvalidOperationException(
                "No secure session established. DataEncryptor required for key import.");

            if (secretKey.Curve.Oid.Value != ECCurve.NamedCurves.nistP256.Oid.Value)
            {
                throw new ArgumentException("Private key must be of type SECP256R1");
            }

            try
            {
                // Prepare the command data
                using var dataStream = new MemoryStream();
                using var dataWriter = new BinaryWriter(dataStream);

                // Write the key version number
                dataWriter.Write(keyRef.VersionNumber);

                // Convert the private key to bytes and encrypt it
                var privateKeyBytes = secretKey.D.AsMemory();
                try
                {
                    // Must be encrypted with the active sessions data encryption key
                    var encryptedKey = encryptor(privateKeyBytes);
                    var privateKeyTlv = new TlvObject(KeyTypeEccPrivateKeyTag, encryptedKey.Span).GetBytes();
                    dataWriter.Write(privateKeyTlv.ToArray());
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(privateKeyBytes.Span);
                }

                // Write the ECC parameters (currently just 0x00 as per Java implementation)
                var paramsTlv = new TlvObject(KeyTypeEccKeyParamsTag, new byte[] { 0x00 }).GetBytes();
                dataWriter.Write(paramsTlv.ToArray());
                dataWriter.Write((byte)0);

                // Create and send the command
                var command = new PutKeyCommand((byte)replaceKvn, keyRef.Id, dataStream.ToArray());
                var response = connection.SendCommand(command);
                if (response.Status != ResponseStatus.Success)
                {
                    throw new SecureChannelException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.YubiKeyOperationFailed,
                            response.StatusMessage));
                }

                // Get the response
                var responseData = response.GetData();
                Span<byte> expectedResponseData = new[] { keyRef.VersionNumber };
                if (!CryptographicOperations.FixedTimeEquals(responseData.Span, expectedResponseData))
                {
                    throw new SecureChannelException("Incorrect key check value");
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to put key set for KeyRef {KeyRef}", keyRef);
                throw;
            }
        }

        /// <summary>
        /// Delete the key set with the given <c>keyVersionNumber</c>. If the key
        /// set to delete is the last SCP key set on the YubiKey, pass
        /// <c>true</c> as the <c>isLastKey</c> arg.
        /// </summary>
        /// <remarks>
        /// The key set used to create the SCP session cannot be the key set to
        /// be deleted, unless both of the other key sets have been deleted, and
        /// you pass <c>true</c> for <c>isLastKey</c>. In this case, the key will
        /// be deleted but the SCP application on the YubiKey will be reset
        /// with the default key.
        /// </remarks>
        /// <param name="keyVersionNumber">
        /// The number specifying which key set to delete.
        /// </param>
        /// <param name="isLastKey">
        /// If this key set is the last SCP key set on the YubiKey, pass
        /// <c>true</c>, otherwise, pass <c>false</c>. This arg has a default of
        /// <c>false</c> so if no argument is given, it will be <c>false</c>.
        /// </param>
        public void DeleteKeySet(byte keyVersionNumber, bool isLastKey = false)
        {
            if (Connection is null)
            {
                throw new InvalidOperationException("No connection initialized. Use the other constructor");
            }

            _log.LogInformation("Deleting an SCP key set from a YubiKey.");

            var command = new DeleteKeyCommand(keyVersionNumber, isLastKey);
            var response = Connection.SendCommand(command);
            if (response.Status != ResponseStatus.Success)
            {
                throw new SecureChannelException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiKeyOperationFailed,
                        response.StatusMessage));
            }
        }

        /// <summary>
        /// Generate a new ECC key pair for the given key reference.
        /// </summary>
        /// <remarks>
        /// GlobalPlatform has no command to generate key pairs on the card itself. This is a
        /// Yubico extension that tries to mimic the format of the GPC PUT KEY
        /// command.
        /// </remarks>
        /// <param name="keyRef">The KID-KVN pair of the key that should be generated.</param>
        /// <param name="replaceKvn">The key version number of the key set that should be replaced, or 0 to generate a new key pair.</param>
        /// <returns>The parameters of the generated key, including the curve and the public point.</returns>
        /// 
        public ECParameters GenerateEcKey(KeyReference keyRef, byte replaceKvn)
        {
            var connection = Connection ??
                throw new InvalidOperationException("No connection initialized. Use the other constructor");

            _log.LogDebug(
                "Generating new key for {KeyRef}{ReplaceMessage}",
                keyRef,
                replaceKvn == 0
                    ? string.Empty
                    : $", replacing KVN=0x{replaceKvn:X2}");

            // Create tlv data for the command
            var paramsTlv = new TlvObject(KeyTypeEccKeyParamsTag, new byte[] { 0 }).GetBytes();
            byte[] commandData = new byte[paramsTlv.Length + 1];
            commandData[0] = keyRef.VersionNumber;
            paramsTlv.CopyTo(commandData.AsMemory(1));

            // Create and send the command
            var command = new GenerateEcKeyCommand(replaceKvn, keyRef.Id, commandData);
            var response = connection.SendCommand(command);
            if (response.Status != ResponseStatus.Success)
            {
                throw new SecureChannelException(response.StatusMessage);
            }

            // Parse the response, extract the public point
            var tlvReader = new TlvReader(response.GetData());
            var encodedPoint = tlvReader.ReadValue(KeyTypeEccPublicKeyTag).Span;

            // Create the ECParameters object with the public point
            return new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = encodedPoint.Slice(1, 32).ToArray(),
                    Y = encodedPoint.Slice(33, 32).ToArray()
                }
            };
        }

        /// <summary>
        /// Perform a factory reset of the Security Domain.
        /// This will remove all keys and associated data, as well as restore the default SCP03 static keys,
        /// and generate a new (attestable) SCP11b key.
        /// </summary>
        public void Reset()
        {
            _log.LogDebug("Resetting all SCP keys");

            var connection = _yubiKey.Connect(YubiKeyApplication.SecurityDomain);

            const byte insInitializeUpdate = 0x50;
            const byte insExternalAuthenticate = 0x82;
            const byte insInternalAuthenticate = 0x88;
            const byte insPerformSecurityOperation = 0x2A;

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
                        ins = insInitializeUpdate;
                        break;
                    case 0x02:
                    case 0x03:
                        continue; // Skip these as they are deleted by 0x01
                    case ScpKid.Scp11a:
                    case ScpKid.Scp11c:
                        ins = insExternalAuthenticate;
                        break;
                    case ScpKid.Scp11b:
                        ins = insInternalAuthenticate;
                        break;
                    default: // 0x10, 0x20-0x2F
                        ins = insPerformSecurityOperation;
                        break;
                }

                // Keys have 65 attempts before blocking (and thus removal)
                for (int i = 0; i < 65; i++)
                {
                    var result = connection.SendCommand(
                        new ResetCommand(ins, overridenKeyRef.VersionNumber, overridenKeyRef.Id, new byte[8]));

                    switch (result.StatusWord)
                    {
                        case SWConstants.AuthenticationMethodBlocked:
                        case SWConstants.SecurityStatusNotSatisfied:
                            i = 65;
                            break;
                        case SWConstants.InvalidCommandDataParameter:
                            continue;
                        default: continue;
                    }
                }
            }

            _log.LogInformation("SCP keys reset");
        }

        /// <summary>
        /// Retrieves the key information stored in the YubiKey and returns it in a dictionary format.
        /// </summary>
        /// <returns>A read only dictionary containing the KeyReference as the key and a dictionary of key components as the value.</returns>
        public IReadOnlyDictionary<KeyReference, Dictionary<byte, byte>> GetKeyInformation()
        {
            const byte tagKeyInformation = 0xE0;

            var keys = new Dictionary<KeyReference, Dictionary<byte, byte>>();
            var getDataResult = GetData(tagKeyInformation).Span;
            var tlvDataList = TlvObjects.DecodeList(getDataResult);
            foreach (var tlvObject in tlvDataList)
            {
                var value = TlvObjects.UnpackValue(0xC0, tlvObject.GetBytes().Span);
                var keyRef = new KeyReference(value.Span[0], value.Span[1]);
                var keyComponents = new Dictionary<byte, byte>();

                while (!(value = value[2..]).IsEmpty)
                {
                    keyComponents.Add(value.Span[0], value.Span[1]);
                }

                keys.Add(keyRef, keyComponents);
            }

            return new ReadOnlyDictionary<KeyReference, Dictionary<byte, byte>>(keys);
        }

        /// <summary>
        /// Retrieves the certificates associated with the given <paramref name="keyReference"/>.
        /// </summary>
        /// <param name="keyReference">The key reference for which the certificates should be retrieved.</param>
        /// <returns>A list of X.509 certificates associated with the key reference.</returns>
        public IReadOnlyList<X509Certificate2> GetCertificates(KeyReference keyReference)
        {
            _log.LogInformation("Getting certificates for key={KeyRef}", keyReference);

            const int certificateStoreTag = 0xBF21;
            const int controlReferenceTemplateTag = 0xA6;
            const int kidKvnTag = 0x83;

            var nestedTlv = new TlvObject(
                controlReferenceTemplateTag,
                new TlvObject(kidKvnTag, keyReference.GetBytes).GetBytes().Span
                ).GetBytes();

            var certificateTlvData = GetData(certificateStoreTag, nestedTlv);
            var certificateTlvList = TlvObjects.DecodeList(certificateTlvData.Span);

            return certificateTlvList
                .Select(tlv => new X509Certificate2(tlv.GetBytes().ToArray()))
                .ToList();
        }

        /// <summary>
        /// Gets data from the YubiKey associated with the given tag.
        /// </summary>
        /// <param name="tag">The tag of the data to retrieve.</param>
        /// <param name="data">Optional data to send with the command.</param>
        /// <returns>The encoded tlv data retrieved from the YubiKey. This will have to be decoded</returns>
        public ReadOnlyMemory<byte> GetData(int tag, ReadOnlyMemory<byte>? data = null)
        {
            var connection = Connection ?? _yubiKey.Connect(YubiKeyApplication.SecurityDomain);
            var response = connection.SendCommand(new GetDataCommand(tag, data));

            return response.GetData();
        }

        /// <summary>
        /// When the ScpSession object goes out of scope, this method is called.
        /// It will close the session. The most important function of closing a
        /// session is to close the connection.
        /// </summary>

        // Note that .NET recommends a Dispose method call Dispose(true) and
        // GC.SuppressFinalize(this). The actual disposal is in the
        // Dispose(bool) method.
        // However, that does not apply to sealed classes.
        // So the Dispose method will simply perform the
        // "closing" process, no call to Dispose(bool) or GC.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Connection?.Dispose();

            _disposed = true;
        }
    }
}
