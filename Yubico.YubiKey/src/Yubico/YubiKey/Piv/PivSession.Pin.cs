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
using System.Security;
using System.Globalization;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Cryptography;

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
        /// remaining to the user and offer the option of cancelling. If the
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
                while (KeyCollector(keyEntryData) == true)
                {
                    var verifyCommand = new VerifyPinCommand(keyEntryData.GetCurrentValue());
                    VerifyPinResponse verifyResponse = Connection.SendCommand(verifyCommand);

                    if (verifyResponse.Status == ResponseStatus.Success)
                    {
                        PinVerified = true;
                        return true;
                    }

                    // If the response is AuthRequired, this will return the
                    // remaining retries count. Any other error return will throw
                    // an exception.
                    keyEntryData.RetriesRemaining = verifyResponse.GetData();

                    if ((keyEntryData.RetriesRemaining ?? 1) == 0)
                    {
                        throw new SecurityException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                ExceptionMessages.NoMoreRetriesRemaining));
                    }

                    keyEntryData.IsRetry = true;
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
        /// See the <see cref="TryVerifyPin"/> method for further documentation
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
            if (TryVerifyPin() == false)
            {
                throw new OperationCanceledException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncompleteCommandInput));
            }
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
        /// explicitly changed the PIN and PUK (<see cref="TryChangePin"/> and
        /// <see cref="TryChangePuk"/>), after changing the retry counts, the PIN
        /// and PUK will be the defaults.
        /// </para>
        /// <para>
        /// You will likely want to write your application to immediately follow
        /// changing the retry counts with setting the PIN and PUK:
        /// (<see cref="TryChangePin"/> and <see cref="TryChangePuk"/>. Another
        /// option is to change these counts during the initial user setup before
        /// changing the PIN and PUK from their defaults, then never offer the
        /// user the option of changing the retry counts again.
        /// </para>
        /// <para>
        /// In order to perform this operation, the management key must be
        /// authenticated and the PIN must be verified during this session. If
        /// the have not been authenticated/verified, this method will call
        /// <see cref="AuthenticateManagementKey"/> and <see cref="VerifyPin"/>.
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
        /// <see cref="TryAuthenticateManagementKey(bool, KeyEntryData)"/> and
        /// <see cref="TryVerifyPin"/> directly before calling this method.
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
            // This will validate the input.
            var setRetriesCommand = new SetPinRetriesCommand(newRetryCountPin, newRetryCountPuk);

            if (ManagementKeyAuthenticated == false)
            {
                AuthenticateManagementKey();
            }
            if (PinVerified == false)
            {
                VerifyPin();
            }

            SetPinRetriesResponse setRetriesResponse = Connection.SendCommand(setRetriesCommand);

            if (setRetriesResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(setRetriesResponse.StatusMessage);
            }
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
        /// remaining to the user and offer the option of cancelling. If the
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
        public bool TryChangePin() => TryChangeReference(KeyEntryRequest.ChangePivPin, ChangePinOrPuk);

        /// <summary>
        /// Change the PIN, throw an exception if the user cancels.
        /// </summary>
        /// <remarks>
        /// This is the same as <c>TryChangePin</c>, except this method will
        /// throw an exception if the <c>KeyCollecter</c> indicates user
        /// cancellation.
        /// <para>
        /// See the <see cref="TryChangePin"/> method for further documentation
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
            if (TryChangeReference(KeyEntryRequest.ChangePivPin, ChangePinOrPuk) == false)
            {
                throw new OperationCanceledException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncompleteCommandInput));
            }
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
        /// The PUK is six to eight bytes byte long. Although most PUKs will be
        /// characters, the YubiKey allows any binary data to be a PUK. The
        /// default is the ASCII string <c>"12345678"</c> which is the byte array
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
        /// remaining to the user and offer the option of cancelling. If the
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
        public bool TryChangePuk() => TryChangeReference(KeyEntryRequest.ChangePivPuk, ChangePinOrPuk);

        /// <summary>
        /// Change the PUK (PIN Unblocking Key), throw an exception if the user cancels.
        /// </summary>
        /// <remarks>
        /// This is the same as <c>TryChangePuk</c>, except this method will
        /// throw an exception if the <c>KeyCollecter</c> indicates user
        /// cancellation.
        /// <para>
        /// See the <see cref="TryChangePuk"/> method for further documentation
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
            if (TryChangeReference(KeyEntryRequest.ChangePivPuk, ChangePinOrPuk) == false)
            {
                throw new OperationCanceledException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncompleteCommandInput));
            }
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
        /// remaining to the user and offer the option of cancelling. If the
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
        public bool TryResetPin() => TryChangeReference(KeyEntryRequest.ResetPivPinWithPuk, ResetPin);

        /// <summary>
        /// Reset the PIN, using the PUK (PIN Unblocking Key), throw an
        /// exception if the user cancels.
        /// </summary>
        /// <remarks>
        /// This is the same as <c>TryResetPin</c>, except this method will
        /// throw an exception if the <c>KeyCollecter</c> indicates user
        /// cancellation.
        /// <para>
        /// See the <see cref="TryResetPin"/> method for further documentation
        /// on this method.
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
            if (TryChangeReference(KeyEntryRequest.ResetPivPinWithPuk, ResetPin) == false)
            {
                throw new OperationCanceledException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncompleteCommandInput));
            }
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
        private bool TryChangeReference(
            KeyEntryRequest request,
            Func<KeyEntryData, ResponseStatus> CommandResponse
            )
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
                while (KeyCollector(keyEntryData) == true)
                {
                    ResponseStatus status = CommandResponse(keyEntryData);

                    if (status == ResponseStatus.Success)
                    {
                        return true;
                    }

                    if ((keyEntryData.RetriesRemaining ?? 1) == 0)
                    {
                        throw new SecurityException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                ExceptionMessages.NoMoreRetriesRemaining));
                    }

                    keyEntryData.IsRetry = true;
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
            byte slotNumber = PivSlot.Puk;
            if (keyEntryData.Request == KeyEntryRequest.ChangePivPin)
            {
                slotNumber = PivSlot.Pin;
            }

            var changeCommand = new ChangeReferenceDataCommand(
                slotNumber, keyEntryData.GetCurrentValue(), keyEntryData.GetNewValue());
            ChangeReferenceDataResponse changeResponse = Connection.SendCommand(changeCommand);

            // If success, GetData returns null.
            // If wrong PIN/PUK, returns count.
            // If error, throwse exception.
            keyEntryData.RetriesRemaining = changeResponse.GetData();

            return changeResponse.Status;
        }

        // This is a delegate that implements the CommandResponse declaration of
        // TryChangeReference. It executes the ResetRetry command and response.
        private ResponseStatus ResetPin(KeyEntryData keyEntryData)
        {
            var resetCommand = new ResetRetryCommand(
                keyEntryData.GetCurrentValue(), keyEntryData.GetNewValue());
            ResetRetryResponse resetResponse = Connection.SendCommand(resetCommand);

            // If success, GetData returns null.
            // If wrong PUK, returns count.
            // If error, throwse exception.
            keyEntryData.RetriesRemaining = resetResponse.GetData();

            return resetResponse.Status;
        }
    }
}
