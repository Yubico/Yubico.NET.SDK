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

using Yubico.YubiKit.Core.Credentials;

namespace Yubico.YubiKit.Core.UnitTests.Credentials;

public class HexParsingTests
{
    [Fact]
    public void ReadCredential_HexMode_ValidInput()
    {
        // Arrange
        var mock = new MockConsoleInput();
        mock.EnqueueKeys("0102030405060708090a0b0c0d0e0f101112131415161718");
        mock.EnqueueKey(ConsoleKey.Enter);

        var reader = new ConsoleCredentialReader(mock);
        var options = CredentialReaderOptions.ForHexKey(24);

        // Act
        using var result = reader.ReadCredential(options);

        // Assert
        Assert.NotNull(result);
        byte[] expected = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                         0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
                         0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18];
        Assert.Equal(expected, result.Memory.ToArray());
    }

    [Fact]
    public void ReadCredential_HexMode_WithSpaceSeparators()
    {
        // Arrange
        var mock = new MockConsoleInput();
        mock.EnqueueKeys("01 02 03 04 05 06 07 08 09 0a 0b 0c 0d 0e 0f 10 11 12 13 14 15 16 17 18");
        mock.EnqueueKey(ConsoleKey.Enter);

        var reader = new ConsoleCredentialReader(mock);
        var options = CredentialReaderOptions.ForHexKey(24);

        // Act
        using var result = reader.ReadCredential(options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(24, result.Memory.Length);
    }

    [Fact]
    public void ReadCredential_HexMode_WithColonSeparators()
    {
        // Arrange
        var mock = new MockConsoleInput();
        mock.EnqueueKeys("01:02:03:04:05:06:07:08:09:0a:0b:0c:0d:0e:0f:10:11:12:13:14:15:16:17:18");
        mock.EnqueueKey(ConsoleKey.Enter);

        var reader = new ConsoleCredentialReader(mock);
        var options = CredentialReaderOptions.ForHexKey(24);

        // Act
        using var result = reader.ReadCredential(options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(24, result.Memory.Length);
    }

    [Fact]
    public void ReadCredential_HexMode_WithHyphenSeparators()
    {
        // Arrange
        var mock = new MockConsoleInput();
        mock.EnqueueKeys("01-02-03-04-05-06-07-08");
        mock.EnqueueKey(ConsoleKey.Enter);

        var reader = new ConsoleCredentialReader(mock);
        var options = CredentialReaderOptions.ForHexKey(8);

        // Act
        using var result = reader.ReadCredential(options);

        // Assert
        Assert.NotNull(result);
        byte[] expected = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        Assert.Equal(expected, result.Memory.ToArray());
    }

    [Fact]
    public void ReadCredential_HexMode_WrongLength_ReturnsNull()
    {
        // Arrange
        var mock = new MockConsoleInput();
        // ForHexKey(2) expects 2 bytes: MinLength=4 (chars), MaxLength=6 (chars)
        // Input "010203" = 6 chars (3 bytes) - within MaxLength but wrong byte count
        mock.EnqueueKeys("010203"); // 3 bytes in hex, but only 2 expected
        mock.EnqueueKey(ConsoleKey.Enter);

        var reader = new ConsoleCredentialReader(mock);
        var options = CredentialReaderOptions.ForHexKey(2); // Expect 2 bytes

        // Act
        var result = reader.ReadCredential(options);

        // Assert
        Assert.Null(result);
        Assert.Contains(mock.Output, s => s.Contains("Expected 2 bytes"));
    }

    [Fact]
    public void ReadCredential_HexMode_UppercaseInput()
    {
        // Arrange
        var mock = new MockConsoleInput();
        mock.EnqueueKeys("AABBCCDD");
        mock.EnqueueKey(ConsoleKey.Enter);

        var reader = new ConsoleCredentialReader(mock);
        var options = CredentialReaderOptions.ForHexKey(4);

        // Act
        using var result = reader.ReadCredential(options);

        // Assert
        Assert.NotNull(result);
        byte[] expected = [0xAA, 0xBB, 0xCC, 0xDD];
        Assert.Equal(expected, result.Memory.ToArray());
    }

    [Fact]
    public void ReadCredential_HexMode_MixedCase()
    {
        // Arrange
        var mock = new MockConsoleInput();
        mock.EnqueueKeys("AaBbCcDd");
        mock.EnqueueKey(ConsoleKey.Enter);

        var reader = new ConsoleCredentialReader(mock);
        var options = CredentialReaderOptions.ForHexKey(4);

        // Act
        using var result = reader.ReadCredential(options);

        // Assert
        Assert.NotNull(result);
        byte[] expected = [0xAA, 0xBB, 0xCC, 0xDD];
        Assert.Equal(expected, result.Memory.ToArray());
    }
}
