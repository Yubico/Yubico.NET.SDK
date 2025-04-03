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

public class RSAPrivateKeyParameters : IPrivateKeyParameters
{
    private KeyDefinition _keyDefinition { get; }
    public RSAParameters Parameters { get; }
    public KeyDefinition KeyDefinition => _keyDefinition;
    public KeyType KeyType => _keyDefinition.KeyType;

    private RSAPrivateKeyParameters(RSAParameters parameters)
    {
        int keyLengthBits = parameters.DP?.Length * 8 * 2 ?? 0;
        
        Parameters = parameters.NormalizeParameters();
        _keyDefinition = KeyDefinitions.GetByRSALength(keyLengthBits);
    }

    public byte[] ExportPkcs8PrivateKey() => AsnPrivateKeyWriter.EncodeToPkcs8(Parameters);

    public void Clear()
    {
        CryptographicOperations.ZeroMemory(Parameters.Modulus);
        CryptographicOperations.ZeroMemory(Parameters.Exponent);
        CryptographicOperations.ZeroMemory(Parameters.P);
        CryptographicOperations.ZeroMemory(Parameters.Q);
        CryptographicOperations.ZeroMemory(Parameters.Q);
        CryptographicOperations.ZeroMemory(Parameters.DP);
        CryptographicOperations.ZeroMemory(Parameters.DQ);
        CryptographicOperations.ZeroMemory(Parameters.InverseQ);
    }

    public static RSAPrivateKeyParameters CreateFromPkcs8(ReadOnlyMemory<byte> encodedKey)
    {
        var parameters = AsnPrivateKeyReader.CreateRSAParameters(encodedKey);
        return new RSAPrivateKeyParameters(parameters);
    }

    public static RSAPrivateKeyParameters CreateFromParameters(RSAParameters parameters) => new(parameters);
    internal static RSAPrivateKeyParameters Empty() => new RSAPrivateKeyParameters(new RSAParameters());
}
