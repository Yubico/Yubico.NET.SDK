// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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

namespace Yubico.YubiKit.Core.Credentials;

/// <summary>
///     Owns the request and resolution lifecycle for one SDK user-presence notification.
/// </summary>
/// <remarks>
///     Enabled instances have one logical owner, belong to one operation, and are not thread-safe. They must not be
///     shared between operations or called concurrently from independent execution flows. <see cref="None" /> is
///     the immutable, shared no-op instance used when no prompt or context was supplied. A successful request is
///     emitted at most once, and only that request can be resolved. A request callback exception propagates and
///     terminally disables the handle without emitting resolution. A resolution callback exception propagates when
///     there is no primary operation exception; otherwise it is logged and the primary exception is preserved.
/// </remarks>
internal sealed class UserPresenceNotification
{
    /// <summary>Tracks the lifecycle of an enabled, single-owner notification.</summary>
    private enum NotificationState
    {
        /// <summary>No request or terminal resolution has been attempted.</summary>
        Initial,

        /// <summary>The request callback is currently running.</summary>
        Requesting,

        /// <summary>The request callback completed successfully and can be resolved.</summary>
        Requested,

        /// <summary>The notification is terminal and cannot be requested or resolved again.</summary>
        Terminal
    }

    /// <summary>Writes cleanup callback failures without adding a public logger dependency.</summary>
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<UserPresenceNotification>();

    /// <summary>The application-owned callback, or <c>null</c> only for <see cref="None" />.</summary>
    private readonly IUserPresencePrompt? _prompt;

    /// <summary>The context used for the successful request and its matching resolution.</summary>
    private UserPresenceContext? _context;

    /// <summary>The current lifecycle state for an enabled instance.</summary>
    private NotificationState _state;

    /// <summary>Initializes the shared no-op notification.</summary>
    private UserPresenceNotification()
    {
    }

    /// <summary>Initializes an enabled notification for one operation.</summary>
    /// <param name="prompt">The application-owned notification callback.</param>
    /// <param name="context">The initial operation context.</param>
    private UserPresenceNotification(IUserPresencePrompt prompt, UserPresenceContext context)
    {
        _prompt = prompt;
        _context = context;
    }

    /// <summary>Gets the immutable shared no-op notification.</summary>
    public static UserPresenceNotification None { get; } = new();

    /// <summary>Gets whether this operation has an application callback and context.</summary>
    public bool IsEnabled => _prompt is not null;

    /// <summary>
    ///     Creates an enabled per-operation notification when both inputs are present; otherwise returns
    ///     <see cref="None" />.
    /// </summary>
    /// <param name="prompt">The optional application-owned notification callback.</param>
    /// <param name="context">The optional operation context.</param>
    /// <returns>An enabled per-operation notification, or <see cref="None" />.</returns>
    public static UserPresenceNotification Create(
        IUserPresencePrompt? prompt,
        UserPresenceContext? context) =>
        prompt is not null && context is not null
            ? new UserPresenceNotification(prompt, context)
            : None;

    /// <summary>Requests user presence once using the context supplied to <see cref="Create" />.</summary>
    /// <param name="cancellationToken">The operation cancellation token supplied to the callback.</param>
    /// <returns>A task representing the request callback.</returns>
    /// <remarks>
    ///     A callback exception propagates, makes the handle terminal, and prevents a matching resolution callback.
    /// </remarks>
    public ValueTask RequestAsync(CancellationToken cancellationToken) =>
        RequestCoreAsync(basis: null, cancellationToken);

    /// <summary>Requests user presence once, replacing the initial context basis before notification.</summary>
    /// <param name="basis">The stronger or more current basis observed at the request site.</param>
    /// <param name="cancellationToken">The operation cancellation token supplied to the callback.</param>
    /// <returns>A task representing the request callback.</returns>
    /// <remarks>
    ///     A callback exception propagates, makes the handle terminal, and prevents a matching resolution callback.
    /// </remarks>
    public ValueTask RequestAsync(
        UserPresenceBasis basis,
        CancellationToken cancellationToken) =>
        RequestCoreAsync(basis, cancellationToken);

    /// <summary>Resolves a successfully requested notification once.</summary>
    /// <param name="outcome">The terminal operation outcome.</param>
    /// <param name="primaryException">
    ///     The operation exception already being propagated, if any. A resolution failure is logged rather than
    ///     replacing this exception.
    /// </param>
    /// <returns>A task representing the resolution callback.</returns>
    /// <remarks>
    ///     The callback always receives <see cref="CancellationToken.None" />. Calling this before a successful
    ///     request terminally disables the notification. Calling it again is a no-op, including after the first
    ///     resolution callback throws.
    /// </remarks>
    public async ValueTask ResolveAsync(
        UserPresenceOutcome outcome,
        Exception? primaryException = null)
    {
        if (_prompt is null)
            return;

        NotificationState previousState = _state;
        _state = NotificationState.Terminal;
        if (previousState is not NotificationState.Requested)
            return;

        UserPresenceContext context = _context
            ?? throw new InvalidOperationException("A requested user-presence notification has no context.");

        try
        {
            await _prompt.OnUserPresenceResolvedAsync(context, outcome, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (primaryException is not null)
        {
            Logger.LogWarning(
                ex,
                "User-presence resolution callback failed while preserving primary {ExceptionType}",
                primaryException.GetType().Name);
        }
    }

    /// <summary>Runs the single request callback with an optional basis replacement.</summary>
    /// <param name="basis">The replacement basis, or <c>null</c> to preserve the initial context.</param>
    /// <param name="cancellationToken">The operation cancellation token supplied to the callback.</param>
    /// <returns>A task representing the request callback.</returns>
    private async ValueTask RequestCoreAsync(
        UserPresenceBasis? basis,
        CancellationToken cancellationToken)
    {
        if (_prompt is null || _state is not NotificationState.Initial)
            return;

        _state = NotificationState.Requesting;
        UserPresenceContext context = _context
            ?? throw new InvalidOperationException("An enabled user-presence notification has no context.");
        if (basis is { } requestedBasis && context.Basis != requestedBasis)
        {
            context = context with { Basis = requestedBasis };
        }

        try
        {
            await _prompt.OnUserPresenceRequestedAsync(context, cancellationToken).ConfigureAwait(false);
            if (_state is NotificationState.Requesting)
            {
                _context = context;
                _state = NotificationState.Requested;
            }
        }
        catch
        {
            _state = NotificationState.Terminal;
            throw;
        }
    }
}