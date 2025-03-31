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

public interface IKeyParameters
{
    public KeyDefinition KeyDefinition { get; }
    public KeyType KeyType { get; }
}

public interface IPublicKeyParameters : IKeyParameters
{
    public byte[] ExportSubjectPublicKeyInfo();
}

public interface IPrivateKeyParameters : IKeyParameters
{
    public byte[] ExportPkcs8PrivateKey();
    public void Clear();
}

public class EmptyPublicKeyParameters : IPublicKeyParameters
{
    public KeyDefinition KeyDefinition { get; } = new();
    public KeyType KeyType { get; }
    public ReadOnlyMemory<byte> PublicPoint { get; }
    public byte[] ExportSubjectPublicKeyInfo() => Array.Empty<byte>();
}

public class EmptyPrivateKeyParameters : IPrivateKeyParameters
{
    public KeyDefinition KeyDefinition { get; } = new KeyDefinition();
    public KeyType KeyType { get; }
    public byte[] ExportPkcs8PrivateKey() => Array.Empty<byte>();

    public ReadOnlyMemory<byte> PrivateKey { get; }
    public void Clear() => throw new NotImplementedException();
}
