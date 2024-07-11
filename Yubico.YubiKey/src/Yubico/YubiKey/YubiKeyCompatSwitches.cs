// Copyright 2024 Yubico AB
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

namespace Yubico.YubiKey
{
    /// <summary>
    /// Compatibility switch names that can be used with `AppContext.SetSwitch` to control breaking behavioral changes
    /// within the `Yubico.YubiKey` layer.
    /// </summary>
    public static class YubiKeyCompatSwitches
    {
        /// <summary>
        /// If set to true, the SDK will ignore whether a YubiKey is capable of faster USB interface switching
        /// and always use the 3-second reclaim timeout.
        /// </summary>
        public const string UseOldReclaimTimeoutBehavior = nameof(UseOldReclaimTimeoutBehavior);
    }
}
