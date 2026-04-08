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

using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Fido2.IntegrationTests.TestExtensions;

/// <summary>
///     Extensions that help integration tests acquire <see cref="FidoSession" />
///     instances while ensuring connections are disposed correctly.
/// </summary>
/// <remarks>
///     <para>
///     The <c>resetBeforeUse</c> parameter (default: <c>true</c>) resets the FIDO2 application
///     before each test to prevent PIN state accumulation across test runs. Without this,
///     tests that call <c>SetPinAsync</c> fail on subsequent runs because a PIN from a
///     previous run remains on the key.
///     </para>
///     <para>
///     CTAP2 reset requires user touch within ~5 seconds of device power-up. If the reset
///     cannot be performed (e.g., <see cref="CtapStatus.NotAllowed" />,
///     <see cref="CtapStatus.UserActionTimeout" />, or <see cref="CtapStatus.ActionTimeout" />),
///     the error is caught and the test proceeds without reset.
///     </para>
/// </remarks>
public static class FidoTestStateExtensions
{
    /// <summary>
    ///     Attempts to reset the FIDO2 application on the device.
    /// </summary>
    /// <remarks>
    ///     CTAP2 reset requires user touch and must be attempted within ~5 seconds of
    ///     device insertion. If the reset fails due to these constraints, the error is
    ///     silently swallowed so that tests can proceed.
    /// </remarks>
    private static async Task TryResetFidoAsync(
        YubiKeyTestState state,
        ProtocolConfiguration? configuration,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var resetSession = await state.Device
                .CreateFidoSessionAsync(configuration: configuration, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            await resetSession.ResetAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (CtapException ex) when (
            ex.Status is CtapStatus.NotAllowed
                or CtapStatus.UserActionTimeout
                or CtapStatus.ActionTimeout
                or CtapStatus.OperationDenied)
        {
            // CTAP2 reset requires touch within ~5s of device power-up.
            // In automated test environments this constraint is rarely met,
            // so we silently continue rather than failing the test.
        }
    }

    extension(YubiKeyTestState state)
    {
        /// <summary>
        ///     Executes an action with a FIDO2 session.
        ///     Automatically handles connection and session lifecycle.
        /// </summary>
        /// <param name="action">The async action to execute with the session.</param>
        /// <param name="resetBeforeUse">
        ///     When <c>true</c> (the default), attempts to reset the FIDO2 application
        ///     before creating the test session. This clears any PIN or credential state
        ///     left over from a previous test run.
        /// </param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task WithFidoSessionAsync(
            Func<FidoSession, Task> action,
            bool resetBeforeUse = true,
            ProtocolConfiguration? configuration = null,
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default)
        {
            if (resetBeforeUse)
            {
                await TryResetFidoAsync(state, configuration, cancellationToken)
                    .ConfigureAwait(false);
            }

            await using var session = await state.Device
                .CreateFidoSessionAsync(scpKeyParams, configuration, cancellationToken)
                .ConfigureAwait(false);

            await action(session).ConfigureAwait(false);
        }

        /// <summary>
        ///     Executes an action with a FIDO2 session and returns the authenticator info.
        ///     Automatically handles connection and session lifecycle.
        /// </summary>
        /// <param name="action">The async action to execute with the session and info.</param>
        /// <param name="resetBeforeUse">
        ///     When <c>true</c> (the default), attempts to reset the FIDO2 application
        ///     before creating the test session.
        /// </param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task WithFidoSessionAsync(
            Func<FidoSession, AuthenticatorInfo, Task> action,
            bool resetBeforeUse = true,
            ProtocolConfiguration? configuration = null,
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default)
        {
            if (resetBeforeUse)
            {
                await TryResetFidoAsync(state, configuration, cancellationToken)
                    .ConfigureAwait(false);
            }

            await using var session = await state.Device
                .CreateFidoSessionAsync(scpKeyParams, configuration, cancellationToken)
                .ConfigureAwait(false);

            var info = await session.GetInfoAsync(cancellationToken).ConfigureAwait(false);
            await action(session, info).ConfigureAwait(false);
        }
    }
}
