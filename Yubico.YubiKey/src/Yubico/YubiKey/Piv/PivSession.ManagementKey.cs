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
    // This portion of the PivSession class contains code for management key
    // operations.
    public sealed partial class PivSession : IDisposable
    {
        /// <summary>
        /// This indicates whether the management key is authenticated or not.
        /// </summary>
        /// <remarks>
        /// Upon instantiation of this class, this property will be set to
        /// <c>false</c>. If the management key is authenticated (either single
        /// or mutual), it will be updated to <c>true</c>. To see the result of a
        /// management key authentication process, see the property
        /// <c>ManagementKeyAuthenticationResult</c>.
        /// </remarks>
        public bool ManagementKeyAuthenticated { get; private set; }

        /// <summary>
        /// This reports the result of the latest management key authentication
        /// attempt.
        /// </summary>
        /// <remarks>
        /// Upon instantiation of this class, this property will be set to
        /// <c>AuthenticateManagementKeyResult.Unauthenticated</c>. After
        /// authentication is attempted, this will be updated to the result.
        /// <para>
        /// The <c>ManagementKeyAuthenticated</c> property reports on whether the
        /// management key is authenticated or not, this reports on the result of
        /// an authentication.
        /// </para>
        /// <para>
        /// For example, if the management key is authenticated, this will be
        /// either <c>SingleAuthenticated</c> or <c>MutualFullyAuthenticated</c>.
        /// If it is "single" and you want "Mutual", you know to run the
        /// <c>TryAuthenticateManagementKey</c> method again, this time
        /// specifying mutual auth.
        /// </para>
        /// <para>
        /// Another example would be if <c>ManagementKeyAuthenticated</c> is
        /// <c>false</c>, you can check to see if this property is
        /// <c>Unauthenticated</c> (never attempted), or maybe it is
        /// <c>MutualYubiKeyAuthenticationFailed</c> which indicates you might be
        /// connected to a counterfeit YubiKey.
        /// </para>
        /// </remarks>
        public AuthenticateManagementKeyResult ManagementKeyAuthenticationResult { get; private set; }

        /// <summary>
        /// Try to authenticate the management key.
        /// </summary>
        /// <remarks>
        /// You need to authenticate the management key only once per session.
        /// But if you have already authenticated, and you call this method, it
        /// will perform the authentication again. If the authentication fails
        /// the second time, the previous auth will be nullified.
        /// <para>
        /// See the <see cref="ManagementKeyAuthenticated"/> property for the
        /// current state of management key authentication.
        /// </para>
        /// <para>
        /// This method will collect the management key using the
        /// <c>KeyCollector</c> delegate. If no such delegate has been set, this
        /// method will throw an exception.
        /// </para>
        /// <para>
        /// The <c>KeyCollector</c> has an option to cancel the operation. That
        /// is, this <c>TryAuthenticateManagementKey</c> method will call the
        /// <c>KeyCollector</c> requesting the management key, and it is possible
        /// that during the collection operations, the user cancels. The
        /// <c>KeyCollector</c> will return to this method noting the
        /// cancellation. In that case, this method will return <c>false</c>.
        /// </para>
        /// <para>
        /// Note that this is the only way to get a <c>false</c> return. Any
        /// other error and this method will throw an exception. In other words,
        /// a <c>false</c> return from this method means the user canceled.
        /// </para>
        /// <para>
        /// It is possible to perform single or mutual authentication. In single,
        /// only the "off-card" application authenticates itself to the YubiKey.
        /// In mutual authentication, both the off-card application and the
        /// YubiKey authenticate each other. If the <c>bool</c> argument
        /// <c>mutualAuthentication</c> is <c>true</c>, this method will perform
        /// mutual authentication. If <c>false</c>, it will perform single. The
        /// default is <c>true</c>, so if no argument is given, this method will
        /// perform mutual authentication.
        /// </para>
        /// <para>
        /// This method will also set the <c>ManagementKeyAuthenticated</c> and
        /// <c>ManagementKeyAuthenticationResult</c> properties. The "Result" is
        /// an <see cref="AuthenticateManagementKeyResult"/> enum, listing the
        /// possible results. For example, if you call for mutual authentication
        /// and it fails, the property will be set to
        /// <c>AuthenticateManagementKeyResult.MutualOffCardAuthenticationFailed</c>
        /// or <c>MutualYubiKeyAuthenticationFailed</c>, depending on what went
        /// wrong.
        /// </para>
        /// <para>
        /// If the management key authenticates, the method will return
        /// <c>true</c>. If not, and the <c>KeyCollector</c> cancels the process,
        /// the method will return <c>false</c> and set the
        /// <c>ManagementKeyAuthenticationResult</c> property to the failure
        /// reason.
        /// </para>
        /// <para>
        /// If the call is for mutual authentication, and the off-card
        /// application authenticates but the YubiKey does not, this method will
        /// set the <c>ManagementKeyAuthenticationResult</c> property to
        /// <c>MutualYubiKeyAuthenticationFailed</c> and throw an exception.
        /// This can happen when the there is an unreliable connection with the
        /// YubiKey, but it is also possible the device is a fraudulent YubiKey.
        /// In this case it is still possible to call on the connected device to
        /// perform operations, after all, the device trusts the off-card
        /// application. Generally, however, an application will not want to use
        /// the device if this happens. Nonetheless, an application could catch
        /// the exception and continue, either try again or use the device
        /// knowing it is untrusted.
        /// </para>
        /// <para>
        /// The method will call the <c>KeyCollector</c> delegate to obtain the
        /// management key. If the management key obtained authenticates, the
        /// method will call the <c>KeyCollector</c> again with the
        /// <c>Request</c> of <c>Release</c>. Upon the return from this release
        /// call to the <c>KeyCollector</c>, the method will return <c>true</c>
        /// and set the <c>ManagementKeyAuthenticated</c> property to <c>true</c>
        /// and the <c>ManagementKeyAuthenticationResult</c> property to
        /// <c>SingleAuthenticated</c> or <c>MutualFullyAuthenticated</c>. Note
        /// that this method ignores the return from the <c>KeyCollector</c> when
        /// the request is <c>Release</c>. That is, this method will return
        /// <c>true</c> or <c>false</c> depending on what happened before the
        /// <c>Release</c>. Note also that the <c>KeyCollector</c> MUST NEVER
        /// throw an exception when the request is <c>Release</c>.
        /// </para>
        /// <para>
        /// If the off-card application authentication fails, the method will
        /// call the <c>KeyCollector</c> delegate again, this time indicating the
        /// previous management key provided failed to authenticate
        /// (<c>KeyEntryRequest.IsRetry</c> will be set to <c>true</c>). The
        /// method will continue to call the <c>KeyCollector</c> and try to
        /// authenticate as long as the returned management key does not
        /// authenticate, and the return from the <c>KeyCollector</c> is
        /// <c>true</c>. If you want to cancel the authentication process after
        /// some number of failed attempts, build your <c>KeyCollector</c> to
        /// allow the user to cancel or keep track of the failures and return
        /// <c>false</c> after the limit has been reached.
        /// </para>
        /// <para>
        /// Note that there is no limit on the number of tries to authenticate
        /// the management key. That is, the management key will never be blocked.
        /// </para>
        /// <para>
        /// Note also that if the call is for mutual authentication and the
        /// YubiKey fails to authenticate, then the method will not call the
        /// <c>KeyCollector</c> again, it will set the
        /// <c>ManagementKeyAuthenticated</c> property to <c>false</c>, the
        /// <c>ManagementKeyAuthenticationResult</c> property to
        /// <c>MutualYubiKeyAuthenticationFailed</c>, and throw an exception.
        /// </para>
        /// <para>
        /// If there is an error during the process, this method will simply call
        /// the <c>KeyCollector</c> with <c>Release</c>, set the
        /// <c>ManagementKeyAuthenticated</c> property to <c>false</c>, the
        /// <c>ManagementKeyAuthenticationResult</c> property to
        /// <c>Unauthenticated</c>, and throw an exception.
        /// </para>
        /// </remarks>
        /// <param name="mutualAuthentication">
        /// If <c>true</c> the method will perform mutual authentication, if
        /// <c>false</c>, only the application will authenticate to the YubiKey.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the management key authenticates,
        /// <c>false</c> if the user cancels.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, the key provided was not a
        /// valid Triple-DES key, or the YubiKey had some other error, such as
        /// unreliable connection.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// The YubiKey returned malformed data and authentication, either single
        /// or double, could not be performed.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Mutual authentication was performed and the YubiKey was not
        /// authenticated.
        /// </exception>
        public bool TryAuthenticateManagementKey(bool mutualAuthentication = true)
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
                Request = KeyEntryRequest.AuthenticatePivManagementKey,
            };

            try
            {
                return TryAuthenticateManagementKey(mutualAuthentication, keyEntryData);
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = KeyCollector(keyEntryData);
            }
        }

        /// <summary>
        /// Authenticate the management key, throw an exception if the user cancels.
        /// </summary>
        /// <remarks>
        /// This is the same as <c>TryAuthenticateManagementKey</c>, except this
        /// method will throw an exception if the <c>KeyCollecter</c> indicates
        /// user cancellation.
        /// <para>
        /// See the <see cref="TryAuthenticateManagementKey(bool)"/> or
        /// <see cref="TryAuthenticateManagementKey(bool, KeyEntryData)"/> method for further
        /// documentation on this method.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, the key provided was not a
        /// valid Triple-DES key, or the YubiKey had some other error, such as
        /// unreliable connection.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// The YubiKey returned malformed data and authentication, either single
        /// or double, could not be performed.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user canceled management key collection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Mutual authentication was performed and the YubiKey was not
        /// authenticated.
        /// </exception>
        public void AuthenticateManagementKey(bool mutualAuthentication = true)
        {
            if (TryAuthenticateManagementKey(mutualAuthentication) == false)
            {
                throw new OperationCanceledException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncompleteCommandInput));
            }
        }

        /// <summary>
        /// Try to change the management key.
        /// </summary>
        /// <remarks>
        /// Upon manufacture of a YubiKey, the PIV application begins with a
        /// default management key (see the User's Manual entry on
        /// <xref href="UsersManualPinPukMgmtKey"> the management key</xref>).
        /// This method changes it. Note that this method can be run at any time,
        /// either during the initial YubiKey setup to change from the default
        /// management key, or later, to change it again.
        /// <para>
        /// The management key is a Triple-DES key, so it is 24 byte long, no
        /// more, no less. It is binary. That's 192 bits. But note that because
        /// of "parity" bits, the actual bit strength of a Triple-DES key is 124
        /// bits. And then further, there are attacks on Triple-DES that leave
        /// its effective bit strength at 112 bits.
        /// </para>
        /// <para>
        /// In order to change it, the current management key must be
        /// authenticated. If it has already been authenticated in this session,
        /// this method will still make the appropriate calls to authenticate (it
        /// will perform mutual authentication). That is, if you want to change
        /// the management key, it is not necessary to call
        /// <c>TryAuthenticateManagementKey</c> or
        /// <c>AuthenticateManagementKey</c> first. You can, but it doesn't
        /// matter, because this method will call it again.
        /// </para>
        /// <para>
        /// This method will collect the current and new management keys using
        /// the <c>KeyCollector</c> delegate. If no such delegate has been set,
        /// this method will throw an exception.
        /// </para>
        /// <para>
        /// The <c>KeyCollector</c> has an option to cancel the operation. That
        /// is, this <c>TryAuthenticateManagementKey</c> method will call the
        /// <c>KeyCollector</c> requesting the current management key, and it is
        /// possible that during the collection operations, the user cancels. The
        /// <c>KeyCollector</c> will return to this method noting the
        /// cancellation. In that case, this method will return <c>false</c>.
        /// </para>
        /// <para>
        /// Note that this is the only way to get a <c>false</c> return. Any
        /// other error and this method will throw an exception. In other words,
        /// a <c>false</c> return from this method means the user canceled.
        /// </para>
        /// <para>
        /// Along with the key data itself, a management key has a touch policy.
        /// See the User's Manual entry on the
        /// <xref href="UsersManualPivPinTouchPolicy"> PIV touch policy</xref>.
        /// </para>
        /// <para>
        /// This method takes in a touch policy argument, but the argument has a
        /// default value, so it is valid to pass no argument to this method. The
        /// default argument value is the <c>Default</c> touch policy.
        /// </para>
        /// <para>
        /// Note: touch policy for the management key is available only on
        /// YubiKey 4 and later. A YubiKey prior to 4 will ignore the touch
        /// policy and simply set the touch policy of the management key to the
        /// default.
        /// </para>
        /// <para>
        /// The touch policy refers to whether use of the management key will
        /// require touch or not, and if so, always or cached. The policy is
        /// specified using the <c>PivTouchPolicy</c> enum. If the input is
        /// <c>None</c> or <c>Never</c>, the YubiKey will not require touch to
        /// complete an operation that requires the management key. <c>Always</c>
        /// means every operation requires touch, even if the YubiKey had been
        /// touched for an operation shortly before. If <c>Cached</c>, one touch
        /// will last for 15 seconds. That is, touch for an operation, and if a
        /// second operation requires the management key, and it is executing
        /// less than 15 seconds after the first, touch is not required.
        /// <c>Default</c> will use the YubiKey's default touch policy.
        /// </para>
        /// <para>
        /// After this method is called, the management key will be authenticated
        /// for this session. That is, in order to change the key, the current
        /// management key must be authenticated. After changing, the new
        /// management key will be considered authenticated, and any subsequent
        /// operation that requires management key authentication in order to
        /// execute (e.g. generate a key pair) will work.
        /// </para>
        /// </remarks>
        /// <param name="touchPolicy">
        /// The touch policy for the new management key. If no argument is given,
        /// the policy will be <c>PivTouchPolicy.Default</c>.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the management key is changed, <c>false</c>
        /// if not.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, one of the keys provided was
        /// not a valid Triple-DES key, or the YubiKey had some other error, such
        /// as unreliable connection.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// The YubiKey returned malformed data and authentication, either single
        /// or double, could not be performed.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Mutual authentication was performed and the YubiKey was not
        /// authenticated.
        /// </exception>
        public bool TryChangeManagementKey(PivTouchPolicy touchPolicy = PivTouchPolicy.Default)
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
                Request = KeyEntryRequest.ChangePivManagementKey,
            };

            try
            {
                if (TryAuthenticateManagementKey(true, keyEntryData) == false)
                {
                    return false;
                }

                var setCommand = new SetManagementKeyCommand(keyEntryData.GetNewValue(), touchPolicy);
                SetManagementKeyResponse setResponse = Connection.SendCommand(setCommand);

                if (setResponse.Status == ResponseStatus.Success)
                {
                    return true;
                }

                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.CommandResponseApduUnexpectedResult,
                        setResponse.StatusWord.ToString("X4", CultureInfo.InvariantCulture)));
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = KeyCollector(keyEntryData);
            }
        }

        /// <summary>
        /// Change the management key, throw an exception if the user cancels.
        /// </summary>
        /// <remarks>
        /// This is the same as <c>TryChangeManagementKey</c>, except this method
        /// will throw an exception if the <c>KeyCollecter</c> indicates user
        /// cancellation.
        /// <para>
        /// See the <see cref="TryChangeManagementKey"/> method for further
        /// documentation on this method.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, the key provided was not a
        /// valid Triple-DES key, or the YubiKey had some other error, such as
        /// unreliable connection.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// The YubiKey returned malformed data and authentication, either single
        /// or double, could not be performed.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user canceled management key collection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Mutual authentication was performed and the YubiKey was not
        /// authenticated.
        /// </exception>
        public void ChangeManagementKey(PivTouchPolicy touchPolicy = PivTouchPolicy.Default)
        {
            if (TryChangeManagementKey(touchPolicy) == false)
            {
                throw new OperationCanceledException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncompleteCommandInput));
            }
        }

        // This is the actual Try code, shared by both TryAuth and TryChange.
        // The caller provides a KeyEntryData object set with the appropriate
        // request:
        //  KeyEntryRequest.AuthenticatePivManagementKey or
        //  KeyEntryRequest.ChangePivManagementKey.
        // This method will call the KeyCollector to collect the management key,
        // and if requested, a new key. It will then authenticate the current
        // management key using single or mutual auth based on the
        // mutualAuthentication arg (true to perform mutual, false to perform
        // single).
        // It will return false if the KeyCollector returns false (user canceled).
        // This method will set the appropriate properties of this class and
        // return the result, but it will not catch exceptions, clear the
        // keyEntryData, nor call the KeyCollector with Release.
        private bool TryAuthenticateManagementKey(bool mutualAuthentication, KeyEntryData keyEntryData)
        {
            if (KeyCollector is null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.MissingKeyCollector));
            }

            ManagementKeyAuthenticationResult = AuthenticateManagementKeyResult.Unauthenticated;
            ManagementKeyAuthenticated = false;

            while (KeyCollector(keyEntryData) == true)
            {
                var initCommand = new InitializeAuthenticateManagementKeyCommand(mutualAuthentication);
                InitializeAuthenticateManagementKeyResponse initResponse = Connection.SendCommand(initCommand);

                var completeCommand = new CompleteAuthenticateManagementKeyCommand(
                    initResponse, keyEntryData.GetCurrentValue().Span);
                CompleteAuthenticateManagementKeyResponse completeResponse = Connection.SendCommand(completeCommand);

                ManagementKeyAuthenticationResult = completeResponse.GetData();
                if (completeResponse.Status == ResponseStatus.Success)
                {
                    // If Success, there are three possibilities, (1) this is
                    // single auth and it succeeded, (2) this is mutual auth
                    // and it succeeded, or (3) this is mutual auth and the
                    // off-card app authenticated, but the YubiKey itself did
                    // not.
                    // If case (3), throw an exception.
                    if (ManagementKeyAuthenticationResult != AuthenticateManagementKeyResult.MutualYubiKeyAuthenticationFailed)
                    {
                        ManagementKeyAuthenticated = true;
                        return true;
                    }

                    throw new SecurityException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.YubiKeyNotAuthenticatedInPiv));
                }

                keyEntryData.IsRetry = true;
            }

            return ManagementKeyAuthenticated;
        }
    }
}
