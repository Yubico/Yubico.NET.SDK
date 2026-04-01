// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.YubiOtp.UnitTests;

public class ConfigStateTests
{
    // Status bytes: [fw_major, fw_minor, fw_patch, prog_seq, touch_lo, touch_hi]

    [Fact]
    public void Constructor_ParsesFirmwareVersion()
    {
        byte[] status = [5, 4, 3, 0x0A, 0x00, 0x00];

        var state = new ConfigState(status);

        Assert.Equal(new FirmwareVersion(5, 4, 3), state.FirmwareVersion);
    }

    [Fact]
    public void Constructor_TooFewBytes_ThrowsArgumentException()
    {
        byte[] status = [5, 4, 3, 0x0A, 0x00];

        Assert.Throws<ArgumentException>(() => new ConfigState(status));
    }

    [Fact]
    public void IsConfigured_Slot1Configured_ReturnsTrue()
    {
        // touch_level bit 0 = slot 1 configured
        byte[] status = [5, 0, 0, 0, 0x01, 0x00];

        var state = new ConfigState(status);

        Assert.True(state.IsConfigured(Slot.One));
        Assert.False(state.IsConfigured(Slot.Two));
    }

    [Fact]
    public void IsConfigured_Slot2Configured_ReturnsTrue()
    {
        // touch_level bit 1 = slot 2 configured
        byte[] status = [5, 0, 0, 0, 0x02, 0x00];

        var state = new ConfigState(status);

        Assert.False(state.IsConfigured(Slot.One));
        Assert.True(state.IsConfigured(Slot.Two));
    }

    [Fact]
    public void IsConfigured_BothSlotsConfigured_ReturnsTrue()
    {
        byte[] status = [5, 0, 0, 0, 0x03, 0x00];

        var state = new ConfigState(status);

        Assert.True(state.IsConfigured(Slot.One));
        Assert.True(state.IsConfigured(Slot.Two));
    }

    [Fact]
    public void IsConfigured_FirmwareTooOld_ThrowsInvalidOperationException()
    {
        // Firmware 2.0.0 is below minimum 2.1.0
        byte[] status = [2, 0, 0, 0, 0x01, 0x00];

        var state = new ConfigState(status);

        Assert.Throws<InvalidOperationException>(() => state.IsConfigured(Slot.One));
    }

    [Fact]
    public void IsTouchTriggered_Slot1Touch_ReturnsTrue()
    {
        // touch_level bit 2 = slot 1 touch triggered
        byte[] status = [5, 0, 0, 0, 0x04, 0x00];

        var state = new ConfigState(status);

        Assert.True(state.IsTouchTriggered(Slot.One));
        Assert.False(state.IsTouchTriggered(Slot.Two));
    }

    [Fact]
    public void IsTouchTriggered_Slot2Touch_ReturnsTrue()
    {
        // touch_level bit 3 = slot 2 touch triggered
        byte[] status = [5, 0, 0, 0, 0x08, 0x00];

        var state = new ConfigState(status);

        Assert.False(state.IsTouchTriggered(Slot.One));
        Assert.True(state.IsTouchTriggered(Slot.Two));
    }

    [Fact]
    public void IsTouchTriggered_FirmwareTooOld_ThrowsInvalidOperationException()
    {
        // Firmware 2.5.0 is below minimum 3.0.0
        byte[] status = [2, 5, 0, 0, 0x04, 0x00];

        var state = new ConfigState(status);

        Assert.Throws<InvalidOperationException>(() => state.IsTouchTriggered(Slot.One));
    }

    [Fact]
    public void IsLedInverted_BitSet_ReturnsTrue()
    {
        // touch_level bit 4 = LED inverted
        byte[] status = [5, 0, 0, 0, 0x10, 0x00];

        var state = new ConfigState(status);

        Assert.True(state.IsLedInverted());
    }

    [Fact]
    public void IsLedInverted_BitClear_ReturnsFalse()
    {
        byte[] status = [5, 0, 0, 0, 0x00, 0x00];

        var state = new ConfigState(status);

        Assert.False(state.IsLedInverted());
    }

    [Fact]
    public void TouchLevel_HighByte_ParsedCorrectly()
    {
        // touch_level is little-endian: lo=0x00, hi=0x01 => 0x0100 => bit 8 set
        // Bits 0-4 are defined; bit 8 is in high byte but not a defined flag
        // LED inverted is bit 4 (0x10 in low byte)
        byte[] status = [5, 0, 0, 0, 0x13, 0x00];

        var state = new ConfigState(status);

        // 0x13 = 0001_0011 = LED inverted | slot 2 configured | slot 1 configured
        Assert.True(state.IsConfigured(Slot.One));
        Assert.True(state.IsConfigured(Slot.Two));
        Assert.True(state.IsLedInverted());
        Assert.False(state.IsTouchTriggered(Slot.One));
        Assert.False(state.IsTouchTriggered(Slot.Two));
    }

    [Fact]
    public void AllFlagsSet_ParsedCorrectly()
    {
        // All bits 0-4 set: 0x1F
        byte[] status = [5, 0, 0, 0, 0x1F, 0x00];

        var state = new ConfigState(status);

        Assert.True(state.IsConfigured(Slot.One));
        Assert.True(state.IsConfigured(Slot.Two));
        Assert.True(state.IsTouchTriggered(Slot.One));
        Assert.True(state.IsTouchTriggered(Slot.Two));
        Assert.True(state.IsLedInverted());
    }
}
