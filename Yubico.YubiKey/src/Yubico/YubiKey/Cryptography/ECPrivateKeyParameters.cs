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
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography
{
    /// <summary>
    /// Represents the parameters for an Elliptic Curve (EC) private key.
    /// </summary>
    /// <remarks>
    /// This class encapsulates the parameters specific to EC private keys and
    /// contains the necessary private key data.
    /// </remarks>
    [Obsolete("Use ECPrivateKey instead", false)]
    public sealed class ECPrivateKeyParameters : ECPrivateKey
    {
        public ECPrivateKeyParameters(ECParameters parameters) : base(parameters)
        {
        }

        public ECPrivateKeyParameters(ECDsa ecdsaObject) : base(ecdsaObject)
        {
        }
    }
}
