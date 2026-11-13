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
    /// <param name="handle">Owner's live descriptor; a separate SafeHandle reference is retained until explicit release after drain.</param>
    /// <param name="generation">Immutable owner generation returned with readiness, in addition to the unique token identity.</param>
    /// <param name="onReady">Bounded internal nonblocking input mechanics, receiving generation and native readiness flags.</param>
    /// <param name="onTerminal">Contained internal terminal notification; its return is included in actual quiescence.</param>
    /// <returns>The published identity, even when activation fails; retain it through quiescence and reference release.</returns>
    ILinuxHidReadRegistration Register(LinuxFileSafeHandle handle, long generation,
        Action<long, short> onReady, Action<Exception> onTerminal);
}