// Copyright 2021 Yubico AB
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
using System.Security.Cryptography;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Otp.Commands;

namespace Yubico.YubiKey.Otp.Operations
{
    /// <summary>
    ///     Configures a YubiKey's OTP slot to perform challenge-response using either
    ///     the Yubico OTP or the HMAC-SHA1 algorithm.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class is not to be instantiated by non-SDK code. Instead, you will get a reference to an
    ///         instance of this class by calling <see cref="OtpSession.ConfigureChallengeResponse(Slot)" />.
    ///     </para>
    ///     <para>
    ///         Once you have a reference to an instance, the member methods of this class can be used to chain
    ///         together configurations using a builder pattern.
    ///     </para>
    ///     <para>
    ///         Challenge-response mode needs to either have the
    ///         <see cref="OtpSettings{T}.UseHmacSha1ChallengeResponseMode(bool)" />
    ///         or the <see cref="OtpSettings{T}.UseYubicoOtpChallengeResponseMode(bool)" /> setting selected.
    ///     </para>
    /// </remarks>
    public class ConfigureChallengeResponse : OperationBase<ConfigureChallengeResponse>
    {
        // There are no configuration flags global to all challenge-response configs,
        // so we need only the normal defaults (allow update, serial visible to API),
        // plus any options the user selects. That means this operation has an empty
        // constructor.
        internal ConfigureChallengeResponse(IYubiKeyConnection connection, IOtpSession session, Slot slot)
            : base(connection, session, slot)
        {
        }

        /// <inheritdoc />
        protected override void ExecuteOperation()
        {
            YubiKeyFlags ykFlags = Settings.YubiKeyFlags;

            var cmd = new ConfigureSlotCommand
            {
                YubiKeyFlags = ykFlags,
                OtpSlot = OtpSlot!.Value
            };

            // If we're doing HMAC-SHA1, then we need two bytes at the end, and
            // some manipulation done to format the APDU for the YubiKey. To
            // accomodate this as safely as possible, we'll use a bit of stack
            // memory to copy the key to, and allow the two bytes at the end.
            // Stack memory can't be moved by the garbage collector, and we can
            // clean it up when we're done. We'll use the same method for both
            // algorithms so that code paths stay as converged as possible.
            int keyBufferSize =
                _useYubicoOtp!.Value
                    ? YubiOtpKeySize
                    : HmacSha1KeySize + 2;

            Span<byte> keyBuffer = stackalloc byte[keyBufferSize];
            try
            {
                _key.Span.CopyTo(keyBuffer);

                cmd.SetAesKey(keyBuffer[..SlotConfigureBase.AesKeyLength]);
                if (!_useYubicoOtp!.Value)
                {
                    cmd.SetUid(keyBuffer[SlotConfigureBase.AesKeyLength..]);
                }

                cmd.ApplyCurrentAccessCode(CurrentAccessCode);
                cmd.SetAccessCode(NewAccessCode);

                ReadStatusResponse response = Connection.SendCommand(cmd);
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
                keyBuffer.Clear();
            }
        }

        /// <inheritdoc />
        protected override void PreLaunchOperation()
        {
            // It's better to go ahead and find all of the problems rather than
            // "death of a thousand cuts."
            var exceptions = new List<Exception>();

            // Make sure an algorithm is chosen.
            if (!_useYubicoOtp.HasValue)
            {
                exceptions.Add(new InvalidOperationException(ExceptionMessages.MustChooseAlgorithm));
            }

            // The block above dealt with whether an algorithm had been chosen.
            // All that's left is making sure a key was either chosen or set to
            // be generated. The setters for those took care of making sure the
            // key buffer was the correct size.
            if (!_generateKey.HasValue)
            {
                exceptions.Add(
                    new InvalidOperationException(ExceptionMessages.MustChooseOrGenerateKey));
            }

            if (exceptions.Count > 0)
            {
                throw exceptions.Count == 1
                    ? exceptions[0]
                    : new AggregateException(ExceptionMessages.MultipleExceptions, exceptions);
            }
        }

        #region Private Fields

        // Key. 20 bytes for HMAC-SHA1, 16 bytes for Yubico OTP.
        private ReadOnlyMemory<byte> _key = Memory<byte>.Empty;
        private Memory<byte> _randomKey = Memory<byte>.Empty;
        private bool? _generateKey;
        private bool _validated;
        private bool? _useYubicoOtp;

        #endregion

        #region Size Constants

        /// <summary>
        ///     The key for a Yubico OTP operation is 16 bytes.
        /// </summary>
        public const int YubiOtpKeySize = 16;

        /// <summary>
        ///     The key for an HMAC-SHA1 operation is 20 bytes.
        /// </summary>
        public const int HmacSha1KeySize = 20;

        #endregion

        #region Properties for Builder Pattern

        /// <summary>
        ///     Configures the challenge-response to use the HMAC-SHA1 algorithm.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     You must choose either Yubico OTP or HMAC-SHA1, but not both.
        /// </exception>
        /// <returns>The current <see cref="ConfigureChallengeResponse" /> instance.</returns>
        public ConfigureChallengeResponse UseHmacSha1()
        {
            if (_useYubicoOtp ?? false)
            {
                throw new InvalidOperationException(ExceptionMessages.OnlyOneAlgorithm);
            }

            _useYubicoOtp = false;
            ProcessKey();

            return Settings.UseHmacSha1ChallengeResponseMode();
        }

        /// <summary>
        ///     Configures the challenge-response to use the Yubico OTP algorithm.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     You must choose either Yubico OTP or HMAC-SHA1, but not both.
        /// </exception>
        /// <returns>The current <see cref="ConfigureChallengeResponse" /> instance.</returns>
        public ConfigureChallengeResponse UseYubiOtp()
        {
            if (!(_useYubicoOtp ?? true))
            {
                throw new InvalidOperationException(ExceptionMessages.OnlyOneAlgorithm);
            }

            _useYubicoOtp = true;
            ProcessKey();

            return Settings.UseYubicoOtpChallengeResponseMode();
        }

        /// <summary>
        ///     Explicitly sets the key of the credential.
        /// </summary>
        /// <remarks>
        ///     Setting an explicit key is not compatible with generating a key. Specifying both will
        ///     result in an exception.
        /// </remarks>
        /// <param name="bytes">A collection of bytes to use for the key.</param>
        /// <exception cref="InvalidOperationException">
        ///     This is thrown when <see cref="GenerateKey" /> has been called before this.
        /// </exception>
        /// <returns>The current <see cref="ConfigureChallengeResponse" /> instance.</returns>
        public ConfigureChallengeResponse UseKey(ReadOnlyMemory<byte> bytes)
        {
            if (_generateKey ?? false)
            {
                throw new InvalidOperationException(ExceptionMessages.CantSpecifyKeyAndGenerate);
            }

            _generateKey = false;
            _key = bytes;
            ProcessKey();

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
        /// <returns>The current <see cref="ConfigureChallengeResponse" /> instance.</returns>
        public ConfigureChallengeResponse GenerateKey(Memory<byte> key)
        {
            if (!(_generateKey ?? true))
            {
                throw new InvalidOperationException(ExceptionMessages.CantSpecifyKeyAndGenerate);
            }

            _generateKey = true;
            _randomKey = key;
            ProcessKey();

            return this;
        }

        // This method is called from a few different places so that the key
        // length can be validated and, if selected, generated as soon as we
        // have enough information to know the expected length of the key and
        // whether the user selected to generate the key or supplied one.
        private void ProcessKey()
        {
            // If the algorithm and generate choices have been set, and if we
            // haven't already validated.
            if (_useYubicoOtp.HasValue
                && _generateKey.HasValue
                && !_validated)
            {
                // Validate key size.
                int keySize = _generateKey.Value
                    ? _randomKey.Length
                    : _key.Length;

                int expectedKeySize = _useYubicoOtp.Value
                    ? YubiOtpKeySize
                    : HmacSha1KeySize;

                if (keySize != expectedKeySize)
                {
                    throw new InvalidOperationException(
                        _useYubicoOtp.Value
                            ? ExceptionMessages.YubicoKeyWrongSize
                            : ExceptionMessages.HmacKeyWrongSize);
                }

                // Handle generating.
                if (_generateKey.Value)
                {
                    using RandomNumberGenerator rng = CryptographyProviders.RngCreator();
                    rng.Fill(_randomKey.Span);
                    _key = _randomKey;

                    // From here forward, we use _key, so we'll release this
                    // reference.
                    _randomKey = Memory<byte>.Empty;
                }

                _validated = true;
            }
        }

        #region Flags to Relay

        /// <summary>
        ///     Set when the HMAC challenge will be less than 64-bytes.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The traditional HMAC challenge is exactly 64-bytes. The YubiKey has a setting that
        ///         indicates a key of less than 64 bytes.
        ///     </para>
        ///     <para>
        ///         <b>Warning:</b> It's important to choose this setting correctly.
        ///         If you set this setting and submit a full 64-byte challenge to the YubiKey, then the
        ///         last byte will be truncated, resulting in a different response.
        ///     </para>
        ///     <para>
        ///         This setting is only valid if configuring for an HMAC challenge. If you set this for
        ///         a Yubico OTP challenge, an <see cref="InvalidOperationException" /> will be thrown when
        ///         you call <see cref="OperationBase{T}.Execute" />.
        ///     </para>
        /// </remarks>
        /// <returns>The current <see cref="ConfigureChallengeResponse" /> instance.</returns>
        public ConfigureChallengeResponse UseSmallChallenge(bool setConfig = true) =>
            Settings.HmacLessThan64Bytes(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.AllowUpdate(bool)" />
        /// <returns>The current <see cref="ConfigureChallengeResponse" /> instance.</returns>
        public ConfigureChallengeResponse SetAllowUpdate(bool setConfig = true) => Settings.AllowUpdate(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.UseButtonTrigger(bool)" />
        /// <returns>The current <see cref="ConfigureChallengeResponse" /> instance.</returns>
        public ConfigureChallengeResponse UseButton(bool setConfig = true) => Settings.UseButtonTrigger(setConfig);

        #endregion

        #endregion
    }
}
