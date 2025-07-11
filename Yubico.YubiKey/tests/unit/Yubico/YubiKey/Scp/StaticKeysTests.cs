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

using System;
using Xunit;

namespace Yubico.YubiKey.Scp
{
    public class StaticKeysTests
    {
        private readonly ReadOnlyMemory<byte> DefaultKey = new ReadOnlyMemory<byte>(new byte[] {
            0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
            0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f
        });

        [Fact]
        public void Constructor_GivenChannelMacKeyWrongLength_ThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => new StaticKeys(new byte[9], DefaultKey, DefaultKey));
        }

        [Fact]
        public void Constructor_GivenChannelEncryptionKeyWrongLength_ThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => new StaticKeys(DefaultKey, new byte[9], DefaultKey));
        }

        [Fact]
        public void Constructor_GivenDataEncryptionKeyWrongLength_ThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => new StaticKeys(DefaultKey, DefaultKey, new byte[9]));
        }
    }
}
