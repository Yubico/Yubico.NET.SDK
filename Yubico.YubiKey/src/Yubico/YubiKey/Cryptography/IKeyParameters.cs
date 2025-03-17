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
    public KeyDefinitions.KeyDefinition GetKeyDefinition();
    public KeyDefinitions.KeyType GetKeyType();
}

public interface IPublicKeyParameters : IKeyParameters
{
    public ReadOnlyMemory<byte> GetPublicPoint();
    public ReadOnlyMemory<byte> ExportSubjectPublicKeyInfo();
}

public interface IPrivateKeyParameters : IKeyParameters
{
    public ReadOnlyMemory<byte> ExportPkcs8PrivateKey(); 
    public ReadOnlyMemory<byte> GetPrivateKey();
}

public abstract class PrivateKeyParameters : IPrivateKeyParameters
{
    public abstract KeyDefinitions.KeyDefinition GetKeyDefinition();
    public abstract KeyDefinitions.KeyType GetKeyType();

    public abstract ReadOnlyMemory<byte> ExportPkcs8PrivateKey();

    public abstract ReadOnlyMemory<byte> GetPrivateKey();
}
