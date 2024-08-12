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
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Yubico.Core.Logging;
using Yubico.YubiKey.Scp03.Commands;

namespace Yubico.YubiKey.Scp03
{
    /// <summary>
    /// Create a session for managing the SCP03 configuration of a YubiKey.
    /// </summary>
    /// <remarks>
    /// See the <xref href="UsersManualScp03">User's Manual entry</xref> on SCP03.
    /// <para>
    /// Usually, you use SCP03 "in the background" to secure the communication
    /// with another application. For example, when you want to perform PIV
    /// operations, but need to send the commands to and get the responses from
    /// the YubiKey securely (such as sending commands remotely where
    /// authenticity and confidentiality are required), you use SCP03.
    /// <code language="csharp">
    ///   if (YubiKeyDevice.TryGetYubiKey(serialNumber, out IYubiKeyDevice yubiKeyDevice))
    ///   {
    ///       using (var pivSession = new PivSession(scp03Device, scp03Keys))
    ///       {
    ///         . . .
    ///       }
    ///   }
    /// </code>
    /// </para>
    /// <para>
    /// However, there are times you need to manage the configuration of SCP03
    /// directly, not as simply the security layer for a PIV or other
    /// applications. The most common operations are loading and deleting SCP03
    /// key sets on the YubiKey.
    /// </para>
    /// <para>
    /// For the SCP03 configuration management operations, use the
    /// <c>Scp03Session</c> class.
    /// </para>
    /// <para>
    /// Once you have the YubiKey to use, you will build an instance of this
    /// <c>Scp03Session</c> class to represent the SCP03 on the hardware.
    /// Because this class implements <c>IDisposable</c>, use the <c>using</c>
    /// keyword. For example,
    /// <code language="csharp">
    ///   if (YubiKeyDevice.TryGetYubiKey(serialNumber, out IYubiKeyDevice yubiKeyDevice))
    ///   {
    ///       var scp03Keys = new StaticKeys();
    ///       using (var scp03 = new Scp03Session(yubiKeyDevice, scp03Keys))
    ///       {
    ///           // Perform SCP03 operations.
    ///       }
    ///   }
    /// </code>
    /// </para>
    /// <para>
    /// If the YubiKey does not support SCP03, the constructor will throw an
    /// exception.
    /// </para>
    /// <para>
    /// If the StaticKeys provided are not correct, the constructor will throw an
    /// exception.
    /// </para>
    /// </remarks>
    public sealed class Scp03Session : IDisposable
    {
        private bool _disposed;
        private readonly ILogger _log = Log.GetLogger<Scp03Session>();

        /// <summary>
        /// The object that represents the connection to the YubiKey. Most
        /// applications will ignore this, but it can be used to call Commands
        /// directly.
        /// </summary>
        public IScp03YubiKeyConnection Connection { get; private set; }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private Scp03Session()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create an instance of <see cref="Scp03Session"/>, the object that
        /// represents SCP03 on the YubiKey.
        /// </summary>
        /// <remarks>
        /// See the <xref href="UsersManualScp03">User's Manual entry</xref> on SCP03.
        /// <para>
        /// Because this class implements <c>IDisposable</c>, use the <c>using</c>
        /// keyword. For example,
        /// <code language="csharp">
        ///   if (YubiKeyDevice.TryGetYubiKey(serialNumber, out IYubiKeyDevice yubiKeyDevice))
        ///   {
        ///       var staticKeys = new StaticKeys();
        ///       // Note that you do not need to call the "WithScp03" method when
        ///       // using the Scp03Session class.
        ///       using (var scp03 = new Scp03Session(yubiKeyDevice, staticKeys))
        ///       {
        ///           // Perform SCP03 operations.
        ///       }
        ///   }
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="yubiKey">
        /// The object that represents the actual YubiKey which will perform the
        /// operations.
        /// </param>
        /// <param name="scp03Keys">
        /// The shared secret keys that will be used to authenticate the caller
        /// and encrypt the communications. This constructor will make a deep
        /// copy of the keys, it will not copy a reference to the object.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>yubiKey</c> or <c>scp03Keys</c> argument is null.
        /// </exception>
        public Scp03Session(IYubiKeyDevice yubiKey, StaticKeys scp03Keys)
        {
            _log.LogInformation("Create a new instance of Scp03Session.");
            if (yubiKey is null)
            {
                throw new ArgumentNullException(nameof(yubiKey));
            }
            if (scp03Keys is null)
            {
                throw new ArgumentNullException(nameof(scp03Keys));
            }

            Connection = yubiKey.ConnectScp03(YubiKeyApplication.Scp03.GetIso7816ApplicationId(), scp03Keys);
            _disposed = false;
        }

        /// <summary>
        /// Put the given key set onto the YubiKey.
        /// </summary>
        /// <remarks>
        /// See the <xref href="UsersManualScp03">User's Manual entry</xref> on
        /// SCP03.
        /// <para>
        /// On each YubiKey that supports SCP03, there is space for three sets of
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
        /// key sets in order to build the SCP03 command and to encrypt and
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
        /// <param name="newKeySet">
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
        public void PutKeySet(StaticKeys newKeySet)
        {
            _log.LogInformation("Put a new SCP03 key set onto a YubiKey.");

            if (newKeySet is null)
            {
                throw new ArgumentNullException(nameof(newKeySet));
            }
            var cmd = new PutKeyCommand(Connection.GetScp03Keys(), newKeySet);
            PutKeyResponse rsp = Connection.SendCommand(cmd);
            if (rsp.Status != ResponseStatus.Success)
            {
                throw new SecureChannelException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiKeyOperationFailed,
                        rsp.StatusMessage));
            }

            ReadOnlyMemory<byte> checksum = rsp.GetData();
            if (!CryptographicOperations.FixedTimeEquals(checksum.Span, cmd.ExpectedChecksum.Span))
            {
                throw new SecureChannelException(ExceptionMessages.ChecksumError);
            }
        }

        /// <summary>
        /// Delete the key set with the given <c>keyVersionNumber</c>. If the key
        /// set to delete is the last SCP03 key set on the YubiKey, pass
        /// <c>true</c> as the <c>isLastKey</c> arg.
        /// </summary>
        /// <remarks>
        /// The key set used to create the SCP03 session cannot be the key set to
        /// be deleted, unless both of the other key sets have been deleted, and
        /// you pass <c>true</c> for <c>isLastKey</c>. In this case, the key will
        /// be deleted but the SCP03 application on the YubiKey will be reset
        /// with the default key.
        /// </remarks>
        /// <param name="keyVersionNumber">
        /// The number specifying which key set to delete.
        /// </param>
        /// <param name="isLastKey">
        /// If this key set is the last SCP03 key set on the YubiKey, pass
        /// <c>true</c>, otherwise, pass <c>false</c>. This arg has a default of
        /// <c>false</c> so if no argument is given, it will be <c>false</c>.
        /// </param>
        public void DeleteKeySet(byte keyVersionNumber, bool isLastKey = false)
        {
            _log.LogInformation("Delete an SCP03 key set from a YubiKey.");

            var cmd = new DeleteKeyCommand(keyVersionNumber, isLastKey);
            Scp03Response rsp = Connection.SendCommand(cmd);
            if (rsp.Status != ResponseStatus.Success)
            {
                throw new SecureChannelException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiKeyOperationFailed,
                        rsp.StatusMessage));
            }
        }

        /// <summary>
        /// When the Scp03Session object goes out of scope, this method is called.
        /// It will close the session. The most important function of closing a
        /// session is to close the connection.
        /// </summary>
        // Note that .NET recommends a Dispose method call Dispose(true) and
        // GC.SuppressFinalize(this). The actual disposal is in the
        // Dispose(bool) method.
        //
        // However, that does not apply to sealed classes.
        // So the Dispose method will simply perform the
        // "closing" process, no call to Dispose(bool) or GC.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Connection.Dispose();

            _disposed = true;
        }
    }
}
