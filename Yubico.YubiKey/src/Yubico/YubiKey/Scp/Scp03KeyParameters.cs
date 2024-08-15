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
using Yubico.YubiKey.Scp03;

namespace Yubico.YubiKey.Scp
{
    public class Scp03KeyParameters : ScpKeyParameters
    {
        public StaticKeys StaticKeys { get; }

        public Scp03KeyParameters(
            KeyReference keyReference,
            StaticKeys staticKeys) : base(keyReference)
        {
            if (keyReference.Id > 3)
            {
                throw new ArgumentException("Invalid KID for SCP03", nameof(keyReference.Id));
            }

            StaticKeys = staticKeys;
        }

        public Scp03KeyParameters(
            byte keyId,
            byte keyVersionNumber,
            StaticKeys staticKeys) : this(new KeyReference(keyId, keyVersionNumber), staticKeys)
        {
        }

        public static Scp03KeyParameters DefaultKey => new Scp03KeyParameters(ScpKid.Scp03, 0xFF, new StaticKeys());
    }
}
