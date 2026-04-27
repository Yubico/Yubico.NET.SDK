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

namespace Yubico.YubiKit.WebAuthn;

/// <summary>
/// Error codes for WebAuthn Client operations.
/// </summary>
public enum WebAuthnClientErrorCode
{
    /// <summary>
    /// The request is invalid or malformed.
    /// </summary>
    InvalidRequest,

    /// <summary>
    /// The operation cannot be performed in the current state.
    /// </summary>
    InvalidState,

    /// <summary>
    /// The operation is not allowed (e.g., user verification failed, credential excluded).
    /// </summary>
    NotAllowed,

    /// <summary>
    /// A constraint was violated (e.g., timeout, credential limit).
    /// </summary>
    Constraint,

    /// <summary>
    /// The requested operation or feature is not supported.
    /// </summary>
    NotSupported,

    /// <summary>
    /// A security-related error occurred (e.g., PIN auth invalid, tampering detected).
    /// </summary>
    Security,

    /// <summary>
    /// The operation was cancelled by the caller (e.g., via CancellationToken).
    /// </summary>
    Cancelled,

    /// <summary>
    /// An unknown or unclassified error occurred.
    /// </summary>
    Unknown
}

/// <summary>
/// Exception thrown by WebAuthn Client operations.
/// </summary>
public sealed class WebAuthnClientError : Exception
{
    /// <summary>
    /// Gets the error code for this exception.
    /// </summary>
    public WebAuthnClientErrorCode Code { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="WebAuthnClientError"/> with the specified error code and message.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    public WebAuthnClientError(WebAuthnClientErrorCode code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="WebAuthnClientError"/> with the specified error code, message, and inner exception.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public WebAuthnClientError(WebAuthnClientErrorCode code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}
