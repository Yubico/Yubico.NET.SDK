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
///     <see cref="DiscoveryReadSkippedException" /> so logs and diagnostics can distinguish the causes
///     (Phase-0 diagnostics misattributed worker-admission saturation to interface contention because the
///     causes were previously indistinguishable).
/// </summary>
internal enum DiscoveryReadSkipCause
{
    /// <summary>The device does not expose the internal discovery-only connection path.</summary>
    NoDiscoveryProvider,

    /// <summary>
    ///     A session in this process owns (or is waiting for) the interface; discovery must not clobber it.
    /// </summary>
    InterfaceLeaseHeld,

    /// <summary>
    ///     All bounded discovery workers were busy and this read path skips instead of waiting
    ///     (the best-effort metadata path; identity reads wait for a slot instead).
    /// </summary>
    WorkerAdmissionSaturated
}

/// <summary>
///     Thrown by discovery's best-effort device-info reads when the target interface cannot safely provide
///     exclusive discovery access (see <see cref="ProtocolDeviceInfo.ReadBoundedAsync" />). This includes an
///     active session or a device that does not expose the internal discovery-only connection path. The read
///     aborts without opening a normal session connection or transmitting, so it cannot clobber applet state.
///     Callers degrade exactly like any other failed best-effort read: identity unknown / try next transport.
/// </summary>
internal sealed class DiscoveryReadSkippedException(
    string deviceId,
    DiscoveryReadSkipCause cause = DiscoveryReadSkipCause.NoDiscoveryProvider) : Exception(
    $"Discovery device-info read for {deviceId} skipped ({cause}): exclusive discovery access is unavailable.")
{
    /// <summary>The specific reason the read was skipped.</summary>
    public DiscoveryReadSkipCause Cause { get; } = cause;
}