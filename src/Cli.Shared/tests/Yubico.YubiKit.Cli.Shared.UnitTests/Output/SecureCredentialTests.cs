// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Text;
using Yubico.YubiKit.Cli.Shared.Output;

namespace Yubico.YubiKit.Cli.Shared.UnitTests.Output;

public sealed class SecureCredentialTests
{
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

        Assert.Equal(Encoding.UTF8.GetBytes("1"), credential.Memory.ToArray());
    }
}
