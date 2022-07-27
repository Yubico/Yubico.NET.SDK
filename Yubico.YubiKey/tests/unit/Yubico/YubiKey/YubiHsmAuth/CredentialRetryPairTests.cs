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

using System;
using Xunit;

namespace Yubico.YubiKey.YubiHsmAuth
{
    public class CredentialRetryPairTests
    {
        Credential cred = new Credential()
        {
            KeyType = CryptographicKeyType.Aes128,
            TouchRequired = false,
            Label = "aaa"
        };

        [Fact]
        public void Constructor_ReturnsObject()
        {
            CredentialRetryPair pair = new CredentialRetryPair(cred, 0);
            Assert.NotNull(pair);
        }

        [Fact]
        public void Constructor_NegativeRetries_ThrowsArgOutOfRangeException()
        {
            Action action = () => new CredentialRetryPair(cred, -1);
            _ = Assert.Throws<ArgumentOutOfRangeException>(action);
        }
    }
}
