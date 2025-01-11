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
    /// This enum lists the possible touch policies of a key in a PIV slot.
    /// </summary>
    public enum PivTouchPolicy
    {
        /// <summary>
        /// For key slots that do not have a touch policy, the policy is None.
        /// </summary>
        None = 0,

        /// <summary>
        /// Touch is never required for operations using the key in the given
        /// slot.
        /// </summary>
        Never = 1,

        /// <summary>
        /// Touch is always required for operations using the key in the given
        /// slot.
        /// </summary>
        Always = 2,

        /// <summary>
        /// Touch is cached for 15 seconds.
        /// </summary>
        Cached = 3,

        /// <summary>
        /// The touch policy is the default for the YubiKey.
        /// </summary>
        Default = 32,
    }
}
