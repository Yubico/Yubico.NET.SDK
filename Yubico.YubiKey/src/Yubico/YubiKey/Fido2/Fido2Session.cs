// Copyright 2025 Yubico AB
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
using System.Security.Cryptography;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using Yubico.Core.Logging;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Represents an active session with the FIDO2 application on the YubiKey.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When you need to perform FIDO2 operations, instantiate this class to create a session, then call on methods
    /// within the class.
    /// </para>
    /// <para>
    /// Generally, you will choose the YubiKey to use by building an instance of <see cref="IYubiKeyDevice" />. This
    /// object will represent the actual YubiKey hardware.
    /// <code language="csharp">
    ///   IYubiKeyDevice SelectYubiKey()
    ///   {
    ///       IEnumerable&lt;IYubiKeyDevice&gt; yubiKeyList = YubiKey.FindAll();
    ///       foreach (IYubiKeyDevice current in yubiKeyList)
    ///       {
    ///           /* Determine which YubiKey to use */
    ///
    ///           if (selected)
    ///           {
    ///               return current;
    ///           }
    ///       }
    ///   }
    /// </code>
    /// </para>
    /// <para>
    /// Once you have the YubiKey to use, you will build an instance of this Fido2Session class to represent the FIDO2
    /// application on the hardware. Because this class implements <c>IDisposable</c>, use the <c>using</c> keyword.
    /// For example,
    /// <code language="csharp">
    ///     IYubiKeyDevice yubiKeyToUse = SelectYubiKey();
    ///     using (var fido2 = new Fido2Session(yubiKeyToUse))
    ///     {
    ///         /* Perform FIDO2 operations. */
    ///     }
    /// </code>
    /// </para>
    /// <para>
    /// If this class is used as part of a <c>using</c> expression or statement, when the session goes out of scope, the
    /// <c>Dispose</c> method will be called to dispose of the active FIDO2 session. This will clear any application state,
    /// and ultimately release the connection to the YubiKey.
    /// </para>
    /// </remarks>
    public sealed partial class Fido2Session : ApplicationSession
    {
        private AuthenticatorInfo? _authenticatorInfo;

        /// <summary>
        /// A callback that this class will call when it needs the YubiKey
        /// touched or a PIN verified.
        /// </summary>
        /// <remarks>
        /// <para>
        /// With a FIDO2 Session, there are three situations where the SDK will call
        /// a <c>KeyCollector</c>: PIN, non-biometric touch, and biometric touch.
        /// Biometric touch is only available on YubiKey Bio Series keys.
        /// </para>
        /// <para>
        /// It is possible to perform PIN operations without using the <c>KeyCollector</c>. Look
        /// for the overloads of TryVerifyPin, TryChangePin, and TrySetPin that take in PIN
        /// parameters. With Touch, the <c>KeyCollector</c> will call your application
        /// when the YubiKey is waiting for proof of user presence. This is so that your
        /// application can alert the user that touch is required. There is nothing the
        /// <c>KeyCollector</c> needs to return to the SDK.
        /// </para>
        /// <para>
        /// If you do not provide a <c>KeyCollector</c> and an operation requires
        /// touch, then the SDK will simply wait for the touch without informing
        /// the caller. However, it will be much more difficult to know when
        /// touch is needed. The end user will have to know that touch is
        /// needed and look for the flashing YubiKey.
        /// </para>
        /// <para>
        /// You can read more about the KeyCollector and its implementation in its
        /// <xref href="UsersManualKeyCollector">user's manual entry</xref>.
        /// </para>
        /// </remarks>
        public Func<KeyEntryData, bool>? KeyCollector { get; set; }

        /// <summary>
        /// The FIDO2 <c>AuthenticatorInfo</c> for the connected YubiKey.
        /// </summary>
        /// <remarks>
        /// Note that it is possible for the <c>AuthenticatorInfo</c> to change
        /// during operations. That is, there are cases where the info says one
        /// thing, then after some operation, the info says something else. This
        /// property is updated each time one such operation is executed.
        /// <para>
        /// For example, if the YubiKey supports a PIN, but it is not set, then
        /// the <c>AuthenticatorInfo.Options</c> will contain the option
        /// "clientPin" and it will be <c>false</c>. After the PIN has been set,
        /// the option "clientPin" will be <c>true</c>.
        /// </para>
        /// <para>
        /// These are not very common, but it is important to use
        /// <c>Fido2Session.AuthenticatorInfo</c> in your code, rather than
        /// getting a copy of the info and using it throughout.
        /// </para>
        /// </remarks>
        public AuthenticatorInfo AuthenticatorInfo => _authenticatorInfo ??= GetAuthenticatorInfoInternal();

        /// <summary>
        /// Retrieves and decrypts the authenticator's unique 128-bit device identifier.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method leverages the <c>encIdentifier</c> value obtained from the authenticator's
        /// <c>authenticatorGetInfo</c> response. The <c>encIdentifier</c> is an encrypted byte string
        /// containing a device identifier that is unique to the authenticator.
        /// </para>
        /// <para>
        /// A valid and active persistent PIN/UV Authentication Token (<c>persistentPinUvAuthToken</c>) is required to decrypt the identifier.
        /// The authenticator must also support and return the `encIdentifier` in its `getInfo` response (YubiKeys v5.8.0 and later).
        /// </para>
        /// </remarks>
        /// <returns>
        /// A byte array containing the decrypted 128-bit (16-byte) unique device identifier.
        /// Returns null or throws an exception if the identifier cannot be decrypted (e.g., if the PPUAT is invalid or the <c>encIdentifier</c> is missing).
        /// </returns>
        public ReadOnlyMemory<byte>? AuthenticatorIdentifier
        {
            get
            {
                var ppuat = GetReadOnlyCredMgmtToken();
                return ppuat.HasValue
                    ? AuthenticatorInfo.GetIdentifier(ppuat.Value)
                    : null;
            }
        }

        /// <summary>
        /// Retrieves and decrypts the authenticator's credential store state.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method leverages the <c>encCredStoreState</c> value obtained from the authenticator's
        /// <c>authenticatorGetInfo</c> response. The <c>encCredStoreState</c> is an encrypted byte string
        /// that platforms can use to detect credential store changes across resets.
        /// </para>
        /// <para>
        /// A valid and active persistent PIN/UV Authentication Token (<c>persistentPinUvAuthToken</c>) is required to decrypt the state.
        /// The authenticator must also support and return the `encCredStoreState` in its `getInfo` response (YubiKeys v5.8.0 and later).
        /// </para>
        /// <para>
        /// By comparing the credential store state before and after operations (or across sessions), platforms can detect
        /// when credentials have been added, removed, or when the authenticator has been reset.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A byte array containing the decrypted 128-bit (16-byte) credential store state.
        /// Returns null if the state cannot be decrypted (e.g., if the PPUAT is invalid or the <c>encCredStoreState</c> is missing).
        /// </returns>
        public ReadOnlyMemory<byte>? AuthenticatorCredStoreState
        {
            get
            {
                var ppuat = GetReadOnlyCredMgmtToken();
                return ppuat.HasValue
                    ? AuthenticatorInfo.GetCredStoreState(ppuat.Value)
                    : null;
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="Fido2Session" />, the object that represents the FIDO2 application on the
        /// YubiKey.
        /// </summary>
        /// <remarks>
        /// Because this class implements <c>IDisposable</c>, use the <c>using</c> keyword. For example,
        /// <code language="csharp">
        ///     IYubiKeyDevice yubiKeyToUse = SelectYubiKey();
        ///     using (var fido2 = new Fido2Session(yubiKeyToUse))
        ///     {
        ///         /* Perform FIDO2 operations. */
        ///     }
        /// </code>
        /// </remarks>
        /// <param name="yubiKey">
        /// The object that represents the actual YubiKey on which the FIDO2 operations should be performed.
        /// </param>
        /// <param name="persistentPinUvAuthToken">If supplied, will be used for credential management read-only operations
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="yubiKey"/> argument is <c>null</c>.
        /// </exception>
        public Fido2Session(IYubiKeyDevice yubiKey, ReadOnlyMemory<byte>? persistentPinUvAuthToken = null)
            : base(Log.GetLogger<Fido2Session>(), yubiKey, YubiKeyApplication.Fido2, keyParameters: null)
        {
            Guard.IsNotNull(yubiKey, nameof(yubiKey));

            Logger.LogInformation(
                "Establishing a new FIDO2 session for YubiKey {SerialNumber}.",
                yubiKey.SerialNumber);

            AuthProtocol = GetPreferredPinProtocol();
            _authTokenPersistent = persistentPinUvAuthToken.HasValue
                ? persistentPinUvAuthToken.Value.ToArray()
                : new Memory<byte>();
        }

        /// <summary>
        /// Returns information about the authenticator (the YubiKey), including defaults and bounds for various fields
        /// and parameters used by FIDO2.
        /// </summary>
        /// <returns>
        /// An <see cref="AuthenticatorInfo"/> instance containing information provided by the YubiKey.
        /// </returns>
        [Obsolete("The GetAuthenticatorInfo method is deprecated, please use the AuthenticatorInfo property instead.")]
        public AuthenticatorInfo GetAuthenticatorInfo() => AuthenticatorInfo;

        private AuthenticatorInfo GetAuthenticatorInfoInternal()
        {
            var response = Connection.SendCommand(new GetInfoCommand());
            return response.GetData();
        }

        private bool OptionPresent(string key) =>
            AuthenticatorInfo.Options != null && AuthenticatorInfo.Options.ContainsKey(key);

        private bool OptionEnabled(string key) => OptionPresent(key) && AuthenticatorInfo.Options![key];

        private static CtapStatus GetCtapError(IYubiKeyResponse r) => (CtapStatus)(r.StatusWord & 0xFF);

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Logger.LogInformation("Disposing of the FIDO2 session.");

                if (_authTokenPersistent.HasValue)
                {
                    CryptographicOperations.ZeroMemory(_authTokenPersistent.Value.Span);
                    _authTokenPersistent = null;
                }

                if (_disposeAuthProtocol)
                {
                    AuthProtocol.Dispose();
                }

                KeyCollector = null;
            }

            base.Dispose(disposing);
        }
    }
}
