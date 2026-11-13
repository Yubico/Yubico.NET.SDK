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

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>A unique registration identity paired with one immutable connection generation.</summary>
/// <remarks>
/// RequestRemoval publishes control before signaling and returns any wake failure, not proof
/// of removal. Quiescence never faults: only actual snapshot/callback drain completes it,
/// including after a later genuine wake following a failure. A permanently broken wake leaves
/// it pending and the reactor retains the token/owner callbacks. Requesting removal from a
/// callback is allowed; synchronously waiting there is not. ReleaseReference is a separate,
/// synchronous owner-worker action after Quiescence, never an implicitly scheduled native job.
/// </remarks>
internal interface ILinuxHidReadRegistration
{
    /// <summary>Gets the immutable connection generation supplied at registration.</summary>
    long Generation { get; }
    /// <summary>Gets the nonfaulting, immutable admission result: null on activation, otherwise the initial activation/removal error.</summary>
    /// <remarks>Readiness may start immediately after activation. Neither success nor failure proves callback drain.</remarks>
    Task<Exception?> Activation { get; }
    /// <summary>Gets the nonfaulting barrier completed only after this identity's snapshots and ready/terminal callbacks have drained.</summary>
    /// <remarks>Does not release the retained reference or prove descriptor close; a broken wake can leave it pending indefinitely.</remarks>
    Task Quiescence { get; }
    /// <summary>Publishes removal and returns the stable first wake error, or null; neither outcome acknowledges drain.</summary>
    /// <remarks>Safe to request within a callback, but never synchronously wait there for <see cref="Quiescence"/>.</remarks>
    Exception? RequestRemoval();
    /// <summary>Release exactly once on the native owner worker after Quiescence; never on a reactor thread.</summary>
    /// <remarks>Early, concurrent, and reactor-thread calls throw. Repeated completed release is a no-op.</remarks>
    void ReleaseReference();
    /// <summary>Explicit control wake, not a read callback or a drain acknowledgement; failures throw IOException.</summary>
    void Signal();
}