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
/// PIV key slots for storing private keys and certificates.
/// </summary>
public enum PivSlot : byte
{
    /// <summary>PIV Authentication slot (9A).</summary>
    Authentication = 0x9A,
    
    /// <summary>Digital Signature slot (9C).</summary>
    Signature = 0x9C,
    
    /// <summary>Key Management slot (9D).</summary>
    KeyManagement = 0x9D,
    
    /// <summary>Card Authentication slot (9E).</summary>
    CardAuthentication = 0x9E,
    
    /// <summary>Retired key slot 1 (82).</summary>
    Retired1 = 0x82,
    
    /// <summary>Retired key slot 2 (83).</summary>
    Retired2 = 0x83,
    
    /// <summary>Retired key slot 3 (84).</summary>
    Retired3 = 0x84,
    
    /// <summary>Retired key slot 4 (85).</summary>
    Retired4 = 0x85,
    
    /// <summary>Retired key slot 5 (86).</summary>
    Retired5 = 0x86,
    
    /// <summary>Retired key slot 6 (87).</summary>
    Retired6 = 0x87,
    
    /// <summary>Retired key slot 7 (88).</summary>
    Retired7 = 0x88,
    
    /// <summary>Retired key slot 8 (89).</summary>
    Retired8 = 0x89,
    
    /// <summary>Retired key slot 9 (8A).</summary>
    Retired9 = 0x8A,
    
    /// <summary>Retired key slot 10 (8B).</summary>
    Retired10 = 0x8B,
    
    /// <summary>Retired key slot 11 (8C).</summary>
    Retired11 = 0x8C,
    
    /// <summary>Retired key slot 12 (8D).</summary>
    Retired12 = 0x8D,
    
    /// <summary>Retired key slot 13 (8E).</summary>
    Retired13 = 0x8E,
    
    /// <summary>Retired key slot 14 (8F).</summary>
    Retired14 = 0x8F,
    
    /// <summary>Retired key slot 15 (90).</summary>
    Retired15 = 0x90,
    
    /// <summary>Retired key slot 16 (91).</summary>
    Retired16 = 0x91,
    
    /// <summary>Retired key slot 17 (92).</summary>
    Retired17 = 0x92,
    
    /// <summary>Retired key slot 18 (93).</summary>
    Retired18 = 0x93,
    
    /// <summary>Retired key slot 19 (94).</summary>
    Retired19 = 0x94,
    
    /// <summary>Retired key slot 20 (95).</summary>
    Retired20 = 0x95,
    
    /// <summary>Attestation slot (F9).</summary>
    Attestation = 0xF9
}