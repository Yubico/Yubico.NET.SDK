// Copyright 2022 Yubico AB
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
using System.Threading.Tasks;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2
{
    // This portion of the Fido2Session class contains code for PIN operations.
    public sealed partial class Fido2Session
    {
        private const int MaximumAuthTokenLength = 48;

        /// <summary>
        /// The PIN protocol to use for all operations on this session instance.
        /// </summary>
        /// <remarks>
        /// <para>
        /// By default, the SDK will ask the YubiKey for its preferred protocol and will instantiate the appropriate
        /// protocol object. A user can also override the YubiKey's choice and provide their own protocol instance
        /// by calling the <see cref="SetAuthProtocol"/> method on the session class.
        /// </para>
        /// <para>
        /// This property's value will always point to the auth protocol that is currently being used by the session.
        /// </para>
        /// </remarks>
        public PinUvAuthProtocolBase AuthProtocol { get; private set; }

        // We need to dispose the automatically created AuthProtocol. If it's overridden by SetAuthProtocol, then
        // that method will dispose the original, set this to false, and update the reference to the new protocol.
        private bool _disposeAuthProtocol = true;

        /// <summary>
        /// The current PIN / UV Auth token, if present.
        /// </summary>
        /// <remarks>
        /// See the <xref href="Fido2AuthTokens">User's Manual entry</xref> for a
        /// deeper discussion of FIDO2 authentication and how AuthTokens,
        /// permissions, PIN/UV, and AuthParams fit together.
        /// <para>
        /// See also the <xref href="SdkAuthTokenLogic">User's Manual entry</xref>
        /// on the SDK's AuthToken logic. That article goes into greater detail
        /// how the SDK performs "automatic" AuthToken retrieval based on the
        /// version of the connected YubiKey, the state of the Fido2 application
        /// on the YubiKey, the input, and the state of the <c>Fido2Session</c>.
        /// </para>
        /// <para>
        /// The PIN / UV Auth Token, or auth token for short, is created when
        /// (Try)VerifyPin or (Try)VerifyUv is called. The auth token may also
        /// have a set of permissions that restrict the use of the token. These
        /// permissions are specified when verifying the PIN or UV and are shown
        /// in the <see cref="AuthTokenPermissions"/> property.
        /// </para>
        /// </remarks>
        public ReadOnlyMemory<byte>? AuthToken { get; private set; }

        /// <summary>
        /// The set of permissions associated with the <see cref="AuthToken"/>.
        /// </summary>
        /// <remarks>
        /// See the <xref href="Fido2AuthTokens">User's Manual entry</xref> for a
        /// deeper discussion of FIDO2 authentication and how AuthTokens,
        /// permissions, PIN/UV, and AuthParams fit together.
        /// <para>
        /// See also the <xref href="SdkAuthTokenLogic">User's Manual entry</xref>
        /// on the SDK's AuthToken logic. That article goes into greater detail
        /// how the SDK performs "automatic" AuthToken retrieval based on the
        /// version of the connected YubiKey, the state of the Fido2 application
        /// on the YubiKey, the input, and the state of the <c>Fido2Session</c>.
        /// </para>
        /// <para>
        /// The permissions for an auth token are set when PIN or UV verification
        /// occur. This property shows the permission set of the most recent
        /// <c>AuthToken</c>.
        /// </para>
        /// <para>
        /// There are exceptions. It is possible this property does not represent
        /// the current AuthToken's permissions. See the
        /// <xref href="SdkAuthTokenLogic">User's Manual entry</xref> on the
        /// SDK's AuthToken logic for a description of the "corner cases" where
        /// this property is not accurate.
        /// </para>
        /// <para>
        /// Note that because an AuthToken can be expired, this property is not
        /// necessarily the permissions of a valid AuthToken that can be used to
        /// build an AuthParam that will authenticate a command. This property
        /// represents a set of permissions originally specified in the calls to
        /// <see cref="AddPermissions"/>, and those added by the SDK needed to
        /// perform all the operations called.
        /// </para>
        /// <para>
        /// Not all YubiKeys support permissions with the auth tokens. To
        /// determine if if this feature is available, check if the
        /// <c>pinUvAuthToken</c> option is present and <c>true</c> in
        /// <see cref="AuthenticatorInfo.Options"/>. If permissions are not
        /// supported, do not specify any permissions when verifying the PIN.
        /// </para>
        /// <para>
        /// Because an AuthToken can be expired, it is possible an operation will
        /// not be able to execute with the current <c>AuthToken</c>. The SDK
        /// might need to retrieve a new AuthToken with the same permissions
        /// represented here during an operation.
        /// </para>
        /// </remarks>
        public PinUvAuthTokenPermissions? AuthTokenPermissions { get; private set; }

        /// <summary>
        /// The relying party ID associated with the permissions.
        /// </summary>
        /// <remarks>
        /// See the <xref href="Fido2AuthTokens">User's Manual entry</xref> for a
        /// deeper discussion of FIDO2 authentication and how AuthTokens,
        /// permissions, PIN/UV, and AuthParams fit together.
        /// <para>
        /// See also the <xref href="SdkAuthTokenLogic">User's Manual entry</xref>
        /// on the SDK's AuthToken logic. That article goes into greater detail
        /// how the SDK performs "automatic" AuthToken retrieval based on the
        /// version of the connected YubiKey, the state of the Fido2 application
        /// on the YubiKey, the input, and the state of the <c>Fido2Session</c>.
        /// </para>
        /// <para>
        /// If the <see cref="AuthToken"/> was obtained with a permission of
        /// <c>MakeCredential</c> and/or <c>GetAssertion</c>, a relying party ID
        /// was associated with it. It is also optional to associate a relying
        /// party with the <c>CredentialManagement</c> permission as well.
        /// </para>
        /// </remarks>
        public string? AuthTokenRelyingPartyId { get; private set; }

        private const int PinMinimumByteLength = 4;
        private const int PinMaximumByteLength = 63;

        /// <summary>
        /// Obtain a PinUvAuthToken that possesses the given permissions along
        /// with the current permissions. This is generally called early in a
        /// session to specify which set of permissions you expect to need for
        /// the operations you will be calling.
        /// </summary>
        /// <remarks>
        /// See the <xref href="Fido2AuthTokens">User's Manual entry</xref> for a
        /// deeper discussion of FIDO2 authentication and how AuthTokens,
        /// PinTokens, permissions, PIN/UV, and AuthParams fit together.
        /// <para>
        /// See also the <xref href="SdkAuthTokenLogic">User's Manual entry</xref>
        /// on the SDK's AuthToken logic. That article goes into greater detail
        /// how this method, as well as other operations, perform "automatic"
        /// AuthToken retrieval based on the version of the connected YubiKey,
        /// the state of the Fido2 application on the YubiKey, the input, and the
        /// state of the <c>Fido2Session</c>.
        /// </para>
        /// <para>
        /// This method will use the <see cref="KeyCollector"/> to obtain the PIN
        /// or prompt for user verification (fingerprint on YubiKey Bio). If no
        /// <c>KeyCollector</c> is loaded, then this method will throw an
        /// exception.
        /// </para>
        /// <para>
        /// If the connected YubiKey does not support the option
        /// <c>pinUvAuthToken</c>, then there is likely no need to call this
        /// method. But if you do, the <c>relyingPartyId</c> arg must be null and
        /// the <c>permissions</c> arg must be <c>None</c>. To know if the
        /// YubiKey supports it, make this call.
        /// <code language="csharp">
        ///     IYubiKeyDevice yubiKeyToUse = SelectYubiKey();
        ///     using (var fido2Session = new Fido2Session(yubiKeyToUse))
        ///     {
        ///         if (fido2Session.AuthenticatorInfo.GetOptionValue(
        ///             AuthenticatorOptions.pinUvAuthToken) == OptionValue.True)
        ///         {
        ///             fido2Session.AddPermissions(
        ///                 PinUvAuthTokenPermissions.GetAssertion | PinUvAuthTokenPermissions.CredentialManagement,
        ///                 relyingPartyIdOfInterest);
        ///         }
        ///     }
        /// </code>
        /// Generally, YubiKeys that support only FIDO2 version 2.0 will not
        /// support <c>pinUvAuthToken</c> and YubiKeys that support FIDO2 version
        /// 2.1, will support it. See also the
        /// <xref href="SdkAuthTokenLogic">User's Manual entry</xref>
        /// on the SDK's AuthToken logic for more details on using this method
        /// with a YubiKey that supports only FIDO2 version 2.0.
        /// </para>
        /// <para>
        /// If the YubiKey does support <c>pinUvAuthToken</c>, then you can still
        /// call this method with no permissions (<c>None</c>) and a null
        /// <c>relyingPartyId</c>. In this case, the SDK will build a PinToken.
        /// See <xref href="Fido2AuthTokens">this User's Manual entry</xref> for
        /// a deeper discussion of PinTokens on YubiKey that supports
        /// PinUvAuthTokens. If you call with no permissions but with a relying
        /// party ID, then this method will throw an exception.
        /// </para>
        /// <para>
        /// Note that if you pass in permissions of <c>None</c>, this method will
        /// obtain an AuthToken that has the same permission set currently set in
        /// the property <see cref="PinUvAuthTokenPermissions"/>, if there are
        /// any.
        /// </para>
        /// <para>
        /// If the YubiKey supports PinUvAuthTokens, and this is called with
        /// permissions, then this method will determine if the YubiKey has UV
        /// capabilities, and if so, whether any bio verification has been
        /// enrolled. On the YubiKey, if there is a fingerprint enrolled, this
        /// method will first try to authenticate using UV, and if that fails,
        /// try to authenticate using PIN (see the FIDO2 standard, sections
        /// 6.5.5.7 and 6.5.5.7.3 step 3.10.3).
        /// </para>
        /// <para>
        /// Calling this method will make sure that the <see cref="AuthToken"/>
        /// property in this class possesses the requested permissions along
        /// with any permissions specified in the
        /// <see cref="AuthTokenPermissions"/> property. To do so, this method
        /// will always obtain a new AuthToken, even if there is an AuthToken
        /// already in the object. More details are in the
        /// <xref href="SdkAuthTokenLogic">User's Manual entry</xref>
        /// on the SDK's AuthToken logic.
        /// </para>
        /// <para>
        /// The <c>MakeCredential</c> and <c>GetAssertion</c> permissions require
        /// a relying party ID. That is, an AuthToken can be used to make a
        /// credential or get an assertion only for a specific relying party. If
        /// the <c>permissions</c> arg does not include <c>MakeCredential</c> or
        /// <c>GetAssertion</c>, then the <c>relyingPartyId</c> arg can be null.
        /// More details are in the
        /// <xref href="SdkAuthTokenLogic">User's Manual entry</xref>
        /// on the SDK's AuthToken logic.
        /// </para>
        /// <para>
        /// It is not necessary to call this method at the beginning. However, if
        /// you do not make this call, then each time the SDK needs an AuthToken,
        /// it will obtain one with only one permission. That means that if you
        /// perform more than one operation, you might be called upon to obtain
        /// the PIN from the user, or require the user to perform user
        /// verification several times during this session.
        /// </para>
        /// <para>
        /// An alternative to this method and the "automatic" AuthToken
        /// collection the SDK performs is to call
        /// <see cref="TryVerifyPin(ReadOnlyMemory{byte}, PinUvAuthTokenPermissions?, string?, out int?, out bool?)"/>,
        /// <see cref="TryVerifyUv(PinUvAuthTokenPermissions, string?)"/> or
        /// some other verification method directly. However, because AuthTokens
        /// "expire" (see the <xref href="Fido2AuthTokens">User's Manual entry</xref>
        /// on AuthTokens), it is possible you will need to re-verify during a
        /// session. Hence, if you want to avoid this method or not build a
        /// <c>KeyCollector</c>, then you must write your code so that it calls,
        /// if needed, the appropriate <c>Verify</c> method before performing an
        /// operation. That is, you must then write your code so that it adheres
        /// to the permissions and expriy logic of FIDO2.
        /// </para>
        /// </remarks>
        /// <param name="permissions">
        /// An OR of all the permissions you expect to be needed during the
        /// session.
        /// </param>
        /// <param name="relyingPartyId">
        /// If needed or wanted, how to specify the relying party in question.
        /// The default for this parameter is null, so if no arg is given, then
        /// there will be no relying party specified.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The connected YubiKey does not support the permissions given, or a
        /// relying party was specified but no permissions were specified.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The permission requires a relyingPartyId and none is given.
        /// </exception>
        public void AddPermissions(PinUvAuthTokenPermissions permissions, string? relyingPartyId = null)
        {
            PinUvAuthTokenPermissions current = AuthTokenPermissions ?? PinUvAuthTokenPermissions.None;
            PinUvAuthTokenPermissions allPermissions = permissions | current;

            // If the caller supplies an RpId, replace the one in the
            // AuthTokenRelyingPartyId property.
            string? rpId = relyingPartyId ?? AuthTokenRelyingPartyId;

            // If there are no permissions and there is no RpId, then we'll get a
            // PinToken. This generally happens with YubiKeys that support only
            // FIDO2 version 2.0, but we will do this with 2.1 as well.
            // If there is a relying party but no permissions, throw an
            // exception.
            if (allPermissions == PinUvAuthTokenPermissions.None)
            {
                if (!(rpId is null))
                {
                    throw new ArgumentException(ExceptionMessages.Fido2PermsMissing);
                }
            }
            else
            {
                // If this does not support the option pinUvAuthToken, then the
                // current permissions must be None and the rpId variable must be
                // null.
                if (AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.pinUvAuthToken) != OptionValue.True)
                {
                    throw new ArgumentException(ExceptionMessages.Fido2PermsNotSupported);
                }

                // If the permissions requested require an RpId, then make sure there
                // is one.
                if ((allPermissions.GetRpIdRequirement() == RequirementValue.Required) && (rpId is null))
                {
                    throw new InvalidOperationException(ExceptionMessages.Fido2RelyingPartyMissing);
                }
            }


            // Try to verify with Uv. If that doesn't work (or is not supported),
            // verify the PIN.
            if (DoVerifyUv(allPermissions, rpId, out string _) != CtapStatus.Ok)
            {
                VerifyPin(allPermissions, rpId);
            }
        }

        // This is called by methods inside the Fido2Session class.
        // When a method wants to perform an operation that requires an AuthToken,
        // it will call this method. This method will return an object that is
        // not nullable.
        // If this method is called with false for forceNewToken, it will check
        // to see if there is an existing AuthToken, and if there is one, return
        // it. If not build a new one.
        // It is possible that the current AuthToken has expired, so returning
        // the existing one will not return a workable one. That's OK, we can't
        // know whether it is valid or not unless we try it, so we'll return it
        // and let the caller try it.
        // If this is called with true for forceNewToken, it will skip the check
        // on the existing AuthToken and simply build a new one. This happens
        // when a method tries to perform an operation, but the response is
        // CTAP2_ERR_PIN_AUTH_INVALID (Ctap2Status.PinAuthInvalid). It will call
        // this method with a true "force" arg.
        // This error generally happens when the AuthToken does not work. And the
        // AuthToken generally does not work when it does not have the
        // appropriate permissions, either because it lost them through expiry,
        // or it never had them in the first place.
        // Although it is possible to get the AUTH_INVALID error for reasons
        // other than "missing permission", when we get this error, we will try
        // this method. It will get a new AuthToken with the required
        // permissions. If the original operation now works, great, if not, then
        // it can throw an exception.
        // This method will check to see if the connected YubiKey supports
        // "pinUvAuthToken". If it does, call AddPermissions with the input here.
        // If not, just call AddPermissions with None and null. This is so
        // callers don't have to make that decision, we make it once. For
        // example, suppose some operation wants to get an assertion, but they
        // got the error. So they could check to see if they need to get an
        // AuthToken with permissions, or if they can only get a PinToken. But by
        // simply calling this method they don't have to worry about that. Just
        // always call the same method with the same arguments.
        // It's possible that someone tried to do something that was not
        // supported on a particular YubiKey (e.g. credential management). In
        // that case, the original error would have been
        // CTAP2_ERR_UNSUPPORTED_OPTION or possibly something else, and so the
        // method operating would have already thrown an exception and will not
        // call this method.
        private ReadOnlyMemory<byte> GetAuthToken(
            bool forceNewToken, PinUvAuthTokenPermissions permissions, string? relyingPartyId = null)
        {
            // If the caller is willing to use the existing AuthToken (force is
            // false), and it exists, return it.
            // Note that we're not going to check permissions here because even
            // if we're on a YubiKey that supports permissions, the AuthToken
            // might be a PinToken and might work. We'll let the caller decide if
            // it works or not. If not, they'll call again with a force of true.
            if (!forceNewToken && !(AuthToken is null))
            {
                return AuthToken.Value;
            }

            if (AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.pinUvAuthToken) == OptionValue.True)
            {
                AddPermissions(permissions, relyingPartyId);
            }
            else
            {
                AddPermissions(PinUvAuthTokenPermissions.None, null);
            }

            return AuthToken ?? ReadOnlyMemory<byte>.Empty;
        }

        /// <summary>
        /// Reset the <see cref="AuthToken"/>, <see cref="AuthTokenPermissions"/>,
        /// and <see cref="AuthTokenRelyingPartyId"/> to null, so that any future
        /// operation that retrieves an <see cref="AuthToken"/> will not use the
        /// current values.
        /// </summary>
        /// <remarks>
        /// See the <xref href="Fido2AuthTokens">User's Manual entry</xref> for a
        /// deeper discussion of FIDO2 authentication and how AuthTokens,
        /// permissions, PIN/UV, and AuthParams fit together.
        /// <para>
        /// See also the <xref href="SdkAuthTokenLogic">User's Manual entry</xref>
        /// on the SDK's AuthToken logic. That article goes into greater detail
        /// how this method, as well as other operations, perform "automatic"
        /// AuthToken retrieval based on the version of the connected YubiKey,
        /// the state of the Fido2 application on the YubiKey, the input, and the
        /// state of the <c>Fido2Session</c>.
        /// </para>
        /// <para>
        /// Generally you will begin a <c>Fido2Session</c> with a call to
        /// <see cref="AddPermissions"/>. If the <c>AuthToken</c> is expired,
        /// and an AuthToken is needed for a new operation, the SDK will obtain a
        /// new AuthToken, using the original permissions (and any new
        /// permisisons needed by the operation) and the
        /// <see cref="AuthTokenRelyingPartyId"/>.
        /// </para>
        /// <para>
        /// However, if you want to set the <c>AuthTokenPermissions</c> to a
        /// completely new value that does not have the same permission set as
        /// the current, or set it to be associated with a new relying party, or
        /// with no relying party at all, then clear the current set of values.
        /// </para>
        /// <para>
        /// If you ever need to clear the <c>AuthToken</c> and associated
        /// properties, you will likely follow up a call to this method with a
        /// call to <c>AddPermissions</c> to start a new process.
        /// </para>
        /// </remarks>
        public void ClearAuthToken()
        {
            AuthToken = null;
            AuthTokenPermissions = null;
            AuthTokenRelyingPartyId = null;
        }

        /// <summary>
        /// Sets the initial FIDO2 PIN using the <c>KeyCollector</c>. To change an existing PIN, use
        /// the <see cref="ChangePin"/> function.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The Yubikey is manufactured with no default PIN set on the FIDO2 application. To configure a YubiKey's
        /// initial PIN, use this function. This function will only succeed if no PIN is currently configured. To change
        /// an existing PIN, use the <see cref="ChangePin"/> method instead. Once set, a PIN cannot be removed
        /// without resetting the FIDO2 application. The reset operation will remove the PIN and clear all registered
        /// credentials.
        /// </para>
        /// <para>
        /// Several considerations must be made when collecting the PIN.
        /// <list type="bullet">
        /// <item><description>It must be encoded in UTF-8 with Normalization Form C.</description></item>
        /// <item><description>It must be at least 4 Unicode code points in length.</description></item>
        /// <item><description>It must not exceed 63 bytes in encoded length.</description></item>
        /// </list>
        /// Read more about PINs <xref href="TheFido2Pin">here</xref>.
        /// </para>
        /// </remarks>
        /// <exception cref="SecurityException">
        /// The YubiKey already has a PIN set. This function cannot be used to change the PIN.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user cancelled PIN collection. This happens when the application returns <c>false</c>
        /// in the <c>KeyCollector</c>.
        /// </exception>
        public void SetPin()
        {
            if (TrySetPin())
            {
                return;
            }

            throw new OperationCanceledException(ExceptionMessages.PinCollectionCancelled);
        }

        /// <summary>
        /// Tries to set the initial FIDO2 PIN using the <c>KeyCollector</c>. To change an existing PIN, use
        /// the <see cref="TryChangePin()"/> function.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The Yubikey is manufactured with no default PIN set on the FIDO2 application. To initially configure
        /// a PIN, use this function. This function will only succeed if no PIN is currently configured. To change
        /// an already set PIN, use the <see cref="ChangePin"/> method instead. Once set, a PIN cannot be removed
        /// without resetting the FIDO2 application. This operation will unset the PIN as well as clear all registered
        /// credentials.
        /// </para>
        /// <para>
        /// Several considerations must be made when collecting the PIN.
        /// <list type="bullet">
        /// <item><description>It must be encoded in UTF-8 with Normalization Form C.</description></item>
        /// <item><description>It must be at least 4 Unicode code points in length.</description></item>
        /// <item><description>It must not exceed 63 bytes in encoded length.</description></item>
        /// </list>
        /// Read more about PINs <xref href="TheFido2Pin">here</xref>.
        /// </para>
        /// </remarks>
        /// <returns>
        /// <c>True</c> on success, <c>False</c> if the user cancelled PIN collection, and an exception for all
        /// other kinds of failures.
        /// </returns>
        /// <exception cref="SecurityException">
        /// The YubiKey already has a PIN set. This function cannot be used to change the PIN.
        /// </exception>
        public bool TrySetPin()
        {
            Func<KeyEntryData, bool> keyCollector = EnsureKeyCollector();

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.SetFido2Pin,
            };

            try
            {
                if (!keyCollector(keyEntryData))
                {
                    return false; // User cancellation
                }

                if (TrySetPin(keyEntryData.GetCurrentValue()))
                {
                    return true;
                }

                throw new SecurityException(ExceptionMessages.PinAlreadySet);
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = keyCollector(keyEntryData);
            }
        }

        /// <summary>
        /// Tries to set the initial FIDO2 PIN. To change an existing PIN, use the
        /// <see cref="TryChangePin()"/> function.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The Yubikey is manufactured with no default PIN set on the FIDO2 application. To initially configure
        /// a PIN, use this function. This function will only succeed if no PIN is currently configured. To change
        /// an already set PIN, use the <see cref="ChangePin"/> method instead. Once set, a PIN cannot be removed
        /// without resetting the FIDO2 application. This operation will unset the PIN as well as clear all registered
        /// credentials.
        /// </para>
        /// </remarks>
        /// <param name="newPin">
        /// The PIN to program onto the YubiKey. It must be encoded in UTF-8 with Normalization Form C. It must be
        /// at least 4 Unicode code points in length, and not to exceed 63 bytes in encoded length. Read more about
        /// PINs <xref href="TheFido2Pin">here</xref>.
        /// </param>
        /// <returns>
        /// <c>True</c> on success, <c>False</c> if the YubiKey has a PIN already set, and an exception for all
        /// other kinds of failures.
        /// </returns>
        public bool TrySetPin(ReadOnlyMemory<byte> newPin)
        {
            VerifyPinLengthRequirements(newPin);

            ObtainSharedSecret();

            SetPinResponse result = Connection.SendCommand(new SetPinCommand(AuthProtocol, newPin));

            if (result.Status == ResponseStatus.Success)
            {
                // Setting the PIN changes the AuthenticatorInfo, so set this to
                // null so the next reference initiates a new GetInfo command.
                _authenticatorInfo = null;
                return true;
            }

            // Spec says "PinAuthInvalid" for PIN already set. YubiKey says "NotAllowed".
            if (GetCtapError(result) == CtapStatus.PinAuthInvalid ||
                GetCtapError(result) == CtapStatus.NotAllowed)
            {
                return false; // PIN is already set.
            }

            throw new Fido2Exception(result.StatusMessage);
        }

        /// <summary>
        /// Changes the PIN using the <c>KeyCollector</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// FIDO2 separates the action of setting the initial PIN from changing it. Use <see cref="SetPin"/> to set
        /// the first PIN, and use this method to change the PIN after that. In order to change the PIN, both
        /// the current PIN and the new PIN must be supplied. A PIN cannot be removed from FIDO2. The only way
        /// to clear the PIN is to reset the entire FIDO2 application, which will result in all credentials being
        /// removed.
        /// </para>
        /// <para>
        /// Several considerations must be made when collecting the PIN.
        /// <list type="bullet">
        /// <item><description>It must be encoded in UTF-8 with Normalization Form C.</description></item>
        /// <item><description>It must be at least 4 Unicode code points in length.</description></item>
        /// <item><description>It must not exceed 63 bytes in encoded length.</description></item>
        /// </list>
        /// Read more about PINs <xref href="TheFido2Pin">here</xref>.
        /// </para>
        /// </remarks>
        /// <exception cref="OperationCanceledException">
        /// The user cancelled PIN collection. This happens when the application returns <c>false</c>
        /// in the <c>KeyCollector</c>.
        /// </exception>
        /// <exception cref="Fido2Exception">
        /// The YubiKey returned an error indicating that the change PIN request could not be completed.
        /// </exception>
        public void ChangePin()
        {
            if (TryChangePin())
            {
                return;
            }

            throw new OperationCanceledException(ExceptionMessages.PinCollectionCancelled);
        }

        /// <summary>
        /// Tries to change the PIN using the <c>KeyCollector</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// FIDO2 separates the action of setting the initial PIN from changing it. Use <see cref="SetPin"/> to set
        /// the first PIN, and use this method to change the PIN after that. In order to change the PIN, both
        /// the current PIN and the new PIN must be supplied. A PIN cannot be removed from FIDO2. The only way
        /// to clear the PIN is to reset the entire FIDO2 application, which will result in all credentials being
        /// removed.
        /// </para>
        /// <para>
        /// Several considerations must be made when collecting the PIN.
        /// <list type="bullet">
        /// <item><description>It must be encoded in UTF-8 with Normalization Form C.</description></item>
        /// <item><description>It must be at least 4 Unicode code points in length.</description></item>
        /// <item><description>It must not exceed 63 bytes in encoded length.</description></item>
        /// </list>
        /// Read more about PINs <xref href="TheFido2Pin">here</xref>.
        /// </para>
        /// </remarks>
        /// <returns>
        /// <c>True</c> on success, <c>False</c> if the user cancelled PIN collection, and an exception for all
        /// other kinds of failures.
        /// </returns>
        /// <exception cref="Fido2Exception">
        /// The YubiKey returned an error indicating that the change PIN request could not be completed.
        /// </exception>
        public bool TryChangePin()
        {
            Func<KeyEntryData, bool> keyCollector = EnsureKeyCollector();

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.ChangeFido2Pin
            };

            try
            {
                while (keyCollector(keyEntryData))
                {
                    if (TryChangePin(keyEntryData.GetCurrentValue(), keyEntryData.GetNewValue()))
                    {
                        return true;
                    }

                    keyEntryData.IsRetry = true;
                }
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = keyCollector(keyEntryData);
            }

            return false;
        }

        /// <summary>
        /// Tries to change the PIN.
        /// </summary>
        /// <remarks>
        /// <para>
        /// FIDO2 separates the action of setting the initial PIN from changing it. Use <see cref="SetPin"/> to set
        /// the first PIN, and use this method to change the PIN after that. In order to change the PIN, both
        /// the current PIN and the new PIN must be supplied. A PIN cannot be removed from FIDO2. The only way
        /// to clear the PIN is to reset the entire FIDO2 application, which will result in all credentials being
        /// removed.
        /// </para>
        /// </remarks>
        /// <param name="currentPin">
        /// The existing PIN encoded using UTF-8 in Normalization Form C.
        /// </param>
        /// <param name="newPin">
        /// The new PIN to program onto the YubiKey. The new PIN:
        /// <list type="bullet">
        /// <item><description>Must be encoded in UTF-8 with Normalization Form C.</description></item>
        /// <item><description>Must be at least 4 Unicode code points in length.</description></item>
        /// <item><description>Must not exceed 63 bytes in encoded length.</description></item>
        /// </list>
        /// Read more about PINs <xref href="TheFido2Pin">here</xref>.
        /// </param>
        /// <returns>True, if successful. Otherwise false.</returns>
        /// <exception cref="Fido2Exception">
        /// The YubiKey returned an error indicating that the change PIN request could not be completed.
        /// </exception>
        public bool TryChangePin(ReadOnlyMemory<byte> currentPin, ReadOnlyMemory<byte> newPin)
        {
            VerifyPinLengthRequirements(newPin);

            ObtainSharedSecret();

            ChangePinResponse result = Connection.SendCommand(new ChangePinCommand(AuthProtocol, currentPin, newPin));

            if (result.Status == ResponseStatus.Success)
            {
                return true;
            }

            if (GetCtapError(result) == CtapStatus.PinInvalid)
            {
                // FIDO authenticators regenerate the public key used for the auth protocol. We need to
                // re-initialize everything so we can obtain the new shared secret.
                AuthProtocol.Initialize();
                return false; // PIN is invalid
            }

            throw new Fido2Exception(result.StatusMessage);
        }

        /// <summary>
        /// Verifies the PIN against the YubiKey using the <c>KeyCollector</c>.
        /// </summary>
        /// <remarks>
        /// If the <c>permissions</c> arg is null or <c>None</c>, then this
        /// method will obtain a PinToken. See
        /// <xref href="Fido2AuthTokens">this User's Manual entry</xref> for a
        /// deeper discussion of PinTokens on YubiKey that supports
        /// PinUvAuthTokens. If you call with no permissions but with a relying
        /// party ID, then this method will throw an exception.
        /// <para>
        /// A YubiKey is manufactured with no PIN set on the FIDO2 application. A PIN must be set before a user
        /// can perform most FIDO2 operations. After a PIN has been set, it must be verified against the YubiKey
        /// before privileged operations can occur. This method will perform that verification.
        /// </para>
        /// <para>
        /// Unlike other applications in this SDK (such as PIV and OATH), the SDK will not automatically verify PIN or
        /// UV using the KeyCollector in methods like <see cref="MakeCredential"/> due to FIDO2's complex user
        /// verification process. Your application must call this method explicitly before attempting to perform a FIDO2
        /// operation that requires verification.
        /// </para>
        /// <para>
        /// This version of VerifyPin uses the <see cref="KeyCollector"/> delegate. You can read about key collectors
        /// in much more detail in the <xref href="UsersManualKeyCollector">user's manual entry</xref>.
        /// </para>
        /// <para>
        /// If the PIN was incorrectly entered, the SDK will automatically retry. The key collector will be called
        /// again allowing for another attempt at entry. Each time the key collector is called, the <c>IsRetry</c>
        /// member will be set to <c>true</c> and the <c>RetryCount</c> will be updated to reflect the number of
        /// retries left before the YubiKey blocks further PIN attempts. To cancel pin collection operations, simply
        /// return <c>false</c> in the handler for the key collector.
        /// </para>
        /// <para>
        /// The PIN, while often comprised of ASCII values, can in fact contain most Unicode characters. The PIN
        /// must be encoded as a byte array using a UTF-8 encoding in Normalized Form C. See the
        /// <xref href="TheFido2Pin">user's manual entry</xref> on FIDO2 PINs for more information.
        /// </para>
        /// </remarks>
        /// <param name="permissions">
        /// The set of operations that this auth token should be permitted to do. This parameter is allowed only if the
        /// YubiKey contains the `pinUvAuthToken` option in <see cref="AuthenticatorInfo.Options"/>. If the YubiKey
        /// does not support this, leave the parameter `null`; the legacy <see cref="GetPinTokenCommand"/> will be used
        /// as a fallback.
        /// </param>
        /// <param name="relyingPartyId">
        /// Some <paramref name="permissions"/> require the qualification of a relying party ID. This parameter should
        /// only be specified when a permission requires it, otherwise it should be left null. See
        /// <see cref="PinUvAuthTokenPermissions"/> for more details on which permissions require the RP ID and for which
        /// it is optional.
        /// </param>
        /// <exception cref="OperationCanceledException">
        /// The user cancelled PIN collection. This happens when the application returns <c>false</c>
        /// in the <c>KeyCollector</c>.
        /// </exception>
        /// <exception cref="SecurityException">
        /// There are no retries remaining.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey does not have a PIN set.
        /// --- or ---
        /// This YubiKey does not support permissions on PIN / UV auth tokens.
        /// </exception>
        /// <exception cref="Fido2Exception">
        /// The YubiKey returned an error indicating that the PIN verification request could not be completed.
        /// </exception>
        public void VerifyPin(PinUvAuthTokenPermissions? permissions = null, string? relyingPartyId = null)
        {
            if (TryVerifyPin(permissions, relyingPartyId))
            {
                return;
            }

            throw new OperationCanceledException(ExceptionMessages.PinCollectionCancelled);
        }

        /// <summary>
        /// Tries to verify the PIN against the YubiKey using the <c>KeyCollector</c>.
        /// </summary>
        /// <remarks>
        /// If the <c>permissions</c> arg is null or <c>None</c>, then this
        /// method will obtain a PinToken. See
        /// <xref href="Fido2AuthTokens">this User's Manual entry</xref> for a
        /// deeper discussion of PinTokens on YubiKey that supports
        /// PinUvAuthTokens. If you call with no permissions but with a relying
        /// party ID, then this method will throw an exception.
        /// <para>
        /// A YubiKey is manufactured with no PIN set on the FIDO2 application. A PIN must be set before a user
        /// can perform most FIDO2 operations. After a PIN has been set, it must be verified against the YubiKey
        /// before privileged operations can occur. This method will perform that verification.
        /// </para>
        /// <para>
        /// Unlike other applications in this SDK (such as PIV and OATH), the SDK will not automatically verify PIN or
        /// UV using the KeyCollector in methods like <see cref="MakeCredential"/> due to FIDO2's complex user
        /// verification process. Your application must call this method explicitly before attempting to perform a FIDO2
        /// operation that requires verification.
        /// </para>
        /// <para>
        /// This version of TryVerifyPin uses the <see cref="KeyCollector"/> delegate. You can read about key collectors
        /// in much more detail in the <xref href="UsersManualKeyCollector">user's manual entry</xref>.
        /// </para>
        /// <para>
        /// If the PIN was incorrectly entered, the SDK will automatically retry. The key collector will be called
        /// again allowing for another attempt at entry. Each time the key collector is called, the <c>IsRetry</c>
        /// member will be set to <c>true</c> and the <c>RetryCount</c> will be updated to reflect the number of
        /// retries left before the YubiKey blocks further PIN attempts. To cancel pin collection operations, simply
        /// return <c>false</c> in the handler for the key collector.
        /// </para>
        /// <para>
        /// The PIN, while often comprised of ASCII values, can in fact contain most Unicode characters. The PIN
        /// must be encoded as a byte array using a UTF-8 encoding in Normalized Form C. See the
        /// <xref href="TheFido2Pin">user's manual entry</xref> on FIDO2 PINs for more information.
        /// </para>
        /// </remarks>
        /// <param name="permissions">
        /// The set of operations that this auth token should be permitted to do. This parameter is allowed only if the
        /// YubiKey contains the `pinUvAuthToken` option in <see cref="AuthenticatorInfo.Options"/>. If the YubiKey
        /// does not support this, leave the parameter `null` and the legacy <see cref="GetPinTokenCommand"/> will be used
        /// as a fallback.
        /// </param>
        /// <param name="relyingPartyId">
        /// Some <paramref name="permissions"/> require the qualification of a relying party ID. This parameter should
        /// only be specified when a permission requires it, otherwise it should be left null. See
        /// <see cref="PinUvAuthTokenPermissions"/> for more details on which permissions require the RP ID and for which
        /// it is optional.
        /// </param>
        /// <returns>
        /// <c>True</c> on success, <c>False</c> if the user cancelled PIN collection, and an exception for all
        /// other kinds of failures.
        /// </returns>
        /// <exception cref="SecurityException">
        /// There are no retries remaining.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey does not have a PIN set.
        /// --- or ---
        /// This YubiKey does not support permissions on PIN / UV auth tokens.
        /// </exception>
        /// <exception cref="Fido2Exception">
        /// The YubiKey returned an error indicating that the PIN verification request could not be completed.
        /// </exception>
        public bool TryVerifyPin(PinUvAuthTokenPermissions? permissions = null, string? relyingPartyId = null)
        {
            Func<KeyEntryData, bool> keyCollector = EnsureKeyCollector();

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.VerifyFido2Pin
            };

            try
            {
                while (keyCollector(keyEntryData))
                {
                    if (TryVerifyPin(keyEntryData.GetCurrentValue(), permissions, relyingPartyId, out int? retriesRemaining, out _))
                    {
                        return true;
                    }

                    keyEntryData.IsRetry = true;
                    keyEntryData.RetriesRemaining = retriesRemaining!; // If we are retrying, we know this won't be null.

                    if (keyEntryData.RetriesRemaining == 0)
                    {
                        throw new SecurityException(ExceptionMessages.Fido2NoMoreRetries);
                    }
                }
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = keyCollector(keyEntryData);
            }

            return false;
        }

        /// <summary>
        /// Tries to verify the PIN against the YubiKey.
        /// </summary>
        /// <remarks>
        /// If the <c>permissions</c> arg is null or <c>None</c>, then this
        /// method will obtain a PinToken. See
        /// <xref href="Fido2AuthTokens">this User's Manual entry</xref> for a
        /// deeper discussion of PinTokens on YubiKey that supports
        /// PinUvAuthTokens. If you call with no permissions but with a relying
        /// party ID, then this method will throw an exception.
        /// <para>
        /// A YubiKey is manufactured with no PIN set on the FIDO2 application. A PIN must be set before a user
        /// can perform most FIDO2 operations. After a PIN has been set, it must be verified against the YubiKey
        /// before privileged operations can occur. This method will perform that verification.
        /// </para>
        /// <para>
        /// Unlike other applications in this SDK (such as PIV and OATH), the SDK will not automatically verify PIN or
        /// UV using the KeyCollector in methods like <see cref="MakeCredential"/> due to FIDO2's complex user
        /// verification process. Your application must call this method explicitly before attempting to perform a FIDO2
        /// operation that requires verification.
        /// </para>
        /// <para>
        /// This version of TryVerifyPin does not use the key collector. This method will only attempt to verify a
        /// single PIN and will not automatically retry. In this case, the method will return <c>false</c> if the
        /// PIN was incorrect. It will throw an exception in all other failure cases.
        /// </para>
        /// <para>
        /// The PIN, while often comprised of ASCII values, can in fact contain most Unicode characters. The PIN
        /// must be encoded as a byte array using a UTF-8 encoding in Normalized Form C. See the
        /// <xref href="TheFido2Pin">user's manual entry</xref> on FIDO2 PINs for more information.
        /// </para>
        /// </remarks>
        /// <param name="currentPin">
        /// The FIDO2 PIN that you wish to verify.
        /// </param>
        /// <param name="retriesRemaining">
        /// The number of PIN retries remaining before the FIDO2 application becomes locked.
        /// </param>
        /// <param name="rebootRequired">
        /// Indicates whether a reboot of the YubiKey (unplug and re-insert) is required before further PIN retries are
        /// allowed.
        /// </param>
        /// <param name="permissions">
        /// The set of operations that this auth token should be permitted to do. This parameter is allowed only if the
        /// YubiKey contains the `pinUvAuthToken` option in <see cref="AuthenticatorInfo.Options"/>. If the YubiKey
        /// does not support this, this parameter must be `null` and the legacy <see cref="GetPinTokenCommand"/> will be
        /// used as a fallback.
        /// </param>
        /// <param name="relyingPartyId">
        /// Some <paramref name="permissions"/> require the qualification of a relying party ID. This parameter should
        /// only be specified when a permission requires it, otherwise it should be left null. See
        /// <see cref="PinUvAuthTokenPermissions"/> for more details on which permissions require the RP ID and for which
        /// it is optional. If <paramref name="permissions"/> is `null`, this parameter must also be `null`.
        /// </param>
        /// <returns>
        /// <c>True</c> if the PIN successfully verified, <c>False</c> if the PIN was incorrect, and an exception for all
        /// other kinds of failures.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey does not have a PIN set.
        /// --- or ---
        /// This YubiKey does not support permissions on PIN / UV auth tokens.
        /// </exception>
        /// <exception cref="Fido2Exception">
        /// The YubiKey returned an error indicating that the PIN verification request could not be completed.
        /// </exception>
        public bool TryVerifyPin(
            ReadOnlyMemory<byte> currentPin,
            PinUvAuthTokenPermissions? permissions,
            string? relyingPartyId,
            out int? retriesRemaining,
            out bool? rebootRequired)
        {
            IYubiKeyCommand<GetPinUvAuthTokenResponse> command;

            if (!OptionEnabled(AuthenticatorInfo, "clientPin"))
            {
                throw new InvalidOperationException(ExceptionMessages.Fido2NoPin);
            }

            ObtainSharedSecret();

            if (!permissions.HasValue || (permissions == PinUvAuthTokenPermissions.None))
            {
                if (!string.IsNullOrEmpty(relyingPartyId))
                {
                    throw new ArgumentException(ExceptionMessages.Fido2PermsMissing);
                }

                command = new GetPinTokenCommand(AuthProtocol, currentPin);
            }
            else
            {
                if (!OptionEnabled(AuthenticatorInfo, "pinUvAuthToken"))
                {
                    throw new InvalidOperationException(ExceptionMessages.Fido2PermsNotSupported);
                }

                command = new GetPinUvAuthTokenUsingPinCommand(
                    AuthProtocol,
                    currentPin,
                    permissions.Value,
                    relyingPartyId);
            }

            GetPinUvAuthTokenResponse response = Connection.SendCommand(command);

            if (response.Status == ResponseStatus.Success)
            {
                AuthToken = response.GetData();
                AuthTokenPermissions = permissions;
                AuthTokenRelyingPartyId = relyingPartyId;

                retriesRemaining = null;
                rebootRequired = null;

                return true;
            }

            if (GetCtapError(response) == CtapStatus.PinInvalid)
            {
                // FIDO authenticators regenerate the public key used for the auth protocol. We need to
                // re-initialize everything so we can obtain the new shared secret.
                AuthProtocol.Initialize();
                (retriesRemaining, rebootRequired) = Connection.SendCommand(new GetPinRetriesCommand()).GetData();

                return false; // PIN is invalid
            }

            throw new Fido2Exception(response.StatusMessage);
        }

        /// <summary>
        /// Performs a User Verification (UV) check on the YubiKey using the onboard biometric sensor. This method is
        /// only supported on YubiKey Bio Series devices. Uses the KeyCollector for touch prompting.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A YubiKey is manufactured with no PIN and no biometric templates set. A PIN must be set before a user
        /// can register fingerprints. After a PIN has been set, a user can enroll one or more fingers using their
        /// platform or operating system's built in registration mechanism.
        /// </para>
        /// <para>
        /// Once both a PIN has been set and a fingerprint has been registered, a user can perform verification. This
        /// method initiates the biometric (or user verification) process. If the user cannot match a valid finger within
        /// the allowed number of retries, it is best practice to fall back to PIN verification.
        /// </para>
        /// <para>
        /// Unlike other applications in this SDK (such as PIV and OATH), the SDK will not automatically verify PIN or
        /// UV using the KeyCollector in methods like <see cref="MakeCredential"/> due to FIDO2's complex user
        /// verification process. Your application must call this method explicitly before attempting to perform a FIDO2
        /// operation that requires verification.
        /// </para>
        /// <para>
        /// If the YubiKey was unable to verify a registered fingerprint, the SDK will automatically retry. The key
        /// collector will be called again to notify your app that touch is required. Each time the key collector is
        /// called, the <c>IsRetry</c> member will be set to <c>true</c> and the <c>RetryCount</c> will be updated to
        /// reflect the number of retries left before the YubiKey blocks further UV attempts. To cancel UV collection
        /// operations, simply return <c>false</c> in the handler for the key collector. When the retries have been
        /// exhausted, a `SecurityException` will be thrown. This, along with user cancellation, are indicators that
        /// your application should switch to verification with PIN.
        /// </para>
        /// </remarks>
        /// <param name="permissions">
        /// The set of operations that this auth token should be permitted to do.
        /// </param>
        /// <param name="relyingPartyId">
        /// Some <paramref name="permissions"/> require the qualification of a relying party ID. This parameter should
        /// only be specified when a permission requires it, otherwise it should be left null. See
        /// <see cref="PinUvAuthTokenPermissions"/> for more details on which permissions require the RP ID and for which
        /// it is optional.
        /// </param>
        /// <exception cref="OperationCanceledException">
        /// The user cancelled UV collection. This happens when the application returns <c>false</c>
        /// in the <c>KeyCollector</c>.
        /// </exception>
        public void VerifyUv(PinUvAuthTokenPermissions permissions, string? relyingPartyId = null)
        {
            if (TryVerifyUv(permissions, relyingPartyId))
            {
                return;
            }

            throw new OperationCanceledException(ExceptionMessages.FingerprintCollectionCancelled);
        }

        /// <summary>
        /// Tries to Perform a User Verification (UV) check on the YubiKey using
        /// the onboard biometric sensor. This method is only supported on
        /// YubiKey Bio Series devices. The permissions argument must be
        /// something other than <c>None</c>.
        /// </summary>
        /// <remarks>
        /// This method will call the KeyCollector to prompt the user to provide
        /// the fingerprint. If there is no KeyCollector, this method will throw
        /// an exception.
        /// <para>
        /// When verifying using Uv, the caller must provide a valid permission.
        /// If the input permissions arg is <c>None</c>, this method will throw
        /// an exception.
        /// </para>
        /// <para>
        /// A YubiKey is manufactured with no PIN and no biometric templates set. A PIN must be set before a user
        /// can register fingerprints. After a PIN has been set, a user can enroll one or more fingers using their
        /// platform or operating system's built in registration mechanism.
        /// </para>
        /// <para>
        /// Once both a PIN has been set and a fingerprint has been registered, a user can perform verification. This
        /// method initiates the biometric (or user verification) process. If the user cannot match a valid finger within
        /// the allowed number of retries, it is best practice to fall back to PIN verification.
        /// </para>
        /// <para>
        /// If the YubiKey was unable to verify a registered fingerprint, the SDK will automatically retry. The key
        /// collector will be called again to notify your app that touch is required. Each time the key collector is
        /// called, the <c>IsRetry</c> member will be set to <c>true</c> and the <c>RetryCount</c> will be updated to
        /// reflect the number of retries left before the YubiKey blocks further UV attempts. To cancel UV collection
        /// operations, call the <see cref="KeyEntryData.SignalUserCancel"/> delegate. When the retries have been
        /// exhausted, a <c>SecurityException</c> will be thrown. This, along with user cancellation, are indicators
        /// that your application should switch to verification with PIN.
        /// </para>
        /// <para>
        /// If the user cancels the operation, this method will return
        /// <c>false</c>. If the YubiKey times out, this method will throw a
        /// <c>TimeoutException</c>.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A boolean, <c>true</c> if the verification succeeds, <c>false</c> if
        /// the user cancels.
        /// </returns>
        /// <param name="permissions">
        /// The set of operations that this auth token should be permitted to do.
        /// This parameter cannot be <c>None</c> for UvVerification.
        /// </param>
        /// <param name="relyingPartyId">
        /// Some <paramref name="permissions"/> require the qualification of a relying party ID. This parameter should
        /// only be specified when a permission requires it, otherwise it should be left null. See
        /// <see cref="PinUvAuthTokenPermissions"/> for more details on which permissions require the RP ID and for which
        /// it is optional.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey does not support onboard user-verification, or else it
        /// does support it but there are no fingerprints enrolled.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The YubiKey has blocked fingerprint verification because of too many
        /// "bad" readings.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// The YubiKey timed out waiting for the user to supply a fingerprint.
        /// </exception>
        /// <exception cref="Fido2Exception">
        /// The permissions arg was <c>None</c> or the YubiKey was not able to
        /// complete the process for some reason described in the exception's
        /// message.
        /// </exception>
        public bool TryVerifyUv(PinUvAuthTokenPermissions permissions, string? relyingPartyId = null)
        {
            CtapStatus status = DoVerifyUv(permissions, relyingPartyId, out string statusMessage);

            switch(status)
            {
                case CtapStatus.Ok:
                    return true;

                case CtapStatus.KeepAliveCancel:
                    return false;

                case CtapStatus.UnsupportedOption:
                    throw new InvalidOperationException(ExceptionMessages.Fido2UvNotSupported);

                case CtapStatus.UvInvalid:
                case CtapStatus.LimitExceeded:
                    throw new SecurityException(ExceptionMessages.Fido2NoMoreRetries);

                case CtapStatus.OperationDenied:
                case CtapStatus.ActionTimeout:
                case CtapStatus.UserActionTimeout:
                    throw new TimeoutException(ExceptionMessages.Fido2TouchTimeout);

                default:
                    throw new Fido2Exception(statusMessage);
            }
        }

        private CtapStatus DoVerifyUv(PinUvAuthTokenPermissions permissions, string? relyingPartyId, out string statusMessage)
        {
            if ((AuthenticatorInfo.GetOptionValue("pinUvAuthToken") != OptionValue.True)
                || (AuthenticatorInfo.GetOptionValue("uv") != OptionValue.True))
            {
                statusMessage = "";
                return CtapStatus.UnsupportedOption;
            }
            if (permissions == PinUvAuthTokenPermissions.None)
            {
                statusMessage = ExceptionMessages.Fido2PermsMissing;
                return CtapStatus.InvalidParameter;
            }

            Func<KeyEntryData, bool> keyCollector = EnsureKeyCollector();

            CtapStatus status;
            ObtainSharedSecret();
            var command = new GetPinUvAuthTokenUsingUvCommand(AuthProtocol, permissions, relyingPartyId);

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.VerifyFido2Uv
            };
            using var touchTask = new TouchFingerprintTask(
                keyCollector,
                keyEntryData,
                Connection,
                CtapConstants.CtapClientPinCmd);

            try
            {
                do
                {
                    GetPinUvAuthTokenResponse response = Connection.SendCommand(command);
                    status = touchTask.IsUserCanceled ? CtapStatus.KeepAliveCancel : response.CtapStatus;
                    statusMessage = response.StatusMessage;

                    if (status == CtapStatus.Ok)
                    {
                        AuthToken = response.GetData();
                        AuthTokenPermissions = permissions;
                        AuthTokenRelyingPartyId = relyingPartyId;
                    }
                    else if (status == CtapStatus.UvInvalid)
                    {
                        keyEntryData.IsRetry = true;
                        keyEntryData.RetriesRemaining = Connection.SendCommand(new GetUvRetriesCommand()).GetData();
                        if (keyEntryData.RetriesRemaining <= 0)
                        {
                            status = CtapStatus.LimitExceeded;
                        }
                    }
                } while(status == CtapStatus.UvInvalid);

                return status;
            }
            finally
            {
                keyEntryData.Request = KeyEntryRequest.Release;
                touchTask.SdkUpdate(keyEntryData);
            }
        }

        /// <summary>
        /// Overrides the default PIN/UV Auth protocol (which is determined by the YubiKey and SDK).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Call this method with an instance of a class that derives <see cref="PinUvAuthProtocolBase"/> - either
        /// <see cref="PinUvAuthProtocolOne"/> or <seealso cref="PinUvAuthProtocolTwo"/>. When called, this method
        /// will replace the existing reference stored as <see cref="AuthProtocol"/> with the one that was passed
        /// to this function. The SDK will automatically dispose of the previous protocol if it was the default
        /// protocol created by the SDK. Otherwise, it will not. The caller of this function owns the lifetime of any
        /// protocol passed into the session - the SDK will not dispose of any user-set auth protocol.
        /// </para>
        /// <para>
        /// The auth protocol is not remembered across sessions. That is - if you call this method on session A, and
        /// later create a session B, the session B will be using the default auth protocol. You would need to call this
        /// method on both session instances to override the default behavior.
        /// </para>
        /// </remarks>
        /// <param name="authProtocol">
        /// The PIN/UV auth protocol instance to use for this session class.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The value specified by <c>authProtocol</c> is null.
        /// </exception>
        public void SetAuthProtocol(PinUvAuthProtocolBase authProtocol)
        {
            if (authProtocol is null)
            {
                throw new ArgumentNullException(nameof(authProtocol));
            }

            if (_disposeAuthProtocol)
            {
                AuthProtocol.Dispose();
                _disposeAuthProtocol = false; // Dispose no longer needed as caller now owns AuthProtocol.
            }

            AuthProtocol = authProtocol;
        }

        private void VerifyPinLengthRequirements(ReadOnlyMemory<byte> newPin)
        {
            int minPinLengthInCodePoints = AuthenticatorInfo.MinimumPinLength ?? PinMinimumByteLength;

            // Assumption - newPIN is already normalized
            if (newPin.Length < minPinLengthInCodePoints)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.PinTooShort,
                        minPinLengthInCodePoints,
                        "bytes",
                        newPin.Length));
            }

            if (newPin.Length > PinMaximumByteLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.PinTooLong,
                        PinMaximumByteLength,
                        "bytes",
                        newPin.Length));
            }
        }

        private PinUvAuthProtocolBase GetPreferredPinProtocol()
        {
            PinUvAuthProtocol protocol = AuthenticatorInfo.PinUvAuthProtocols?[0] ?? PinUvAuthProtocol.ProtocolOne;

            return protocol switch
            {
                PinUvAuthProtocol.ProtocolOne => new PinUvAuthProtocolOne(),
                PinUvAuthProtocol.ProtocolTwo => new PinUvAuthProtocolTwo(),
                _ => throw new NotSupportedException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.PinProtocolNotSupported,
                        protocol))
            };
        }

        private CoseEcPublicKey GetPeerCoseKey()
        {
            GetKeyAgreementResponse keyAgreementResponse =
                Connection.SendCommand(new GetKeyAgreementCommand(AuthProtocol.Protocol));

            CoseEcPublicKey peerCoseKey = keyAgreementResponse.GetData();
            return peerCoseKey;
        }

        private void ObtainSharedSecret()
        {
            if (AuthProtocol.PlatformPublicKey is null)
            {
                AuthProtocol.Initialize();
                CoseEcPublicKey peerCoseKey = GetPeerCoseKey();
                AuthProtocol.Encapsulate(peerCoseKey);
            }
        }

        private Func<KeyEntryData, bool> EnsureKeyCollector()
        {
            if (KeyCollector is null)
            {
                throw new InvalidOperationException(ExceptionMessages.MissingKeyCollector);
            }

            return KeyCollector;
        }
    }
}
