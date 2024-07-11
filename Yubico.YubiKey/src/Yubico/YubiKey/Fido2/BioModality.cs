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
    /// This enum lists all the possible biometric methods supported by the FIDO2
    /// standard.
    /// </summary>
    /// <remarks>
    /// When you call <see cref="Fido2Session.GetBioModality"/> it will return this
    /// enum, indicating what biometric method the connected YubiKey supports. If
    /// the YubiKey is not a BIO series device, it will return <c>None</c>.
    /// </remarks>
    public enum BioModality
    {
        /// <summary>
        /// Indicates that a YubiKey does not support any biometric method.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that a YubiKey supports the fingerprint biometric method.
        /// </summary>
        Fingerprint = 1
    }
}
