// Copyright 2022 Yubico AB
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

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// An enumeration denoting the FIDO2 PIN/UV enterprise attestation
    /// </summary>
    /// <remarks>
    /// The FIDO2 standard specifies that
    /// > An enterprise is some form of organization, often a business entity. An
    /// > enterprise context is in effect when a device, e.g., a computer, an
    /// > authenticator, etc., is controlled by an enterprise.
    /// >
    /// > An enterprise attestation is an attestation that may include uniquely
    /// > identifying information. This is intended for controlled deployments
    /// > within an enterprise where the organization wishes to tie registrations
    /// > to specific authenticators.
    /// >
    /// > The expectation is that enterprises will work directly with their
    /// > authenticator vendor(s) in order to source their enterprise attestation
    /// > capable authenticators.
    /// <para>
    /// When requesting a credential, it is possible to request one of the two
    /// enterprise attestations as well: Vendor and Platform. Use this enum to
    /// specify which attestation you want.
    /// </para>
    /// <para>
    /// If the YubiKey does not support enterprise attestation, requesting it
    /// will generate an error return. To know if enterprise attestation is
    /// supported, get the device info
    /// (<see cref="Fido2Session.GetAuthenticatorInfo"/>) and check the
    /// <c>Options</c> property of <see cref="AuthenticatorInfo"/>). If the
    /// option <c>"ep"</c> is listed and is <c>true</c>, then enterprise
    /// attestation is supported.
    /// </para>
    /// <para>
    /// The standard also specifies that an authenticator that supports only
    /// vendor-facilitated enterprise attestation can, when given a request for
    /// platform-managed enterprise attestation, return the vendor attestation.
    /// </para>
    /// </remarks>
    public enum EnterpriseAttestation
    {
        /// <summary>
        /// No enterprise attesteation is requested or used.
        /// </summary>
        None = 0,

        /// <summary>
        /// Identifier for Vendor-Facilitated Enterprise Attestation.
        /// </summary>
        VendorFacilitated = 1,

        /// <summary>
        /// Identifier for Platform-Managed Enterprise Attestation.
        /// </summary>
        PlatformManaged = 2,
    }
}
