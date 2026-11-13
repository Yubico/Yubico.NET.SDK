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

/// <summary>
/// Abstraction over the native udev monitor, poll(), and shutdown eventfd used by
/// <see cref="LinuxHidDeviceListener"/>. Enables no-hardware fault-injection tests of the
/// listener's loop policy (error transitions, hint emission and caller-requested stop).
/// </summary>
/// <remarks>
/// Ownership: the listener session's thread owns the source and disposes it when the loop
/// exits. <see cref="SignalShutdown"/> may be called concurrently from other threads and
/// must tolerate a source that is concurrently disposing.
/// Fake-driven loop-policy and caller-stop tests do not establish native poll completion or a shutdown deadline.
/// </remarks>
internal interface ILinuxHidEventSource : IDisposable
{
    /// <summary>
    /// Creates the udev context, monitor (hidraw subsystem filter, receiving enabled), and
    /// shutdown event fd. Returns false when any native step fails; the caller disposes the
    /// source in that case.
    /// </summary>
    bool Initialize();

    /// <summary>
    /// Blocks until the monitor has an event, the shutdown fd is signaled, or an error occurs.
    /// </summary>
    LinuxHidPollOutcome WaitForEvent();

    /// <summary>
    /// Receives one pending udev event, or returns null when the source has no event to return.
    /// </summary>
    /// <remarks>
    /// The built-in source returns null for a missing or invalid monitor or a native no-device result.
    /// The native result can mean no event is available or reception failed. Receive failures can coincide
    /// with lost notifications, so the listener requests a rescan on null to reconcile potentially missed
    /// changes, including removals.
    /// </remarks>
    LinuxHidUdevEvent? ReceiveEvent();

    /// <summary>
    /// Attempts a shutdown wake so a thread blocked in <see cref="WaitForEvent"/> can observe cancellation.
    /// </summary>
    /// <remarks>
    /// May be called from any thread and must tolerate concurrent <see cref="IDisposable.Dispose"/>.
    /// A missing, invalid or concurrently disposed shutdown handle, or a failed signal write, can leave the
    /// native wait unwoken. A normal return does not acknowledge a wake, loop termination, drain or release.
    /// </remarks>
    void SignalShutdown();

    /// <summary>
    /// Returns true when the hidraw device node exists and can be opened, indicating the node
    /// is ready for enumeration by discovery rescans.
    /// </summary>
    bool IsHidrawReady(string? devNode);
}