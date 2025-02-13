// Copyright 2025 Yubico AB
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

namespace Yubico.YubiKey.Scp
{
    /// <summary>
    /// Represents common key IDs for Secure Channel Protocol (SCP) keys.
    /// <para><br/>
    /// KID '10' for PK.CA-KLOC.ECDSA<br/>
    /// KID '11' for SK.SD.ECKA used for SCP11a<br/>
    /// KID '12' for the optional static Key-DEK used with SCP11a only<br/>
    /// KID '13' for SK.SD.ECKA used for SCP11b<br/>
    /// KID '14' for the optional static Key-DEK used with SCP11b only<br/>
    /// KID '15' for SK.SD.ECKA used for SCP11c<br/>
    /// KID '16' for the optional static Key-DEK used with SCP11c only<br/>
    /// KID from '20' to '2F' for additional PK.CA-KLOC.ECDSA<br/>
    /// </para>
    /// </summary>
    /// <remarks>See the GlobalPlatform Technology Card Specification v2.3 Amendment F §5.1 Cryptographic Keys for more information on the available KIDs.</remarks>
    public static class ScpKeyIds
    {
        /// <summary>
        /// Key ID '0x01' for static keys used for SCP03.
        /// <remarks>When storing SCP03 keysets, the SDK
        /// will store ke KID's 0x01, 0x02 and 0x03 for ENC, MAC, DEK on the YubiKey
        /// </remarks>
        /// </summary>
        public const byte Scp03 = 0x01;

        /// <summary>
        /// Key ID '0x10' for the public key of the certificate authority, also known as 'PK.CA-KLOC.ECDSA'. Needs to be an ECDSA key.
        /// </summary>
        public const byte ScpCaPublicKey = 0x10;

        /// <summary>
        /// Key ID '0x11' for SK.SD.ECKA used for SCP11a.
        /// </summary>
        public const byte Scp11A = 0x11;

        /// <summary>
        /// Key ID '0x13' for SK.SD.ECKA used for SCP11b.
        /// </summary>
        public const byte Scp11B = 0x13;

        /// <summary>
        /// Key ID '0x14' for the optional static Key-DEK (data encryption key) used with SCP11b only.
        /// </summary>
        public const byte Scp11BOptionalDek = 0x14;

        /// <summary>
        /// Key ID '0x15' for SK.SD.ECKA used for SCP11c
        /// </summary>
        public const byte Scp11C = 0x15;

        /// <summary>
        /// Key ID '0x16' for the optional static Key-DEK (data encryption key) used with SCP11c only.
        /// </summary>
        public const byte Scp11COptionalDek = 0x16;
    }
}
