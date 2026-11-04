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
///         During creation and post-reset reinitialization, SecurityDomainSession wraps selected
///         secure-channel configuration failures when SCP parameters were supplied: ApduException,
///         BadResponseException, and NotSupportedException. Protocol preparation and application
///         selection occur before that catch, and cancellation is rethrown unchanged. This type is
///         not a wrapper for every session-creation failure or for later per-operation failures.
///     </para>
///     <para>
///         The wrapped exception is preserved as <see cref="Exception.InnerException" />. If that
///         immediate exception is an ApduException, its status word is copied to StatusWord;
///         nested inner exceptions are not searched.
///     </para>
/// </remarks>
public class SecureChannelException : Exception
{
    /// <summary>
    ///     Gets the status word of the immediate wrapped ApduException, or null for other exception types.
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