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

using System;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// The interface that defines the object that mgmt key auth will use to
    /// perform symmetric key encryption/decryption operations.
    /// </summary>
    internal interface ISymmetricForManagementKey : IDisposable
    {
        /// <summary>
        /// Indicates whether the class was instantiated to encrypt (<c>true</c>)
        /// or decrypt (<c>false</c>).
        /// </summary>
        public bool IsEncrypting { get; }

        /// <summary>
        /// The block size of the underlying symmetric algorithm.
        /// </summary>
        public int BlockSize { get; }

        /// <summary>
        /// Encrypt or decrypt the block, depending on whether
        /// <c>IsEncrypting</c> is true or not.
        /// </summary>
        /// <remarks>
        /// The length of the input must be a multiple of the implementing
        /// algorithm's block size (8 for 3DES, 16 for AES), and cannot be 0. The
        /// length of the result will be the length of the input. It is possible
        /// to process more than one block, but the <c>outputBuffer</c> (starting
        /// at <c>outputOffset</c>) must be big enough to accept the the result.
        /// </remarks>
        /// <param name="inputBuffer">
        /// The buffer containing the data to process.
        /// </param>
        /// <param name="inputOffset">
        /// The offset into the <c>inputBuffer</c> where the data to process
        /// begins.
        /// </param>
        /// <param name="inputCount">
        /// The number of bytes to process.
        /// </param>
        /// <param name="outputBuffer">
        /// The buffer where the result will be deposited.
        /// </param>
        /// <param name="outputOffset">
        /// The offset into the <c>outputBuffer</c> where the method will begin
        /// depositing the result.
        /// </param>
        /// <returns>
        /// The number of bytes placed into the output buffer.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <c>inputBuffer</c> or <c>outputBuffer</c> argument is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The <c>inputCount</c> is not a multiple of the block size (note that
        /// 0 is not a valid input), the input or output buffers are not big
        /// enough for the specified offset and length.
        /// </exception>
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset);
    }
}
