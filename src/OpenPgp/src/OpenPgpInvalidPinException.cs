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

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Exception thrown when OpenPGP User, Admin, or Reset Code PIN verification fails.
/// </summary>
/// <remarks>
///     <para>
///         Thrown by <see cref="IOpenPgpSession.VerifyPinAsync" /> and
///         <see cref="IOpenPgpSession.VerifyAdminAsync" /> whenever the card rejects the
///         supplied PIN, regardless of which status word the card used to report it: a
///         standard retry-counted failure (SW 0x63Cx), "security status not satisfied"
///         (SW 0x6982, returned by some 5.8.0-alpha firmware instead of 0x63Cx), or the PIN
///         already being permanently blocked (SW 0x6983).
///     </para>
///     <para>
///         <see cref="RetriesRemaining" /> exposes the parsed retry count as a typed value so
///         callers do not need to parse <see cref="Exception.Message" /> or separately query
///         <see cref="IOpenPgpSession.GetPinStatusAsync" />. It is <c>0</c> when the PIN is
///         already blocked, a positive count when retries remain, and <c>null</c> only when the
///         card's failure status carries no retry count and a fallback status query also failed.
///     </para>
///     <para>
///         This type derives from <see cref="ApduException" /> so existing code that catches
///         <see cref="ApduException" /> continues to observe every OpenPGP PIN verification
///         failure.
///     </para>
/// </remarks>
public sealed class OpenPgpInvalidPinException : ApduException
{
    /// <summary>
    ///     Gets the number of verification attempts remaining before the PIN is blocked, or
    ///     <c>null</c> if the card did not report a retry count and it could not otherwise be
    ///     determined.
    /// </summary>
    public int? RetriesRemaining { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="OpenPgpInvalidPinException" /> class.
    /// </summary>
    /// <param name="retriesRemaining">
    ///     The number of remaining verification attempts, or <c>null</c> if unknown.
    /// </param>
    /// <param name="message">The message that describes the error.</param>
    public OpenPgpInvalidPinException(int? retriesRemaining, string message)
        : base(message)
    {
        RetriesRemaining = retriesRemaining;
    }
}