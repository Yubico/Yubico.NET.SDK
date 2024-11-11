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
using System.Security;
using Microsoft.Extensions.Logging;
using Yubico.Core.Iso7816;
using Yubico.Core.Logging;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Scp;

namespace Yubico.YubiKey.Piv
{
    /// <summary>
    ///     Create a session and perform PIV operations within that session.
    /// </summary>
    /// <remarks>
    ///     When you need to perform PIV operations, instantiate this class to create
    ///     a session, then call on the methods in the class.
    ///     <para>
    ///         Generally you will choose the YubiKey to use by building an instance of
    ///         <see cref="IYubiKeyDevice" />. This object will represent the actual
    ///         hardware.
    ///         <code language="csharp">
    ///             IYubiKeyDevice SelectYubiKey()
    ///             {
    ///                 IEnumerable&lt;IYubiKeyDevice&gt; yubiKeyList = YubiKey.FindAll();
    ///                 foreach (IYubiKeyDevice current in yubiKeyList)
    ///                 {
    ///                     /* determine which YubiKey to use */
    ///                     if(selected)
    ///                     {
    ///                         return current;
    ///                     }
    ///                 }
    ///             }
    ///         </code>
    ///     </para>
    ///     <para>
    ///         Once you have the YubiKey to use, you will build an instance of this
    ///         <c>PivSession</c> class to represent the PIV application on the hardware.
    ///         Because this class implements <c>IDisposable</c>, use the <c>using</c>
    ///         keyword. For example,
    ///         <code language="csharp">
    ///             IYubiKeyDevice yubiKeyToUse = SelectYubiKey();
    ///             using (var piv = new PivSession(yubiKeyToUse))
    ///             {
    ///                 /* Perform PIV operations. */
    ///             }
    ///         </code>
    ///     </para>
    ///     <para>
    ///         If this class is used as part of a <c>using</c> expression, when the
    ///         session goes out of scope, the <c>Dispose</c> method will be called to
    ///         dispose the active PIV session, clearing any authenticated state, and
    ///         ultimately releasing the connection to the YubiKey.
    ///     </para>
    ///     <para>
    ///         Note that while a session is open, any management key authentication
    ///         and PIN verification will be active. That is, you need to authenticate
    ///         the management key or verify the PIN only once per session. Touch might
    ///         be needed more than once.
    ///     </para>
    ///     <para>
    ///         There are some exceptions to the "verify the PIN once per session" rule.
    ///         For example, to change a PIN, you need to enter the current and new PIN,
    ///         even if the current PIN has been verified. The documentation for each
    ///         method in this class will indicate if the PIN is needed, and if so, must
    ///         it be verified or entered. See also the User's Manual entries on the
    ///         <xref href="UsersManualPinPukMgmtKey"> PIV PIN, PUK, and 
    ///         Management Key</xref>
    ///         and <xref href="UsersManualPivAccessControl"> PIV commands access
    ///         control</xref>.
    ///     </para>
    ///     <para>
    ///         Note that PIN/PUK/Management Key verification or authentication will
    ///         happen automatically when you call a method that needs it. You can
    ///         call the <c>TryVerifyPin</c> or <c>TryAuthenticateManagementKey</c>
    ///         methods if you want, but any method that can be executed only if the PIN
    ///         and/or management key has been verified or authenticated will determine
    ///         if the appropriate values have been verified/authenticated, and if not,
    ///         will make the appropriate calls to do so. The caller must supply a
    ///         PIN/PUK/management key collector delegate (callback).
    ///     </para>
    ///     <para>
    ///         This class needs a delegate (callback): a method to enter the PIN, PUK,
    ///         or management key. Although some operations will not need this delegate,
    ///         any useful application will almost certainly call one or more methods
    ///         that do need it. You will need to set the appropriate property after
    ///         instantiating this class.
    ///         <code language="csharp">
    ///             using (var pivSession = new PivSession(yubiKeyToUse))
    ///             {
    ///                 KeyCollector = SomeCollectorDelegate;
    ///             };
    ///         </code>
    ///     </para>
    ///     <para>
    ///         You supply the delegate as the <c>KeyCollector</c> property. See also the
    ///         User's Manual entry on
    ///         <xref href="UsersManualDelegatesInSdk"> delegates in the SDK</xref>.
    ///     </para>
    ///     <para>
    ///         Note that the YubiKey is manufactured with default PIN, PUK, and
    ///         management key values. This is a requirement of the PIV standard.
    ///         <code>
    ///             management key (hex): 01 02 03 04 05 06 07 08
    ///                                   01 02 03 04 05 06 07 08
    ///                                   01 02 03 04 05 06 07 08
    ///         </code>
    ///         <code>
    ///             PIN (hex): 31 32 33 34 35 36
    ///             as an ASCII string, this would be "123456"
    ///         </code>
    ///         <code>
    ///             PUK (hex): 31 32 33 34 35 36 37 38
    ///             as an ASCII string, this would be "12345678"
    ///         </code>
    ///     </para>
    ///     <para>
    ///         The PIN, PUK, and management key are supplied as byte arrays. The reason
    ///         is that they can be binary data (they are not necessarily strings, ASCII
    ///         or otherwise), and a byte array can be overwritten to limit the contents'
    ///         exposure. See the User's Manual entries on
    ///         <xref href="UsersManualSensitive"> sensitive data</xref>.
    ///     </para>
    ///     <para>
    ///         This class will also need a random number generator and a Triple-DES
    ///         encryptor/decryptor. It will get them from
    ///         <see cref="CryptographyProviders" />. That class will return default
    ///         implementations, unless you replace them. Very few applications will
    ///         choose to replace the defaults, but if you want to, see the documentation
    ///         for that class and the User's Manual entry on
    ///         <xref href="UsersManualAlternateCrypto"> alternate crypto
    ///         implementations</xref>
    ///         to learn how to do so.
    ///     </para>
    /// </remarks>
    public sealed partial class PivSession : ApplicationSession
    {
        private bool _disposed;

        /// <summary>
        ///     Create an instance of <c>PivSession</c>, the object that represents
        ///     the PIV application on the YubiKey. The communication between the SDK
        ///     and the YubiKey will be protected by SCP03.
        /// </summary>
        /// <remarks>
        ///     See the User's Manual entry on
        ///     <xref href="UsersManualScp03"> SCP03 </xref> for more information on
        ///     this communication protocol.
        ///     <para>
        ///         Because this class implements <c>IDisposable</c>, use the <c>using</c>
        ///         keyword. For example,
        ///         <code language="csharp">
        ///             IYubiKeyDevice yubiKeyToUse = SelectYubiKey();
        ///             // Assume you have some method that obtains the appropriate SCP03
        ///             // key set.
        ///             using StaticKeys scp03Keys = CollectScp03Keys();
        ///             using (var piv = new PivSession(yubiKeyToUse, scp03Keys))
        ///             {
        ///                 /* Perform PIV operations. */
        ///             }
        ///         </code>
        ///     </para>
        /// </remarks>
        /// <param name="yubiKey">
        ///     The object that represents the actual YubiKey which will perform the
        ///     operations.
        /// </param>
        /// <param name="scp03Keys">
        ///     The SCP03 key set to use in establishing the connection.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The <c>yubiKey</c> argument is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     This exception is thrown when unable to determine the management key type.
        /// </exception>
        [Obsolete("Use new Scp")]
        public PivSession(IYubiKeyDevice yubiKey, Yubico.YubiKey.Scp03.StaticKeys scp03Keys)
            : this(yubiKey, scp03Keys.ConvertToScp03KeyParameters())
        {
        }

        /// <summary>
        ///     Create an instance of <c>PivSession</c>, the object that represents
        ///     the PIV application on the YubiKey. The communication between the SDK
        ///     and the YubiKey will be protected by SCP03.
        /// </summary>
        /// <remarks>
        ///     See the User's Manual entry on
        ///     <xref href="UsersManualScp03"> SCP03 </xref> for more information on
        ///     this communication protocol.
        ///     <para>
        ///         Because this class implements <c>IDisposable</c>, use the <c>using</c>
        ///         keyword. For example,
        ///         <code language="csharp">
        ///             IYubiKeyDevice yubiKeyToUse = SelectYubiKey();
        ///             // Assume you have some method that obtains the appropriate SCP03
        ///             // key set.
        ///             using StaticKeys scp03Keys = CollectScp03Keys();
        ///             using (var piv = new PivSession(yubiKeyToUse, scp03Keys))
        ///             {
        ///                 /* Perform PIV operations. */
        ///             }
        ///         </code>
        ///     </para>
        /// </remarks>
        /// <param name="yubiKey">
        ///     The object that represents the actual YubiKey which will perform the
        ///     operations.
        /// </param>
        /// <param name="keyParameters">
        ///     The SCP03 key parameters, if any, to use in establishing the SCP connection.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The <c>yubiKey</c> argument is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     This exception is thrown when unable to determine the management key type.
        /// </exception>
        public PivSession(IYubiKeyDevice yubiKey, ScpKeyParameters? keyParameters = null)
            : base(Log.GetLogger<PivSession>(), yubiKey, YubiKeyApplication.Piv, keyParameters)
        {
            ResetAuthenticationStatus();
            UpdateManagementKey(yubiKey);
        }

        // /// <summary>
        // ///     The object that represents the connection to the YubiKey. Most
        // ///     applications will ignore this, but it can be used to call Commands
        // ///     directly.
        // /// </summary>
        // public IYubiKeyConnection Connection { get; }

        /// <summary>
        ///     The Delegate this class will call when it needs a PIN, PUK, or
        ///     management key.
        /// </summary>
        /// <remarks>
        ///     The delegate provided will read the <c>KeyEntryData</c> which
        ///     contains the information needed to determine what to collect and
        ///     methods to submit what was collected. The delegate will return
        ///     <c>true</c> for success or <c>false</c> for "cancel". A cancel will
        ///     usually happen when the user has clicked a "Cancel" button. That is
        ///     often the case when the user has entered the wrong value a number of
        ///     times, the remaining tries count is getting low, and they would like
        ///     to stop trying before the YubiKey is blocked.
        ///     <para>
        ///         Note that the SDK will call the <c>KeyCollector</c> with a
        ///         <c>Request</c> of <c>Release</c> when the process completes. In this
        ///         case, the <c>KeyCollector</c> MUST NOT throw an exception. The
        ///         <c>Release</c> is called from inside a <c>finally</c> block, and it
        ///         is a bad idea to throw exceptions from inside <c>finally</c>.
        ///     </para>
        /// </remarks>
        public Func<KeyEntryData, bool>? KeyCollector { get; set; }

        /// <summary>
        ///     When the PivSession object goes out of scope, this method is called.
        ///     It will close the session. The most important function of closing a
        ///     session is to "un-authenticate" the management key and "un-verify"
        ///     the PIN.
        /// </summary>
        // Note that .NET recommends a Dispose method call Dispose(true) and
        // GC.SuppressFinalize(this). The actual disposal is in the
        // Dispose(bool) method.
        // However, that does not apply to sealed classes.
        // So the Dispose method will simply perform the
        // "closing" process, no call to Dispose(bool) or GC.
        public override void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            KeyCollector = null;
            ResetAuthenticationStatus();
            base.Dispose();
            _disposed = true;
        }

        /// <summary>
        ///     Get information about the specified slot.
        /// </summary>
        /// <remarks>
        ///     This feature is available only on YubiKeys 5.3 and later. If you call
        ///     this method on an earlier YubiKey, it will throw an exception. A good
        ///     idea is to verify that the version number is valid before calling.
        ///     <code language="csharp">
        ///         IEnumerable&lt;IYubiKeyDevice&gt; list = YubiKey.FindByTransport(Transport.UsbSmartCard);
        ///         IYubiKeyDevice yubiKey = list.First();
        ///     
        ///         using (var pivSession = new PivSession(yubiKey))
        ///         {
        ///             if (yubiKey.FirmwareVersion &gt;= new FirmwareVersion(5, 3, 0))
        ///             {
        ///                 PivMetadata metadataSlot9A =
        ///                     pivSession.GetMetadata(PivSlot.Authentication);
        ///             }
        ///         }
        ///     </code>
        ///     <para>
        ///         See the User's Manual
        ///         <xref href="UsersManualPivCommands#get-metadata"> entry on getting metadata</xref>
        ///         for specific information about what information is returned. Different
        ///         slots return different sets of data. That page also lists the valid
        ///         slots for which metadata is available.
        ///     </para>
        /// </remarks>
        /// <param name="slotNumber">
        ///     The slot for which the information is requested.
        /// </param>
        /// <returns>
        ///     A new instance of a <c>PivMetadata</c> object containing information
        ///     about the given slot.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     The slot specified is not valid for getting metadata.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     The YubiKey queried does not support metadata, or the operation could
        ///     not be completed because of some error such as unreliable connection.
        /// </exception>
        public PivMetadata GetMetadata(byte slotNumber)
        {
            Logger.LogInformation("GetMetadata for slot number {SlotNumber:X2}.", slotNumber);

            if (!YubiKey.HasFeature(YubiKeyFeature.PivMetadata))
            {
                throw new NotSupportedException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.NotSupportedByYubiKeyVersion));
            }

            var command = new GetMetadataCommand(slotNumber);
            var response = Connection.SendCommand(command);

            return response.GetData();
        }

        /// <summary>
        /// Get information about YubiKey Bio multi-protocol.
        /// </summary>
        /// <remarks>
        /// This feature is available only on YubiKey Bio multi-protocol keys (FW 5.6 and later). If you call
        /// this method on an incompatible YubiKey, it will throw a <c>NotSupportedException</c>.
        /// <code language="csharp">
        ///     IEnumerable&lt;IYubiKeyDevice&gt; list = YubiKey.FindByTransport(Transport.UsbSmartCard);
        ///     IYubiKeyDevice yubiKey = list.First();<br/>
        ///     using (var pivSession = new PivSession(yubiKey))
        ///     {
        ///         try
        ///         {
        ///             var bioMetaData = PivSession.GetBioMetadata();
        ///             /* use bioMetaData */
        ///         }
        ///         catch (NotSupportedException e) {
        ///             /* this device does not support Bio multi-protocol metadata */
        ///         }
        ///     }
        /// </code>
        /// <para>
        /// See the User's Manual
        /// <xref href="UsersManualPivCommands#get-bio-metadata"> entry on getting bio metadata</xref>
        /// for specifics about what information is returned.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A new instance of a <c>PivBioMetadata</c> object.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// The queried YubiKey does not support bio metadata.
        /// </exception>
        /// <exception cref="ApduException">
        /// The operation could not be completed.
        /// </exception>
        public PivBioMetadata GetBioMetadata()
        {
            Logger.LogInformation("GetBioMetadata");
            return Connection.SendCommand(new GetBioMetadataCommand()).GetData();
        }

        /// <summary>
        ///     Reset the PIV application to the default state.
        /// </summary>
        /// <remarks>
        ///     This will delete all keys and certs in all the asymmetric key slots
        ///     other than F9, delete any added information to the data elements and
        ///     set the PIN, PUK, and management key to their default values. That
        ///     is, this will set the PIV application's state to what it was upon
        ///     manufacture. See the User's Manual entries on the
        ///     <xref href="UsersManualPinPukMgmtKey"> PIV PIN, PUK, and management key </xref>
        ///     and <xref href="UsersManualPivGetAndPutData"> data elements</xref>
        ///     for more information on the defaults and data added to elements.
        ///     <para>
        ///         Note that this has no effect on the other YubiKey applications. This
        ///         does NOT reset OTP, OATH, OpenPgp Card, FIDO U2F, or FIDO2.
        ///     </para>
        ///     <para>
        ///         Users will generally want to reset only if both the PIN and PUK are
        ///         blocked. If a PIN has been blocked, it can only be restored using the
        ///         PUK, but if the PUK is also blocked, there is no way to recover the
        ///         PIN. Once there is no PIN, and no way to recover it, there is very
        ///         little useful work the PIV application on a YubiKey can do. Resetting
        ///         the application does not make the situation worse, but it does
        ///         improve things somewhat, because the PIV application is usable again,
        ///         just with new key pairs.
        ///     </para>
        ///     <para>
        ///         However, it is important to note that this method will reset the PIV
        ///         application even if the PIN and/or PUK are not blocked. The YubiKey
        ///         will not allow itself to be reset until both the PIN and PUK are
        ///         blocked. This method will take steps necessary to block the PIN and
        ///         PUK, then call on the YubiKey to reset.
        ///     </para>
        ///     <para>
        ///         Before attempting to reset a YubiKey Bio Multi-protocol Edition key with ResetApplication(), verify that the PIV application is not blocked from using this method by checking the <see cref="IYubiKeyDeviceInfo.ResetBlocked"/> property. If the application is blocked, use <see cref="IYubiKeyDevice.DeviceReset"/>.
        ///     </para>
        /// </remarks>
        /// <exception cref="SecurityException">
        ///     The application could not be reset because of some error such as
        ///     unreliable connection.
        /// </exception>
        public void ResetApplication()
        {
            Logger.LogInformation("Resetting the PIV application.");

            // To reset, both the PIN and PUK must be blocked.
            TryBlock(PivSlot.Pin);
            TryBlock(PivSlot.Puk);

            var command = new ResetPivCommand();
            var response = Connection.SendCommand(command);
            if (response.Status != ResponseStatus.Success)
            {
                throw new SecurityException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.ApplicationResetFailure));
            }

            ResetAuthenticationStatus();

            // As resetting the PIV application resets the management key,
            // the management key must be updated to account for the case when the previous management key type
            // was not the default key type.
            UpdateManagementKey(YubiKey);
        }

        /// <summary>
        ///     Moves a key from one slot to another.
        ///     The source slot must not be the <see cref="PivSlot.Attestation" />-slot and the destination slot must be empty.
        /// </summary>
        /// <remarks>
        ///     Internally this method attempts to authenticate to the Yubikey by calling
        ///     <see cref="AuthenticateManagementKey" /> which may in turn throw its' own exceptions.
        /// </remarks>
        /// <param name="sourceSlot">The Yubikey slot of the key you want to move. This must be a valid slot number.</param>
        /// <param name="destinationSlot">The target Yubikey slot for the key you want to move. This must be a valid slot number.</param>
        /// <exception cref="InvalidOperationException">
        ///     There is no <c>KeyCollector</c> loaded, the key provided was not a
        ///     valid Triple-DES key, or the YubiKey had some other error, such as
        ///     unreliable connection.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        ///     The YubiKey returned malformed data and authentication, either single
        ///     or double, could not be performed.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     The user canceled management key collection.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     Mutual authentication was performed and the YubiKey was not authenticated.
        /// </exception>
        /// <exception cref="NotSupportedException">Thrown when the Yubikey doesn't support the Move-operation.</exception>
        public void MoveKey(byte sourceSlot, byte destinationSlot)
        {
            YubiKey.ThrowOnMissingFeature(YubiKeyFeature.PivMoveOrDeleteKey);

            if (!ManagementKeyAuthenticated)
            {
                AuthenticateManagementKey();
            }

            Logger.LogDebug("Moving key from {SourceSlot} to {DestinationSlot}", sourceSlot, destinationSlot);

            var command = new MoveKeyCommand(sourceSlot, destinationSlot);
            var response = Connection.SendCommand(command);
            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }

            Logger.LogInformation(
                "Successfully moved key from {SourceSlot} to {DestinationSlot}", sourceSlot, destinationSlot);
        }

        /// <summary>
        ///     Deletes/clears any key at a given <see cref="PivSlot" />.
        /// </summary>
        /// <remarks>
        ///     Internally this method attempts to authenticate to the Yubikey by calling
        ///     <see cref="AuthenticateManagementKey" /> which may in turn throw its' own exceptions.
        /// </remarks>
        /// <param name="slotToClear">The Yubikey slot of the key you want to clear. This must be a valid slot number.</param>
        /// <seealso cref="PivSlot" />
        /// <exception cref="InvalidOperationException">
        ///     Either the call to the Yubikey was unsuccessful or
        ///     there wasn't any <c>KeyCollector</c> loaded, the key provided was not a valid Triple-DES key, or the YubiKey
        ///     had some other error, such as unreliable connection. Refer to the specific exception message.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        ///     The YubiKey returned malformed data and authentication, either single or double, could not be performed.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     The user canceled management key collection.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     Mutual authentication was performed and the YubiKey was not authenticated.
        /// </exception>
        /// <exception cref="NotSupportedException">Thrown when the Yubikey doesn't support the Delete-operation.</exception>
        /// <seealso cref="PivSlot" />
        /// <seealso cref="AuthenticateManagementKey" />
        public void DeleteKey(byte slotToClear)
        {
            YubiKey.ThrowOnMissingFeature(YubiKeyFeature.PivMoveOrDeleteKey);

            if (!ManagementKeyAuthenticated)
            {
                AuthenticateManagementKey();
            }

            Logger.LogDebug("Deleting key at slot {TargetSlot}", slotToClear);

            var command = new DeleteKeyCommand(slotToClear);
            var response = Connection.SendCommand(command);

            bool unsuccessfulStatus =
                response.Status != ResponseStatus.Success &&
                response.Status != ResponseStatus.NoData;

            if (unsuccessfulStatus)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }

            string logMessage = response.Status == ResponseStatus.Success
                ? "Successfully deleted key at slot {targetSlot}."
                : "No data received from Yubikey after attempted delete on slot {targetSlot}, indicating that was likely empty to begin with.";

            Logger.LogInformation(logMessage, slotToClear);
        }

        // Block the PIN or PUK
        // To get the PIN or PUK into a blocked state, try to change it. Each
        // time the current PIN/PUK entered is incorrect, the retries remaining
        // count is decremented. When it hits zero, it is blocked, return true.
        // Call the ChangeReferenceDataCommand with arbitrary current and new
        // PIN/PUK values. They must be different. If the arbitrary current value
        // happens to be correct, the first call to change the PIN/PUK will work
        // and it will become the new PIN/PUK. For the next call, use the same
        // current value, which is now the wrong current value.
        // If the slotNumber argument is PivSlot.Pin, block the PIN, if it is
        // PivSlot.Puk, block the PUK.
        private bool BlockPinOrPuk(byte slotNumber)
        {
            Logger.LogInformation($"Block the {(slotNumber == 0x80 ? "PIN" : "PUK")}.");
            int retriesRemaining;

            do
            {
                byte[] currentValue =
                {
                    0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01
                };

                byte[] newValue =
                {
                    0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22
                };

                var command = new ChangeReferenceDataCommand(slotNumber, currentValue, newValue);
                var response = Connection.SendCommand(command);
                if (response.Status == ResponseStatus.Failed)
                {
                    return false;
                }

                retriesRemaining = response.GetData() ?? 1;
            }
            while (retriesRemaining > 0);

            return true;
        }

        private void TryBlock(byte slot)
        {
            if (BlockPinOrPuk(slot))
            {
                return;
            }

            throw new SecurityException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.ApplicationResetFailure));
        }

        private void UpdateManagementKey(IYubiKeyDevice yubiKey) =>
            ManagementKeyAlgorithm = yubiKey.HasFeature(YubiKeyFeature.PivAesManagementKey)
                ? GetManagementKeyAlgorithm()
                : PivAlgorithm.TripleDes; // Default for keys with firmware version < 5.7

        private PivAlgorithm GetManagementKeyAlgorithm()
        {
            var response = Connection.SendCommand(new GetMetadataCommand(PivSlot.Management));
            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }

            var metadata = response.GetData();
            return metadata.Algorithm;
        }

        // Reset any fields and properties related to authentication or
        // verification to the initial state: not authenticated, verified, etc.
        private void ResetAuthenticationStatus()
        {
            ManagementKeyAuthenticated = false;
            ManagementKeyAuthenticationResult = AuthenticateManagementKeyResult.Unauthenticated;
            PinVerified = false;
        }
    }
}
