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
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Piv.Objects;

namespace Yubico.YubiKey.Piv
{
    // This portion of the PivSession class contains code for both PIN and PUK
    // operations.
    public sealed partial class PivSession : IDisposable
    {
        /// <summary>
        /// This indicates the current state of the PIN verification.
        /// </summary>
        /// <remarks>
        /// Upon instantiation of this class, this property will be set to
        /// <c>false</c>. If the PIN is authenticated (using
        /// <c>TryVerifyPin</c>), this will be updated to <c>true</c>
        /// </remarks>
        public bool PinVerified { get; private set; }

        /// <summary>
        /// Try to verify the PIN. If the user cancels, return <c>false</c>
        /// </summary>
        /// <remarks>
        /// You need to verify the PIN only once per session. But if you have
        /// already verified, and you call this method, it will perform the
        /// verification again. If the verification fails the second time, the
        /// previous verification will be nullified.
        /// <para>
        /// See the <see cref="PinVerified"/> property for the current state of PIN
        /// verification.
        /// </para>
        /// <para>
        /// This method will collect the PIN using the <c>KeyCollector</c>
        /// delegate. If no such delegate has been set, this method will throw an
        /// exception.
        /// </para>
        /// <para>
        /// The <c>KeyCollector</c> has an option to cancel the operation. That
        /// is, this <c>TryVerifyPin</c> method will call the <c>KeyCollector</c>
        /// requesting the PIN, and it is possible that during the collection
        /// operations, the user cancels. The <c>KeyCollector</c> will return to
        /// this method noting the cancellation. In that case, this method will
        /// return <c>false</c>.
        /// </para>
        /// <para>
        /// Note that this is the only way to get a <c>false</c> return. Any
        /// other error and this method will throw an exception. In other words,
        /// a <c>false</c> return from this method means the user canceled.
        /// </para>
        /// <para>
        /// This method will also set the <c>PinVerified</c> property of this
        /// class. That property is a boolean. If <c>true</c>, the PIN has been
        /// verified this session, otherwise it is <c>false</c>.
        /// </para>
        /// <para>
        /// If the PIN verifies, the method will return <c>true</c>. If not, and
        /// the <c>KeyCollector</c> cancels the process, the method will return
        /// <c>false</c> and set the <c>PinVerified</c> property to <c>false</c>.
        /// </para>
        /// <para>
        /// If the PIN does not verify, and the remaining retries count is not
        /// zero, the method will call the <c>KeyCollector</c> again with
        /// <c>KeyEntryData.IsRetry</c> set to <c>true</c> and
        /// <c>KeyEntryData.RetriesRemaining</c> set to the number of tries
        /// remaining until the PIN is blocked. The <c>KeyCollector</c> can try
        /// to collect the PIN again, but will likely report the retries
        /// remaining to the user and offer the option of canceling. If the
        /// <c>KeyCollector</c> returns <c>false</c>, this method will call the
        /// <c>KeyCollector</c> with <c>Release</c> and return <c>false</c>.
        /// </para>
        /// <para>
        /// If the PIN does not verify, and the remaining retries count is zero,
        /// the method will call the <c>KeyCollector</c> again, indicating
        /// <c>Release</c> and then throw an exception. That is, once the
        /// remaining retries count goes to zero the PIN is blocked. At this
        /// point, this method will not try to collect the PIN any more and will
        /// throw an exception.
        /// </para>
        /// <para>
        /// If there is an error during the process, this method will simply call
        /// the <c>KeyCollector</c> with <c>Release</c>, set the
        /// <c>PinVerified</c> property to <c>false</c>, and throw an exception.
        /// </para>
        /// <para>
        /// Note that when this method calls the <c>KeyCollector</c> with
        /// <c>Release</c>, the return from the <c>KeyCollector</c> is ignored,
        /// this method will return <c>true</c> or <c>false</c> depending on what
        /// happened before the <c>Release</c>.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A boolean, <c>true</c> if the PIN verifies, <c>false</c> if the
        /// <c>KeyCollector</c> cancels.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, or the YubiKey had some other
        /// error, such as unreliable connection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The remaining retries count indicates the PIN is blocked.
        /// </exception>
        public bool TryVerifyPin()
        {
            Logger.LogInformation("Try to verify the PIV PIN with KeyCollector.");

            if (KeyCollector is null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.MissingKeyCollector));
            }

            PinVerified = false;

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.VerifyPivPin,
            };

            try
            {
                while (KeyCollector(keyEntryData))
                {
                    if (TryVerifyPin(keyEntryData.GetCurrentValue(), out int? retriesRemaining))
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
                _ = KeyCollector(keyEntryData);
            }

            return PinVerified;
        }

        /// <summary>
        /// Verify the PIN, throw an exception if the user cancels.
        /// </summary>
        /// <remarks>
        /// This is the same as <c>TryVerifyPin</c>, except this method will
        /// throw an exception if the <c>KeyCollecter</c> indicates user
        /// cancellation.
        /// <para>
        /// See the <see cref="TryVerifyPin()"/> method for further documentation
        /// on this method.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, or the YubiKey had some other
        /// error, such as unreliable connection.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user canceled PIN collection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The remaining retries count indicates the PIN is blocked.
        /// </exception>
        public void VerifyPin()
        {
            Logger.LogInformation("Verify the PIV PIN.");

            if (TryVerifyPin() == false)
            {
                throw new OperationCanceledException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncompleteCommandInput));
            }
        }

        /// <summary>
        /// Try to verify the PIN. This method will use the PIN provided, rather
        /// than the <c>KeyCollector</c>.
        /// </summary>
        /// <remarks>
        /// Normally, an application will not call any <c>VerifyPin</c> method.
        /// Under the covers, the SDK determines when the PIN needs to be
        /// verified and calls the <c>KeyCollector</c>. It is only at that point
        /// the application needs to supply the PIN. The SDK will call the
        /// application-supplied <c>KeyCollector</c>, indicating what it needs
        /// (PIN, PUK, management key), and the <c>KeyCollector</c> does what it
        /// needs to obtain the value requested. This system also contains a
        /// mechanism to report if the previous value was incorrect and how many
        /// retries remain before the value is blocked. If the PIN is never needed,
        /// the PIN is never provided.
        /// <para>
        /// See the User's Manual entry on the
        /// <xref href="UsersManualKeyCollector"> Key Collector </xref> for a
        /// more detailed explanation of this process.
        /// </para>
        /// <para>
        /// With this method, the caller provides the PIN and the
        /// <c>KeyCollector</c> is never contacted.
        /// </para>
        /// <para>
        /// Generally, it is necessary to verify a PIN once per session. Once the
        /// PIN is verified, any operation that required the PIN in order to
        /// execute, called during that session, will work. The exceptions include
        /// performing a private key operation using a key that was generated or
        /// imported with the PIN policy of always, changing or resetting a PIN,
        /// and setting a YubiKey or session to PIN-derived (note that you should
        /// never set a YubiKey to PIN-derived, that feature is provided only for
        /// backwards compatibility).
        /// </para>
        /// <para>
        /// Some applications would like to avoid using a <c>KeyCollector</c>.
        /// For such situations, this method is provided. As long as the
        /// application does not perform an operation that requires the PIN even
        /// if it has been verified in the session, the <c>KeyCollector</c> is
        /// not needed.
        /// </para>
        /// <para>
        /// Note that if the PIN is needed during the session even after the PIN
        /// is verified using this method (see exceptions above), and no
        /// <c>KeyCollector</c> is provided, the SDK will throw an exception.
        /// </para>
        /// <para>
        /// The PIN is provided to this method as a
        /// <c>ReadOnlyMemory&lt;byte&gt;</c>. It is possible to pass a
        /// <c>byte[]</c>, because it will be automatically cast. Most PINs will
        /// be six to eight numbers, but the YubiKey allows any binary data. For
        /// example, if the PIN is "123456" (the default value), what is actually
        /// supplied to the YubiKey are the six bytes <c>0x31 32 33 34 35 36</c>,
        /// the ASCII representation of the numerals '1' through '6'. If the PIN
        /// is "ABCDefg", then the value to send to the YubiKey is the byte array
        /// <c>0x41 42 43 44 65 66 67</c>. But a PIN could also be
        /// <c>0xE7 05 3F 81 1C C9</c>. Those are not ASCII characters, but a
        /// YubiKey would accept them.
        /// </para>
        /// <para>
        /// If the wrong PIN is provided, this method will return <c>false</c>.
        /// </para>
        /// </remarks>
        /// <param name="pin">
        /// The PIN to verify.
        /// </param>
        /// <param name="retriesRemaining">
        /// An output, it will be set to the number of retries remaining if the
        /// PIN is not verified. If the PIN is verified, this will be set to null.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the PIN verifies, <c>false</c> if not.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey had some error, such as unreliable connection.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The wrong PIN was provided.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The remaining retries count indicates the PIN is blocked.
        /// </exception>
        public bool TryVerifyPin(ReadOnlyMemory<byte> pin, out int? retriesRemaining)
        {
            Logger.LogInformation("Try to verify the PIV PIN with supplied PIN.");

            retriesRemaining = null;
            PinVerified = false;

            var command = new VerifyPinCommand(pin);
            var response = Connection.SendCommand(command);

            PinVerified = response.Status == ResponseStatus.Success;
            if (PinVerified)
            {
                return true;
            }

            retriesRemaining = response.GetData() ?? 1;
            if (retriesRemaining == 0)
            {
                throw new SecurityException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.NoMoreRetriesRemaining));
            }

            return false;
        }

        /// <summary>
        /// Change the retry counts for the PIN and PUK.
        /// &gt; [!WARNING]
        /// &gt; This will reset the PIN and PUK to their default values as well as
        /// &gt; set the retry counts.
        /// </summary>
        /// <remarks>
        /// See the user's manual entry on
        /// <xref href="UsersManualPinPukMgmtKey#changing-the-retry-counts">
        /// changing the retry counts</xref>.
        /// <para>
        /// The retry count is the number of times a wrong PIN or PUK can be
        /// entered before the PIN or PUK is blocked. The YubiKey is manufactured
        /// with a retry count of three for both the PIN and PUK.
        /// </para>
        /// <para>
        /// Call this method to change the retry count of both the PIN and PUK.
        /// It is allowed to change the counts to different values. For example,
        /// it is acceptable to change the PIN retry count to 7 and the PUK retry
        /// count to 4.
        /// &gt; [!NOTE]
        /// &gt; You must change the retry counts of both the PIN and PUK. There is
        /// &gt; no way to change the retry count for only one secret.
        /// </para>
        /// <para>
        /// Supply the new retry counts in this method. The maximum retry count
        /// is 255, hence, the input arguments are bytes. The minimum retry count
        /// is 1. If one of the arguments is 0, this method will throw an
        /// exception. Note that a retry count of 1 means there are no retries.
        /// If the user enters the wrong PIN or PUK just once, the secret is
        /// blocked.
        /// </para>
        /// <para>
        /// After resetting the retry counts, the PIN and PUK will be reset to
        /// their default values (PIN: "123456", PUK: "12345678"). Even though
        /// you never reset the application (<see cref="ResetApplication"/>) or
        /// explicitly changed the PIN and PUK (<see cref="TryChangePin()"/> and
        /// <see cref="TryChangePuk()"/>), after changing the retry counts, the PIN
        /// and PUK will be the defaults.
        /// </para>
        /// <para>
        /// You will likely want to write your application to immediately follow
        /// changing the retry counts with setting the PIN and PUK:
        /// (<see cref="TryChangePin()"/> and <see cref="TryChangePuk()"/>. Another
        /// option is to change these counts during the initial user setup before
        /// changing the PIN and PUK from their defaults, then never offer the
        /// user the option of changing the retry counts again.
        /// </para>
        /// <para>
        /// In order to perform this operation, the management key must be
        /// authenticated and the PIN must be verified during this session. If
        /// the have not been authenticated/verified, this method will call
        /// <see cref="AuthenticateManagementKey"/> and <see cref="VerifyPin()"/>.
        /// That is, your application does not need to authenticate the
        /// management key and verify the PIN separately, this method will
        /// determine if they have been authenticated/verified or not, and if
        /// not, it will make the calls to perform authentication and
        /// verification.
        /// </para>
        /// <para>
        /// The authentication and verification methods will collect the
        /// management key and PIN using the <c>KeyCollector</c> delegate. If no
        /// such delegate has been set, this method will throw an exception.
        /// </para>
        /// <para>
        /// The <c>KeyCollector</c> has an option to cancel the operation. That
        /// is, the <c>Authenticate</c> and <c>Verify</c> methods will call the
        /// <c>KeyCollector</c> requesting the management key or PIN, and it is
        /// possible that during the collection operations, the user cancels. The
        /// <c>KeyCollector</c> will return to the authentication or verification
        /// method noting the cancellation. In that case, this method will throw
        /// an exception. If you want the authentication to return <c>false</c>
        /// on user cancellation, you must call
        /// <see cref="TryAuthenticateManagementKey(bool)"/> or
        /// <see cref="TryVerifyPin()"/> directly before calling this method.
        /// </para>
        /// </remarks>
        /// <param name="newRetryCountPin">
        /// The PIN's new retry count.
        /// </param>
        /// <param name="newRetryCountPuk">
        /// The PUK's new retry count.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The new retry count provided is invalid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, the key provided was not a
        /// valid Triple-DES key, or the YubiKey had some other error, such as
        /// unreliable connection.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user canceled management key collection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Mutual authentication was performed and the YubiKey was not
        /// authenticated.
        /// </exception>
        public void ChangePinAndPukRetryCounts(byte newRetryCountPin, byte newRetryCountPuk)
        {
            Logger.LogInformation("Change the PIV PIN and PUK retry counts: {PinCount}, {PukCount}.", newRetryCountPin,
                newRetryCountPuk);

            // This will validate the input.
            var setRetriesCommand = new SetPinRetriesCommand(newRetryCountPin, newRetryCountPuk);

            // Check to see if this is PIN-derived.
            // If it is not, this call will not verify the PIN, nor authenticate
            // the mgmt key, it will simply set mode to None and return true.
            // If PIN-derived, this will try to authenticate the PIN. By calling
            // with an Empty PIN, the method will use the KeyCollector.
            // If the PIN does not verify and the user cancels, this method will
            // return false.
            // If the PIN verifies, it will derive the mgmt key and authenticate.
            // If it does not authenticate, we will just say the mgmt key is not
            // PIN-derived, set mode to None and return true.
            // If it does return, then check the mode. If it is PIN-derived, the
            // PIN has been verified and the mgmt key has been authenticated.
            if (!TryGetChangePinMode(ReadOnlyMemory<byte>.Empty, out var pinOnlyMode, out _))
            {
                throw new OperationCanceledException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncompleteCommandInput));
            }

            if (!ManagementKeyAuthenticated)
            {
                AuthenticateManagementKey();
            }

            if (!PinVerified)
            {
                VerifyPin();
            }

            var response = Connection.SendCommand(setRetriesCommand);
            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }

            if (pinOnlyMode != PivPinOnlyMode.None)
            {
                // By passing Empty, this method will use the default PIN.
                SetPinOnlyMode(ReadOnlyMemory<byte>.Empty, pinOnlyMode, out _);
            }

            UpdateAdminData();
        }

        /// <summary>
        /// Try to change the retry counts for both the PIN and PUK. This method
        /// will use the <c>managementKey</c> and <c>pin</c> provided.
        /// &gt; [!WARNING]
        /// &gt; This will reset the PIN and PUK to their default values as well as
        /// &gt; set the retry counts.
        /// </summary>
        /// <remarks>
        /// Normally, an application would call the
        /// <c>ChangePinAndPukRetryCounts(int, int)</c> method and the SDK would
        /// call on the loaded <c>KeyCollector</c> to retrieve the management key
        /// and PIN. With this method, the caller provides the management key and
        /// PIN and the <c>KeyCollector</c> is never contacted.
        /// <para>
        /// Some applications would like to avoid using a <c>KeyCollector</c>.
        /// For such situations, this method is provided.
        /// </para>
        /// <para>
        /// See the <see cref="ChangePinAndPukRetryCounts(byte, byte)"/> method
        /// for further documentation on this method.
        /// </para>
        /// <para>
        /// If the wrong management key or PIN is provided, this method will
        /// return <c>false</c>.
        /// </para>
        /// <para>
        /// This method will authenticate the management key provided and verify
        /// the PIN provided, even if one or both have already been
        /// authenticated/verified.
        /// </para>
        /// <para>
        /// If the YubiKey is configured for PIN-only, the <c>managementKey</c>
        /// argument will be ignored. In this case, you can pass in an empty
        /// management key: <c>ReadOnlyMemory&lt;byte&gt;.Empty</c>.
        /// </para>
        /// </remarks>
        /// <param name="managementKey">
        /// The current management key. If it is <c>Empty</c>, the method will
        /// authenticate using PIN-only.
        /// </param>
        /// <param name="pin">
        /// The current PIN.
        /// </param>
        /// <param name="newRetryCountPin">
        /// The PIN's new retry count.
        /// </param>
        /// <param name="newRetryCountPuk">
        /// The PUK's new retry count.
        /// </param>
        /// <param name="retriesRemaining">
        /// An output, it will be set to the number of retries remaining if the
        /// PIN is not verified. If the management key is not authenticated or
        /// the PIN is verified, this will be set to null.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the retry counts are changed, <c>false</c>
        /// if not.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey had some error, such as unreliable connection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The remaining retries count indicates the PIN is blocked.
        /// </exception>
        public bool TryChangePinAndPukRetryCounts(ReadOnlyMemory<byte> managementKey,
                                                  ReadOnlyMemory<byte> pin,
                                                  byte newRetryCountPin,
                                                  byte newRetryCountPuk,
                                                  out int? retriesRemaining)
        {
            Logger.LogInformation(
                "Try to change the PIV PIN and PUK retry counts: {PinCount}, {PukCount} with supplied mgmt key and PIN.",
                newRetryCountPin, newRetryCountPuk);

            // This will validate the input.
            var setRetriesCommand = new SetPinRetriesCommand(newRetryCountPin, newRetryCountPuk);

            // Check to see if this is PIN-only. If it is, the PIN and mgmt key
            // will be verified/authenticated.
            // This method will return false if the PIN does not verify.
            // It will return true if the YubiKey is PIN-derived and the PIN
            // verifies. In that case, the mgmt key has been authenticated.
            // It will also return true if the mode is None (YubiKey is not
            // Pin-derived), in which case neither the PIN nor mgmt key is
            // verified/authenticated.
            if (TryGetChangePinMode(pin, out var mode, out retriesRemaining))
            {
                if (ManagementKeyAuthenticated || TryAuthenticateManagementKey(managementKey, true))
                {
                    if (PinVerified || TryVerifyPin(pin, out retriesRemaining))
                    {
                        var setRetriesResponse = Connection.SendCommand(setRetriesCommand);
                        if (setRetriesResponse.Status == ResponseStatus.Success)
                        {
                            if (mode != PivPinOnlyMode.None)
                            {
                                // By passing Empty, this method will use the default PIN.
                                SetPinOnlyMode(ReadOnlyMemory<byte>.Empty, mode, out _);
                            }

                            UpdateAdminData();

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Try to change the PIN.
        /// </summary>
        /// <remarks>
        /// Upon manufacture of a YubiKey, the PIV application begins with a
        /// default PIN (see the User's Manual entry on
        /// <xref href="UsersManualPinPukMgmtKey"> the PIN</xref>). This method
        /// changes it. Note that this method can be run at any time, either
        /// during the initial YubiKey setup to change from the default PIN, or
        /// later, to change it again.
        /// <para>
        /// The PIN is six to eight bytes byte long. Although most PINs will be
        /// characters, the YubiKey allows any binary data to be a PIN. The
        /// default is the ASCII string <c>"123456"</c> which is the byte array
        /// <code>
        ///   0x31 0x32 0x33 0x34 0x35 0x36
        /// </code>
        /// </para>
        /// <para>
        /// In order to change the PIN, both the current PIN and new PIN must be
        /// provided. Even if the current PIN had been verified already (using
        /// one of the methods <c>TryVerifyPin</c> or <c>VerifyPin</c>), the
        /// current PIN must be provided again. This method will call the
        /// <c>KeyCollector</c> delegate with the <c>Request</c> of
        /// <c>KeyEntryRequest.ChangePivPin</c>. The <c>KeyCollector</c> must
        /// then provide to this method both the current and new PIN.
        /// </para>
        /// <para>
        /// The <c>KeyCollector</c> has an option to cancel the operation. That
        /// is, this <c>TryChangePin</c> method will call the <c>KeyCollector</c>
        /// requesting the PIN, and it is possible that during the collection
        /// operations, the user cancels. The <c>KeyCollector</c> will return to
        /// this method noting the cancellation. In that case, this method will
        /// return <c>false</c>.
        /// </para>
        /// <para>
        /// Note that this is the only way to get a <c>false</c> return. Any
        /// other error and this method will throw an exception. In other words,
        /// a <c>false</c> return from this method means the user canceled.
        /// </para>
        /// <para>
        /// An incorrect current PIN will decrement the remaining tries count.
        /// Hence, trying to change the PIN with the wrong current PIN too many
        /// times (retry count times) will cause the PIN to be blocked.
        /// </para>
        /// <para>
        /// If the wrong current PIN is provided, and the remaining retries count
        /// is not zero, the method will call the <c>KeyCollector</c> again with
        /// <c>KeyEntryData.IsRetry</c> set to <c>true</c> and
        /// <c>KeyEntryData.RetriesRemaining</c> set to the number of tries
        /// remaining until the PIN is blocked. The <c>KeyCollector</c> can try
        /// to collect the PIN again, but will likely report the retries
        /// remaining to the user and offer the option of canceling. If the
        /// <c>KeyCollector</c> returns <c>false</c>, this method will call the
        /// <c>KeyCollector</c> with <c>Release</c> and return <c>false</c>.
        /// </para>
        /// <para>
        /// If the wrong current PIN is provided, and the remaining retries count
        /// is zero, the method will call the <c>KeyCollector</c> again,
        /// indicating <c>Release</c> and then throw an exception. That is, once
        /// the remaining retries count goes to zero the PIN is blocked. At this
        /// point, this method will not try to collect the PIN any more and will
        /// throw an exception.
        /// </para>
        /// <para>
        /// If there is an error during the process, this method will simply call
        /// the <c>KeyCollector</c> with <c>Release</c> and throw an exception.
        /// </para>
        /// <para>
        /// Note that when this method calls the <c>KeyCollector</c> with
        /// <c>Release</c>, the return from the <c>KeyCollector</c> is ignored,
        /// this method will return <c>true</c> or <c>false</c> depending on what
        /// happened before the <c>Release</c>.
        /// </para>
        /// <para>
        /// Changing the PIN does not affect the status of the Session's PIN
        /// verification. That is, the <c>PinVerified</c> property will not change
        /// after this method completes. If the status is <c>false</c>
        /// (unverified), and the PIN is successfully changed, the PIN will still
        /// be unverified and any operation that requires the PIN (such as
        /// signing), will need the PIN verified (with the new PIN). If the
        /// status is <c>true</c> (verified) and this method succeeds, the PIN will
        /// still be considered verified (the previous PIN), even though there is
        /// a new PIN. If the status is <c>true</c> (verified) and this method
        /// fails, the PIN will still be considered verified (the current PIN).
        /// </para>
        /// </remarks>
        /// <returns>
        /// A boolean, <c>true</c> if the PIN is changed, <c>false</c> if not.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, or the YubiKey had some other
        /// error, such as unreliable connection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The remaining retries count indicates the PIN is blocked.
        /// </exception>
        public bool TryChangePin()
        {
            Logger.LogInformation("Try to change the PIV PIN with KeyCollector.");

            if (TryGetChangePinMode(ReadOnlyMemory<byte>.Empty, out var mode, out _))
            {
                return TryChangeReference(KeyEntryRequest.ChangePivPin, ChangePinOrPuk, mode);
            }

            return false;
        }

        /// <summary>
        /// Change the PIN, throw an exception if the user cancels.
        /// </summary>
        /// <remarks>
        /// This is the same as <c>TryChangePin</c>, except this method will
        /// throw an exception if the <c>KeyCollector</c> indicates user
        /// cancellation.
        /// <para>
        /// See the <see cref="TryChangePin()"/> method for further documentation
        /// on this method.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, or the YubiKey had some other
        /// error, such as unreliable connection.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user canceled PIN collection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The remaining retries count indicates the PIN is blocked.
        /// </exception>
        public void ChangePin()
        {
            Logger.LogInformation("Change the PIV PIN.");

            if (!TryChangePin())
            {
                throw new OperationCanceledException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncompleteCommandInput));
            }
        }

        /// <summary>
        /// Try to change the PIN. This method will use the <c>currentPin</c> and
        /// <c>newPin</c> provided.
        /// </summary>
        /// <remarks>
        /// Normally, an application would call the <c>TryChangePin()</c> method
        /// (no arguments) and the SDK would call on the loaded
        /// <c>KeyCollector</c> to retrieve the PINs. With this method, the
        /// caller provides the PINs and the <c>KeyCollector</c> is never
        /// contacted.
        /// <para>
        /// Some applications would like to avoid using a <c>KeyCollector</c>.
        /// For such situations, this method is provided.
        /// </para>
        /// <para>
        /// See the <see cref="TryChangePin()"/> method for further documentation
        /// on this method.
        /// </para>
        /// <para>
        /// If the wrong current PIN is provided, this method will return
        /// <c>false</c>.
        /// </para>
        /// <para>
        /// Changing the PIN does not affect the status of the Session's PIN
        /// verification. That is, the <c>PinVerified</c> property will not change
        /// after this method completes. If the status is <c>false</c>
        /// (unverified), and the PIN is successfully changed, the PIN will still
        /// be unverified and any operation that requires the PIN (such as
        /// signing), will need the PIN verified (with the new PIN). If the
        /// status is <c>true</c> (verified) and this method succeeds, the PIN will
        /// still be considered verified (the previous PIN), even though there is
        /// a new PIN. If the status is <c>true</c> (verified) and this method
        /// fails, the PIN will still be considered verified (the current PIN).
        /// </para>
        /// <para>
        /// If the YubiKey is configured for PIN-derived, this method will update
        /// the management key, so that it is derived from the new PIN. Note that
        /// if the ADMIN DATA and/or PRINTED storage locations have been
        /// overwritten, this method might not be able to correctly update the
        /// management key. It is a good idea to call
        /// <see cref="TryRecoverPinOnlyMode()"/> before changing the PIN. See
        /// also the User's Manual entry on
        /// <xref href="UsersManualPivPinOnlyMode#pin-derived"> PIN-only</xref>
        /// modes.
        /// </para>
        /// </remarks>
        /// <param name="currentPin">
        /// The current PIN, the PIN that is to be changed.
        /// </param>
        /// <param name="newPin">
        /// The new PIN, what the PIN will be changed to.
        /// </param>
        /// <param name="retriesRemaining">
        /// An output, it will be set to the number of retries remaining if the
        /// current PIN is not verified. If the current PIN is verified and the
        /// PIN is changed, this will be set to null.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the PIN is changed, <c>false</c> if not.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey had some error, such as unreliable connection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The remaining retries count indicates the PIN is blocked.
        /// </exception>
        public bool TryChangePin(ReadOnlyMemory<byte> currentPin, ReadOnlyMemory<byte> newPin,
                                 out int? retriesRemaining)
        {
            Logger.LogInformation("Try to change the PIV PIN with supplied PINs.");

            if (TryGetChangePinMode(currentPin, out var mode, out retriesRemaining))
            {
                var command = new ChangeReferenceDataCommand(PivSlot.Pin, currentPin, newPin);
                var response = Connection.SendCommand(command);
                if (response.Status == ResponseStatus.Success)
                {
                    if (mode != PivPinOnlyMode.None)
                    {
                        SetPinOnlyMode(newPin, mode, out retriesRemaining);
                    }

                    UpdateAdminData();

                    return true;
                }

                if (response.Status == ResponseStatus.ConditionsNotSatisfied)
                {
                    throw new SecurityException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.PinComplexityViolation
                        )
                    );

                }

                retriesRemaining = response.GetData();
            }

            if ((retriesRemaining ?? 1) == 0)
            {
                throw new SecurityException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.NoMoreRetriesRemaining));
            }

            return false;
        }

        /// <summary>
        /// Try to change the PUK (PIN Unblocking Key).
        /// </summary>
        /// <remarks>
        /// Upon manufacture of a YubiKey, the PIV application begins with a
        /// default PUK (see the User's Manual entry on
        /// <xref href="UsersManualPinPukMgmtKey"> the PUK</xref>). This method
        /// changes it. Note that this method can be run at any time, either
        /// during the initial YubiKey setup to change from the default PUK, or
        /// later, to change it again.
        /// <para>
        /// The PUK must be 6-8 characters. For YubiKeys with firmware versions prior to 5.7, the PUK is allowed to be any character in the <c>0x00</c> - <c>0xFF</c> range for a total length of 6-8 bytes. For YubiKeys with firmware version 5.7 and above, the PUK is allowed to be any character in the <c>0x00</c> - <c>0x7F</c> range for a total length of 6-8 Unicode code points. The
        /// default PUK is the ASCII string <c>"12345678"</c>, which is the byte array
        /// <code>
        ///   0x31 0x32 0x33 0x34 0x35 0x36 0x37 0x38
        /// </code>
        /// </para>
        /// <para>
        /// In order to change the PUK, both the the current PUK and new PUK must
        /// be provided. This method will call the <c>KeyCollector</c> delegate
        /// with the <c>Request</c> of <c>KeyEntryRequest.ChangePivPuk</c>. The
        /// <c>KeyCollector</c> provides both the current and new PUK.
        /// </para>
        /// <para>
        /// The <c>KeyCollector</c> has an option to cancel the operation. That
        /// is, this <c>TryChangePuk</c> method will call the <c>KeyCollector</c>
        /// requesting the PUK, and it is possible that during the collection
        /// operations, the user cancels. The <c>KeyCollector</c> will return to
        /// this method noting the cancellation. In that case, this method will
        /// return <c>false</c>.
        /// </para>
        /// <para>
        /// Note that this is the only way to get a <c>false</c> return. Any
        /// other error and this method will throw an exception. In other words,
        /// a <c>false</c> return from this method means the user canceled.
        /// </para>
        /// <para>
        /// An incorrect current PUK will decrement the remaining tries count.
        /// Hence, trying to change the PUK with the wrong current PUK too many
        /// times (retry count times) will cause the PUK to be blocked.
        /// </para>
        /// <para>
        /// If the wrong current PUK is provided, and the remaining retries count
        /// is not zero, the method will call the <c>KeyCollector</c> again with
        /// <c>KeyEntryData.IsRetry</c> set to <c>true</c> and
        /// <c>KeyEntryData.RetriesRemaining</c> set to the number of tries
        /// remaining until the PUK is blocked. The <c>KeyCollector</c> can try
        /// to collect the PUK again, but will likely report the retries
        /// remaining to the user and offer the option of canceling. If the
        /// <c>KeyCollector</c> returns <c>false</c>, this method will call the
        /// <c>KeyCollector</c> with <c>Release</c> and return <c>false</c>.
        /// </para>
        /// <para>
        /// If the wrong current PUK is provided, and the remaining retries count
        /// is zero, the method will call the <c>KeyCollector</c> again,
        /// indicating <c>Release</c> and then throw an exception. That is, once
        /// the remaining retries count goes to zero the PUK is blocked. At this
        /// point, this method will not try to collect the PUK any more and will
        /// throw an exception.
        /// </para>
        /// <para>
        /// If there is an error during the process, this method will simply call
        /// the <c>KeyCollector</c> with <c>Release</c> and throw an exception.
        /// </para>
        /// <para>
        /// Note that when this method calls the <c>KeyCollector</c> with
        /// <c>Release</c>, the return from the <c>KeyCollector</c> is ignored,
        /// this method will return <c>true</c> or <c>false</c> depending on what
        /// happened before the <c>Release</c>.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A boolean, <c>true</c> if the PUK is changed, <c>false</c> if not.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, or the YubiKey had some other
        /// error, such as unreliable connection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The remaining retries count indicates the PUK is blocked.
        /// </exception>
        public bool TryChangePuk()
        {
            Logger.LogInformation("Try to change the PIV PUK with KeyCollector.");

            return TryChangeReference(KeyEntryRequest.ChangePivPuk, ChangePinOrPuk, PivPinOnlyMode.None);
        }

        /// <summary>
        /// Change the PUK (PIN Unblocking Key), throw an exception if the user cancels.
        /// </summary>
        /// <remarks>
        /// This is the same as <c>TryChangePuk</c>, except this method will
        /// throw an exception if the <c>KeyCollector</c> indicates user
        /// cancellation.
        /// <para>
        /// See the <see cref="TryChangePuk()"/> method for further documentation
        /// on this method.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, or the YubiKey had some other
        /// error, such as unreliable connection.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user canceled PUK collection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The remaining retries count indicates the PUK is blocked.
        /// </exception>
        public void ChangePuk()
        {
            Logger.LogInformation("Change the PIV PUK.");

            if (TryChangeReference(KeyEntryRequest.ChangePivPuk, ChangePinOrPuk, PivPinOnlyMode.None) == false)
            {
                throw new OperationCanceledException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncompleteCommandInput));
            }
        }

        /// <summary>
        /// Try to change the PUK. This method will use the <c>currentPuk</c> and
        /// <c>newPuk</c> provided.
        /// </summary>
        /// <remarks>
        /// Normally, an application would call the <c>TryChangePuk()</c> method
        /// (no arguments) and the SDK would call on the loaded
        /// <c>KeyCollector</c> to retrieve the PUKs. With this method, the
        /// caller provides the current and new PUKs and the <c>KeyCollector</c>
        /// is never contacted.
        /// <para>
        /// Some applications would like to avoid using a <c>KeyCollector</c>.
        /// For such situations, this method is provided.
        /// </para>
        /// <para>
        /// See the <see cref="TryChangePuk()"/> method for further documentation
        /// on this method.
        /// </para>
        /// <para>
        /// If the wrong current PUK is provided, this method will return
        /// <c>false</c>.
        /// </para>
        /// </remarks>
        /// <param name="currentPuk">
        /// The current PUK, the PUK that is to be changed.
        /// </param>
        /// <param name="newPuk">
        /// The new PUK, what the PUK will be changed to.
        /// </param>
        /// <param name="retriesRemaining">
        /// An output, it will be set to the number of retries remaining if the
        /// current PUK is not correct. If the PUK is changed, this will be set to null.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the PUK is changed, <c>false</c> if not.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey had some error, such as unreliable connection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The remaining retries count indicates the PUK is blocked.
        /// </exception>
        public bool TryChangePuk(ReadOnlyMemory<byte> currentPuk, ReadOnlyMemory<byte> newPuk,
                                 out int? retriesRemaining)
        {
            Logger.LogInformation("Try to change the PIV PUK with supplied PUKs.");

            var command = new ChangeReferenceDataCommand(PivSlot.Puk, currentPuk, newPuk);
            var response = Connection.SendCommand(command);
            if (response.Status == ResponseStatus.ConditionsNotSatisfied)
            {
                retriesRemaining = null;
                throw new SecurityException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.PinComplexityViolation
                    )
                );
            }

            retriesRemaining = response.GetData();

            return response.Status == ResponseStatus.Success;
        }

        /// <summary>
        /// Try to reset the PIN, using the PUK (PIN Unblocking Key).
        /// </summary>
        /// <remarks>
        /// If a user loses (or forgets) the PIN, it is possible to reset it
        /// using the PUK. Whether the PIN has been blocked (the wrong value was
        /// entered too many times in calls to <c>TryVerifyPin</c> or
        /// <c>TryChangePin</c>, and the retry count was exhausted) or not, it is
        /// possible to reset the PIN to a new value. That is, if the PIN is
        /// blocked, use this method to unblock it. But even if the PIN is not
        /// blocked, you can use this method to reset it.
        /// <para>
        /// This is essentially the same operation as <c>TryChangePin</c>, except
        /// instead of using the current PIN to provide permission to change the
        /// PIN, it uses the PUK.
        /// </para>
        /// <para>
        /// If the PUK is blocked, this method will not execute. Note that if a
        /// YubiKey is configured PIN-only, the PUK will be blocked.
        /// </para>
        /// <para>
        /// The PIN is six to eight bytes byte long. Although most PINs will be
        /// characters, the YubiKey allows any binary data to be a PIN.
        /// </para>
        /// <para>
        /// In order to change the PUK, both the the current PUK and a new PIN
        /// must be provided. This method will call the <c>KeyCollector</c>
        /// delegate with the <c>Request</c> of
        /// <c>KeyEntryRequest.ResetPivPinWithPuk</c>. The <c>KeyCollector</c>
        /// provides both the current PUK and a new PIN. Once it has both the PUK
        /// and a new PIN, this method will try to reset the PIN. That is, this
        /// method will not verify the current PUK in a separate step, the
        /// current PUK will only be verified for the reset with the new PIN.
        /// </para>
        /// <para>
        /// The <c>KeyCollector</c> has an option to cancel the operation. That
        /// is, this <c>TryResetPin</c> method will call the <c>KeyCollector</c>
        /// requesting the PUK and PIN, and it is possible that during the
        /// collection operations, the user cancels. The <c>KeyCollector</c> will
        /// return to this method noting the cancellation. In that case, this
        /// method will return <c>false</c>.
        /// </para>
        /// <para>
        /// Note that this is the only way to get a <c>false</c> return. Any
        /// other error and this method will throw an exception. In other words,
        /// a <c>false</c> return from this method means the user canceled.
        /// </para>
        /// <para>
        /// An incorrect current PUK will decrement the remaining tries count.
        /// Hence, trying to change the PIN with the wrong current PUK too many
        /// times (retry count times) will cause the PUK to be blocked.
        /// </para>
        /// <para>
        /// If the wrong current PUK is provided, and the remaining retries count
        /// is not zero, the method will call the <c>KeyCollector</c> again with
        /// <c>KeyEntryData.IsRetry</c> set to <c>true</c> and
        /// <c>KeyEntryData.RetriesRemaining</c> set to the number of tries
        /// remaining until the PUK is blocked. The <c>KeyCollector</c> can try
        /// to collect the PUK again, but will likely report the retries
        /// remaining to the user and offer the option of canceling. If the
        /// <c>KeyCollector</c> returns <c>false</c>, this method will call the
        /// <c>KeyCollector</c> with <c>Release</c> and return <c>false</c>.
        /// </para>
        /// <para>
        /// If the wrong current PUK is provided, and the remaining retries count
        /// is zero, the method will call the <c>KeyCollector</c> again,
        /// indicating <c>Release</c> and then throw an exception. That is, once
        /// the remaining retries count goes to zero the PUK is blocked. At this
        /// point, this method will not try to collect the PUK any more and will
        /// throw an exception.
        /// </para>
        /// <para>
        /// If there is an error during the process, this method will simply call
        /// the <c>KeyCollector</c> with <c>Release</c> and throw an exception.
        /// </para>
        /// <para>
        /// Note that when this method calls the <c>KeyCollector</c> with
        /// <c>Release</c>, the return from the <c>KeyCollector</c> is ignored,
        /// this method will return <c>true</c> or <c>false</c> depending on what
        /// happened before the <c>Release</c>.
        /// </para>
        /// <para>
        /// Resetting the PIN does not affect the status of the Session's PIN
        /// verification. That is, the <c>PinVerified</c> property will not change
        /// after this method completes. If the status is <c>false</c>
        /// (unverified), and the PIN is successfully reset, the PIN will still
        /// be unverified and any operation that requires the PIN (such as
        /// signing), will need the PIN verified (with the new PIN). If the
        /// status is <c>true</c> (verified) and this method succeeds, the PIN will
        /// still be considered verified (the previous PIN), even though there is
        /// a new PIN. If the status is <c>true</c> (verified) and this method
        /// fails, the PIN will still be considered verified (the current PIN).
        /// </para>
        /// </remarks>
        /// <returns>
        /// A boolean, <c>true</c> if the PIN is reset, <c>false</c> if not.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, or the YubiKey had some other
        /// error, such as unreliable connection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The remaining retries count indicates the PUK is blocked.
        /// </exception>
        public bool TryResetPin()
        {
            Logger.LogInformation("Try to reset the PIV PIN using the PIV PUK with KeyCollector.");

            if (TryGetChangePinMode(ReadOnlyMemory<byte>.Empty, out var pinOnlyMode, out _))
            {
                return TryChangeReference(KeyEntryRequest.ResetPivPinWithPuk, ResetPin, pinOnlyMode);
            }

            return false;
        }

        /// <summary>
        /// Reset the PIN, using the PUK (PIN Unblocking Key), throw an
        /// exception if the user cancels.
        /// </summary>
        /// <remarks>
        /// This is the same as <c>TryResetPin</c>, except this method will
        /// throw an exception if the <c>KeyCollector</c> indicates user
        /// cancellation.
        /// <para>
        /// See the <see cref="TryResetPin()"/> method for further documentation
        /// on this method.
        /// </para>
        /// <para>
        /// If the PUK is blocked, this method will not execute. Note that if a
        /// YubiKey is configured PIN-only, the PUK will be blocked.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, or the YubiKey had some other
        /// error, such as unreliable connection.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user canceled PUK and PIN collection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The remaining retries count indicates the PUK is blocked.
        /// </exception>
        public void ResetPin()
        {
            Logger.LogInformation("Reset the PIV PIN using the PIV PUK.");

            if (TryChangeReference(KeyEntryRequest.ResetPivPinWithPuk, ResetPin, PivPinOnlyMode.None) == false)
            {
                throw new OperationCanceledException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncompleteCommandInput));
            }
        }

        /// <summary>
        /// Try to reset the PIN using the PUK (PIN Unblocking Key). This method
        /// will use the <c>puk</c> and <c>pin</c> provided.
        /// </summary>
        /// <remarks>
        /// Normally, an application would call the <c>TryResetPin()</c> method
        /// (no arguments) and the SDK would call on the loaded
        /// <c>KeyCollector</c> to retrieve the PUK and new PIN. With this
        /// method, the caller provides the PUK and new PIN and the
        /// <c>KeyCollector</c> is never contacted.
        /// <para>
        /// Some applications would like to avoid using a <c>KeyCollector</c>.
        /// For such situations, this method is provided.
        /// </para>
        /// <para>
        /// See the <see cref="TryResetPin()"/> method for further documentation
        /// on this method.
        /// </para>
        /// <para>
        /// If the wrong PUK is provided, this method will return <c>false</c>.
        /// </para>
        /// <para>
        /// Changing the PIN does not affect the status of the Session's PIN
        /// verification. That is, the <c>PinVerified</c> property will not change
        /// after this method completes. If the status is <c>false</c>
        /// (unverified), and the PIN is successfully changed, the PIN will still
        /// be unverified and any operation that requires the PIN (such as
        /// signing), will need the PIN verified (with the new PIN). If the
        /// status is <c>true</c> (verified) and this method succeeds, the PIN will
        /// still be considered verified (the previous PIN), even though there is
        /// a new PIN. If the status is <c>true</c> (verified) and this method
        /// fails, the PIN will still be considered verified (the current PIN).
        /// </para>
        /// <para>
        /// If the PUK is blocked, this method will not execute. Note that if a
        /// YubiKey is configured PIN-only, the PUK will be blocked.
        /// </para>
        /// </remarks>
        /// <param name="puk">
        /// The PIN Unblocking Key.
        /// </param>
        /// <param name="newPin">
        /// The new PIN, what the PIN will be changed to.
        /// </param>
        /// <param name="retriesRemaining">
        /// An output, it will be set to the number of retries remaining if the
        /// PUK is not correct. If the PIN is reset, this will be set to null.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the PIN is changed, <c>false</c> if not.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey had some error, such as unreliable connection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The remaining retries count indicates the PIN is blocked.
        /// </exception>
        public bool TryResetPin(ReadOnlyMemory<byte> puk, ReadOnlyMemory<byte> newPin, out int? retriesRemaining)
        {
            Logger.LogInformation("Try to reset the PIV PIN using the PIV PUK with supplied PUK and PIN.");

            var command = new ResetRetryCommand(puk, newPin);
            var response = Connection.SendCommand(command);
            if (response.Status == ResponseStatus.ConditionsNotSatisfied)
            {
                retriesRemaining = null;
                throw new SecurityException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.PinComplexityViolation
                    )
                );
            }

            retriesRemaining = response.GetData();
            if ((retriesRemaining ?? 1) == 0)
            {
                throw new SecurityException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.NoMoreRetriesRemaining));
            }

            return response.Status == ResponseStatus.Success;
        }

        // Common code to change a PIN or PUK by either ChangeReferenceData or
        // ResetRetry.
        // The caller passes in a request, indicating what the operation will be.
        // It must be one of the following.
        //   ChangePivPin
        //   ChangePivPuk
        //   ResetPivPinWithPuk
        // The delegate is a callback will perform the appropriate
        // Command/Response operations (Change or Reset).
        // If the mode is not None, then set the YubiKey to that mode.
        private bool TryChangeReference(KeyEntryRequest request,
                                        Func<KeyEntryData, ResponseStatus> commandResponse,
                                        PivPinOnlyMode mode)
        {
            if (KeyCollector is null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.MissingKeyCollector));
            }

            var keyEntryData = new KeyEntryData()
            {
                Request = request,
            };

            try
            {
                while (KeyCollector(keyEntryData))
                {
                    var responseStatus = commandResponse(keyEntryData);
                    if (responseStatus == ResponseStatus.Success)
                    {
                        if (mode != PivPinOnlyMode.None)
                        {
                            SetPinOnlyMode(keyEntryData.GetNewValue(), mode, out _);
                        }

                        if (request == KeyEntryRequest.ChangePivPin)
                        {
                            UpdateAdminData();
                        }

                        return true;
                    }

                    if ((keyEntryData.RetriesRemaining ?? 1) == 0)
                    {
                        throw new SecurityException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                ExceptionMessages.NoMoreRetriesRemaining));
                    }

                    if (responseStatus == ResponseStatus.ConditionsNotSatisfied)
                    {
                        keyEntryData.IsViolatingPinComplexity = true;
                    }
                    else
                    {
                        keyEntryData.IsRetry = true;
                    }
                }
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = KeyCollector(keyEntryData);
            }

            return false;
        }

        // This is a delegate that implements the CommandResponse declaration of
        // TryChangeReference. It executes the ChangeReference command and response.
        private ResponseStatus ChangePinOrPuk(KeyEntryData keyEntryData)
        {
            byte slotNumber = keyEntryData.Request == KeyEntryRequest.ChangePivPin
                ? PivSlot.Pin
                : PivSlot.Puk;

            var command = new ChangeReferenceDataCommand(
                slotNumber, keyEntryData.GetCurrentValue(), keyEntryData.GetNewValue());

            var response = Connection.SendCommand(command);

            // If success, GetData returns null.
            // If wrong PIN/PUK, returns count.
            // If error, throws exception.
            keyEntryData.RetriesRemaining = response.GetData();

            var status = response.Status;
            switch (status)
            {
                case ResponseStatus.Success:
                    Logger.LogInformation(
                        slotNumber == PivSlot.Pin
                            ? "The PIV PIN has been changed"
                            : "The PIV PUK has been changed");

                    break;
                case ResponseStatus.ConditionsNotSatisfied:
                    Logger.LogWarning(
                        slotNumber == PivSlot.Pin
                            ? "The PIV PIN does not meet the complexity requirements"
                            : "The PIV PUK does not meet the complexity requirements");

                    break;
                default:
                    Logger.LogError(
                        slotNumber == PivSlot.Pin
                            ? $"The PIV PIN could not be changed. Reason: {response.StatusMessage} (0x{response.StatusWord:X4})" 
                            : $"The PIV PUK could not be changed. Reason: {response.StatusMessage} (0x{response.StatusWord:X4})");

                    break;
            }

            return status;
        }

        // This is a delegate that implements the CommandResponse declaration of
        // TryChangeReference. It executes the ResetRetry command and response.
        private ResponseStatus ResetPin(KeyEntryData keyEntryData)
        {
            var command = new ResetRetryCommand(
                keyEntryData.GetCurrentValue(), keyEntryData.GetNewValue());

            var response = Connection.SendCommand(command);

            // If success, GetData returns null.
            // If wrong PUK, returns count.
            // If error, throws exception.
            keyEntryData.RetriesRemaining = response.GetData();

            return response.Status;
        }

        // If the PIN has been changed, update the AdminData element
        // PinLastUpdated.
        // This method is called by those operations that change the PIN,
        // ChangePin and ChangePinAndPukRetryCounts.
        // This will get the AdminData, and if the YubKey is set for PinProtected
        // or PinDerived, it will set the PinLastUpdated field to the current
        // time and store the updated AdminData. To do so, it must authenticate
        // the management key. Hence, it will do this only if it can authenticate
        // with PIN-only.
        // If there is currently no AdminData in this YubiKey, this method will
        // do nothing. That is, it will not create a new AdminData with the
        // current time.
        // If the mgmt key is not authenticated, it will do nothing.
        private void UpdateAdminData()
        {
            if (!ManagementKeyAuthenticated)
            {
                Logger.LogDebug("Unauthenticated attempt to update AdminData failed.");
                return;
            }

            bool isValid = TryReadObject(out AdminData adminData);

            using (adminData)
            {
                if (!isValid || adminData.IsEmpty)
                {
                    return;
                }

                if (!adminData.PinProtected && adminData.Salt is null)
                {
                    return;
                }

                adminData.PinLastUpdated = DateTime.UtcNow;
                WriteObject(adminData);
            }
        }
    }
}
