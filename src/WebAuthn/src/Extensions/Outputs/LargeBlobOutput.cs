// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.WebAuthn.Extensions.Outputs;

/// <summary>
/// Output from the largeBlob extension during registration.
/// </summary>
/// <remarks>
/// Inspect this result even when <c>Required</c> was requested: current registration handling can
/// succeed with <c>Supported = false</c>. A missing extension output is distinct from this false result.
/// </remarks>
/// <param name="Supported">Whether large blob storage is supported for this credential.</param>
public sealed record class LargeBlobRegistrationOutput(bool Supported);

/// <summary>
/// Output from the largeBlob extension during authentication.
/// </summary>
/// <remarks>
/// <para>
/// A supported read keeps the output present even when there is no key, no stored data, or a CTAP,
/// CBOR, or invalid-operation storage failure. In those cases Blob is null; a stored empty blob
/// is present with length zero. Cancellation is not treated as a read result.
/// </para>
/// <para>
/// Retrieved data is decrypted application plaintext. The client transfers the fresh writable buffer
/// to the caller, who must zero sensitive data after last use. Disposing the client does not clear it.
/// Record copies share the buffer; direct construction retains supplied memory without copying.
/// </para>
/// </remarks>
/// <param name="Blob">Read data when supplied. Null is not a reliable diagnostic of why data is unavailable.</param>
/// <param name="Written">
/// True for a successful write, false for a caught CTAP or invalid-operation storage failure, and null
/// when no write result was produced. Cancellation propagates rather than becoming false.
/// </param>
public sealed record class LargeBlobAuthenticationOutput(
    Memory<byte>? Blob = null,
    bool? Written = null);
