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
/// <para>
/// Discriminated status union for <see cref="WebAuthnClient"/> streaming operations.
/// Consumers use pattern matching to handle each status variant.
/// </para>
/// <para>
/// These statuses report progress; they never gather input. A PIN, when one is needed, comes
/// from the PIN bytes passed to the operation or from the client's configured
/// <see cref="Yubico.YubiKit.Core.Credentials.ICredentialPrompt"/>. To abandon an operation,
/// cancel the token supplied to it.
/// </para>
/// </remarks>
public abstract record WebAuthnStatus;

/// <summary>
/// The operation is in progress (processing internal state).
/// </summary>
public sealed record WebAuthnStatusProcessing : WebAuthnStatus;

/// <summary>
/// The authenticator is waiting for the user to touch it.
/// </summary>
/// <remarks>
/// Emitted when the authenticator reports that it is awaiting user presence, which is the moment
/// to show a "touch your key" prompt. Only the HID transport signals this; over SmartCard the
/// ceremony appears as continuous <see cref="WebAuthnStatusProcessing"/>.
/// </remarks>
public sealed record WebAuthnStatusWaitingForUser : WebAuthnStatus;

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