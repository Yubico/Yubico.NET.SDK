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
using Yubico.YubiKey.Otp.Commands;

namespace Yubico.YubiKey.Otp.Operations
{
    /// <summary>
    /// Configures a YubiKey's NDEF slot for text or URI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is not to be instantiated by non-SDK code. Instead, you will get a reference to an
    /// instance of this class by calling <see cref="OtpSession.ConfigureNdef"/>.
    /// </para>
    /// <para>
    /// Once you have a reference to an instance, the member methods of this class can be used to chain
    /// together configurations using a builder pattern.
    /// </para>
    /// </remarks>
    public class ConfigureNdef : OperationBase<ConfigureNdef>
    {
        private Uri? _uri;
        private string? _text;
        private bool _useUtf16;
        private string? _languageCode;

        internal ConfigureNdef(IYubiKeyConnection connection, IOtpSession session, Slot slot)
            : base(connection, session, slot) { }

        /// <inheritdoc/>
        protected override void PreLaunchOperation()
        {
            const string defaultLanguage = "en-US";

            if (_uri != null)
            {
                if (_useUtf16 || !string.IsNullOrEmpty(_languageCode))
                {
                    throw new InvalidOperationException(ExceptionMessages.OtpNdefPropertiesHaveNoEffect);
                }
            }
            else if (_text is null)
            {
                throw new InvalidOperationException(ExceptionMessages.OtpNdefNoTypeChosen);
            }

            _languageCode = string.IsNullOrEmpty(_languageCode) ? defaultLanguage : _languageCode;
        }

        /// <inheritdoc/>
        protected override void ExecuteOperation()
        {
            ReadOnlyMemory<byte> configBuffer = _text is null
                ? NdefConfig.CreateUriConfig(_uri!)
                : NdefConfig.CreateTextConfig(_text!, _languageCode!, _useUtf16);

            var response = Connection.SendCommand(new ConfigureNdefCommand(OtpSlot!.Value, configBuffer.Span));
            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.YubiKeyOperationFailed,
                    response.StatusMessage));
            }
        }

        /// <summary>
        /// Configures the NDEF slot to use a URI as the basis for the generated OTP.
        /// </summary>
        /// <param name="uri">
        /// The URI to program into the NDEF slot.
        /// </param>
        /// <returns>
        /// The current <see cref="ConfigureNdef"/> instance.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// This exception is thrown if the configuration builder has already been configured to use
        /// text using the <see cref="AsText"/> method.
        /// </exception>
        public ConfigureNdef AsUri(Uri? uri)
        {
            if (uri != null)
            {
                if (!string.IsNullOrEmpty(_text))
                {
                    throw new InvalidOperationException(ExceptionMessages.OtpNdefTypeConflict);
                }

                _uri = uri;
            }
            return this;
        }

        /// <summary>
        /// Configures the NDEF slot to use freeform text as the basis for the generated OTP.
        /// </summary>
        /// <param name="text">
        /// The text to program into the NDEF slot.
        /// </param>
        /// <returns>
        /// The current <see cref="ConfigureNdef"/> instance.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// This exception is thrown if the configuration build has already been configured to use a URI using the
        /// <see cref="AsUri"/> method.
        /// </exception>
        public ConfigureNdef AsText(string text)
        {
            if (text is { })
            {
                if (_uri != null)
                {
                    throw new InvalidOperationException(ExceptionMessages.OtpNdefTypeConflict);
                }

                _text = text;
            }
            return this;
        }

        /// <summary>
        /// Encode the NDEF text using the UTF-16LE instead of UTF-8.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This setting only has an effect when programming the NDEF slot using <see cref="AsText"/>. If this is set
        /// and the <see cref="AsUri"/> method is called, an exception will be thrown when calling <see cref="OperationBase{T}.Execute"/>.
        /// </para>
        /// <para>
        /// By default, the text that is configured into the NDEF slot is encoded using UTF-8. This is the standard that
        /// is overwhelmingly preferred for modern operating systems. Some platforms, such as Microsoft Windows, use
        /// UTF-16 little endian encoding internally. However, when interacting with other systems, they still interpret
        /// UTF-8 encoding with no loss of functionality or compatibility. The option to use UTF-16 encoding is provided
        /// to you, but you are strongly advised to consider whether this is the correct choice for your scenario. If you
        /// aren't certain of the choice, then the default (UTF-8) is the correct choice and you should not call this
        /// method.
        /// </para>
        /// </remarks>
        /// <returns>
        /// The current <see cref="ConfigureNdef"/> instance.
        /// </returns>
        public ConfigureNdef UseUtf16Encoding(bool setOption = true)
        {
            _useUtf16 = setOption;
            return this;
        }

        /// <summary>
        /// Add an ISO/IANA language code to the NDEF text configuration. Defaults to "en-US".
        /// </summary>
        /// <remarks>
        /// If you are using this in a builder pattern where you might call this method with
        /// a conditional statement, the negative condition should pass either null or an empty
        /// string. If you pass null or an empty string, and you called <see cref="AsText(string)"/>,
        /// then the default en-US will be applied.
        /// </remarks>
        /// <param name="languageCode">The Language Code Identifier (LCID) of the text.</param>
        /// <returns>
        /// The current <see cref="ConfigureNdef"/> instance.
        /// </returns>
        public ConfigureNdef WithLanguage(string languageCode)
        {
            _languageCode = languageCode;
            return this;
        }

        #region Flags to Relay
        /// <inheritdoc cref="OtpSettings{T}.SetSerialNumberApiVisible(bool)"/>
        /// <returns>The current <see cref="ConfigureNdef"/> instance.</returns>
        public ConfigureNdef SetSerialNumberApiVisible(bool setConfig = true) =>
            Settings.SetSerialNumberApiVisible(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.SetSerialNumberButtonVisible(bool)"/>
        /// <returns>The current <see cref="ConfigureNdef"/> instance.</returns>
        public ConfigureNdef SetSerialNumberButtonVisible(bool setConfig = true) =>
            Settings.SetSerialNumberButtonVisible(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.SetSerialNumberUsbVisible(bool)"/>
        /// <returns>The current <see cref="ConfigureNdef"/> instance.</returns>
        public ConfigureNdef SetSerialNumberUsbVisible(bool setConfig = true) =>
            Settings.SetSerialNumberUsbVisible(setConfig);
        #endregion
    }
}
