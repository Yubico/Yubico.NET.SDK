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
///     Thrown by discovery's best-effort device-info reads when the target interface gained another live
///     connection between the pre-connect skip check and the first APDU (see
///     <see cref="ProtocolDeviceInfo.ReadBoundedAsync" />). The read aborts without transmitting so it
///     cannot clobber the concurrent session's applet state. Callers degrade exactly like any other failed
///     best-effort read: identity unknown / try next transport.
/// </summary>
internal sealed class DiscoveryReadSkippedException(string deviceId) : Exception(
    $"Discovery device-info read for {deviceId} aborted: the interface gained a live connection while the read was starting.");