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

namespace Yubico.YubiKey.Piv
{
    /// <summary>
    /// This enum lists the possible PIN policies of a key in a PIV slot.
    /// </summary>
    public enum PivPinPolicy
    {
        /// <summary>
        /// For key slots that do not have a PIN policy, the policy is None.
        /// </summary>
        None = 0,

        /// <summary>
        /// The PIN is never checked for operations using the key in the
        /// given slot.
        /// </summary>
        Never = 1,

        /// <summary>
        /// The PIN is checked for only the first operation in a session
        /// using the key in the given slot.
        /// </summary>
        Once = 2,

        /// <summary>
        /// The PIN is verified before every operation that uses the key in
        /// the given slot.
        /// </summary>
        Always = 3,

        /// <summary>
        /// Biometric verification succeeds or the PIN is checked for only
        /// the first operation in a session using the key in the given slot.
        /// </summary>
        MatchOnce = 4,

        /// <summary>
        /// Biometric verification succeeds or the PIN is verified before every
        /// operation that uses the key in the given slot.
        /// </summary>
        MatchAlways = 5,

        /// <summary>
        /// The PIN policy is the default for the YubiKey.
        /// </summary>
        Default = 32,
    }
}
