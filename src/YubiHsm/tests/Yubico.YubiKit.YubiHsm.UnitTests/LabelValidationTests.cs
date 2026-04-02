// Copyright 2026 Yubico AB
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

using System.Text;

namespace Yubico.YubiKit.YubiHsm.UnitTests;

public class LabelValidationTests
{
    [Fact]
    public void ValidateAndEncodeLabel_ValidLabel_ReturnsUtf8Bytes()
    {
        var result = HsmAuthSession.ValidateAndEncodeLabel("my-credential");

        Assert.Equal(Encoding.UTF8.GetBytes("my-credential"), result);
    }

    [Fact]
    public void ValidateAndEncodeLabel_SingleChar_Succeeds()
    {
        var result = HsmAuthSession.ValidateAndEncodeLabel("x");

        Assert.Single(result);
        Assert.Equal((byte)'x', result[0]);
    }

    [Fact]
    public void ValidateAndEncodeLabel_Exactly64Bytes_Succeeds()
    {
        var label = new string('a', 64);
        var result = HsmAuthSession.ValidateAndEncodeLabel(label);

        Assert.Equal(64, result.Length);
    }

    [Fact]
    public void ValidateAndEncodeLabel_Empty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => HsmAuthSession.ValidateAndEncodeLabel(""));
    }

    [Fact]
    public void ValidateAndEncodeLabel_Null_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => HsmAuthSession.ValidateAndEncodeLabel(null!));
    }

    [Fact]
    public void ValidateAndEncodeLabel_Exceeds64Bytes_ThrowsArgumentException()
    {
        // 65 ASCII characters = 65 UTF-8 bytes
        var label = new string('a', 65);

        Assert.Throws<ArgumentException>(() => HsmAuthSession.ValidateAndEncodeLabel(label));
    }

    [Fact]
    public void ValidateAndEncodeLabel_MultiByteUtf8_CountsByteLength()
    {
        // Each CJK character is 3 UTF-8 bytes
        // 22 CJK chars = 66 bytes, exceeds 64
        var label = new string('\u4e00', 22);

        Assert.Throws<ArgumentException>(() => HsmAuthSession.ValidateAndEncodeLabel(label));
    }

    [Fact]
    public void ValidateAndEncodeLabel_MultiByteUtf8Within64_Succeeds()
    {
        // 21 CJK chars = 63 bytes, within limit
        var label = new string('\u4e00', 21);
        var result = HsmAuthSession.ValidateAndEncodeLabel(label);

        Assert.Equal(63, result.Length);
    }
}
