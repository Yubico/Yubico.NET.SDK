using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using System.Formats.Cbor;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Ctap2.Extensions;
using NSubstitute;
namespace Audit20261111.DocExamples.Test11;
public sealed class DocumentationBody11
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
