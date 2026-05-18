// Copyright 2026 Yubico AB
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
using Microsoft.Extensions.Logging;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2
{
    // CTAP 2.1 §6.9 authenticatorSelection (0x0B): User Presence (UP) for single or multi-YubiKey selection. YubiKey firmware 5.5.1+.
    public sealed partial class Fido2Session
    {
        /// <summary>
        /// Requests User Presence (UP) on this YubiKey so the user can select it for intended use
        /// (CTAP 2.1 §6.9 <c>authenticatorSelection</c>, command byte 0x0B). Requires YubiKey firmware 5.5.1 or later.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Per the CTAP specification, after a successful selection the platform should send cancel
        /// to other authenticators. This SDK does not manage multiple devices; callers orchestrate that.
        /// </para>
        /// <para>
        /// This method calls the <see cref="KeyCollector"/> with <see cref="KeyEntryRequest.TouchRequest"/>
        /// while waiting for touch, the same as <see cref="GetAssertions(GetAssertionParameters)"/>.
        /// </para>
        /// <para>
        /// Returns <c>false</c> if the YubiKey returns <see cref="Yubico.YubiKey.Fido2.CtapStatus.InvalidCommand"/> (command
        /// not implemented) or <see cref="Yubico.YubiKey.Fido2.CtapStatus.OperationDenied"/> (user declined). There is no SDK
        /// firmware gate; behavior depends on the YubiKey's firmware version (5.5.1+).
        /// </para>
        /// </remarks>
        /// <param name="response">The response from the YubiKey, including the CTAP status (see <see cref="Commands.Fido2Response.CtapStatus"/>).</param>
        /// <returns><c>true</c> if the operation completed with <see cref="Yubico.YubiKey.Fido2.CtapStatus.Ok"/>; otherwise <c>false</c> for unsupported or denied selection.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="KeyCollector"/> is not set.</exception>
        /// <exception cref="TimeoutException">The authenticator timed out waiting for user action.</exception>
        /// <exception cref="OperationCanceledException">The operation was canceled (e.g. keepalive cancel).</exception>
        /// <exception cref="Fido2Exception">Another CTAP error occurred.</exception>
        public bool TryAuthenticatorSelection(out AuthenticatorSelectionResponse response)
        {
            Logger.LogInformation("Authenticator selection.");

            var keyCollector = EnsureKeyCollector();
            var keyEntryData = new KeyEntryData
            {
                Request = KeyEntryRequest.TouchRequest,
            };

            using var touchTask = new TouchFingerprintTask(
                keyCollector,
                keyEntryData,
                Connection,
                CtapConstants.CtapAuthenticatorSelectionCmd);

            try
            {
                response = Connection.SendCommand(new AuthenticatorSelectionCommand());
                CtapStatus ctapStatus = touchTask.IsUserCanceled ? CtapStatus.KeepAliveCancel : response.CtapStatus;

                switch (ctapStatus)
                {
                    case CtapStatus.Ok:
                        return true;

                    case CtapStatus.InvalidCommand:
                    case CtapStatus.OperationDenied:
                        return false;

                    case CtapStatus.KeepAliveCancel:
                        throw new OperationCanceledException(ExceptionMessages.OperationCancelled);

                    case CtapStatus.ActionTimeout:
                    case CtapStatus.UserActionTimeout:
                        throw new TimeoutException(ExceptionMessages.Fido2TouchTimeout);

                    default:
                        throw new Fido2Exception(response.CtapStatus, response.StatusMessage);
                }
            }
            finally
            {
                keyEntryData.Clear();
                keyEntryData.Request = KeyEntryRequest.Release;
                touchTask.SdkUpdate(keyEntryData);
            }
        }
    }
}