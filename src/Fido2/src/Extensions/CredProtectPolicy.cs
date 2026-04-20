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

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Credential protection policy for the credProtect extension.
/// </summary>
/// <remarks>
/// <para>
/// The credential protection policy specifies when credentials can be used
/// in assertions. Higher values provide stronger protection but may limit
/// compatibility with some relying parties.
/// </para>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#sctn-credProtect-extension
/// </para>
/// </remarks>
public enum CredProtectPolicy
{
    /// <summary>
    /// User verification is optional.
    /// </summary>
    /// <remarks>
    /// Credentials can be discovered and used without user verification.
    /// This is the default for non-discoverable credentials.
    /// </remarks>
    UserVerificationOptional = 1,
    
    /// <summary>
    /// User verification is optional with credential ID list.
    /// </summary>
    /// <remarks>
    /// For discoverable credentials, user verification is required if
    /// no credential ID is provided (i.e., when listing discoverable credentials).
    /// When a credential ID is provided in allowList, UV is optional.
    /// This is the default for discoverable credentials on most authenticators.
    /// </remarks>
    UserVerificationOptionalWithCredentialIdList = 2,
    
    /// <summary>
    /// User verification is always required.
    /// </summary>
    /// <remarks>
    /// The credential can only be used with user verification. Assertions
    /// without UV will fail. This provides the highest protection level.
    /// </remarks>
    UserVerificationRequired = 3
}
