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
///     Attaches the application's reason for an operation to the user-presence notifications it causes.
/// </summary>
/// <remarks>
///     <para>
///         The SDK reports which operation is waiting for a touch in <see cref="UserPresenceContext.Operation" />. Only
///         the application knows why the user started it, such as "approve the transfer". Wrap the SDK call in a scope
///         and every notification that call raises carries the text in <see cref="UserPresenceContext.Intent" />:
///     </para>
///     <code>
///         using (UserPresenceIntent.BeginScope("approve the transfer"))
///         {
///             await client.GetAssertionAsync(options, cancellationToken);
///         }
///     </code>
///     <para>
///         The scope follows the async flow of the code that opened it, like <c>Activity.Current</c>. Concurrent
///         operations in other flows do not see it, and code started from inside it, including <c>Task.Run</c>, does.
///         The SDK copies the intent into the context when the operation begins, so the prompt receives it regardless
///         of which thread the notification is raised on. Scopes nest: the innermost undisposed scope wins, and
///         disposing a scope, in any order and any number of times, removes only that scope.
///     </para>
///     <para>
///         Call <see cref="BeginScope" /> in the same method that awaits the SDK call, or in a synchronous helper. Do not
///         open it inside an <c>async</c> helper and return the scope to the caller: the returned object is still
///         undisposed, but the intent does not flow back out of the helper, so the caller's operation gets no intent.
///         This is standard <see cref="AsyncLocal{T}" /> behavior.
///     </para>
/// </remarks>
public static class UserPresenceIntent
{
    private static readonly AsyncLocal<Scope?> CurrentScope = new();

    /// <summary>
    ///     Starts a scope whose <paramref name="intent" /> is attached to user-presence notifications raised by SDK
    ///     operations started inside it.
    /// </summary>
    /// <param name="intent">
    ///     Display text describing why the user is performing the operation, written as a lowercase verb phrase such as
    ///     <c>"approve the transfer"</c> so it reads naturally after "Touch your YubiKey to". It is passed to the prompt
    ///     unchanged and is never logged by the SDK.
    /// </param>
    /// <returns>An object that ends the scope when disposed.</returns>
    /// <exception cref="ArgumentException"><paramref name="intent" /> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="intent" /> is <c>null</c>.</exception>
    public static IDisposable BeginScope(string intent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(intent);

        var scope = new Scope(intent, CurrentScope.Value);
        CurrentScope.Value = scope;
        return scope;
    }

    /// <summary>Gets the intent of the innermost undisposed scope in the current async flow.</summary>
    internal static string? Current => Scope.FirstActive(CurrentScope.Value)?.Intent;

    private sealed class Scope(string intent, Scope? parent) : IDisposable
    {
        private int _disposed;

        public string Intent { get; } = intent;

        private Scope? Parent { get; } = parent;

        public static Scope? FirstActive(Scope? scope)
        {
            while (scope is not null && Volatile.Read(ref scope._disposed) != 0)
            {
                scope = scope.Parent;
            }

            return scope;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            // Only unwind the flow that still points at this scope. Other flows, and out-of-order disposal, skip
            // disposed scopes when reading, so a disposed intent can never be observed again.
            if (ReferenceEquals(CurrentScope.Value, this))
            {
                CurrentScope.Value = FirstActive(Parent);
            }
        }
    }
}