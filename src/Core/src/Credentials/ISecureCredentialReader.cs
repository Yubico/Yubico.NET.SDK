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

using System.Buffers;

namespace Yubico.YubiKit.Core.Credentials;

/// <summary>
/// Reads credential bytes into a disposable memory owner.
/// </summary>
/// <remarks>
/// <para>
/// Dispose each returned <see cref="IMemoryOwner{T}"/> to clear its credential bytes.
/// Input storage depends on the implementation: <see cref="ConsoleCredentialReader"/> uses
/// pooled characters interactively, but redirected input passes through a managed string.
/// </para>
/// <para>
/// This is a synchronous, application-initiated terminal input helper with display and encoding
/// options. It is distinct from <see cref="ICredentialPrompt"/>, which is an asynchronous
/// SDK-to-application callback carrying protocol context for secrets requested during an operation.
/// </para>
/// <para>
/// This method borrows an open PIV session. The credential owner remains alive until verification
/// completes and is then cleared, including on failure. The caller retains session ownership.
/// <code>
/// static async Task ReadAndVerifyPinAsync(
///     Yubico.YubiKit.Piv.IPivSession pivSession, CancellationToken cancellationToken)
/// {
///     var reader = new ConsoleCredentialReader();
///     using var credential = reader.ReadCredential(CredentialReaderOptions.ForPin(), cancellationToken);
///     if (credential is not null)
///     {
///         await pivSession.VerifyPinAsync(credential.Memory, cancellationToken);
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public interface ISecureCredentialReader
{
    /// <summary>
    /// Reads a credential from the user with the specified options.
    /// </summary>
    /// <param name="options">Configuration for the credential input behavior.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// An <see cref="IMemoryOwner{T}"/> containing the credential bytes, or <c>null</c>
    /// if input is declined. The console reader also returns null on end of input or rejected input.
    /// </returns>
    /// <remarks>
    /// The returned memory owner automatically zeros its contents when disposed.
    /// Callers must dispose the result to ensure sensitive data is cleared.
    /// </remarks>
    /// <exception cref="OperationCanceledException">The implementation observes cancellation. The console reader checks while waiting for an interactive key, rather than during redirected reads.</exception>
    IMemoryOwner<byte>? ReadCredential(CredentialReaderOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a credential with confirmation (user must enter it twice).
    /// </summary>
    /// <param name="options">Configuration for the credential input behavior.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// An <see cref="IMemoryOwner{T}"/> containing the credential bytes if both entries
    /// matched, or <c>null</c> if an entry was declined, rejected, unavailable, or did not match.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Uses timing-safe comparison to verify the credentials match.
    /// </para>
    /// <para>
    /// The console reader retains the first entry while acquiring the second. It disposes the second
    /// entry after comparison, transfers the first to the caller on a match, and disposes the first
    /// on every other exit. The caller must dispose the successful result.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">The implementation observes cancellation while reading an entry.</exception>
    IMemoryOwner<byte>? ReadCredentialWithConfirmation(CredentialReaderOptions options, CancellationToken cancellationToken = default);
}