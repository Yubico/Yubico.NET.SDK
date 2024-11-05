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

using System;
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography
{
    /// <summary>
    /// EC private key parameters
    /// </summary>
    public sealed class ECPrivateKeyParameters : ECKeyParameters
    {
        public ECPrivateKeyParameters(ECParameters parameters) : base(parameters)
        {
            if (parameters.D == null)
            {
                throw new ArgumentException("Parameters must contain private key data (D value)", nameof(parameters));
            }
        }

        public ECPrivateKeyParameters(ECDsa ecdsaObject) : base(ecdsaObject.ExportParameters(true)) { }
    }
}