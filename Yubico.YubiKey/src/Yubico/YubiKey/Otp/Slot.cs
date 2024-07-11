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

namespace Yubico.YubiKey.Otp
{
    /// <summary>
    /// An identifier for the configurable OTP slots.
    /// </summary>
    public enum Slot : byte
    {
        /// <summary>
        /// This is the default state for the <see cref="Slot"/> enumeration.
        /// If an operation is requested that requires a slot, this value will
        /// cause an exception.
        /// </summary>
        None = 0,

        /// <summary>
        /// Refers to the configuration slot that is activated by a short duration touch of the YubiKey.
        /// This is also sometimes referred to as "Slot 1".
        /// </summary>
        ShortPress = 1,

        /// <summary>
        /// Refers to the configuration slot that is activated by a longer duration touch of the YubiKey.
        /// This is also sometimes referred to as "Slot 2".
        /// </summary>
        LongPress = 2
    }
}
