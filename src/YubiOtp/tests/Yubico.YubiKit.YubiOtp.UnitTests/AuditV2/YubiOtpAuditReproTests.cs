namespace Yubico.YubiKit.YubiOtp.UnitTests.AuditV2;

public sealed class YubiOtpAuditReproTests
{
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_UpdateDoesNotSilentlyDiscardAllowUpdate()
    {
        using var config = new UpdateConfiguration();
        config.AllowUpdate(false);
        byte[] disabled = config.GetConfig();
        config.AllowUpdate(true);
        byte[] enabled = config.GetConfig();

        Assert.NotEqual(disabled[45], enabled[45]);
        Assert.Equal((byte)ExtendedFlag.AllowUpdate, (byte)(enabled[45] & (byte)ExtendedFlag.AllowUpdate));
    }

    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_UpdateRejectsUnsupportedProtectSlot2()
    {
        using var config = new UpdateConfiguration();
        Assert.Throws<ArgumentException>(() => config.ProtectSlot2());
    }

    /// <summary>
    ///     The request bytes are what ykman sends: challenge.ljust(64, b"\1" if challenge.endswith(b"\0") else b"\0").
    ///     A short challenge and its explicitly padded 64-byte form are the same request by design; in
    ///     short-challenge mode the YubiKey reads the final byte as the end marker either way.
    /// </summary>
    [Theory]
    [InlineData(new byte[0], (byte)0x00)]
    [InlineData(new byte[] { 0x41 }, (byte)0x00)]
    [InlineData(new byte[] { 0x41, 0x00 }, (byte)0x01)]
    [InlineData(new byte[] { 0x00 }, (byte)0x01)]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_PaddingMatchesYkman(byte[] challenge, byte padByte)
    {
        byte[] expected = [.. challenge, .. Enumerable.Repeat(padByte, 64 - challenge.Length)];

        Assert.Equal(expected, YubiOtpSession.PadHmacChallenge(challenge));
    }

    [Theory]
    [InlineData(63)]
    [InlineData(64)]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_LongChallengesAreSentAsGiven(int length)
    {
        byte[] challenge = Enumerable.Range(1, length).Select(i => (byte)i).ToArray();

        byte[] padded = YubiOtpSession.PadHmacChallenge(challenge);

        Assert.Equal(challenge, padded[..length]);
        Assert.All(padded[length..], b => Assert.Equal(0x00, b));
    }
}