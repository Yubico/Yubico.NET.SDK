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

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
/// Defines the contract for cryptographic private keys.
/// </summary>
/// <remarks>
/// This interface extends <see cref="IKeyBase"/> to include private key-specific operations
/// for PKCS#8 export and secure memory cleanup.
/// Known implementations include <see cref="ECPrivateKey"/>, <see cref="RSAPrivateKey"/> and <see cref="Curve25519PrivateKey"/>,.
/// </remarks>
public interface IPrivateKey : IKeyBase
{
    /// <summary>
    /// Exports the current key in the PKCS#8 PrivateKeyInfo format.
    /// </summary>
    /// <returns>
    /// A byte array containing the PKCS#8 PrivateKeyInfo representation of this key.
    /// </returns>
    public byte[] ExportPkcs8PrivateKey();

    /// <summary>
    /// Clears the buffers containing private key data.
    /// </summary>
    public void Clear();
}
