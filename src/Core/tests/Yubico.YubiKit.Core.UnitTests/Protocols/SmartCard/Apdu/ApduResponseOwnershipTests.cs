using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;

namespace Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Apdu;

public class ApduResponseOwnershipTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Clear_ZeroesOwnedAliasesWithoutChangingCallerMemory(bool includesStatus)
    {
        byte[] source = includesStatus ? [1, 2, 3, 0x90, 0x00] : [1, 2, 3];
        byte[] original = [.. source];
        var response = includesStatus ? new ApduResponse(source) : new ApduResponse(source, unchecked((short)0x9000));
        ReadOnlyMemory<byte> rawAlias = response.RawData;
        ReadOnlyMemory<byte> dataAlias = response.Data;
        var sameOwner = response;

        sameOwner.Clear();
        response.Clear();

        Assert.Equal(original, source);
        Assert.All(rawAlias.ToArray(), value => Assert.Equal(0, value));
        Assert.All(dataAlias.ToArray(), value => Assert.Equal(0, value));
        Assert.All(response.RawData.ToArray(), value => Assert.Equal(0, value));
    }
}