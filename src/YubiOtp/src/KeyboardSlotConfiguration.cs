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

    /// <summary>
    /// Validates and copies Yubico OTP / static ticket key material into the wire format fields.
    /// </summary>
    /// <param name="publicId">The public identity prefix (up to 16 bytes).</param>
    /// <param name="privateId">The 6-byte private identity.</param>
    /// <param name="aesKey">The 16-byte AES-128 secret key.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="publicId"/> exceeds 16 bytes,
    /// <paramref name="privateId"/> is not 6 bytes, or
    /// <paramref name="aesKey"/> is not 16 bytes.
    /// </exception>
    protected void InitializeKeys(
        ReadOnlySpan<byte> publicId,
        ReadOnlySpan<byte> privateId,
        ReadOnlySpan<byte> aesKey)
    {
        if (publicId.Length > YubiOtpConstants.FixedSize)
        {
            throw new ArgumentException(
                $"Public ID must be at most {YubiOtpConstants.FixedSize} bytes.",
                nameof(publicId));
        }

        if (privateId.Length != YubiOtpConstants.UidSize)
        {
            throw new ArgumentException(
                $"Private ID must be exactly {YubiOtpConstants.UidSize} bytes.",
                nameof(privateId));
        }

        if (aesKey.Length != YubiOtpConstants.KeySize)
        {
            throw new ArgumentException(
                $"AES key must be exactly {YubiOtpConstants.KeySize} bytes.",
                nameof(aesKey));
        }

        publicId.CopyTo(_fixed);
        privateId.CopyTo(_uid);
        aesKey.CopyTo(_key);
        _fixedSize = (byte)publicId.Length;
    }
}
