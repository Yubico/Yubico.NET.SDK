// Copyright 2023 Yubico AB
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

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Extension methods to operate on the PivAlgorithm enum.
    /// </summary>
    public static class PinUvAuthTokenPermissionsExtensions
    {
        /// <summary>
        /// Determines if the given permission is one for which a Relying Party
        /// ID is required, optional, or ignored.
        /// </summary>
        /// <remarks>
        /// Generally, this will be called with a single permission. That is, the
        /// check will be for a <c>permissions</c> variable of only
        /// <c>MakeCredential</c> or <c>CredentialManagement</c>. However, this
        /// extension will check for all permissions. If one permission set
        /// requires the RP ID, then this will return <c>Required</c>.
        /// </remarks>
        /// <param name="permissions">
        /// The list of permissions to check.
        /// </param>
        /// <returns>
        /// A <c>RequirementValue</c> indicating whether the Relying Party ID is
        /// required, optional, or ignored for the permission.
        /// </returns>
        public static RequirementValue GetRpIdRequirement(this PinUvAuthTokenPermissions permissions)
        {
            if (permissions.HasFlag(PinUvAuthTokenPermissions.MakeCredential)
                || permissions.HasFlag(PinUvAuthTokenPermissions.GetAssertion))
            {
                return RequirementValue.Required;
            }

            if (permissions.HasFlag(PinUvAuthTokenPermissions.CredentialManagement))
            {
                return RequirementValue.Optional;
            }

            return RequirementValue.Ignored;
        }
    }
}
