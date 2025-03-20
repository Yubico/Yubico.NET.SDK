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
    public KeyType KeyType{ get; }
}

public interface IPublicKeyParameters : IKeyParameters
{
    public ReadOnlyMemory<byte> PublicPoint { get; } // TODO Only valid for EC? Perhaps remove, and keep only exportPublicKeyInfo
    public ReadOnlyMemory<byte> ExportSubjectPublicKeyInfo();
}

public interface IPrivateKeyParameters : IKeyParameters
{
    public ReadOnlyMemory<byte> ExportPkcs8PrivateKey();
    public ReadOnlyMemory<byte> PrivateKey { get; }
}
