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

namespace Yubico.YubiKit.Core.Protocols.Otp.Hid;

/// <summary>
/// Constants for OTP HID protocol (8-byte feature reports).
/// Describes the Yubico slot/report layout used by this implementation.
/// </summary>
public static class OtpConstants
{
    /// <summary>
    /// Size of feature reports for OTP protocol.
    /// </summary>
    public const int FeatureReportSize = 8;

    /// <summary>
    /// Size of data in each feature report (excludes status/sequence byte).
    /// </summary>
    public const int FeatureReportDataSize = FeatureReportSize - 1;

    /// <summary>
    /// Size of slot data payload (64 bytes).
    /// </summary>
    public const int SlotDataSize = 64;

    /// <summary>
    /// Size of a complete frame: 64-byte payload + 1-byte slot + 2-byte CRC + 3-byte filler.
    /// </summary>
    public const int FrameSize = SlotDataSize + 6; // 70 bytes

    /// <summary>
    /// Response pending flag - bit 6 set indicates response data is available.
    /// </summary>
    public const byte ResponsePendingFlag = 0x40;

    /// <summary>
    /// Write flag - set by application, cleared by device when ready.
    /// </summary>
    public const byte SlotWriteFlag = 0x80;

    /// <summary>
    /// Timeout wait flag - waiting for touch, seconds left in lower 5 bits.
    /// </summary>
    public const byte ResponseTimeoutWaitFlag = 0x20;

    /// <summary>
    /// Dummy report write - forces device update or aborts current operation.
    /// </summary>
    public const byte DummyReportWrite = 0x8F;

    /// <summary>
    /// Mask for sequence number (lower 5 bits).
    /// </summary>
    public const byte SequenceMask = 0x1F;

    /// <summary>
    /// Offset of the programming sequence byte in the eight-byte status report.
    /// </summary>
    public const int StatusOffsetProgSeq = 4;

    /// <summary>
    /// Offset of touch/slot low bits in status.
    /// </summary>
    public const int StatusOffsetTouchLow = 5;

    /// <summary>
    /// Mask for checking if config slots are programmed.
    /// </summary>
    public const byte ConfigSlotsProgrammedMask = 0b00000011;

    /// <summary>
    /// Legacy assumed response-length offset. The current reader does not use this value;
    /// inbound data length is command-specific rather than a universal field at byte 64.
    /// </summary>
    public const int ResponseLengthOffset = SlotDataSize;

    /// <summary>
    /// Legacy assumed response-CRC offset. The current reader does not use this value;
    /// callers validate CRC at the command's expected data length.
    /// </summary>
    public const int ResponseCrcOffset = ResponseLengthOffset + 1;

    // OTP slot commands for Management
    /// <summary>
    /// Set device mode/config command.
    /// </summary>
    public const byte CmdDeviceConfig = 0x11;

    /// <summary>
    /// Read device capabilities/config command.
    /// </summary>
    public const byte CmdYk4Capabilities = 0x13;

    /// <summary>
    /// Write device info command.
    /// </summary>
    public const byte CmdYk4SetDeviceInfo = 0x15;
}