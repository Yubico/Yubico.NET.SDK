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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Connection-selection helpers for <see cref="IYubiKey" />.
/// </summary>
public static class YubiKeyConnectionExtensions
{
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
    public static ConnectionType ResolveSessionTransport(
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

            return requested;
        }

        var resolved = yubiKey.ResolvePreferredConnection(defaultOrder);
        if (resolved != ConnectionType.Unknown)
            return resolved;

        throw new NotSupportedException(
            $"This YubiKey exposes no connection usable for a {sessionName} session " +
            $"(available: {yubiKey.AvailableConnections}).");
    }

    /// <summary>
    ///     Opens exactly one selected transport and creates a session-shaped result over it.
    /// </summary>
    /// <remarks>
    ///     No fallback is attempted. Connection and session-creation failures propagate unchanged. If opening
    ///     succeeds but <paramref name="createAsync" /> fails, the connection is disposed before the failure is
    ///     rethrown.
    /// </remarks>
    /// <param name="yubiKey">The physical device.</param>
    /// <param name="transport">The single concrete transport selected for the session.</param>
    /// <param name="createAsync">Creates the result over the opened connection.</param>
    /// <param name="cancellationToken">A token to cancel connection opening or session creation.</param>
    public static async Task<TResult> CreateSessionOverTransportAsync<TResult>(
        this IYubiKey yubiKey,
        ConnectionType transport,
        Func<IConnection, CancellationToken, Task<TResult>> createAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(yubiKey);
        ArgumentNullException.ThrowIfNull(createAsync);
        if (transport is not (ConnectionType.SmartCard or ConnectionType.HidFido or ConnectionType.HidOtp))
            throw new ArgumentException(
                $"Connection '{transport}' is not a single concrete transport.", nameof(transport));
        cancellationToken.ThrowIfCancellationRequested();

        var connection = await yubiKey.OpenSessionConnectionAsync(transport, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            return await createAsync(connection, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<IConnection> OpenSessionConnectionAsync(
        this IYubiKey yubiKey,
        ConnectionType transport,
        CancellationToken cancellationToken) => transport switch
        {
            ConnectionType.SmartCard => await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken)
                .ConfigureAwait(false),
            ConnectionType.HidFido => await yubiKey.ConnectAsync<IFidoHidConnection>(cancellationToken)
                .ConfigureAwait(false),
            ConnectionType.HidOtp => await yubiKey.ConnectAsync<IOtpHidConnection>(cancellationToken)
                .ConfigureAwait(false),
            _ => throw new InvalidOperationException("Concrete transport validation did not select a connection type.")
        };

}