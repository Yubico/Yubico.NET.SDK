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

namespace Yubico.YubiKey.Cryptography;

public class RSAPublicKeyParameters : RSAKeyParameters, IPublicKeyParameters
{
    private readonly KeyDefinitions.KeyDefinition _keyDefinition;
    private readonly ReadOnlyMemory<byte> _encodedKey;

    public RSAPublicKeyParameters(RSAParameters parameters)
    {
        Parameters = parameters.DeepCopy();
        _keyDefinition = KeyDefinitions.GetByRsaLength(parameters.Modulus.Length * 8);
        _encodedKey = AsnPublicKeyWriter.EncodeToSpki(parameters);
    }

    public ReadOnlyMemory<byte> ExportSubjectPublicKeyInfo() => _encodedKey;

    public ReadOnlyMemory<byte> GetPublicPoint() =>
        throw new NotSupportedException("Not supported for RSA keys. Use Parameters instead for RSA keys.");

    public KeyDefinitions.KeyDefinition GetKeyDefinition() => _keyDefinition;
    public KeyDefinitions.KeyType GetKeyType() => _keyDefinition.KeyType;

    public static RSAPublicKeyParameters CreateFromParameters(RSAParameters rsaParameters) => new(rsaParameters);
}
