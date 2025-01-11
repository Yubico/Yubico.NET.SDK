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
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Fido2.Cose
{
    public static class CoseEcCurveExtensions
    {
        public static KeyDefinitions.KeyDefinition GetKeyDefinition(this CoseEcCurve curve)
        {
            var keyDefinitions = new KeyDefinitions.Helper(); //todo get singleton
            
            var definition = curve switch
            {
                CoseEcCurve.P256 => keyDefinitions.GetKeyDefinition(KeyDefinitions.KeyType.P256),
                CoseEcCurve.P384 => keyDefinitions.GetKeyDefinition(KeyDefinitions.KeyType.P384),
                CoseEcCurve.P521 => keyDefinitions.GetKeyDefinition(KeyDefinitions.KeyType.P521),
                CoseEcCurve.X25519 => keyDefinitions.GetKeyDefinition(KeyDefinitions.KeyType.X25519),
                CoseEcCurve.Ed25519 => keyDefinitions.GetKeyDefinition(KeyDefinitions.KeyType.Ed25519),
                _ => throw new ArgumentException(nameof(curve), "Unknown curve")
            };

            return definition;
        }
    }
}
