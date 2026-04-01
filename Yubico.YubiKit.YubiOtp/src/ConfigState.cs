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

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Represents the configuration state of OTP slots on a YubiKey,
/// parsed from the status bytes returned by the OTP applet.
/// </summary>
/// <remarks>
/// Status bytes layout: [fw_major, fw_minor, fw_patch, prog_seq, touch_level_lo, touch_level_hi]
///
/// The touch_level field (bytes 4-5, little-endian) encodes CFGSTATE flags:
/// - Bit 0 (0x0001): Slot 1 is configured (requires firmware >= 2.1.0)
/// - Bit 1 (0x0002): Slot 2 is configured (requires firmware >= 2.1.0)
/// - Bit 2 (0x0004): Slot 1 requires touch (requires firmware >= 3.0.0)
/// - Bit 3 (0x0008): Slot 2 requires touch (requires firmware >= 3.0.0)
/// - Bit 4 (0x0010): LED behavior is inverted
/// </remarks>
public readonly struct ConfigState
{
    private const int ConfiguredSlot1 = 0x01;
    private const int ConfiguredSlot2 = 0x02;
    private const int TouchTriggeredSlot1 = 0x04;
    private const int TouchTriggeredSlot2 = 0x08;
    private const int LedInverted = 0x10;

    private static readonly FirmwareVersion MinVersionConfigured = new(2, 1, 0);
    private static readonly FirmwareVersion MinVersionTouchTriggered = new(3, 0, 0);

    /// <summary>
    /// The firmware version of the YubiKey.
    /// </summary>
    public FirmwareVersion FirmwareVersion { get; }

    /// <summary>
    /// The raw touch level flags from the status bytes.
    /// </summary>
    private int TouchLevel { get; }

    /// <summary>
    /// Creates a <see cref="ConfigState"/> from raw status bytes.
    /// </summary>
    /// <param name="statusBytes">The 6-byte status response from the OTP applet.</param>
    /// <exception cref="ArgumentException">Thrown when status bytes are fewer than 6 bytes.</exception>
    public ConfigState(ReadOnlySpan<byte> statusBytes)
    {
        if (statusBytes.Length < YubiOtpConstants.StatusBytesLength)
        {
            throw new ArgumentException(
                $"Status bytes must be at least {YubiOtpConstants.StatusBytesLength} bytes, got {statusBytes.Length}.",
                nameof(statusBytes));
        }

        FirmwareVersion = new FirmwareVersion(statusBytes[0], statusBytes[1], statusBytes[2]);
        TouchLevel = statusBytes[4] | (statusBytes[5] << 8);
    }

    /// <summary>
    /// Creates a <see cref="ConfigState"/> from a firmware version and touch level flags.
    /// </summary>
    internal ConfigState(FirmwareVersion firmwareVersion, int touchLevel)
    {
        FirmwareVersion = firmwareVersion;
        TouchLevel = touchLevel;
    }

    /// <summary>
    /// Gets whether the specified slot has been configured.
    /// Requires firmware version 2.1.0 or later.
    /// </summary>
    /// <param name="slot">The slot to check.</param>
    /// <returns><c>true</c> if the slot is configured; otherwise <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when firmware is too old to report this information.</exception>
    public bool IsConfigured(Slot slot)
    {
        if (FirmwareVersion < MinVersionConfigured)
        {
            throw new InvalidOperationException(
                $"Slot configuration state requires firmware {MinVersionConfigured} or later.");
        }

        return slot switch
        {
            Slot.One => (TouchLevel & ConfiguredSlot1) != 0,
            Slot.Two => (TouchLevel & ConfiguredSlot2) != 0,
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
        };
    }

    /// <summary>
    /// Gets whether the specified slot requires physical touch to trigger.
    /// Requires firmware version 3.0.0 or later.
    /// </summary>
    /// <param name="slot">The slot to check.</param>
    /// <returns><c>true</c> if the slot requires touch; otherwise <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when firmware is too old to report this information.</exception>
    public bool IsTouchTriggered(Slot slot)
    {
        if (FirmwareVersion < MinVersionTouchTriggered)
        {
            throw new InvalidOperationException(
                $"Touch trigger state requires firmware {MinVersionTouchTriggered} or later.");
        }

        return slot switch
        {
            Slot.One => (TouchLevel & TouchTriggeredSlot1) != 0,
            Slot.Two => (TouchLevel & TouchTriggeredSlot2) != 0,
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
        };
    }

    /// <summary>
    /// Gets whether the LED behavior is inverted.
    /// </summary>
    public bool IsLedInverted() => (TouchLevel & LedInverted) != 0;
}
