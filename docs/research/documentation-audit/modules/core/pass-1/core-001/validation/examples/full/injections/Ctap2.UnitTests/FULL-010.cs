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
namespace Audit20261111.DocExamples.Test10;
public sealed class DocumentationBody10
{
[Fact]
public void AuthenticatorInfo_Decode_ParsesVersions()
{
    // Partial parser fixture, not a conforming GetInfo envelope: the required AAGUID is absent.
    var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
    writer.WriteStartMap(1);
    writer.WriteInt32(1);
    writer.WriteStartArray(1);
    writer.WriteTextString("FIDO_2_0");
    writer.WriteEndArray();
    writer.WriteEndMap();
    byte[] cborData = writer.Encode();
    
    // Act
    var info = AuthenticatorInfo.Decode(cborData);
    
    // Assert
    Assert.Contains("FIDO_2_0", info.Versions);
}
}
