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

namespace Yubico.YubiKit.Ctap2;

/// <summary>
/// CTAP2 status codes per FIDO Alliance specifications.
/// </summary>
/// <remarks>
/// <para>
/// Includes CTAP2.1 statuses and retained legacy identifiers. The meaning of some values differs
/// by revision; enum membership does not establish that every revision assigns that status.
/// These values do not collapse CTAPHID transport errors into CTAP CBOR response statuses.
/// </para>
/// <para>
/// CTAP 2.1: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#error-responses
/// </para>
/// <para>
/// CTAP2.0 legacy statuses: https://fidoalliance.org/specs/fido-v2.0-ps-20190130/fido-client-to-authenticator-protocol-v2.0-ps-20190130.html#error-responses
/// </para>
/// <para>
/// CTAP2.2 Review Draft, October 3, 2024: https://fidoalliance.org/specs/fido-v2.2-rd-20241003/fido-client-to-authenticator-protocol-v2.2-rd-20241003.html#error-responses
/// CTAP2.3 Proposed Standard, February 26, 2026: https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#error-responses
/// </para>
/// </remarks>
public enum CtapStatus : byte
{
    /// <summary>
    /// Indicates successful response.
    /// </summary>
    Success = 0x00,

    /// <summary>
    /// The command is not a valid CTAP command.
    /// </summary>
    InvalidCommand = 0x01,

    /// <summary>
    /// The command included an invalid parameter.
    /// </summary>
    InvalidParameter = 0x02,

    /// <summary>
    /// Invalid message or item length.
    /// </summary>
    InvalidLength = 0x03,

    /// <summary>
    /// Invalid message sequencing.
    /// </summary>
    InvalidSeq = 0x04,

    /// <summary>
    /// Message timed out.
    /// </summary>
    Timeout = 0x05,

    /// <summary>
    /// Channel busy.
    /// </summary>
    ChannelBusy = 0x06,

    /// <summary>
    /// Command requires channel lock.
    /// </summary>
    LockRequired = 0x0A,

    /// <summary>
    /// Command not allowed on this cid.
    /// </summary>
    InvalidChannel = 0x0B,

    /// <summary>
    /// Invalid/unexpected CBOR error.
    /// </summary>
    CborUnexpectedType = 0x11,

    /// <summary>
    /// Error when parsing CBOR.
    /// </summary>
    InvalidCbor = 0x12,

    /// <summary>
    /// Missing non-optional parameter.
    /// </summary>
    MissingParameter = 0x14,

    /// <summary>
    /// Limit for number of items exceeded.
    /// </summary>
    LimitExceeded = 0x15,

    /// <summary>
    /// Fingerprint database is full.
    /// </summary>
    FpDatabaseFull = 0x17,

    /// <summary>
    /// Large blob storage is full.
    /// </summary>
    LargeBlobStorageFull = 0x18,

    /// <summary>
    /// Valid credential found in the exclude list.
    /// </summary>
    CredentialExcluded = 0x19,

    /// <summary>
    /// Processing (Authenticator is busy).
    /// </summary>
    Processing = 0x21,

    /// <summary>
    /// Credential not valid for the authenticator.
    /// </summary>
    InvalidCredential = 0x22,

    /// <summary>
    /// Authentication is waiting for user interaction.
    /// </summary>
    UserActionPending = 0x23,

    /// <summary>
    /// Processing, long timeout (authenticator is busy, long timeout).
    /// </summary>
    OperationPending = 0x24,

    /// <summary>
    /// No request is pending.
    /// </summary>
    NoOperations = 0x25,

    /// <summary>
    /// Authenticator does not support requested algorithm.
    /// </summary>
    UnsupportedAlgorithm = 0x26,

    /// <summary>
    /// Not authorized for requested operation.
    /// </summary>
    OperationDenied = 0x27,

    /// <summary>
    /// Internal key storage is full.
    /// </summary>
    KeyStoreFull = 0x28,

    /// <summary>
    /// Unsupported option.
    /// </summary>
    UnsupportedOption = 0x2B,

    /// <summary>
    /// Not a valid option for current operation.
    /// </summary>
    InvalidOption = 0x2C,

    /// <summary>
    /// The outstanding CTAP request was cancelled; this is its response, not a response to HID CANCEL.
    /// </summary>
    KeepAliveCancel = 0x2D,

    /// <summary>
    /// No valid credentials provided.
    /// </summary>
    NoCredentials = 0x2E,

    /// <summary>
    /// A user action timeout occurred.
    /// </summary>
    UserActionTimeout = 0x2F,

    /// <summary>
    /// The command is not allowed in the current state, including an invalid continuation or late reset.
    /// </summary>
    NotAllowed = 0x30,

    /// <summary>
    /// PIN Invalid.
    /// </summary>
    PinInvalid = 0x31,

    /// <summary>
    /// PIN retries are exhausted; FIDO reset is destructive and requires coordinated timing/presence.
    /// </summary>
    PinBlocked = 0x32,

    /// <summary>
    /// PIN authentication,pinUvAuthParam, verification failed.
    /// </summary>
    PinAuthInvalid = 0x33,

    /// <summary>
    /// PIN authentication is temporarily blocked; CTAP2.1 requires a power cycle before further PIN attempts.
    /// </summary>
    PinAuthBlocked = 0x34,

    /// <summary>
    /// No PIN has been set.
    /// </summary>
    PinNotSet = 0x35,

    /// <summary>
    /// A pinUvAuthToken is required for the selected operation.
    /// </summary>
    /// <remarks>CTAP2.0 names value 0x36 PIN_REQUIRED; later revisions use PUAT_REQUIRED.</remarks>
    PuatRequired = 0x36,

    /// <summary>
    /// PIN policy violation; CTAP2.1 describes minimum length, while CTAP2.3 also names complexity.
    /// </summary>
    PinPolicyViolation = 0x37,

    /// <summary>
    /// Legacy CTAP2.0 PIN_TOKEN_EXPIRED: the authenticator's pinToken expired.
    /// </summary>
    /// <remarks>Value 0x38 is reserved in CTAP2.1, the CTAP2.2 October 2024 Review Draft and
    /// CTAP2.3 February 2026 Proposed Standard. This is not a CTAP2.2 status addition.</remarks>
    PinTokenExpired = 0x38,

    /// <summary>
    /// Authenticator cannot handle this request due to memory constraints.
    /// </summary>
    RequestTooLarge = 0x39,

    /// <summary>
    /// The current operation has timed out.
    /// </summary>
    ActionTimeout = 0x3A,

    /// <summary>
    /// User presence is required for the requested operation.
    /// </summary>
    UpRequired = 0x3B,

    /// <summary>
    /// built-in user verification is disabled.
    /// </summary>
    UvBlocked = 0x3C,

    /// <summary>
    /// A checksum did not match.
    /// </summary>
    IntegrityFailure = 0x3D,

    /// <summary>
    /// The requested subcommand is either invalid or not implemented.
    /// </summary>
    InvalidSubcommand = 0x3E,

    /// <summary>
    /// built-in user verification unsuccessful. The platform SHOULD retry.
    /// </summary>
    UvInvalid = 0x3F,

    /// <summary>
    /// The permissions parameter contains an unauthorized permission.
    /// </summary>
    UnauthorizedPermission = 0x40,

    /// <summary>
    /// Other unspecified error.
    /// </summary>
    Other = 0x7F,

    /// <summary>
    /// Start of the extension-specific error range (0xE0–0xEF).
    /// </summary>
    ExtensionFirst = 0xE0,

    /// <summary>
    /// End of the extension-specific error range (0xE0–0xEF).
    /// </summary>
    ExtensionLast = 0xEF,

    /// <summary>
    /// Vendor specific error range start.
    /// </summary>
    VendorFirst = 0xF0,

    /// <summary>
    /// Vendor specific error range end.
    /// </summary>
    VendorLast = 0xFF,
}