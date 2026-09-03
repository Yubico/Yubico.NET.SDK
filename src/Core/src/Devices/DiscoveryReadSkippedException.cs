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

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Why a best-effort discovery device-info read was skipped. Carried on
///     <see cref="DiscoveryReadSkippedException" /> so logs and diagnostics can distinguish unsupported
///     discovery access, active connection ownership, worker-admission saturation, and transport
///     supersession.
/// </summary>
internal enum DiscoveryReadSkipCause
{
    /// <summary>The device does not expose the internal discovery-only connection path.</summary>
    NoDiscoveryProvider,

    /// <summary>
    ///     A connection in this process owns (or is waiting for) the interface; discovery must not clobber it.
    /// </summary>
    InterfaceLeaseHeld,

    /// <summary>
    ///     All bounded discovery workers were busy and this read path skips instead of waiting
    ///     (the best-effort metadata path; identity reads wait for a slot instead).
    /// </summary>
    WorkerAdmissionSaturated,

    /// <summary>
    ///     Hotplug activity superseded this read before it opened the interface: the physical topology it
    ///     was started against may no longer exist, so it aborts (or its queued admission wait is
    ///     cancelled) instead of reading hardware it can no longer name. Retried on the next scan.
    /// </summary>
    SupersededByTransportActivity
}

/// <summary>
///     Thrown when a best-effort discovery device-info read is intentionally skipped because the target does
///     not support the internal discovery-only connection path, an active or waiting connection prevents
///     exclusive access, bounded worker admission is saturated, or transport activity superseded the read
///     before it opened the interface (see <see cref="ProtocolDeviceInfo.ReadBoundedAsync" />). The read does
///     not open a normal session connection or transmit. Callers degrade like any other failed best-effort
///     read: identity unknown / try next transport.
/// </summary>
internal sealed class DiscoveryReadSkippedException(
    string deviceId,
    DiscoveryReadSkipCause cause = DiscoveryReadSkipCause.NoDiscoveryProvider) : Exception(
    $"Discovery device-info read for {deviceId} skipped ({cause}).")
{
    /// <summary>The specific reason the read was skipped.</summary>
    public DiscoveryReadSkipCause Cause { get; } = cause;
}