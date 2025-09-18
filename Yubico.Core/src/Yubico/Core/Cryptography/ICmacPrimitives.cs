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

using System;

namespace Yubico.Core.Cryptography;

/// <summary>
///     An interface exposing AES-GCM primitive operations.
/// </summary>
public interface ICmacPrimitives : IDisposable
{
    /// <summary>
    ///     Initialize the object to perform CMAC with the given key.
    /// </summary>
    /// <remarks>
    ///     The key length must match the algorithm specified at instantiation.
    ///     To know the required key length, in bytes, use the
    ///     <c>CmacBlockCipherAlgorithm</c> extension <c>KeyLength</c>.
    /// </remarks>
    /// <param name="keyData">
    ///     The key to be used in the CMAC operations.
    /// </param>
    void CmacInit(ReadOnlySpan<byte> keyData);

    /// <summary>
    ///     Continue the CMAC operation with the given input data.
    /// </summary>
    /// <remarks>
    ///     Call this with the next amount of data that is to be processed. When
    ///     there is no more data to process, call <c>Final</c>.
    /// </remarks>
    /// <param name="dataToMac">
    ///     The next amount of data to process.
    /// </param>
    void CmacUpdate(ReadOnlySpan<byte> dataToMac);

    /// <summary>
    ///     Complete the CMAC process, generating the result.
    /// </summary>
    /// <remarks>
    ///     Call this with there is no more data to process. This method will
    ///     fill the provided <c>macBuffer</c> with the resulting CMAC value. The
    ///     Span must be the exact length of the result. To know the required
    ///     output size, in bytes, use the <c>CmacBlockCipherAlgorithm</c>
    ///     extension <c>MacLength</c>.
    /// </remarks>
    /// <param name="macBuffer">
    ///     The Span into which the method will place the result.
    /// </param>
    void CmacFinal(Span<byte> macBuffer);
}
