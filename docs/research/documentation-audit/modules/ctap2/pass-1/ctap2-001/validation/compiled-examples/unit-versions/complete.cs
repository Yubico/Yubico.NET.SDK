using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Ctap2;
using System.Formats.Cbor;
using Yubico.YubiKit.Ctap2.Cbor;
using Xunit;

namespace AuditCt001.Prepared.unit_versions;

public sealed class Example
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
