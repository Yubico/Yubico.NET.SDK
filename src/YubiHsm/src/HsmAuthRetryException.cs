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
///         This exception is thrown uniformly for every retry-counted YubiHSM Auth operation:
///         management-key-gated credential operations (<c>PutCredential*Async</c>,
///         <c>DeleteCredentialAsync</c>, <c>GenerateCredentialAsymmetricAsync</c>,
///         <c>PutManagementKeyAsync</c>, <c>ChangeCredentialPasswordAdminAsync</c>) and
///         credential-password-gated session-key operations
///         (<c>CalculateSessionKeysSymmetricAsync</c>, <c>CalculateSessionKeysAsymmetricAsync</c>,
///         <c>ChangeCredentialPasswordAsync</c>).
///     </para>
///     <para>
///         <see cref="RetriesRemaining" /> exposes the parsed retry count as a typed value so
///         callers do not need to parse <see cref="Exception.Message" /> or separately call
///         <see cref="SWConstants.ExtractRetryCount" />.
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
    ///     Gets the number of verification attempts remaining before the credential or
    ///     management key is locked out.
    /// </summary>
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