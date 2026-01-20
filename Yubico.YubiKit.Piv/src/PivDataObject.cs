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
/// PIV data object identifiers.
/// </summary>
public static class PivDataObject
{
    /// <summary>PIV application capability container.</summary>
    public const int Capability = 0x5FC107;
    
    /// <summary>Card Holder Unique Identifier.</summary>
    public const int Chuid = 0x5FC102;
    
    /// <summary>PIV authentication certificate (slot 9A).</summary>
    public const int Authentication = 0x5FC105;
    
    /// <summary>Card authentication certificate (slot 9E).</summary>
    public const int CardAuthentication = 0x5FC101;
    
    /// <summary>Digital signature certificate (slot 9C).</summary>
    public const int Signature = 0x5FC10A;
    
    /// <summary>Key management certificate (slot 9D).</summary>
    public const int KeyManagement = 0x5FC10B;
    
    /// <summary>Card Holder Fingerprints.</summary>
    public const int Fingerprints = 0x5FC103;
    
    /// <summary>Security Object.</summary>
    public const int SecurityObject = 0x5FC106;
    
    /// <summary>Card Holder Facial Image.</summary>
    public const int FacialImage = 0x5FC108;
    
    /// <summary>Printed Information.</summary>
    public const int PrintedInformation = 0x5FC109;
    
    /// <summary>Discovery Object.</summary>
    public const int Discovery = 0x7E;
    
    /// <summary>Key History Object.</summary>
    public const int KeyHistory = 0x5FC10C;
    
    /// <summary>Retired certificate 1 (slot 82).</summary>
    public const int Retired1 = 0x5FC10D;
    
    /// <summary>Retired certificate 2 (slot 83).</summary>
    public const int Retired2 = 0x5FC10E;
    
    /// <summary>Retired certificate 3 (slot 84).</summary>
    public const int Retired3 = 0x5FC10F;
    
    /// <summary>Retired certificate 4 (slot 85).</summary>
    public const int Retired4 = 0x5FC110;
    
    /// <summary>Retired certificate 5 (slot 86).</summary>
    public const int Retired5 = 0x5FC111;
    
    /// <summary>Retired certificate 6 (slot 87).</summary>
    public const int Retired6 = 0x5FC112;
    
    /// <summary>Retired certificate 7 (slot 88).</summary>
    public const int Retired7 = 0x5FC113;
    
    /// <summary>Retired certificate 8 (slot 89).</summary>
    public const int Retired8 = 0x5FC114;
    
    /// <summary>Retired certificate 9 (slot 8A).</summary>
    public const int Retired9 = 0x5FC115;
    
    /// <summary>Retired certificate 10 (slot 8B).</summary>
    public const int Retired10 = 0x5FC116;
    
    /// <summary>Retired certificate 11 (slot 8C).</summary>
    public const int Retired11 = 0x5FC117;
    
    /// <summary>Retired certificate 12 (slot 8D).</summary>
    public const int Retired12 = 0x5FC118;
    
    /// <summary>Retired certificate 13 (slot 8E).</summary>
    public const int Retired13 = 0x5FC119;
    
    /// <summary>Retired certificate 14 (slot 8F).</summary>
    public const int Retired14 = 0x5FC11A;
    
    /// <summary>Retired certificate 15 (slot 90).</summary>
    public const int Retired15 = 0x5FC11B;
    
    /// <summary>Retired certificate 16 (slot 91).</summary>
    public const int Retired16 = 0x5FC11C;
    
    /// <summary>Retired certificate 17 (slot 92).</summary>
    public const int Retired17 = 0x5FC11D;
    
    /// <summary>Retired certificate 18 (slot 93).</summary>
    public const int Retired18 = 0x5FC11E;
    
    /// <summary>Retired certificate 19 (slot 94).</summary>
    public const int Retired19 = 0x5FC11F;
    
    /// <summary>Retired certificate 20 (slot 95).</summary>
    public const int Retired20 = 0x5FC120;
    
    /// <summary>Attestation certificate (slot F9).</summary>
    public const int Attestation = 0x5FC121;
}