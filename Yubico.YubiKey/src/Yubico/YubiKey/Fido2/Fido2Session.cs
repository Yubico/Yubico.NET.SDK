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
using Yubico.YubiKey.Scp;

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
        /// Retrieves and decrypts the authenticator's unique 128-bit device identifier. It will call the KeyCollector to retrieve a persistent PIN/UV Authentication Token (PPUAT), which is required to perform the decryption operation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property leverages the <c>encIdentifier</c> value obtained from the authenticator's
        /// <c>authenticatorGetInfo</c> response. The <c>encIdentifier</c> is an encrypted byte string
        /// containing a device identifier that is unique to the authenticator.
        /// </para>
        /// <para>
        /// A valid and active PPUAT is automatically obtained.
        /// The authenticator must support and return the `encIdentifier` in its `getInfo` response (YubiKeys v5.8.0 and later).
        /// </para>
        /// <para>
        /// The identifier remains constant across PIN changes and other FIDO2 operations, allowing platforms to track
        /// the same physical authenticator across different sessions and states. The identifier is only set to a new random value when the YubiKey's FIDO2 application is reset, as is required by the CTAP 2.3 spec (<see href="https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#authenticatorReset">section 6.6</see>).
        /// </para>
        /// </remarks>
        /// <returns>
        /// <para>
        /// A nullable <see cref="ReadOnlyMemory{T}"/> containing the decrypted 16-byte unique device identifier.
        /// </para>
        /// <para>
        /// Returns <c>null</c> if:
        /// <br/> - The YubiKey firmware does not support this feature (firmware &lt; 5.8.0).
        /// <br/> - The PPUAT could not be obtained.
        /// <br/> - The user cancels PIN entry when prompted.
        /// </para>
        /// <para>
        /// Always check <c>result.HasValue</c> before accessing <c>result.Value</c>.
        /// When <c>HasValue</c> is true, the value will always contain a valid 16-byte identifier.
        /// </para>
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
        /// Retrieves and decrypts the authenticator's credential store state. It will call the KeyCollector to retrieve a persistent PIN/UV Authentication Token (PPUAT), which is required to perform the decryption operation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property leverages the <c>encCredStoreState</c> value obtained from the authenticator's
        /// <c>authenticatorGetInfo</c> response. The <c>encCredStoreState</c> is an encrypted byte string
        /// that platforms can use to detect credential store changes. The credential store state is only set to a new random value after resetting the FIDO2 application, adding or deleting a discoverable credential, and updating a credential's user information, as required by the CTAP 2.3 spec (see <see href="https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#authenticatorReset">section 6.6</see>, <see href="https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#op-makecred-step-rk">section 6.1.2</see>, <see href="https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#deleteCredential">section 6.8.5</see>, and <see href="https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#updateUserInformation">section 6.8.6</see>)
        /// </para>
        /// <para>
        /// A valid and active PPUAT is automatically obtained.
        /// The authenticator must support and return the `encCredStoreState` in its `getInfo` response (YubiKeys v5.8.0 and later).
        /// </para>
        /// <para>
        /// By comparing the credential store state before and after operations (or across sessions), platforms can detect
        /// when important authenticator operations have taken place and react accordingly (e.g. remove a deleted credential from a list of credentials displayed in an application window).
        /// </para>
        /// </remarks>
        /// <returns>
        /// <para>
        /// A nullable <see cref="ReadOnlyMemory{T}"/> containing the decrypted 16-byte credential store state.
        /// </para>
        /// <para>
        /// Returns <c>null</c> if:
        /// <br/> - The YubiKey firmware does not support this feature (firmware &lt; 5.8.0).
        /// <br/> - The PPUAT could not be obtained.
        /// <br/> - The user cancels PIN entry when prompted.
        /// </para>
        /// <para>
        /// Always check <c>result.HasValue</c> before accessing <c>result.Value</c>.
        /// When <c>HasValue</c> is true, the value will always contain a valid 16-byte state.
        /// </para>
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
        /// <para>
        /// Because this class implements <c>IDisposable</c>, use the <c>using</c> keyword. For example,
        /// <code language="csharp">
        ///     IYubiKeyDevice yubiKeyToUse = SelectYubiKey();
        ///     using (var fido2 = new Fido2Session(yubiKeyToUse))
        ///     {
        ///         /* Perform FIDO2 operations. */
        ///     }
        /// </code>
        /// </para>
        /// <para>
        /// To establish an SCP-protected FIDO2 session:
        /// <code language="csharp">
        ///     using (var fido2 = new Fido2Session(yubiKeyToUse, keyParameters: Scp03KeyParameters.DefaultKey))
        ///     {
        ///         /* All FIDO2 commands are encrypted via SCP. */
        ///     }
        /// </code>
        /// </para>
        /// <para>
        /// <b>Transport notes for FIDO2 over SCP:</b> On YubiKey firmware 5.8 and later, FIDO2 is
        /// available over both HID and USB CCID (SmartCard), so SCP works over USB as well as NFC.
        /// On earlier firmware, FIDO2 communicates via HID only over USB, which does not support SCP
        /// (a SmartCard-layer protocol). Over NFC, all firmware versions expose FIDO2 via SmartCard.
        /// </para>
        /// </remarks>
        /// <param name="yubiKey">
        /// The object that represents the actual YubiKey on which the FIDO2 operations should be performed.
        /// </param>
        /// <param name="persistentPinUvAuthToken">If supplied, will be used for credential management read-only operations.
        /// </param>
        /// <param name="keyParameters">
        /// Optional parameters for establishing a Secure Channel Protocol (SCP) connection.
        /// When provided, all communication with the YubiKey will be encrypted and authenticated
        /// using the specified SCP protocol (e.g., SCP03 or SCP11). On firmware prior to 5.8, this
        /// requires an NFC connection. On firmware 5.8+, SCP is also supported over USB.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="yubiKey"/> argument is <c>null</c>.
        /// </exception>
        public Fido2Session(
            IYubiKeyDevice yubiKey,
            ReadOnlyMemory<byte>? persistentPinUvAuthToken = null,
            ScpKeyParameters? keyParameters = null)
            : base(Log.GetLogger<Fido2Session>(), yubiKey, YubiKeyApplication.Fido2, keyParameters)
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
