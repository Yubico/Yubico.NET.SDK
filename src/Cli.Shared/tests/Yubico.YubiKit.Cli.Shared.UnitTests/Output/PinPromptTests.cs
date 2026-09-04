// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Text;
using Yubico.YubiKit.Cli.Shared.Output;

namespace Yubico.YubiKit.Cli.Shared.UnitTests.Output;

public sealed class PinPromptTests
{
    [Fact]
    public void Resolve_WithCommandLineValue_DoesNotPrompt()
    {
        using var resolved = PinPrompt.Resolve("123456", "PIN");

        Assert.NotNull(resolved);
        Assert.Equal(Encoding.UTF8.GetBytes("123456"), resolved.Memory.ToArray());
    }

    [Fact]
    public void ConfirmMatches_ReturnsTrueForIdenticalEntry()
    {
        using var first = SecureCredential.FromUtf8String("correct horse");

        Assert.True(PinPrompt.ConfirmMatches(
            first,
            () => SecureCredential.FromUtf8String("correct horse")));
    }

    [Fact]
    public void ConfirmMatches_ReturnsFalseForDifferentEntry()
    {
        using var first = SecureCredential.FromUtf8String("correct horse");

        Assert.False(PinPrompt.ConfirmMatches(
            first,
            () => SecureCredential.FromUtf8String("battery staple")));
    }

    [Fact]
    public void ConfirmMatches_ReturnsFalseForPrefixOfTheSecret()
    {
        // Guards against a length-insensitive comparison treating a prefix as a match.
        using var first = SecureCredential.FromUtf8String("123456");

        Assert.False(PinPrompt.ConfirmMatches(
            first,
            () => SecureCredential.FromUtf8String("1234")));
    }

    [Fact]
    public void ConfirmMatches_TreatsDeclinedConfirmationAsMismatch()
    {
        using var first = SecureCredential.FromUtf8String("123456");

        Assert.False(PinPrompt.ConfirmMatches(first, () => null));
    }

    [Fact]
    public void ConfirmMatches_DisposesTheConfirmationEntry()
    {
        using var first = SecureCredential.FromUtf8String("123456");
        var confirmation = SecureCredential.FromUtf8String("123456");
        var buffer = confirmation.DangerousGetBufferForTesting();

        Assert.True(PinPrompt.ConfirmMatches(first, () => confirmation));

        Assert.All(buffer, b => Assert.Equal(0, b));
    }
}
