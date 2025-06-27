// Copyright 2025 Yubico AB
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

namespace Yubico.Core.Cryptography
{
    /// <summary>
    /// This enumeration lists the block cipher algorithms supported by the
    /// <see cref="ICmacPrimitives"/> interface.
    /// </summary>
    public enum CmacBlockCipherAlgorithm
    {
        /// <summary>
        /// Use this enum value in order to specify CMAC with AES-128.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Use this enum value in order to specify CMAC with AES-128.
        /// </summary>
        Aes128 = 1,

        /// <summary>
        /// Use this enum value in order to specify CMAC with AES-192.
        /// </summary>
        Aes192 = 2,

        /// <summary>
        /// Use this enum value in order to specify CMAC with AES-256.
        /// </summary>
        Aes256 = 3,
    }
}
