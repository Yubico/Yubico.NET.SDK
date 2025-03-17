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

public class Curve25519PublicKeyParameters : IPublicKeyParameters
{
    private readonly KeyDefinitions.KeyDefinition _keyDefinition;
    private readonly Memory<byte> _publicPoint;
    private readonly Memory<byte> _encodedKey;

    public Curve25519PublicKeyParameters(
        ReadOnlyMemory<byte> encodedKey,
        ReadOnlyMemory<byte> publicPoint,
        KeyDefinitions.KeyDefinition keyDefinition)
    {
        _keyDefinition = keyDefinition;
        _publicPoint = new byte[publicPoint.Length];
        _encodedKey = new byte[encodedKey.Length];

        publicPoint.CopyTo(_publicPoint);
        encodedKey.CopyTo(_encodedKey);
    }

    public KeyDefinitions.KeyType GetKeyType() => _keyDefinition.KeyType;
    public ReadOnlyMemory<byte> ExportSubjectPublicKeyInfo() => _encodedKey;
    public KeyDefinitions.KeyDefinition GetKeyDefinition() => _keyDefinition;
    public ReadOnlyMemory<byte> GetPublicPoint() => _publicPoint;
}
