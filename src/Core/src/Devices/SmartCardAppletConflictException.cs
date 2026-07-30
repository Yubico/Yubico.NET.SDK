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
///     Thrown when an applet SELECT would deselect an applet that another open connection on the same
///     smart-card (CCID) interface is still using.
/// </summary>
/// <remarks>
///     <para>
///         A YubiKey's CCID interface has one active applet. Selecting a different applet deselects the
///         previous one <em>and destroys its security state</em>: the earlier session's next command fails
///         with <c>SW=0x6D00</c> ("instruction not supported"), because the applet is gone, not merely
///         deauthenticated. Nothing on the wire reports this to the session that was clobbered, and the
///         SELECT that caused it succeeds. Measured on hardware — see
///         <c>docs/plans/session-contention/phase1-findings.md</c>.
///     </para>
///     <para>
///         Re-selecting the <em>same</em> applet is safe and is allowed: concurrent sessions on one applet
///         share the interface by reference count. So is switching applets on a connection that is the only
///         one holding the interface — that session can only disturb itself.
///     </para>
///     <para>
///         This exception is raised <em>before</em> the conflicting SELECT is transmitted, so the applet
///         holder is left intact. Nothing is retried, restored, or re-authenticated on your behalf: an
///         automatic re-SELECT would silently rebuild a security state the caller never re-authorized.
///         Resolve it by closing the other session first, or by giving this session a different interface —
///         a HID interface on the same YubiKey can be used while CCID is held (pass
///         <c>preferredConnection</c> to choose one). Note that HID transports are considerably slower to
///         open, which is why no fallback happens automatically.
///     </para>
///     <para>
///         Detection is in-process only, matching <c>DeviceConnectionRegistry</c>: another process holding
///         the same card can still deselect an applet without this SDK being able to see it.
///     </para>
/// </remarks>
public sealed class SmartCardAppletConflictException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SmartCardAppletConflictException" /> class.
    /// </summary>
    /// <param name="interfaceId">Identifier of the smart-card interface whose applet is contended.</param>
    /// <param name="heldApplicationId">The application identifier (AID) currently selected on the interface.</param>
    /// <param name="requestedApplicationId">The application identifier (AID) this SELECT asked for.</param>
    /// <remarks>
    ///     Both AIDs are copied. The refused SELECT is detected mid-transmit, and the buffer its AID points
    ///     into is zeroed as soon as this exception unwinds past the APDU transmitter — an exception whose
    ///     properties read as all-zeroes by the time a caller inspects them would be worse than useless.
    /// </remarks>
    public SmartCardAppletConflictException(
        string interfaceId,
        ReadOnlyMemory<byte> heldApplicationId,
        ReadOnlyMemory<byte> requestedApplicationId) : base(
        BuildMessage(interfaceId, heldApplicationId.Span, requestedApplicationId.Span))
    {
        InterfaceId = interfaceId;
        HeldApplicationId = heldApplicationId.ToArray();
        RequestedApplicationId = requestedApplicationId.ToArray();
    }

    /// <summary>The smart-card interface on which the conflict occurred (the registry's interface DeviceId).</summary>
    public string InterfaceId { get; }

    /// <summary>The AID another open connection on this interface currently has selected.</summary>
    public ReadOnlyMemory<byte> HeldApplicationId { get; }

    /// <summary>The AID whose selection was refused.</summary>
    public ReadOnlyMemory<byte> RequestedApplicationId { get; }

    private static string BuildMessage(
        string interfaceId,
        ReadOnlySpan<byte> heldApplicationId,
        ReadOnlySpan<byte> requestedApplicationId) =>
        $"Smart-card interface '{interfaceId}' already has application {Describe(heldApplicationId)} selected by "
        + $"another open connection. Selecting {Describe(requestedApplicationId)} would deselect it and leave that "
        + "connection failing with SW=0x6D00, so the SELECT was refused and nothing was transmitted. Close the other "
        + "connection first, or open this one on a different interface — a HID interface on the same YubiKey can be "
        + "used while CCID is held (pass preferredConnection). Selecting the same application as the current holder "
        + "is always allowed.";

    private static string Describe(ReadOnlySpan<byte> applicationId) =>
        applicationId.IsEmpty ? "<none>" : Convert.ToHexString(applicationId);
}