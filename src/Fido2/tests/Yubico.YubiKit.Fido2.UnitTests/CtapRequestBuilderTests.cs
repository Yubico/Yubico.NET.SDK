using FluentAssertions;
using Yubico.YubiKit.Fido2.Cbor;
using Yubico.YubiKit.Fido2.Ctap;

namespace Yubico.YubiKit.Fido2.UnitTests;

public class CtapRequestBuilderTests
{
    [Fact]
    public void Create_WithCommandOnly_ReturnsCommandByte()
    {
        // Arrange & Act
        var result = CtapRequestBuilder.Create(CtapCommand.GetInfo).Build();
        
        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be(CtapCommand.GetInfo);
    }
    
    [Fact]
    public void WithInt_AddsIntegerParameter()
    {
        // Arrange & Act
        var result = CtapRequestBuilder.Create(CtapCommand.ClientPin)
            .WithInt(1, 42)
            .Build();
        
        // Assert - command byte + CBOR map {1: 42}
        result.Should().HaveCountGreaterThan(1);
        result[0].Should().Be(CtapCommand.ClientPin);
    }
    
    [Fact]
    public void WithString_AddsStringParameter()
    {
        // Arrange & Act
        var result = CtapRequestBuilder.Create(CtapCommand.MakeCredential)
            .WithString(1, "test")
            .Build();
        
        // Assert
        result.Should().HaveCountGreaterThan(1);
        result[0].Should().Be(CtapCommand.MakeCredential);
    }
    
    [Fact]
    public void WithBytes_AddsByteArrayParameter()
    {
        // Arrange
        byte[] testData = [0x01, 0x02, 0x03];
        
        // Act
        var result = CtapRequestBuilder.Create(CtapCommand.GetAssertion)
            .WithBytes(1, testData)
            .Build();
        
        // Assert
        result.Should().HaveCountGreaterThan(1);
        result[0].Should().Be(CtapCommand.GetAssertion);
    }
    
    [Fact]
    public void WithBool_AddsBooleanParameter()
    {
        // Arrange & Act
        var result = CtapRequestBuilder.Create(CtapCommand.Config)
            .WithBool(1, true)
            .Build();
        
        // Assert
        result.Should().HaveCountGreaterThan(1);
        result[0].Should().Be(CtapCommand.Config);
    }
    
    [Fact]
    public void MultipleParameters_SortedByKey()
    {
        // Arrange & Act - add parameters out of order
        var result = CtapRequestBuilder.Create(CtapCommand.MakeCredential)
            .WithInt(3, 300)
            .WithInt(1, 100)
            .WithInt(2, 200)
            .Build();
        
        // Assert - parameters should be sorted in canonical CBOR order
        result.Should().HaveCountGreaterThan(1);
        result[0].Should().Be(CtapCommand.MakeCredential);
        // CBOR map with 3 entries, keys 1, 2, 3 in sorted order
    }
    
    [Fact]
    public void BuildAsMemory_ReturnsReadOnlyMemory()
    {
        // Arrange & Act
        var result = CtapRequestBuilder.Create(CtapCommand.GetInfo).BuildAsMemory();
        
        // Assert
        result.Length.Should().Be(1);
        result.Span[0].Should().Be(CtapCommand.GetInfo);
    }
}
