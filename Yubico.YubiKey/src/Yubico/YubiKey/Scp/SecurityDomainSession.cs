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
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Yubico.Core.Iso7816;
using Yubico.Core.Logging;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Scp.Commands;
using Yubico.YubiKit.Core.Util;

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
        /// Put the given key set onto the YubiKey.
        /// </summary>
        /// <remarks>
        /// See the <xref href="UsersManualScp">User's Manual entry</xref> on
        /// SCP.
        /// <para>
        /// On each YubiKey that supports SCP, there is space for three sets of
        /// keys. Each set contains three keys: "ENC", "MAC", and "DEK" (Channel
        /// Encryption, Channel MAC, and Data Encryption).
        /// <code language="adoc">
        ///    slot 1:   ENC   MAC   DEK
        ///    slot 2:   ENC   MAC   DEK
        ///    slot 3:   ENC   MAC   DEK
        /// </code>
        /// Each key is 16 bytes. YubiKeys do not support any other key size.
        /// </para>
        /// <para>
        /// Note that the standard allows changing one key in a key set. However,
        /// YubiKeys only allow calling this command with all three keys. That is,
        /// with a YubiKey, it is possible only to set or change all three keys of a
        /// set.
        /// </para>
        /// <para>
        /// Standard YubiKeys are manufactured with one key set, and each key in that
        /// set is the default value.
        /// <code language="adoc">
        ///    slot 1:   ENC(default)  MAC(default)  DEK(default)
        ///    slot 2:   --empty--
        ///    slot 3:   --empty--
        /// </code>
        /// The default value is 0x40 41 42 ... 4F.
        /// </para>
        /// <para>
        /// The key sets are not specified using a "slot number", rather, each key
        /// set is given a Key Version Number (KVN). Each key in the set is given a
        /// Key Identifier (KeyId). The YubiKey allows only 1, 2, and 3 as the
        /// KeyIds, and SDK users never need to worry about them. If the YubiKey
        /// contains the default key, the KVN is 255 (0xFF).
        /// <code language="adoc">
        ///    slot 1: KVN=0xff  KeyId=1:ENC(default)  KeyId=2:MAC(default)  KeyId=3:DEK(default)
        ///    slot 2:   --empty--
        ///    slot 3:   --empty--
        /// </code>
        /// </para>
        /// <para>
        /// It is possible to use this method to replace or add a key set. However,
        /// if the YubiKey contains only the initial, default keys, then it is only
        /// possible to replace that set. For example, suppose you have a YubiKey
        /// with the default keys and you try to set the keys in slot 2. The YubiKey
        /// will not allow that and will return an error.
        /// </para>
        /// <para>
        /// When you replace the initial, default keys, you must specify the KVN of
        /// the new keys. For the YubiKey, in this situation, the KVN must be 1.
        /// If you supply any other values for the KVN, the YubiKey will return
        /// an error. Hence, after replacing the initial, default keys, your
        /// three sets of keys will be the following:
        /// <code language="adoc">
        ///    slot 1: KVN=1  newENC  newMAC  newDEK
        ///    slot 2:   --empty--
        ///    slot 3:   --empty--
        /// </code>
        /// </para>
        /// <para>
        /// In order to add or change any key set, you must supply one of the existing
        /// key sets in order to build the SCP command and to encrypt and
        /// authenticate the new keys. When replacing the initial, default keys, you
        /// only have the choice to supply the keys with the KVN of 0xFF.
        /// </para>
        /// <para>
        /// Once you have replaced the original key set, you can use that set to add
        /// a second set to slot 2. It's KVN must be 2.
        /// <code language="adoc">
        ///    slot 1: KVN=1  ENC  MAC  DEK
        ///    slot 2: KVN=2  ENC  MAC  DEK
        ///    slot 3:   --empty--
        /// </code>
        /// </para>
        /// <para>
        /// You can use either key set to add a set to slot 3. You can use a key set
        /// to replace itself.
        /// </para>
        /// </remarks>
        /// <param name="keyParameters">
        /// The keys and KeyVersion Number of the set that will be loaded onto
        /// the YubiKey.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>newKeySet</c> argument is null.
        /// </exception>
        /// <exception cref="SecureChannelException">
        /// The new key set's checksum failed to verify, or some other error
        /// described in the exception message.
        /// </exception>
        public void PutKeySet(ScpKeyParameters keyParameters)
        {
            if (Connection is null)
            {
                throw new InvalidOperationException("No connection initialized. Use the other constructor");
            }

            _log.LogInformation("Put a new SCP key set onto a YubiKey.");

            if (keyParameters is null)
            {
                throw new ArgumentNullException(nameof(keyParameters));
            }

            //TODO PutKeyCommand for each, or make a generic one that handles all cases of Scp?
            var command = new PutKeyCommand(Connection.KeyParameters, keyParameters);
            var response = Connection.SendCommand(command);
            if (response.Status != ResponseStatus.Success)
            {
                throw new SecureChannelException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiKeyOperationFailed,
                        response.StatusMessage));
            }

            var checksum = response.GetData();
            if (!CryptographicOperations.FixedTimeEquals(checksum.Span, command.ExpectedChecksum.Span))
            {
                throw new SecureChannelException(ExceptionMessages.ChecksumError);
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

            const byte keyTypeEccKeyParamsTag = 0xF0;
            var paramsTlv = new TlvObject(keyTypeEccKeyParamsTag, new byte[] { 0 }).GetBytes();
            byte[] commandData = new byte[paramsTlv.Length + 1];
            commandData[0] = keyRef.VersionNumber;
            paramsTlv.CopyTo(commandData.AsMemory(1));

            var command = new GenerateEcKeyCommand(replaceKvn, keyRef.Id, commandData);
            var response = connection.SendCommand(command);
            if (response.Status != ResponseStatus.Success)
            {
                throw new SecureChannelException(response.StatusMessage);
            }

            const byte keyTypeEccPublicKeyTag = 0xB0;
            var tlvReader = new TlvReader(response.GetData());
            var encodedPoint = tlvReader.ReadValue(keyTypeEccPublicKeyTag).Span;

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
            var tlvDataList = TlvObjects.DecodeList(GetData(tagKeyInformation).Span);
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
        /// <returns>The data retrieved from the YubiKey.</returns>
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
