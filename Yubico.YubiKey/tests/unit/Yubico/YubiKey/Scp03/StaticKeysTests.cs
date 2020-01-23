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

using System;
using Xunit;

namespace Yubico.YubiKey.Scp03
{
    public class StaticKeysTests
    {
        [Fact]
        public void Constructor_GivenNullChannelMacKey_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => new StaticKeys(null, StaticKeys.DefaultKey, StaticKeys.DefaultKey));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void Constructor_GivenNullChannelEncryptionKey_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => new StaticKeys(StaticKeys.DefaultKey, null, StaticKeys.DefaultKey));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void Constructor_GivenNullDataEncryptionKey_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => new StaticKeys(StaticKeys.DefaultKey, StaticKeys.DefaultKey, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void Constructor_GivenChannelMacKeyWrongLength_ThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => new StaticKeys(new byte[9], StaticKeys.DefaultKey, StaticKeys.DefaultKey));
        }

        [Fact]
        public void Constructor_GivenChannelEncryptionKeyWrongLength_ThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => new StaticKeys(StaticKeys.DefaultKey, new byte[9], StaticKeys.DefaultKey));
        }

        [Fact]
        public void Constructor_GivenDataEncryptionKeyWrongLength_ThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => new StaticKeys(StaticKeys.DefaultKey, StaticKeys.DefaultKey, new byte[9]));
        }
    }
}
