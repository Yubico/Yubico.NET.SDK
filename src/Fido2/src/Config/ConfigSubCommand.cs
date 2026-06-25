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

namespace Yubico.YubiKit.Fido2.Config;

/// <summary>
/// CTAP2.1 authenticatorConfig sub-commands.
/// </summary>
/// <remarks>
/// <para>
/// See: CTAP 2.1 Section 6.10 - authenticatorConfig (0x0D)
/// </para>
/// </remarks>
internal static class ConfigSubCommand
{
    /// <summary>
    /// Enable enterprise attestation (0x01).
    /// </summary>
    /// <remarks>
    /// Enables the authenticator to return enterprise attestation during
    /// credential creation when requested. Requires ep option support.
    /// </remarks>
    public const byte EnableEnterpriseAttestation = 0x01;
    
    /// <summary>
    /// Toggle always require UV (0x02).
    /// </summary>
    /// <remarks>
    /// Toggles the alwaysUv option. When enabled, user verification is
    /// always required regardless of the uv parameter in requests.
    /// </remarks>
    public const byte ToggleAlwaysUv = 0x02;
    
    /// <summary>
    /// Set minimum PIN length (0x03).
    /// </summary>
    /// <remarks>
    /// Sets the minimum PIN length required by the authenticator.
    /// The new value must be greater than or equal to the current value.
    /// </remarks>
    public const byte SetMinPinLength = 0x03;
    
    /// <summary>
    /// Vendor specific prototype command for config (0x04).
    /// </summary>
    /// <remarks>
    /// Used by some implementations for vendor-specific configuration.
    /// </remarks>
    public const byte VendorPrototype = 0x04;
    
    /// <summary>
    /// Set minimum PIN length with RP IDs list (0x05).
    /// </summary>
    /// <remarks>
    /// Sets the minimum PIN length and optionally specifies a list of
    /// RP IDs that are allowed to observe the current minimum PIN length.
    /// Requires setMinPINLength option support.
    /// </remarks>
    public const byte SetMinPinLengthRpIds = 0x05;
}
