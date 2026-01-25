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
/// Provides secure credential input that never exposes sensitive data as strings.
/// </summary>
/// <remarks>
/// <para>
/// All credential data is returned as <see cref="IMemoryOwner{T}"/> which automatically
/// zeros sensitive memory when disposed. Callers should use the <c>using</c> pattern
/// to ensure credentials are cleared as soon as possible.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var reader = new ConsoleCredentialReader();
/// using var credential = reader.ReadCredential(CredentialReaderOptions.ForPin());
/// if (credential is not null)
/// {
///     // Use credential.Memory.Span to access the PIN bytes
///     pivSession.VerifyPin(credential.Memory.Span);
/// } // Memory is automatically zeroed here
/// </code>
/// </para>
/// </remarks>
public interface ISecureCredentialReader
{
    /// <summary>
    /// Reads a credential from the user with the specified options.
    /// </summary>
    /// <param name="options">Configuration for the credential input behavior.</param>
    /// <returns>
    /// An <see cref="IMemoryOwner{T}"/> containing the credential bytes, or <c>null</c>
    /// if the user cancelled the input (e.g., pressed Escape).
    /// </returns>
    /// <remarks>
    /// The returned memory owner automatically zeros its contents when disposed.
    /// Callers must dispose the result to ensure sensitive data is cleared.
    /// </remarks>
    IMemoryOwner<byte>? ReadCredential(CredentialReaderOptions options);

    /// <summary>
    /// Reads a credential with confirmation (user must enter it twice).
    /// </summary>
    /// <param name="options">Configuration for the credential input behavior.</param>
    /// <returns>
    /// An <see cref="IMemoryOwner{T}"/> containing the credential bytes if both entries
    /// matched, or <c>null</c> if the user cancelled or entries did not match.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Uses timing-safe comparison to verify the credentials match.
    /// </para>
    /// <para>
    /// The first entry buffer is always zeroed before the second prompt,
    /// and both buffers are zeroed if they don't match.
    /// </para>
    /// </remarks>
    IMemoryOwner<byte>? ReadCredentialWithConfirmation(CredentialReaderOptions options);
}
