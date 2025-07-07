using Xunit;
using Yubico.YubiKey.Otp;

namespace Yubico.YubiKey.Pipelines;

public class OtpErrorTransformTests
{
    [Theory]
    [InlineData(0, 1)]      // First increment
    [InlineData(5, 6)]      // Normal increment  
    [InlineData(254, 255)]  // Near byte boundary
    [InlineData(257, 258)]  // Near byte boundary
    
    public void IsValidSequenceProgression_NormalIncrement_ReturnsTrue(int before, int after)
    {
        var status = CreateOtpStatus(touchLevel: 0xFF); // TouchLevel irrelevant
        
        bool isValid = OtpErrorTransform.IsValidSequenceProgression(before, after, status);
        
        Assert.True(isValid);
    }

    [Theory]
    [InlineData(0, 2)]      // Skip increment
    [InlineData(5, 7)]      // Jump by 2
    [InlineData(10, 8)]     // Backwards
    public void IsValidSequenceProgression_InvalidIncrement_ReturnsFalse(int before, int after)
    {
        var status = CreateOtpStatus(touchLevel: 0x00); // TouchLevel irrelevant
        
        bool isValid = OtpErrorTransform.IsValidSequenceProgression(before, after, status);
        
        Assert.False(isValid);
    }

    [Theory]
    [InlineData(1, 0x00)]   // Config bits clear
    [InlineData(5, 0x20)]   // Upper bits set, config clear  
    [InlineData(255, 0xE0)] // Multiple upper bits, config clear
    public void IsValidSequenceProgression_ValidReset_ReturnsTrue(int before, byte touchLevel)
    {
        var status = CreateOtpStatus(touchLevel);
        
        bool isValid = OtpErrorTransform.IsValidSequenceProgression(before, 0, status);
        
        Assert.True(isValid);
    }

    [Theory]
    [InlineData(0, 0x00)]   // Already zero (invalid before state)
    [InlineData(1, 0x01)]   // Config bit set
    [InlineData(5, 0x1F)]   // All config bits set
    [InlineData(5, 0x21)]   // Config + upper bits set
    public void IsValidSequenceProgression_InvalidReset_ReturnsFalse(int before, byte touchLevel)
    {
        var status = CreateOtpStatus(touchLevel);
        
        bool isValid = OtpErrorTransform.IsValidSequenceProgression(before, 0, status);
        
        Assert.False(isValid);
    }

    private static OtpStatus CreateOtpStatus(byte touchLevel) => new()
    {
        FirmwareVersion = new FirmwareVersion(),
        SequenceNumber = 0,
        TouchLevel = touchLevel,
        ShortPressConfigured = false,
        LongPressConfigured = false,
        ShortPressRequiresTouch = false,
        LongPressRequiresTouch = false,
        LedBehaviorInverted = false
    };
}
