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

namespace Yubico.YubiKit.Fido2.Ctap;

/// <summary>
/// Exception thrown when a CTAP command fails.
/// </summary>
public class CtapException : Exception
{
    /// <summary>
    /// Gets the CTAP status code from the authenticator.
    /// </summary>
    public CtapStatus Status { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CtapException"/> class.
    /// </summary>
    /// <param name="status">The CTAP status code.</param>
    public CtapException(CtapStatus status)
        : base(GetMessage(status))
    {
        Status = status;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CtapException"/> class.
    /// </summary>
    /// <param name="status">The CTAP status code.</param>
    /// <param name="message">The error message.</param>
    public CtapException(CtapStatus status, string message)
        : base(message)
    {
        Status = status;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CtapException"/> class.
    /// </summary>
    /// <param name="status">The CTAP status code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public CtapException(CtapStatus status, string message, Exception innerException)
        : base(message, innerException)
    {
        Status = status;
    }
    
    /// <summary>
    /// Throws a <see cref="CtapException"/> if the status is not <see cref="CtapStatus.Success"/>.
    /// </summary>
    /// <param name="status">The status to check.</param>
    public static void ThrowIfError(CtapStatus status)
    {
        if (status != CtapStatus.Success)
        {
            throw new CtapException(status);
        }
    }
    
    /// <summary>
    /// Throws a <see cref="CtapException"/> if the status is not <see cref="CtapStatus.Success"/>.
    /// </summary>
    /// <param name="status">The status byte to check.</param>
    public static void ThrowIfError(byte status)
    {
        ThrowIfError((CtapStatus)status);
    }
    
    private static string GetMessage(CtapStatus status) => status switch
    {
        CtapStatus.Success => "Success",
        CtapStatus.InvalidCommand => "Invalid CTAP command",
        CtapStatus.InvalidParameter => "Invalid parameter",
        CtapStatus.InvalidLength => "Invalid length",
        CtapStatus.InvalidSeq => "Invalid message sequence",
        CtapStatus.Timeout => "Operation timed out",
        CtapStatus.ChannelBusy => "Channel busy",
        CtapStatus.LockRequired => "Channel lock required",
        CtapStatus.InvalidChannel => "Invalid channel ID",
        CtapStatus.CborUnexpectedType => "Unexpected CBOR type",
        CtapStatus.InvalidCbor => "Invalid CBOR",
        CtapStatus.MissingParameter => "Missing required parameter",
        CtapStatus.LimitExceeded => "Limit exceeded",
        CtapStatus.FpDatabaseFull => "Fingerprint database full",
        CtapStatus.LargeBlobStorageFull => "Large blob storage full",
        CtapStatus.CredentialExcluded => "Credential excluded",
        CtapStatus.Processing => "Authenticator is processing",
        CtapStatus.InvalidCredential => "Invalid credential",
        CtapStatus.UserActionPending => "User action pending",
        CtapStatus.OperationPending => "Operation pending",
        CtapStatus.NoOperations => "No operations pending",
        CtapStatus.UnsupportedAlgorithm => "Unsupported algorithm",
        CtapStatus.OperationDenied => "Operation denied",
        CtapStatus.KeyStoreFull => "Key storage full",
        CtapStatus.UnsupportedOption => "Unsupported option",
        CtapStatus.InvalidOption => "Invalid option",
        CtapStatus.KeepAliveCancel => "Keep alive cancelled",
        CtapStatus.NoCredentials => "No credentials found",
        CtapStatus.UserActionTimeout => "User action timeout",
        CtapStatus.NotAllowed => "Operation not allowed",
        CtapStatus.PinInvalid => "PIN is invalid",
        CtapStatus.PinBlocked => "PIN is blocked",
        CtapStatus.PinAuthInvalid => "PIN authentication failed",
        CtapStatus.PinAuthBlocked => "PIN authentication blocked",
        CtapStatus.PinNotSet => "PIN not set",
        CtapStatus.PuvathRequired => "PIN/UV auth token required",
        CtapStatus.PinPolicyViolation => "PIN policy violation",
        CtapStatus.RequestTooLarge => "Request too large",
        CtapStatus.ActionTimeout => "Action timeout",
        CtapStatus.UpRequired => "User presence required",
        CtapStatus.UvBlocked => "User verification blocked",
        CtapStatus.IntegrityFailure => "Integrity check failed",
        CtapStatus.InvalidSubcommand => "Invalid subcommand",
        CtapStatus.UvInvalid => "User verification failed",
        CtapStatus.UnauthorizedPermission => "Unauthorized permission",
        CtapStatus.Other => "Unspecified error",
        _ => $"Unknown CTAP error: 0x{(byte)status:X2}"
    };
}
