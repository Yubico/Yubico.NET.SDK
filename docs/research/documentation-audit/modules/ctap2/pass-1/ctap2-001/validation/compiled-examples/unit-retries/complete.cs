using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Pin;
using System.Formats.Cbor;
using Yubico.YubiKit.Ctap2.Cbor;
using Xunit;
using NSubstitute;

namespace AuditCt001.Prepared.unit_retries;

public sealed class Example
{
[Fact]
public async Task PinRetries_EncodesProtocolAndSubcommand()
{
    var session = Substitute.For<ICtap2Session>();
    ReadOnlyMemory<byte> mockResponse = new byte[] { 0xA1, 0x03, 0x08 }; // {pinRetries: 8}
    ReadOnlyMemory<byte> capturedRequest = default;
    session.SendCborRequestAsync(
        Arg.Do<ReadOnlyMemory<byte>>(value => capturedRequest = value),
        Arg.Any<CancellationToken>()).Returns(mockResponse);
    using var protocol = new PinUvAuthProtocolV2();
    using var clientPin = new ClientPin(session, protocol);

    var retries = await clientPin.GetPinRetriesAsync(TestContext.Current.CancellationToken);

    Assert.Equal(8, retries.RetriesRemaining);
    Assert.Equal(CtapCommand.ClientPin, capturedRequest.Span[0]);
    var reader = new CborReader(capturedRequest.Slice(1), CborConformanceMode.Ctap2Canonical);
    Assert.Equal(2, reader.ReadStartMap());
    Assert.Equal(1, reader.ReadInt32());
    Assert.Equal(protocol.Version, reader.ReadInt32());
    Assert.Equal(2, reader.ReadInt32());
    Assert.Equal(ClientPinSubCommand.GetRetries, reader.ReadInt32());
    reader.ReadEndMap();
}
}
