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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using Yubico.Core.Devices.Hid;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Otp.Commands;


namespace Yubico.YubiKey.Otp.Operations
{
    /// <summary>
    /// Operation class for configuring a YubiKey slot to send a static
    /// password, whether generated or specified.
    /// </summary>
    public class ConfigureStaticPassword : OperationBase<ConfigureStaticPassword>
    {
        internal ConfigureStaticPassword(IYubiKeyConnection connection, IOtpSession session, Slot slot)
            : base(connection, session, slot)
        {
            _ = Settings.UseStaticPasswordMode();
        }

        #region Private Fields
        // .NET design guidelines say that we are consumers of this Memory<T>
        // object for the duration of this instance. However, once we have HID
        // codes, we won't be using it. It's strictly a place to keep the chars
        // until we know what keyboard layout we have.
        private ReadOnlyMemory<char> _password = Memory<char>.Empty;
        // We're going to hold these in separate references so that the one
        // for a specified password can remain read-only.
        private Memory<char> _generatedPassword = Memory<char>.Empty;
        private byte[] _passwordHidCodes = Array.Empty<byte>();
        private KeyboardLayout? _keyboardLayout;
        private bool? _generatePassword;
        private static readonly byte[] _excluded = { 0x28, 0x2b, 0x2c };
        #endregion

        /// <inheritdoc/>
        protected override void ExecuteOperation()
        {
            try
            {
                // Pad and left justify the buffer.
                _passwordHidCodes = _passwordHidCodes
                    .Concat(new byte[SlotConfigureBase.MaxPasswordLength - _passwordHidCodes.Length])
                    .ToArray();

                var yubiKeyFlags = Settings.YubiKeyFlags;
                var cmd = new ConfigureSlotCommand
                {
                    YubiKeyFlags = yubiKeyFlags,
                    OtpSlot = OtpSlot!.Value
                };
                cmd.SetFixedData(_passwordHidCodes.AsSpan(0, SlotConfigureBase.FixedDataLength));
                cmd.SetUid(_passwordHidCodes.AsSpan(SlotConfigureBase.FixedDataLength, SlotConfigureBase.UidLength));
                cmd.SetAesKey(_passwordHidCodes.AsSpan(SlotConfigureBase.FixedDataLength + SlotConfigureBase.UidLength));
                cmd.ApplyCurrentAccessCode(CurrentAccessCode);
                cmd.SetAccessCode(NewAccessCode);

                try
                {
                    var response = Connection.SendCommand(cmd);
                    if (response.Status != ResponseStatus.Success)
                    {
                        throw new InvalidOperationException(
                            string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.YubiKeyOperationFailed,
                            response.StatusMessage
                            ));
                    }
                }
                finally
                {
                    cmd.Clear();
                }
            }
            finally
            {
                ClearHidBuffer();
            }
        }

        /// <inheritdoc/>
        protected override void PreLaunchOperation()
        {
            var exceptions = new List<Exception>();
            if (!_generatePassword.HasValue)
            {
                exceptions.Add(new InvalidOperationException(ExceptionMessages.MustSpecifyOrGeneratePassword));
            }
            if (!_keyboardLayout.HasValue)
            {
                exceptions.Add(new InvalidOperationException(ExceptionMessages.MustSpecifyKeyboardLayout));
            }

            if (exceptions.Count > 0)
            {
                throw exceptions.Count == 1
                    ? exceptions[0]
                    : new AggregateException(ExceptionMessages.MultipleExceptions, exceptions);
            }
        }

        /// <summary>
        /// The maximum length for a YubiKey static password.
        /// </summary>
        public const int MaxPasswordLength = SlotConfigureBase.MaxPasswordLength;

        /// <summary>
        /// The length of an access code, which is exactly six bytes.
        /// </summary>
        public const int AccessCodeLength = SlotConfigureBase.AccessCodeLength;

        #region Properties Specific to This Task
        /// <summary>
        /// Set the static password the slot on the YubiKey should be configured
        /// with.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This API can take explicit passwords set by this method, or it can
        /// generate a password. These are mutually exclusive options, so if you
        /// call both <see cref="GeneratePassword(Memory{char})"/> and this method,
        /// an exception will happen.
        /// </para>
        /// <para>
        /// Because this method needs to know which <see cref="KeyboardLayout"/>
        /// you're using before we can know if there are any invalid characters,
        /// this method will only check that if you have already specified the layout.
        /// </para>
        /// <para>
        /// If you specify the password before you specify the
        /// <see cref="KeyboardLayout"/>, the when you set the layout, that
        /// operation will check the characters and throw an
        /// <see cref="InvalidOperationException"/> if there are invalid characters.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// You cannot both generate and specify a static password.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if your password is too long or zero-length.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if your password has characters that are not available in
        /// your selected <see cref="KeyboardLayout"/>.
        /// </exception>
        /// <param name="password">The static password to configure the YubiKey with.</param>
        /// <returns>The <see cref="ConfigureStaticPassword"/> instance</returns>
        public ConfigureStaticPassword SetPassword(ReadOnlyMemory<char> password)
        {
            if (_generatePassword ?? false)
            {
                throw new InvalidOperationException(ExceptionMessages.BothGenerateAndSpecify);
            }
            _generatePassword = false;

            // This check just catches passwords that are too long, or zero-length.
            // We can't check for invalid characters until we populate the HID codes.
            if (password.Length > MaxPasswordLength
                || password.Length == 0)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.StaticPasswordInvalidLength,
                        MaxPasswordLength),
                        nameof(password));
            }
            _password = password;

            // See if we have all the parts needed to generate a password and/or
            // populate the HID codes.
            PopulateHidCodesIfReady();

            return this;
        }

        /// <summary>
        /// Instruct the API to generate a password for the YubiKey.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The generated password will be placed in <paramref name="generatedPassword"/>.
        /// The length of the generated password is directly controlled by the length
        /// of the buffer supplied. The length of the password must be between 1 and
        /// <see cref="MaxPasswordLength"/>.
        /// </para>
        /// <para>
        /// This API can generate passwords by calling this method, or it can use a
        /// specified password. These are mutually exclusive, so if you use both,
        /// an exception will occur.
        /// </para>
        /// </remarks>
        /// <param name="generatedPassword">Memory reference to contain the generated password.</param>
        /// <exception cref="InvalidOperationException">
        /// You cannot both generate and specify a static password.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The static password must be between 1 and 38 characters.
        /// </exception>
        /// <returns>The <see cref="ConfigureStaticPassword"/> instance</returns>
        public ConfigureStaticPassword GeneratePassword(Memory<char> generatedPassword)
        {
            if (!(_generatePassword ?? true))
            {
                throw new InvalidOperationException(ExceptionMessages.BothGenerateAndSpecify);
            }
            if (generatedPassword.Length < 1 || generatedPassword.Length > MaxPasswordLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.StaticPasswordInvalidLength,
                        MaxPasswordLength));
            }
            _generatePassword = true;

            _generatedPassword = generatedPassword;

            // See if we have all the parts needed to generate a password and/or
            // populate the HID codes.
            PopulateHidCodesIfReady();

            return this;
        }

        /// <summary>
        /// Set the <see cref="KeyboardLayout"/> to use.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The YubiKey itself does not understand the concept of a keyboard
        /// layout. It only sends HID codes to the USB port. The keyboard layout
        /// is used at the operating system level to translate between the HID
        /// codes and actual characters.
        /// </para>
        /// <para>
        /// For example, if you have an English, U.S. keyboard and press the
        /// <c>[Y]</c> button, an HID usage report with an ID of <c>0x1C</c>
        /// is generated by your keyboard. This is converted by your operating
        /// system to whatever internal scheme it uses, then to the letter "Y".
        /// </para>
        /// <para>
        /// However, if you program your key with a keyboard setting, but then
        /// someone uses the key on a system that has a German layout, the keyboard
        /// key that sends an HID usage ID of <c>0x1C</c> is the <c>[Z]</c> key.
        /// </para>
        /// <para>
        /// If you can be reasonably sure that your YubiKey will always be used
        /// on a system with the same keyboard layout, you can use this setting.
        /// However, Yubico's custom layout called ModHex is a reduced set that
        /// only includes mappings that are the same on most keyboard layouts.
        /// </para>
        /// </remarks>
        /// <param name="keyboard">The keyboard layout to use for the static password.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if your password has characters that are not available in
        /// your selected <see cref="KeyboardLayout"/>.
        /// </exception>
        /// <returns>The <see cref="ConfigureStaticPassword"/> instance</returns>
        public ConfigureStaticPassword WithKeyboard(KeyboardLayout keyboard)
        {
            _keyboardLayout = keyboard;

            // See if we have all the parts needed to generate a password and/or
            // populate the HID codes.
            PopulateHidCodesIfReady();

            return this;
        }

        #region Flags to Relay
        /// <inheritdoc cref="OtpSettings{T}.AppendCarriageReturn(bool)"/>
        public ConfigureStaticPassword AppendCarriageReturn(bool setConfig = true) =>
            Settings.AppendCarriageReturn(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.SendTabFirst(bool)"/>
        public ConfigureStaticPassword SendTabFirst(bool setConfig = true) =>
            Settings.SendTabFirst(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.AppendTabToFixed(bool)"/>
        public ConfigureStaticPassword AppendTabToFixed(bool setConfig) =>
            Settings.AppendTabToFixed(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.AppendDelayToFixed(bool)"/>
        public ConfigureStaticPassword AppendDelayToFixed(bool setConfig = true) =>
            Settings.AppendDelayToFixed(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.Use10msPacing(bool)"/>
        public ConfigureStaticPassword Use10msPacing(bool setConfig = true) =>
            Settings.Use10msPacing(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.Use20msPacing(bool)"/>
        public ConfigureStaticPassword Use20msPacing(bool setConfig = true) =>
            Settings.Use20msPacing(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.UseNumericKeypad(bool)"/>
        public ConfigureStaticPassword UseNumericKeypad(bool setConfig = true) =>
            Settings.UseNumericKeypad(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.UseFastTrigger(bool)"/>
        public ConfigureStaticPassword UseFastTrigger(bool setConfig = true) =>
            Settings.UseFastTrigger(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.AllowUpdate(bool)"/>
        public ConfigureStaticPassword SetAllowUpdate(bool setConfig = true) =>
            Settings.AllowUpdate(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.AllowManualUpdate(bool)"/>
        public ConfigureStaticPassword AllowManualUpdate(bool setConfig = true) =>
            Settings.AllowManualUpdate(setConfig);
        #endregion
        #endregion

        #region Utility Methods
        private void PopulateHidCodesIfReady()
        {
            try
            {
                // To populate the HID codes, we need:
                // 1. Not to have already done it.
                // 2. A keyboard layout.
                // 3. A password or a memory object to contain a generated password.
                if (_passwordHidCodes.Length == 0
                    && _keyboardLayout.HasValue
                    && _generatePassword.HasValue)
                {
                    var translator = HidCodeTranslator.GetInstance(_keyboardLayout!.Value);
                    var password = _password.Span;

                    if (_generatePassword.Value)
                    {
                        GenerateRandomPassword(translator);
                        password = _generatedPassword.Span;
                    }

                    // Here's where we populate the HID codes. Even though we're
                    // dealing with HID codes above, it's just for the purpose of
                    // making sure our generated password can be represented with
                    // the chosen keyboard layout.
                    _passwordHidCodes = new byte[password.Length];
                    for (int i = 0; i < password.Length; ++i)
                    {
                        _passwordHidCodes[i] = translator[password[i]];
                    }

                    // At this point, we don't need to reference the password data.
                    _password = _generatedPassword = Array.Empty<char>();
                }
            }
            catch (Exception)
            {
                ClearHidBuffer();
                throw;
            }


            // This local function just generates the password from the possible
            // values in the keyboard layout. Populating the HID codes isn't done
            // here.
            void GenerateRandomPassword(HidCodeTranslator translator)
            {
                var password = _generatedPassword.Span;
                using var rng = CryptographyProviders.RngCreator();
                // Build the table of possible random characters.
                byte[] hidTable = translator
                    .SupportedHidCodes
                    .Where(c => !_excluded.Contains(c))
                    .ToArray();

                // Generate the random characters.
                for (int i = 0; i < password.Length; ++i)
                {
                    byte hidCode = hidTable[rng.GetByte(0, (byte)hidTable.Length)];
                    password[i] = translator[hidCode];
                }
            }
        }

        private void ClearHidBuffer()
        {
            // If the buffer isn't null, and the length is greater than zero, clear it.
            if ((_passwordHidCodes?.Length ?? 0) > 0)
            {
                try
                {
                    CryptographicOperations.ZeroMemory(_passwordHidCodes);
                }
                finally
                {
                    // Just in case, make sure that at least the reference gets cleared.
                    _passwordHidCodes = Array.Empty<byte>();
                }
            }
        }
        #endregion
    }
}
