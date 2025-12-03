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
using System.Security.Cryptography;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Otp.Commands;

namespace Yubico.YubiKey.Otp.Operations
{
    /// <summary>
    /// Configures a YubiKey's OTP slot to perform OTP using the Yubico OTP protocol.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Once configured, pressing the button on the YubiKey will cause it to emit the standard
    /// Yubico OTP challenge string.
    /// </para>
    /// <para>
    /// This class is not to be instantiated by non-SDK code. Instead, you will get a reference to an
    /// instance of this class by calling <see cref="OtpSession.ConfigureYubicoOtp(Slot)"/>.
    /// </para>
    /// <para>
    /// Once you have a reference to an instance, the member methods of this class can be used to chain
    /// together configurations using a builder pattern.
    /// </para>
    /// </remarks>
    public class ConfigureYubicoOtp : OperationBase<ConfigureYubicoOtp>
    {
        // There are no configuration flags for setting YubiOTP. Just the
        // normal defaults (allow update, serial visible to API), plus any
        // options the user selects are set. That means this operation has
        // an empty constructor.
        internal ConfigureYubicoOtp(IYubiKeyConnection connection, IOtpSession session, Slot slot)
            : base(connection, session, slot) { }

        #region Private Fields
        // Can be up to sixteen bytes. In ykman, this is "fixed".
        private ReadOnlyMemory<byte> _publicIdentifier = Array.Empty<byte>();
        // Six byte value. In ykman, this is "uid".
        private ReadOnlyMemory<byte> _privateIdentifier = Array.Empty<byte>();
        // Sixteen byte value. In ykman, this is "key".
        private ReadOnlyMemory<byte> _key = Array.Empty<byte>();
        // The following bool? fields serve two purposes. They determine whether
        // the condition is true, and whether the condition has been set. This
        // allows us to prevent conflicting options, and we can prevent callers
        // from attempting to retrieve a value that hasn't been set yet.
        private bool? _useSerialAsPublicId;
        private bool? _generatePrivateId;
        private bool? _generateKey;
        #endregion

        #region Size Constants
        /// <summary>
        /// The count of bytes that are prepended to the Yubico OTP challenge.
        /// </summary>
        public const int PublicIdentifierMaxLength = 16;
        /// <summary>
        /// The count of bytes used as the private identifier for the Yubico OTP credential.
        /// </summary>
        public const int PrivateIdentifierSize = 6;
        /// <summary>
        /// The key size of the Yubico OTP credential.
        /// </summary>
        public const int KeySize = 16;
        #endregion

        /// <inheritdoc/>
        protected override void ExecuteOperation()
        {
            var yubiKeyFlags = Settings.YubiKeyFlags;
            var cmd = new ConfigureSlotCommand
            {
                YubiKeyFlags = yubiKeyFlags,
                OtpSlot = OtpSlot!.Value
            };
            try
            {
                cmd.SetFixedData(_publicIdentifier.Span);
                cmd.SetUid(_privateIdentifier.Span);
                cmd.SetAesKey(_key.Span);
                cmd.ApplyCurrentAccessCode(CurrentAccessCode);
                cmd.SetAccessCode(NewAccessCode);

                var response = Connection.SendCommand(cmd);
                if (response.Status != ResponseStatus.Success)
                {
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiKeyOperationFailed,
                        response.StatusMessage));
                }
            }
            finally
            {
                cmd.Clear();
            }
        }

        /// <inheritdoc/>
        protected override void PreLaunchOperation()
        {
            // It's better to go ahead and find all of the problems rather than
            // "death of a thousand cuts."
            var exceptions = new List<Exception>();
            if (!_generatePrivateId.HasValue)
            {
                exceptions.Add(new InvalidOperationException(ExceptionMessages.MustChooseOrGeneratePrivateId));
            }

            if (!_useSerialAsPublicId.HasValue)
            {
                exceptions.Add(new InvalidOperationException(ExceptionMessages.MustChooseOrUseSerialAsPublicId));
            }

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
        /// Explicitly sets the public ID of the Yubico OTP credential.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The Yubico OTP online service requires the public ID to begin with
        /// 0xff (or "vv" in ModHex). If the credential will be uploaded, you must
        /// validate this or it will fail.</para>
        /// <para>Setting an explicit public ID is not compatible with using the YubiKey serial
        /// number as the public ID. Specifying both will result in an exception.
        /// </para>
        /// </remarks>
        /// <param name="publicId">A collection of bytes to use for the public ID.</param>
        /// <exception cref="ArgumentException">
        /// This is thrown when the byte collection is not the appropriate size.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// This is thrown when <see cref="UseSerialNumberAsPublicId"/> has been called before this.
        /// </exception>
        /// <exception cref="AggregateException">
        /// This is thrown when multiple exceptions have been encountered.
        /// </exception>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp UsePublicId(ReadOnlyMemory<byte> publicId)
        {
            // Check multiple things so the user doesn't have to keep checking.
            var exceptions = new List<Exception>();
            if (_useSerialAsPublicId ?? false)
            {
                exceptions.Add(
                    new InvalidOperationException(ExceptionMessages.CantSpecifyPublicIdAndUseSerial));
            }
            _useSerialAsPublicId = false;

            if (publicId.Length == 0 || publicId.Length > PublicIdentifierMaxLength)
            {
                exceptions.Add(
                    new ArgumentException(ExceptionMessages.PublicIdWrongSize));
            }

            if (exceptions.Count > 0)
            {
                throw exceptions.Count == 1
                    ? exceptions[0]
                    : new AggregateException(ExceptionMessages.MultipleExceptions, exceptions);
            }

            _publicIdentifier = publicId;
            return this;
        }

        /// <summary>
        /// Uses a binary representation of the YubiKey serial number as the public ID for this
        /// credential.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Using the YubiKey serial number is not compatible with setting an explicit byte collection
        /// as the public ID. Specifying both will result in an exception.
        /// </para>
        /// <para>
        /// If you do not need to receive the public ID that was generated from the serial
        /// number, you can either pass <see langword="null"/> or simply nothing.
        /// </para>
        /// </remarks>
        /// <param name="publicId">
        /// A <see cref="Memory{T}"/> object to receive the public ID. This object must be exactly
        /// six bytes.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// This will be thrown either if the caller called <see cref="UsePublicId(ReadOnlyMemory{byte})"/>
        /// before calling this method, or if the serial number is not readable on the YubiKey.
        /// </exception>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp UseSerialNumberAsPublicId(Memory<byte>? publicId = null)
        {
            var serialAsId = publicId ?? new byte[6];
            var exceptions = new List<Exception>();
            if (!(_useSerialAsPublicId ?? true))
            {
                exceptions.Add(
                    new InvalidOperationException(ExceptionMessages.CantSpecifyPublicIdAndUseSerial));
            }
            _useSerialAsPublicId = true;

            int? serialNumber = Session.YubiKey.SerialNumber;
            if (!serialNumber.HasValue)
            {
                exceptions.Add(
                    new InvalidOperationException(ExceptionMessages.KeyHasNoVisibleSerial));
            }

            if (serialAsId.Length != 6)
            {
                exceptions.Add(
                    new InvalidOperationException(ExceptionMessages.MustBeSixBytesForSerial));
            }

            if (exceptions.Count > 0)
            {
                throw exceptions.Count == 1
                    ? exceptions[0]
                    : new AggregateException(ExceptionMessages.MultipleExceptions, exceptions);
            }

            _publicIdentifier = serialAsId;

            // The ykman tool takes the device serial number, which is a three
            // byte integer, right-pads it with 0x00 to five bytes, and
            // prepends 0xff to it. They convert it to MODHEX so that the code
            // path is the same for when a public ID is supplied by the caller.
            // We will be taking a supplied public ID as a byte collection, so
            // we won't bother with the MODHEX step.
            // When ykman builds the byte collection, it deals with the serial
            // number as big-endian before converting it, so that's what we'll do.
            var pidSpan = serialAsId.Span;
            pidSpan[0] = 0xff;
            pidSpan[1] = 0x00;
            BinaryPrimitives.WriteInt32BigEndian(pidSpan[2..], serialNumber!.Value);

            return this;
        }

        /// <summary>
        /// Explicitly sets the private ID of the Yubico OTP credential.
        /// </summary>
        /// <remarks>
        /// Setting an explicit private ID is not compatible with generating a private ID. Specifying
        /// both will result in an exception.
        /// </remarks>
        /// <param name="privateId">A collection of bytes to use for the private ID.</param>
        /// <exception cref="ArgumentException">
        /// This is thrown when the byte collection is not the appropriate size.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// This is thrown when <see cref="GeneratePrivateId"/> has been called before this.
        /// </exception>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp UsePrivateId(ReadOnlyMemory<byte> privateId)
        {
            var exceptions = new List<Exception>();
            if (_generatePrivateId ?? false)
            {
                exceptions.Add(
                    new InvalidOperationException(ExceptionMessages.CantSpecifyPrivateIdAndGenerate));
            }
            _generatePrivateId = false;

            if (privateId.Length != PrivateIdentifierSize)
            {
                exceptions.Add(
                    new ArgumentException(ExceptionMessages.PrivateIdWrongSize, nameof(privateId)));
            }

            if (exceptions.Count > 0)
            {
                throw exceptions.Count == 1
                    ? exceptions[0]
                    : new AggregateException(ExceptionMessages.MultipleExceptions, exceptions);
            }
            _privateIdentifier = privateId;

            return this;
        }

        /// <summary>
        /// Generates a cryptographically random series of bytes as the private ID
        /// for the Yubico OTP credential.
        /// </summary>
        /// <remarks>
        /// Generating a private ID is not compatible with setting an explicit
        /// byte collection as the private ID. Specifying both will result in
        /// an exception.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// This will be thrown if the caller called <see cref="UsePrivateId(ReadOnlyMemory{byte})"/>
        /// before calling this method.
        /// </exception>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp GeneratePrivateId(Memory<byte> privateId)
        {
            if (!(_generatePrivateId ?? true))
            {
                throw new InvalidOperationException(ExceptionMessages.CantSpecifyPrivateIdAndGenerate);
            }
            _generatePrivateId = true;

            if (privateId.Length != PrivateIdentifierSize)
            {
                throw new ArgumentException(ExceptionMessages.PrivateIdWrongSize, nameof(privateId));
            }

            _privateIdentifier = privateId;

            using var rng = CryptographyProviders.RngCreator();
            rng.Fill(privateId.Span);

            return this;
        }

        /// <summary>
        /// Explicitly sets the key of the Yubico OTP credential.
        /// </summary>
        /// <remarks>
        /// Setting an explicit key is not compatible with generating a key. Specifying both will
        /// result in an exception.
        /// </remarks>
        /// <param name="key">A collection of bytes to use for the key.</param>
        /// <exception cref="InvalidOperationException">
        /// This is thrown when <see cref="GenerateKey"/> has been called before this.
        /// </exception>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp UseKey(Memory<byte> key)
        {
            if (_generateKey ?? false)
            {
                throw new InvalidOperationException(ExceptionMessages.CantSpecifyKeyAndGenerate);
            }
            _generateKey = false;

            if (key.Length != KeySize)
            {
                throw new ArgumentException(ExceptionMessages.YubicoKeyWrongSize, nameof(key));
            }
            _key = key;

            return this;
        }

        /// <summary>
        /// Generates a cryptographically random series of bytes as the key for the Yubico OTP credential.
        /// </summary>
        /// <remarks>
        /// Generating a key is not compatible with setting an explicit byte collection as the key.
        /// Specifying both will result in an exception.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// This will be thrown if the caller called <see cref="UseKey(Memory{byte})"/>
        /// before calling this method.
        /// </exception>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp GenerateKey(Memory<byte> key)
        {
            if (!(_generateKey ?? true))
            {
                throw new InvalidOperationException(ExceptionMessages.CantSpecifyKeyAndGenerate);
            }
            _generateKey = true;

            if (key.Length != KeySize)
            {
                throw new ArgumentException(ExceptionMessages.YubicoKeyWrongSize, nameof(key));
            }
            _key = key;

            using var rng = CryptographyProviders.RngCreator();
            rng.Fill(key.Span);
            return this;
        }

        #region Flags to Relay
        /// <inheritdoc cref="OtpSettings{T}.AppendCarriageReturn(bool)"/>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp AppendCarriageReturn(bool setConfig = true) =>
            Settings.AppendCarriageReturn(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.SendTabFirst(bool)"/>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp SendTabFirst(bool setConfig = true) =>
            Settings.SendTabFirst(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.AppendTabToFixed(bool)"/>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp AppendTabToFixed(bool setConfig) =>
            Settings.AppendTabToFixed(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.AppendDelayToFixed(bool)"/>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp AppendDelayToFixed(bool setConfig = true) =>
            Settings.AppendDelayToFixed(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.AppendDelayToOtp(bool)"/>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp AppendDelayToOtp(bool setConfig = true) =>
            Settings.AppendDelayToOtp(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.Use10msPacing(bool)"/>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp Use10msPacing(bool setConfig = true) =>
            Settings.Use10msPacing(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.Use20msPacing(bool)"/>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp Use20msPacing(bool setConfig = true) =>
            Settings.Use20msPacing(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.UseNumericKeypad(bool)"/>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp UseNumericKeypad(bool setConfig = true) =>
            Settings.UseNumericKeypad(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.UseFastTrigger(bool)"/>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp UseFastTrigger(bool setConfig = true) =>
            Settings.UseFastTrigger(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.AllowUpdate(bool)"/>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp SetAllowUpdate(bool setConfig = true) =>
            Settings.AllowUpdate(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.SendReferenceString(bool)"/>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp SendReferenceString(bool setConfig = true) =>
            Settings.SendReferenceString(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.SetSerialNumberApiVisible(bool)"/>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp SetSerialNumberApiVisible(bool setConfig = true) =>
            Settings.SetSerialNumberApiVisible(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.SetSerialNumberButtonVisible(bool)"/>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp SetSerialNumberButtonVisible(bool setConfig = true) =>
            Settings.SetSerialNumberButtonVisible(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.SetSerialNumberUsbVisible(bool)"/>
        /// <returns>The current <see cref="ConfigureYubicoOtp"/> instance.</returns>
        public ConfigureYubicoOtp SetSerialNumberUsbVisible(bool setConfig = true) =>
            Settings.SetSerialNumberUsbVisible(setConfig);
        #endregion
        #endregion
    }
}
