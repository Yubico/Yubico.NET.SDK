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
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Sessions;

/// <summary>
///     Which session, if any, is live on a given connection. One connection hosts one session at a time.
/// </summary>
/// <remarks>
///     <para>
///         This is the connection-scoped half of the ownership contract; the interface-scoped half lives in
///         <see cref="DeviceConnectionRegistry" />. The interface lease cannot see this case: one connection,
///         opened once, handed to two sessions. Each session opens with its own applet SELECT, which on a CCID
///         interface deselects whatever the other session was using.
///     </para>
///     <para>
///         Enforced at binding, before any wire operation, which is what canonical yubikit does — Rust makes a
///         second session a compile error by taking the connection by value; Python's base <c>Session</c> stores
///         itself on the connection at construction. Refusal happens on the call that would cause the damage,
///         not on the victim's next operation.
///     </para>
///     <para>
///         Keyed by connection identity in a <see cref="ConditionalWeakTable{TKey,TValue}" /> rather than by a
///         field on the connection: <see cref="IConnection" /> is a public interface with caller-provided
///         implementations, so the guard must work without asking implementers to carry state. Entries die with
///         the connection.
///     </para>
/// </remarks>
internal static class ConnectionSessionGuard
{
    private static readonly ConditionalWeakTable<IConnection, Slot> Slots = new();

    /// <summary>
    ///     Binds <paramref name="session" /> to <paramref name="connection" /> as its live session.
    /// </summary>
    /// <exception cref="ConnectionInUseException">The connection already has a live session.</exception>
    public static void Attach(IConnection connection, object session)
    {
        var slot = Slots.GetOrCreateValue(connection);
        lock (slot)
        {
            if (slot.Session is { } holder)
                throw new ConnectionInUseException(
                    $"This connection already has a live {holder.GetType().Name}. A connection hosts one " +
                    "session at a time, because each session selects its own application on the card and " +
                    "would deselect the other's. Dispose the current session first — successive sessions " +
                    "over one connection are supported and do not require reconnecting.");

            slot.Session = session;
        }
    }

    /// <summary>
    ///     Whether <paramref name="session" /> is the live session recorded for <paramref name="connection" />.
    /// </summary>
    /// <remarks>
    ///     Used to catch a session that never went through <see cref="ApplicationSession.Construct{TSession}" />.
    ///     Because binding is no longer automatic in the constructor, an unbound session would otherwise run
    ///     completely unguarded — a silent loss of the one-session-per-connection rule.
    /// </remarks>
    public static bool IsHolder(IConnection connection, object session)
    {
        if (!Slots.TryGetValue(connection, out var slot))
            return false;

        lock (slot)
        {
            return ReferenceEquals(slot.Session, session);
        }
    }

    /// <summary>
    ///     Releases <paramref name="session" />'s claim, if it still holds one. Idempotent, and a no-op when a
    ///     different session now holds the connection.
    /// </summary>
    public static void Detach(IConnection connection, object session)
    {
        if (!Slots.TryGetValue(connection, out var slot))
            return;

        lock (slot)
        {
            if (ReferenceEquals(slot.Session, session))
                slot.Session = null;
        }
    }

    private sealed class Slot
    {
        public object? Session { get; set; }
    }
}