// Copyright 2021 Yubico AB
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

namespace Yubico.YubiKey.Scp03
{
    internal class SessionKeys
    {
        public byte[] SessionMacKey { get; set; }
        public byte[] SessionEncryptionKey { get; set; }
        public byte[] SessionRmacKey { get; set; }

        public SessionKeys(byte[] sessionMacKey, byte[] sessionEncryptionKey, byte[] sessionRmacKey)
        {
            SessionMacKey = sessionMacKey;
            SessionEncryptionKey = sessionEncryptionKey;
            SessionRmacKey = sessionRmacKey;
        }
    }
}
