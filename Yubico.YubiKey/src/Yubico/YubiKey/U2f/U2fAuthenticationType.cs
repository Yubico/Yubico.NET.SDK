// Copyright 2021 Yubico AB
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

namespace Yubico.YubiKey.U2f
{
    /// <summary>
    /// The types of U2F authentication supported by the FIDO U2F application on
    /// the YubiKey.
    /// </summary>
    /// <remarks>
    /// There are different types of FIDO U2F authentication. For example, it is
    /// possible to specify that a particular authentication will not enforce
    /// "User Presence", meaning the user does not need to touch the YubiKey or
    /// whatever other action is needed to prove the user is present for the
    /// operation.
    /// <para>
    /// This enum lists all such authentication options for U2F.
    /// </para>
    /// </remarks>
    public enum U2fAuthenticationType
    {
        // These values are defined to be the control byte of an authentication
        // request message. The standard defines them to be specific values.
        Unknown = 0,

        /// <summary>
        /// Indicates that an authentication operation will only check if the key
        /// handle was created by the YubiKey.
        /// </summary>
        CheckOnly = 0x07,

        /// <summary>
        /// Indicates that an authentication operation will not complete until
        /// the user has proved presence. Completion includes computing a
        /// signature.
        /// </summary>
        EnforceUserPresence = 0x03,

        /// <summary>
        /// Indicates that an authentication operation can complete even if the
        /// user has not proved presence. Completion includes computing a
        /// signature.
        /// </summary>
        DontEnforceUserPresence = 0x08,
    }
}
