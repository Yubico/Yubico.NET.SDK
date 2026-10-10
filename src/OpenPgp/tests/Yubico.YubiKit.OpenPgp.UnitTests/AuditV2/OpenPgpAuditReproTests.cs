using System.Security.Cryptography;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.OpenPgp.UnitTests.AuditV2;

public sealed class OpenPgpAuditReproTests
{
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public async Task YESDK1634_ImportDoesNotTargetTemplateSlotInsteadOfRequestedSlot()
    {
        byte[] appData = ApplicationData();
        var connection = new RecordingSmartCardConnection(
            [0x90, 0x00], [0x05, 0x08, 0x00, 0x90, 0x00], appData,
            [0x90, 0x00], [0x90, 0x00], appData);
        await using var session = await OpenPgpSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);
        var template = new RsaKeyTemplate(KeyRef.Aut, new byte[] { 0x01, 0x00, 0x01 },
            new byte[] { 0x03 }, new byte[] { 0x05 });

        await session.PutKeyAsync(KeyRef.Sig, template, RsaAttributes.Create(RsaSize.Rsa2048),
            TestContext.Current.CancellationToken);

        byte[] import = Assert.Single(connection.TransmittedCommands, command => command[1] == 0xDB);
        // The outer 4D length is variable; the first child CRT must be SIG (B6 00), not AUT (A4 00).
        Assert.Equal([0xB6, 0x00], import.AsSpan(7, 2).ToArray());
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [Trait("Audit", "YESDK-1633")]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1633_ShortKdfCountStillHashesCompletePin(int count)
    {
        using var kdf = new KdfIterSaltedS2k
        {
            HashAlgorithm = KdfHashAlgorithm.Sha256,
            SaltUser = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            IterationCount = count
        };
        byte[] expected = SHA256.HashData([1, 2, 3, 4, 5, 6, 7, 8, .. "123456"u8]);
        byte[] actual = kdf.Process(Pw.User, "123456"u8);
        try
        {
            Assert.Equal(expected, actual);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(expected);
            CryptographicOperations.ZeroMemory(actual);
        }
    }

    [Fact]
    [Trait("Audit", "YESDK-1633")]
    public void YESDK1633_UnsignedKdfCountDoesNotWrapNegative()
    {
        byte[] encoded =
        [
            0x81, 1, 3, 0x82, 1, 8, 0x83, 4, 0x80, 0, 0, 0,
            0x84, 8, 1, 2, 3, 4, 5, 6, 7, 8
        ];
        using var parsed = Kdf.Parse(encoded);
        Assert.Throws<ArgumentException>(() => parsed.Process(Pw.User, "123456"u8));
    }

    private static byte[] ApplicationData()
    {
        // Application-related data requires all three algorithm attributes and PIN status.
        using var aid = new Tlv(0x4F, [0xD2, 0x76, 0, 1, 0x24, 1, 3, 4, 0, 6, 0x12, 0x34, 0x56, 0x78]);
        using var capabilities = new Tlv(0xC0, [0x75, 0, 0, 0xFF, 4, 0x80, 0, 0xFF, 0, 0]);
        byte[] rsa = [1, 8, 0, 0, 0x11, 0];
        using var sig = new Tlv(0xC1, rsa);
        using var dec = new Tlv(0xC2, rsa);
        using var aut = new Tlv(0xC3, rsa);
        using var pw = new Tlv(0xC4, [0, 0x7F, 0x7F, 0x7F, 3, 0, 3]);
        using var fingerprints = new Tlv(0xC5, new byte[60]);
        using var caFingerprints = new Tlv(0xC6, new byte[60]);
        using var generationTimes = new Tlv(0xCD, new byte[12]);
        using var discretionary = new Tlv(0x73,
            [.. capabilities.AsSpan(), .. sig.AsSpan(), .. dec.AsSpan(), .. aut.AsSpan(),
                .. pw.AsSpan(), .. fingerprints.AsSpan(), .. caFingerprints.AsSpan(), .. generationTimes.AsSpan()]);
        using var outer = new Tlv(0x6E, [.. aid.AsSpan(), .. discretionary.AsSpan()]);
        return [.. outer.AsSpan(), 0x90, 0x00];
    }
}
