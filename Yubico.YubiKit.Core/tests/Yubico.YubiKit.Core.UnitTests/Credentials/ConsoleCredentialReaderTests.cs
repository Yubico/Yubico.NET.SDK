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

public class ConsoleCredentialReaderTests
{
    [Fact]
    public void ReadCredential_ValidPin_ReturnsCredential()
    {
        // Arrange
        var mock = new MockConsoleInput();
        mock.EnqueueKeys("123456");
        mock.EnqueueKey(ConsoleKey.Enter);

        var reader = new ConsoleCredentialReader(mock);
        var options = CredentialReaderOptions.ForPin();

        // Act
        using var result = reader.ReadCredential(options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("123456"u8.ToArray(), result.Memory.ToArray());
    }

    [Fact]
    public void ReadCredential_WithBackspace_EditsInput()
    {
        // Arrange
        var mock = new MockConsoleInput();
        mock.EnqueueKeys("1234");
        mock.EnqueueKey(ConsoleKey.Backspace);
        mock.EnqueueKey(ConsoleKey.Backspace);
        mock.EnqueueKeys("5678");
        mock.EnqueueKey(ConsoleKey.Enter);

        var reader = new ConsoleCredentialReader(mock);
        var options = CredentialReaderOptions.ForPin();

        // Act
        using var result = reader.ReadCredential(options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("125678"u8.ToArray(), result.Memory.ToArray());
    }

    [Fact]
    public void ReadCredential_EscapeKey_ReturnsNull()
    {
        // Arrange
        var mock = new MockConsoleInput();
        mock.EnqueueKeys("123");
        mock.EnqueueKey(ConsoleKey.Escape);

        var reader = new ConsoleCredentialReader(mock);
        var options = CredentialReaderOptions.ForPin();

        // Act
        var result = reader.ReadCredential(options);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ReadCredential_BelowMinLength_PromptsRetry()
    {
        // Arrange
        var mock = new MockConsoleInput();
        mock.EnqueueKeys("123"); // Too short
        mock.EnqueueKey(ConsoleKey.Enter);
        mock.EnqueueKeys("123456"); // Valid
        mock.EnqueueKey(ConsoleKey.Enter);

        var reader = new ConsoleCredentialReader(mock);
        var options = CredentialReaderOptions.ForPin();

        // Act
        using var result = reader.ReadCredential(options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("123456"u8.ToArray(), result.Memory.ToArray());
        Assert.Contains(mock.Output, s => s.Contains("too short", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReadCredential_CharacterFilter_RejectsInvalidChars()
    {
        // Arrange
        var mock = new MockConsoleInput();
        mock.EnqueueKeys("12abc34def56"); // Letters should be filtered out
        mock.EnqueueKey(ConsoleKey.Enter);

        var reader = new ConsoleCredentialReader(mock);
        var options = CredentialReaderOptions.ForPin(); // Digits only

        // Act
        using var result = reader.ReadCredential(options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("123456"u8.ToArray(), result.Memory.ToArray());
    }

    [Fact]
    public void ReadCredential_MaxLength_IgnoresExcessChars()
    {
        // Arrange
        var mock = new MockConsoleInput();
        mock.EnqueueKeys("12345678999999"); // More than 8 chars
        mock.EnqueueKey(ConsoleKey.Enter);

        var reader = new ConsoleCredentialReader(mock);
        var options = CredentialReaderOptions.ForPin(); // Max 8

        // Act
        using var result = reader.ReadCredential(options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(8, result.Memory.Length);
        Assert.Equal("12345678"u8.ToArray(), result.Memory.ToArray());
    }

    [Fact]
    public void ReadCredentialWithConfirmation_MatchingCredentials_ReturnsCredential()
    {
        // Arrange
        var mock = new MockConsoleInput();
        // First entry
        mock.EnqueueKeys("123456");
        mock.EnqueueKey(ConsoleKey.Enter);
        // Confirmation
        mock.EnqueueKeys("123456");
        mock.EnqueueKey(ConsoleKey.Enter);

        var reader = new ConsoleCredentialReader(mock);
        var options = CredentialReaderOptions.ForPin();

        // Act
        using var result = reader.ReadCredentialWithConfirmation(options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("123456"u8.ToArray(), result.Memory.ToArray());
    }

    [Fact]
    public void ReadCredentialWithConfirmation_MismatchedCredentials_ReturnsNull()
    {
        // Arrange
        var mock = new MockConsoleInput();
        // First entry
        mock.EnqueueKeys("123456");
        mock.EnqueueKey(ConsoleKey.Enter);
        // Confirmation (different)
        mock.EnqueueKeys("654321");
        mock.EnqueueKey(ConsoleKey.Enter);

        var reader = new ConsoleCredentialReader(mock);
        var options = CredentialReaderOptions.ForPin();

        // Act
        var result = reader.ReadCredentialWithConfirmation(options);

        // Assert
        Assert.Null(result);
        Assert.Contains(mock.Output, s => s.Contains("do not match", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReadCredential_NonInteractive_ReadsLine()
    {
        // Arrange
        var mock = new MockConsoleInput { IsInteractive = false };
        mock.EnqueueLine("123456");

        var reader = new ConsoleCredentialReader(mock);
        var options = CredentialReaderOptions.ForPin();

        // Act
        using var result = reader.ReadCredential(options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("123456"u8.ToArray(), result.Memory.ToArray());
    }

    [Fact]
    public void ReadCredential_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var mock = new MockConsoleInput();
        // Don't enqueue any keys - the reader should check cancellation before blocking
        
        var reader = new ConsoleCredentialReader(mock);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        var options = CredentialReaderOptions.ForPin();

        // Act & Assert
        Assert.Throws<OperationCanceledException>(() => 
            reader.ReadCredential(options, cts.Token));
    }
}
