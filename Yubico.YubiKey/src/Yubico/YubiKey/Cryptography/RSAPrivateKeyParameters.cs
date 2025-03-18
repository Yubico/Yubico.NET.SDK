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

public class RSAPrivateKeyParameters : RSAKeyParameters, IPrivateKeyParameters
{
    private readonly KeyDefinitions.KeyDefinition _keyDefinition;
    // private readonly ReadOnlyMemory<byte> _encodedKey;

    public RSAPrivateKeyParameters(RSAParameters parameters)
    {
        Parameters = parameters;
        _keyDefinition = KeyDefinitions.GetByRsaLength(parameters.DP.Length * 8 * 2);
        // _encodedKey =  AsnPrivateKeyWriter.EncodeToPkcs8(parameters); 
    }

    // public ReadOnlyMemory<byte> ExportPkcs8PrivateKey() => _encodedKey;
    public ReadOnlyMemory<byte> ExportPkcs8PrivateKey() => throw new NotSupportedException("Not supported for RSA keys.");

    public ReadOnlyMemory<byte> GetPrivateKey() =>
        throw new InvalidOperationException("Not supported for RSA keys. Use Parameters instead for RSA keys.");

    public KeyDefinitions.KeyDefinition GetKeyDefinition() => _keyDefinition;
    public KeyDefinitions.KeyType GetKeyType() => _keyDefinition.KeyType;

    public static RSAPrivateKeyParameters CreateFromPkcs8(ReadOnlyMemory<byte> encodedKey)
    {
        var parameters = AsnPrivateKeyReader.CreateRSAParameters(encodedKey);
        return new RSAPrivateKeyParameters(parameters);
    }
    public static RSAPrivateKeyParameters CreateFromParameters(RSAParameters parameters) => new(parameters);
}
