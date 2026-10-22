using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.UnitTests.Cryptography;
using Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Apdu.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Scp;

/// <summary>
///     Under SCP with R-MAC in the security level, the card authenticates every success response,
///     including one with no data (GlobalPlatform SCP03 Amd D §6.2.5); a YubiKey appends the 8-byte
///     R-MAC to an empty 9000 under SCP03 and SCP11. A success response without one is therefore not
///     the card's, and is refused like a wrong MAC. Error status words carry no R-MAC and pass through.
/// </summary>
[Collection(CryptographyProvidersCollection.Name)]
public sealed class ScpEmptyResponseTests
{
    private const byte InsExternalAuthenticate = 0x82;

    [Theory]
    [InlineData(0x90, 0x00)]
    [InlineData(0x62, 0x82)]
    [InlineData(0x63, 0xC2)]
    public async Task SuccessOrWarningWithoutData_IsRefusedAndLatchesRecovery(byte sw1, byte sw2)
    {
        var connection = new FakeSmartCardConnection();
        connection.EnqueueResponse(new byte[] { sw1, sw2 });
        var baseProtocol = new PcscProtocol(connection);
        using var state = CreateZeroedScpState();
        using var wrapped = new PcscProtocolScp(baseProtocol,
            ScpInitializer.CreateSecureProcessor(baseProtocol.GetBaseCommandProcessor(), state,
                new FirmwareVersion(5, 7, 2), 0xC0, baseProtocol.MarkRecoveryRequired),
            data => data.ToArray());

        var failure = await Assert.ThrowsAsync<BadResponseException>(() =>
            wrapped.TransmitAndReceiveAsync(new ApduCommand(0, 0xCA, 0, 0),
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("R-MAC", failure.Message);
        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wrapped.TransmitAndReceiveAsync(new ApduCommand(0, 0xCB, 0, 0),
                cancellationToken: TestContext.Current.CancellationToken));
        Assert.Same(failure, refusal.InnerException);
    }

    [Fact]
    public async Task ErrorWithoutData_IsReturnedAsTheCardsStatus()
    {
        var connection = new FakeSmartCardConnection();
        connection.EnqueueResponse(new byte[] { 0x69, 0x84 });
        var baseProtocol = new PcscProtocol(connection);
        using var state = CreateZeroedScpState();
        using var wrapped = new PcscProtocolScp(baseProtocol,
            ScpInitializer.CreateSecureProcessor(baseProtocol.GetBaseCommandProcessor(), state,
                new FirmwareVersion(5, 7, 2), 0xC0, baseProtocol.MarkRecoveryRequired),
            data => data.ToArray());

        var failure = await Assert.ThrowsAsync<ApduException>(() =>
            wrapped.TransmitAndReceiveAsync(new ApduCommand(0, 0xCA, 0, 0),
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(unchecked((short)0x6984), failure.SW);
    }

    /// <summary>The R-MAC session starts after EXTERNAL AUTHENTICATE; its own empty 9000 is unauthenticated by design.</summary>
    [Fact]
    public async Task ExternalAuthenticateWithoutData_IsAccepted()
    {
        var connection = new FakeSmartCardConnection();
        connection.EnqueueResponse(new byte[] { 0x90, 0x00 });
        var baseProtocol = new PcscProtocol(connection);
        using var state = CreateZeroedScpState();
        using var processor = new ScpProcessor(baseProtocol.GetBaseCommandProcessor(), state, baseProtocol.MarkRecoveryRequired);

        var response = await processor.TransmitAsync(
            new ApduCommand(0x84, InsExternalAuthenticate, 0x33, 0x00, new byte[8]),
            useScp: true, encrypt: false, TestContext.Current.CancellationToken);

        Assert.Equal(SWConstants.Success, response.SW);
        Assert.Equal(0, response.Data.Length);
    }

    private static ScpState CreateZeroedScpState() =>
        new(new SessionKeys(new byte[16], new byte[16], new byte[16]), new byte[16]);
}