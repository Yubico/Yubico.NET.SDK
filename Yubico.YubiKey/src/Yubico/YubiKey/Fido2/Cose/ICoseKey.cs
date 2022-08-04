// Copyright 2022 Yubico AB
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
using System.Collections.Generic;

namespace Yubico.YubiKey.Fido2.Cose
{
    public interface ICoseKey
    {
        // Label (1)
        CoseKeyType Type { get; }

        // Label (2)
        ReadOnlyMemory<byte> KeyId { get; }

        // Label (3)
        CoseAlgorithmIdentifier Algorithm { get; }

        // Label (4)
        IReadOnlyList<CoseKeyOperations> Operations { get; }

        // Label (5)
        ReadOnlyMemory<byte> BaseIv { get; }
    }
}
