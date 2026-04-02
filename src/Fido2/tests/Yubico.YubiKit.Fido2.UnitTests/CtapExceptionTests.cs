using FluentAssertions;
using Yubico.YubiKit.Fido2.Ctap;

namespace Yubico.YubiKit.Fido2.UnitTests;

public class CtapExceptionTests
{
    [Fact]
    public void Constructor_WithStatus_SetsStatus()
    {
        // Arrange & Act
        var exception = new CtapException(CtapStatus.PinInvalid);
        
        // Assert
        exception.Status.Should().Be(CtapStatus.PinInvalid);
        exception.Message.Should().Contain("PIN");
    }
    
    [Fact]
    public void Constructor_WithStatusAndMessage_SetsProperties()
    {
        // Arrange & Act
        var exception = new CtapException(CtapStatus.NoCredentials, "Custom message");
        
        // Assert
        exception.Status.Should().Be(CtapStatus.NoCredentials);
        exception.Message.Should().Be("Custom message");
    }
    
    [Fact]
    public void Constructor_WithInnerException_PreservesInnerException()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner");
        
        // Act
        var exception = new CtapException(CtapStatus.Other, "Outer", inner);
        
        // Assert
        exception.InnerException.Should().Be(inner);
    }
    
    [Fact]
    public void ThrowIfError_WithSuccess_DoesNotThrow()
    {
        // Act & Assert
        var action = () => CtapException.ThrowIfError(CtapStatus.Success);
        action.Should().NotThrow();
    }
    
    [Fact]
    public void ThrowIfError_WithError_ThrowsException()
    {
        // Act & Assert
        var action = () => CtapException.ThrowIfError(CtapStatus.PinBlocked);
        action.Should().Throw<CtapException>()
            .Which.Status.Should().Be(CtapStatus.PinBlocked);
    }
    
    [Fact]
    public void ThrowIfError_WithByte_ThrowsException()
    {
        // Act & Assert
        var action = () => CtapException.ThrowIfError((byte)0x32); // PinBlocked
        action.Should().Throw<CtapException>()
            .Which.Status.Should().Be(CtapStatus.PinBlocked);
    }
    
    [Theory]
    [InlineData(CtapStatus.InvalidCommand, "Invalid CTAP command")]
    [InlineData(CtapStatus.InvalidParameter, "Invalid parameter")]
    [InlineData(CtapStatus.Timeout, "timed out")]
    [InlineData(CtapStatus.CredentialExcluded, "excluded")]
    [InlineData(CtapStatus.NoCredentials, "No credentials")]
    [InlineData(CtapStatus.UserActionTimeout, "timeout")]
    public void Constructor_WithKnownStatus_HasDescriptiveMessage(CtapStatus status, string expectedContains)
    {
        // Arrange & Act
        var exception = new CtapException(status);
        
        // Assert
        exception.Message.Should().ContainEquivalentOf(expectedContains);
    }
}
