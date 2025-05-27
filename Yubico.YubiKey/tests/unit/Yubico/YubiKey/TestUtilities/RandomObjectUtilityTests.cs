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

using System.Linq;
using Xunit;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.TestUtilities;

public class RandomObjectUtilityTests
{
    [Fact]
    public void FixedBytes_Replace()
    {
        byte[] fixedBytes =
        {
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
            0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48
        };

        var replacement = RandomObjectUtility.SetRandomProviderFixedBytes(fixedBytes);

        try
        {
            var random = CryptographyProviders.RngCreator();
            var randomBytes = new byte[32];
            random.GetBytes(randomBytes);
            var compareResult = randomBytes.SequenceEqual(fixedBytes);
            Assert.True(compareResult);
        }
        finally
        {
            replacement.RestoreRandomProvider();
        }
    }
}
