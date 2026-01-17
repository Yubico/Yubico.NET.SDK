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

namespace Yubico.YubiKit.Fido2.Pin;

/// <summary>
/// Sub-command codes for the authenticatorClientPin command.
/// </summary>
/// <remarks>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#authenticatorClientPIN
/// </para>
/// </remarks>
public static class ClientPinSubCommand
{
    /// <summary>
    /// Get PIN retries remaining.
    /// </summary>
    public const int GetRetries = 0x01;
    
    /// <summary>
    /// Get authenticator's key agreement public key.
    /// </summary>
    public const int GetKeyAgreement = 0x02;
    
    /// <summary>
    /// Set a new PIN (first-time PIN setup).
    /// </summary>
    public const int SetPin = 0x03;
    
    /// <summary>
    /// Change the existing PIN.
    /// </summary>
    public const int ChangePin = 0x04;
    
    /// <summary>
    /// Get a PIN token using PIN.
    /// </summary>
    public const int GetPinToken = 0x05;
    
    /// <summary>
    /// Get a PIN/UV auth token using UV (CTAP 2.1).
    /// </summary>
    public const int GetPinUvAuthTokenUsingUvWithPermissions = 0x06;
    
    /// <summary>
    /// Get UV retries remaining (CTAP 2.1).
    /// </summary>
    public const int GetUvRetries = 0x07;
    
    /// <summary>
    /// Get a PIN/UV auth token using PIN with permissions (CTAP 2.1).
    /// </summary>
    public const int GetPinUvAuthTokenUsingPinWithPermissions = 0x09;
}

/// <summary>
/// CBOR map keys for authenticatorClientPin command parameters.
/// </summary>
internal static class ClientPinParam
{
    /// <summary>PIN/UV protocol version (uint, required)</summary>
    public const int PinUvAuthProtocol = 0x01;
    
    /// <summary>Sub-command code (uint, required)</summary>
    public const int SubCommand = 0x02;
    
    /// <summary>Platform key agreement key (COSE_Key)</summary>
    public const int KeyAgreement = 0x03;
    
    /// <summary>PIN/UV auth parameter (byte string)</summary>
    public const int PinUvAuthParam = 0x04;
    
    /// <summary>Encrypted new PIN (byte string)</summary>
    public const int NewPinEnc = 0x05;
    
    /// <summary>Encrypted PIN hash (byte string)</summary>
    public const int PinHashEnc = 0x06;
    
    /// <summary>Permissions bit field (uint, CTAP 2.1)</summary>
    public const int Permissions = 0x09;
    
    /// <summary>Relying party ID for permission scope (text string, CTAP 2.1)</summary>
    public const int RpId = 0x0A;
}

/// <summary>
/// CBOR map keys for authenticatorClientPin response.
/// </summary>
internal static class ClientPinResponse
{
    /// <summary>Authenticator's key agreement key (COSE_Key)</summary>
    public const int KeyAgreement = 0x01;
    
    /// <summary>Encrypted PIN/UV auth token (byte string)</summary>
    public const int PinUvAuthToken = 0x02;
    
    /// <summary>PIN retries remaining (uint)</summary>
    public const int PinRetries = 0x03;
    
    /// <summary>Whether a power cycle is required (bool)</summary>
    public const int PowerCycleState = 0x04;
    
    /// <summary>UV retries remaining (uint)</summary>
    public const int UvRetries = 0x05;
}

/// <summary>
/// Permission flags for PIN/UV auth tokens (CTAP 2.1).
/// </summary>
/// <remarks>
/// <para>
/// Permissions control what operations a PIN/UV auth token can authorize.
/// Multiple permissions can be combined using bitwise OR.
/// </para>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#permissions
/// </para>
/// </remarks>
[Flags]
public enum PinUvAuthTokenPermissions : uint
{
    /// <summary>No permissions.</summary>
    None = 0x00,
    
    /// <summary>Permission to execute authenticatorMakeCredential.</summary>
    MakeCredential = 0x01,
    
    /// <summary>Permission to execute authenticatorGetAssertion.</summary>
    GetAssertion = 0x02,
    
    /// <summary>Permission to execute authenticatorCredentialManagement.</summary>
    CredentialManagement = 0x04,
    
    /// <summary>Permission to execute authenticatorBioEnrollment.</summary>
    BioEnrollment = 0x08,
    
    /// <summary>Permission to execute large blob operations.</summary>
    LargeBlobWrite = 0x10,
    
    /// <summary>Permission to execute authenticatorConfig.</summary>
    AuthenticatorConfig = 0x20,
}
