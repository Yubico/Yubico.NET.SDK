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
using Yubico.Core.Logging;
using Yubico.YubiKey.Piv.Commands;

namespace Yubico.YubiKey.Piv
{
    // This portion of the PivSession class contains code for management key
    // operations.
    public sealed partial class PivSession : IDisposable
    {
        /// <summary>
        ///     This specifies the algorithm of the management key.
        /// </summary>
        public PivAlgorithm ManagementKeyAlgorithm { get; private set; }

        /// <summary>
        ///     This indicates whether the management key is authenticated or not.
        /// </summary>
        /// <remarks>
        ///     Upon instantiation of this class, this property will be set to
        ///     <c>false</c>. If the management key is authenticated (either single
        ///     or mutual), it will be updated to <c>true</c>. To see the result of a
        ///     management key authentication process, see the property
        ///     <c>ManagementKeyAuthenticationResult</c>.
        /// </remarks>
        public bool ManagementKeyAuthenticated { get; private set; }

        /// <summary>
        ///     This reports the result of the latest management key authentication
        ///     attempt.
        /// </summary>
        /// <remarks>
        ///     Upon instantiation of this class, this property will be set to
        ///     <c>AuthenticateManagementKeyResult.Unauthenticated</c>. After
        ///     authentication is attempted, this will be updated to the result.
        ///     <para>
        ///         The <c>ManagementKeyAuthenticated</c> property reports on whether the
        ///         management key is authenticated or not, this reports on the result of
        ///         an authentication.
        ///     </para>
        ///     <para>
        ///         For example, if the management key is authenticated, this will be
        ///         either <c>SingleAuthenticated</c> or <c>MutualFullyAuthenticated</c>.
        ///         If it is "single" and you want "Mutual", you know to run the
        ///         <c>TryAuthenticateManagementKey</c> method again, this time
        ///         specifying mutual auth.
        ///     </para>
        ///     <para>
        ///         Another example would be if <c>ManagementKeyAuthenticated</c> is
        ///         <c>false</c>, you can check to see if this property is
        ///         <c>Unauthenticated</c> (never attempted), or maybe it is
        ///         <c>MutualYubiKeyAuthenticationFailed</c> which indicates you might be
        ///         connected to a counterfeit YubiKey.
        ///     </para>
        /// </remarks>
        public AuthenticateManagementKeyResult ManagementKeyAuthenticationResult { get; private set; }

        /// <summary>
        ///     Try to authenticate the management key.
        /// </summary>
        /// <remarks>
        ///     You need to authenticate the management key only once per session.
        ///     But if you have already authenticated, and you call this method, it
        ///     will perform the authentication again. If the authentication fails
        ///     the second time, the previous auth will be nullified.
        ///     <para>
        ///         See the <see cref="ManagementKeyAuthenticated" /> property for the
        ///         current state of management key authentication.
        ///     </para>
        ///     <para>
        ///         This method will determine if the YubiKey is set for PIN-only. If so,
        ///         it will collect the management key using the PIN and authenticate. If
        ///         not, it will use the <c>KeyCollector</c> to obtain the management key.
        ///         The ADMIN DATA and PRINTED storage locations contain information
        ///         about PIN-only (PIN-protected, PIN-derived, or both). If that
        ///         information is inaccurate (some other application overwrote the data
        ///         in one or both locations, PIN-only authentication might fail and this
        ///         method will use the <c>KeyCollector</c>.
        ///     </para>
        ///     <para>
        ///         If the YubiKey is not set for PIN-only, this method will collect the
        ///         management key using the <c>KeyCollector</c> delegate. If no such
        ///         delegate has been set, this method will throw an exception.
        ///     </para>
        ///     <para>
        ///         Beginning with YubiKey 5.4.2, the management key can be AES as well
        ///         as Triple-DES. When the SDK calls the <c>KeyCollector</c> delegate,
        ///         it will not specify the expected algorithm or length of the
        ///         management key. If you need to know which algorithm the management
        ///         keys is, look at the property
        ///         <c>PivSession.ManagementKeyAlgorithm</c>. Maybe your Key Collector is
        ///         an object and you can let it know which algorithm the management key
        ///         is so when the management key is requested, the user will know how
        ///         many bytes to provide. For example,
        ///         <code language="csharp">
        ///    KeyCollectorClass.ManagementKeyAlgorithm = pivSession.ManagementKeyAlgorithm;
        ///    pivSession.KeyCollector = KeyCollectorClass.KeyCollectorDelegate;
        /// </code>
        ///     </para>
        ///     <para>
        ///         The <c>KeyCollector</c> has an option to cancel the operation. That
        ///         is, this <c>TryAuthenticateManagementKey</c> method will call the
        ///         <c>KeyCollector</c> requesting the management key, and it is possible
        ///         that during the collection operations, the user cancels. The
        ///         <c>KeyCollector</c> will return to this method noting the
        ///         cancellation. In that case, this method will return <c>false</c>.
        ///     </para>
        ///     <para>
        ///         Note that this is the only way to get a <c>false</c> return. Any
        ///         other error and this method will throw an exception. In other words,
        ///         a <c>false</c> return from this method means the user canceled.
        ///     </para>
        ///     <para>
        ///         It is possible to perform single or mutual authentication. In single,
        ///         only the "off-card" application authenticates itself to the YubiKey.
        ///         In mutual authentication, both the off-card application and the
        ///         YubiKey authenticate each other. If the <c>bool</c> argument
        ///         <c>mutualAuthentication</c> is <c>true</c>, this method will perform
        ///         mutual authentication. If <c>false</c>, it will perform single. The
        ///         default is <c>true</c>, so if no argument is given, this method will
        ///         perform mutual authentication.
        ///     </para>
        ///     <para>
        ///         This method will also set the <c>ManagementKeyAuthenticated</c> and
        ///         <c>ManagementKeyAuthenticationResult</c> properties. The "Result" is
        ///         an <see cref="AuthenticateManagementKeyResult" /> enum, listing the
        ///         possible results. For example, if you call for mutual authentication
        ///         and it fails, the property will be set to
        ///         <c>AuthenticateManagementKeyResult.MutualOffCardAuthenticationFailed</c>
        ///         or <c>MutualYubiKeyAuthenticationFailed</c>, depending on what went
        ///         wrong.
        ///     </para>
        ///     <para>
        ///         If the management key authenticates, the method will return
        ///         <c>true</c>. If not, and the <c>KeyCollector</c> cancels the process,
        ///         the method will return <c>false</c> and set the
        ///         <c>ManagementKeyAuthenticationResult</c> property to the failure
        ///         reason.
        ///     </para>
        ///     <para>
        ///         If the call is for mutual authentication, and the off-card
        ///         application authenticates but the YubiKey does not, this method will
        ///         set the <c>ManagementKeyAuthenticationResult</c> property to
        ///         <c>MutualYubiKeyAuthenticationFailed</c> and throw an exception.
        ///         This can happen when the there is an unreliable connection with the
        ///         YubiKey, but it is also possible the device is a fraudulent YubiKey.
        ///         In this case it is still possible to call on the connected device to
        ///         perform operations, after all, the device trusts the off-card
        ///         application. Generally, however, an application will not want to use
        ///         the device if this happens. Nonetheless, an application could catch
        ///         the exception and continue, either try again or use the device
        ///         knowing it is untrusted.
        ///     </para>
        ///     <para>
        ///         The method will call the <c>KeyCollector</c> delegate to obtain the
        ///         management key. If the management key obtained authenticates, the
        ///         method will call the <c>KeyCollector</c> again with the
        ///         <c>Request</c> of <c>Release</c>. Upon the return from this release
        ///         call to the <c>KeyCollector</c>, the method will return <c>true</c>
        ///         and set the <c>ManagementKeyAuthenticated</c> property to <c>true</c>
        ///         and the <c>ManagementKeyAuthenticationResult</c> property to
        ///         <c>SingleAuthenticated</c> or <c>MutualFullyAuthenticated</c>. Note
        ///         that this method ignores the return from the <c>KeyCollector</c> when
        ///         the request is <c>Release</c>. That is, this method will return
        ///         <c>true</c> or <c>false</c> depending on what happened before the
        ///         <c>Release</c>. Note also that the <c>KeyCollector</c> MUST NEVER
        ///         throw an exception when the request is <c>Release</c>.
        ///     </para>
        ///     <para>
        ///         If the off-card application authentication fails, the method will
        ///         call the <c>KeyCollector</c> delegate again, this time indicating the
        ///         previous management key provided failed to authenticate
        ///         (<c>KeyEntryRequest.IsRetry</c> will be set to <c>true</c>). The
        ///         method will continue to call the <c>KeyCollector</c> and try to
        ///         authenticate as long as the returned management key does not
        ///         authenticate, and the return from the <c>KeyCollector</c> is
        ///         <c>true</c>. If you want to cancel the authentication process after
        ///         some number of failed attempts, build your <c>KeyCollector</c> to
        ///         allow the user to cancel or keep track of the failures and return
        ///         <c>false</c> after the limit has been reached.
        ///     </para>
        ///     <para>
        ///         Note that there is no limit on the number of tries to authenticate
        ///         the management key. That is, the management key will never be blocked.
        ///     </para>
        ///     <para>
        ///         Note also that if the call is for mutual authentication and the
        ///         YubiKey fails to authenticate, then the method will not call the
        ///         <c>KeyCollector</c> again, it will set the
        ///         <c>ManagementKeyAuthenticated</c> property to <c>false</c>, the
        ///         <c>ManagementKeyAuthenticationResult</c> property to
        ///         <c>MutualYubiKeyAuthenticationFailed</c>, and throw an exception.
        ///     </para>
        ///     <para>
        ///         If there is an error during the process, this method will simply call
        ///         the <c>KeyCollector</c> with <c>Release</c>, set the
        ///         <c>ManagementKeyAuthenticated</c> property to <c>false</c>, the
        ///         <c>ManagementKeyAuthenticationResult</c> property to
        ///         <c>Unauthenticated</c>, and throw an exception.
        ///     </para>
        /// </remarks>
        /// <param name="mutualAuthentication">
        ///     If <c>true</c> the method will perform mutual authentication, if
        ///     <c>false</c>, only the application will authenticate to the YubiKey.
        /// </param>
        /// <returns>
        ///     A boolean, <c>true</c> if the management key authenticates,
        ///     <c>false</c> if the user cancels.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     There is no <c>KeyCollector</c> loaded, the key provided was not a
        ///     valid key, or the YubiKey had some other error, such as unreliable
        ///     connection.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        ///     The YubiKey returned malformed data and authentication, either single
        ///     or double, could not be performed.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     Mutual authentication was performed and the YubiKey was not
        ///     authenticated.
        /// </exception>
        public bool TryAuthenticateManagementKey(bool mutualAuthentication = true)
        {
            _log.LogInformation(
                $"Try to authenticate the management key: {(mutualAuthentication ? "mutual" : "single")} auth.");

            PivPinOnlyMode currentMode = TryAuthenticatePinOnly(true);
            if (currentMode.HasFlag(PivPinOnlyMode.PinProtected) || currentMode.HasFlag(PivPinOnlyMode.PinDerived))
            {
                return true;
            }

            return TryAuthenticateWithKeyCollector(mutualAuthentication);
        }

        // This tries to authenticate the management key using the KeyCollector.
        // Call this if the YubiKey is not PIN-only.
        // Think of it as the actual implementation of
        // TryAuthenticateManagementKey. That method checks to see if the YubiKey
        // is PIN-only, and if so, performs that auth. If not, it calls this
        // method.
        private bool TryAuthenticateWithKeyCollector(bool mutualAuthentication)
        {
            var keyEntryData = new KeyEntryData
            {
                Request = KeyEntryRequest.AuthenticatePivManagementKey
            };

            try
            {
                return TryAuthenticateWithKeyCollector(mutualAuthentication, keyEntryData);
            }
            finally
            {
                keyEntryData.Clear();

                if (!(KeyCollector is null))
                {
                    keyEntryData.Request = KeyEntryRequest.Release;
                    _ = KeyCollector(keyEntryData);
                }
            }
        }

        /// <summary>
        ///     Authenticate the management key, throw an exception if the user cancels.
        /// </summary>
        /// <remarks>
        ///     This is the same as <c>TryAuthenticateManagementKey</c>, except this
        ///     method will throw an exception if the <c>KeyCollected</c> indicates
        ///     user cancellation.
        ///     <para>
        ///         See the <see cref="TryAuthenticateManagementKey(bool)" /> method for
        ///         further documentation on this method.
        ///     </para>
        /// </remarks>
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
        ///     Mutual authentication was performed and the YubiKey was not
        ///     authenticated.
        /// </exception>
        public void AuthenticateManagementKey(bool mutualAuthentication = true)
        {
            _log.LogInformation(
                $"Authenticate the management key: {(mutualAuthentication ? "mutual" : "single")} auth.");

            if (TryAuthenticateManagementKey(mutualAuthentication) == false)
            {
                throw new OperationCanceledException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncompleteCommandInput));
            }
        }

        /// <summary>
        ///     Try to authenticate the management key. This method will use the key
        ///     data provided, rather than the <c>KeyCollector</c>.
        /// </summary>
        /// <remarks>
        ///     Normally, an application will not call any
        ///     <c>AuthenticateManagementKey</c> method. Under the covers, the SDK
        ///     determines when the management key needs to be verified and calls the
        ///     <c>KeyCollector</c>. It is only at that point the application needs
        ///     to supply the key. The SDK will call the application-supplied
        ///     <c>KeyCollector</c>, indicating what it needs (PIN, PUK, management
        ///     key), and the <c>KeyCollector</c> does what it needs to obtain the
        ///     value requested. This system also contains a mechanism to report if
        ///     the previous value was incorrect. If the management key is never
        ///     needed, the key is never provided.
        ///     <para>
        ///         See the User's Manual entry on the
        ///         <xref href="UsersManualKeyCollector"> Key Collector </xref> for a
        ///         more detailed explanation of this process.
        ///     </para>
        ///     <para>
        ///         With this method, however, the caller provides the management key and
        ///         the <c>KeyCollector</c> is never contacted.
        ///     </para>
        ///     <para>
        ///         See the <see cref="TryAuthenticateManagementKey(bool)" /> method for
        ///         further documentation on this method.
        ///     </para>
        ///     <para>
        ///         Generally, it is necessary to authenticate a management key once per
        ///         session. Once the key is authenticated, any operation that required
        ///         the key in order to execute, called during that session, will work.
        ///         The exceptions include changing the management key, and setting a
        ///         YubiKey to PIN-only.
        ///     </para>
        ///     <para>
        ///         Some applications would like to avoid using a <c>KeyCollector</c>.
        ///         For such situations, this method is provided. As long as the
        ///         application does not perform an operation that requires the
        ///         management key even if it has been authenticated in the session, the
        ///         <c>KeyCollector</c> is not needed.
        ///     </para>
        ///     <para>
        ///         Note that if the management key is needed during the session even
        ///         after the key is authenticated using this method (see exceptions
        ///         above), and no <c>KeyCollector</c> is provided, the SDK will throw an
        ///         exception.
        ///     </para>
        ///     <para>
        ///         The management key is provided to this method as a
        ///         <c>ReadOnlyMemory&lt;byte&gt;</c>. It is possible to pass a
        ///         <c>byte[]</c>, because it will be automatically cast. The management
        ///         key is 24 bytes, no more, no less.
        ///     </para>
        ///     <para>
        ///         If the wrong key is provided, this method will return <c>false</c>.
        ///     </para>
        /// </remarks>
        /// <param name="managementKey">
        ///     The key to authenticate.
        /// </param>
        /// <param name="mutualAuthentication">
        ///     If <c>true</c> the method will perform mutual authentication, if
        ///     <c>false</c>, only the application will authenticate to the YubiKey.
        ///     The default is <c>true</c>.
        /// </param>
        /// <returns>
        ///     A boolean, <c>true</c> if the management key authenticates,
        ///     <c>false</c> if it does not.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The key provided was not a valid Triple-DES key, or the YubiKey had
        ///     some other error, such as unreliable connection.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        ///     The YubiKey returned malformed data and authentication, either single
        ///     or double, could not be performed.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     Mutual authentication was performed and the YubiKey was not
        ///     authenticated.
        /// </exception>
        public bool TryAuthenticateManagementKey(ReadOnlyMemory<byte> managementKey, bool mutualAuthentication = true)
        {
            ManagementKeyAuthenticated = false;

            return TryAuthenticateManagementKey(mutualAuthentication, managementKey.Span, ManagementKeyAlgorithm);
        }

        /// <summary>
        ///     Try to change the management key. This will assume the new key is to
        ///     be Triple-DES.
        /// </summary>
        /// <remarks>
        ///     Upon manufacture of a YubiKey, the PIV application begins with a
        ///     default management key (see the User's Manual entry on
        ///     <xref href="UsersManualPinPukMgmtKey"> the management key</xref>).
        ///     This method changes it. Note that this method can be run at any time,
        ///     either during the initial YubiKey setup to change from the default
        ///     management key, or later, to change it again.
        ///     <para>
        ///         If the YubiKey is set for PIN-only, this method will throw an
        ///         exception.
        ///     </para>
        ///     <para>
        ///         Beginning with YubiKey 5.4.2, the management key can be either
        ///         Triple-DES or AES. When changing the management key, the SDK can
        ///         obtain the metadata for the management key slot to determine the
        ///         algorithm of the current key. However, the caller must supply the
        ///         algorithm of the new key (if it is 24 bytes, is it Triple-DES or
        ///         AES-192?). There is a <c>Try</c> method for changing the management
        ///         key that has an input argument for the algorithm. This method does
        ///         not have such an arg. This one will set the new key to Triple-DES.
        ///     </para>
        ///     <para>
        ///         The Triple-DES management key is 24 byte long, no more, no less. It
        ///         is binary. That's 192 bits. But note that because of "parity" bits,
        ///         the actual bit strength of a Triple-DES key is 124 bits. And then
        ///         further, there are attacks on Triple-DES that leave its effective bit
        ///         strength at 112 bits.
        ///     </para>
        ///     <para>
        ///         In order to change it, the current management key must be
        ///         authenticated. If it has already been authenticated in this session,
        ///         this method will still make the appropriate calls to authenticate (it
        ///         will perform mutual authentication). That is, if you want to change
        ///         the management key, it is not necessary to call
        ///         <c>TryAuthenticateManagementKey</c> or
        ///         <c>AuthenticateManagementKey</c> first. You can, but it doesn't
        ///         matter, because this method will call it again.
        ///     </para>
        ///     <para>
        ///         This method will collect the current and new management keys using
        ///         the <c>KeyCollector</c> delegate. If no such delegate has been set,
        ///         this method will throw an exception.
        ///     </para>
        ///     <para>
        ///         The <c>KeyCollector</c> has an option to cancel the operation. That
        ///         is, this <c>TryAuthenticateManagementKey</c> method will call the
        ///         <c>KeyCollector</c> requesting the current management key, and it is
        ///         possible that during the collection operations, the user cancels. The
        ///         <c>KeyCollector</c> will return to this method noting the
        ///         cancellation. In that case, this method will return <c>false</c>.
        ///     </para>
        ///     <para>
        ///         Note that this is the only way to get a <c>false</c> return. Any
        ///         other error and this method will throw an exception. In other words,
        ///         a <c>false</c> return from this method means the user canceled.
        ///     </para>
        ///     <para>
        ///         Along with the key data itself, a management key has a touch policy.
        ///         See the User's Manual entry on the
        ///         <xref href="UsersManualPivPinTouchPolicy"> PIV touch policy</xref>.
        ///     </para>
        ///     <para>
        ///         This method takes in a touch policy argument, but the argument has a
        ///         default value, so it is valid to pass no argument to this method. The
        ///         default argument value is the <c>Default</c> touch policy.
        ///     </para>
        ///     <para>
        ///         Note: touch policy for the management key is available only on
        ///         YubiKey 4 and later. A YubiKey prior to 4 will ignore the touch
        ///         policy and simply set the touch policy of the management key to the
        ///         default.
        ///     </para>
        ///     <para>
        ///         The touch policy refers to whether use of the management key will
        ///         require touch or not, and if so, always or cached. The policy is
        ///         specified using the <c>PivTouchPolicy</c> enum. If the input is
        ///         <c>None</c> or <c>Never</c>, the YubiKey will not require touch to
        ///         complete an operation that requires the management key. <c>Always</c>
        ///         means every operation requires touch, even if the YubiKey had been
        ///         touched for an operation shortly before. If <c>Cached</c>, one touch
        ///         will last for 15 seconds. That is, touch for an operation, and if a
        ///         second operation requires the management key, and it is executing
        ///         less than 15 seconds after the first, touch is not required.
        ///         <c>Default</c> will use the YubiKey's default touch policy.
        ///     </para>
        ///     <para>
        ///         After this method is called, the management key will be authenticated
        ///         for this session. That is, in order to change the key, the current
        ///         management key must be authenticated. After changing, the new
        ///         management key will be considered authenticated, and any subsequent
        ///         operation that requires management key authentication in order to
        ///         execute (e.g. generate a key pair) will work.
        ///     </para>
        /// </remarks>
        /// <param name="touchPolicy">
        ///     The touch policy for the new management key. If no argument is given,
        ///     the policy will be <c>PivTouchPolicy.Default</c>.
        /// </param>
        /// <returns>
        ///     A boolean, <c>true</c> if the management key is changed, <c>false</c>
        ///     if not.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     There is no <c>KeyCollector</c> loaded, one of the keys provided was
        ///     not a valid Triple-DES key, or the YubiKey had some other error, such
        ///     as unreliable connection.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        ///     The YubiKey returned malformed data and authentication, either single
        ///     or double, could not be performed.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     Mutual authentication was performed and the YubiKey was not
        ///     authenticated.
        /// </exception>
        public bool TryChangeManagementKey(PivTouchPolicy touchPolicy = PivTouchPolicy.Default) =>
            TryChangeManagementKey(touchPolicy, PivAlgorithm.TripleDes);

        /// <summary>
        ///     Try to change the management key. The new key will be the specified
        ///     algorithm.
        /// </summary>
        /// <remarks>
        ///     Upon manufacture of a YubiKey, the PIV application begins with a
        ///     default management key (see the User's Manual entry on
        ///     <xref href="UsersManualPinPukMgmtKey"> the management key</xref>).
        ///     This method changes it. Note that this method can be run at any time,
        ///     either during the initial YubiKey setup to change from the default
        ///     management key, or later, to change it again.
        ///     <para>
        ///         If the YubiKey is set for PIN-only, this method will throw an
        ///         exception.
        ///     </para>
        ///     <para>
        ///         Beginning with YubiKey 5.4.2, the management key can be either
        ///         Triple-DES or AES. When changing the management key, the SDK can
        ///         obtain the metadata for the management key slot to determine the
        ///         algorithm of the current key. However, the caller must supply the
        ///         algorithm of the new key (if it is 24 bytes, is it Triple-DES or
        ///         AES-192?).
        ///     </para>
        ///     <para>
        ///         Note that a Triple-DES key is 24 byte long, no more, no less. It is
        ///         binary. That's 192 bits. But because of "parity" bits, the actual bit
        ///         strength of a Triple-DES key is 124 bits. Furthermore, there are
        ///         attacks on Triple-DES that leave its effective bit strength at 112
        ///         bits.
        ///     </para>
        ///     <para>
        ///         In order to change it, the current management key must be
        ///         authenticated. If it has already been authenticated in this session,
        ///         this method will still make the appropriate calls to authenticate (it
        ///         will perform mutual authentication). That is, if you want to change
        ///         the management key, it is not necessary to call
        ///         <c>TryAuthenticateManagementKey</c> or
        ///         <c>AuthenticateManagementKey</c> first. You can, but it doesn't
        ///         matter, because this method will call it again.
        ///     </para>
        ///     <para>
        ///         This method will collect the current and new management keys using
        ///         the <c>KeyCollector</c> delegate. If no such delegate has been set,
        ///         this method will throw an exception.
        ///     </para>
        ///     <para>
        ///         The <c>KeyCollector</c> has an option to cancel the operation. That
        ///         is, this <c>TryAuthenticateManagementKey</c> method will call the
        ///         <c>KeyCollector</c> requesting the current management key, and it is
        ///         possible that during the collection operations, the user cancels. The
        ///         <c>KeyCollector</c> will return to this method noting the
        ///         cancellation. In that case, this method will return <c>false</c>.
        ///     </para>
        ///     <para>
        ///         Note that this is the only way to get a <c>false</c> return. Any
        ///         other error and this method will throw an exception. In other words,
        ///         a <c>false</c> return from this method means the user canceled.
        ///     </para>
        ///     <para>
        ///         Along with the key data itself, a management key has a touch policy.
        ///         See the User's Manual entry on the
        ///         <xref href="UsersManualPivPinTouchPolicy"> PIV touch policy</xref>.
        ///         This method requires a touch policy argument, it does not have a
        ///         default.
        ///     </para>
        ///     <para>
        ///         Note: touch policy for the management key is available only on
        ///         YubiKey 4 and later. A YubiKey prior to 4 will ignore the touch
        ///         policy and simply set the touch policy of the management key to the
        ///         default.
        ///     </para>
        ///     <para>
        ///         The touch policy refers to whether use of the management key will
        ///         require touch or not, and if so, always or cached. The policy is
        ///         specified using the <c>PivTouchPolicy</c> enum. If the input is
        ///         <c>None</c> or <c>Never</c>, the YubiKey will not require touch to
        ///         complete an operation that requires the management key. <c>Always</c>
        ///         means every operation requires touch, even if the YubiKey had been
        ///         touched for an operation shortly before. If <c>Cached</c>, one touch
        ///         will last for 15 seconds. That is, touch for an operation, and if a
        ///         second operation requires the management key, and it is executing
        ///         less than 15 seconds after the first, touch is not required.
        ///         <c>Default</c> will use the YubiKey's default touch policy.
        ///     </para>
        ///     <para>
        ///         After this method is called, the management key will be authenticated
        ///         for this session. That is, in order to change the key, the current
        ///         management key must be authenticated. After changing, the new
        ///         management key will be considered authenticated, and any subsequent
        ///         operation that requires management key authentication in order to
        ///         execute (e.g. generate a key pair) will work.
        ///     </para>
        /// </remarks>
        /// <param name="touchPolicy">
        ///     The touch policy for the new management key.
        /// </param>
        /// <param name="newKeyAlgorithm">
        ///     The new management key's algorithm.
        /// </param>
        /// <returns>
        ///     A boolean, <c>true</c> if the management key is changed, <c>false</c>
        ///     if not.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     There is no <c>KeyCollector</c> loaded, one of the keys provided was
        ///     not a valid key for the specified algorithm, or the YubiKey had some
        ///     other error, such as unreliable connection.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        ///     The YubiKey returned malformed data and authentication, either single
        ///     or double, could not be performed.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     Mutual authentication was performed and the YubiKey was not
        ///     authenticated.
        /// </exception>
        public bool TryChangeManagementKey(PivTouchPolicy touchPolicy, PivAlgorithm newKeyAlgorithm)
        {
            _log.LogInformation(
                "Try to change the management key, touch policy = {0}, algorithm = {1}.",
                touchPolicy.ToString(), newKeyAlgorithm.ToString());

            CheckManagementKeyAlgorithm(newKeyAlgorithm, checkMode: true);

            var keyEntryData = new KeyEntryData
            {
                Request = KeyEntryRequest.ChangePivManagementKey
            };

            try
            {
                if (TryAuthenticateWithKeyCollector(mutualAuthentication: true, keyEntryData) == false)
                {
                    return false;
                }

                var setCommand = new SetManagementKeyCommand(keyEntryData.GetNewValue(), touchPolicy, newKeyAlgorithm);
                SetManagementKeyResponse setResponse = Connection.SendCommand(setCommand);

                if (setResponse.Status == ResponseStatus.Success)
                {
                    ManagementKeyAlgorithm = newKeyAlgorithm;

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

                if (!(KeyCollector is null))
                {
                    _ = KeyCollector(keyEntryData);
                }
            }
        }

        /// <summary>
        ///     Change the management key, throw an exception if the user cancels.
        ///     The new key will be Triple-DES.
        /// </summary>
        /// <remarks>
        ///     This is the same as <c>TryChangeManagementKey(PivTouchPolicy)</c>,
        ///     except this method will throw an exception if the <c>KeyCollector</c>
        ///     indicates user cancellation.
        ///     <para>
        ///         See the <see cref="TryChangeManagementKey(PivTouchPolicy)" /> method for
        ///         further documentation on this method.
        ///     </para>
        /// </remarks>
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
        ///     Mutual authentication was performed and the YubiKey was not
        ///     authenticated.
        /// </exception>
        public void ChangeManagementKey(PivTouchPolicy touchPolicy = PivTouchPolicy.Default) =>
            ChangeManagementKey(touchPolicy, PivAlgorithm.TripleDes);

        /// <summary>
        ///     Change the management key, throw an exception if the user cancels.
        ///     The new key will be of the specified algorithm.
        /// </summary>
        /// <remarks>
        ///     This is the same as
        ///     <c>TryChangeManagementKey(PivTouchPolicy,PivAlgorithm)</c>,
        ///     except this method will throw an exception if the <c>KeyCollecter</c>
        ///     indicates user cancellation.
        ///     <para>
        ///         See the
        ///         <see
        ///             cref="TryChangeManagementKey(PivTouchPolicy,PivAlgorithm)" />
        ///         method for
        ///         further documentation on this method.
        ///     </para>
        /// </remarks>
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
        ///     Mutual authentication was performed and the YubiKey was not
        ///     authenticated.
        /// </exception>
        public void ChangeManagementKey(PivTouchPolicy touchPolicy, PivAlgorithm newKeyAlgorithm)
        {
            _log.LogInformation(
                "Change the management key, touch policy = {0}, algorithm = {1}.",
                touchPolicy.ToString(), newKeyAlgorithm.ToString());

            if (TryChangeManagementKey(touchPolicy, newKeyAlgorithm) == false)
            {
                throw new OperationCanceledException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncompleteCommandInput));
            }
        }

        /// <summary>
        ///     Try to change the management key. This method will use the
        ///     <c>currentKey</c> and <c>newKey</c> provided. The new key's algorithm
        ///     will be Triple-DES.
        /// </summary>
        /// <remarks>
        ///     Normally, an application would call the
        ///     <c>TryChangeManagementKey(PivTouchPolicy)</c> method and the SDK
        ///     would call on the loaded <c>KeyCollector</c> to retrieve the current
        ///     and new keys. With this method, the caller provides the keys and the
        ///     <c>KeyCollector</c> is never contacted.
        ///     <para>
        ///         Some applications would like to avoid using a <c>KeyCollector</c>.
        ///         For such situations, this method is provided.
        ///     </para>
        ///     <para>
        ///         See the <see cref="TryChangeManagementKey(PivTouchPolicy)" /> method
        ///         for further documentation
        ///         on this method.
        ///     </para>
        ///     <para>
        ///         If the wrong current key is provided, this method will return
        ///         <c>false</c>.
        ///     </para>
        /// </remarks>
        /// <param name="currentKey">
        ///     The current management key.
        /// </param>
        /// <param name="newKey">
        ///     What the management key will be changed to.
        /// </param>
        /// <param name="touchPolicy">
        ///     The touch policy for the new management key. If no argument is given,
        ///     the policy will be <c>PivTouchPolicy.Default</c>.
        /// </param>
        /// <returns>
        ///     A boolean, <c>true</c> if the management key is changed, <c>false</c>
        ///     if not.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     One of the keys provided was not a valid Triple-DES key, or the
        ///     YubiKey had some other error, such as unreliable connection.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        ///     The YubiKey returned malformed data and authentication, either single
        ///     or double, could not be performed.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     Mutual authentication was performed and the YubiKey was not
        ///     authenticated.
        /// </exception>
        public bool TryChangeManagementKey(
            ReadOnlyMemory<byte> currentKey,
            ReadOnlyMemory<byte> newKey,
            PivTouchPolicy touchPolicy = PivTouchPolicy.Default) =>
            TryChangeManagementKey(currentKey, newKey, touchPolicy, PivAlgorithm.TripleDes);

        /// <summary>
        ///     Try to change the management key. This method will use the
        ///     <c>currentKey</c> and <c>newKey</c> provided. The new key's algorithm
        ///     will be as specified.
        /// </summary>
        /// <remarks>
        ///     Normally, an application would call the
        ///     <c>TryChangeManagementKey(PivTouchPolicy)</c> method and the SDK
        ///     would call on the loaded <c>KeyCollector</c> to retrieve the current
        ///     and new keys. With this method, the caller provides the keys and the
        ///     <c>KeyCollector</c> is never contacted.
        ///     <para>
        ///         Some applications would like to avoid using a <c>KeyCollector</c>.
        ///         For such situations, this method is provided.
        ///     </para>
        ///     <para>
        ///         See the
        ///         <see
        ///             cref="TryChangeManagementKey(PivTouchPolicy,PivAlgorithm)" />
        ///         method
        ///         for further documentation on this method.
        ///     </para>
        ///     <para>
        ///         Note that with this method, a touch policy argument is required,
        ///         there is no default value.
        ///     </para>
        ///     <para>
        ///         If the wrong current key is provided, this method will return
        ///         <c>false</c>.
        ///     </para>
        /// </remarks>
        /// <param name="currentKey">
        ///     The current management key.
        /// </param>
        /// <param name="newKey">
        ///     What the management key will be changed to.
        /// </param>
        /// <param name="touchPolicy">
        ///     The touch policy for the new management key.
        /// </param>
        /// <param name="newKeyAlgorithm">
        ///     The algorithm the new management key will be.
        /// </param>
        /// <returns>
        ///     A boolean, <c>true</c> if the management key is changed, <c>false</c>
        ///     if not.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     One of the keys provided was not a valid Triple-DES key, or the
        ///     YubiKey had some other error, such as unreliable connection.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        ///     The YubiKey returned malformed data and authentication, either single
        ///     or double, could not be performed.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     Mutual authentication was performed and the YubiKey was not
        ///     authenticated.
        /// </exception>
        public bool TryChangeManagementKey(
            ReadOnlyMemory<byte> currentKey,
            ReadOnlyMemory<byte> newKey,
            PivTouchPolicy touchPolicy,
            PivAlgorithm newKeyAlgorithm)
        {
            CheckManagementKeyAlgorithm(newKeyAlgorithm, checkMode: true);

            return TryForcedChangeManagementKey(currentKey, newKey, touchPolicy, newKeyAlgorithm);
        }

        // Try to change the management key, even if the YubiKey is set to
        // PIN-derived.
        private bool TryForcedChangeManagementKey(
            ReadOnlyMemory<byte> currentKey,
            ReadOnlyMemory<byte> newKey,
            PivTouchPolicy touchPolicy,
            PivAlgorithm newKeyAlgorithm)
        {
            if (TryAuthenticateManagementKey(currentKey, mutualAuthentication: true))
            {
                var setCommand = new SetManagementKeyCommand(newKey, touchPolicy, newKeyAlgorithm);
                SetManagementKeyResponse setResponse = Connection.SendCommand(setCommand);

                if (setResponse.Status == ResponseStatus.Success)
                {
                    ManagementKeyAlgorithm = newKeyAlgorithm;

                    return true;
                }
            }

            return false;
        }

        // Verify that and that the given algorithm is allowed.
        // If checkMode is true, also check that the PIN-only mode is None.
        // This is called by methods that set PIN-only mode or change the mgmt
        // key.
        // The algorithm can only be 3DES or AES, and it can only be AES if the
        // YubiKey is 5.4.2 or later.
        // It is not allowed to change the mgmt key if it is PIN-only, so those
        // methods that change, will check the mode as well (they will pass true
        // as the checkMode arg).
        // If setting PIN-only, then the mode is not an issue, so don't check
        // (pass false as the checkMode arg).
        // If everything is fine, return, otherwise throw an exception.
        private void CheckManagementKeyAlgorithm(PivAlgorithm algorithm, bool checkMode)
        {
            if (checkMode)
            {
                PivPinOnlyMode mode = GetPinOnlyMode();

                if (mode.HasFlag(PivPinOnlyMode.PinProtected) || mode.HasFlag(PivPinOnlyMode.PinDerived))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.MgmtKeyCannotBeChanged));
                }
            }

            bool isValid = false;

            switch (algorithm)
            {
                case PivAlgorithm.TripleDes:
                    isValid = true;

                    break;

                case PivAlgorithm.Aes128:
                case PivAlgorithm.Aes192:
                case PivAlgorithm.Aes256:
                    isValid = _yubiKeyDevice.HasFeature(YubiKeyFeature.PivAesManagementKey);

                    break;
            }

            if (!isValid)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.UnsupportedAlgorithm));
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
        private bool TryAuthenticateWithKeyCollector(bool mutualAuthentication, KeyEntryData keyEntryData)
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

            while (KeyCollector(keyEntryData))
            {
                ManagementKeyAuthenticated = TryAuthenticateManagementKey(
                    mutualAuthentication, keyEntryData.GetCurrentValue().Span, ManagementKeyAlgorithm);

                if (ManagementKeyAuthenticated)
                {
                    return true;
                }

                keyEntryData.IsRetry = true;
            }

            return false;
        }

        // Perform one auth attempt.
        // This method assumes ManagementKeyAuthenticationResult was init to
        // Unauthenticated, and ManagementKeyAuthenticated was init to false.
        // Set ManagementKeyAuthenticationResult and ManagementKeyAuthenticated
        // if the auth succeeds.
        // If auth works, return true, otherwise, return false.
        // Throw an exception if the YubiKey fails to auth.
        private bool TryAuthenticateManagementKey(
            bool mutualAuthentication,
            ReadOnlySpan<byte> mgmtKey,
            PivAlgorithm algorithm)
        {
            var initCommand = new InitializeAuthenticateManagementKeyCommand(mutualAuthentication, algorithm);
            InitializeAuthenticateManagementKeyResponse initResponse = Connection.SendCommand(initCommand);

            var completeCommand = new CompleteAuthenticateManagementKeyCommand(initResponse, mgmtKey);
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
                if (ManagementKeyAuthenticationResult ==
                    AuthenticateManagementKeyResult.MutualYubiKeyAuthenticationFailed)
                {
                    throw new SecurityException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.YubiKeyNotAuthenticatedInPiv));
                }

                ManagementKeyAuthenticated = true;
            }

            return ManagementKeyAuthenticated;
        }
    }
}
