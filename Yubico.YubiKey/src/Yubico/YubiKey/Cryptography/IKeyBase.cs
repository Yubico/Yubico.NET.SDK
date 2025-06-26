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

namespace Yubico.YubiKey.Cryptography;

/// <summary>
/// Defines the base contract for all cryptographic keys, providing key type identification.
/// </summary>
/// <remarks>
/// This interface serves as the foundation for both public and private key abstractions,
/// enabling polymorphic key type handling across different cryptographic algorithms.
/// </remarks>
public interface IKeyBase
{
    /// <summary>
    /// Gets the type of the cryptographic key.
    /// </summary>
    /// <value>
    /// A <see cref="KeyType"/> value indicating the type of the key.
    /// </value>
    public KeyType KeyType { get; }
}
