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

namespace Yubico.YubiKey.Cryptography;

/// <summary>
///     Abstract base class for public key implementations.
/// </summary>
/// <remarks>
///     This class provides common structure for public key types, requiring derived classes
///     to implement key type identification and X.509 export operations.
///     <para>
///         Concrete implementations include <see cref="ECPublicKey" />, <see cref="RSAPublicKey" /> and
///         <see cref="Curve25519PublicKey" />, each providing
///         algorithm-specific public key handling and export mechanisms.
///     </para>
/// </remarks>
public abstract class PublicKey : IPublicKey
{
    #region IPublicKey Members

    /// <inheritdoc />
    public abstract KeyType KeyType { get; }

    /// <inheritdoc />
    public abstract byte[] ExportSubjectPublicKeyInfo();

    #endregion
}
