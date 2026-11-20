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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;

/// <summary>
///     Extensions that help integration tests acquire <see cref="Ctap2Session" />
///     instances while ensuring connections are disposed correctly.
/// </summary>
/// <remarks>
///     <para>
///     After session creation, each helper call defaults to <see cref="NormalizePinAsync" />
///     before invoking the action. Normalization can set a PIN, change it twice for forcePinChange
///     recovery or request a token to verify it. These paths require approved PIN work and known
///     state on a dedicated device, even when the action only reads GetInfo. Pass normalizePin:false
///     only when the test deliberately needs no normalization; touch filtering does not disable it.
///     </para>
///     <para>
///     A matching PIN still triggers a token exchange. A mismatch can consume a retry before a skip.
///     A skip or cancellation does not roll back PIN changes. Force-change recovery can leave the
///     temporary reversed PIN if its second change fails; that temporary buffer is cleared only on
///     success. The verification token is discarded without clearing. Reset advice in diagnostic
///     strings is not a coordinated product reset procedure, and temporary PinAuthBlocked differs
///     from exhausted PinBlocked.
///     </para>
/// </remarks>
public static class Ctap2TestStateExtensions
{
    /// <summary>
    ///     The known test PIN used across all CTAP2 integration tests: <c>"11234567"</c>.
    /// </summary>
    /// <remarks>
    ///     This known 8-digit test value is not a universal product/firmware complexity guarantee.
    ///     The device's configured PIN policy still applies; it is not production secret input.
    ///     UTF-8 bytes: <c>[0x31, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37]</c>.
    /// </remarks>
    public static readonly byte[] KnownTestPin = [0x31, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37];

    /// <summary>
    ///     The known test PIN as a string: <c>"11234567"</c>.
    /// </summary>
    public const string KnownTestPinString = "11234567";

    /// <summary>
    ///     Attempts to set, recover or verify the known test PIN before the action.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>If no PIN is set, calls <c>SetPinAsync</c> with the known PIN.</item>
    ///         <item>If a PIN is set, verifies it matches the known PIN by requesting a PIN token.</item>
    ///         <item>If the PIN is wrong, skips the test with a diagnostic message.</item>
    ///         <item>If forcePinChange is reported, attempts known→temporary→known PIN changes.</item>
    ///         <item>If PIN authentication is blocked, skips with a diagnostic; recovery depends on the status.</item>
    ///     </list>
    /// </remarks>
    private static async Task NormalizePinAsync(
        Ctap2Session session,
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

        // If a prior test left forcePinChange set, clear it via PIN change.
        // ChangePinAsync uses direct PIN hash encryption (not PIN tokens), so it
        // works even when forcePinChange blocks token requests.
        // A reversed temporary value avoids a same-PIN change where device policy rejects it.
        // This is not transactional: a failed second change can leave the temporary PIN configured.
        if (info.ForcePinChange == true)
        {
            try
            {
                byte[] tempPin = KnownTestPin.Reverse().ToArray();
                await clientPin.ChangePinAsync(KnownTestPin, tempPin, cancellationToken)
                    .ConfigureAwait(false);
                await clientPin.ChangePinAsync(tempPin, KnownTestPin, cancellationToken)
                    .ConfigureAwait(false);
                // This success-path clearing does not cover failed changes.
                CryptographicOperations.ZeroMemory(tempPin);
            }
            catch (CtapException ex) when (ex.Status is CtapStatus.PinInvalid
                                               or CtapStatus.PinPolicyViolation)
            {
                Skip.If(true,
                    $"CTAP2 forcePinChange recovery failed ({ex.Status}). PIN differs from " +
                    "known test PIN '11234567' or policy rejects it. Reset: ykman fido reset");
            }
        }

        // A token request verifies the PIN even when it already matches; a mismatch can consume a retry.
        // The returned secret is currently discarded without clearing.
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

    private static string GetWindowsHidAccessDeniedTestMessage(ConnectionType? preferredConnection) =>
        $"Windows denied access while opening a CTAP2 session over {preferredConnection ?? ConnectionType.HidFido}. " +
        "If this test targets HID FIDO, run the test host elevated as Administrator or close software that may hold the HID interface exclusively.";

    extension(YubiKeyTestState state)
    {
        /// <summary>
        ///     Executes an action with a CTAP2 session.
        ///     Automatically handles connection and session lifecycle.
        /// </summary>
        /// <param name="action">The async action to execute with the session.</param>
        /// <param name="normalizePin">
        ///     When true (the default), attempts known-PIN setup/recovery/token verification after
        ///     creation and before the action. Requires approved PIN work, including for GetInfo tests.
        /// </param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
        /// <param name="preferredConnection">
        ///     Optional explicit CTAP2 transport override. Left <c>null</c> here so the shared default path
        ///     keeps exercising the CTAP2 default order (HID FIDO first); SmartCard-tagged tests pass
        ///     <see cref="ConnectionType.SmartCard" /> to pin the SmartCard transport (so they do not silently
        ///     pass over HID FIDO on a merged composite device).
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task WithCtap2SessionAsync(
            Func<Ctap2Session, Task> action,
            bool normalizePin = true,
            ProtocolConfiguration? configuration = null,
            ScpKeyParameters? scpKeyParams = null,
            ConnectionType? preferredConnection = null,
            CancellationToken cancellationToken = default)
        {
            Ctap2Session session;
            try
            {
                session = await state.Device
                    .CreateCtap2SessionAsync(
                        new SessionCreationOptions
                        {
                            ScpKeyParameters = scpKeyParams,
                            ProtocolConfiguration = configuration,
                            PreferredConnectionType = preferredConnection
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException ex) when (OperatingSystem.IsWindows())
            {
                throw new UnauthorizedAccessException(GetWindowsHidAccessDeniedTestMessage(preferredConnection), ex);
            }

            await using (session.ConfigureAwait(false))
            {
                if (normalizePin)
                {
                    await NormalizePinAsync(session, cancellationToken)
                        .ConfigureAwait(false);
                }

                await action(session).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Executes an action with a CTAP2 session and a fresh authenticator-info snapshot.
        ///     Automatically handles connection and session lifecycle.
        /// </summary>
        /// <param name="action">The async action to execute with the session and info.</param>
        /// <param name="normalizePin">
        ///     When true (the default), attempts known-PIN setup/recovery/token verification after
        ///     creation and before the fresh info read/action. Requires approved PIN work.
        /// </param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
        /// <param name="preferredConnection">
        ///     Optional explicit CTAP2 transport override. Left <c>null</c> here so the shared default path
        ///     keeps exercising the CTAP2 default order (HID FIDO first); SmartCard-tagged tests pass
        ///     <see cref="ConnectionType.SmartCard" /> to pin the SmartCard transport (so they do not silently
        ///     pass over HID FIDO on a merged composite device).
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task WithCtap2SessionAsync(
            Func<Ctap2Session, AuthenticatorInfo, Task> action,
            bool normalizePin = true,
            ProtocolConfiguration? configuration = null,
            ScpKeyParameters? scpKeyParams = null,
            ConnectionType? preferredConnection = null,
            CancellationToken cancellationToken = default)
        {
            Ctap2Session session;
            try
            {
                session = await state.Device
                    .CreateCtap2SessionAsync(
                        new SessionCreationOptions
                        {
                            ScpKeyParameters = scpKeyParams,
                            ProtocolConfiguration = configuration,
                            PreferredConnectionType = preferredConnection
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException ex) when (OperatingSystem.IsWindows())
            {
                throw new UnauthorizedAccessException(GetWindowsHidAccessDeniedTestMessage(preferredConnection), ex);
            }

            await using (session.ConfigureAwait(false))
            {
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
}