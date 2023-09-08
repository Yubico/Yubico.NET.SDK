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

using System;

namespace Yubico.Core.Cryptography
{
    /// <summary>
    /// Extension methods to operate on the CmacBlockCipherAlgorithm enum.
    /// </summary>
    public static class CmacBlockCipherAlgorithmExtensions
    {
        /// <summary>
        /// Returns the length, in bytes, of the key to be used in the operations
        /// given the specified underlying block cipher algorithm.
        /// </summary>
        /// <param name="algorithm">
        /// The algorithm name to check.
        /// </param>
        /// <returns>
        /// An int, the length, in bytes, of the key for the specified block
        /// cipher algorithm.
        /// </returns>
        public static int KeyLength(this CmacBlockCipherAlgorithm algorithm) => algorithm switch
        {
            CmacBlockCipherAlgorithm.Aes128 => 16,
            CmacBlockCipherAlgorithm.Aes192 => 24,
            CmacBlockCipherAlgorithm.Aes256 => 32,
            _ => throw new ArgumentOutOfRangeException(ExceptionMessages.InvalidCmacInput),
        };

        /// <summary>
        /// Returns the size, in bytes, of the resulting CMAC given the specified
        /// underlying block cipher algorithm.
        /// </summary>
        /// <param name="algorithm">
        /// The algorithm name to check.
        /// </param>
        /// <returns>
        /// An int, the size, in bytes, of the CMAC result for the specified
        /// block cipher algorithm.
        /// </returns>
        public static int MacLength(this CmacBlockCipherAlgorithm algorithm) => algorithm switch
        {
            CmacBlockCipherAlgorithm.Aes128 => 16,
            CmacBlockCipherAlgorithm.Aes192 => 16,
            CmacBlockCipherAlgorithm.Aes256 => 16,
            _ => throw new ArgumentOutOfRangeException(ExceptionMessages.InvalidCmacInput),
        };
    }
}
