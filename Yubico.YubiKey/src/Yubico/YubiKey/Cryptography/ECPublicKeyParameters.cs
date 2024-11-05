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
using System.Linq;
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography
{
    /// <summary>
    /// EC public key parameters
    /// </summary>
    public sealed class ECPublicKeyParameters : ECKeyParameters
    {
        public ECPublicKeyParameters(ECParameters parameters) : base(parameters)
        {
            if (parameters.D != null)
            {
                throw new ArgumentException(
                    "Parameters must not contain private key data (D value)", nameof(parameters));
            }
        }

        public ECPublicKeyParameters(ECDsa ecdsa) : base(ecdsa.ExportParameters(false)) { }

        public Memory<byte> GetBytes()
        {
            byte[] formatIdentifier = { 0x4 }; // Uncompressed point
            var publicKeyRawData =
                formatIdentifier
                    .Concat(Parameters.Q.X)
                    .Concat(Parameters.Q.Y)
                    .ToArray()
                    .AsMemory();

            return publicKeyRawData;
        }
    }
}
