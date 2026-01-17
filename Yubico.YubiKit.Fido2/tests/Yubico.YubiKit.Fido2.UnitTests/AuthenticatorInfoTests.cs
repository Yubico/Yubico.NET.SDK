using System.Formats.Cbor;
using FluentAssertions;
using Yubico.YubiKit.Core.Cryptography.Cose;

namespace Yubico.YubiKit.Fido2.UnitTests;

public class AuthenticatorInfoTests
{
    [Fact]
    public void Decode_MinimalGetInfoResponse_ParsesVersions()
    {
        // Arrange - Minimal GetInfo response with just versions
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteInt32(0x01); // versions key
        writer.WriteStartArray(2);
        writer.WriteTextString("FIDO_2_0");
        writer.WriteTextString("U2F_V2");
        writer.WriteEndArray();
        writer.WriteEndMap();
        var data = writer.Encode();
        
        // Act
        var info = AuthenticatorInfo.Decode(data);
        
        // Assert
        info.Versions.Should().HaveCount(2);
        info.Versions.Should().Contain("FIDO_2_0");
        info.Versions.Should().Contain("U2F_V2");
    }
    
    [Fact]
    public void Decode_WithAaguid_ParsesCorrectly()
    {
        // Arrange
        byte[] aaguid = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                         0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10];
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteInt32(0x01); // versions
        writer.WriteStartArray(1);
        writer.WriteTextString("FIDO_2_0");
        writer.WriteEndArray();
        writer.WriteInt32(0x03); // aaguid
        writer.WriteByteString(aaguid);
        writer.WriteEndMap();
        var data = writer.Encode();
        
        // Act
        var info = AuthenticatorInfo.Decode(data);
        
        // Assert
        info.Aaguid.ToArray().Should().BeEquivalentTo(aaguid);
    }
    
    [Fact]
    public void Decode_WithOptions_ParsesBoolMap()
    {
        // Arrange
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteInt32(0x01); // versions
        writer.WriteStartArray(1);
        writer.WriteTextString("FIDO_2_1");
        writer.WriteEndArray();
        writer.WriteInt32(0x04); // options
        writer.WriteStartMap(3);
        writer.WriteTextString("rk");
        writer.WriteBoolean(true);
        writer.WriteTextString("up");
        writer.WriteBoolean(true);
        writer.WriteTextString("uv");
        writer.WriteBoolean(false);
        writer.WriteEndMap();
        writer.WriteEndMap();
        var data = writer.Encode();
        
        // Act
        var info = AuthenticatorInfo.Decode(data);
        
        // Assert
        info.Options.Should().HaveCount(3);
        info.Options["rk"].Should().BeTrue();
        info.Options["up"].Should().BeTrue();
        info.Options["uv"].Should().BeFalse();
    }
    
    [Fact]
    public void Decode_WithMaxMsgSize_ParsesInteger()
    {
        // Arrange
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteInt32(0x01); // versions
        writer.WriteStartArray(1);
        writer.WriteTextString("FIDO_2_0");
        writer.WriteEndArray();
        writer.WriteInt32(0x05); // maxMsgSize
        writer.WriteInt32(2048);
        writer.WriteEndMap();
        var data = writer.Encode();
        
        // Act
        var info = AuthenticatorInfo.Decode(data);
        
        // Assert
        info.MaxMsgSize.Should().Be(2048);
    }
    
    [Fact]
    public void Decode_WithPinProtocols_ParsesIntArray()
    {
        // Arrange
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteInt32(0x01); // versions
        writer.WriteStartArray(1);
        writer.WriteTextString("FIDO_2_1");
        writer.WriteEndArray();
        writer.WriteInt32(0x06); // pinUvAuthProtocols
        writer.WriteStartArray(2);
        writer.WriteInt32(2);
        writer.WriteInt32(1);
        writer.WriteEndArray();
        writer.WriteEndMap();
        var data = writer.Encode();
        
        // Act
        var info = AuthenticatorInfo.Decode(data);
        
        // Assert
        info.PinUvAuthProtocols.Should().HaveCount(2);
        info.PinUvAuthProtocols.Should().Contain(1);
        info.PinUvAuthProtocols.Should().Contain(2);
    }
    
    [Fact]
    public void Decode_WithAlgorithms_ParsesCredentialParameters()
    {
        // Arrange
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteInt32(0x01); // versions
        writer.WriteStartArray(1);
        writer.WriteTextString("FIDO_2_0");
        writer.WriteEndArray();
        writer.WriteInt32(0x0A); // algorithms
        writer.WriteStartArray(1);
        // Algorithm entry
        writer.WriteStartMap(2);
        writer.WriteTextString("alg");
        writer.WriteInt32(-7); // ES256
        writer.WriteTextString("type");
        writer.WriteTextString("public-key");
        writer.WriteEndMap();
        writer.WriteEndArray();
        writer.WriteEndMap();
        var data = writer.Encode();
        
        // Act
        var info = AuthenticatorInfo.Decode(data);
        
        // Assert
        info.Algorithms.Should().HaveCount(1);
        info.Algorithms[0].Algorithm.Should().Be(CoseAlgorithmIdentifier.ES256);
        info.Algorithms[0].Type.Should().Be("public-key");
    }
    
    [Fact]
    public void Decode_WithExtensions_ParsesStringArray()
    {
        // Arrange
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteInt32(0x01); // versions
        writer.WriteStartArray(1);
        writer.WriteTextString("FIDO_2_1");
        writer.WriteEndArray();
        writer.WriteInt32(0x02); // extensions
        writer.WriteStartArray(3);
        writer.WriteTextString("hmac-secret");
        writer.WriteTextString("credProtect");
        writer.WriteTextString("credBlob");
        writer.WriteEndArray();
        writer.WriteEndMap();
        var data = writer.Encode();
        
        // Act
        var info = AuthenticatorInfo.Decode(data);
        
        // Assert
        info.Extensions.Should().HaveCount(3);
        info.Extensions.Should().Contain("hmac-secret");
        info.Extensions.Should().Contain("credProtect");
        info.Extensions.Should().Contain("credBlob");
    }
    
    [Fact]
    public void Decode_WithUnknownKeys_SkipsUnknownValues()
    {
        // Arrange - Include an unknown key (0xFF)
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteInt32(0x01); // versions
        writer.WriteStartArray(1);
        writer.WriteTextString("FIDO_2_0");
        writer.WriteEndArray();
        writer.WriteInt32(0xFF); // unknown key
        writer.WriteTextString("unknown value");
        writer.WriteEndMap();
        var data = writer.Encode();
        
        // Act - Should not throw
        var info = AuthenticatorInfo.Decode(data);
        
        // Assert
        info.Versions.Should().HaveCount(1);
    }
}
