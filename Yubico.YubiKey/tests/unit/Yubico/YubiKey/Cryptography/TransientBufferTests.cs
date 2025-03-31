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
using Xunit;

namespace Yubico.YubiKey.Cryptography;

public class TransientBufferTests
{

    [Fact]
    public void Dispose_ShouldClearArrayContent()
    {
        byte[] privateKeyData = new byte[] { 10, 20, 30, 40, 50 };

        using (var secureData = new MemoryWiper(privateKeyData))
        {
            Assert.Equal(new byte[] { 10, 20, 30, 40, 50 }, secureData.Data);
        }

        Assert.All(privateKeyData, b => Assert.Equal(0, b)); // Ensure each byte is 0
    }
}
