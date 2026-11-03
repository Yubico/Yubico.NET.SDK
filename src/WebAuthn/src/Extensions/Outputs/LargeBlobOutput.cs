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
/// A missing largeBlob output, missing read data, a valid empty blob, and a failed read are distinct
/// conditions. Do not use a null value alone to infer support, absence of stored data, or failure.
/// Current read handling can omit the output on an InvalidOperationException or propagate storage
/// errors after the assertion succeeds. When storage returns no blob, the current array-to-memory
/// conversion produces a present value of length zero. <c>Blob.HasValue</c> and the blob's length
/// therefore do not distinguish that result from a stored empty blob.
/// </para>
/// <para>
/// Retrieved data is decrypted application plaintext and may be sensitive. SDK-produced results have
/// no disposal or zeroing guarantee; disposing the client or clearing a caller-created copy does not
/// clear the SDK-created array. Directly constructed results may wrap caller-owned memory, whose
/// lifetime and cleanup remain the caller's responsibility. Do not log sensitive blob contents.
/// </para>
/// </remarks>
/// <param name="Blob">Read data when supplied. Null is not a reliable diagnostic of why data is unavailable.</param>
/// <param name="Written">
/// True for a successful write, false for a caught CTAP or invalid-operation storage failure, and null
/// when no write result was produced. Cancellation propagates rather than becoming false.
/// </param>
public sealed record class LargeBlobAuthenticationOutput(
    ReadOnlyMemory<byte>? Blob = null,
    bool? Written = null);