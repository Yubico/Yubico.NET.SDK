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
///     Reports a recognized credential-command failure with an optional retry count.
/// </summary>
/// <remarks>
///     <para>
///         Used for selected statuses from VERIFY, CHANGE and Reset-Code recovery. The original
///         <see cref="ApduException.SW" /> and command header remain available. Unclassified statuses
///         and administrator-authenticated recovery failures propagate as general APDU exceptions.
///     </para>
///     <para>
///         <see cref="RetriesRemaining" /> carries an explicit count, zero for a blocked credential,
///         or the result of a status query. It is null when the count is unavailable. In particular,
///         6982 may indicate something other than an incorrect credential, even when a count is available.
///         In either User PIN verification mode, the query reads the User PIN counter.
///         Diagnose uncertainty before retrying. User recovery remains available through Reset Code/Admin.
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
    ///     Gets the available reported retry count, or <c>null</c> when unavailable.
    /// </summary>
    /// <remarks>
    ///     Diagnose the cause before another verification attempt.
    /// </remarks>
    public int? RetriesRemaining { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="OpenPgpInvalidPinException" /> class.
    /// </summary>
    /// <param name="retriesRemaining">
    ///     The reported retry count, or <c>null</c> if unknown.
    /// </param>
    /// <param name="message">The message that describes the error.</param>
    public OpenPgpInvalidPinException(int? retriesRemaining, string message)
        : base(message)
    {
        RetriesRemaining = retriesRemaining;
    }
}