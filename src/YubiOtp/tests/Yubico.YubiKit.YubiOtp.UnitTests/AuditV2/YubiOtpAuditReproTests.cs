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

    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_DistinctChallengeLengthsDoNotHaveSameRequestBytes()
    {
        byte[] shortChallenge = [0x41];
        byte[] fixedChallenge = [0x41, .. new byte[63]];
        byte[] shortRequest = YubiOtpSession.PadHmacChallenge(shortChallenge);
        byte[] fixedRequest = YubiOtpSession.PadHmacChallenge(fixedChallenge);

        Assert.NotEqual(shortRequest, fixedRequest);
    }
}
