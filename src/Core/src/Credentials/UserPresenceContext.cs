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

using System.Runtime.CompilerServices;

namespace Yubico.YubiKit.Core.Credentials;

/// <summary>Describes one user-presence notification issued by an SDK operation.</summary>
/// <remarks>
///     This is a non-positional record so optional context can be added in later releases without breaking callers
///     or implementations. Implementations should ignore properties they do not use.
/// </remarks>
public sealed record UserPresenceContext
{
    /// <summary>Gets the reason the SDK issued the notification.</summary>
    public required UserPresenceBasis Basis { get; init; }

    /// <summary>
    ///     Gets the SDK application or applet performing the operation. See <see cref="UserPresenceApplications" />.
    /// </summary>
    public required string Application { get; init; }

    /// <summary>
    ///     Gets a stable identifier for the SDK method requesting presence, normally one of
    ///     <see cref="UserPresenceOperations" />. This is the method name without <c>Async</c>, not localized
    ///     display text or necessarily a single user-facing verb (for example, PIV <c>SignOrDecrypt</c>).
    /// </summary>
    /// <remarks>
    ///     Always set on SDK-created contexts; may be null on caller-created contexts. Interpret together with
    ///     <see cref="Application" /> and treat unknown values as a generic prompt.
    /// </remarks>
    public string? Operation { get; init; }

    /// <summary>
    ///     Gets an optional display-oriented description of the operation or object requiring presence, such as a
    ///     relying-party identifier, credential label, or key slot.
    /// </summary>
    public string? Scope { get; init; }

    /// <summary>Compares notification contexts by reference identity.</summary>
    public bool Equals(UserPresenceContext? other) => ReferenceEquals(this, other);

    /// <summary>Returns an identity-based hash code for this notification context.</summary>
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

    /// <summary>Formats the context without including <see cref="Scope" />.</summary>
    /// <remarks>
    ///     <see cref="Application" /> and <see cref="Operation" /> are SDK identifiers on SDK-created contexts;
    ///     caller-created contexts can carry arbitrary values and should not be assumed non-sensitive.
    /// </remarks>
    public override string ToString() =>
        $"{nameof(UserPresenceContext)} {{ {nameof(Basis)} = {Basis}, {nameof(Application)} = {Application}, " +
        $"{nameof(Operation)} = {Operation} }}";
}