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

namespace Yubico.YubiKit.WebAuthn.Client.Status;

/// <summary>
/// Base type for WebAuthn operation status updates in a streaming context.
/// </summary>
/// <remarks>
/// Discriminated status union for <see cref="WebAuthnClient"/> streaming operations.
/// Consumers use pattern matching to handle each status variant.
/// </remarks>
public abstract record WebAuthnStatus;

/// <summary>
/// The operation is in progress (processing internal state).
/// </summary>
public sealed record WebAuthnStatusProcessing : WebAuthnStatus;

/// <summary>
/// Waiting for user interaction (touch, biometric, or other authenticator prompt).
/// </summary>
/// <param name="Cancel">Call to cancel the wait.</param>
public sealed record WebAuthnStatusWaitingForUser(Func<ValueTask> Cancel) : WebAuthnStatus;

/// <summary>
/// The operation is requesting a decision on whether to use user verification.
/// </summary>
/// <param name="SetUseUv">Call with true to use UV, false to skip UV.</param>
public sealed record WebAuthnStatusRequestingUv(Func<bool, ValueTask> SetUseUv) : WebAuthnStatus;

/// <summary>
/// The operation requires a PIN to proceed.
/// </summary>
/// <param name="SubmitPin">Submit PIN bytes (UTF-8 encoded) to continue.</param>
/// <param name="Cancel">Call to cancel PIN entry and abort the operation.</param>
public sealed record WebAuthnStatusRequestingPin(
    Func<ReadOnlyMemory<byte>, ValueTask> SubmitPin,
    Func<ValueTask> Cancel) : WebAuthnStatus;

/// <summary>
/// The operation has finished successfully.
/// </summary>
/// <typeparam name="T">The result type (RegistrationResponse or IReadOnlyList&lt;MatchedCredential&gt;).</typeparam>
/// <param name="Result">The successful operation result.</param>
public sealed record WebAuthnStatusFinished<T>(T Result) : WebAuthnStatus;

/// <summary>
/// The operation has failed with an error.
/// </summary>
/// <param name="Error">The error that caused the failure.</param>
public sealed record WebAuthnStatusFailed(WebAuthnClientError Error) : WebAuthnStatus;
