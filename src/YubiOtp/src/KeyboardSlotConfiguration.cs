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

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Abstract intermediate configuration for slot types that produce keyboard output
/// (Yubico OTP, HOTP, static password, static ticket). Adds shared CR, tab, delay,
/// pacing, and numeric keypad flag setters.
/// </summary>
public abstract class KeyboardSlotConfiguration : SlotConfiguration
{
    protected KeyboardSlotConfiguration()
    {
        _tktFlags |= TicketFlag.AppendCr;
        _extFlags |= ExtendedFlag.FastTrigger;
    }

    /// <summary>
    /// Appends a carriage return after the OTP output.
    /// </summary>
    public KeyboardSlotConfiguration AppendCr(bool enable = true)
    {
        SetTktFlag(TicketFlag.AppendCr, enable);
        return this;
    }

    /// <summary>
    /// Sends a tab character before the fixed part of the output.
    /// </summary>
    public KeyboardSlotConfiguration TabFirst(bool enable = true)
    {
        SetTktFlag(TicketFlag.TabFirst, enable);
        return this;
    }

    /// <summary>
    /// Sends a tab character between the fixed and dynamic parts.
    /// </summary>
    public KeyboardSlotConfiguration AppendTab1(bool enable = true)
    {
        SetTktFlag(TicketFlag.AppendTab1, enable);
        return this;
    }

    /// <summary>
    /// Sends a tab character after the OTP output.
    /// </summary>
    public KeyboardSlotConfiguration AppendTab2(bool enable = true)
    {
        SetTktFlag(TicketFlag.AppendTab2, enable);
        return this;
    }

    /// <summary>
    /// Adds a 500ms delay between the fixed and dynamic parts of the output.
    /// </summary>
    public KeyboardSlotConfiguration AppendDelay1(bool enable = true)
    {
        SetTktFlag(TicketFlag.AppendDelay1, enable);
        return this;
    }

    /// <summary>
    /// Adds a 500ms delay after the OTP output.
    /// </summary>
    public KeyboardSlotConfiguration AppendDelay2(bool enable = true)
    {
        SetTktFlag(TicketFlag.AppendDelay2, enable);
        return this;
    }

    /// <summary>
    /// Enables fast triggering (slot 2 only, reduced activation delay).
    /// </summary>
    public KeyboardSlotConfiguration FastTrigger(bool enable = true)
    {
        SetExtFlag(ExtendedFlag.FastTrigger, enable);
        return this;
    }

    /// <summary>
    /// Adds a 20ms delay between each keystroke for slow USB hosts.
    /// </summary>
    public KeyboardSlotConfiguration PacingChar10(bool enable = true)
    {
        SetCfgFlag(ConfigFlag.PacingChar10, enable);
        return this;
    }

    /// <summary>
    /// Adds a 40ms delay between each keystroke for very slow USB hosts.
    /// </summary>
    public KeyboardSlotConfiguration PacingChar20(bool enable = true)
    {
        SetCfgFlag(ConfigFlag.PacingChar20, enable);
        return this;
    }

    /// <summary>
    /// Uses numeric keypad scan codes instead of main keyboard.
    /// </summary>
    public KeyboardSlotConfiguration UseNumericKeypad(bool enable = true)
    {
        SetExtFlag(ExtendedFlag.UseNumericKeypad, enable);
        return this;
    }
}
