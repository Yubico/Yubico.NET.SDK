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

using Yubico.YubiKit.Core.Native.Linux.Libc;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>Internal readiness mechanics, not an application callback/event API.</summary>
/// <remarks>
/// One lazy poller serves all live registrations, exits when idle, and recreates its eventfd
/// on the next admission. DisposeAsync permanently shuts down the instance. Open/write/ioctl
/// and checked device close belong to the connection's native owner, never this poller.
/// </remarks>
internal interface ILinuxHidReadReactor : IAsyncDisposable
{
    /// <summary>Every published admission returns a retained token, including a failed activation.</summary>
    /// <remarks>
    /// Generation is immutable and supplied to every readiness callback. Callbacks must be
    /// bounded internal mechanics (for example one nonblocking TryRead), never application
    /// code or synchronous close. Complete future consumer TCSs with RunContinuationsAsynchronously.
    /// POLLERR/HUP/NVAL and callback exceptions remove the registration and notify onTerminal
    /// once. Terminal-notification exceptions cannot escape into sibling registrations.
    /// Activation reports the immutable admission outcome (null on success), independently
    /// from actual Quiescence. Exceptions from Register occur only before publication.
    /// Admission while the local wake fd is retiring waits in Activation on that same poller;
    /// no replacement wake fd/thread or cleanup job is created before retirement finishes.
    /// </remarks>
    ILinuxHidReadRegistration Register(LinuxFileSafeHandle handle, long generation,
        Action<long, short> onReady, Action<Exception> onTerminal);
}

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
    long Generation { get; }
    Task<Exception?> Activation { get; }
    Task Quiescence { get; }
    Exception? RequestRemoval();
    /// <summary>Release exactly once on the native owner worker after Quiescence; never on a reactor thread.</summary>
    /// <remarks>Early, concurrent, and reactor-thread calls throw. Repeated completed release is a no-op.</remarks>
    void ReleaseReference();
    /// <summary>Explicit control wake, not a read callback or a drain acknowledgement; failures throw IOException.</summary>
    void Signal();
}