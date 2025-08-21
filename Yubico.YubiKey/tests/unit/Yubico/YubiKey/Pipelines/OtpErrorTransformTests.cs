using Xunit;
using Yubico.YubiKey.Otp;

namespace Yubico.YubiKey.Pipelines;

public class OtpErrorTransformTests
{
    [Theory]
    [InlineData(0, 1)] // First increment
    [InlineData(5, 6)] // Normal increment  
    [InlineData(254, 255)] // Near byte boundary
    public void IsValidSequenceProgression_NormalIncrement_ReturnsTrue(
        byte before,
        byte after)
    {
        var beforeStatus = CreateOtpStatus(before, hasConfigs: true);
        var afterStatus = CreateOtpStatus(after, hasConfigs: true);

        var isValid = OtpErrorTransform.IsValidSequenceProgression(beforeStatus, afterStatus);

        Assert.True(isValid);
    }

    [Theory]
    [InlineData(0, 2)] // Skip increment
    [InlineData(5, 7)] // Jump by 2
    [InlineData(10, 8)] // Backwards
    [InlineData(255, 0)] // Byte overflow (should be rejected as normal increment)
    public void IsValidSequenceProgression_InvalidIncrement_ReturnsFalse(
        byte before,
        byte after)
    {
        var beforeStatus = CreateOtpStatus(before, hasConfigs: true);
        var afterStatus = CreateOtpStatus(after, hasConfigs: true); // Still has configs

        var isValid = OtpErrorTransform.IsValidSequenceProgression(beforeStatus, afterStatus);

        Assert.False(isValid);
    }

    [Theory]
    [InlineData(1)] // Delete from sequence 1
    [InlineData(5)] // Delete from sequence 5  
    [InlineData(255)] // Delete from sequence 255
    public void IsValidSequenceProgression_ValidReset_ReturnsTrue(
        byte before)
    {
        var beforeStatus = CreateOtpStatus(before, hasConfigs: true);
        var afterStatus = CreateOtpStatus(0, hasConfigs: false); // No configs remain

        var isValid = OtpErrorTransform.IsValidSequenceProgression(beforeStatus, afterStatus);

        Assert.True(isValid);
    }

    [Theory]
    [InlineData(0, false)] // Already zero
    [InlineData(1, true)] // Reset but configs still exist
    [InlineData(5, true)] // Reset but configs still exist
    public void IsValidSequenceProgression_InvalidReset_ReturnsFalse(
        byte before,
        bool afterHasConfigs)
    {
        var beforeStatus = CreateOtpStatus(before, hasConfigs: true);
        var afterStatus = CreateOtpStatus(0, hasConfigs: afterHasConfigs);

        var isValid = OtpErrorTransform.IsValidSequenceProgression(beforeStatus, afterStatus);

        Assert.False(isValid);
    }

    private static OtpStatus CreateOtpStatus(
        byte sequenceNumber,
        bool hasConfigs) => new()
    {
        FirmwareVersion = new FirmwareVersion(),
        SequenceNumber = sequenceNumber,
        TouchLevel = 0x00,
        ShortPressConfigured = hasConfigs,
        LongPressConfigured = false, // Only test one slot for simplicity
        ShortPressRequiresTouch = false,
        LongPressRequiresTouch = false,
        LedBehaviorInverted = false
    };
}
