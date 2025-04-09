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

namespace Yubico.YubiKey.Cryptography
{
    [Obsolete("Use ECPublicKey instead", false)]
    public sealed class ECPublicKeyParameters : ECPublicKey
    {
        public ECPublicKeyParameters(ECParameters parameters) : base(parameters)
        {
        }

        public ECPublicKeyParameters(ECDsa ecdsa) : base(ecdsa)
        {
        }
    }
}
