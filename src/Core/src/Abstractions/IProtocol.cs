// Copyright 2025 Yubico AB
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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;

namespace Yubico.YubiKit.Core.Abstractions;

/// <summary>
///     Base contract for a configured transport protocol over a single opened YubiKey connection.
/// </summary>
/// <remarks>
///     <para>
///         A protocol is a user of the connection it was created from, never its owner. Disposing a
///         protocol does not dispose that connection: whoever created the connection disposes it. The
///         one deliberate exception is a decorating protocol such as the SCP wrapper, which owns the
///         protocol it wraps and disposes it (along with any session key material) — the cascade still
///         stops before the connection.
///     </para>
///     <para>
///         Sessions configure the protocol after applet probing has resolved firmware. Call
///         <see cref="Configure" /> before normal applet operations and before establishing decorators
///         such as SCP.
///     </para>
///     <para>
///         Implementations serialize full logical exchanges internally. Concurrent callers are safe, but
///         work executes sequentially because YubiKey transports maintain chained APDU, CTAP HID, OTP HID,
///         or SCP state across packets.
///     </para>
///     <para>
///         Application sessions keep the effective protocol for the session lifetime and dispose it when
///         the session is disposed. Applet backends borrow the protocol; they do not own it.
///     </para>
/// </remarks>
public interface IProtocol : IDisposable
{
    void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null);
}