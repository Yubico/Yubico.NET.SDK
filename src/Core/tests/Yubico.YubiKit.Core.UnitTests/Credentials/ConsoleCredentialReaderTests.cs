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
        using var result = reader.ReadCredential(options, TestContext.Current.CancellationToken);

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
        using var result = reader.ReadCredential(options, TestContext.Current.CancellationToken);

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
        var result = reader.ReadCredential(options, TestContext.Current.CancellationToken);

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
        using var result = reader.ReadCredential(options, TestContext.Current.CancellationToken);

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
        using var result = reader.ReadCredential(options, TestContext.Current.CancellationToken);

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
        using var result = reader.ReadCredential(options, TestContext.Current.CancellationToken);

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
        using var result = reader.ReadCredentialWithConfirmation(options, TestContext.Current.CancellationToken);

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
        var result = reader.ReadCredentialWithConfirmation(options, TestContext.Current.CancellationToken);

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
        using var result = reader.ReadCredential(options, TestContext.Current.CancellationToken);

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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadCredential_PreCancelledWithReadyInput_DoesNotPromptOrConsumeInput(bool interactive)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var input = new MockConsoleInput { IsInteractive = interactive };
        input.EnqueueKeys("123456");
        input.EnqueueKey(ConsoleKey.Enter);
        input.EnqueueLine("123456");
        int readCount = 0;
        var console = new CallbackConsole(input)
        {
            AfterReadKey = () => readCount++,
            ReadRedirectedLine = () =>
            {
                readCount++;
                return input.ReadLine();
            },
        };
        var reader = new ConsoleCredentialReader(console);
        var options = CredentialReaderOptions.ForPin();
        cts.Cancel();

        var error = Assert.Throws<OperationCanceledException>(() =>
        {
            using var unexpectedResult = reader.ReadCredential(options, cts.Token);
        });

        Assert.Equal(cts.Token, error.CancellationToken);
        Assert.Empty(input.Output);
        Assert.Equal(0, readCount);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(7)] // Enter
    public void ReadCredential_CancelledInsideReadKey_StopsBeforeNextKeyOrResult(int cancelOnRead)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var input = new MockConsoleInput();
        input.EnqueueKeys("123456");
        input.EnqueueKey(ConsoleKey.Enter);
        input.EnqueueKey(ConsoleKey.Escape);
        int readCount = 0;
        var console = new CallbackConsole(input)
        {
            AfterReadKey = () =>
            {
                if (++readCount == cancelOnRead)
                {
                    cts.Cancel();
                }
            },
        };
        var reader = new ConsoleCredentialReader(console);

        var error = Assert.Throws<OperationCanceledException>(() =>
        {
            using var unexpectedResult = reader.ReadCredential(CredentialReaderOptions.ForPin(), cts.Token);
        });

        Assert.Equal(cts.Token, error.CancellationToken);
        Assert.Equal(cancelOnRead, readCount);
        Assert.True(input.KeyAvailable);
    }

    [Fact]
    public async Task ReadCredential_CancelledDuringRedirectedRead_WaitsForLineThenThrows()
    {
        var testToken = TestContext.Current.CancellationToken;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var timeout = TimeSpan.FromSeconds(10);
        var console = new CallbackConsole(new MockConsoleInput { IsInteractive = false })
        {
            ReadRedirectedLine = () =>
            {
                entered.SetResult();
                if (!release.Wait(timeout, testToken))
                {
                    throw new TimeoutException("The test did not release the redirected read.");
                }

                return "123456";
            },
        };
        var reader = new ConsoleCredentialReader(console);
        var read = Task.Factory.StartNew(() => Record.Exception(() =>
        {
            using var unexpectedResult = reader.ReadCredential(CredentialReaderOptions.ForPin(), cts.Token);
        }), testToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        Exception? error;
        try
        {
            await entered.Task.WaitAsync(timeout, testToken);
            cts.Cancel();
            Assert.False(read.IsCompleted);
        }
        finally
        {
            release.Set();
            // Drain the caller before disposing its gate and token source, even if the test is canceled.
            error = await read.WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(cts.Token, Assert.IsType<OperationCanceledException>(error).CancellationToken);
    }

    private sealed class CallbackConsole(MockConsoleInput input) : IConsoleInputSource
    {
        public Action? AfterReadKey { get; init; }
        public Func<string?>? ReadRedirectedLine { get; init; }
        public bool IsInteractive => input.IsInteractive;
        public bool KeyAvailable => input.KeyAvailable;

        public ConsoleKeyInfo ReadKey(bool intercept)
        {
            var key = input.ReadKey(intercept);
            AfterReadKey?.Invoke();
            return key;
        }

        public string? ReadLine() => ReadRedirectedLine is null ? input.ReadLine() : ReadRedirectedLine();
        public void Write(string text) => input.Write(text);
        public void WriteLine(string text) => input.WriteLine(text);
    }
}