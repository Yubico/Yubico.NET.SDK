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

namespace Yubico.YubiKit.YubiHsm;

/// <summary>
///     Exception thrown when a YubiHSM Auth management-key or credential-password verification
///     fails with a status word that carries a remaining-retry count (0x63Cx).
/// </summary>
/// <remarks>
///     <para>
///         The session checks retry-count status words for these operations:
///         management-key-gated credential operations (<c>PutCredential*Async</c>,
///         <c>DeleteCredentialAsync</c>, <c>GenerateCredentialAsymmetricAsync</c>,
///         <c>PutManagementKeyAsync</c>, <c>ChangeCredentialPasswordAdminAsync</c>) and
///         credential-password-gated session-key operations
///         (<c>CalculateSessionKeysSymmetricAsync</c>, <c>CalculateSessionKeysAsymmetricAsync</c>,
///         <c>ChangeCredentialPasswordAsync</c>).
///     </para>
///     <para>
///         <c>GetChallengeAsync</c> uses the general <see cref="ApduException" /> error path, including
///         when a password is transmitted. Inspect its status word when applying a retry policy.
///         Each SDK call makes one authentication attempt; the application decides whether to retry.
///     </para>
///     <para>
///         <see cref="RetriesRemaining" /> exposes the parsed retry count as a typed value so
///         callers can apply retry policy directly from the property.
///     </para>
///     <para>
///         This type derives from <see cref="ApduException" /> so existing code that catches
///         <see cref="ApduException" /> continues to observe every YubiHSM Auth protocol failure,
///         including retry failures.
///     </para>
/// </remarks>
public sealed class HsmAuthRetryException : ApduException
{
    /// <summary>
    ///     Gets the attempts remaining before credential deletion or management-key blocking.
    /// </summary>
    /// <remarks>
    ///     Yubico's YubiHSM Auth overview describes successful authentication before exhaustion as
    ///     resetting the corresponding counter to eight. Exhausting credential retries deletes that
    ///     credential; exhausting management-key retries blocks management operations until an applet
    ///     reset. Reset erases every credential and restores the factory management key.
    /// </remarks>
    public int RetriesRemaining { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="HsmAuthRetryException" /> class.
    /// </summary>
    /// <param name="retriesRemaining">The number of remaining verification attempts.</param>
    /// <param name="message">The message that describes the error.</param>
    public HsmAuthRetryException(int retriesRemaining, string message)
        : base(message)
    {
        RetriesRemaining = retriesRemaining;
    }
}