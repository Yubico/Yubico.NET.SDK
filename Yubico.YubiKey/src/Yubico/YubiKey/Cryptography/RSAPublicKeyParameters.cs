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

public class RSAPublicKeyParameters : IPublicKeyParameters
{
    private KeyDefinition _keyDefinition { get; }
    public RSAParameters Parameters { get; }
    public KeyDefinition KeyDefinition => _keyDefinition;
    public KeyType KeyType => _keyDefinition.KeyType;

    public RSAPublicKeyParameters(RSAParameters parameters)
    {
        Parameters = parameters.DeepCopy();
        _keyDefinition = KeyDefinitions.GetByRSALength(parameters.Modulus.Length * 8);
    }

    public byte[] ExportSubjectPublicKeyInfo()
    {
        if (Parameters.Exponent == null ||
            Parameters.Modulus == null)
        {
            throw new InvalidOperationException("Cannot export public key, missing required parameters");
        }

        return AsnPublicKeyWriter.EncodeToSubjectPublicKeyInfo(Parameters);
    }

    public static RSAPublicKeyParameters CreateFromParameters(RSAParameters rsaParameters) => new(rsaParameters);

    public static IPublicKeyParameters CreateFromPkcs8(ReadOnlyMemory<byte> encodedKey) =>
        AsnPublicKeyReader.CreateKeyParameters(encodedKey);
}
