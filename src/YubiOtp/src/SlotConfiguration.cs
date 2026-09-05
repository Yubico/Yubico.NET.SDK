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

using System.Buffers.Binary;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;

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
    private readonly byte[] _fixed = new byte[YubiOtpConstants.FixedSize];
    private readonly byte[] _uid = new byte[YubiOtpConstants.UidSize];
    private readonly byte[] _key = new byte[YubiOtpConstants.KeySize];
    private byte _fixedSize;
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
    /// Alpha/beta firmware sentinels are treated as modern firmware (see <see cref="FirmwareVersion.IsAlphaOrBeta" />).
    /// </summary>
    public bool IsSupportedBy(FirmwareVersion version) =>
        version.IsAlphaOrBeta || version.IsAtLeast(MinimumFirmwareVersion);

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
            if (accCode.Length != YubiOtpConstants.AccessCodeSize)
            {
                throw new ArgumentException(
                    $"Access code must be exactly {YubiOtpConstants.AccessCodeSize} bytes, got {accCode.Length}.",
                    nameof(accCode));
            }

            accCode.CopyTo(config.AsSpan(38, YubiOtpConstants.AccessCodeSize));
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

    /// <summary>
    /// Replaces the fixed field and records its wire-format length.
    /// </summary>
    /// <param name="value">The fixed field value, up to 16 bytes.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> exceeds <see cref="YubiOtpConstants.FixedSize"/> bytes.
    /// </exception>
    protected void SetFixed(ReadOnlySpan<byte> value)
    {
        ThrowIfDisposed();

        if (value.Length > YubiOtpConstants.FixedSize)
        {
            throw new ArgumentException(
                $"Fixed data must be at most {YubiOtpConstants.FixedSize} bytes, got {value.Length}.",
                nameof(value));
        }

        CryptographicOperations.ZeroMemory(_fixed);
        value.CopyTo(_fixed);
        _fixedSize = (byte)value.Length;
    }

    /// <summary>
    /// Replaces the UID field.
    /// </summary>
    /// <param name="value">The exact 6-byte UID field value.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is not exactly <see cref="YubiOtpConstants.UidSize"/> bytes.
    /// </exception>
    protected void SetUid(ReadOnlySpan<byte> value)
    {
        ThrowIfDisposed();

        if (value.Length != YubiOtpConstants.UidSize)
        {
            throw new ArgumentException(
                $"UID data must be exactly {YubiOtpConstants.UidSize} bytes, got {value.Length}.",
                nameof(value));
        }

        value.CopyTo(_uid);
    }

    /// <summary>
    /// Replaces the key field.
    /// </summary>
    /// <param name="value">The exact 16-byte key field value.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is not exactly <see cref="YubiOtpConstants.KeySize"/> bytes.
    /// </exception>
    protected void SetKey(ReadOnlySpan<byte> value)
    {
        ThrowIfDisposed();

        if (value.Length != YubiOtpConstants.KeySize)
        {
            throw new ArgumentException(
                $"Key data must be exactly {YubiOtpConstants.KeySize} bytes, got {value.Length}.",
                nameof(value));
        }

        value.CopyTo(_key);
    }

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
    /// Validates and splits an exact-length HMAC key for wire format storage.
    /// The first 16 bytes are stored in the key field and the remaining 4 bytes in the UID field.
    /// </summary>
    /// <param name="hmacKey">
    /// The raw HMAC key. Must be exactly <see cref="YubiOtpConstants.HmacKeySize"/> (20) bytes.
    /// </param>
    /// <param name="initialMovingFactor">
    /// The HOTP initial moving factor divided by 0x10000, or zero for HMAC challenge-response.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="hmacKey"/> is not exactly
    /// <see cref="YubiOtpConstants.HmacKeySize"/> bytes. No device I/O is performed when this
    /// is thrown, and the key is never hashed or padded to fit.
    /// </exception>
    protected void SetHmacKey(ReadOnlySpan<byte> hmacKey, ushort initialMovingFactor)
    {
        ThrowIfDisposed();

        if (hmacKey.Length != YubiOtpConstants.HmacKeySize)
        {
            throw new ArgumentException(
                $"HMAC key must be exactly {YubiOtpConstants.HmacKeySize} bytes, got {hmacKey.Length}.",
                nameof(hmacKey));
        }

        Span<byte> uid = stackalloc byte[YubiOtpConstants.UidSize];
        uid.Clear();
        try
        {
            hmacKey[YubiOtpConstants.KeySize..].CopyTo(uid);
            BinaryPrimitives.WriteUInt16BigEndian(uid[4..], initialMovingFactor);

            SetKey(hmacKey[..YubiOtpConstants.KeySize]);
            SetUid(uid);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(uid);
        }
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