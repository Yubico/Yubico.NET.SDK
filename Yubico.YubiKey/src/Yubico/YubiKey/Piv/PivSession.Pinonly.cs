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
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Piv.Objects;
using Yubico.YubiKey.Cryptography;
using Yubico.Core.Logging;

namespace Yubico.YubiKey.Piv
{
    // This portion of the PivSession class contains code related to PIN-only
    // mode (PIN-protected and PIN-derived) operations.
    public sealed partial class PivSession : IDisposable
    {
        private const int AdminDataDataTag = 0x005FFF00;

        /// <summary>
        /// Return an enum indicating the PIN-only mode, if any, for which the
        /// YubiKey PIV application is configured.
        /// </summary>
        /// <remarks>
        /// PIN-only mode means that the application does not need to enter the
        /// management key in order to perform PIV operations that normally
        /// require it, only the PIN is needed.
        /// <para>
        /// See the User's Manual entry on
        /// <xref href="UsersManualPivPinOnlyMode"> PIV PIN-only mode</xref> for
        /// a deeper discussion of this feature.
        /// </para>
        /// <para>
        /// This returns a result based on the contents of ADMIN DATA. That
        /// storage location contains information about PIN-protected and
        /// PIN-derived. It is possible for a different application to overwrite
        /// the data to make it inaccurate. That is unlikely, however, if all
        /// applications follow good programming practices outlined by the SDK
        /// documentation. This method will not actually verify the management
        /// key in order to ensure the return value is correct.
        /// </para>
        /// <para>
        /// If the ADMIN DATA is overwritten, it is possible to call
        /// <see cref="TryRecoverPinOnlyMode()"/> to restore the YubiKey to a
        /// proper PIN-only state.
        /// </para>
        /// <para>
        /// Note that the return is a bit field and the return can be one or more
        /// of the bits set. There are bits that indicate a YubiKey is
        /// unavailable for PIN-protected or PIN-derived. Call this method before
        /// trying to set a YubiKey to PIN-only to make sure it is not already
        /// set, and if not, it can be set.
        /// </para>
        /// <para>
        /// Note that this returns the PIN-only mode for the PIV application on
        /// the YubiKey, it has nothing to do with OATH, FIDO, or OpenPGP.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A <c>PivPinOnlyMode</c>, which is an enum indicating the mode or
        /// modes.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey is not able to return the ADMIN DATA.
        /// </exception>
        public PivPinOnlyMode GetPinOnlyMode()
        {
            _log.LogInformation("Get the PIV PIN-only mode of a YubiKey based on AdminData.");

            PivPinOnlyMode returnValue = PivPinOnlyMode.PinProtectedUnavailable | PivPinOnlyMode.PinDerivedUnavailable;
            if (TryReadObject<AdminData>(out AdminData adminData))
            {
                returnValue = PivPinOnlyMode.None;
                if (adminData.PinProtected)
                {
                    returnValue |= PivPinOnlyMode.PinProtected;
                }
                if (!(adminData.Salt is null))
                {
                    returnValue |= PivPinOnlyMode.PinDerived;
                }

                adminData.Dispose();
            }

            return returnValue;
        }

        /// <summary>
        /// Try to recover the PIN-only state. If successful, this will
        /// authenticate the management key and reset the ADMIN DATA and or
        /// PRINTED storage locations.
        /// &gt; [!WARNING]
        /// &gt; This can overwrite the contents of ADMIN DATA and/or PRINTED. If
        /// &gt; some other application relies on that data it will be lost.
        /// </summary>
        /// <remarks>
        /// See the User's Manual entry on
        /// <xref href="UsersManualPivPinOnlyMode"> PIV PIN-only mode</xref> for
        /// a deeper discussion of this operation.
        /// <para>
        /// The ADMIN DATA contains information about PIN-only. The PIN-protected
        /// management key is stored in PRINTED. Applications should never store
        /// information in those locations, only Yubico-supplied products should
        /// use them. However, it is possible for an application to overwrite the
        /// contents of one or both of these storage locations, making the
        /// PIN-only data inaccurate.
        /// </para>
        /// <para>
        /// This method will obtain the data stored in the two storage locations,
        /// and determine if they contain PIN-only data that can be used to
        /// authenticate the management key. If it can't, it will return
        /// <c>PivPinOnlyMode.None</c> or <c>Unavailable</c>. If it can, it will
        /// authenticate and set the ADMIN DATA and PRINTED to contain data
        /// compatible with correct PIN-only modes. It will return a
        /// <c>PivPinOnlyMode</c> value indicating which mode is set.
        /// </para>
        /// <para>
        /// For example, suppose the data in both is correct, and it indicates
        /// the management key is PIN-protected. After calling this method, the
        /// management key will be authenticated, the storage locations will not
        /// be changed, and the return will be <c>PivPinOnlyMode.PinProtected</c>.
        /// </para>
        /// <para>
        /// Another possibility is the ADMIN DATA was overwritten by some
        /// application so it is inaccurate, but the PIN-protected data is still
        /// in PRINTED. This method will be able to authenticate the management
        /// key using that data. It will replace the contents of ADMIN DATA with
        /// correct PIN-only information and return
        /// <c>PivPinOnlyMode.PinProtected</c>.
        /// </para>
        /// <para>
        /// If ADMIN DATA and PRINTED contain no data, or if ADMIN DATA contains
        /// correct information that indicates the YubiKey is not set to PIN-only
        /// mode, then this method will not authenticate the management key, it
        /// will not put any data into the storage locations, and it will return
        /// <c>PivPinOnlyMode.None</c>.
        /// </para>
        /// <para>
        /// It is possible this method is not able to recover. For example,
        /// suppose the ADMIN DATA is correct and indicates the YubiKey is
        /// PIN-protected, but not PIN-derived (there is no salt to use to derive
        /// a key), but the data in PRINTED is not correct. In this case, the
        /// method will not be able to authenticate the management key as
        /// PIN-protected. It will try to authenticate using the default
        /// management key, and if that does not work, it will call on the
        /// <c>KeyCollector</c> to obtain the it. If that does succeeds, it will
        /// set ADMIN DATA to indicate the YubiKey is not PIN-protected, it will
        /// clear the contents of PRINTED, and it will return
        /// <c>PivPinOnlyMode.None</c>. If the <c>KeyColletor</c> is not able to
        /// provide the management key, this method will not be able to reset the
        /// ADMIN DATA nor PRINTED (management key authentication is necessary to
        /// set a storage location), and will return <c>Unavailable</c>.
        /// </para>
        /// <para>
        /// This method will require the PIN to be verified. It is possible that
        /// the PIN has already been verified and this method will verify it
        /// again. If it needs to verify the PIN, it will call on the
        /// <c>KeyCollector</c> to obtain it.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A <c>PivPinOnlyMode</c>, which is an enum indicating the mode or
        /// modes the YubiKey is in.
        /// </returns>
        public PivPinOnlyMode TryRecoverPinOnlyMode()
        {
            _log.LogInformation("Try to authenticate using PIN-only.");

            PivPinOnlyMode returnValue = TryAuthenticatePinOnly(false);

            // If the result is None, or PinProtected, or PinDerived, or
            // PinProtected | PinDerived, then everythng is fine, just return.
            // In orther words, if it does not contain an Unavailable.
            if (!returnValue.HasFlag(PivPinOnlyMode.PinProtectedUnavailable) &&
                !returnValue.HasFlag(PivPinOnlyMode.PinDerivedUnavailable))
            {
                return returnValue;
            }

            // If we reach this point, either PinProtectedUnavailable or
            // PinDerivedUnavailable is (or both are) set.
            // If the returnValue contains PinProtected, then we know the PRINTED
            // data is correct and the mgmt key has been authenticated. But we
            // also know that PinDerivedUnavailable is set. That means the ADMIN
            // DATA is wrong. We need to reset it.
            if (returnValue.HasFlag(PivPinOnlyMode.PinProtected))
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
            // If the returnValue contains PinDeriveed, then we know the ADMIN
            // DATA is "correct", and the mgmt key was authenticated. But PRINTED
            // is incorrect.
            // Reset PRINTED to empty, and make sure ADMIN DATA indicates PUK
            // blocked and PinProtected is false.
            if (returnValue.HasFlag(PivPinOnlyMode.PinDerived))
            {
                // Read the AdminData to get the salt.
                AdminData adminData = ReadObject<AdminData>();

                // Make sure the PUK is blocked. It probably is, but we're going
                // to set the PukBlocked field in adminData to true, so make sure
                // it is indeed true.
                _ = BlockPinOrPuk(PivSlot.Puk);
                adminData.PukBlocked = true;
                adminData.PinProtected = false;
                WriteObject(adminData);

                using var pinProtect = new PinProtectedData();
                WriteObject(pinProtect);

                return PivPinOnlyMode.PinDerived;
            }

            // At this point, neither PinProtected nor PinDerived is set. That
            // means the mgmt key is not authenticated.
            // If we can authenticate the mgmt key, then set ADMIN DATA and
            // PRINTED.
            Func<KeyEntryData, bool>? UserKeyCollector = KeyCollector;
            using var specialKeyCollector = new SpecialKeyCollector();

            try
            {
                specialKeyCollector.AuthMgmtKeyAndSave(this, UserKeyCollector);

                // If the PinDerivedUnavailable bit is not set, that means either
                // there was no ADMIN DATA, or it was "correct". If it was
                // "correct, we want to leave it as is, except make sure the
                // PinProtected property is false and the Salt is null.
                // If that bit is set, then we want to clear ADMIN DATA.
                using AdminData adminData = returnValue.HasFlag(PivPinOnlyMode.PinDerivedUnavailable) ?
                    new AdminData() : ReadObject<AdminData>();
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
                if (returnValue.HasFlag(PivPinOnlyMode.PinProtectedUnavailable))
                {
                    using var pinProtect = new PinProtectedData();
                    WriteObject(pinProtect);
                }

                return PivPinOnlyMode.None;
            }
            catch (InvalidOperationException)
            {
                return returnValue;
            }
            catch (OperationCanceledException)
            {
                return returnValue;
            }
            finally
            {
                KeyCollector = UserKeyCollector;
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

            PivPinOnlyMode returnValue = PivPinOnlyMode.None;
            if (trustAdminData)
            {
                returnValue = GetPinOnlyMode();

                tryPinProtected = returnValue.HasFlag(PivPinOnlyMode.PinProtected);
                tryPinDerived = returnValue.HasFlag(PivPinOnlyMode.PinDerived);
            }

            Func<KeyEntryData, bool>? UserKeyCollector = KeyCollector;
            using var specialKeyCollector = new SpecialKeyCollector();

            try
            {
                if (tryPinProtected)
                {
                    returnValue = GetPrintedPinProtectedStatus(specialKeyCollector, UserKeyCollector);

                    if (trustAdminData && returnValue.HasFlag(PivPinOnlyMode.PinProtected))
                    {
                        return returnValue;
                    }
                }

                if (tryPinDerived)
                {
                    using var adminData = new AdminData();
                    returnValue |= GetPinDerivedStatus(
                        adminData, returnValue.HasFlag(PivPinOnlyMode.PinProtected), specialKeyCollector, UserKeyCollector);
                }

                return returnValue;
            }
            finally
            {
                KeyCollector = UserKeyCollector;
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
            Func<KeyEntryData, bool>? UserKeyCollector)
        {
            // We could call the ReadObject method, but if the PIN is not
            // verified, ReadObject won't collect and save it.
            // Hence, in order to be able to call VerifyPinAndSave, but only if
            // needed, call the GetDataCommand directly.
            var getDataCommand = new GetDataCommand((int)PivDataTag.Printed);
            GetDataResponse getDataResponse = Connection.SendCommand(getDataCommand);

            if (getDataResponse.Status == ResponseStatus.AuthenticationRequired)
            {
                specialKeyCollector.VerifyPinAndSave(this, UserKeyCollector);
                getDataResponse = Connection.SendCommand(getDataCommand);
            }

            if (getDataResponse.Status == ResponseStatus.NoData)
            {
                return PivPinOnlyMode.None;
            }

            if (getDataResponse.Status == ResponseStatus.Success)
            {
                using var pinProtect = new PinProtectedData();
                if (pinProtect.TryDecode(getDataResponse.GetData()))
                {
                    if (pinProtect.ManagementKey is null)
                    {
                        return PivPinOnlyMode.None;
                    }

                    specialKeyCollector.SetKeyData(
                        SpecialKeyCollector.SetKeyDataBuffer, (ReadOnlyMemory<byte>)pinProtect.ManagementKey, false);
                    KeyCollector = specialKeyCollector.KeyCollectorSpecial;
                    if (TryAuthenticateWithKeyCollector(true))
                    {
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
            Func<KeyEntryData, bool>? UserKeyCollector)
        {
            // We could use the TryReadObject to get the admin data, but that
            // returns a new object. We need to fill the incoming object with the
            // data.
            var getDataCommand = new GetDataCommand(adminData.DataTag);
            GetDataResponse getDataResponse = Connection.SendCommand(getDataCommand);

            if (getDataResponse.Status == ResponseStatus.NoData)
            {
                return PivPinOnlyMode.None;
            }

            if (getDataResponse.Status == ResponseStatus.Success)
            {
                if (adminData.TryDecode(getDataResponse.GetData()))
                {
                    if (adminData.Salt is null)
                    {
                        return PivPinOnlyMode.None;
                    }

                    // If we have already collected the PIN, this call will do
                    // nothing (it won't collect it again).
                    specialKeyCollector.VerifyPinAndSave(this, UserKeyCollector);
                    // If we're already PIN-protected, then the current mgmt key
                    // is the PIN-protected value. So put the derived key into
                    // the new buffer and compare.
                    // If not, put it into the current buffer and authenticate.
                    _ = specialKeyCollector.DeriveKeyData((ReadOnlyMemory<byte>)adminData.Salt, isPinProtected);
                    if (isPinProtected)
                    {
                        if (MemoryExtensions.SequenceEqual(
                            specialKeyCollector.GetCurrentMgmtKey().Span,
                            specialKeyCollector.GetNewMgmtKey().Span))
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
        /// Set the YubiKey's PIV application to be PIN-only. This sets the
        /// YubiKey to either
        /// <code>
        ///   PivPinOnlyMode.PinProtected
        ///   PivPinOnlyMode.PinDerived
        ///   PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived
        ///   PivPinOnlyMode.None
        /// </code>
        /// If the YubiKey is set to PinProtected, PinDerived, or both, the PUK
        /// will also be blocked.
        /// &gt; [!WARNING]
        /// &gt; You should not set a YubiKey for PIN-derived, this feature is
        /// &gt; provided only for backwards compatibility.
        /// </summary>
        /// <remarks>
        /// PIN-only mode means that the application does not need to enter the
        /// management key in order to perform PIV operations that normally
        /// require it, only the PIN is needed.
        /// <para>
        /// See the User's Manual entry on
        /// <xref href="UsersManualPivPinOnlyMode"> PIV PIN-only mode</xref> for
        /// a deeper discussion of this feature.
        /// </para>
        /// <para>
        /// Note that this sets the PIV application on the specific YubiKey to a
        /// PIN-only mode, it has nothing to do with OATH, FIDO, or OpenPGP.
        /// </para>
        /// <para>
        /// Upon successful completion of this method, the PUK will be blocked.
        /// </para>
        /// <para>
        /// If the YubiKey is already set to the specified PIN-protected mode,
        /// this will not reset the YubiKey, but will simply authenticate the
        /// management key.
        /// </para>
        /// <para>
        /// If the YubiKey is already set to PIN-only, but not the specified
        /// mode, this method will make sure the YubiKey is set to both PIN-only
        /// modes.
        /// </para>
        /// <para>
        /// If the requested <c>pinOnlyMode</c> is both, then this will make sure
        /// the YubiKey is both PIN-protected and PIN-derived.
        /// </para>
        /// <para>
        /// If the input <c>pinOnlyMode</c> is <c>None</c>, and the YubiKey is
        /// currently set to PIN-only (and neither PinProtected nor PinDerived is
        /// Unavailable), this method will remove the contents of the storage
        /// locations ADMIN DATA and PRINTED, and reset the management key to the
        /// default:
        /// <code>
        ///   0x01 02 03 04 05 06 07 08
        ///     01 02 03 04 05 06 07 08
        ///     01 02 03 04 05 06 07 08
        /// </code>
        /// The touch policy of the management key will also be set to the
        /// default (Never). Note that the management key must be authenticated
        /// and the PIN verified in order to perform this task. This method will
        /// authenticate the management key using the PIN-only mode to which the
        /// YubiKey is currently set, then clear the contents. It will then
        /// change the management key to the default value. Hence, after setting
        /// the YubiKey out of PIN-only mode, you should change the management
        /// key. Note also that the PUK will still be blocked. The only way to
        /// unblock the PUK is to reset the retry counts. This will set both the
        /// PIN and PUK to their default values. Resetting the retry counts
        /// requires management key authentication.
        /// </para>
        /// <para>
        /// If the input <c>pinOnlyMode</c> is <c>None</c>, and the YubiKey is
        /// currently set to one PIN-only mode, but the other is Unavailable,
        /// this method will clear the one that is set but leave the one that is
        /// Unavailable as is. For example, suppose the current mode is
        /// <c>PinProtected | PinDerivedUnavailable</c>, then after setting to
        /// None, the PRINTED storage area will be empty and the ADMIN DATA
        /// storage area will be left as is.
        /// </para>
        /// <para>
        /// If the input <c>pinOnlyMode</c> is <c>None</c>, and the YubiKey is
        /// currently NOT set to either PIN-only modes, this method will do
        /// nothing. It will not remove anything from ADMIN DATA or PRINTED, it
        /// will not try to authenticate the management key, nor will it try to
        /// verify the PIN. It will not change the management to the default.
        /// </para>
        /// <para>
        /// If a YubiKey is currently set to both modes, and you call this
        /// method with only one of the modes, this method will NOT "remove" the
        /// other mode.
        /// </para>
        /// <para>
        /// If a YubiKey is currently set to both PIN-only modes and you want to
        /// "remove" one of them, call this method and set the YubiKey to
        /// <c>None</c>, then call this method a second time and set the YubiKey
        /// to the desired mode.
        /// </para>
        /// <para>
        /// In order to set the YubiKey to be PIN-only, this method must
        /// authenticate the management key. Even if the management key is already
        /// authenticated, this method will authenticate. It will try to
        /// authenticate using the PIN-only techniques, and if that does not
        /// work, it will try to authenticate using the default managment key. If
        /// either of those techniques works, the user will not have to enter the
        /// management key. But if the YubiKey is not PIN-only, and the default
        /// management key does not authenticate, the method will call on the
        /// <c>KeyCollector</c> to obtain the correct value.
        /// </para>
        /// <para>
        /// If the mode to set is PIN-protected, then this method will use the
        /// existing mgmt key, unless it is the default. In that case, this
        /// method will generate a new, random mgmt key, set the YubiKey with
        /// this new value, and PIN-protect the new key.
        /// </para>
        /// <para>
        /// This method also requires the PIN to be verified. If setting to
        /// PIN-derived, even if the PIN is already verified, this method will
        /// call on the <c>KeyCollector</c> to obtain the PIN. If setting to
        /// PIN-protected, this method will verify the PIN only if it has not yet
        /// been verified.
        /// </para>
        /// <para>
        /// Note that this method will throw an exception if the current contents
        /// of ADMIN DATA and/or PRINTED are not compatible with PIN-only mode.
        /// If there are no current contents, then this method will set one or
        /// both of those storage locations with appropriate data. But if there
        /// is already something in one or both, and it is not PIN-only
        /// information, then this method will not replace that data, it will
        /// throw an exception.
        /// </para>
        /// </remarks>
        /// <param name="pinOnlyMode">
        /// The mode to which the YubiKey is to be set.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, one of the keys provided was
        /// not a valid Triple-DES key, the data stored on the YubiKey is
        /// incompatible with PIN-only, or the YubiKey had some other error, such
        /// as unreliable connection.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user canceled management key or PIN collection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Mutual authentication was performed and the YubiKey was not
        /// authenticated, or the remaining retries count indicates the PIN is
        /// blocked.
        /// </exception>
        public void SetPinOnlyMode(PivPinOnlyMode pinOnlyMode)
        {
            _log.LogInformation("Set a YubiKey to PIV PIN-only mode: {0}.", pinOnlyMode.ToString());

            if (pinOnlyMode.HasFlag(PivPinOnlyMode.PinProtectedUnavailable)
                || pinOnlyMode.HasFlag(PivPinOnlyMode.PinDerivedUnavailable))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPivPinOnlyMode));
            }

            Func<KeyEntryData, bool>? UserKeyCollector = KeyCollector;
            using var specialKeyCollector = new SpecialKeyCollector();

            try
            {
                PivPinOnlyMode currentMode = GetPrintedPinProtectedStatus(specialKeyCollector, UserKeyCollector);

                // At this point, if the requested mode is PinProtected, and the
                // currentMode is PinProtected, we're done.
                // Note that we cannot simply compare currentMode and
                // pinOnlyMode. E.g. pinOnlyMode could be None and currentMode
                // could be None, but in that case we still need to check
                // PinDerived.
                if ((pinOnlyMode == PivPinOnlyMode.PinProtected) && (currentMode == PivPinOnlyMode.PinProtected))
                {
                    return;
                }

                using var adminData = new AdminData();
                PivPinOnlyMode derivedMode = GetPinDerivedStatus(
                    adminData, currentMode.HasFlag(PivPinOnlyMode.PinProtected), specialKeyCollector, UserKeyCollector);

                // At this point, if the requested mode is PinDerived, and the
                // derivedMode is indeed PinDerived, we're done.
                if ((pinOnlyMode == PivPinOnlyMode.PinDerived) && (derivedMode == PivPinOnlyMode.PinDerived))
                {
                    return;
                }

                currentMode |= derivedMode;

                // Determine which key was used to authenticate the mgmt key. Set
                // the authMode variable to that value. This is a new variable
                // that cannot contain Unavailable.
                // If PinProtected is set, set authMode to that.
                // If PinProtected is not set, check PinDerived.
                // If neither are set, the authMode is None.
                PivPinOnlyMode authMode = PivPinOnlyMode.None;
                if (currentMode.HasFlag(PivPinOnlyMode.PinProtected))
                {
                    authMode = PivPinOnlyMode.PinProtected;
                }
                else if (currentMode.HasFlag(PivPinOnlyMode.PinDerived))
                {
                    authMode = PivPinOnlyMode.PinDerived;
                }

                // If the requested is already set, then we're done. We don't
                // want to simply fall through because if we do so, while we
                // won't call the SetDerived or SetProtected, we will write to
                // AdminData again.
                //
                // If the caller requested Protected and current is either
                // Protected or both, we handled that case.
                //
                // If the caller requested Derived and current is either Derived
                // or both, we handled that case.
                //
                // 1. If the caller requested both, and the current is both, or
                // 2. If the caller requested None, and the current is None,
                // we're done.
                // If the caller requested None, then currently Unavailable is
                // the same as None.
                if ((pinOnlyMode == currentMode) || (pinOnlyMode == authMode))
                {
                    return;
                }

                // If the current mode indicates that the YubiKey is unavailable for
                // the requested PIN-only mode, then throw an exception.
                if ((pinOnlyMode.HasFlag(PivPinOnlyMode.PinProtected)
                    && currentMode.HasFlag(PivPinOnlyMode.PinProtectedUnavailable))
                    || (pinOnlyMode.HasFlag(PivPinOnlyMode.PinDerived)
                    && currentMode.HasFlag(PivPinOnlyMode.PinDerivedUnavailable)))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.PinOnlyNotPossible));
                }

                // If the mgmt key has not yet been authenticated (by
                // GetPinOnlyMode), then get it using the KeyCollector.
                if (authMode == PivPinOnlyMode.None)
                {
                    // This will either return with the mgmt key authenticated,
                    // or it will throw an exception.
                    specialKeyCollector.AuthMgmtKeyAndSave(this, UserKeyCollector);
                }

                // At this point, the mgmt key is in the currentKey buffer.
                // If it was PinProtected, authMode will be PinProtected and
                // that is the current key.
                // If it was PinDerived but not PinProtected, authMode will be
                // PinDerived and that is the current key.
                // If it was authenticated not using PIN-only, then the authMode
                // is None.

                // If the caller wanted to set the YubiKey to no longer be
                // PIN-only, set PRINTED (if needed) and ADMIN DATA to empty.
                // Then set the management key to the default.
                // Note that it is possible that the ADMIN DATA contains a value
                // for PinLastUpdated. But we are using that field only if the
                // YubiKey is PIN-only, so it is safe to lose it.
                if (pinOnlyMode == PivPinOnlyMode.None)
                {
                    ClearPinOnly(currentMode, specialKeyCollector);
                    return;
                }

                // There are two times when we need to set the YubiKey to
                // PIN-protected.
                //   1. The caller requests it, pinOnlyMode is set with
                //      PinProtected, and the current mode is not PinProtected.
                //   2. The YubiKey was already set with PinProtected, but not
                //      PinDerived, and we now set to PinDerived. In that case,
                //      the mgmt key was changed to the derived value and we now
                //      need to set PRINTED with this new key.
                // Create this new variable, set it to true if either of the
                // conditions are met.
                bool setPinProtected =
                    pinOnlyMode.HasFlag(PivPinOnlyMode.PinProtected)
                    && !currentMode.HasFlag(PivPinOnlyMode.PinProtected);

                if (pinOnlyMode.HasFlag(PivPinOnlyMode.PinDerived)
                    && !currentMode.HasFlag(PivPinOnlyMode.PinDerived))
                {
                    SetYubiKeyPinDerived(adminData, authMode, specialKeyCollector, UserKeyCollector, ref setPinProtected);
                }

                if (setPinProtected)
                {
                    SetYubiKeyPinProtected(adminData, specialKeyCollector);
                }

                // Update the ADMIN DATA in case we changed anything.
                // If the currentMode is UnavailablePinDerived, then don't update
                // AdminData. It was something other than defined value, so we're
                // leaving it.
                if (!currentMode.HasFlag(PivPinOnlyMode.PinDerivedUnavailable))
                {
                    WriteObject(adminData);
                }
            }
            finally
            {
                KeyCollector = UserKeyCollector;
            }
        }

        // Clear any data indicating PIN-only, and set the management key to
        // default.
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

            specialKeyCollector.SetKeyData(SpecialKeyCollector.SetKeyDataDefault, ReadOnlyMemory<byte>.Empty, true);
            specialKeyCollector.ChangeManagementKey(this);
        }

        private void PutEmptyData(int dataTag)
        {
            byte[] emptyObject = new byte[] { 0x53, 0x00 };

            var putCmd = new PutDataCommand(dataTag, emptyObject);
            PutDataResponse putRsp = Connection.SendCommand(putCmd);
            if (putRsp.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(putRsp.StatusMessage);
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
            PivPinOnlyMode authMode,
            SpecialKeyCollector specialKeyCollector,
            Func<KeyEntryData, bool>? UserKeyCollector,
            ref bool setPinProtected)
        {
            // We need the actual PIN in order to derive the mgmt key, so even if
            // the PIN has already been verified, collect it.
            // This method will do nothing if the PIN has already been collected.
            specialKeyCollector.VerifyPinAndSave(this, UserKeyCollector);

            // If the authMode says the YubiKey is already set to
            // PinProtected, we're going to "delete" that management key and set
            // it to the derived value. Then we're going to set the protected key
            // to the derived as well.
            if (authMode == PivPinOnlyMode.PinProtected)
            {
                ClearPinOnly(authMode, specialKeyCollector);
                setPinProtected = true;
            }

            ReadOnlyMemory<byte> salt = specialKeyCollector.DeriveKeyData(ReadOnlyMemory<byte>.Empty, true);

            // Call this method instead of the PivSession.Change method directly,
            // becuase this method will update the current key with the new key.
            specialKeyCollector.ChangeManagementKey(this);
            _ = BlockPinOrPuk(PivSlot.Puk);
            adminData.SetSalt(salt);
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
            SpecialKeyCollector specialKeyCollector)
        {
            if (specialKeyCollector.IsKeyDataDefault(false))
            {
                specialKeyCollector.SetKeyData(SpecialKeyCollector.SetKeyDataRandom, ReadOnlyMemory<byte>.Empty, true);
                specialKeyCollector.ChangeManagementKey(this);
            }

            _ = BlockPinOrPuk(PivSlot.Puk);
            adminData.PukBlocked = true;
            adminData.PinProtected = true;

            using var pinProtect = new PinProtectedData();
            pinProtect.SetManagementKey(specialKeyCollector.GetCurrentMgmtKey());

            WriteObject(pinProtect);
        }

        private sealed class SpecialKeyCollector : IDisposable
        {
            public const int SetKeyDataBuffer = 1;
            public const int SetKeyDataRandom = 2;
            public const int SetKeyDataDefault = 4;
            private const int PinDerivedSaltLength = 16;
            private const int MgmtKeyLength = 24;
            private readonly Memory<byte> _defaultKey;
            private readonly Memory<byte> _currentKey;
            private readonly byte[] _currentKeyData = new byte[MgmtKeyLength];
            private readonly Memory<byte> _newKey;
            private readonly byte[] _newKeyData = new byte[MgmtKeyLength];

            private const int MaxPinLength = 8;
            private int _pinLength;
            private readonly Memory<byte> _pinMemory;
            private readonly byte[] _pinData = new byte[MaxPinLength];

            private bool _disposed;

            public bool PinCollected { get; private set; }

            public SpecialKeyCollector()
            {
                _defaultKey = new Memory<byte>(new byte[MgmtKeyLength] {
                    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
                });
                _currentKey = new Memory<byte>(_currentKeyData);
                _newKey = new Memory<byte>(_newKeyData);

                // Make sure the current key is init to the default.
                _defaultKey.CopyTo(_currentKey);

                PinCollected = false;
                _pinMemory = new Memory<byte>(_pinData);
                _pinLength = 0;
            }

            // Check to see if the data is the default mgmt key.
            // If isNewKey is true, check to see if the key data in _newKey is
            // default.
            // If isNewKey is false, check the _currentKey.
            public bool IsKeyDataDefault(bool isNewKey)
            {
                Memory<byte> cmpBuffer = isNewKey ? _newKey : _currentKey;
                return MemoryExtensions.SequenceEqual(_defaultKey.Span, cmpBuffer.Span);
            }

            // Check to see if the key is weak.
            // If bytes 0 - 7 are the same as 8 - 15, or 8 - 15 are the same as
            // 16 - 23, this is a waek key.
            // If isNewKey is true, check the key data in _newKey.
            // If isNewKey is false, check the key data in _currentKey.
            public bool IsKeyDataWeak(bool isNewKey)
            {
                Memory<byte> cmpBuffer = isNewKey ? _newKey : _currentKey;
                if (!MemoryExtensions.SequenceEqual(cmpBuffer.Slice(0, 8).Span, cmpBuffer.Slice(8, 8).Span))
                {
                    if (!MemoryExtensions.SequenceEqual(cmpBuffer.Slice(8, 8).Span, cmpBuffer.Slice(16, 8).Span))
                    {
                        return false;
                    }
                }

                return true;
            }

            // Set either the current or new mgmt key.
            // Set to either the data in the buffer, new random data, or the
            // default.
            // If the setFlag is SetKeyDataBuffer (1), set the appropriate buffer
            // to be the 24 bytes of the input. This will copy the key data, not
            // a reference. This will not check the length, it is the
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
            public void SetKeyData(int setFlag, ReadOnlyMemory<byte> keyData, bool isNewKey)
            {
                Memory<byte> dest = isNewKey ? _newKey : _currentKey;

                if (setFlag == SetKeyDataBuffer)
                {
                    keyData.CopyTo(dest);
                    return;
                }

                if (setFlag != SetKeyDataRandom)
                {
                    _defaultKey.CopyTo(dest);
                    return;
                }

                byte[] randomBytes = isNewKey ? _newKeyData : _currentKeyData;
                using RandomNumberGenerator randomObject = CryptographyProviders.RngCreator();

                do
                {
                    randomObject.GetBytes(randomBytes, 0, MgmtKeyLength);
                } while (IsKeyDataWeak(isNewKey));
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
            public ReadOnlyMemory<byte> DeriveKeyData(ReadOnlyMemory<byte> salt, bool isNewKey)
            {
                ReadOnlyMemory<byte> returnValue = salt;

                if (salt.Length != PinDerivedSaltLength)
                {
                    byte[] saltData = new byte[PinDerivedSaltLength];
                    returnValue = new ReadOnlyMemory<byte>(saltData);
                    using RandomNumberGenerator randomObject = CryptographyProviders.RngCreator();

                    do
                    {
                        randomObject.GetBytes(saltData, 0, PinDerivedSaltLength);
                        PerformKeyDerive(saltData, isNewKey);
                    } while (IsKeyDataWeak(isNewKey));
                }
                else
                {
                    PerformKeyDerive(salt.ToArray(), isNewKey);
                }

                return returnValue;
            }

            // Derive a key from the PIN and salt (iteration count = 10,000).
            // If isNewKey is true, place the result in _newKey
            // If isNewKey is false, place the result in _currentKey
            private void PerformKeyDerive(byte[] saltData, bool isNewKey)
            {
                byte[] dest = isNewKey ? _newKeyData : _currentKeyData;
                byte[] derivedKey = Array.Empty<byte>();
                byte[] pin = Array.Empty<byte>();
                try
                {
                    pin = new byte[_pinLength];
                    Array.Copy(_pinData, 0, pin, 0, _pinLength);

                    // This will use PBKDF2, with the PRF of HMAC with SHA-1.
#pragma warning disable CA5379, CA5387 // These warnings complain about SHA-1 and <100,000 iterations, but we use it to be backwards-compatible.
                    using var kdf = new Rfc2898DeriveBytes(pin, saltData, 10000);
                    derivedKey = kdf.GetBytes(MgmtKeyLength);
#pragma warning restore CA5379, CA5387
                    Array.Copy(derivedKey, dest, MgmtKeyLength);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(derivedKey);
                    CryptographicOperations.ZeroMemory(pin);
                }
            }

            // Change the management key from what is in current to what is in
            // new. Make sure after the change that the contents of current are
            // set to the contents of new.
            public void ChangeManagementKey(PivSession pivSession)
            {
                pivSession.ChangeManagementKey();
                SetKeyData(SpecialKeyCollector.SetKeyDataBuffer, _newKey, false);
            }

            // Obtain the mgmt key and authenticate it, make sure the mgmt key is set
            // in the SpecialKeyCollector.
            // This method assumes the special key collector is in a state just after
            // instantiation.
            // If the user cancels (the UserKeyCollector returns false), this method
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
                Func<KeyEntryData, bool>? UserKeyCollector)
            {
                // First, try the default key. If it works, we're done.
                // If we reach this point, the special key collector has just been
                // instantiated, so the default key is in the current key spot.
                pivSession.KeyCollector = KeyCollectorSpecial;
                if (pivSession.TryAuthenticateWithKeyCollector(true))
                {
                    return;
                }

                // If the default did not authenticate, use the caller-supplied
                // KeyCollector to obtain the key.
                if (UserKeyCollector is null)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.MissingKeyCollector));
                }

                var keyEntryData = new KeyEntryData()
                {
                    Request = KeyEntryRequest.AuthenticatePivManagementKey,
                };

                try
                {
                    while (UserKeyCollector(keyEntryData) == true)
                    {
                        SetKeyData(SetKeyDataBuffer, keyEntryData.GetCurrentValue(), false);
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
                    _ = UserKeyCollector(keyEntryData);
                }

                // If the user cancels, throw the canceled exception.
                throw new OperationCanceledException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncompleteCommandInput));
            }

            // Return a reference to the current management key.
            public ReadOnlyMemory<byte> GetCurrentMgmtKey() => _currentKey;

            // Return a reference to the new management key.
            public ReadOnlyMemory<byte> GetNewMgmtKey() => _newKey;

            // If the PIN is already collected, do nothing.
            // If not, obtain the PIN and verify it, set the PinCollected
            // property.
            // If the If the user cancels (the UserKeyCollector returns false),
            // this method will throw an exception.
            public void VerifyPinAndSave(
                PivSession pivSession,
                Func<KeyEntryData, bool>? UserKeyCollector)
            {
                if (PinCollected)
                {
                    return;
                }

                if (UserKeyCollector is null)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.MissingKeyCollector));
                }

                var keyEntryData = new KeyEntryData()
                {
                    Request = KeyEntryRequest.VerifyPivPin,
                };

                pivSession.KeyCollector = KeyCollectorSpecial;
                try
                {
                    while (UserKeyCollector(keyEntryData) == true)
                    {
                        SetPin(keyEntryData.GetCurrentValue());

                        if (pivSession.TryVerifyPin())
                        {
                            PinCollected = true;
                            return;
                        }

                        keyEntryData.IsRetry = true;
                    }
                }
                finally
                {
                    keyEntryData.Clear();

                    keyEntryData.Request = KeyEntryRequest.Release;
                    _ = UserKeyCollector(keyEntryData);
                }

                // If the user cancels, throw the canceled exception.
                throw new OperationCanceledException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncompleteCommandInput));
            }

            // Set the PIN data in this object to the input data.
            public void SetPin(ReadOnlyMemory<byte> pin)
            {
                pin.CopyTo(_pinMemory);
                _pinLength = pin.Length;
            }

            // This is the KeyCollector delegate.
            public bool KeyCollectorSpecial(KeyEntryData keyEntryData)
            {
                if (keyEntryData is null)
                {
                    return false;
                }

                if (keyEntryData.IsRetry == true)
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
                        keyEntryData.SubmitValue(_currentKey.Span);
                        return true;

                    case KeyEntryRequest.ChangePivManagementKey:
                        keyEntryData.SubmitValues(_currentKey.Span, _newKey.Span);
                        return true;

                    case KeyEntryRequest.VerifyPivPin:
                        keyEntryData.SubmitValue(_pinMemory.Slice(0, _pinLength).Span);
                        return true;
                }
            }

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

                CryptographicOperations.ZeroMemory(_currentKey.Span);
                CryptographicOperations.ZeroMemory(_newKey.Span);
                CryptographicOperations.ZeroMemory(_pinMemory.Span);
                _disposed = true;
            }
        }
    }
}
