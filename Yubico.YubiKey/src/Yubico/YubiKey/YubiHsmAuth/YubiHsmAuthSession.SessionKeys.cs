﻿// Copyright 2025 Yubico AB
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Yubico.YubiKey.YubiHsmAuth.Commands;

namespace Yubico.YubiKey.YubiHsmAuth
{
    // This portion of the YubiHSM Auth Session class contains operations
    // related to SCP03 session keys
    public partial class YubiHsmAuthSession
    {
        /// <summary>
        /// Calculate session keys from an AES-128 credential. These session
        /// keys are used to establish a secure session with a YubiHSM 2
        /// device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Some steps must be performed prior to calling this command. First,
        /// generate an 8-byte "host challenge" using a
        /// random or pseudorandom method. Next, send the host challenge to the
        /// YubiHSM 2 device using the
        /// <a href="https://developers.yubico.com/yubihsm-shell/API_Documentation/yubihsm_8h.html#a296b43eadb1151017ba8e9578b351c5e">yh_begin_create_session_ext method</a>
        /// of the <a href="https://developers.yubico.com/yubihsm-shell/">libyubihsm library</a>,
        /// where the YubiHSM 2 device responds with an 8-byte "HSM device challenge".
        /// Both of these challenges are then used to construct this command.
        /// </para>
        /// <para>
        /// There is a limit of 8 attempts to authenticate with the credential's
        /// password before the credential is deleted. Once the credential is
        /// deleted, it cannot be recovered. Supplying the correct password before the
        /// credential is deleted will reset the retry counter to 8.
        /// </para>
        /// <para>
        /// If the credential requires touch (see <see cref="Credential.TouchRequired"/>),
        /// then the user must also touch the YubiKey as part of the authentication
        /// procedure. A <see cref="TimeoutException"/> will be thrown if touch is
        /// not supplied in time.
        /// </para>
        /// <para>
        /// The secure session protocol is based on Secure Channel Protocol 3
        /// (SCP03). The session keys returned by the application are the
        /// Session Secure Channel Encryption Key (S-ENC),
        /// Secure Channel Message Authentication Code Key for Command (S-MAC),
        /// and Secure Channel Message Authentication Code Key for Response
        /// (S-RMAC). These session-specific keys are used to encrypt and
        /// authenticate commands and responses with a YubiHSM 2 device during
        /// a single session. The session keys are discarded afterwards.
        /// </para>
        /// </remarks>
        /// <returns>
        /// Session keys are used to establish an encrypted and authenticated
        /// session with a YubiHSM 2 device. The secure session is based on the
        /// Global Platform Secure Channel Protocol '03' (SCP03).
        /// </returns>
        /// <param name="credentialLabel">
        /// The label of the credential for calculating the session keys. The
        /// string must meet the same requirements as
        /// <see cref="Credential.Label"/>.
        /// </param>
        /// <param name="credentialPassword">
        /// The password of the credential for calculating the session keys.
        /// It must meet the same requirements as
        /// <see cref="CredentialWithSecrets.CredentialPassword"/>.
        /// </param>
        /// <param name="hostChallenge">
        /// The 8 byte challenge generated by the host.
        /// </param>
        /// <param name="hsmDeviceChallenge">
        /// The 8 byte challenge generated by the YubiHSM 2 device.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The credential could not be found.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The credential password was incorrect, or touch was required but
        /// not supplied.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// The operation timed out waiting for touch.
        /// </exception>
        public SessionKeys GetAes128SessionKeys(string credentialLabel,
                                                ReadOnlyMemory<byte> credentialPassword,
                                                ReadOnlyMemory<byte> hostChallenge,
                                                ReadOnlyMemory<byte> hsmDeviceChallenge)
        {
            var command = new GetAes128SessionKeysCommand(
                    credentialLabel,
                    credentialPassword,
                    hostChallenge,
                    hsmDeviceChallenge);

            var response = Connection.SendCommand(command);
            if (response.Status != ResponseStatus.Success)
            {
                if (response.Status == ResponseStatus.AuthenticationRequired)
                {
                    throw new SecurityException(string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiHsmAuthCredPasswordAuthFailed,
                        response.RetriesRemaining));
                }

                if (response.Status == ResponseStatus.RetryWithTouch)
                {
                    throw new TimeoutException(ExceptionMessages.YubiHsmAuthTouchTimeout);
                }

                throw new InvalidOperationException(response.StatusMessage);
            }

            return response.GetData();
        }

        /// <summary>
        /// Calculate session keys from an AES-128 credential, using the
        /// <see cref="KeyCollector"/> to retrieve the credential password
        /// and prompt for touch when required. These session
        /// keys are used to establish a secure session with a YubiHSM 2
        /// device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Some steps must be performed prior to calling this command. First,
        /// generate an 8-byte "host challenge" using a
        /// random or pseudorandom method. Next, send the host challenge to the
        /// YubiHSM 2 device using the
        /// <a href="https://developers.yubico.com/yubihsm-shell/API_Documentation/yubihsm_8h.html#a296b43eadb1151017ba8e9578b351c5e">yh_begin_create_session_ext method</a>
        /// of the <a href="https://developers.yubico.com/yubihsm-shell/">libyubihsm library</a>,
        /// where the YubiHSM 2 device responds with an 8-byte "HSM device challenge".
        /// Both of these challenges are then used to construct this command.
        /// </para>
        /// <para>
        /// There is a limit of 8 attempts to authenticate with the credential's
        /// password before the credential is deleted. Once the credential is
        /// deleted, it cannot be recovered. Supplying the correct password before the
        /// credential is deleted will reset the retry counter to 8.
        /// </para>
        /// <para>
        /// When the credential password is needed, the <see cref="KeyCollector"/>
        /// is called with <see cref="KeyEntryData.Request"/> set to
        /// <see cref="KeyEntryRequest.AuthenticateYubiHsmAuthCredentialPassword"/>.
        /// The <c>KeyCollector</c> gets the credential password from the user,
        /// stores it using <see cref="KeyEntryData.SubmitValue(ReadOnlySpan{byte})"/>,
        /// and returns <c>true</c>.
        /// </para>
        /// <para>
        /// Next, if the credential requires touch (see
        /// <see cref="Credential.TouchRequired"/>), the <see cref="KeyCollector"/>
        /// is called with <see cref="KeyEntryData.Request"/> set to
        /// <see cref="KeyEntryRequest.TouchRequest"/>. Typically, you will want
        /// to react to this request by alerting your user that they need to
        /// physically touch the YubiKey. Additionally, the return value will be
        /// ignored. That is, it is not possible to cancel the operation once
        /// this <c>TouchRequest</c> is sent. Ideally, you should not block this
        /// call. However, to ensure the proper function of the SDK, this request
        /// will be issued on a separate thread from the one that originated this
        /// call.
        /// </para>
        /// <para>
        /// If the user does not touch the YubiKey in time, a
        /// <see cref="TimeoutException"/> will be thrown. Failing to touch the
        /// YubiKey does not change the credential's retry count.
        /// </para>
        /// <para>
        /// The secure session protocol is based on Secure Channel Protocol 3
        /// (SCP03). The session keys returned by the application are the
        /// Session Secure Channel Encryption Key (S-ENC),
        /// Secure Channel Message Authentication Code Key for Command (S-MAC),
        /// and Secure Channel Message Authentication Code Key for Response
        /// (S-RMAC). These session-specific keys are used to encrypt and
        /// authenticate commands and responses with a YubiHSM 2 device during
        /// a single session. The session keys are discarded afterwards.
        /// </para>
        /// </remarks>
        /// <returns>
        /// <c>True</c>, when the management key has been changed successfully.
        /// <c>False</c> when the <c>KeyCollector</c> returns <c>false</c>
        /// (usually indicating user cancellation).
        /// </returns>
        /// <param name="credentialLabel">
        /// The label of the credential for calculating the session keys. The
        /// string must meet the same requirements as
        /// <see cref="Credential.Label"/>.
        /// </param>
        /// <param name="hostChallenge">
        /// The 8 byte challenge generated by the host.
        /// </param>
        /// <param name="hsmDeviceChallenge">
        /// The 8 byte challenge generated by the YubiHSM 2 device.
        /// </param>
        /// <param name="sessionKeys">
        /// Session keys are used to establish an encrypted and authenticated
        /// session with a YubiHSM 2 device. The secure session is based on the
        /// Global Platform Secure Channel Protocol '03' (SCP03).
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The credential could not be found.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// The operation timed out waiting for touch.
        /// </exception>
        public bool TryGetAes128SessionKeys(string credentialLabel,
                                            ReadOnlyMemory<byte> hostChallenge,
                                            ReadOnlyMemory<byte> hsmDeviceChallenge,
                                            [NotNullWhen(true)] out SessionKeys? sessionKeys)
        {
            sessionKeys = null;

            // Check if this credential requires touch
            bool touchRequired =
                ListCredentials()
                    .Single(c => c.Credential.Label == credentialLabel)
                    .Credential.TouchRequired;

            var keyCollectorFunc = GetKeyCollector();

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.AuthenticateYubiHsmAuthCredentialPassword,
            };

            try
            {
                while (keyCollectorFunc(keyEntryData))
                {
                    var command =
                        new GetAes128SessionKeysCommand(
                            credentialLabel,
                            keyEntryData.GetCurrentValue(),
                            hostChallenge,
                            hsmDeviceChallenge);

                    if (touchRequired)
                    {
                        // Touch is required for this credential, so spawn a
                        // new thread and send a touch request to the key collector

                        keyEntryData.Request = KeyEntryRequest.TouchRequest;
                        _ = Task.Run(() => keyCollectorFunc(keyEntryData));

                        // We ignore the return value, regardless. So no need to wait.
                    }

                    var response = Connection.SendCommand(command);
                    if (response.Status == ResponseStatus.Success)
                    {
                        sessionKeys = response.GetData();

                        return true;
                    }

                    // Handle failure cases
                    if (response.Status == ResponseStatus.AuthenticationRequired)
                    {
                        // Incorrect credential password - retry auth (if possible)

                        if (response.RetriesRemaining == 0)
                        {
                            throw new SecurityException(
                                string.Format(
                                    CultureInfo.CurrentCulture,
                                    ExceptionMessages.NoMoreRetriesRemaining));
                        }

                        keyEntryData.Request = KeyEntryRequest.AuthenticateYubiHsmAuthCredentialPassword;
                        keyEntryData.IsRetry = true;
                        keyEntryData.RetriesRemaining = response.RetriesRemaining;

                        continue;
                    }
                    else if (response.Status == ResponseStatus.RetryWithTouch)
                    {
                        // Touch was expected
                        throw new TimeoutException(ExceptionMessages.YubiHsmAuthTouchTimeout);
                    }
                    else
                    {
                        // Other error
                        throw new InvalidOperationException(response.StatusMessage);
                    }
                }
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = keyCollectorFunc(keyEntryData);
            }

            // User cancelled
            return false;
        }
    }
}
