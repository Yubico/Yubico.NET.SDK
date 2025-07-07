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
using Yubico.Core.Logging;
using Yubico.YubiKey.Oath;
using Yubico.YubiKey.Otp.Commands;
using Yubico.YubiKey.Otp.Operations;
using Yubico.YubiKey.Scp;

namespace Yubico.YubiKey.Otp
{
    /// <summary>
    /// Entry point for all high-level OTP operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Most methods will return a reference to the operation class that performs
    /// the operation being executed. Then, methods on that class that configure
    /// the operation also return the same reference. This is known as a fluent
    /// builder pattern. This allows operations to be built by stringing together
    /// configuration methods. The operation is usually concluded with a call to a
    /// method named <c>Execute()</c>.
    /// </para>
    /// <para>
    /// Methods in this class that instantiate classes that support the fluent
    /// builder pattern are any that return a class reference. Methods that return
    /// void do not require additional configuration to perform their operations.
    /// See the example section below for more details.
    /// </para>
    /// </remarks>
    /// <example>
    /// This is an example of using the fluent builder pattern to configure a
    /// YubiKey OTP slot to emit a static password.
    /// <code language="csharp">
    /// ReadOnlyMemory&lt;char&gt; password = "Shhhh!Don'tTell!".ToCharArray();
    /// using (OtpSession otp = new OtpSession(yubiKey))
    /// {
    ///     otp.ConfigureStaticPassword(Slot.ShortPress)
    ///         .SetPassword(password)
    ///         .WithKeyboard(KeyboardLayout.en_US)
    ///         .AppendCarriageReturn()
    ///         .Execute();
    /// };
    /// </code>
    /// The method, <see cref="ConfigureStaticPassword(Slot)"/>, instantiates an
    /// instance of the class <see cref="Operations.ConfigureStaticPassword"/>.
    /// The next line configures the operation with the password to set. The next
    /// line configures the keyboard layout. The next line configures the operation
    /// to tell the YubiKey to send a carriage-return after sending the password,
    /// and finally, <c>Execute()</c> tells the operation class to perform the
    /// configuration on the YubiKey.
    /// </example>
    public sealed class OtpSession : ApplicationSession, IOtpSession
    {
        /// <summary>
        /// Constructs a <see cref="OtpSession"/> instance for high-level OTP operations.
        /// </summary>
        /// <remarks>
        /// This constructor should be used to obtain an instance of this class for
        /// performing operations on the YubiKey OTP application. The instance of
        /// <see cref="IYubiKeyDevice"/> passed in should be a connected YubiKey.
        /// </remarks>
        /// <param name="yubiKey">An instance of a class that implements <see cref="IYubiKeyDevice"/>.</param>
        /// <param name="keyParameters">An instance of <see cref="Scp03KeyParameters"/> containing the
        /// parameters for the SCP03 key. If <see langword="null"/>, the default parameters will be used. </param>
        public OtpSession(IYubiKeyDevice yubiKey, ScpKeyParameters? keyParameters = null)
            : base(Log.GetLogger<OtpSession>(), yubiKey, YubiKeyApplication.Otp, keyParameters)
        {
            // Getting the OTP status allows the user to read the OTP status on the OtpSession object.
            _otpStatus = Connection.SendCommand(new ReadStatusCommand()).GetData();
        }

        #region OTP Operation Object Factory
        /// <summary>
        /// Submits a challenge to the YubiKey OTP application to be calculated.
        /// </summary>
        /// <param name="slot">The identifier for the OTP application slot to configure.</param>
        /// <returns>An instance of <see cref="Operations.CalculateChallengeResponse"/>.</returns>
        public CalculateChallengeResponse CalculateChallengeResponse(Slot slot) =>
            new CalculateChallengeResponse(Connection, this, slot);

        /// <summary>
        /// Configures one of the OTP application slots to act as a Yubico OTP device.
        /// </summary>
        /// <param name="slot">The identifier for the OTP application slot to configure.</param>
        /// <returns>Instance of <see cref="Operations.ConfigureYubicoOtp"/>.</returns>
        public ConfigureYubicoOtp ConfigureYubicoOtp(Slot slot) =>
            new ConfigureYubicoOtp(Connection, this, slot);

        /// <summary>
        /// Removes a slot configuration in the YubiKey's OTP application.
        /// </summary>
        /// <param name="slot">The identifier for the OTP application slot configuration to delete.</param>
        /// <returns>An instance of <see cref="Operations.DeleteSlotConfiguration"/>.</returns>
        public DeleteSlotConfiguration DeleteSlotConfiguration(Slot slot) =>
            new DeleteSlotConfiguration(Connection, this, slot);

        /// <summary>
        /// Configures one of the OTP application slots to respond to challenges.
        /// </summary>
        /// <param name="slot">The identifier for the OTP application slot to configure.</param>
        /// <returns>Instance of <see cref="Operations.ConfigureChallengeResponse"/>.</returns>
        public ConfigureChallengeResponse ConfigureChallengeResponse(Slot slot) =>
            new ConfigureChallengeResponse(Connection, this, slot);

        /// <inheritdoc cref="Operations.ConfigureHotp"/>
        /// <param name="slot">OTP Slot to configure.</param>
        public ConfigureHotp ConfigureHotp(Slot slot) =>
            new ConfigureHotp(Connection, this, slot);

        /// <inheritdoc cref="Operations.ConfigureNdef"/>
        /// <param name="slot">OTP Slot to configure.</param>
        public ConfigureNdef ConfigureNdef(Slot slot) =>
            new ConfigureNdef(Connection, this, slot);

        /// <summary>
        /// Sets a static password for an OTP application slot on a YubiKey.
        /// </summary>
        /// <remarks>
        /// This method returns a <see cref="Operations.ConfigureStaticPassword"/> instance.
        /// This instance exposes methods to set the parameters for the static
        /// password you intend to set. Each of those parameters returns a reference
        /// to the <see cref="Operations.ConfigureStaticPassword"/> instance so they can be
        /// chained together. Once all the parameters are set, the last call should
        /// be to the <see cref="OperationBase{T}.Execute"/> method.
        /// </remarks>
        /// <param name="slot">The identifier for the OTP application slot to configure.</param>
        /// <returns>Instance of <see cref="Operations.ConfigureStaticPassword"/>.</returns>
        public ConfigureStaticPassword ConfigureStaticPassword(Slot slot) =>
            new ConfigureStaticPassword(Connection, this, slot);

        /// <summary>
        /// Updates the settings of an OTP application slot on a YubiKey without removing
        /// the existing configuration.
        /// </summary>
        /// <param name="slot">The identifier for the OTP application slot to configure.</param>
        /// <inheritdoc cref="OtpSettings{T}.AllowUpdate(bool)" path="/remarks"/>
        public UpdateSlot UpdateSlot(Slot slot) =>
            new UpdateSlot(Connection, this, slot);
        #endregion

        #region Non-Builder Implementations
        /// <summary>
        /// Removes an OTP slot configuration and sets it to empty.
        /// </summary>
        /// <remarks>
        /// Use this method if there is not access code set on the slot. If you need to
        /// specify an access code, use the builder version (<see cref="DeleteSlotConfiguration(Slot)"/>),
        /// which exposes <see cref="OperationBase{T}.UseCurrentAccessCode(SlotAccessCode)"/>.
        /// </remarks>
        /// <param name="slot">The <see cref="Slot"/> to reset to empty.</param>
        public void DeleteSlot(Slot slot) =>
            DeleteSlotConfiguration(slot)
            .Execute();

        /// <summary>
        /// Swaps the configurations in the short and long press slots.
        /// </summary>
        /// <remarks>
        /// If either of the two slots is protected with an access code, this command will fail.
        /// In order to swap slot configurations, you will need to remove the access codes in a separate
        /// operation. After the swap, you can reapply the access codes, also in a separate operation.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The <c>SwapSlotsCommand</c> failed or is not supported on this YubiKey.
        /// </exception>
        public void SwapSlots()
        {
            if (_otpStatus.FirmwareVersion < FirmwareVersion.V2_3_2)
            {
                throw new InvalidOperationException(ExceptionMessages.OtpSwapCommandNotSupported);
            }

            if (!IsShortPressConfigured && !IsLongPressConfigured)
            {
                throw new InvalidOperationException(ExceptionMessages.OtpSlotsNotConfigured);
            }

            var swapResponse = Connection.SendCommand(new SwapSlotsCommand());
            if (swapResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(swapResponse.StatusMessage);
            }
        }

        /// <summary>
        /// Reads the OTP programmed in the short-press slot using the NFC Data-Exchange Format (NDEF) tag from NFC
        /// enabled YubiKeys. (Requires the YubiKey be connected via NFC).
        /// </summary>
        /// <remarks>
        /// <para>
        /// YubiKeys like the NEO and the 5-NFC series supports Near-Field Communication (NFC). NFC is a method in which
        /// the YubiKey can draw power from and communicate with another device over very short distances. This device
        /// can be a mobile phone, or a dedicated NFC smart card reader. The distances in which the YubiKey can operate
        /// depend on the signal strength provided by the reader, but is typically 1-3 centimeters.
        /// </para>
        /// <para>
        /// For most YubiKey operations, the behavior will be the same over NFC as would be seen over USB. One significant
        /// difference, however, is reading content out of the "touch-enabled" configuration slots that the OTP application
        /// is built around. Devices like a smart phone will often automatically read the contents out of an NDEF tag. This
        /// allows the YubiKey to emulate the touch experience over NFC, at least when smart phones are involved.
        /// </para>
        /// <para>
        /// Typically, desktop NFC readers do not automatically read NDEF tag contents. Therefore, a
        /// mechanism to programmatically read NDEF tags is required to achieve full feature parity
        /// with USB and NFC enabled smart phones. This method allows you to read the contents of an
        /// NDEF tag. Since the NDEF "slot" can contain either a URI or a text blob, an
        /// <see cref="NdefDataReader"/> instance is returned. This class allows you to read the NDEF
        /// data and interpret in a form that is the most appropriate for your application's needs.
        /// </para>
        /// <para>
        /// Note: This method modifies the underlying connection to the YubiKey that the session uses. If an exception
        /// is thrown from this method, it is not guaranteed that the session will still be connected to the YubiKey. In
        /// that case, a new session will need to be established.
        /// </para>
        /// </remarks>
        /// <returns>
        /// An <see cref="NdefDataReader"/> instance that can interpret the NDEF data in the form that best suits your
        /// needs.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when this method is called on a YubiKey that is not connected via NFC.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the YubiKey could not select the NDEF file ID.
        /// </exception>
        public NdefDataReader ReadNdefTag()
        {
            // If this is a YubiKey device type that we know about, check for NFC capabilities. If we don't know about
            // the device type, allow the call to go through assuming the other implementation will fail as appropriate.
            if (YubiKey is YubiKeyDevice { IsNfcDevice: false })
            {
                throw new NotSupportedException(ExceptionMessages.RequiresNfc);
            }

            // NDEF is actually a separate application that we need to connect to. Disconnect from OTP, run the NDEF
            // command, and then reconnect to OTP.
            Connection.Dispose();

            ReadNdefDataResponse response;
            using (Connection = YubiKey.Connect(YubiKeyApplication.OtpNdef))
            {
                var selectResponse = Connection.SendCommand(new SelectNdefDataCommand() { FileID = NdefFileId.Ndef });
                if (selectResponse.Status != ResponseStatus.Success)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.OtpNdefSelectFileFailed,
                            selectResponse.StatusMessage));
                }

                response = Connection.SendCommand(new ReadNdefDataCommand());
            }

            Connection = GetConnection(YubiKey, YubiKeyApplication.Otp, KeyParameters);
            return new NdefDataReader(response.GetData().Span);
        }

        #endregion

        #region Properties
        /// <inheritdoc cref="OtpStatus.ShortPressConfigured"/>
        public bool IsShortPressConfigured => _otpStatus.ShortPressConfigured;

        /// <inheritdoc cref="OtpStatus.ShortPressRequiresTouch"/>
        public bool ShortPressRequiresTouch => _otpStatus.ShortPressRequiresTouch;

        /// <inheritdoc cref="OtpStatus.LongPressConfigured"/>
        public bool IsLongPressConfigured => _otpStatus.LongPressConfigured;

        /// <inheritdoc cref="OtpStatus.LongPressRequiresTouch"/>
        public bool LongPressRequiresTouch => _otpStatus.LongPressRequiresTouch;

        internal FirmwareVersion FirmwareVersion => _otpStatus.FirmwareVersion;

        FirmwareVersion IOtpSession.FirmwareVersion => FirmwareVersion;

        IYubiKeyDevice IOtpSession.YubiKey => YubiKey;
        #endregion

        #region Private Fields
        private readonly OtpStatus _otpStatus;
        #endregion
    }
}
