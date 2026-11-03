// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Oath.UnitTests;

public sealed class OathInputTests
{
    [Theory]
    [InlineData("A")]
    [InlineData("AAA")]
    [InlineData("AAAAAA")]
    [InlineData("AAAAAAAAA")]
    [InlineData("MY=")]
    [InlineData("MY=======")]
    [InlineData("========")]
    [InlineData("M=Y=====")]
    [InlineData("MZXW6YTB!")]
    public void Decode_MalformedTerminalForms_DoNotProduceCredentialMaterial(string encoded)
    {
        Assert.Throws<ArgumentException>(() => CredentialData.ParseBase32Key(encoded));
    }

    [Theory]
    [InlineData("MY======", "f")]
    [InlineData("MZXQ====", "fo")]
    [InlineData("MZXW6===", "foo")]
    [InlineData("MZXW6YQ=", "foob")]
    [InlineData("MZXW6YTB", "fooba")]
    [InlineData("MZXW6YTBOI======", "foobar")]
    [InlineData("m z x w 6 y t b o i", "foobar")]
    [InlineData("MZ", "f")] // Preserve permissive unused pad bits.
    public void Decode_RfcVectors_PreserveExactBytes(string encoded, string expected)
    {
        foreach (string input in new[] { encoded, encoded.TrimEnd('=') })
        {
            byte[] decoded = CredentialData.ParseBase32Key(input);
            try { Assert.True(CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), decoded)); }
            finally { CryptographicOperations.ZeroMemory(decoded); }
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("%20%20")]
    public void ParseUri_AbsentSecretMaterial_DoesNotCreatePaddedZeroKey(string secret)
    {
        Assert.Throws<ArgumentException>(() => CredentialData.ParseUri($"otpauth://totp/name?secret={secret}"));
    }

    [Theory]
    [InlineData("digits=262")]
    [InlineData("digits=5")]
    [InlineData("counter=-1", "hotp")]
    [InlineData("period=0")]
    public void ParseUri_InvalidNonSecretFields_AreRejectedBeforeDecoding(string query, string type = "totp")
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            CredentialData.ParseUri($"otpauth://{type}/name?secret=INVALID!&{query}"));
        Assert.DoesNotContain("Base32", exception.Message);
    }

    [Theory]
    [InlineData("60/demo")]
    [InlineData("issuer:")]
    [InlineData("name?issuer=")]
    [InlineData("name?issuer=a%3Ab")]
    public void ParseUri_UnrepresentableIdentity_IsRejectedBeforeDecoding(string label)
    {
        string separator = label.Contains('?') ? "&" : "?";
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            CredentialData.ParseUri($"otpauth://totp/{label}{separator}secret=INVALID!"));
        Assert.DoesNotContain("Base32", exception.Message);
    }

    [Theory]
    [InlineData("", null, 30, 6, 0, OathType.Totp)]
    [InlineData("60/demo", null, 30, 6, 0, OathType.Totp)]
    [InlineData("demo", "", 30, 6, 0, OathType.Totp)]
    [InlineData("demo", "a:b", 30, 6, 0, OathType.Totp)]
    [InlineData("a:b", null, 30, 6, 0, OathType.Totp)]
    [InlineData("demo", "60/a", 30, 6, 0, OathType.Totp)]
    [InlineData("demo", null, 0, 6, 0, OathType.Totp)]
    [InlineData("demo", null, 30, 262, 0, OathType.Totp)]
    [InlineData("demo", null, 30, -250, 0, OathType.Totp)]
    [InlineData("demo", null, 30, 9, 0, OathType.Totp)]
    [InlineData("demo", null, 30, 6, -1, OathType.Hotp)]
    [InlineData("ééééééééééééééééééééééééééééééééé", null, 30, 6, 0, OathType.Totp)]
    public async Task Put_InvalidRequest_SendsNothingAndPreservesOwnedSeed(
        string name, string? issuer, int period, int digits, int counter, OathType type)
    {
        var connection = Connection();
        await using var session = await OathSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        byte[] input = [0xA5];
        using var data = new CredentialData
        {
            Name = name,
            Issuer = issuer,
            Period = period,
            Digits = digits,
            Counter = counter,
            OathType = type,
            HashAlgorithm = OathHashAlgorithm.Sha1,
            Secret = input
        };
        ReadOnlyMemory<byte> borrowed = data.Secret;
        try
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => session.PutCredentialAsync(data, TestContext.Current.CancellationToken));
            Assert.Single(connection.TransmittedCommands); // SELECT only.
            Assert.True(CryptographicOperations.FixedTimeEquals(input, borrowed.Span));
        }
        finally { Clear(connection); CryptographicOperations.ZeroMemory(input); }
    }

    [Fact]
    public async Task Put_EmptySeed_DoesNotTransmitAnAllZeroKey()
    {
        var connection = Connection();
        await using var session = await OathSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        using var data = new CredentialData { Name = "name", OathType = OathType.Totp, HashAlgorithm = OathHashAlgorithm.Sha1, Secret = ReadOnlyMemory<byte>.Empty };
        await Assert.ThrowsAnyAsync<ArgumentException>(() => session.PutCredentialAsync(data, TestContext.Current.CancellationToken));
        Assert.Single(connection.TransmittedCommands);
        Clear(connection);
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("60/demo", null)]
    [InlineData("demo", "")]
    [InlineData("demo", "a:b")]
    [InlineData("a:b", null)]
    [InlineData("demo", "60/a")]
    [InlineData("ééééééééééééééééééééééééééééééééé", null)]
    public async Task Rename_AmbiguousOrOversizedIdentity_DoesNotTransmit(string name, string? issuer)
    {
        var connection = Connection();
        await using var session = await OathSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        var original = new Credential(session.DeviceId, "old"u8.ToArray(), null, "old", OathType.Totp, 30, false);
        await Assert.ThrowsAnyAsync<ArgumentException>(() => session.RenameCredentialAsync(original, issuer, name, TestContext.Current.CancellationToken));
        Assert.Single(connection.TransmittedCommands);
        Assert.Equal("old"u8.ToArray(), original.Id.ToArray());
        Clear(connection);
    }

    [Theory]
    [InlineData(OathType.Totp, "a:b", "issuer", 30)]
    [InlineData(OathType.Totp, ":name", null, 30)]
    [InlineData(OathType.Totp, "60/demo", "issuer", 60)]
    [InlineData(OathType.Hotp, "60/demo", null, 30)]
    [InlineData(OathType.Hotp, "a:b", "issuer", -1)]
    public async Task Put_RepresentableMetadata_RoundTripsAndPreservesSevenDigits(OathType type, string name, string? issuer, int period)
    {
        var connection = Connection();
        await using var session = await OathSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        byte[] input = [0xA5];
        using var data = new CredentialData { Name = name, Issuer = issuer, OathType = type, Period = period, Digits = 7, Counter = int.MaxValue, HashAlgorithm = OathHashAlgorithm.Sha1, Secret = input };
        try
        {
            await session.PutCredentialAsync(data, TestContext.Current.CancellationToken);
            byte[] command = connection.TransmittedCommands[1];
            using var tlvs = TlvHelper.DecodeList(command.AsSpan(5, command[4]));
            var parsed = Credential.ParseCredentialId(tlvs[0].Value.Span, type);
            Assert.Equal(issuer, parsed.Issuer);
            Assert.Equal(name, parsed.Name);
            Assert.Equal(type == OathType.Totp ? period : 0, parsed.Period);
            Assert.Equal(7, tlvs[1].Value.Span[1]);
            if (type == OathType.Hotp) { Assert.Equal(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, tlvs[2].Value.ToArray()); }
            Assert.True(CryptographicOperations.FixedTimeEquals(input, data.Secret.Span));
        }
        finally { Clear(connection); CryptographicOperations.ZeroMemory(input); }
    }

    [Theory]
    [InlineData(OathType.Totp, 30)]
    [InlineData(OathType.Totp, 60)]
    [InlineData(OathType.Hotp, 30)]
    [InlineData(OathType.Hotp, 0)]
    public async Task Rename_Exactly64Utf8Bytes_RoundTripsWithoutRestrictingRepresentableColons(OathType type, int period)
    {
        var connection = Connection();
        await using var session = await OathSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        string prefix = type == OathType.Totp && period != 30 ? "60/" : "";
        string name = new('a', 64 - prefix.Length - "issuer:".Length - ":tail".Length);
        name += ":tail";
        var original = new Credential(session.DeviceId, "old"u8.ToArray(), null, "old", type, period, false);
        var renamed = await session.RenameCredentialAsync(original, "issuer", name, TestContext.Current.CancellationToken);
        Assert.Equal(64, renamed.Id.Length);
        Assert.Equal(("issuer", name, type == OathType.Totp ? period : 0), Credential.ParseCredentialId(renamed.Id.Span, type));
        using var tlvs = TlvHelper.DecodeList(connection.TransmittedCommands[1].AsSpan(5, connection.TransmittedCommands[1][4]));
        Assert.Equal(renamed.Id.ToArray(), tlvs[1].Value.ToArray());
        Assert.Equal("old"u8.ToArray(), original.Id.ToArray());
        Clear(connection);
    }

    [Fact]
    public async Task Put_HotpDefaultCounterAndIgnoredPeriod_OmitsImf()
    {
        var connection = Connection();
        await using var session = await OathSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        using var data = CredentialData.ParseUri("otpauth://hotp/name?secret=MY&period=-1");
        Assert.Equal(0, data.Counter);
        try
        {
            await session.PutCredentialAsync(data, TestContext.Current.CancellationToken);
            byte[] command = connection.TransmittedCommands[1];
            using var tlvs = TlvHelper.DecodeList(command.AsSpan(5, command[4]));
            Assert.Equal(2, tlvs.Count);
            Assert.Equal("name"u8.ToArray(), tlvs[0].Value.ToArray());
        }
        finally { Clear(connection); }
    }

    private static RecordingSmartCardConnection Connection() => new(
        [0x79, 3, 5, 7, 0, 0x71, 4, 1, 2, 3, 4, 0x90, 0], [0x90, 0]);
    private static void Clear(RecordingSmartCardConnection connection)
    {
        foreach (byte[] command in connection.TransmittedCommands) { CryptographicOperations.ZeroMemory(command); }
    }
}