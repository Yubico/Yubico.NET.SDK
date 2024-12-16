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
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Piv.Objects;

namespace Yubico.YubiKey.Piv
{
    // This portion of the PivSession class contains code related to PIN-only
    // mode (PIN-protected and PIN-derived) operations.
    public sealed partial class PivSession : IDisposable
    {
        private const int AdminDataDataTag = 0x005FFF00;

        /// <summary>
        ///     Return an enum indicating the PIN-only mode, if any, for which the
        ///     YubiKey PIV application is configured.
        /// </summary>
        /// <remarks>
        ///     PIN-only mode means that the application does not need to enter the
        ///     management key in order to perform PIV operations that normally
        ///     require it, only the PIN is needed.
        ///     <para>
        ///         See the User's Manual entry on
        ///         <xref href="UsersManualPivPinOnlyMode"> PIV PIN-only mode</xref> for
        ///         a deeper discussion of this feature.
        ///     </para>
        ///     <para>
        ///         This returns a result based on the contents of ADMIN DATA. That
        ///         storage location contains information about PIN-protected and
        ///         PIN-derived. It is possible for a different application to overwrite
        ///         the data to make it inaccurate. That is unlikely, however, if all
        ///         applications follow good programming practices outlined by the SDK
        ///         documentation. This method will not actually verify the management
        ///         key in order to ensure the return value is correct.
        ///     </para>
        ///     <para>
        ///         If the ADMIN DATA is overwritten, it is possible to call
        ///         <see cref="TryRecoverPinOnlyMode()" /> to restore the YubiKey to a
        ///         proper PIN-only state.
        ///     </para>
        ///     <para>
        ///         Note also that it is possible that the ADMIN DATA says the YubiKey is
        ///         PIN-protected, but some app has overwritten the data in PRINTED. In
        ///         that case, this method will return a result indicating
        ///         <c>PinProtected</c>, when in reality PIN-protected is unavailable.
        ///         That is because this returns a value based only on the contents of
        ///         ADMIN DATA. The method <c>TryRecoverPinOnlyMode</c> will check more
        ///         than ADMIN DATA.
        ///     </para>
        ///     <para>
        ///         Note that the return is a bit field and the return can be one or more
        ///         of the bits set. There are bits that indicate a YubiKey is
        ///         unavailable for PIN-protected or PIN-derived. Call this method before
        ///         trying to set a YubiKey to PIN-only to make sure it is not already
        ///         set, and if not, it can be set.
        ///     </para>
        ///     <para>
        ///         Note that this returns the PIN-only mode for the PIV application on
        ///         the YubiKey, it has nothing to do with OATH, FIDO, or OpenPGP.
        ///     </para>
        /// </remarks>
        /// <returns>
        ///     A <c>PivPinOnlyMode</c>, which is an enum indicating the mode or
        ///     modes.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The YubiKey is not able to return the ADMIN DATA.
        /// </exception>
        public PivPinOnlyMode GetPinOnlyMode()
        {
            _log.LogInformation("Get the PIV PIN-only mode of a YubiKey based on AdminData.");

            var pinOnlyMode = PivPinOnlyMode.PinProtectedUnavailable | PivPinOnlyMode.PinDerivedUnavailable;

            if (TryReadObject(out AdminData adminData))
            {
                pinOnlyMode = PivPinOnlyMode.None;

                if (adminData.PinProtected)
                {
                    pinOnlyMode |= PivPinOnlyMode.PinProtected;
                }

                if (!(adminData.Salt is null))
                {
                    pinOnlyMode |= PivPinOnlyMode.PinDerived;
                }

                adminData.Dispose();
            }

            return pinOnlyMode;
        }

        /// <summary>
        ///     Try to recover the PIN-only state. If successful, this will
        ///     authenticate the management key and reset the ADMIN DATA and or
        ///     PRINTED storage locations.
        ///     &gt; [!WARNING]
        ///     &gt; This can overwrite the contents of ADMIN DATA and/or PRINTED. If
        ///     &gt; some other application relies on that data it will be lost.
        /// </summary>
        /// <remarks>
        ///     See the User's Manual entry on
        ///     <xref href="UsersManualPivPinOnlyMode"> PIV PIN-only mode</xref> for
        ///     a deeper discussion of this operation.
        ///     <para>
        ///         The ADMIN DATA contains information about PIN-only. The PIN-protected
        ///         management key is stored in PRINTED. Applications should never store
        ///         information in those locations, only Yubico-supplied products should
        ///         use them. However, it is possible for an application to overwrite the
        ///         contents of one or both of these storage locations, making the
        ///         PIN-only data inaccurate.
        ///     </para>
        ///     <para>
        ///         This method will obtain the data stored in the two storage locations,
        ///         and determine if they contain PIN-only data that can be used to
        ///         authenticate the management key. If it can't, it will return
        ///         <c>PivPinOnlyMode.None</c> or <c>Unavailable</c>. If it can, it will
        ///         authenticate and set the ADMIN DATA and PRINTED to contain data
        ///         compatible with correct PIN-only modes. It will return a
        ///         <c>PivPinOnlyMode</c> value indicating which mode is set.
        ///     </para>
        ///     <para>
        ///         For example, suppose the data in both is correct, and it indicates
        ///         the management key is PIN-protected. After calling this method, the
        ///         management key will be authenticated, the storage locations will not
        ///         be changed, and the return will be <c>PivPinOnlyMode.PinProtected</c>.
        ///     </para>
        ///     <para>
        ///         Another possibility is the ADMIN DATA was overwritten by some
        ///         application so it is inaccurate, but the PIN-protected data is still
        ///         in PRINTED. This method will be able to authenticate the management
        ///         key using that data. It will replace the contents of ADMIN DATA with
        ///         correct PIN-only information and return
        ///         <c>PivPinOnlyMode.PinProtected</c>.
        ///     </para>
        ///     <para>
        ///         If ADMIN DATA and PRINTED contain no data, or if ADMIN DATA contains
        ///         correct information that indicates the YubiKey is not set to PIN-only
        ///         mode, then this method will not authenticate the management key, it
        ///         will not put any data into the storage locations, and it will return
        ///         <c>PivPinOnlyMode.None</c>.
        ///     </para>
        ///     <para>
        ///         It is possible this method is not able to recover. For example,
        ///         suppose the ADMIN DATA is correct and indicates the YubiKey is
        ///         PIN-protected, but not PIN-derived (there is no salt to use to derive
        ///         a key), but the data in PRINTED is not correct. In this case, the
        ///         method will not be able to authenticate the management key as
        ///         PIN-protected. It will try to authenticate using the default
        ///         management key, and if that does not work, it will call on the
        ///         <c>KeyCollector</c> to obtain the it. If that does succeeds, it will
        ///         set ADMIN DATA to indicate the YubiKey is not PIN-protected, it will
        ///         clear the contents of PRINTED, and it will return
        ///         <c>PivPinOnlyMode.None</c>. If the <c>KeyCollector</c> is not able to
        ///         provide the management key, this method will not be able to reset the
        ///         ADMIN DATA nor PRINTED (management key authentication is necessary to
        ///         set a storage location), and will return <c>Unavailable</c>.
        ///     </para>
        ///     <para>
        ///         This method will require the PIN to be verified. It is possible that
        ///         the PIN has already been verified and this method will verify it
        ///         again. If it needs to verify the PIN, it will call on the
        ///         <c>KeyCollector</c> to obtain it.
        ///     </para>
        /// </remarks>
        /// <returns>
        ///     A <c>PivPinOnlyMode</c>, which is an enum indicating the mode or
        ///     modes the YubiKey is in.
        /// </returns>
        public PivPinOnlyMode TryRecoverPinOnlyMode()
        {
            _log.LogInformation("Try to authenticate using PIN-only.");

            var pinOnlyMode = TryAuthenticatePinOnly(false);

            // If the result is None, or PinProtected, or PinDerived, or
            // PinProtected | PinDerived, then everything is fine, just return.
            // In other words, if it does not contain an Unavailable.
            if (!pinOnlyMode.HasFlag(PivPinOnlyMode.PinProtectedUnavailable) &&
                !pinOnlyMode.HasFlag(PivPinOnlyMode.PinDerivedUnavailable))
            {
                return pinOnlyMode;
            }

            // If we reach this point, either PinProtectedUnavailable or
            // PinDerivedUnavailable is (or both are) set.
            // If the returnValue contains PinProtected, then we know the PRINTED
            // data is correct and the mgmt key has been authenticated. But we
            // also know that PinDerivedUnavailable is set. That means the ADMIN
            // DATA is wrong. We need to reset it.
            if (pinOnlyMode.HasFlag(PivPinOnlyMode.PinProtected))
            {
                // Make sure the PUK is blocked. It probably is, but we're going
                // to set the PukBlocked field in adminData to true, so make sure
                // it is indeed true.
                _ = BlockPinOrPuk(PivSlot.Puk);

                using var adminData = new AdminData
                {
                    PukBlocked = true,
                    PinProtected = true
                };

                WriteObject(adminData);

                return PivPinOnlyMode.PinProtected;
            }

            // If we reach this point, either PinProtectedUnavailable or
            // PinDerivedUnavailable is (or both are) set.
            // If the returnValue contains PinDerived, then we know the ADMIN
            // DATA is "correct", and the mgmt key was authenticated. But PRINTED
            // is incorrect.
            // Check adminData.PinProtected. If it is true, we want to reset the
            // YubiKey to also PIN-protected. Otherwise
            // Reset PRINTED to empty, and make sure ADMIN DATA indicates PUK
            // blocked and PinProtected is false.
            if (pinOnlyMode.HasFlag(PivPinOnlyMode.PinDerived))
            {
                // Read the AdminData to get the salt.
                var adminData = ReadObject<AdminData>();

                // Clear out the contents of PRINTED.
                using var pinProtect = new PinProtectedData();
                WriteObject(pinProtect);

                var protectMode = PivPinOnlyMode.None;

                if (adminData.PinProtected)
                {
                    SetPinOnlyMode(PivPinOnlyMode.PinProtected, ManagementKeyAlgorithm);
                    protectMode = PivPinOnlyMode.PinProtected;
                }

                // Make sure the PUK is blocked. It probably is, but we're going
                // to set the PukBlocked field in adminData to true, so make sure
                // it is indeed true.
                _ = BlockPinOrPuk(PivSlot.Puk);
                adminData.PukBlocked = true;
                WriteObject(adminData);

                return PivPinOnlyMode.PinDerived | protectMode;
            }

            // At this point, neither PinProtected nor PinDerived is set. That
            // means the mgmt key is not authenticated.
            // If we can authenticate the mgmt key, then set ADMIN DATA and
            // PRINTED.
            var userKeyCollector = KeyCollector;
            using var specialKeyCollector = new SpecialKeyCollector();

            try
            {
                KeyCollector = specialKeyCollector.KeyCollectorSpecial;
                specialKeyCollector.AuthMgmtKeyAndSave(this, userKeyCollector);

                // If the PinDerivedUnavailable bit is not set, that means either
                // there was no ADMIN DATA, or it was "correct". If it was
                // "correct, we want to leave it as is, except make sure the
                // PinProtected property is false and the Salt is null.
                // If that bit is set, then we want to clear ADMIN DATA.
                using var adminData = pinOnlyMode.HasFlag(PivPinOnlyMode.PinDerivedUnavailable)
                    ? new AdminData()
                    : ReadObject<AdminData>();

                if (!adminData.IsEmpty)
                {
                    adminData.PinProtected = false;
                }

                adminData.SetSalt(ReadOnlyMemory<byte>.Empty);
                WriteObject(adminData);

                // If the PinProtectedUnavailable bit is not set, that means
                // there was no PRINTED data (if there was data we would see
                // either the PinProtected or the PinProtectedUnavailable bit
                // set, and if we reach this point we know the PinProtected bit
                // is not set). Just leave it. If it was set, clear PRINTED.
                if (pinOnlyMode.HasFlag(PivPinOnlyMode.PinProtectedUnavailable))
                {
                    using var pinProtect = new PinProtectedData();
                    WriteObject(pinProtect);
                }

                return PivPinOnlyMode.None;
            }
            catch (InvalidOperationException)
            {
                return pinOnlyMode;
            }
            catch (OperationCanceledException)
            {
                return pinOnlyMode;
            }
            finally
            {
                KeyCollector = userKeyCollector;
            }
        }

        // Shared code.
        // This is the implementation of the public method. The difference is
        // that it is possible to call this one and trust ADMIN DATA.
        // Call with trustAdminData true, and this method will get ADMIN DATA and
        // try PIN-protected if it says so, and if not, it will try PIN-derived
        // if it says so. If ADMIN DATA is incorrect, then this will not try
        // anything and simply return Unavailable.
        // If the ADMIN DATA says the YubiKey is both PinProtected and
        // PinDerived, and PinProtected authenticates, then this method will not
        // try PinDerived.
        // Call it with trustAdminData as false, and this will get the data out
        // of PRINTED and if there is proper data there, try to authenticate the
        // management key with that data. It will then determine if the ADMIN
        // DATA contains a salt, and if so, can it be used to derive the
        // management key. This will try both if possible, even if one of them
        // has authenticated the management key already.
        private PivPinOnlyMode TryAuthenticatePinOnly(bool trustAdminData)
        {
            bool tryPinProtected = true;
            bool tryPinDerived = true;

            var pinOnlyMode = PivPinOnlyMode.None;

            if (trustAdminData)
            {
                pinOnlyMode = GetPinOnlyMode();
                tryPinProtected = pinOnlyMode.HasFlag(PivPinOnlyMode.PinProtected);
                tryPinDerived = pinOnlyMode.HasFlag(PivPinOnlyMode.PinDerived);
            }

            var userKeyCollector = KeyCollector;
            using var specialKeyCollector = new SpecialKeyCollector();

            try
            {
                KeyCollector = specialKeyCollector.KeyCollectorSpecial;

                if (tryPinProtected)
                {
                    pinOnlyMode = GetPrintedPinProtectedStatus(specialKeyCollector, userKeyCollector);
                    if (trustAdminData && pinOnlyMode.HasFlag(PivPinOnlyMode.PinProtected))
                    {
                        return pinOnlyMode;
                    }
                }

                if (tryPinDerived)
                {
                    using var adminData = new AdminData();

                    pinOnlyMode |= GetPinDerivedStatus(
                        adminData, pinOnlyMode.HasFlag(PivPinOnlyMode.PinProtected), specialKeyCollector,
                        userKeyCollector);
                }

                return pinOnlyMode;
            }
            finally
            {
                KeyCollector = userKeyCollector;
            }
        }

        // Get the contents of PRINTED into the pinProtect object.
        // If there is none, leave pinProtect empty and return None.
        // If there is data and it is valid PinProtectedData, but no mgmt key,
        // return None.
        // If there is data and it is valid PinProtectedData with a mgmt key, set
        // the pinProtect object with that data and try to authenticate. If it
        // authenticates, return PinProtected. If not, return Unavailable.
        // If there is data but it is not PinProtectedData, the pinProtect object
        // will be empty and return Unavailable.
        private PivPinOnlyMode GetPrintedPinProtectedStatus(
            SpecialKeyCollector specialKeyCollector,
            Func<KeyEntryData, bool>? userKeyCollector)
        {
            // We could call the ReadObject method, but if the PIN is not
            // verified, ReadObject won't collect and save it.
            // Hence, in order to be able to call VerifyPinAndSave, but only if
            // needed, call the GetDataCommand directly.
            var command = new GetDataCommand((int)PivDataTag.Printed);
            var response = Connection.SendCommand(command);

            if (response.Status == ResponseStatus.AuthenticationRequired)
            {
                specialKeyCollector.VerifyPinAndSave(this, userKeyCollector);
                response = Connection.SendCommand(command);
            }

            if (response.Status == ResponseStatus.NoData)
            {
                return PivPinOnlyMode.None;
            }

            if (response.Status == ResponseStatus.Success)
            {
                using var pinProtect = new PinProtectedData();

                if (pinProtect.TryDecode(response.GetData()))
                {
                    if (pinProtect.ManagementKey is null)
                    {
                        return PivPinOnlyMode.None;
                    }

                    if (TryAuthenticateManagementKey((ReadOnlyMemory<byte>)pinProtect.ManagementKey))
                    {
                        specialKeyCollector.SetKeyData(
                            SpecialKeyCollector.SetKeyDataBuffer,
                            (ReadOnlyMemory<byte>)pinProtect.ManagementKey,
                            isNewKey: false,
                            ManagementKeyAlgorithm);

                        return PivPinOnlyMode.PinProtected;
                    }
                }
            }

            return PivPinOnlyMode.PinProtectedUnavailable;
        }

        // Get the contents of ADMIN DATA into the adminData object.
        // If there is none, leave adminData empty and return None.
        // If there is data and it is valid ADMIN DATA, but no salt,
        // return None.
        // If there is data and it is valid ADMIN DATA with a salt, set
        // the adminData object with that data and try to authenticate. If it
        // authenticates, return PinDerived. If not, return Unavailable.
        // If there is data but it is not ADMIN DATA, the adminData object
        // will be empty and return Unavailable.
        // If the isPinProtected arg is true, then the correct mgmt key is in the
        // specialKeyCollector, so to authenticate the derived key just do a
        // SequenceEqual.
        // This will update the adminData object passed in with the contents of
        // the ADMIN DATA storage location. This method expects the adminData to
        // be empty.
        private PivPinOnlyMode GetPinDerivedStatus(
            AdminData adminData,
            bool isPinProtected,
            SpecialKeyCollector specialKeyCollector,
            Func<KeyEntryData, bool>? userKeyCollector)
        {
            // We could use the TryReadObject to get the admin data, but that
            // returns a new object. We need to fill the incoming object with the
            // data.
            var command = new GetDataCommand(adminData.DataTag);
            var response = Connection.SendCommand(command);
            if (response.Status == ResponseStatus.NoData)
            {
                return PivPinOnlyMode.None;
            }

            if (response.Status == ResponseStatus.Success)
            {
                if (adminData.TryDecode(response.GetData()))
                {
                    if (adminData.Salt is null)
                    {
                        return PivPinOnlyMode.None;
                    }

                    // If we have already collected the PIN, this call will do
                    // nothing (it won't collect it again).
                    specialKeyCollector.VerifyPinAndSave(this, userKeyCollector);

                    // If we're already PIN-protected, then the current mgmt key
                    // is the PIN-protected value. So put the derived key into
                    // the new buffer and compare.
                    // If not, put it into the current buffer and authenticate.
                    _ = specialKeyCollector.DeriveKeyData(
                        (ReadOnlyMemory<byte>)adminData.Salt, ManagementKeyAlgorithm, isPinProtected);

                    if (isPinProtected)
                    {
                        if (specialKeyCollector.GetCurrentMgmtKey().Span
                            .SequenceEqual(specialKeyCollector.GetNewMgmtKey().Span))
                        {
                            return PivPinOnlyMode.PinDerived;
                        }
                    }
                    else
                    {
                        if (TryAuthenticateWithKeyCollector(true))
                        {
                            return PivPinOnlyMode.PinDerived;
                        }
                    }
                }
            }

            return PivPinOnlyMode.PinDerivedUnavailable;
        }

        /// <summary>
        ///     Set the YubiKey's PIV application to be PIN-only with a PIN-derived
        ///     and/or PIN-Protected Triple-DES management key. This sets the
        ///     YubiKey to either
        ///     <code>
        ///        PivPinOnlyMode.PinProtected
        ///        PivPinOnlyMode.PinDerived
        ///        PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived
        ///        PivPinOnlyMode.None
        ///     </code>
        ///     If the YubiKey is set to PinProtected, PinDerived, or both, the PUK
        ///     will also be blocked.
        ///     &gt; [!WARNING]
        ///     &gt; You should not set a YubiKey for PIN-derived, this feature is
        ///     &gt; provided only for backwards compatibility.
        /// </summary>
        /// <remarks>
        ///     PIN-only mode means that the application does not need to enter the
        ///     management key in order to perform PIV operations that normally
        ///     require it, only the PIN is needed.
        ///     <para>
        ///         See the documentation for
        ///         <see cref="SetPinOnlyMode(PivPinOnlyMode, PivAlgorithm)" /> and the
        ///         User's Manual entry on
        ///         <xref href="UsersManualPivPinOnlyMode"> PIV PIN-only mode</xref> for
        ///         a deeper discussion of this feature.
        ///     </para>
        ///     <para>
        ///         Note that this sets the PIV application on the specific YubiKey to a
        ///         PIN-only mode, it has nothing to do with OATH, FIDO, or OpenPGP.
        ///     </para>
        ///     <para>
        ///         Note also that this will make sure that the management key algorithm
        ///         will be Triple-DES, even if the current management key is a different
        ///         algorithm. This behavior matches how this method operated in previous
        ///         versions of the SDK.
        ///     </para>
        /// </remarks>
        /// <param name="pinOnlyMode">
        ///     The mode to which the YubiKey is to be set.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     There is no <c>KeyCollector</c> loaded, one of the keys provided was
        ///     not a valid Triple-DES key, the data stored on the YubiKey is
        ///     incompatible with PIN-only, or the YubiKey had some other error, such
        ///     as unreliable connection.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     The user canceled management key or PIN collection.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     Mutual authentication was performed and the YubiKey was not
        ///     authenticated, or the remaining retries count indicates the PIN is
        ///     blocked.
        /// </exception>
        public void SetPinOnlyMode(PivPinOnlyMode pinOnlyMode) => SetPinOnlyMode(pinOnlyMode, PivAlgorithm.TripleDes);

        /// <summary>
        ///     Set the YubiKey's PIV application to be PIN-only with a PIN-derived
        ///     and/or PIN-Protected management key of the specified algorithm. This
        ///     sets the YubiKey to either
        ///     <code>
        ///        PivPinOnlyMode.PinProtected
        ///        PivPinOnlyMode.PinDerived
        ///        PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived
        ///        PivPinOnlyMode.None
        ///     </code>
        ///     If the YubiKey is set to PinProtected, PinDerived, or both, the PUK
        ///     will also be blocked.
        ///     &gt; [!WARNING]
        ///     &gt; You should not set a YubiKey for PIN-derived, this feature is
        ///     &gt; provided only for backwards compatibility.
        /// </summary>
        /// <remarks>
        ///     PIN-only mode means that the application does not need to enter the
        ///     management key in order to perform PIV operations that normally
        ///     require it, only the PIN is needed.
        ///     <para>
        ///         See the User's Manual entry on
        ///         <xref href="UsersManualPivPinOnlyMode"> PIV PIN-only mode</xref> for
        ///         a deeper discussion of this feature.
        ///     </para>
        ///     <para>
        ///         Note that this sets the PIV application on the specific YubiKey to a
        ///         PIN-only mode, it has nothing to do with OATH, FIDO, or OpenPGP.
        ///     </para>
        ///     <para>
        ///         Upon successful completion of this method, the PUK will be blocked.
        ///     </para>
        ///     <para>
        ///         The management key derived and/or stored in PRINTED will be for the
        ///         specified algorithm. For all YubiKeys, <c>TripleDes</c> is a valid
        ///         algorithm. For YubiKeys 5.4.2 and later, it is possible to set the
        ///         management key to an AES key. Before setting the
        ///         <c>mgmtKeyAlgorithm</c> arg to an AES algorithm, make sure it is
        ///         allowed on the YubiKey. You can use the <c>HasFeature</c> call. For
        ///         example,
        ///         <code language="csharp">
        ///   PivAlgorithm mgmtKeyAlgorithm = yubiKey.HasFeature(YubiKeyFeature.PivAesManagementKey) ?
        ///       PivAlgorithm.Aes128 : PivAlgorithm.TripleDes;
        ///   pivSession.SetPinOnlyMode(PivPinOnlyMode.PinProtected, mgmtKeyAlgorithm);
        /// </code>
        ///         If the algorithm is not supported by the YubiKey, this method will
        ///         throw an exception. It will not change the YubiKey, it will not set
        ///         it to PIN-only.
        ///     </para>
        ///     <para>
        ///         If the YubiKey is already set to the specified PIN-protected mode and
        ///         algorithm, this will not reset the YubiKey, but will simply
        ///         authenticate the management key.
        ///     </para>
        ///     <para>
        ///         If the YubiKey is already set to PIN-only, but not the specified
        ///         mode, this method will make sure the YubiKey is set to both PIN-only
        ///         modes.
        ///     </para>
        ///     <para>
        ///         If the YubiKey is already set to PIN-only, but not the specified
        ///         algorithm, this method will change to a new management key of the
        ///         specified algorithm.
        ///     </para>
        ///     <para>
        ///         If the requested <c>pinOnlyMode</c> is both, then this will make sure
        ///         the YubiKey is both PIN-protected and PIN-derived.
        ///     </para>
        ///     <para>
        ///         If the input <c>pinOnlyMode</c> is <c>None</c>, and the YubiKey is
        ///         currently set to PIN-only (and neither PinProtected nor PinDerived is
        ///         Unavailable), this method will remove the contents of the storage
        ///         locations ADMIN DATA and PRINTED, and reset the management key to the
        ///         default:
        ///         <code>
        ///   Triple-DES
        ///   0x01 02 03 04 05 06 07 08
        ///     01 02 03 04 05 06 07 08
        ///     01 02 03 04 05 06 07 08
        /// </code>
        ///         In this case, the <c>mgmtKeyAlgorithm</c> arg will be ignored, the
        ///         management key's algorithm after removing PIN-only status will be
        ///         Triple-DES. The touch policy of the management key will also be set
        ///         to the default (Never). Note that the management key must be
        ///         authenticated and the PIN verified in order to perform this task.
        ///         This method will authenticate the management key using the PIN-only
        ///         mode to which the YubiKey is currently set, then clear the contents.
        ///         It will then change the management key to the default value. Hence,
        ///         after setting the YubiKey out of PIN-only mode, you should change the
        ///         management key. Note also that the PUK will still be blocked. The
        ///         only way to unblock the PUK is to reset the retry counts. This will
        ///         set both the PIN and PUK to their default values. Resetting the retry
        ///         counts requires management key authentication.
        ///     </para>
        ///     <para>
        ///         If the input <c>pinOnlyMode</c> is <c>None</c>, and the YubiKey is
        ///         currently set to one PIN-only mode, but the other is Unavailable,
        ///         this method will clear the one that is set but leave the one that is
        ///         Unavailable as is. For example, suppose the current mode is
        ///         <c>PinProtected | PinDerivedUnavailable</c>, then after setting to
        ///         None, the PRINTED storage area will be empty and the ADMIN DATA
        ///         storage area will be left as is.
        ///     </para>
        ///     <para>
        ///         If the input <c>pinOnlyMode</c> is <c>None</c>, and the YubiKey is
        ///         currently NOT set to either PIN-only modes, this method will do
        ///         nothing. It will not remove anything from ADMIN DATA or PRINTED, it
        ///         will not try to authenticate the management key, nor will it try to
        ///         verify the PIN. It will not change the management to the default. It
        ///         will ignore the <c>mgmtKeyAlgorithm</c> argument.
        ///     </para>
        ///     <para>
        ///         If a YubiKey is currently set to both modes, and you call this
        ///         method with only one of the modes, this method will NOT "remove" the
        ///         other mode.
        ///     </para>
        ///     <para>
        ///         If a YubiKey is currently set to both PIN-only modes and you want to
        ///         "remove" one of them, call this method and set the YubiKey to
        ///         <c>None</c>, then call this method a second time and set the YubiKey
        ///         to the desired mode.
        ///     </para>
        ///     <para>
        ///         In order to set the YubiKey to be PIN-only, this method must
        ///         authenticate the management key. Even if the management key is already
        ///         authenticated, this method will authenticate. It will try to
        ///         authenticate using the PIN-only techniques, and if that does not
        ///         work, it will try to authenticate using the default management key. If
        ///         either of those techniques works, the user will not have to enter the
        ///         management key. But if the YubiKey is not PIN-only, and the default
        ///         management key does not authenticate, the method will call on the
        ///         <c>KeyCollector</c> to obtain the correct value.
        ///     </para>
        ///     <para>
        ///         If the mode to set is PIN-protected, then this method will use the
        ///         existing mgmt key, unless it is the default. In that case, this
        ///         method will generate a new, random mgmt key, set the YubiKey with
        ///         this new value, and PIN-protect the new key.
        ///     </para>
        ///     <para>
        ///         This method also requires the PIN to be verified. If setting to
        ///         PIN-derived, even if the PIN is already verified, this method will
        ///         call on the <c>KeyCollector</c> to obtain the PIN. If setting to
        ///         PIN-protected, this method will verify the PIN only if it has not yet
        ///         been verified.
        ///     </para>
        ///     <para>
        ///         Note that this method will throw an exception if the current contents
        ///         of ADMIN DATA and/or PRINTED are not compatible with PIN-only mode.
        ///         If there are no current contents, then this method will set one or
        ///         both of those storage locations with appropriate data. But if there
        ///         is already something in one or both, and it is not PIN-only
        ///         information, then this method will not replace that data, it will
        ///         throw an exception.
        ///     </para>
        /// </remarks>
        /// <param name="pinOnlyMode">
        ///     The mode to which the YubiKey is to be set.
        /// </param>
        /// <param name="mgmtKeyAlgorithm">
        ///     The algorithm to which the management key will be set.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     There is no <c>KeyCollector</c> loaded, one of the keys provided was
        ///     not a valid Triple-DES key, the data stored on the YubiKey is
        ///     incompatible with PIN-only, or the YubiKey had some other error, such
        ///     as unreliable connection.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     The user canceled management key or PIN collection.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     Mutual authentication was performed and the YubiKey was not
        ///     authenticated, or the remaining retries count indicates the PIN is
        ///     blocked.
        /// </exception>
        public void SetPinOnlyMode(PivPinOnlyMode pinOnlyMode, PivAlgorithm mgmtKeyAlgorithm)
        {
            _log.LogInformation(
                "Set a YubiKey to PIV PIN-only mode: {PivPinOnlyMode}, mgmt key alg = {PivAlgorithm}.",
                pinOnlyMode.ToString(), mgmtKeyAlgorithm.ToString());

            var userKeyCollector = KeyCollector;
            using var specialKeyCollector = new SpecialKeyCollector();

            try
            {
                KeyCollector = specialKeyCollector.KeyCollectorSpecial;
                SetPinOnlyMode(specialKeyCollector, userKeyCollector, pinOnlyMode, mgmtKeyAlgorithm);
            }
            finally
            {
                KeyCollector = userKeyCollector;
            }
        }

        // Set the YubiKey to the PIN-only mode specified, using the given pin.
        // If the pin is Empty, use the default PIN.
        // This is called by the Change PIN methods. The new management key (if
        // there is one) will be the same algorithm of the current one.
        private void SetPinOnlyMode(ReadOnlyMemory<byte> pin, PivPinOnlyMode pinOnlyMode, out int? retriesRemaining)
        {
            var pinToUse = pin;
            if (pinToUse.Length == 0)
            {
                pinToUse = new ReadOnlyMemory<byte>(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 });
            }

            var userKeyCollector = KeyCollector;
            using var specialKeyCollector = new SpecialKeyCollector();

            try
            {
                KeyCollector = specialKeyCollector.KeyCollectorSpecial;

                if (specialKeyCollector.TrySetPin(this, pinToUse, out retriesRemaining))
                {
                    SetPinOnlyMode(specialKeyCollector, userKeyCollector, pinOnlyMode, ManagementKeyAlgorithm);
                }
            }
            finally
            {
                KeyCollector = userKeyCollector;
            }
        }

        // Shared code.
        // The caller might have already collected the PIN and placed it into the
        // specialKeyCollector. If not, this method will call on the User's
        // KeyCollector to obtain it.
        // This method assumes that the caller has set this PivSession's
        // KeyCollector to the special, and will reset it to the User's when done.
        private void SetPinOnlyMode(
            SpecialKeyCollector specialKeyCollector,
            Func<KeyEntryData, bool>? userKeyCollector,
            PivPinOnlyMode pinOnlyMode,
            PivAlgorithm mgmtKeyAlgorithm)
        {
            if (pinOnlyMode.HasFlag(PivPinOnlyMode.PinProtectedUnavailable)
                || pinOnlyMode.HasFlag(PivPinOnlyMode.PinDerivedUnavailable))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPivPinOnlyMode));
            }

            // If the YubiKey does not allow AES, but the caller specified AES,
            // throw an exception.
            CheckManagementKeyAlgorithm(mgmtKeyAlgorithm, checkMode: false);

            // Determine if we are using a new algorithm for this Set.
            // Later on, we'll use this to determine if we need to Clear the
            // YubiKey of PinOnly first.
            // If the caller wants a Mode of None, we're going to ignore the
            // mgmtKeyAlgorithm arg, and we're going to want to clear the
            // YubiKey, so say it is a new algorithm.
            bool newAlgorithm = mgmtKeyAlgorithm != ManagementKeyAlgorithm || pinOnlyMode == PivPinOnlyMode.None;

            // We're creating this variable so that we know which mode to set.
            // We might need to set a mode because the caller requests it and it
            // is not yet set.
            // We might need to set a mode because it is currently set and the
            // caller wants a new algorithm.
            // Or some other reason.
            var newPinOnlyMode = PivPinOnlyMode.None;
            var currentPinOnlyMode = GetPrintedPinProtectedStatus(specialKeyCollector, userKeyCollector);
            
            var pinOnlyCheck = CheckPinOnlyStatus(
                currentPinOnlyMode, pinOnlyMode, PivPinOnlyMode.PinProtected, PivPinOnlyMode.PinProtectedUnavailable,
                newAlgorithm, ref newPinOnlyMode);

            using var adminData = new AdminData();

            if (pinOnlyCheck == PinOnlyCheck.CanContinue)
            {
                currentPinOnlyMode |= GetPinDerivedStatus(
                    adminData, currentPinOnlyMode.HasFlag(PivPinOnlyMode.PinProtected), specialKeyCollector, userKeyCollector);

                pinOnlyCheck = CheckPinOnlyStatus(
                    currentPinOnlyMode, pinOnlyMode, PivPinOnlyMode.PinDerived, PivPinOnlyMode.PinDerivedUnavailable,
                    newAlgorithm, ref newPinOnlyMode);
            }

            if (pinOnlyCheck == PinOnlyCheck.Unavailable)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.PinOnlyNotPossible));
            }

            // We can  quit if the above check found no need to move on. That
            // happens when the requested is Protected and it is already set to
            // Protected (regardless of the status of Derived), or if the
            // requested is Derived and it is already set to Derived.
            if (pinOnlyCheck == PinOnlyCheck.Complete)
            {
                return;
            }

            // If the mgmt key has not yet been authenticated, then get it
            // using the KeyCollector.
            if (!currentPinOnlyMode.HasFlag(PivPinOnlyMode.PinProtected) && !currentPinOnlyMode.HasFlag(PivPinOnlyMode.PinDerived))
            {
                // Actually, before we do that, check to see if the requested is
                // None. It's possible that one or both of the modes is
                // Unavailable. In that case, it is equivalent to None (if we're
                // in this section, no one authenticated so no one is set to
                // PinOnly). So if the requested is None, we're done.
                if (pinOnlyMode == PivPinOnlyMode.None)
                {
                    return;
                }

                // This will either return with the mgmt key authenticated,
                // or it will throw an exception.
                specialKeyCollector.AuthMgmtKeyAndSave(this, userKeyCollector);

                // If we reach this code, we can also set newAlgorithm to false.
                // If this is true, we're going to later on Clear the YubiKey of
                // PinOnly. But if we're here in this section, we know the
                // YubiKey is already clear, so no need to do it again.
                // This happens when the YubiKey is not set and we want to set it
                // the first time.
                newAlgorithm = false;
            }

            // If we're using a new algorithm, we need to Clear so that we can
            // change the management key (the ChangeManagementKey method throws
            // an exception if the YubiKey is still PinOnly).
            // The newAlgorithm variable is also set to true if the requested
            // mode is None.
            // We'll want to clear in that case as well, but we will also want to
            // simply return. Once it's clear, there's nothing left to do. We
            // don't want to fall through and update AdminData, the Clear will
            // take care of that.
            if (newAlgorithm)
            {
                ClearPinOnly(currentPinOnlyMode, specialKeyCollector);

                if (pinOnlyMode == PivPinOnlyMode.None)
                {
                    return;
                }
            }

            // At this point, the mgmt key is in the specialKeyCollector's
            // currentKey buffer.
            // If it was PinProtected, currentMode will contain PinProtected and
            // that is the current key.
            // If it was PinDerived but not PinProtected, currentMode will
            // contain PinDerived and that is the current key.
            // If it was authenticated not using PIN-only, then the currentMode
            // contains neither Protected nor Derived.

            if (newPinOnlyMode.HasFlag(PivPinOnlyMode.PinDerived))
            {
                // This will also check to see if we need to set PinProtected
                // as well, but the newMode is not set yet.
                // That happens if the current mode is Protected, but the new
                // mode is Derived. We need to set Derived, but then reset
                // Protected to the Derived value.
                SetYubiKeyPinDerived(
                    adminData, currentPinOnlyMode, mgmtKeyAlgorithm, specialKeyCollector, userKeyCollector, ref newPinOnlyMode);
            }

            if (newPinOnlyMode.HasFlag(PivPinOnlyMode.PinProtected))
            {
                SetYubiKeyPinProtected(adminData, mgmtKeyAlgorithm, specialKeyCollector);
            }

            // Update the ADMIN DATA in case we changed anything.
            // If the currentMode is UnavailablePinDerived, then don't update
            // AdminData. It was something other than defined value, so we're
            // leaving it.
            if (!currentPinOnlyMode.HasFlag(PivPinOnlyMode.PinDerivedUnavailable))
            {
                WriteObject(adminData);
            }
        }

        // Check the currentMode against pinOnlyMode, newAlgorithm, tested, and
        // testedUnavailable.
        // Set newMode and return a value based on the results of the comparison
        // The tested arg is what we're testing for, PinProtected or PinDerived.
        private static PinOnlyCheck CheckPinOnlyStatus(
            PivPinOnlyMode currentMode,
            PivPinOnlyMode pinOnlyMode,
            PivPinOnlyMode tested,
            PivPinOnlyMode testedUnavailable,
            bool newAlgorithm,
            ref PivPinOnlyMode newMode)
        {
            // Look at PinProtected.
            // At this point, if the requested mode is PinProtected, and the
            // currentMode is PinProtected, we're done ...
            //  ... but only if the requested algorithm is the same as the
            //  current.
            // If it is currently PinProtected and the caller has asked
            // for a new algorithm, make sure newMode is set to indicate
            // PinProtected.
            // Also, if the caller has requested PinProtected and it is not
            // PinProtected, make sure to set newMode.
            // Now look at PinDerived, it is the same story.
            if (currentMode.HasFlag(tested))
            {
                if (newAlgorithm)
                {
                    newMode |= tested;
                }
                else if (pinOnlyMode == tested)
                {
                    // Note that we're making this check and returning this value
                    // so that we don't have to run unnecessary code and we avoid
                    // dealing with the case where the "other mode" is
                    // unavailable.
                    return PinOnlyCheck.Complete;
                }
            }

            else if (currentMode.HasFlag(testedUnavailable))
            {
                if (pinOnlyMode.HasFlag(tested))
                {
                    return PinOnlyCheck.Unavailable;
                }
            }

            // At this point currentMode contains None for the tested (what we're
            // testing for is neither set nor unavailable).
            else if (pinOnlyMode.HasFlag(tested))
            {
                newMode |= tested;
            }

            return PinOnlyCheck.CanContinue;
        }

        // Clear any data indicating PIN-only, and set the management key to
        // default.
        // Set PRINTED (if needed) and ADMIN DATA to empty.
        // Then set the management key to the default.
        // Note that it is possible that the ADMIN DATA contains a value
        // for PinLastUpdated. But we are using that field only if the
        // YubiKey is PIN-only, so it is safe to lose it.
        // Well, if a particular storage location indicates Unavailable, don't
        // clear that one.
        private void ClearPinOnly(PivPinOnlyMode currentMode, SpecialKeyCollector specialKeyCollector)
        {
            if (!currentMode.HasFlag(PivPinOnlyMode.PinProtectedUnavailable))
            {
                PutEmptyData((int)PivDataTag.Printed);
            }

            if (!currentMode.HasFlag(PivPinOnlyMode.PinDerivedUnavailable))
            {
                PutEmptyData(AdminDataDataTag);
            }

            specialKeyCollector.SetKeyData(
                SpecialKeyCollector.SetKeyDataDefault, ReadOnlyMemory<byte>.Empty, isNewKey: true,
                PivAlgorithm.TripleDes);

            specialKeyCollector.ChangeManagementKey(this, PivAlgorithm.TripleDes);
        }

        private void PutEmptyData(int dataTag)
        {
            byte[] emptyObject = { 0x53, 0x00 };

            var command = new PutDataCommand(dataTag, emptyObject);
            var response = Connection.SendCommand(command);
            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }
        }

        // Call this method only if the caller's pinOnlyMode requests
        // PIN-derived, and the YubiKey is not yet PIN-derived.
        // Set the YubiKey to be PIN-derived. Generate a new salt, derive a key,
        // set the mgmt key, and make sure the current key in specialKeyCollector
        // is set to this new value.
        // If the YubiKey is currently set to PIN-protected, then we'll need to
        // store this new key in PRINTED. That is, we'll need to set
        // PIN-protected. If that's the case, authMode will be set to
        // PinProtected. Set the ref arg setPinProtected to true in this case.
        // Otherwise, leave that arg alone.
        private void SetYubiKeyPinDerived(
            AdminData adminData,
            PivPinOnlyMode currentMode,
            PivAlgorithm mgmtKeyAlgorithm,
            SpecialKeyCollector specialKeyCollector,
            Func<KeyEntryData, bool>? userKeyCollector,
            ref PivPinOnlyMode newMode)
        {
            // We need the actual PIN in order to derive the mgmt key, so even if
            // the PIN has already been verified, collect it.
            // This method will do nothing if the PIN has already been collected.
            specialKeyCollector.VerifyPinAndSave(this, userKeyCollector);

            // If the currentMode says the mgmt key had been authenticated using
            // PinProtected, we're going to "delete" that management key and set
            // it to the derived value. Then we're going to set the protected key
            // to the derived as well.
            if (currentMode.HasFlag(PivPinOnlyMode.PinProtected))
            {
                ClearPinOnly(currentMode, specialKeyCollector);
                newMode |= PivPinOnlyMode.PinProtected;
            }

            var saltBytes = specialKeyCollector.DeriveKeyData
                (ReadOnlyMemory<byte>.Empty, mgmtKeyAlgorithm, isNewKey: true);

            // Call this method instead of the PivSession.Change method directly,
            // because this method will update the current key with the new key.
            specialKeyCollector.ChangeManagementKey(this, mgmtKeyAlgorithm);
            _ = BlockPinOrPuk(PivSlot.Puk);
            
            adminData.SetSalt(saltBytes);
            adminData.PukBlocked = true;
        }

        // This can be called under two conditions. One, the YubiKey is currently
        // not PinProtected and the caller requested it. Or two, the YubiKey was
        // PinProtected, but not PinDerived, and the caller just set this to
        // PinDerived.
        // If this a new PinProtected, generate a new mgmt key and store it.
        // If the current key in specialKeyCollector is not the default, use that
        // key data. That data is either the "pre-existing" mgmt key data, or it
        // is the PIN-derived data.
        private void SetYubiKeyPinProtected(
            AdminData adminData,
            PivAlgorithm mgmtKeyAlgorithm,
            SpecialKeyCollector specialKeyCollector)
        {
            if (specialKeyCollector.IsCurrentKeyDefault())
            {
                specialKeyCollector.SetKeyData(
                    SpecialKeyCollector.SetKeyDataRandom, ReadOnlyMemory<byte>.Empty, isNewKey: true, mgmtKeyAlgorithm);

                specialKeyCollector.ChangeManagementKey(this, mgmtKeyAlgorithm);
            }

            _ = BlockPinOrPuk(PivSlot.Puk);
            adminData.PukBlocked = true;
            adminData.PinProtected = true;

            using var pinProtect = new PinProtectedData();
            pinProtect.SetManagementKey(specialKeyCollector.GetCurrentMgmtKey());

            WriteObject(pinProtect);
        }

        // Someone wants to change the PIN. Get the mode we're going to set the
        // YubiKey to after changing the PIN. That is, this is not the current
        // mode, it is the "Change PIN mode", the mode we will set the YubiKey
        // after the PIN has been changed.
        // If the Change PIN mode includes PinDerived, this method will clear the
        // PIN-only data and set the mgmt key to default.
        // If the YubiKey is not PinDerived, set mode to None and return true.
        // Note that if the YubiKey is PinProtected only, we're still setting the
        // mode to None.
        // If it is PinDerived, verify the PIN.
        // If it does not verify, return false.
        // If it does verify, derive the mgmt key and authenticate.
        // If it authenticates, check to see if the YubiKey is also PinProtected.
        // If so, OR in PinProtected to the mode.
        // If the derived mgmt key does not authenticate, set mode to None and
        // return true. This is because we're saying that although the ADMIN DATA
        // says PinDerived, because the PIN-derived key does not authenticate, it
        // really is not PIN-derived.
        // The only way to get a false return is if the PIN does not verify, and
        // even then, only if the ADMIN DATA says the mgmt key is PinDerived.
        private bool TryGetChangePinMode(ReadOnlyMemory<byte> pin, out PivPinOnlyMode mode, out int? retriesRemaining)
        {
            retriesRemaining = null;

            mode = PivPinOnlyMode.None;

            var userKeyCollectorFunc = KeyCollector;
            using var specialKeyCollector = new SpecialKeyCollector();

            bool isValid = TryReadObject(out AdminData adminData);

            try
            {
                if (!isValid || adminData.Salt is null)
                {
                    return true;
                }

                // If AdminData says PinDerived, then derive the mgmt key and
                // authenticate it.
                // In order to do that we need to verify the PIN.
                isValid = pin.Length switch
                {
                    0 => specialKeyCollector.TryVerifyPinAndSave(this, userKeyCollectorFunc, out retriesRemaining),
                    _ => specialKeyCollector.TrySetPin(this, pin, out retriesRemaining)
                };

                if (!isValid)
                {
                    return false;
                }

                var salt = (ReadOnlyMemory<byte>)adminData.Salt;

                _ = specialKeyCollector.DeriveKeyData(salt, ManagementKeyAlgorithm, isNewKey: false);

                specialKeyCollector.SetKeyData(
                    SpecialKeyCollector.SetKeyDataDefault, ReadOnlyMemory<byte>.Empty, isNewKey: true,
                    PivAlgorithm.TripleDes);

                // If this fails, then the mgmt key is not PIN-derived from the
                // PIN and salt, so we'll say it is not PIN-derived.
                if (!TryForcedChangeManagementKey(
                        specialKeyCollector.GetCurrentMgmtKey(),
                        specialKeyCollector.GetNewMgmtKey(),
                        PivTouchPolicy.Never,
                        PivAlgorithm.TripleDes))
                {
                    return true;
                }

                // We now know we are going to set the mgmt key to PIN-derived
                // after changing the PIN.
                mode = PivPinOnlyMode.PinDerived;

                // Will we need to set the YubiKey to PIN-protected as well?
                // If there is data in PRINTED, and it contains the same mgmt key
                // that was derived from the PIN and Salt, then yes.
                isValid = TryReadObject(out PinProtectedData pinProtect);

                using (pinProtect)
                {
                    if (isValid && !(pinProtect.ManagementKey is null))
                    {
                        var mgmtKey = (ReadOnlyMemory<byte>)pinProtect.ManagementKey;

                        if (specialKeyCollector.GetCurrentMgmtKey().Span.SequenceEqual(mgmtKey.Span))
                        {
                            mode |= PivPinOnlyMode.PinProtected;
                        }
                    }
                }

                // Clear appropriate storage locations.
                if (mode.HasFlag(PivPinOnlyMode.PinProtected))
                {
                    PutEmptyData((int)PivDataTag.Printed);
                }

                if (mode.HasFlag(PivPinOnlyMode.PinDerived))
                {
                    PutEmptyData(adminData.DataTag);
                }
            }
            finally
            {
                adminData.Dispose();
                KeyCollector = userKeyCollectorFunc;
            }

            return true;
        }

        private enum PinOnlyCheck
        {
            Unavailable = 0,
            Complete = 1,
            CanContinue = 2
        }

        // This class keeps track of the key data and its length.
        private sealed class MgmtKeyHolder : IDisposable
        {
            private const int PinDerivedSaltLength = 16;
            private const int MaxKeyLength = 32;
            private readonly byte[] _keyBuffer = new byte[MaxKeyLength];
            private readonly Memory<byte> _keyData;

            private bool _disposed;

            public MgmtKeyHolder()
            {
                _keyData = new Memory<byte>(_keyBuffer);
                KeyData = _keyData;

                _disposed = false;
            }

            // This property will be the key data, of the appropriate length.
            public Memory<byte> KeyData { get; private set; }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                CryptographicOperations.ZeroMemory(_keyData.Span);
                _disposed = true;
            }

            // Copy the newData into the local buffer.
            // Set the KeyData property to the actual key data. That is, if the
            // newData is smaller than the internal buffer, then KeyData will be
            // a Slice that contains only the new data bytes.
            // If the newData is Empty, generate random bytes and set the length
            // based on the algorithm
            public void SetKeyData(ReadOnlyMemory<byte> newData, PivAlgorithm algorithm)
            {
                int newLength = algorithm switch
                {
                    PivAlgorithm.Aes128 => 16,
                    PivAlgorithm.Aes256 => 32,
                    _ => 24
                };

                if (!newData.IsEmpty)
                {
                    newData.CopyTo(_keyData);
                }
                else
                {
                    using var randomObject = CryptographyProviders.RngCreator();

                    do
                    {
                        randomObject.GetBytes(_keyBuffer, offset: 0, newLength);
                    }
                    while (IsKeyDataWeak(algorithm));
                }

                KeyData = _keyData.Slice(start: 0, newLength);
            }

            // Derive a key of the appropriate length (based on algorithm), using
            // the pin and salt.
            // Return the salt.
            // It is the responsibility of the caller to make sure the pin is the
            // correct length.
            // It will also set the KeyData of the MgmtKeyHolder
            public ReadOnlyMemory<byte> DeriveKeyData(
                ReadOnlyMemory<byte> pin,
                ReadOnlyMemory<byte> salt,
                PivAlgorithm algorithm)
            {
                var returnValue = salt;

                if (salt.Length != PinDerivedSaltLength)
                {
                    byte[] saltData = new byte[PinDerivedSaltLength];
                    returnValue = new ReadOnlyMemory<byte>(saltData);
                    using var randomObject = CryptographyProviders.RngCreator();

                    do
                    {
                        randomObject.GetBytes(saltData, offset: 0, PinDerivedSaltLength);
                        PerformKeyDerive(pin, saltData, algorithm);
                    }
                    while (IsKeyDataWeak(algorithm));
                }
                else
                {
                    PerformKeyDerive(pin, salt.ToArray(), algorithm);
                }

                return returnValue;
            }

            // Derive a key from the PIN and salt (iteration count = 10,000).
            // Place the result into the derivedKey buffer. The result will be 24
            // bytes, the derivedKey buffer must be of Length 24, no more no less.
            public void PerformKeyDerive(ReadOnlyMemory<byte> pin, byte[] saltData, PivAlgorithm algorithm)
            {
                int newLength = algorithm switch
                {
                    PivAlgorithm.Aes128 => 16,
                    PivAlgorithm.Aes256 => 32,
                    _ => 24
                };

                byte[] result = Array.Empty<byte>();
                byte[] pinData = pin.ToArray();

                try
                {
                    // This will use PBKDF2, with the PRF of HMAC with SHA-1.
#pragma warning disable CA5379, CA5387 // These warnings complain about SHA-1 and <100,000 iterations, but we use it to be backwards-compatible.
                    using var kdf = new Rfc2898DeriveBytes(pinData, saltData, iterations: 10000);
                    result = kdf.GetBytes(newLength);
#pragma warning restore CA5379, CA5387
                    Array.Copy(result, _keyBuffer, newLength);
                    KeyData = _keyData.Slice(start: 0, newLength);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(result);
                    CryptographicOperations.ZeroMemory(pinData);
                }
            }

            // Check to see if the key is weak.
            // If the algorithm is 3DES, then ...
            // If bytes 0 - 7 are the same as 8 - 15, or 8 - 15 are the same as
            // 16 - 23, this is a week key.
            // If not 3DES, it is not weak.
            public bool IsKeyDataWeak(PivAlgorithm algorithm)
            {
                if (algorithm == PivAlgorithm.TripleDes)
                {
                    if (_keyData.Span.Slice(start: 0, length: 8).SequenceEqual(_keyData.Span.Slice(start: 8, length: 8))
                        || _keyData.Span.Slice(start: 8, length: 8)
                            .SequenceEqual(_keyData.Span.Slice(start: 16, length: 8)))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private sealed class SpecialKeyCollector : IDisposable
        {
            public const int SetKeyDataBuffer = 1;
            public const int SetKeyDataRandom = 2;
            public const int SetKeyDataDefault = 4;

            private const int MaxPinLength = 8;
            private readonly MgmtKeyHolder _currentKey;
            private readonly Memory<byte> _defaultKey;
            private readonly MgmtKeyHolder _newKey;
            private readonly byte[] _pinData = new byte[MaxPinLength];
            private readonly Memory<byte> _pinMemory;

            private bool _disposed;
            private int _pinLength;

            public SpecialKeyCollector()
            {
                _defaultKey = new Memory<byte>(
                    new byte[]
                    {
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
                    });

                _currentKey = new MgmtKeyHolder();
                _newKey = new MgmtKeyHolder();

                // Make sure the current key is init to the default.
                _currentKey.SetKeyData(_defaultKey, PivAlgorithm.TripleDes);

                PinCollected = false;
                _pinMemory = new Memory<byte>(_pinData);
                _pinLength = 0;

                _disposed = false;
            }

            public bool PinCollected { get; private set; }

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

                _currentKey.Dispose();
                _newKey.Dispose();
                CryptographicOperations.ZeroMemory(_pinMemory.Span);
                _disposed = true;
            }

            // Check to see if the data is the default mgmt key.
            public bool IsCurrentKeyDefault() => _defaultKey.Span.SequenceEqual(_currentKey.KeyData.Span);

            // Set either the current or new mgmt key.
            // Set to either the data in the buffer, new random data, or the
            // default.
            // If the setFlag is SetKeyDataBuffer (1), set the appropriate buffer
            // to be the input. This will copy the key data, not simply a
            // reference. This will not check the length, it is the
            // responsibility of the caller not to make mistakes.
            // If the setFlag is SetKeyDataRandom (2), generate new, random key
            // data and store it in the appropriate buffer. Ignore the keyData
            // arg.
            // If the setFlag is SetKeyDataDefault (4), set the appropriate
            // buffer to be the 24 bytes of the default management key.
            // If setFlag is any other value, this method will set the key to be
            // default.
            // If isNewKey is true, set _newKey.
            // If isNewKey is false, set _currentKey.
            // If generating new key data, this will reject weak keys.
            public void SetKeyData(int setFlag, ReadOnlyMemory<byte> keyData, bool isNewKey, PivAlgorithm algorithm)
            {
                var destinationKeyHolder = isNewKey ? _newKey : _currentKey;

                if (setFlag == SetKeyDataBuffer)
                {
                    destinationKeyHolder.SetKeyData(keyData, algorithm);

                    return;
                }

                if (setFlag == SetKeyDataRandom)
                {
                    destinationKeyHolder.SetKeyData(ReadOnlyMemory<byte>.Empty, algorithm);

                    return;
                }

                destinationKeyHolder.SetKeyData(_defaultKey, PivAlgorithm.TripleDes);
            }

            // Derive the mgmt key from the PIN in this object, along with the
            // salt provided. If no salt is provided, generate a new 16-byte
            // salt. If the input salt is not exactly 16 bytes, then this method
            // considers that no salt provided. An input of Length 0 is no salt,
            // but also an input Length of 5 or 17.
            // Return the salt. That is, if a salt is provided, return it. If
            // not, return the new data.
            // This hard codes the iteration count to 10,000.
            // This will place the resulting bytes into either the newKey buffer
            // (if isNewKey is true), or the currentKey buffer (if isNewKey is
            // false).
            // If the new key generated using a random salt is weak, this method
            // will generate a new salt and try again.
            // But this method will generate whatever key (weak or not) is the
            // result if a salt is given.
            public ReadOnlyMemory<byte> DeriveKeyData(ReadOnlyMemory<byte> salt, PivAlgorithm algorithm, bool isNewKey)
            {
                var destinationKeyHolder = isNewKey ? _newKey : _currentKey;

                return destinationKeyHolder.DeriveKeyData(_pinMemory.Slice(start: 0, _pinLength), salt, algorithm);
            }

            // Change the management key from what is in current to what is in
            // new. Make sure after the change that the contents of current are
            // set to the contents of new.
            public void ChangeManagementKey(PivSession pivSession, PivAlgorithm algorithm)
            {
                pivSession.ChangeManagementKey(PivTouchPolicy.Never, algorithm);
                SetKeyData(SetKeyDataBuffer, _newKey.KeyData, isNewKey: false, algorithm);
            }

            // Obtain the mgmt key and authenticate it, make sure the mgmt key is set
            // in the SpecialKeyCollector.
            // This method assumes the special key collector is in a state just after
            // instantiation.
            // If the user cancels (the userKeyCollector returns false), this method
            // will throw an exception.
            // This method assumes the special key collector is in the PivSession's
            // KeyCollector and it expects the default mgmt key is the current key.
            // Upon construction of the SpecialKeyCollector, the default is placed
            // into the current key. So if this method is called after construction
            // but before anything else is called, this will be fine.
            // If the mgmt key cannot be authenticated, this method throws an
            // exception.
            // Upon completion of this method, the correct mgmt key is in the current
            // key.
            public void AuthMgmtKeyAndSave(
                PivSession pivSession,
                Func<KeyEntryData, bool>? userKeyCollector)
            {
                // First, try the default key. If it works, we're done.
                // If we reach this point, the special key collector has just been
                // instantiated, so the default key is in the current key spot.
                if (pivSession.TryAuthenticateWithKeyCollector(true))
                {
                    return;
                }

                // If the default did not authenticate, use the caller-supplied
                // KeyCollector to obtain the key.
                if (userKeyCollector is null)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.MissingKeyCollector));
                }

                var keyEntryData = new KeyEntryData
                {
                    Request = KeyEntryRequest.AuthenticatePivManagementKey
                };

                try
                {
                    while (userKeyCollector(keyEntryData))
                    {
                        SetKeyData(
                            SetKeyDataBuffer, keyEntryData.GetCurrentValue(), isNewKey: false,
                            pivSession.ManagementKeyAlgorithm);

                        if (pivSession.TryAuthenticateWithKeyCollector(true))
                        {
                            return;
                        }

                        keyEntryData.IsRetry = true;
                    }
                }
                finally
                {
                    keyEntryData.Clear();

                    keyEntryData.Request = KeyEntryRequest.Release;
                    _ = userKeyCollector(keyEntryData);
                }

                // If the user cancels, throw the canceled exception.
                throw new OperationCanceledException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncompleteCommandInput));
            }

            // Return a reference to the current management key.
            public ReadOnlyMemory<byte> GetCurrentMgmtKey() => _currentKey.KeyData;

            // Return a reference to the new management key.
            public ReadOnlyMemory<byte> GetNewMgmtKey() => _newKey.KeyData;

            // If the PIN is already collected, do nothing.
            // If not, obtain the PIN and verify it, set the PinCollected
            // property.
            // If the If the user cancels (the userKeyCollector returns false),
            // this method will throw an exception.
            public void VerifyPinAndSave(
                PivSession pivSession,
                Func<KeyEntryData, bool>? userKeyCollector)
            {
                if (!TryVerifyPinAndSave(pivSession, userKeyCollector, out _))
                {
                    // If the user cancels, throw the canceled exception.
                    throw new OperationCanceledException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.IncompleteCommandInput));
                }
            }

            // Verify the PIN and save it in this.
            // If the user cancels, return false.
            public bool TryVerifyPinAndSave(
                PivSession pivSession,
                Func<KeyEntryData, bool>? userKeyCollector,
                out int? retriesRemaining)
            {
                retriesRemaining = null;

                if (PinCollected)
                {
                    return true;
                }

                if (userKeyCollector is null)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.MissingKeyCollector));
                }

                var keyEntryData = new KeyEntryData
                {
                    Request = KeyEntryRequest.VerifyPivPin
                };

                try
                {
                    while (userKeyCollector(keyEntryData))
                    {
                        if (TrySetPin(pivSession, keyEntryData.GetCurrentValue(), out retriesRemaining))
                        {
                            return true;
                        }

                        keyEntryData.IsRetry = true;
                        keyEntryData.RetriesRemaining = retriesRemaining;
                    }
                }
                finally
                {
                    keyEntryData.Clear();

                    keyEntryData.Request = KeyEntryRequest.Release;
                    _ = userKeyCollector(keyEntryData);
                }

                // We reach this point if the user cancels.
                return false;
            }

            // Set the PIN data in this object to the input data.
            // Verify it. If it verifies, then the PIN has been collected.
            public bool TrySetPin(PivSession pivSession, ReadOnlyMemory<byte> pin, out int? retriesRemaining)
            {
                pin.CopyTo(_pinMemory);
                _pinLength = pin.Length;

                PinCollected = pivSession.TryVerifyPin(pin, out retriesRemaining);

                return PinCollected;
            }

            // This is the KeyCollector delegate.
            public bool KeyCollectorSpecial(KeyEntryData keyEntryData)
            {
                if (keyEntryData is null)
                {
                    return false;
                }

                if (keyEntryData.IsRetry)
                {
                    return false;
                }

                switch (keyEntryData.Request)
                {
                    default:
                        return false;

                    case KeyEntryRequest.Release:
                        return true;

                    case KeyEntryRequest.AuthenticatePivManagementKey:
                        keyEntryData.SubmitValue(_currentKey.KeyData.Span);

                        return true;

                    case KeyEntryRequest.ChangePivManagementKey:
                        keyEntryData.SubmitValues(_currentKey.KeyData.Span, _newKey.KeyData.Span);

                        return true;

                    case KeyEntryRequest.VerifyPivPin:
                        keyEntryData.SubmitValue(_pinMemory.Slice(start: 0, _pinLength).Span);

                        return true;
                }
            }
        }
    }
}
