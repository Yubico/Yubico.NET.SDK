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

namespace Yubico.YubiKit.Ctap2.Config;

/// <summary>
/// CTAP2.1 authenticatorConfig sub-commands.
/// </summary>
/// <remarks>
/// <para>
/// See CTAP 2.1 (2022-06-21 errata), Section 6.11, authenticatorConfig (0x0D).
/// The active helper uses only subcommands 1-3. The unused constants 4 and 5 below do not
/// match subcommands defined in that revision and must not be used as standard identifiers.
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
    /// Toggles the alwaysUv option for credential creation/authentication.
    /// Disabling can be refused; read fresh GetInfo to reconcile state.
    /// </remarks>
    public const byte ToggleAlwaysUv = 0x02;

    /// <summary>
    /// Set minimum PIN length (0x03).
    /// </summary>
    /// <remarks>
    /// Sets the ClientPIN minimum in Unicode code points and optionally its RP reporting list/force-change policy.
    /// The new value must be greater than or equal to the current value.
    /// </remarks>
    public const byte SetMinPinLength = 0x03;

    /// <summary>
    /// Unused local prototype constant (0x04), not the standard vendorPrototype identifier.
    /// </summary>
    /// <remarks>
    /// CTAP 2.1 (2022-06-21 errata), Section 6.11.3, assigns vendorPrototype=0xFF.
    /// No vendor wire definition is established for this local value.
    /// </remarks>
    public const byte VendorPrototype = 0x04;

    /// <summary>
    /// Unused local minimum-PIN/RP-list constant (0x05).
    /// </summary>
    /// <remarks>
    /// The cited CTAP 2.1 revision defines RP IDs as parameters of subcommand 3,
    /// not a separate subcommand 5. AuthenticatorConfig uses that standard parameter map.
    /// </remarks>
    public const byte SetMinPinLengthRpIds = 0x05;
}