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

namespace Yubico.YubiKey.Cryptography;

public class Ed25519PrivateKeyParameters : IPrivateKeyParameters
{
    private readonly Memory<byte> _privateKeyData;
    private readonly Memory<byte> _encodedKey;
    private KeyDefinition _keyDefinition { get; }

    public Ed25519PrivateKeyParameters(
        ReadOnlyMemory<byte> encodedKey,
        ReadOnlyMemory<byte> privateKeyData,
        KeyDefinition keyDefinition)
    {
        _keyDefinition = keyDefinition;
        _privateKeyData = new byte[privateKeyData.Length];
        _encodedKey = new byte[encodedKey.Length];

        privateKeyData.CopyTo(_privateKeyData);
        encodedKey.CopyTo(_encodedKey);
    }

    public ReadOnlyMemory<byte> ExportPkcs8PrivateKey() => _encodedKey;
    public KeyDefinition KeyDefinition => _keyDefinition;
    public KeyType KeyType => _keyDefinition.KeyType;
    public ReadOnlyMemory<byte> PrivateKey => _privateKeyData;
    
}
