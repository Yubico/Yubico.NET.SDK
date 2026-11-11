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
///     Reports an unsuccessful User or Admin VERIFY response with an optional retry count.
/// </summary>
/// <remarks>
///     <para>
///         Thrown by <see cref="IOpenPgpSession.VerifyPinAsync" /> and
///         <see cref="IOpenPgpSession.VerifyAdminAsync" /> for every unsuccessful VERIFY response.
///         SW 63Cx supplies a retry count, 6983 reports a blocked credential, and 6982 triggers a
///         PIN-status query. Other command/security failures also currently become this exception;
///         inspect <see cref="ApduException.SW" /> before interpreting the failure as a wrong PIN.
///         PIN change and recovery methods propagate general APDU exceptions.
///     </para>
///     <para>
///         <see cref="RetriesRemaining" /> carries the count from 63Cx, zero from 6983, or the result
///         of a fallback status query for 6982. It is null when the count is unavailable.
///         Current extended User verification selects Reset Code identity for that
///         query, so its count can describe the Reset Code rather than the User PIN. Query fresh
///         <see cref="IOpenPgpSession.GetPinStatusAsync" /> and inspect <see cref="PwStatus.AttemptsUser" />
///         after an extended-mode failure; typed zero alone is insufficient to establish User blocking.
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
    ///     Extended-mode 6982 fallback currently selects Reset Code attempts. Query fresh
    ///     <see cref="IOpenPgpSession.GetPinStatusAsync" /> and inspect <see cref="PwStatus.AttemptsUser" />
    ///     before deciding User PIN recovery; a reported zero alone does not establish User blocking.
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