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

using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Piv;

/// <summary>
/// PIV feature support by firmware version.
/// </summary>
public static class PivFeatures
{
    /// <summary>P-384 elliptic curve support.</summary>
    public static Feature P384 { get; } = new("P-384 Curve", 4, 0, 0);
    
    /// <summary>PIN and touch policy support.</summary>
    public static Feature UsagePolicy { get; } = new("PIN/Touch Policy", 4, 0, 0);
    
    /// <summary>Cached touch policy support.</summary>
    public static Feature TouchCached { get; } = new("Cached Touch", 4, 3, 0);
    
    /// <summary>Key attestation support.</summary>
    public static Feature Attestation { get; } = new("Attestation", 4, 3, 0);
    
    /// <summary>Serial number retrieval support.</summary>
    public static Feature Serial { get; } = new("Serial Number", 5, 0, 0);
    
    /// <summary>Metadata retrieval support.</summary>
    public static Feature Metadata { get; } = new("Metadata", 5, 3, 0);
    
    /// <summary>AES management key support.</summary>
    public static Feature AesKey { get; } = new("AES Management Key", 5, 4, 0);
    
    /// <summary>Key move and delete operations support.</summary>
    public static Feature MoveKey { get; } = new("Move/Delete Key", 5, 7, 0);
    
    /// <summary>Curve25519 algorithm support.</summary>
    public static Feature Cv25519 { get; } = new("Curve25519", 5, 7, 0);
    
    /// <summary>RSA 3072 and RSA 4096 algorithm support.</summary>
    public static Feature Rsa3072Rsa4096 { get; } = new("RSA 3072/4096", 5, 7, 0);
    
    /// <summary>
    /// Checks if RSA key generation is supported for the given firmware version.
    /// RSA generation is broken on YubiKey firmware 4.2.6-4.3.4 due to ROCA vulnerability.
    /// </summary>
    /// <param name="version">Firmware version to check.</param>
    /// <returns>True if RSA generation is supported and safe.</returns>
    public static bool SupportsRsaGeneration(FirmwareVersion version) =>
        version < new FirmwareVersion(4, 2, 6) || version >= new FirmwareVersion(4, 3, 5);
}