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

namespace Yubico.YubiKit.YubiHsm.UnitTests;

public class SessionKeysTests
{
    [Fact]
    public void Parse_Valid48ByteResponse_SplitsIntoThreeKeys()
    {
        // Arrange - 48 bytes: S-ENC[0..16], S-MAC[16..32], S-RMAC[32..48]
        var response = new byte[48];
        for (var i = 0; i < 16; i++)
        {
            response[i] = 0xAA;          // S-ENC
            response[i + 16] = 0xBB;     // S-MAC
            response[i + 32] = 0xCC;     // S-RMAC
        }

        // Act
        using var keys = SessionKeys.Parse(response);

        // Assert
        Assert.Equal(16, keys.SEnc.Length);
        Assert.Equal(16, keys.SMac.Length);
        Assert.Equal(16, keys.SRmac.Length);

        Assert.True(keys.SEnc.SequenceEqual(Enumerable.Repeat((byte)0xAA, 16).ToArray()));
        Assert.True(keys.SMac.SequenceEqual(Enumerable.Repeat((byte)0xBB, 16).ToArray()));
        Assert.True(keys.SRmac.SequenceEqual(Enumerable.Repeat((byte)0xCC, 16).ToArray()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(47)]
    [InlineData(49)]
    [InlineData(64)]
    public void Parse_InvalidLength_ThrowsArgumentException(int length)
    {
        var response = new byte[length];

        Assert.Throws<ArgumentException>(() => SessionKeys.Parse(response));
    }

    [Fact]
    public void Dispose_ZerosAllKeyMaterial()
    {
        // Arrange
        var response = new byte[48];
        for (var i = 0; i < 48; i++)
            response[i] = 0xFF;

        var keys = SessionKeys.Parse(response);

        // Capture references to the internal arrays via Span before disposal
        var sEncCopy = keys.SEnc.ToArray();
        var sMacCopy = keys.SMac.ToArray();
        var sRmacCopy = keys.SRmac.ToArray();

        // Verify keys are non-zero before disposal
        Assert.Contains(sEncCopy, b => b != 0);
        Assert.Contains(sMacCopy, b => b != 0);
        Assert.Contains(sRmacCopy, b => b != 0);

        // Act
        keys.Dispose();

        // Assert - accessing after disposal should throw
        Assert.Throws<ObjectDisposedException>(() => _ = keys.SEnc);
        Assert.Throws<ObjectDisposedException>(() => _ = keys.SMac);
        Assert.Throws<ObjectDisposedException>(() => _ = keys.SRmac);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var response = new byte[48];
        var keys = SessionKeys.Parse(response);

        keys.Dispose();
        keys.Dispose(); // Should not throw
    }
}
