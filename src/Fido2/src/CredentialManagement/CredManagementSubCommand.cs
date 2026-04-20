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

namespace Yubico.YubiKit.Fido2.CredentialManagement;

/// <summary>
/// CTAP2 authenticatorCredentialManagement sub-command codes.
/// </summary>
/// <remarks>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#authenticatorCredentialManagement
/// </remarks>
internal static class CredManagementSubCommand
{
    /// <summary>
    /// getCredsMetadata (0x01) - Get credential storage metadata.
    /// </summary>
    public const byte GetCredsMetadata = 0x01;
    
    /// <summary>
    /// enumerateRPsBegin (0x02) - Start enumerating relying parties.
    /// </summary>
    public const byte EnumerateRPsBegin = 0x02;
    
    /// <summary>
    /// enumerateRPsGetNextRP (0x03) - Get next relying party.
    /// </summary>
    public const byte EnumerateRPsGetNextRP = 0x03;
    
    /// <summary>
    /// enumerateCredentialsBegin (0x04) - Start enumerating credentials for an RP.
    /// </summary>
    public const byte EnumerateCredentialsBegin = 0x04;
    
    /// <summary>
    /// enumerateCredentialsGetNextCredential (0x05) - Get next credential.
    /// </summary>
    public const byte EnumerateCredentialsGetNextCredential = 0x05;
    
    /// <summary>
    /// deleteCredential (0x06) - Delete a credential.
    /// </summary>
    public const byte DeleteCredential = 0x06;
    
    /// <summary>
    /// updateUserInformation (0x07) - Update user information for a credential.
    /// </summary>
    public const byte UpdateUserInformation = 0x07;
}
