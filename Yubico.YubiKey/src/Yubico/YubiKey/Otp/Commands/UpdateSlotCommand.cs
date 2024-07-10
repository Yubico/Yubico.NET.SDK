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

namespace Yubico.YubiKey.Otp.Commands
{
    /// <summary>
    ///     Applies a subset of configurable flags to one of the two OTP slots.
    /// </summary>
    public class UpdateSlotCommand : SlotConfigureBase
    {
        protected override byte ShortPressCode => OtpConstants.UpdateShortPressSlot;
        protected override byte LongPressCode => OtpConstants.UpdateLongPressSlot;

        /// <summary>
        ///     Extended flags that control behaviors on either a slot or global basis.
        /// </summary>
        /// <remarks>
        ///     <list type="table">
        ///         <listheader>When updating a configuration, the following flags may be changed:</listheader>
        ///         <item>
        ///             <term>
        ///                 <see cref="ExtendedFlags.AllowUpdate" />
        ///             </term>
        ///             <description>
        ///                 <inheritdoc cref="ExtendedFlags.AllowUpdate" path="/summary" />
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <see cref="ExtendedFlags.Dormant" />
        ///             </term>
        ///             <description>
        ///                 <inheritdoc cref="ExtendedFlags.Dormant" path="/summary" />
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <see cref="ExtendedFlags.FastTrigger" />
        ///             </term>
        ///             <description>
        ///                 <inheritdoc cref="ExtendedFlags.FastTrigger" path="/summary" />
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <see cref="ExtendedFlags.InvertLed" />
        ///             </term>
        ///             <description>
        ///                 <inheritdoc cref="ExtendedFlags.InvertLed" path="/summary" />
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <see cref="ExtendedFlags.SerialNumberApiVisible" />
        ///             </term>
        ///             <description>
        ///                 <inheritdoc cref="ExtendedFlags.SerialNumberApiVisible" path="/summary" />
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <see cref="ExtendedFlags.SerialNumberButtonVisible" />
        ///             </term>
        ///             <description>
        ///                 <inheritdoc cref="ExtendedFlags.SerialNumberButtonVisible" path="/summary" />
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <see cref="ExtendedFlags.SerialNumberUsbVisible" />
        ///             </term>
        ///             <description>
        ///                 <inheritdoc cref="ExtendedFlags.SerialNumberUsbVisible" path="/summary" />
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <see cref="ExtendedFlags.UseNumericKeypad" />
        ///             </term>
        ///             <description>
        ///                 <inheritdoc cref="ExtendedFlags.UseNumericKeypad" path="/summary" />
        ///             </description>
        ///         </item>
        ///     </list>
        ///     <para>
        ///         The <see cref="ExtendedFlags.AllowUpdate" /> flag must be present if the slot is to remain
        ///         updatable. Failure to set this flag will effectively make the configuration read-only,
        ///         until a brand new, unrelated configuration overwrites it.
        ///     </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if an invalid flag set is specified.
        /// </exception>
        public override ExtendedFlags ExtendedFlags
        {
            get => base.ExtendedFlags;
            set
            {
                value.ValidateFlagsForUpdate();
                base.ExtendedFlags = value;
            }
        }

        /// <summary>
        ///     Flags that control the output format of the text returned by the YubiKey button press.
        /// </summary>
        /// <remarks>
        ///     <list>
        ///         <listheader>
        ///             When updating a configuration, the following flags may be changed:
        ///         </listheader>
        ///         <item>
        ///             <term>
        ///                 <see cref="TicketFlags.AppendCarriageReturn" />
        ///             </term>
        ///             <description>
        ///                 <inheritdoc cref="TicketFlags.AppendCarriageReturn" path="/summary" />
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <see cref="TicketFlags.AppendDelayToFixed" />
        ///             </term>
        ///             <description>
        ///                 <inheritdoc cref="TicketFlags.AppendDelayToFixed" path="/summary" />
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <see cref="TicketFlags.AppendDelayToOtp" />
        ///             </term>
        ///             <description>
        ///                 <inheritdoc cref="TicketFlags.AppendDelayToOtp" path="/summary" />
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <see cref="TicketFlags.AppendTabToFixed" />
        ///             </term>
        ///             <description>
        ///                 <inheritdoc cref="TicketFlags.AppendTabToFixed" path="/summary" />
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <see cref="TicketFlags.AppendTabToOtp" />
        ///             </term>
        ///             <description>
        ///                 <inheritdoc cref="TicketFlags.AppendTabToOtp" path="/summary" />
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <see cref="TicketFlags.TabFirst" />
        ///             </term>
        ///             <description>
        ///                 <inheritdoc cref="TicketFlags.TabFirst" path="/summary" />
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if an invalid flag set is specified.
        /// </exception>
        public override TicketFlags TicketFlags
        {
            get => base.TicketFlags;
            set
            {
                value.ValidateFlagsForUpdate();
                base.TicketFlags = value;
            }
        }

        /// <summary>
        ///     Flags that define the mode and other configurable options for this slot.
        /// </summary>
        /// <remarks>
        ///     <list type="table">
        ///         <listheader>
        ///             When updating a configuration, the following flags may be changed:
        ///         </listheader>
        ///         <item>
        ///             <term>
        ///                 <see cref="ConfigurationFlags.Use10msPacing" />
        ///             </term>
        ///             <description>
        ///                 <inheritdoc cref="ConfigurationFlags.Use10msPacing" path="/summary" />
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <see cref="ConfigurationFlags.Use20msPacing" />
        ///             </term>
        ///             <description>
        ///                 <inheritdoc cref="ConfigurationFlags.Use20msPacing" path="/summary" />
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if an invalid flag set is specified.
        /// </exception>
        public override ConfigurationFlags ConfigurationFlags
        {
            get => base.ConfigurationFlags;
            set
            {
                value.ValidateFlagsForUpdate();
                base.ConfigurationFlags = value;
            }
        }
    }
}
