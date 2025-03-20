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
    private KeyDefinition _keyDefinition { get; }

    public RSAPrivateKeyParameters(RSAParameters parameters)
    {
        // Get the key length from the CRT component before normalization
        // This uses Chinese Remainder Theorem (CRT) components which is what the YubiKey uses
        int keyLengthBits = parameters.DP?.Length * 8 * 2 ?? 0;
        
        // Apply normalization for cross-platform compatibility
        Parameters = parameters.NormalizeParameters();
        
        // Use the original key length from CRT component for key definition
        _keyDefinition = KeyDefinitions.GetByRSALength(keyLengthBits);
    }

    // public ReadOnlyMemory<byte> ExportPkcs8PrivateKey() => AsnPrivateKeyWriter.EncodeToPkcs8(parameters);
    public ReadOnlyMemory<byte> ExportPkcs8PrivateKey() =>
        throw new NotSupportedException("Not supported for RSA keys.");

    public ReadOnlyMemory<byte> PrivateKey =>
        throw new InvalidOperationException("Not supported for RSA keys. Use Parameters instead for RSA keys.");

    public KeyDefinition KeyDefinition => _keyDefinition;
    public KeyType KeyType => _keyDefinition.KeyType;

    public static RSAPrivateKeyParameters CreateFromPkcs8(ReadOnlyMemory<byte> encodedKey)
    {
        var parameters = AsnPrivateKeyReader.CreateRSAParameters(encodedKey);
        return new RSAPrivateKeyParameters(parameters);
    }

    public static RSAPrivateKeyParameters CreateFromParameters(RSAParameters parameters) => new(parameters);
}
