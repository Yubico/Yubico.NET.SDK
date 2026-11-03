// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.UnitTests.Utilities;

public sealed class Base32Tests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("MY======", "f")]
    [InlineData("MZXQ====", "fo")]
    [InlineData("MZXW6===", "foo")]
    [InlineData("MZXW6YQ=", "foob")]
    [InlineData("MZXW6YTB", "fooba")]
    [InlineData("MZXW6YTBOI======", "foobar")]
    [InlineData("MZ", "f")]
    [InlineData("m z x w 6 y t b o i", "foobar")]
    public void Decode_RfcVectors_WriteExactBytesAndLeaveUnusedDestinationAlone(string encoded, string expected)
    {
        foreach (string input in new[] { encoded, encoded.TrimEnd('=') })
        {
            byte[] destination = new byte[32];
            Array.Fill(destination, (byte)0xA5);
            byte[] decoded = Base32.Decode(input.AsSpan());
            try
            {
                int written = Base32.Decode(input.AsSpan(), destination);
                Assert.Equal(expected.Length, written);
                Assert.True(CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), destination.AsSpan(0, written)));
                Assert.True(CryptographicOperations.FixedTimeEquals(decoded, destination.AsSpan(0, written)));
                Assert.True(destination.AsSpan(written).IndexOfAnyExcept((byte)0xA5) < 0);
            }
            finally { CryptographicOperations.ZeroMemory(destination); CryptographicOperations.ZeroMemory(decoded); }
        }
    }

    [Theory]
    [InlineData("A")]
    [InlineData("AAA")]
    [InlineData("AAAAAA")]
    [InlineData("AAAAAAAAA")]
    [InlineData("MZXW6YTB!")]
    [InlineData("MZXW6YTBé")]
    [InlineData("MZXW6YTB\t")]
    [InlineData("MZXW6YTB\n")]
    [InlineData("MZXW6YTBOI=====")]
    [InlineData("MZXW6YTBOI=======")]
    [InlineData("MZXW6YTB=OI=====")]
    [InlineData("========")]
    [InlineData("MZXW6YTB========")]
    public void Decode_InvalidInput_ClearsEntireCallerVisibleDestination(string encoded)
    {
        byte[] destination = new byte[32];
        Array.Fill(destination, (byte)0xA5);
        var exception = Assert.Throws<ArgumentException>(() => Base32.Decode(encoded.AsSpan(), destination));
        Assert.True(destination.AsSpan().IndexOfAnyExcept((byte)0) < 0);
        Assert.DoesNotContain(encoded, exception.Message);
        Assert.Throws<ArgumentException>(() => Base32.Decode(encoded.AsSpan()));
    }

    [Fact]
    public void Decode_DestinationTooSmall_ClearsPartialOutputRatherThanLeavingPrefix()
    {
        byte[] destination = [0xA5, 0xA5];
        Assert.Throws<ArgumentException>(() => Base32.Decode("MZXW6YTB".AsSpan(), destination));
        Assert.True(destination.AsSpan().IndexOfAnyExcept((byte)0) < 0);
    }

    [Theory]
    [InlineData("MZXW6YTB", 8)] // Forward writes would corrupt unread characters.
    [InlineData("MZXW6YTB", 0)]
    [InlineData("MZXW6YTB", 1)] // Byte overlap need not align with a character.
    [InlineData("MZXW6YTB!", 8)]
    public void Decode_OverlappingStorage_RejectsWithoutChangingSourceOrDestination(string encoded, int byteOffset)
    {
        char[] text = encoded.ToCharArray();
        byte[] originalDestination = MemoryMarshal.AsBytes(text.AsSpan()).Slice(byteOffset, 5).ToArray();
        try
        {
            var exception = Record.Exception(() =>
                Base32.Decode(text.AsSpan(), MemoryMarshal.AsBytes(text.AsSpan()).Slice(byteOffset, 5)));

            Assert.Equal(encoded.ToCharArray(), text);
            Assert.True(CryptographicOperations.FixedTimeEquals(
                originalDestination, MemoryMarshal.AsBytes(text.AsSpan()).Slice(byteOffset, 5)));
            Assert.Equal("destination", Assert.IsType<ArgumentException>(exception).ParamName);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(text.AsSpan()));
            CryptographicOperations.ZeroMemory(originalDestination);
        }
    }
}