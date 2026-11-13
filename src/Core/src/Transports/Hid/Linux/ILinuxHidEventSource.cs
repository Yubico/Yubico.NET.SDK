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
/// listener's loop policy (bounded exit, error transitions, hint emission).
/// </summary>
/// <remarks>
/// Ownership: the listener session's thread owns the source and disposes it when the loop
/// exits. <see cref="SignalShutdown"/> may be called concurrently from other threads and
/// must tolerate a source that is concurrently disposing.
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
    /// Receives one pending udev event. Returns null when the receive fails — which is most
    /// likely during event storms (for example ENOBUFS after the kernel dropped notifications),
    /// exactly when a removal may have been lost.
    /// </summary>
    LinuxHidUdevEvent? ReceiveEvent();

    /// <summary>
    /// Wakes a thread blocked in <see cref="WaitForEvent"/> so it can observe cancellation.
    /// Safe to call from any thread, including concurrently with <see cref="IDisposable.Dispose"/>.
    /// </summary>
    void SignalShutdown();

    /// <summary>
    /// Returns true when the hidraw device node exists and can be opened, indicating the node
    /// is ready for enumeration by discovery rescans.
    /// </summary>
    bool IsHidrawReady(string? devNode);
}