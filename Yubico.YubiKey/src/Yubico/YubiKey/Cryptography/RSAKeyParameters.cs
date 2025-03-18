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
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography;

// TODO Follow same pattern as ECKeyParameters?
// public abstract class RSAKeyParameters : IKeyParameters 
public abstract class RSAKeyParameters
{
    public RSAParameters Parameters { get; set; }
    // public KeyDefinitions.KeyDefinition GetKeyDefinition() => throw new NotImplementedException();
    //
    // public KeyDefinitions.KeyType GetKeyType() => throw new NotImplementedException();
    //
    // protected RSAKeyParameters()
    // {
    //     
    // }
    
}
