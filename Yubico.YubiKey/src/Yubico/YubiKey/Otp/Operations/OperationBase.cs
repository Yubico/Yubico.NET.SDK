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
using Yubico.YubiKey.Otp.Commands;

namespace Yubico.YubiKey.Otp.Operations
{
    /// <summary>
    ///     Base class for OTP configuration operations.
    /// </summary>
    /// <typeparam name="T">The child class type.</typeparam>
    /// <remarks>
    ///     The reference to the child type allows builder methods and properties to return
    ///     a reference to the calling class to allow chaining.
    /// </remarks>
    public abstract class OperationBase<T> where T : OperationBase<T>
    {
        private Memory<byte> _currentAccessCode = new byte[SlotConfigureBase.AccessCodeLength];

        private Memory<byte> _newAccessCode = new byte[SlotConfigureBase.AccessCodeLength];

        /// <summary>
        ///     Constructs as <see cref="OperationBase{T}" /> instance.
        /// </summary>
        /// <param name="yubiKey">The connection to the YubiKey session.</param>
        /// <param name="session">Reference to <see cref="IOtpSession" /> instance.</param>
        /// <param name="slot">
        ///     Optional parameter specifying the slot to configure.
        ///     <b>Important:</b> Inheriting classes that configure an OTP slot must supply this.
        /// </param>
        /// <remarks>
        ///     <para>
        ///         This generic abstract class is meant to be inherited from by a class that specifies
        ///         itself as the templated type. This is so that this base class can allow the child
        ///         class to call methods to configure settings in <see cref="OtpSettings{T}" /> and get
        ///         back a reference to the calling class. This allows us to use method-chaining to
        ///         implement the fluent builder pattern.
        ///     </para>
        ///     <para>
        ///         Most, but not all, classes that inherit from <see cref="OperationBase{T}" /> will
        ///         be configuring an OTP slot. Those classes must call this constructor with a valid
        ///         value. Classes that do not configure a slot should either not specify this parameter,
        ///         or call it with <see langword="null" />.
        ///     </para>
        /// </remarks>
        protected OperationBase(IYubiKeyConnection yubiKey, IOtpSession session, Slot? slot = null)
        {
            OtpSlot = slot;
            Session = session;
            Settings = new OtpSettings<T>((T)this);
            Connection = yubiKey;
            Version = Session.FirmwareVersion;
            _ = Settings.SetSerialNumberApiVisible();
            _ = Settings.AllowUpdate();
        }

        /// <summary>
        ///     The six-byte access code currently set to protect the OTP slot.
        /// </summary>
        protected Span<byte> CurrentAccessCode
        {
            get => _currentAccessCode.Span;
            set => _currentAccessCode = value.ToArray();
        }

        /// <summary>
        ///     The six-byte access code to set for the OTP slot after applying the configuration.
        /// </summary>
        protected Span<byte> NewAccessCode
        {
            get => _newAccessCode.Span;
            set => _newAccessCode = value.ToArray();
        }

        /// <summary>
        ///     The OTP <see cref="Slot" /> to configure.
        /// </summary>
        protected Slot? OtpSlot { get; set; }

        /// <summary>
        ///     A reference to the <see cref="IOtpSession" /> object that created the operation.
        /// </summary>
        protected IOtpSession Session { get; set; }

        internal OtpSettings<T> Settings { get; }

        /// <summary>
        ///     The firmware version on the YubiKey this task is associated with.
        /// </summary>
        public FirmwareVersion Version { get; private set; } = new FirmwareVersion();

        /// <summary>
        ///     Reference to the <see cref="IYubiKeyConnection" /> for the YubiKey being configured.
        /// </summary>
        protected IYubiKeyConnection Connection { get; }

        /// <summary>
        ///     Commit the settings and perform the operation.
        /// </summary>
        public void Execute()
        {
            PreLaunch();
            ExecuteOperation();
        }

        /// <summary>
        ///     Execute the operation here.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method is called after pre-launch code has run. Everything that could be
        ///         validated should have been before this method is called.
        ///     </para>
        ///     <para>
        ///         The only validation could that should be in this method are things that could
        ///         not be checked in the <see cref="PreLaunchOperation" /> method. For example, if
        ///         an operation must be completed in multiple steps, and subsequent steps depend
        ///         on the success of previous steps, then it must be in this method by necessity.
        ///     </para>
        /// </remarks>
        protected abstract void ExecuteOperation();

        /// <summary>
        ///     Validate all settings and choices here.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         All possible validation should be done here. The point of this method is to simplify
        ///         the <see cref="ExecuteOperation" /> method that each operation must implement.
        ///     </para>
        ///     <para>
        ///         Conflicting choices that could not be checked by the <see cref="OtpSettings{T}" />
        ///         methods should be checked here.
        ///     </para>
        ///     <para>
        ///         Many of the operation classes use nullable fields (<see langword="bool?" />) for choices.
        ///         This allows the <see cref="PreLaunchOperation" /> implementation to verify that a
        ///         choice has been made. In the <see cref="ExecuteOperation" /> method, the field has
        ///         already been validated, and an exception thrown if it was not set, so null-forgiving
        ///         operators are used when accessing those fields in <see cref="ExecuteOperation" />.
        ///     </para>
        /// </remarks>
        protected virtual void PreLaunchOperation() { }

        private void PreLaunch()
        {
            // If this is not an operation that needs a slot, it will be null.
            // This protects against someone casting some random int to Slot.
            // To clarify, if you're writing an operation that does not need a
            // slot, set _slot to Slot.None in your constructor.
            if (OtpSlot.HasValue && OtpSlot != Slot.ShortPress && OtpSlot != Slot.LongPress)
            {
                throw new InvalidOperationException(ExceptionMessages.SlotNotSet);
            }

            PreLaunchOperation();
        }

        /// <summary>
        ///     Set the current access code the YubiKey slot is programmed with.
        /// </summary>
        /// <remarks>
        ///     There are two access code methods - <see cref="UseCurrentAccessCode(SlotAccessCode)" />
        ///     and <see cref="SetNewAccessCode(SlotAccessCode)" />. If the YubiKey slot is
        ///     currently protected by an access code, it must be specified when you call
        ///     <see cref="UseCurrentAccessCode(SlotAccessCode)" />. If you wish to retain the same
        ///     access code, it must also be specified to <see cref=" SetNewAccessCode(SlotAccessCode)" />.
        ///     If you do not specify it as the new access code, then the slot will have
        ///     its protection removed and have no access code.
        /// </remarks>
        /// <param name="code">The <see cref="SlotAccessCode" /> to use.</param>
        /// <returns>The <see cref="ConfigureStaticPassword" /> instance</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown if the access code is longer than <see cref="SlotAccessCode.MaxAccessCodeLength" />.
        /// </exception>
        public T UseCurrentAccessCode(SlotAccessCode code)
        {
            if (code is null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            CurrentAccessCode = code.AccessCodeBytes;
            return (T)this;
        }

        /// <summary>
        ///     Set the new access code the YubiKey slot will be programmed with.
        /// </summary>
        /// <inheritdoc cref="UseCurrentAccessCode(SlotAccessCode)" />
        public T SetNewAccessCode(SlotAccessCode code)
        {
            if (code is null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            NewAccessCode = code.AccessCodeBytes;
            return (T)this;
        }
    }
}
