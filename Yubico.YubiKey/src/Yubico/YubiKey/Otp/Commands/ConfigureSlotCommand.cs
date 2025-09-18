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

using System;
using System.Globalization;

namespace Yubico.YubiKey.Otp.Commands;

/// <summary>
///     Applies a configuration to one of the two configurable OTP slots.
/// </summary>
public class ConfigureSlotCommand : SlotConfigureBase
{
    /// <inheritdoc />
    protected override byte ShortPressCode => OtpConstants.ConfigureShortPressSlot;

    /// <inheritdoc />
    protected override byte LongPressCode => OtpConstants.ConfigureLongPressSlot;

    /// <summary>
    ///     A fixed data field used to set any static configuration content.
    /// </summary>
    /// <exception cref="ArgumentException">
    ///     Thrown if the length of the contents of parameter <paramref name="fixedData" /> is greater
    ///     than <see cref="SlotConfigureBase.FixedDataLength" />.
    /// </exception>
    /// <remarks>For static passwords, this is also used as part of the password.</remarks>
    public void SetFixedData(ReadOnlySpan<byte> fixedData)
    {
        // We should not check for an exact length. There are operations
        // where only part of this buffer is used. The rest should be zeroed.
        // This is how the Python code does it.
        if (fixedData.Length > FixedDataLength)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPropertyLength,
                    nameof(fixedData),
                    FixedDataLength,
                    fixedData.Length));
        }

        var target = ConfigurationBuffer.Slice(FixedDataOffset, FixedDataLength);
        fixedData.CopyTo(target);

        // If the data is less than the buffer, make sure the rest is empty.
        if (fixedData.Length < FixedDataLength)
        {
            for (int i = fixedData.Length; i < FixedDataLength; ++i)
            {
                target[i] = 0;
            }
        }

        ConfigurationBuffer[FixedSizeOffset] = (byte)fixedData.Length;
    }

    /// <summary>
    ///     The user (or private) ID used by the OTP generator.
    /// </summary>
    /// <remarks>For static passwords, this is also used as part of the password.</remarks>
    public void SetUid(ReadOnlySpan<byte> uid)
    {
        if (uid.Length != UidLength)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPropertyLength,
                    nameof(uid),
                    UidLength,
                    uid.Length));
        }

        uid.CopyTo(ConfigurationBuffer.Slice(UidOffset, UidLength));
    }

    /// <summary>
    ///     The AES key with which the OTP is encrypted.
    /// </summary>
    /// <remarks>For static passwords, this is also used as part of the password.</remarks>
    public void SetAesKey(ReadOnlySpan<byte> aesKey)
    {
        if (aesKey.Length != AesKeyLength)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPropertyLength,
                    nameof(aesKey),
                    AesKeyLength,
                    aesKey.Length));
        }

        aesKey.CopyTo(ConfigurationBuffer.Slice(AesKeyOffset, AesKeyLength));
    }
}
