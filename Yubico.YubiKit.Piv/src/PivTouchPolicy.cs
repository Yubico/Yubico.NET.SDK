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
/// Touch policy for PIV private key usage.
/// </summary>
public enum PivTouchPolicy : byte
{
    /// <summary>Use YubiKey default touch policy.</summary>
    Default = 0x00,
    
    /// <summary>Touch is never required.</summary>
    Never = 0x01,
    
    /// <summary>Touch is required for every operation.</summary>
    Always = 0x02,
    
    /// <summary>Touch is cached for 15 seconds after first touch (requires YubiKey 4.3+).</summary>
    Cached = 0x03
}