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

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.PlatformInterop.Desktop.SCard;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
///     Connection-selection helpers for <see cref="IYubiKey" />.
/// </summary>
public static class YubiKeyConnectionExtensions
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger(nameof(YubiKeyConnectionExtensions));

    /// <summary>
    ///     Returns the first connection in <paramref name="preferenceOrder" /> that this device supports, or
    ///     <see cref="ConnectionType.Unknown" /> when it supports none of them.
    /// </summary>
    /// <remarks>
    ///     This is a policy-free mechanism: the caller supplies the preference order. Application/module
    ///     extension methods use it to pick a concrete transport on a physical (possibly multi-connection)
    ///     device instead of the ambiguity-throwing parameterless <see cref="IYubiKey.ConnectAsync(System.Threading.CancellationToken)" />.
    /// </remarks>
    public static ConnectionType ResolvePreferredConnection(
        this IYubiKey yubiKey,
        params ConnectionType[] preferenceOrder)
    {
        ArgumentNullException.ThrowIfNull(yubiKey);
        ArgumentNullException.ThrowIfNull(preferenceOrder);

        foreach (var candidate in preferenceOrder)
        {
            // Only concrete, openable connections are valid results; ignore Hid/All/Unknown candidates so
            // the resolver never returns a non-openable group flag.
            if (candidate is ConnectionType.SmartCard or ConnectionType.HidFido or ConnectionType.HidOtp
                && yubiKey.SupportsConnection(candidate))
                return candidate;
        }

        return ConnectionType.Unknown;
    }

    /// <summary>
    ///     Resolves the concrete transport a multi-transport application session should open, applying the
    ///     app-specific smart default and an optional explicit caller override.
    /// </summary>
    /// <param name="yubiKey">The physical device.</param>
    /// <param name="preferredConnection">
    ///     The explicit caller override, or <see langword="null" /> to use the application's documented default
    ///     order. When non-null it must be exactly one concrete transport
    ///     (<see cref="ConnectionType.SmartCard" />, <see cref="ConnectionType.HidFido" />, or
    ///     <see cref="ConnectionType.HidOtp" />) that is valid for this session (present in
    ///     <paramref name="defaultOrder" />) and supported by the device.
    /// </param>
    /// <param name="sessionName">The application/session name, used only for diagnostic messages.</param>
    /// <param name="defaultOrder">
    ///     The application's ordered default candidate list. It also defines the set of transports that are
    ///     valid for this session (used to validate an explicit override). Kept explicit at the call site so a
    ///     later held-transport fallback can iterate the remaining candidates without reshaping callers.
    /// </param>
    /// <returns>
    ///     The ordered, non-empty list of concrete transports to attempt, most-preferred first. For an
    ///     explicit override this is a single element (an override never falls back). For the default path it
    ///     is the device-supported subset of <paramref name="defaultOrder" />, in order. Callers open the first
    ///     element today; the full ordered list is returned so a later held-transport fallback (Phase 38.5) can
    ///     iterate the remaining candidates without reshaping callers.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     <paramref name="preferredConnection" /> is not exactly one concrete transport (e.g. a group flag,
    ///     a combined value, or <see cref="ConnectionType.Unknown" />), or it is a concrete transport that is
    ///     not valid for this session.
    /// </exception>
    /// <exception cref="NotSupportedException">
    ///     An override that is valid for the session is not exposed by the device, or — for the default path —
    ///     the device exposes none of the session's candidate transports.
    /// </exception>
    public static IReadOnlyList<ConnectionType> ResolveSessionTransports(
        this IYubiKey yubiKey,
        ConnectionType? preferredConnection,
        string sessionName,
        params ConnectionType[] defaultOrder)
    {
        ArgumentNullException.ThrowIfNull(yubiKey);
        ArgumentNullException.ThrowIfNull(defaultOrder);

        if (preferredConnection is { } requested)
        {
            // 1) Must be exactly one concrete transport (not Unknown/Hid/All or a combined flag).
            if (requested is not (ConnectionType.SmartCard or ConnectionType.HidFido or ConnectionType.HidOtp))
                throw new ArgumentException(
                    $"The requested connection '{requested}' is not a single concrete transport. Specify exactly " +
                    $"one of {ConnectionType.SmartCard}, {ConnectionType.HidFido}, or {ConnectionType.HidOtp}.",
                    nameof(preferredConnection));

            // 2) Must be a transport this session can actually use (programming error, even if device-supported).
            if (Array.IndexOf(defaultOrder, requested) < 0)
                throw new ArgumentException(
                    $"A {sessionName} session cannot use the {requested} transport. Valid transports for this " +
                    $"session are: {string.Join(", ", defaultOrder)}.",
                    nameof(preferredConnection));

            // 3) Must be exposed by this device (a capability question, not a programming error).
            if (!yubiKey.SupportsConnection(requested))
                throw new NotSupportedException(
                    $"This YubiKey does not expose the requested {requested} connection for a {sessionName} " +
                    $"session (available: {yubiKey.AvailableConnections}).");

            // An explicit override never falls back: exactly one candidate.
            return [requested];
        }

        // Default path: the device-supported subset of the ordered candidate list, preference order preserved.
        var candidates = new List<ConnectionType>(defaultOrder.Length);
        foreach (var candidate in defaultOrder)
        {
            if (candidate is ConnectionType.SmartCard or ConnectionType.HidFido or ConnectionType.HidOtp
                && yubiKey.SupportsConnection(candidate))
                candidates.Add(candidate);
        }

        if (candidates.Count == 0)
            throw new NotSupportedException(
                $"This YubiKey exposes no connection usable for a {sessionName} session " +
                $"(available: {yubiKey.AvailableConnections}).");

        return candidates;
    }

    /// <summary>
    ///     Opens the first transport in <paramref name="candidates" /> that connects, falling back to the next
    ///     candidate only when a <see cref="ConnectionType.SmartCard" /> connect fails because another process
    ///     is holding the card (PC/SC <c>SCARD_E_SHARING_VIOLATION</c> / <c>SCARD_E_SERVER_TOO_BUSY</c>).
    /// </summary>
    /// <remarks>
    ///     This is the connect half of the resolve→connect seam: callers pass the ordered, validated candidate
    ///     list produced by <see cref="ResolveSessionTransports" /> and receive an opened connection. The
    ///     "default-path-only fallback" and "override-never-falls-back" guarantees are properties of the applet
    ///     entry points (which pass a single-element list for an explicit override, so the loop rethrows on the
    ///     first failure); this helper simply follows the list it is given. Held-transport fallback is gated to
    ///     the SmartCard transport: a held-coded error on any other transport propagates unchanged (a held HID
    ///     transport is out of scope). Any non-held error, and <see cref="OperationCanceledException" />,
    ///     propagates immediately. The helper does not re-validate device capability; a transport the device
    ///     does not expose surfaces its own connect error.
    /// </remarks>
    /// <param name="yubiKey">The physical device.</param>
    /// <param name="candidates">
    ///     The ordered, non-empty list of concrete transports to attempt, most-preferred first (typically the
    ///     output of <see cref="ResolveSessionTransports" />). Every element must be a single concrete transport
    ///     (<see cref="ConnectionType.SmartCard" />, <see cref="ConnectionType.HidFido" />, or
    ///     <see cref="ConnectionType.HidOtp" />) and no transport may appear more than once.
    /// </param>
    /// <param name="sessionName">The application/session name, used only for diagnostic logging.</param>
    /// <param name="cancellationToken">A token to cancel the operation. Checked before each attempt.</param>
    /// <returns>The opened <see cref="IConnection" />; the caller owns its disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="yubiKey" /> or <paramref name="candidates" /> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="candidates" /> is empty, contains a non-concrete transport, or contains a duplicate.
    /// </exception>
    public static async Task<IConnection> ConnectSessionTransportAsync(
        this IYubiKey yubiKey,
        IReadOnlyList<ConnectionType> candidates,
        string sessionName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(yubiKey);
        ArgumentNullException.ThrowIfNull(candidates);

        if (candidates.Count == 0)
            throw new ArgumentException(
                "At least one candidate transport is required.", nameof(candidates));

        // Validate every element is a single concrete transport and that none repeats: the helper attempts
        // each transport at most once, so a duplicate would be a same-transport retry, which is not its job.
        var seen = ConnectionType.Unknown;
        foreach (var candidate in candidates)
        {
            if (candidate is not (ConnectionType.SmartCard or ConnectionType.HidFido or ConnectionType.HidOtp))
                throw new ArgumentException(
                    $"Candidate '{candidate}' is not a single concrete transport. Each candidate must be one " +
                    $"of {ConnectionType.SmartCard}, {ConnectionType.HidFido}, or {ConnectionType.HidOtp}.",
                    nameof(candidates));

            if ((seen & candidate) != 0)
                throw new ArgumentException(
                    $"Candidate transport '{candidate}' appears more than once; each transport is attempted " +
                    "at most once.",
                    nameof(candidates));

            seen |= candidate;
        }

        for (var i = 0; i < candidates.Count; i++)
        {
            // Check before every attempt (including the first) and, by re-entering the loop, again after a
            // fallback: a token canceled between attempts must stop us rather than open a fallback transport.
            cancellationToken.ThrowIfCancellationRequested();

            var transport = candidates[i];
            try
            {
                IConnection connection = transport switch
                {
                    ConnectionType.SmartCard => await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken)
                        .ConfigureAwait(false),
                    ConnectionType.HidFido => await yubiKey.ConnectAsync<IFidoHidConnection>(cancellationToken)
                        .ConfigureAwait(false),
                    _ => await yubiKey.ConnectAsync<IOtpHidConnection>(cancellationToken)
                        .ConfigureAwait(false)
                };

                Logger.LogDebug(
                    "Opened {Transport} connection for a {SessionName} session.", transport, sessionName);
                return connection;
            }
            // Fall back ONLY when a held SmartCard transport failed AND a further candidate remains. The
            // SmartCard gate keeps held-HID out of scope; the index gate makes the last candidate's held
            // error (and an override's single element) propagate unchanged. Non-held errors and cancellation
            // never match this filter, so they propagate immediately.
            catch (Exception ex) when (
                transport == ConnectionType.SmartCard
                && IsHeldTransportError(ex)
                && i < candidates.Count - 1)
            {
                Logger.LogDebug(
                    ex,
                    "The {Transport} transport for a {SessionName} session is held by another process; " +
                    "falling back to the next supported transport.",
                    transport,
                    sessionName);
            }
        }

        // Unreachable: the loop returns on success, and the catch filter cannot swallow the final candidate
        // (it requires a further candidate), so the final candidate's failure always propagates from the try.
        throw new NotSupportedException(
            $"This YubiKey exposes no connection usable for a {sessionName} session.");
    }

    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="exception" /> indicates the smart card is held
    ///     by another process — a PC/SC <c>SCARD_E_SHARING_VIOLATION</c> or <c>SCARD_E_SERVER_TOO_BUSY</c>
    ///     carried by an <see cref="SCardException" />. <see cref="SCardException" /> stores the PC/SC status in
    ///     <see cref="System.Exception.HResult" /> (as <c>(int)errorCode</c>), so the round-trip compares
    ///     <c>(uint)HResult</c>. Detection is intentionally narrow: no other exception type or status code
    ///     counts as held.
    /// </summary>
    private static bool IsHeldTransportError(Exception exception) =>
        exception is SCardException scardException
        && (uint)scardException.HResult is ErrorCode.SCARD_E_SHARING_VIOLATION
            or ErrorCode.SCARD_E_SERVER_TOO_BUSY;
}