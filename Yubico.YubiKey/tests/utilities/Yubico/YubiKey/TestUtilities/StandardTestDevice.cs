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

namespace Yubico.YubiKey.TestUtilities
{
    public enum StandardTestDevice
    {
        /// <summary>
        /// Major version 3, USB A keychain, not FIPS
        /// </summary>
        Fw3,

        /// <summary>
        /// Major version 4, USB A keychain, FIPS
        /// </summary>
        Fw4Fips,

        /// <summary>
        /// Major version 5, USB A keychain, not FIPS
        /// </summary>
        Fw5,

        /// <summary>
        /// Major version 5, USB A keychain, FIPS
        /// </summary>
        Fw5Fips,

        /// <summary>
        /// Major version 5, USB C Lightning, not FIPS
        /// </summary>
        Fw5ci
    }
}
