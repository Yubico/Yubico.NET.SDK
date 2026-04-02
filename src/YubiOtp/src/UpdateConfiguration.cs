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
/// Configuration for updating an existing slot without reprogramming key material.
/// Only flags within the defined update masks are written to the device.
/// </summary>
/// <remarks>
/// Update operations allow modifying behavioral flags on a slot that was originally
/// programmed with <see cref="ExtendedFlag.AllowUpdate"/> enabled.
/// Only flags in <see cref="ExtendedFlagMasks.UpdateMask"/>,
/// <see cref="TicketFlagMasks.UpdateMask"/>, and <see cref="ConfigFlagMasks.UpdateMask"/>
/// are transmitted.
/// </remarks>
public sealed class UpdateConfiguration : SlotConfiguration
{
    public override FirmwareVersion MinimumFirmwareVersion => new(2, 3, 0);

    /// <summary>
    /// Appends a carriage return after the OTP output.
    /// </summary>
    public UpdateConfiguration AppendCr(bool enable = true)
    {
        SetTktFlag(TicketFlag.AppendCr, enable);
        return this;
    }

    /// <summary>
    /// Sends a tab character before the fixed part of the output.
    /// </summary>
    public UpdateConfiguration TabFirst(bool enable = true)
    {
        SetTktFlag(TicketFlag.TabFirst, enable);
        return this;
    }

    /// <summary>
    /// Sends a tab character between the fixed and dynamic parts.
    /// </summary>
    public UpdateConfiguration AppendTab1(bool enable = true)
    {
        SetTktFlag(TicketFlag.AppendTab1, enable);
        return this;
    }

    /// <summary>
    /// Sends a tab character after the OTP output.
    /// </summary>
    public UpdateConfiguration AppendTab2(bool enable = true)
    {
        SetTktFlag(TicketFlag.AppendTab2, enable);
        return this;
    }

    /// <summary>
    /// Adds a 500ms delay between the fixed and dynamic parts of the output.
    /// </summary>
    public UpdateConfiguration AppendDelay1(bool enable = true)
    {
        SetTktFlag(TicketFlag.AppendDelay1, enable);
        return this;
    }

    /// <summary>
    /// Adds a 500ms delay after the OTP output.
    /// </summary>
    public UpdateConfiguration AppendDelay2(bool enable = true)
    {
        SetTktFlag(TicketFlag.AppendDelay2, enable);
        return this;
    }

    /// <summary>
    /// Enables or disables dormant mode for the slot.
    /// </summary>
    public new UpdateConfiguration Dormant(bool enable = true)
    {
        SetExtFlag(ExtendedFlag.Dormant, enable);
        return this;
    }

    /// <summary>
    /// Enables fast triggering (slot 2 only, reduced activation delay).
    /// </summary>
    public UpdateConfiguration FastTrigger(bool enable = true)
    {
        SetExtFlag(ExtendedFlag.FastTrigger, enable);
        return this;
    }

    /// <summary>
    /// Uses numeric keypad scan codes instead of main keyboard.
    /// </summary>
    public UpdateConfiguration UseNumericKeypad(bool enable = true)
    {
        SetExtFlag(ExtendedFlag.UseNumericKeypad, enable);
        return this;
    }

    /// <summary>
    /// Adds a 20ms delay between each keystroke for slow USB hosts.
    /// </summary>
    public UpdateConfiguration PacingChar10(bool enable = true)
    {
        SetCfgFlag(ConfigFlag.PacingChar10, enable);
        return this;
    }

    /// <summary>
    /// Adds a 40ms delay between each keystroke for very slow USB hosts.
    /// </summary>
    public UpdateConfiguration PacingChar20(bool enable = true)
    {
        SetCfgFlag(ConfigFlag.PacingChar20, enable);
        return this;
    }

    protected override ExtendedFlag GetEffectiveExtFlags() =>
        _extFlags & ExtendedFlagMasks.UpdateMask;

    protected override TicketFlag GetEffectiveTktFlags() =>
        _tktFlags & TicketFlagMasks.UpdateMask;

    protected override ConfigFlag GetEffectiveCfgFlags() =>
        _cfgFlags & ConfigFlagMasks.UpdateMask;
}
