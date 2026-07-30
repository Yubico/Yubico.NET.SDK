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

using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;

namespace Yubico.YubiKit.SecurityDomain;

/// <summary>
///     Exception thrown when establishing a secure channel (SCP03 or SCP11) with the Security
///     Domain application fails during the handshake/authentication phase.
/// </summary>
/// <remarks>
///     <para>
///         This exception is thrown only for failures encountered while a secure channel is being
///         established during <see cref="SecurityDomainSession.CreateAsync" /> or
///         <see cref="SecurityDomainSession.ResetAsync" />'s post-reset reinitialization — for
///         example, an SCP03 static-key mismatch, an SCP11 key-agreement receipt mismatch, an SCP11
///         certificate rejected by the device, or SCP requested against firmware/transport that does
///         not support it. It lets callers distinguish "the secure channel could not be established"
///         as a category from a generic per-operation Security Domain failure
///         (<see cref="ApduException" /> or <see cref="Yubico.YubiKit.Core.BadResponseException" />)
///         that occurs after a channel is already open, such as <c>GetDataAsync</c>,
///         <c>PutKeyAsync</c>, or <c>DeleteKeyAsync</c> — those are unchanged by this type and
///         continue to surface their original exception directly.
///     </para>
///     <para>
///         The original exception raised by Core's SCP initialization (an <see cref="ApduException" />
///         for an APDU-level rejection such as a wrong key, a
///         <see cref="Yubico.YubiKit.Core.BadResponseException" /> for a cryptographic verification
///         failure such as a card-cryptogram or receipt mismatch, or a <see cref="NotSupportedException" />
///         when SCP itself is not supported) is always preserved as
///         <see cref="Exception.InnerException" />. When the underlying failure carried an ISO 7816
///         status word, it is also preserved on <see cref="StatusWord" />.
///     </para>
/// </remarks>
public class SecureChannelException : Exception
{
    /// <summary>
    ///     Gets the ISO 7816 status word returned by the device during the failed handshake, or
    ///     <see langword="null" /> if the underlying failure did not carry one (for example, a
    ///     cryptographic verification failure detected locally, or an unsupported-firmware/transport
    ///     failure detected before any APDU was sent).
    /// </summary>
    public short? StatusWord { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SecureChannelException" /> class, wrapping the
    ///     underlying failure raised while establishing the secure channel.
    /// </summary>
    /// <param name="innerException">
    ///     The exception raised by Core's SCP initialization — typically an <see cref="ApduException" />,
    ///     a <see cref="Yubico.YubiKit.Core.BadResponseException" />, or a
    ///     <see cref="NotSupportedException" />.
    /// </param>
    public SecureChannelException(Exception innerException)
        : this(GetMessage(innerException), innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SecureChannelException" /> class with a custom
    ///     message, wrapping the underlying failure raised while establishing the secure channel.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public SecureChannelException(string message, Exception innerException)
        : base(message, innerException ?? throw new ArgumentNullException(nameof(innerException)))
    {
        StatusWord = (innerException as ApduException)?.SW;
    }

    private static string GetMessage(Exception innerException)
    {
        ArgumentNullException.ThrowIfNull(innerException);
        return $"Failed to establish a secure channel (SCP) with the Security Domain: {innerException.Message}";
    }
}