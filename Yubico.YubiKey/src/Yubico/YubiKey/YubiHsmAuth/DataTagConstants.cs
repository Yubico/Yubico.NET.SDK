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

namespace Yubico.YubiKey.YubiHsmAuth
{
    /// <summary>
    /// Tag values for TLV-formatted data associated with the YubiHSM Auth
    /// application.
    /// </summary>
    internal static class DataTagConstants
    {
        public const byte Label = 0x71;

        /// <summary>
        /// The data contains a Credential and the number of remaining
        /// retries.
        /// </summary>
        public const byte LabelList = 0x72;

        public const byte Password = 0x73;
        public const byte CryptographicKeyType = 0x74;
        public const byte EncryptionKey = 0x75;
        public const byte MacKey = 0x76;
        public const byte Context = 0x77;
        public const byte Response = 0x78;
        public const byte Version = 0x79;
        public const byte Touch = 0x7a;
        public const byte ManagementKey = 0x7b;
    }
}
