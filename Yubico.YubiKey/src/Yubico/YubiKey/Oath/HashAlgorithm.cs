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

namespace Yubico.YubiKey.Oath
{
    /// <summary>
    /// The types of hash algorithms that are used by OATH credentials.
    /// </summary>
    public enum HashAlgorithm
    {
        None = 0,
        Sha1 = 0x01,
        Sha256 = 0x02,
        Sha512 = 0x03
    }
}
