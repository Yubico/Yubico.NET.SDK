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
/// Defines the contract for cryptographic public keys.
/// </summary>
/// <remarks>
/// This interface extends <see cref="IKeyBase"/> to include public key-specific operations
/// for X.509 SubjectPublicKeyInfo export.
/// <para>
/// Concrete implementations include <see cref="ECPublicKey"/>, <see cref="RSAPublicKey"/> and <see cref="Curve25519PublicKey"/>, each providing
/// algorithm-specific public key handling and export mechanisms.
/// </para>
/// </remarks>
public interface IPublicKey : IKeyBase
{
    /// <summary>
    /// Exports the public-key portion of the current key in the X.509 SubjectPublicKeyInfo format.
    /// </summary>
    /// <returns>
    /// A byte array containing the X.509 SubjectPublicKeyInfo representation of the public-key portion of this key
    /// </returns>
    public byte[] ExportSubjectPublicKeyInfo();
}
