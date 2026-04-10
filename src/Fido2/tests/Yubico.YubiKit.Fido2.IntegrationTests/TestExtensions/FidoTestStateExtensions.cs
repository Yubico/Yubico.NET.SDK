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
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Fido2.IntegrationTests.TestExtensions;

/// <summary>
///     Extensions that help integration tests acquire <see cref="FidoSession" />
///     instances while ensuring connections are disposed correctly.
/// </summary>
/// <remarks>
///     <para>
///     Before each test session, <see cref="NormalizePinAsync" /> ensures the
///     authenticator has the known test PIN (<c>"11234567"</c>) configured.
///     This avoids the need for a CTAP2 reset (which requires a manual
///     unplug/replug/touch ceremony that cannot be automated).
///     </para>
///     <para>
///     If the PIN is already set to the known value, normalization is a no-op.
///     If no PIN is set, <c>SetPinAsync</c> is called. If the PIN differs or
///     is blocked, the test is skipped with a diagnostic message.
///     </para>
/// </remarks>
public static class FidoTestStateExtensions
{
    /// <summary>
    ///     The known test PIN used across all FIDO2 integration tests: <c>"11234567"</c>.
    /// </summary>
    /// <remarks>
    ///     This 8-digit PIN satisfies the YubiKey 5.7+ PIN complexity requirements.
    ///     UTF-8 bytes: <c>[0x31, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37]</c>.
    /// </remarks>
    public static readonly byte[] KnownTestPin = [0x31, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37];

    /// <summary>
    ///     The known test PIN as a string: <c>"11234567"</c>.
    /// </summary>
    public const string KnownTestPinString = "11234567";

    /// <summary>
    ///     Ensures the authenticator has the known test PIN configured.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>If no PIN is set, calls <c>SetPinAsync</c> with the known PIN.</item>
    ///         <item>If a PIN is set, verifies it matches the known PIN by requesting a PIN token.</item>
    ///         <item>If the PIN is wrong, skips the test with a diagnostic message.</item>
    ///         <item>If the PIN is blocked, skips the test with a manual reset instruction.</item>
    ///     </list>
    /// </remarks>
    private static async Task NormalizePinAsync(
        FidoSession session,
        CancellationToken cancellationToken)
    {
        var info = await session.GetInfoAsync(cancellationToken).ConfigureAwait(false);

        // Determine if a PIN is currently configured.
        // clientPin option: true = PIN is set, false = PIN not set, absent = not supported.
        var pinIsSet = info.Options.TryGetValue("clientPin", out var clientPinValue)
            && clientPinValue;

        // Select the best available PIN/UV auth protocol (prefer V2).
        var protocolVersion = info.PinUvAuthProtocols.Contains(2) ? 2 : 1;
        IPinUvAuthProtocol protocol = protocolVersion == 2
            ? new PinUvAuthProtocolV2()
            : new PinUvAuthProtocolV1();

        using var clientPin = new ClientPin(session, protocol);

        if (!pinIsSet)
        {
            // No PIN configured yet -- set the known test PIN.
            await clientPin.SetPinAsync(KnownTestPin, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        // PIN is configured -- verify it matches the known test PIN.
        try
        {
            _ = await clientPin.GetPinTokenAsync(KnownTestPin, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (CtapException ex) when (ex.Status is CtapStatus.PinInvalid)
        {
            Skip.If(true,
                "FIDO2 PIN differs from known test PIN '11234567'. " +
                "Set it manually: ykman fido access change-pin");
        }
        catch (CtapException ex) when (ex.Status is CtapStatus.PinBlocked or CtapStatus.PinAuthBlocked)
        {
            Skip.If(true,
                "FIDO2 PIN is blocked. Manual reset required: ykman fido access reset");
        }
    }

    extension(YubiKeyTestState state)
    {
        /// <summary>
        ///     Executes an action with a FIDO2 session.
        ///     Automatically handles connection and session lifecycle.
        /// </summary>
        /// <param name="action">The async action to execute with the session.</param>
        /// <param name="normalizePin">
        ///     When <c>true</c> (the default), ensures the authenticator has the known
        ///     test PIN (<c>"11234567"</c>) configured before running the test action.
        /// </param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task WithFidoSessionAsync(
            Func<FidoSession, Task> action,
            bool normalizePin = true,
            ProtocolConfiguration? configuration = null,
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default)
        {
            await using var session = await state.Device
                .CreateFidoSessionAsync(scpKeyParams, configuration, cancellationToken)
                .ConfigureAwait(false);

            if (normalizePin)
            {
                await NormalizePinAsync(session, cancellationToken)
                    .ConfigureAwait(false);
            }

            await action(session).ConfigureAwait(false);
        }

        /// <summary>
        ///     Executes an action with a FIDO2 session and returns the authenticator info.
        ///     Automatically handles connection and session lifecycle.
        /// </summary>
        /// <param name="action">The async action to execute with the session and info.</param>
        /// <param name="normalizePin">
        ///     When <c>true</c> (the default), ensures the authenticator has the known
        ///     test PIN (<c>"11234567"</c>) configured before running the test action.
        /// </param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task WithFidoSessionAsync(
            Func<FidoSession, AuthenticatorInfo, Task> action,
            bool normalizePin = true,
            ProtocolConfiguration? configuration = null,
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default)
        {
            await using var session = await state.Device
                .CreateFidoSessionAsync(scpKeyParams, configuration, cancellationToken)
                .ConfigureAwait(false);

            if (normalizePin)
            {
                await NormalizePinAsync(session, cancellationToken)
                    .ConfigureAwait(false);
            }

            var info = await session.GetInfoAsync(cancellationToken).ConfigureAwait(false);
            await action(session, info).ConfigureAwait(false);
        }
    }
}
