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
using System.Linq;
using Xunit;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.TestUtilities;

public class RandomObjectUtilityTests
{
    [Fact]
    public void GetRandomObject_GetBytes_ConsumesFixedBytesAcrossOverloads()
    {
        using var random = RandomObjectUtility.GetRandomObject(new byte[] { 1, 2, 3, 4 });
        var first = new byte[2];
        var second = new byte[4];

        random.GetBytes(first);
        random.GetBytes(second, 1, 2);

        Assert.Equal(new byte[] { 1, 2 }, first);
        Assert.Equal(new byte[] { 0, 3, 4, 0 }, second);
        Assert.Throws<ArgumentException>(() => random.GetBytes(new byte[1]));
        random.GetBytes(Array.Empty<byte>());
    }

    [Fact]
    public void GetRandomObject_CopiesInputAndCreatesIndependentGenerators()
    {
        byte[] fixedBytes = { 1, 2 };
        using var first = RandomObjectUtility.GetRandomObject(fixedBytes);
        using var second = RandomObjectUtility.GetRandomObject(fixedBytes);
        fixedBytes[0] = 99;
        var buffer = new byte[2];

        first.GetBytes(buffer);
        Assert.Equal(new byte[] { 1, 2 }, buffer);
        second.GetBytes(buffer);
        Assert.Equal(new byte[] { 1, 2 }, buffer);
    }

    [Fact]
    public void GetRandomObject_EmptyInput_RejectsNonemptyRequests()
    {
        using var random = RandomObjectUtility.GetRandomObject(Array.Empty<byte>());

        random.GetBytes(Array.Empty<byte>());
        Assert.Throws<ArgumentException>(() => random.GetBytes(new byte[1]));
    }

    [Fact]
    public void GetRandomObject_InvalidArguments_DoNotConsumeBytes()
    {
        using var random = RandomObjectUtility.GetRandomObject(new byte[] { 1, 2 });
        var buffer = new byte[2];

        Assert.Throws<ArgumentNullException>(() => random.GetBytes(null!));
        Assert.Throws<ArgumentNullException>(() => random.GetBytes(null!, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.GetBytes(buffer, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.GetBytes(buffer, 0, -1));
        Assert.Throws<ArgumentException>(() => random.GetBytes(buffer, 1, 2));
        Assert.Throws<ArgumentException>(() => random.GetBytes(new byte[3]));

        random.GetBytes(buffer);
        Assert.Equal(new byte[] { 1, 2 }, buffer);
    }

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
