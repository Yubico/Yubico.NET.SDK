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
/// Outcome of a single blocking wait on the Linux HID event source.
/// </summary>
internal enum LinuxHidPollOutcome
{
    /// <summary>No actionable outcome (EINTR or spurious wake); poll again.</summary>
    Retry = 0,

    /// <summary>The udev monitor has at least one event ready to receive.</summary>
    Event,

    /// <summary>The shutdown event fd was signaled; exit the loop cleanly.</summary>
    ShutdownSignaled,

    /// <summary>The shutdown event fd reported an error condition; the loop cannot be woken reliably.</summary>
    ShutdownFdError,

    /// <summary>The udev monitor fd reported an error condition; events can no longer be received.</summary>
    MonitorFdError,

    /// <summary>poll() failed with a non-EINTR error.</summary>
    PollFailed
}