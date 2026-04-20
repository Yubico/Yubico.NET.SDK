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

namespace Yubico.YubiKit.Piv;

/// <summary>
/// PIV management key algorithm types.
/// </summary>
public enum PivManagementKeyType : byte
{
    /// <summary>3DES key (24 bytes, 8-byte challenge).</summary>
    TripleDes = 0x03,
    
    /// <summary>AES-128 key (16 bytes, 16-byte challenge) - requires YubiKey 5.4+.</summary>
    Aes128 = 0x08,
    
    /// <summary>AES-192 key (24 bytes, 16-byte challenge) - requires YubiKey 5.4+.</summary>
    Aes192 = 0x0A,
    
    /// <summary>AES-256 key (32 bytes, 16-byte challenge) - requires YubiKey 5.4+.</summary>
    Aes256 = 0x0C
}