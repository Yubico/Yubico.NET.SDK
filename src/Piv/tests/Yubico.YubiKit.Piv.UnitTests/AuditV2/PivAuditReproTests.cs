// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Security.Cryptography;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests.AuditV2;

public class PivAuditReproTests
{
    [Theory]
    [InlineData(10)]
    [InlineData(64)]
    [Trait("Audit", "YESDK-1632")]
    public async Task YESDK1632_Ed25519ChallengeContainsEntireMessage(int messageLength)
    {
        byte[] message = Enumerable.Range(1, messageLength).Select(i => (byte)i).ToArray();
        var connection = new RecordingSmartCardConnection(
            [0x90, 0x00], // SELECT PIV
            [0x00, 0x00, 0x01, 0x90, 0x00], // version
            [0x01, 0x01, 0x03, 0x02, 0x02, 0x00, 0x00, 0x05, 0x01, 0x01, 0x90, 0x00],
            [0x7C, 0x03, 0x82, 0x01, 0xAA, 0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        _ = await session.SignOrDecryptAsync(
            PivSlot.Authentication, PivAlgorithm.Ed25519, message, TestContext.Current.CancellationToken);

        byte[] apdu = connection.TransmittedCommands[^1];
        Assert.Equal(0x87, apdu[1]);
        Assert.Equal((byte)PivAlgorithm.Ed25519, apdu[2]);
        // Entire short APDU, including the nested 7C / 82 / 81 dynamic-authentication template.
        byte[] expected = [0x00, 0x87, (byte)PivAlgorithm.Ed25519, (byte)PivSlot.Authentication,
            (byte)(messageLength + 6), 0x7C, (byte)(messageLength + 4), 0x82, 0x00,
            0x81, (byte)messageLength, .. message];
        Assert.Equal(expected, apdu);
    }

    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public async Task YESDK1634_ShortEcDigestIsLeftPaddedNotRightPadded()
    {
        var connection = CreateConnection();
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        _ = await session.SignOrDecryptAsync(PivSlot.Authentication, PivAlgorithm.EccP256,
            new byte[] { 0xA1, 0xB2 }, TestContext.Current.CancellationToken);

        byte[] apdu = connection.TransmittedCommands[^1];
        Assert.Equal(0x81, apdu[9]);
        Assert.Equal(32, apdu[10]);
        Assert.Equal(new byte[30], apdu.AsSpan(11, 30).ToArray());
        Assert.Equal(new byte[] { 0xA1, 0xB2 }, apdu.AsSpan(41, 2).ToArray());
    }

    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public async Task YESDK1634_OversizedRsaBlockIsRejectedRatherThanSilentlyTruncated()
    {
        var connection = CreateConnection();
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        var block = new byte[129];
        block[0] = 0xA1;
        await Assert.ThrowsAsync<ArgumentException>(() => session.SignOrDecryptAsync(
            PivSlot.Authentication, PivAlgorithm.Rsa1024, block, TestContext.Current.CancellationToken));
        Assert.DoesNotContain(connection.TransmittedCommands, apdu => apdu[1] == 0x87);
    }

    private static RecordingSmartCardConnection CreateConnection() => new(
        [0x90, 0x00],
        [0x00, 0x00, 0x01, 0x90, 0x00],
        [0x01, 0x01, 0x03, 0x02, 0x02, 0x00, 0x00, 0x05, 0x01, 0x01, 0x90, 0x00],
        [0x7C, 0x03, 0x82, 0x01, 0xAA, 0x90, 0x00]);
}
