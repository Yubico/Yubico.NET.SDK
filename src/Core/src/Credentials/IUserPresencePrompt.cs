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

namespace Yubico.YubiKit.Core.Credentials;

/// <summary>
///     Receives notifications when an SDK operation requires or may require physical user presence.
/// </summary>
/// <remarks>
///     <para>
///         This callback is informational. Completing a notification means that the application has handled the
///         notification; it does not report that the user has interacted with the device and does not complete the
///         device operation.
///     </para>
///     <para>
///         The SDK retains the implementation supplied through session creation options for the session lifetime.
///         The caller owns the implementation and remains responsible for disposing it, if applicable.
///     </para>
/// </remarks>
public interface IUserPresencePrompt
{
    /// <summary>Notifies the application that an operation requires or may require user presence.</summary>
    /// <param name="context">Describes the application, scope, and basis for the notification.</param>
    /// <param name="cancellationToken">Token that the implementation must monitor for cancellation requests.</param>
    /// <remarks>
    ///     Implementations should present or update their user-presence indication and return without waiting for
    ///     the physical interaction itself. The device operation determines whether presence was supplied. The SDK
    ///     pairs this request with a resolution only after this callback completes successfully. If it throws, the
    ///     SDK performs any protocol-safe cancellation, drain, or reset that is required and propagates the exception
    ///     without calling <see cref="OnUserPresenceResolvedAsync" />.
    /// </remarks>
    ValueTask OnUserPresenceRequestedAsync(
        UserPresenceContext context,
        CancellationToken cancellationToken);

    /// <summary>Notifies the application that the corresponding user-presence request has ended.</summary>
    /// <param name="context">The context supplied with the request notification.</param>
    /// <param name="outcome">The terminal outcome of the operation associated with the notification.</param>
    /// <param name="cancellationToken">
    ///     Token for the cleanup notification. The SDK always supplies <see cref="CancellationToken.None" /> so the
    ///     implementation can clear any user interface it displayed after operation cancellation.
    /// </param>
    /// <remarks>
    ///     The default implementation does nothing, allowing implementations interested only in the request
    ///     notification to omit this method. If this callback throws after a successful device operation, its
    ///     exception propagates. If an operation or cancellation exception is already being propagated, the SDK logs
    ///     the resolution failure and preserves the primary exception.
    /// </remarks>
    ValueTask OnUserPresenceResolvedAsync(
        UserPresenceContext context,
        UserPresenceOutcome outcome,
        CancellationToken cancellationToken) => default;
}