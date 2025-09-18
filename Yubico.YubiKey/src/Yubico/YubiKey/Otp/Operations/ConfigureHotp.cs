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
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Otp.Commands;

namespace Yubico.YubiKey.Otp.Operations;

/// <summary>
///     Configures a YubiKey OTP slot to emit sequence-based OTP codes.
/// </summary>
public class ConfigureHotp : OperationBase<ConfigureHotp>
{
    /// <summary>
    ///     The key size for an HMAC credential.
    /// </summary>
    public const int HmacKeySize = 20;

    internal ConfigureHotp(IYubiKeyConnection connection, IOtpSession session, Slot slot)
        : base(connection, session, slot)
    {
        _ = Settings.SetOathHotp();
    }

    /// <inheritdoc />
    protected override void ExecuteOperation()
    {
        var yubiKeyFlags = Settings.YubiKeyFlags;

        var cmd = new ConfigureSlotCommand
        {
            YubiKeyFlags = yubiKeyFlags,
            OtpSlot = OtpSlot!.Value
        };

        // The UID field is used differently for this operation. The first
        // four bytes are the last four of the key. The last two of the
        // UID are the upper 8 bits of the 12-bit OATH initial moving factor.
        // We'll handle validating input and shifting the bits in the setter,
        // so we're just dealing with a ushort here.

        // Because of the way the YubiKey needs things spliced up for this,
        // we are going to have to make a copy. We'll use stack memory so
        // that at least we aren't adding memory that can be moved around
        // by the garbage collector, and that we can absolutely know is clear
        // when we're done. Note the + 2 is the size of the initial moving
        // factor.
        Span<byte> hotpKey = stackalloc byte[HmacKeySize + 2];
        try
        {
            // Copy the key from user-supplied memory to stack memory.
            _key.Span.CopyTo(hotpKey[..HmacKeySize]);

            // Get a span that points to the bytes for the IMF.
            var imf = hotpKey[HmacKeySize..];

            // Write it in network order (big endian).
            BinaryPrimitives.WriteUInt16BigEndian(imf, _imf);

            // Split the key up.
            cmd.SetAesKey(hotpKey[..SlotConfigureBase.AesKeyLength]);
            cmd.SetUid(hotpKey[SlotConfigureBase.AesKeyLength..]);

            cmd.ApplyCurrentAccessCode(CurrentAccessCode);
            cmd.SetAccessCode(NewAccessCode);

            var response = Connection.SendCommand(cmd);
            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiKeyOperationFailed,
                        response.StatusMessage));
            }
        }
        finally
        {
            hotpKey.Clear();
        }
    }

    /// <inheritdoc />
    protected override void PreLaunchOperation()
    {
        // It's better to go ahead and find all of the problems rather than
        // "death of a thousand cuts."
        var exceptions = new List<Exception>();
        if (!_generateKey.HasValue)
        {
            exceptions.Add(new InvalidOperationException(ExceptionMessages.MustChooseOrGenerateKey));
        }

        if (exceptions.Count > 0)
        {
            throw exceptions.Count == 1
                ? exceptions[0]
                : new AggregateException(ExceptionMessages.MultipleExceptions, exceptions);
        }
    }

    #region Properties for Builder Pattern

    /// <summary>
    ///     Set the initial moving factor for the credential.
    /// </summary>
    /// <param name="imf">
    ///     Initial moving factor to set. Must be an integer between 0 and 0xffff0 (1,048,560) that is divisible
    ///     by 0x10 (16).
    /// </param>
    /// <returns>The current <see cref="ConfigureHotp" /> instance.</returns>
    public ConfigureHotp UseInitialMovingFactor(int imf)
    {
        if (imf < 0 || imf > 0xffff0 || (imf & 0xf) != 0)
        {
            throw new ArgumentException(ExceptionMessages.InvalidImfValue, nameof(imf));
        }

        _imf = (ushort)(imf >> 4);
        return this;
    }

    /// <summary>
    ///     Explicitly sets the key of the credential.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The <see cref="Memory{T}" /> collection containing the key is used by
    ///         the operation to program the YubiKey, but the source continues to be
    ///         owned by the caller. This means that the caller is responsible for
    ///         clearing the memory after use to avoid exposing sensitive information.
    ///     </para>
    ///     <para>
    ///         Setting an explicit key is not compatible with generating a key. Specifying both will
    ///         result in an exception.
    ///     </para>
    /// </remarks>
    /// <param name="key">A collection of bytes to use for the key.</param>
    /// <exception cref="InvalidOperationException">
    ///     This is thrown when <see cref="GenerateKey" /> has been called before this.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     This is thrown when <paramref name="key" /> is not the correct length.
    /// </exception>
    /// <returns>The current <see cref="ConfigureHotp" /> instance.</returns>
    public ConfigureHotp UseKey(ReadOnlyMemory<byte> key)
    {
        if (_generateKey ?? false)
        {
            throw new InvalidOperationException(ExceptionMessages.CantSpecifyKeyAndGenerate);
        }

        _generateKey = false;

        if (key.Length != HmacKeySize)
        {
            throw new ArgumentException(ExceptionMessages.HmacKeyWrongSize, nameof(key));
        }

        _key = key;

        return this;
    }

    /// <summary>
    ///     Generates a cryptographically random series of bytes as the key for the credential.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Generating a key is not compatible with setting an explicit byte collection as the key.
    ///         Specifying both will result in an exception.
    ///     </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///     This will be thrown if the caller called <see cref="UseKey(ReadOnlyMemory{byte})" />
    ///     before calling this method.
    /// </exception>
    /// <returns>The current <see cref="ConfigureHotp" /> instance.</returns>
    public ConfigureHotp GenerateKey(Memory<byte> key)
    {
        if (!(_generateKey ?? true))
        {
            throw new InvalidOperationException(ExceptionMessages.CantSpecifyKeyAndGenerate);
        }

        _generateKey = true;

        if (key.Length != HmacKeySize)
        {
            throw new ArgumentException(ExceptionMessages.HmacKeyWrongSize, nameof(key));
        }

        _key = key;

        using var rng = CryptographyProviders.RngCreator();
        rng.Fill(key.Span);
        return this;
    }

    #region Flags to Relay

    /// <inheritdoc cref="OtpSettings{T}.Use8DigitHotp(bool)" />
    /// <returns>The current <see cref="ConfigureHotp" /> instance.</returns>
    public ConfigureHotp Use8Digits(bool setConfig = true) => Settings.Use8DigitHotp(setConfig);

    /// <inheritdoc cref="OtpSettings{T}.AppendCarriageReturn(bool)" />
    /// <returns>The current <see cref="ConfigureHotp" /> instance.</returns>
    public ConfigureHotp AppendCarriageReturn(bool setConfig = true) => Settings.AppendCarriageReturn(setConfig);

    /// <inheritdoc cref="OtpSettings{T}.AllowUpdate(bool)" />
    /// <returns>The current <see cref="ConfigureHotp" /> instance.</returns>
    public ConfigureHotp SetAllowUpdate(bool setConfig = true) => Settings.AllowUpdate(setConfig);

    /// <inheritdoc cref="OtpSettings{T}.SendTabFirst(bool)" />
    /// <returns>The current <see cref="ConfigureHotp" /> instance.</returns>
    public ConfigureHotp SendTabFirst(bool setConfig = true) => Settings.SendTabFirst(setConfig);

    /// <inheritdoc cref="OtpSettings{T}.AppendTabToFixed(bool)" />
    /// <returns>The current <see cref="ConfigureHotp" /> instance.</returns>
    public ConfigureHotp AppendTabToFixed(bool setConfig) => Settings.AppendTabToFixed(setConfig);

    /// <inheritdoc cref="OtpSettings{T}.AppendDelayToFixed(bool)" />
    /// <returns>The current <see cref="ConfigureHotp" /> instance.</returns>
    public ConfigureHotp AppendDelayToFixed(bool setConfig = true) => Settings.AppendDelayToFixed(setConfig);

    /// <inheritdoc cref="OtpSettings{T}.AppendDelayToOtp(bool)" />
    /// <returns>The current <see cref="ConfigureHotp" /> instance.</returns>
    public ConfigureHotp AppendDelayToOtp(bool setConfig = true) => Settings.AppendDelayToOtp(setConfig);

    /// <inheritdoc cref="OtpSettings{T}.Use10msPacing(bool)" />
    /// <returns>The current <see cref="ConfigureHotp" /> instance.</returns>
    public ConfigureHotp Use10msPacing(bool setConfig = true) => Settings.Use10msPacing(setConfig);

    /// <inheritdoc cref="OtpSettings{T}.Use20msPacing(bool)" />
    /// <returns>The current <see cref="ConfigureHotp" /> instance.</returns>
    public ConfigureHotp Use20msPacing(bool setConfig = true) => Settings.Use20msPacing(setConfig);

    /// <inheritdoc cref="OtpSettings{T}.UseNumericKeypad(bool)" />
    /// <returns>The current <see cref="ConfigureHotp" /> instance.</returns>
    public ConfigureHotp UseNumericKeypad(bool setConfig = true) => Settings.UseNumericKeypad(setConfig);

    /// <inheritdoc cref="OtpSettings{T}.UseFastTrigger(bool)" />
    /// <returns>The current <see cref="ConfigureHotp" /> instance.</returns>
    public ConfigureHotp UseFastTrigger(bool setConfig = true) => Settings.UseFastTrigger(setConfig);

    /// <inheritdoc cref="OtpSettings{T}.SendReferenceString(bool)" />
    /// <returns>The current <see cref="ConfigureHotp" /> instance.</returns>
    public ConfigureHotp SendReferenceString(bool setConfig = true) => Settings.SendReferenceString(setConfig);

    #endregion

    #endregion

    #region Private Fields

    private ReadOnlyMemory<byte> _key = Memory<byte>.Empty;
    private ushort _imf;
    private bool? _generateKey;

    #endregion
}
