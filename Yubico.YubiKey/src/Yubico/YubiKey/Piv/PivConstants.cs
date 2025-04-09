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

namespace Yubico.YubiKey.Piv;

internal static class PivConstants
{
    public const int PublicKeyTag = 0x7F49;

    public const int PublicRSAModulusTag = 0x81;
    public const int PublicRSAExponentTag = 0x82;

    public const int PrivateRSAPrimePTag = 0x01;
    public const int PrivateRSAPrimeQTag = 0x02;
    public const int PrivateRSAExponentPTag = 0x03;
    public const int PrivateRSAExponentQTag = 0x04;
    public const int PrivateRSACoefficientTag = 0x05;

    public const int PublicECTag = 0x86; // P-256, P-384, P-521, Ed25519 and X25519

    public const int PrivateECDsaTag = 0x06; // P-256, P-384, P-521
    public const int PrivateECEd25519Tag = 0x7;
    public const int PrivateECX25519Tag = 0x8;

    public static bool IsValidPrivateECTag(int peekTag)
    {
        return peekTag switch
        {
            PrivateECDsaTag or
                PrivateECEd25519Tag or
                PrivateECX25519Tag => true,
            _ => false
        };
    }

    public static bool IsValidPrivateRSATag(int peekTag)
    {
        return peekTag switch
        {
            PrivateRSAPrimePTag or
                PrivateRSAPrimeQTag or
                PrivateRSAExponentPTag or
                PrivateRSAExponentQTag or
                PrivateRSACoefficientTag => true,
            _ => false
        };
    }

    public static bool IsValidPublicECTag(int peekTag)
    {
        return peekTag switch
        {
            PublicECTag => true,
            _ => false
        };
    }

    public static bool IsValidPublicRSATag(int peekTag)
    {
        return peekTag switch
        {
            PublicRSAModulusTag or
                PublicRSAExponentTag
                => true,
            _ => false
        };
    }
}
