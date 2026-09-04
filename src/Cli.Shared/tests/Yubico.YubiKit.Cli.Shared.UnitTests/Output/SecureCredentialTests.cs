// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Text;
using Yubico.YubiKit.Cli.Shared.Output;

namespace Yubico.YubiKit.Cli.Shared.UnitTests.Output;

public sealed class SecureCredentialTests
{
    private sealed class TrackingArrayPool(int bufferLength) : ArrayPool<byte>
    {
        private readonly byte[] _buffer = new byte[bufferLength];

        public int RequestedLength { get; private set; }

        public int ReturnCount { get; private set; }

        public bool ReturnedCleared { get; private set; }

        public override byte[] Rent(int minimumLength)
        {
            if (minimumLength > _buffer.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumLength),
                    minimumLength,
                    "Test pool buffer is too small.");
            }

            RequestedLength = minimumLength;
            return _buffer;
        }

        public override void Return(byte[] array, bool clearArray = false)
        {
            ReturnCount++;
            ReturnedCleared = array.AsSpan().IndexOfAnyExcept((byte)0) < 0;
        }
    }

    [Fact]
    public void FromUtf8String_ExposesUtf8Bytes()
    {
        using var credential = SecureCredential.FromUtf8String("123456");

        Assert.Equal(Encoding.UTF8.GetBytes("123456"), credential.Memory.ToArray());
    }

    [Fact]
    public void FromUtf8String_ThrowsForEmptyValue()
    {
        Assert.Throws<ArgumentException>(() => SecureCredential.FromUtf8String(""));
    }

    [Fact]
    public void FromUtf8String_DisposeZerosOwnedBuffer()
    {
        var credential = SecureCredential.FromUtf8String("123456");
        var buffer = credential.DangerousGetBufferForTesting();

        credential.Dispose();

        Assert.All(buffer, b => Assert.Equal(0, b));
    }

    [Fact]
    public void Memory_ThrowsAfterDispose()
    {
        var credential = SecureCredential.FromUtf8String("123456");

        credential.Dispose();

        Assert.Throws<ObjectDisposedException>((Action)(() => _ = credential.Memory));
    }

    [Fact]
    public void FromConsoleKeysForTesting_BackspaceAfterMultiByteCharacter_RemovesWholeCharacter()
    {
        ConsoleKeyInfo[] keys =
        [
            new('é', ConsoleKey.E, shift: false, alt: false, control: false),
            new('\0', ConsoleKey.Backspace, shift: false, alt: false, control: false),
            new('1', ConsoleKey.D1, shift: false, alt: false, control: false),
            new('\r', ConsoleKey.Enter, shift: false, alt: false, control: false)
        ];

        using var credential = SecureCredential.FromConsoleKeysForTesting(keys);

        Assert.NotNull(credential);
        Assert.Equal(Encoding.UTF8.GetBytes("1"), credential.Memory.ToArray());
    }

    [Fact]
    public void ReadMaskedConsoleInputForTesting_BackspaceZeroesRetractedMultiByteScalar()
    {
        ConsoleKeyInfo[] keys =
        [
            new('é', ConsoleKey.E, shift: false, alt: false, control: false),
            new('\0', ConsoleKey.Backspace, shift: false, alt: false, control: false),
            new('1', ConsoleKey.D1, shift: false, alt: false, control: false),
            new('\r', ConsoleKey.Enter, shift: false, alt: false, control: false)
        ];
        var buffer = new byte[8];

        var length = SecureCredential.ReadMaskedConsoleInputForTesting(keys, buffer);

        Assert.Equal(1, length);
        Assert.Equal((byte)'1', buffer[0]);
        Assert.All(buffer[1..], value => Assert.Equal(0, value));
    }

    [Fact]
    public void FromConsoleKeysForTesting_BackspaceToEmptyThenEnterDeclines()
    {
        ConsoleKeyInfo[] keys =
        [
            new('1', ConsoleKey.D1, shift: false, alt: false, control: false),
            new('\0', ConsoleKey.Backspace, shift: false, alt: false, control: false),
            new('\r', ConsoleKey.Enter, shift: false, alt: false, control: false)
        ];

        Assert.Null(SecureCredential.FromConsoleKeysForTesting(keys));
    }

    [Fact]
    public void FromConsoleKeysForTesting_ValidSurrogatePairEncodesUnicodeScalar()
    {
        ConsoleKeyInfo[] keys =
        [
            new('\uD83D', ConsoleKey.NoName, shift: false, alt: false, control: false),
            new('\uDE00', ConsoleKey.NoName, shift: false, alt: false, control: false),
            new('\r', ConsoleKey.Enter, shift: false, alt: false, control: false)
        ];

        using var credential = SecureCredential.FromConsoleKeysForTesting(keys);

        Assert.NotNull(credential);
        Assert.Equal(Encoding.UTF8.GetBytes("\U0001F600"), credential.Memory.ToArray());
    }

    [Fact]
    public void FromConsoleKeysForTesting_BackspaceAfterSurrogatePairRemovesWholeScalar()
    {
        ConsoleKeyInfo[] keys =
        [
            new('\uD83D', ConsoleKey.NoName, shift: false, alt: false, control: false),
            new('\uDE00', ConsoleKey.NoName, shift: false, alt: false, control: false),
            new('\0', ConsoleKey.Backspace, shift: false, alt: false, control: false),
            new('1', ConsoleKey.D1, shift: false, alt: false, control: false),
            new('\r', ConsoleKey.Enter, shift: false, alt: false, control: false)
        ];

        using var credential = SecureCredential.FromConsoleKeysForTesting(keys);

        Assert.NotNull(credential);
        Assert.Equal(Encoding.UTF8.GetBytes("1"), credential.Memory.ToArray());
    }

    [Fact]
    public void FromConsoleKeysForTesting_BackspaceRemovesPendingHighSurrogate()
    {
        ConsoleKeyInfo[] keys =
        [
            new('\uD83D', ConsoleKey.NoName, shift: false, alt: false, control: false),
            new('\0', ConsoleKey.Backspace, shift: false, alt: false, control: false),
            new('1', ConsoleKey.D1, shift: false, alt: false, control: false),
            new('\r', ConsoleKey.Enter, shift: false, alt: false, control: false)
        ];

        using var credential = SecureCredential.FromConsoleKeysForTesting(keys);

        Assert.NotNull(credential);
        Assert.Equal(Encoding.UTF8.GetBytes("1"), credential.Memory.ToArray());
    }

    [Theory]
    [InlineData('\uD83D')]
    [InlineData('\uDE00')]
    public void ReadMaskedConsoleInputForTesting_UnmatchedSurrogateIsRejectedAndCleared(char surrogate)
    {
        ConsoleKeyInfo[] keys =
        [
            new('9', ConsoleKey.D9, shift: false, alt: false, control: false),
            new(surrogate, ConsoleKey.NoName, shift: false, alt: false, control: false),
            new('\r', ConsoleKey.Enter, shift: false, alt: false, control: false)
        ];
        var buffer = new byte[16];

        Assert.Throws<InvalidOperationException>(
            () => SecureCredential.ReadMaskedConsoleInputForTesting(keys, buffer));
        Assert.All(buffer, value => Assert.Equal(0, value));
    }

    [Fact]
    public void ReadMaskedConsoleInputForTesting_HighSurrogateBeforeNonLowSurrogateIsRejectedAndCleared()
    {
        ConsoleKeyInfo[] keys =
        [
            new('9', ConsoleKey.D9, shift: false, alt: false, control: false),
            new('\uD83D', ConsoleKey.NoName, shift: false, alt: false, control: false),
            new('1', ConsoleKey.D1, shift: false, alt: false, control: false)
        ];
        var buffer = new byte[16];

        Assert.Throws<InvalidOperationException>(
            () => SecureCredential.ReadMaskedConsoleInputForTesting(keys, buffer));
        Assert.All(buffer, value => Assert.Equal(0, value));
    }

    [Fact]
    public void FromConsoleKeysForTesting_ControlKeyInterruptsPendingSurrogate()
    {
        ConsoleKeyInfo[] keys =
        [
            new('\uD83D', ConsoleKey.NoName, shift: false, alt: false, control: false),
            new('\0', ConsoleKey.Home, shift: false, alt: false, control: false),
            new('\uDE00', ConsoleKey.NoName, shift: false, alt: false, control: false)
        ];

        Assert.Throws<InvalidOperationException>(() => SecureCredential.FromConsoleKeysForTesting(keys));
    }

    [Fact]
    public void ReadMaskedConsoleInputForTesting_MultiByteScalarThatWouldExceedMaxLengthIsRejectedAndCleared()
    {
        ConsoleKeyInfo[] keys =
        [
            new('a', ConsoleKey.A, shift: false, alt: false, control: false),
            new('é', ConsoleKey.E, shift: false, alt: false, control: false)
        ];
        var buffer = new byte[2];

        Assert.Throws<InvalidOperationException>(
            () => SecureCredential.ReadMaskedConsoleInputForTesting(keys, buffer));
        Assert.All(buffer, value => Assert.Equal(0, value));
    }

    [Fact]
    public void ReadMaskedConsoleInputForTesting_EscapeClearsPendingHighSurrogateAndBufferedBytes()
    {
        ConsoleKeyInfo[] keys =
        [
            new('1', ConsoleKey.D1, shift: false, alt: false, control: false),
            new('\uD83D', ConsoleKey.NoName, shift: false, alt: false, control: false),
            new('\u001b', ConsoleKey.Escape, shift: false, alt: false, control: false)
        ];
        var buffer = new byte[16];

        Assert.Equal(-1, SecureCredential.ReadMaskedConsoleInputForTesting(keys, buffer));
        Assert.All(buffer, value => Assert.Equal(0, value));
    }

    [Fact]
    public void FromConsoleKeysForTesting_EscapeDeclines()
    {
        ConsoleKeyInfo[] keys =
        [
            new('1', ConsoleKey.D1, shift: false, alt: false, control: false),
            new('2', ConsoleKey.D2, shift: false, alt: false, control: false),
            new('\u001b', ConsoleKey.Escape, shift: false, alt: false, control: false)
        ];

        Assert.Null(SecureCredential.FromConsoleKeysForTesting(keys));
    }

    [Fact]
    public void FromConsoleKeysForTesting_EnterOnEmptyInputDeclines()
    {
        ConsoleKeyInfo[] keys =
        [
            new('\r', ConsoleKey.Enter, shift: false, alt: false, control: false)
        ];

        Assert.Null(SecureCredential.FromConsoleKeysForTesting(keys));
    }

    [Theory]
    [InlineData("123456\n", "123456")]
    [InlineData("123456\r\n", "123456")]
    [InlineData("123456", "123456")]
    [InlineData("123456\r", "123456")]
    public void ReadRedirectedInput_StripsLineTerminators(string input, string expected)
    {
        Span<byte> buffer = new byte[128];
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));

        var length = SecureCredential.ReadRedirectedInput(buffer, stream);

        Assert.Equal(expected, Encoding.UTF8.GetString(buffer[..length]));
    }

    [Fact]
    public void ReadRedirectedInput_CrLf_SecondReadSeesTheSecondLine()
    {
        // A carriage return must not terminate the read on its own: doing so leaves the line feed
        // unread, and the next prompt in the same process reports a declined credential. That
        // breaks every two-secret flow (confirmation, change-PIN) on CRLF input.
        Span<byte> first = new byte[128];
        Span<byte> second = new byte[128];
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("alpha\r\nbeta\r\n"));

        var firstLength = SecureCredential.ReadRedirectedInput(first, stream);
        var secondLength = SecureCredential.ReadRedirectedInput(second, stream);

        Assert.Equal("alpha", Encoding.UTF8.GetString(first[..firstLength]));
        Assert.Equal("beta", Encoding.UTF8.GetString(second[..secondLength]));
    }

    [Fact]
    public void ReadRedirectedInput_EmptyLineIsDeclined()
    {
        Span<byte> buffer = new byte[128];
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("\r\n"));

        Assert.Equal(0, SecureCredential.ReadRedirectedInput(buffer, stream));
    }

    [Theory]
    [InlineData("123456\n")]
    [InlineData("123456\r\n")]
    [InlineData("123456")]
    public void ReadRedirectedInput_AcceptsCredentialOfExactlyMaxLength(string input)
    {
        // The CRLF carriage return must not be counted against the buffer, or a credential of
        // exactly the maximum length is wrongly rejected as over-long on Windows-style input.
        Span<byte> buffer = new byte[6];
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));

        var length = SecureCredential.ReadRedirectedInput(buffer, stream);

        Assert.Equal("123456", Encoding.UTF8.GetString(buffer[..length]));
    }

    [Fact]
    public void ReadRedirectedInput_ThrowsWhenCredentialExceedsMaxLength()
    {
        Span<byte> buffer = new byte[6];
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("1234567\r\n"));

        try
        {
            SecureCredential.ReadRedirectedInput(buffer, stream);
            Assert.Fail("Expected an over-long credential to be rejected.");
        }
        catch (InvalidOperationException)
        {
        }
    }

    [Fact]
    public void MemoryIsSizedExactlyToTheSecret()
    {
        // The SDK transmits the whole of Memory, so a buffer longer than the secret would send
        // trailing padding as part of the credential.
        ConsoleKeyInfo[] keys =
        [
            new('1', ConsoleKey.D1, shift: false, alt: false, control: false),
            new('\r', ConsoleKey.Enter, shift: false, alt: false, control: false)
        ];

        using var credential = SecureCredential.FromConsoleKeysForTesting(keys, maxByteLength: 128);

        Assert.NotNull(credential);
        Assert.Equal(1, credential.Memory.Length);
        Assert.True(credential.DangerousGetBufferForTesting().Length >= 128);
    }

    [Fact]
    public void FromConsoleKeysForTesting_DisposeReturnsClearedBufferToPool()
    {
        ConsoleKeyInfo[] keys =
        [
            new('1', ConsoleKey.D1, shift: false, alt: false, control: false),
            new('\r', ConsoleKey.Enter, shift: false, alt: false, control: false)
        ];
        var pool = new TrackingArrayPool(bufferLength: 256);

        var credential = SecureCredential.FromConsoleKeysForTesting(
            keys,
            maxByteLength: 128,
            pool);

        Assert.NotNull(credential);
        Assert.Equal(128, pool.RequestedLength);
        Assert.Equal(0, pool.ReturnCount);

        credential.Dispose();

        Assert.Equal(1, pool.ReturnCount);
        Assert.True(pool.ReturnedCleared);
    }

    [Fact]
    public void FromConsoleKeysForTesting_DeclineReturnsClearedBufferToPool()
    {
        ConsoleKeyInfo[] keys =
        [
            new('9', ConsoleKey.D9, shift: false, alt: false, control: false),
            new('\u001b', ConsoleKey.Escape, shift: false, alt: false, control: false)
        ];
        var pool = new TrackingArrayPool(bufferLength: 16);

        var credential = SecureCredential.FromConsoleKeysForTesting(
            keys,
            maxByteLength: 4,
            pool);

        Assert.Null(credential);
        Assert.Equal(1, pool.ReturnCount);
        Assert.True(pool.ReturnedCleared);
    }

    [Fact]
    public void FromConsoleKeysForTesting_InputCannotUsePoolSlack()
    {
        ConsoleKeyInfo[] keys =
        [
            new('1', ConsoleKey.D1, shift: false, alt: false, control: false),
            new('2', ConsoleKey.D2, shift: false, alt: false, control: false),
            new('3', ConsoleKey.D3, shift: false, alt: false, control: false),
            new('4', ConsoleKey.D4, shift: false, alt: false, control: false),
            new('5', ConsoleKey.D5, shift: false, alt: false, control: false)
        ];
        var pool = new TrackingArrayPool(bufferLength: 16);

        Assert.Throws<InvalidOperationException>(() =>
            SecureCredential.FromConsoleKeysForTesting(
                keys,
                maxByteLength: 4,
                pool));
        Assert.Equal(1, pool.ReturnCount);
        Assert.True(pool.ReturnedCleared);
    }

    [Fact]
    public void ExposesItselfAsIMemoryOwnerSizedToTheSecret()
    {
        using var credential = SecureCredential.FromUtf8String("123456");

        IMemoryOwner<byte> owner = credential;

        Assert.Equal(Encoding.UTF8.GetBytes("123456"), owner.Memory.ToArray());
    }

    [Fact]
    public void IMemoryOwnerMemory_ThrowsAfterDispose()
    {
        IMemoryOwner<byte> owner = SecureCredential.FromUtf8String("123456");

        owner.Dispose();

        Assert.Throws<ObjectDisposedException>((Action)(() => _ = owner.Memory));
    }

    [Fact]
    public void DeclinedInteractiveInputLeavesNoSecretBehind()
    {
        ConsoleKeyInfo[] keys =
        [
            new('9', ConsoleKey.D9, shift: false, alt: false, control: false),
            new('\u001b', ConsoleKey.Escape, shift: false, alt: false, control: false)
        ];

        Assert.Null(SecureCredential.FromConsoleKeysForTesting(keys));
    }
}
