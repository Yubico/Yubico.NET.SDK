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

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// This enum lists all the possible "credProtect" extension policies
    /// supported by the FIDO2 standard.
    /// </summary>
    public enum CredProtectPolicy
    {
        /// <summary>
        /// No policy is specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// This policy specifies that the RP is not requiring UV.
        /// </summary>
        UserVerificationOptional = 1,

        /// <summary>
        /// This policy specifies that UV is optional if a credential ID is
        /// supplied at the time of Get Assertion, but required if no credential
        /// ID is supplied.
        /// </summary>
        UserVerificationOptionalWithCredentialIDList = 2,

        /// <summary>
        /// This policy specifies that UV is required to get an assertion.
        /// </summary>
        UserVerificationRequired = 3
    }
}
