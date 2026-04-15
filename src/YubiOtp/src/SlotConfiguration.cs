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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Hid.Otp;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Abstract base class for OTP slot configurations. Provides common flag setters,
/// wire format assembly, and secure disposal of key material.
/// </summary>
/// <remarks>
/// The 52-byte wire format struct layout:
/// <code>
/// Offset  Size  Field
///   0      16   fixed (modhex public ID, scan codes, or zero-padded)
///  16       6   uid (private ID, HMAC key overflow, or HOTP IMF)
///  22      16   key (AES key, HMAC key prefix, or scan code overflow)
///  38       6   acc_code (access code, or zeros)
///  44       1   fixed_size
///  45       1   ext_flags
///  46       1   tkt_flags
///  47       1   cfg_flags
///  48       2   rfu (reserved, zero)
///  50       2   crc (CRC-16, complement of CalculateCrc)
/// </code>
/// </remarks>
public abstract class SlotConfiguration : IDisposable
{
    protected readonly byte[] _fixed = new byte[YubiOtpConstants.FixedSize];
    protected readonly byte[] _uid = new byte[YubiOtpConstants.UidSize];
    protected readonly byte[] _key = new byte[YubiOtpConstants.KeySize];
    protected byte _fixedSize;
    protected ExtendedFlag _extFlags = ExtendedFlag.SerialApiVisible | ExtendedFlag.AllowUpdate;
    protected TicketFlag _tktFlags;
    protected ConfigFlag _cfgFlags;
    private bool _disposed;

    /// <summary>
    /// Gets the minimum firmware version required for this configuration type.
    /// </summary>
    public virtual FirmwareVersion MinimumFirmwareVersion => new(2, 0, 0);

    /// <summary>
    /// Checks whether this configuration is supported by the given firmware version.
    /// Major version 0 is treated as a sentinel for alpha firmware (see ApplicationSession.IsSupported).
    /// </summary>
    public bool IsSupportedBy(FirmwareVersion version) =>
        version.Major == 0 || version.IsAtLeast(MinimumFirmwareVersion);

    /// <summary>
    /// Enables or disables the AllowUpdate flag, permitting future update operations on this slot.
    /// </summary>
    public SlotConfiguration AllowUpdate(bool enable = true)
    {
        SetExtFlag(ExtendedFlag.AllowUpdate, enable);
        return this;
    }

    /// <summary>
    /// Enables or disables dormant mode for the slot.
    /// </summary>
    public SlotConfiguration Dormant(bool enable = true)
    {
        SetExtFlag(ExtendedFlag.Dormant, enable);
        return this;
    }

    /// <summary>
    /// Enables or disables LED inversion.
    /// </summary>
    public SlotConfiguration InvertLed(bool enable = true)
    {
        SetExtFlag(ExtendedFlag.InvertLed, enable);
        return this;
    }

    /// <summary>
    /// Enables or disables serial number visibility via API calls.
    /// </summary>
    public SlotConfiguration SerialApiVisible(bool enable = true)
    {
        SetExtFlag(ExtendedFlag.SerialApiVisible, enable);
        return this;
    }

    /// <summary>
    /// Enables or disables serial number visibility via USB descriptor.
    /// </summary>
    public SlotConfiguration SerialUsbVisible(bool enable = true)
    {
        SetExtFlag(ExtendedFlag.SerialUsbVisible, enable);
        return this;
    }

    /// <summary>
    /// Enables or disables serial number visibility via button press.
    /// </summary>
    public SlotConfiguration SerialBtnVisible(bool enable = true)
    {
        SetExtFlag(ExtendedFlag.SerialBtnVisible, enable);
        return this;
    }

    /// <summary>
    /// Enables or disables slot 2 protection (requires slot 1 touch before slot 2 activates).
    /// </summary>
    public SlotConfiguration ProtectSlot2(bool enable = true)
    {
        SetTktFlag(TicketFlag.ProtectSlot2, enable);
        return this;
    }

    /// <summary>
    /// Assembles the 52-byte wire format configuration struct.
    /// </summary>
    /// <param name="accCode">Optional 6-byte access code. If empty, zeros are used.</param>
    /// <returns>A 52-byte array containing the complete configuration struct with CRC.</returns>
    public byte[] GetConfig(ReadOnlySpan<byte> accCode = default)
    {
        ThrowIfDisposed();

        var config = new byte[YubiOtpConstants.ConfigSize];

        _fixed.CopyTo(config.AsSpan(0, YubiOtpConstants.FixedSize));
        _uid.CopyTo(config.AsSpan(YubiOtpConstants.FixedSize, YubiOtpConstants.UidSize));
        _key.CopyTo(config.AsSpan(YubiOtpConstants.FixedSize + YubiOtpConstants.UidSize, YubiOtpConstants.KeySize));

        if (!accCode.IsEmpty)
        {
            int accLen = Math.Min(accCode.Length, YubiOtpConstants.AccessCodeSize);
            accCode[..accLen].CopyTo(config.AsSpan(38, accLen));
        }

        config[44] = _fixedSize;
        config[45] = (byte)GetEffectiveExtFlags();
        config[46] = (byte)GetEffectiveTktFlags();
        config[47] = (byte)GetEffectiveCfgFlags();

        // Bytes 48-49 are RFU (reserved), already zero

        ushort crc = (ushort)(~ChecksumUtils.CalculateCrc(config, 50) & 0xFFFF);
        config[50] = (byte)(crc & 0xFF);
        config[51] = (byte)((crc >> 8) & 0xFF);

        return config;
    }

    /// <summary>
    /// Gets the effective extended flags for wire format assembly.
    /// Override in subclasses to apply flag masks (e.g., update operations).
    /// </summary>
    protected virtual ExtendedFlag GetEffectiveExtFlags() => _extFlags;

    /// <summary>
    /// Gets the effective ticket flags for wire format assembly.
    /// </summary>
    protected virtual TicketFlag GetEffectiveTktFlags() => _tktFlags;

    /// <summary>
    /// Gets the effective configuration flags for wire format assembly.
    /// </summary>
    protected virtual ConfigFlag GetEffectiveCfgFlags() => _cfgFlags;

    protected void SetExtFlag(ExtendedFlag flag, bool enable)
    {
        if (enable)
        {
            _extFlags |= flag;
        }
        else
        {
            _extFlags &= ~flag;
        }
    }

    protected void SetTktFlag(TicketFlag flag, bool enable)
    {
        if (enable)
        {
            _tktFlags |= flag;
        }
        else
        {
            _tktFlags &= ~flag;
        }
    }

    protected void SetCfgFlag(ConfigFlag flag, bool enable)
    {
        if (enable)
        {
            _cfgFlags |= flag;
        }
        else
        {
            _cfgFlags &= ~flag;
        }
    }

    /// <summary>
    /// Processes an HMAC key for wire format storage: keys longer than 20 bytes are
    /// shortened via SHA-1; keys shorter are zero-padded. The result is split into
    /// <paramref name="key"/> (16 bytes) and <paramref name="uid"/> (4 bytes).
    /// </summary>
    /// <param name="hmacKey">The raw HMAC key (must not be empty).</param>
    /// <param name="key">Destination for the first 16 bytes of the processed key.</param>
    /// <param name="uid">Destination for bytes 16-19 of the processed key (written to first 4 bytes).</param>
    protected static void ProcessHmacKey(ReadOnlySpan<byte> hmacKey, Span<byte> key, Span<byte> uid)
    {
        Span<byte> processedKey = stackalloc byte[YubiOtpConstants.HmacKeySize];

        if (hmacKey.Length > YubiOtpConstants.HmacKeySize)
        {
            SHA1.HashData(hmacKey, processedKey);
        }
        else
        {
            hmacKey.CopyTo(processedKey);
            // Remaining bytes are already zero from stackalloc
        }

        // Split: first 16 bytes -> key, next 4 bytes -> uid[0..4]
        processedKey[..YubiOtpConstants.KeySize].CopyTo(key);
        processedKey[YubiOtpConstants.KeySize..].CopyTo(uid);

        CryptographicOperations.ZeroMemory(processedKey);
    }

    protected void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            CryptographicOperations.ZeroMemory(_fixed);
            CryptographicOperations.ZeroMemory(_uid);
            CryptographicOperations.ZeroMemory(_key);
        }

        _disposed = true;
    }
}
