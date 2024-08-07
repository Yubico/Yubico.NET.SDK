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
using Yubico.YubiKey.Otp;

namespace Yubico.YubiKey.TestApp.Plugins.Otp
{
    internal class UpdateSlot : OtpPluginBase
    {
        public override string Name => "Update";

        public override string Description => "Updates non-security related parameters on a YubiKey.";

        protected override ParameterUse ParametersUsed =>
            ParameterUse.Slot
            | ParameterUse.CurrentAccessCode
            | ParameterUse.NewAccessCode
            | ParameterUse.Force
            | ParameterUse.NoEnter;

        public UpdateSlot(IOutput output) : base(output)
        {
            // We'll use the base class for things that are common to other plug-ins.
            // We'll add everything else here. As we implement things from here that
            // other classes use, we'll remove them from here and put them in the base
            // class.
            Parameters["dormant"] = new Parameter
            {
                Name = "Dormant",
                Shortcut = "d",
                Description = "Allows a configuration to be stored without being accessible.",
                Type = typeof(bool)
            };
            Parameters["fasttrigger"] = new Parameter
            {
                Name = "FastTrigger",
                Shortcut = "ft",
                Description = "Causes the trigger action of the YubiKey button to become faster. " +
                    "This only applies when one configuration is written. If both configurations " +
                    "are active, this setting has no effect.",
                Type = typeof(bool)
            };
            Parameters["invertled"] = new Parameter
            {
                Name = "InvertLed",
                Shortcut = "i",
                Description = "Inverts the configured state of the LED.",
                Type = typeof(bool)
            };
            Parameters["serialapi"] = new Parameter
            {
                Name = "SerialApi",
                Shortcut = "sa",
                Description = "Allows the serial number to be read by proprietary means, including " +
                    "being visible to the Yubico.YubiKey SDK. This is a device wide setting. " +
                    "If it is set in either configurable slot, it is considered enabled by the device.",
                Type = typeof(bool)
            };
            Parameters["serialbutton"] = new Parameter
            {
                Name = "SerialButton",
                Shortcut = "sb",
                Description = "Allows the serial number to be retrieved by holding down the YubiKey " +
                    "button while inserting the device into the USB port. Once the LED starts to " +
                    "flash, release the button and the serial number will then be sent as a string " +
                    "of digits. This is a device wide setting. If it is set in either configurable " +
                    "slot, it is considered enabled by the device.",
                Type = typeof(bool)
            };
            Parameters["serialusb"] = new Parameter
            {
                Name = "SerialUsb",
                Shortcut = "su",
                Description = "Makes the serial number appear in the YubiKey's USB descriptor's " +
                    "iSerialNumber field. This makes each device unique from the host computer's " +
                    "view. This is a device wide setting. If it is set in either configurable slot, " +
                    "it is considered enabled by the device.",
                Type = typeof(bool)
            };
            Parameters["numerickeypad"] = new Parameter
            {
                Name = "NumericKeypad",
                Shortcut = "nk",
                Description = "Causes numeric characters to be sent as keystrokes from the numeric " +
                    "keypad rather than the normal numeric keys on an 84-key keyboard.",
                Type = typeof(bool)
            };
            Parameters["sendtabfirst"] = new Parameter
            {
                Name = "SendTabFirst",
                Shortcut = "t",
                Description = "Sends a tab character before the fixed string.",
                Type = typeof(bool)
            };
            Parameters["appendtabtofixed"] = new Parameter
            {
                Name = "AppendTabToFixed",
                Shortcut = "tf",
                Description = "Sends a tab character after the fixed string.",
                Type = typeof(bool)
            };
            Parameters["appendtabtootp"] = new Parameter
            {
                Name = "AppendTabToOtp",
                Shortcut = "to",
                Description = "Sends a tab character after the OTP string.",
                Type = typeof(bool)
            };
            Parameters["appenddelaytofixed"] = new Parameter
            {
                Name = "AppendDelayToFixed",
                Shortcut = "df",
                Description = "Adds a 500ms delay after sending the fixed string.",
                Type = typeof(bool)
            };
            Parameters["appenddelaytootp"] = new Parameter
            {
                Name = "AppendDelayToOtp",
                Shortcut = "do",
                Description = "Adds a 500ms delay after sending the OTP string.",
                Type = typeof(bool)
            };
            Parameters["use10mspacing"] = new Parameter
            {
                Name = "Use10msPacing",
                Shortcut = "10ms",
                Description = "Adds an inter-character pacing time of 10ms between each keystroke. " +
                    "This setting is not compatible with challenge-response (YubiOtp or HMAC-SHA1). " +
                    "You can combine this setting with Use20msPacing to get 30ms pacing.",
                Type = typeof(bool)
            };
            Parameters["use20mspacing"] = new Parameter
            {
                Name = "Use20msPacing",
                Shortcut = "20ms",
                Description = "Adds an inter-character pacing time of 20ms between each keystroke. " +
                    "This setting is not compatible with challenge-response (YubiOtp or HMAC-SHA1). " +
                    "You can combine this setting with Use10msPacing to get 30ms pacing.",
                Type = typeof(bool)
            };
            Parameters["allowupdate"] = new Parameter
            {
                Name = "AllowUpdate",
                Shortcut = "au",
                Description = "Allow certain non-security properties to be updated in the configuration.",
                Type = typeof(bool)
            };
        }

        public override void HandleParameters()
        {
            base.HandleParameters();

            _dormant = (bool?)Parameters["dormant"].Value ?? false;
            _fastTrigger = (bool?)Parameters["fasttrigger"].Value ?? false;
            _invertLed = (bool?)Parameters["invertled"].Value ?? false;
            _serialApi = (bool?)Parameters["serialapi"].Value ?? false;
            _serialButton = (bool?)Parameters["serialbutton"].Value ?? false;
            _serialUsb = (bool?)Parameters["serialusb"].Value ?? false;
            _numericKeypad = (bool?)Parameters["numerickeypad"].Value ?? false;
            _sendTabFirst = (bool?)Parameters["sendtabfirst"].Value ?? false;
            _appendTabToFixed = (bool?)Parameters["appendtabtofixed"].Value ?? false;
            _appendTabToOtp = (bool?)Parameters["appendtabtootp"].Value ?? false;
            _appendDelayToFixed = (bool?)Parameters["appenddelaytofixed"].Value ?? false;
            _appendDelayToOtp = (bool?)Parameters["appenddelaytootp"].Value ?? false;
            _use10msPacing = (bool?)Parameters["use10mspacing"].Value ?? false;
            _use20msPacing = (bool?)Parameters["use20mspacing"].Value ?? false;
            _allowUpdate = (bool?)Parameters["allowupdate"].Value ?? false;

            // We don't have any way to get configuration from the key, so we'll
            // just have to assume that options chosen here that aren't compatible
            // with the settings on the key are just something we can't do anything
            // about here.
            _yubiKey = GetYubiKey(_serialNumber);

            if (_slot == Slot.None)
            {
                throw new InvalidOperationException("No slot was specified.");
            }
        }

        /// <summary>
        /// Change non-security-related settings on the YubiKey.
        /// </summary>
        /// <inheritdoc cref="YubiKey.Otp.OtpSettings{T}.AllowUpdate(bool)" path="/remarks" />
        /// <returns>True if success, false if not.</returns>
        public override bool Execute()
        {
            var otp = new OtpSession(_yubiKey!);

            bool ready =
                _slot == Slot.ShortPress
                ? otp.IsShortPressConfigured
                : otp.IsLongPressConfigured;
            if (!ready)
            {
                Output.WriteLine($"Slot[{_slot}] is not programmed and can't be updated.");
                Output.WriteLine("Aborted.");
                return false;
            }

            if (!ConfirmConfig())
            {
                Output.WriteLine("Aborted.", OutputLevel.Error);
                return false;
            }
            otp.UpdateSlot(_slot)
                .SetDormant(_dormant)
                .SetFastTrigger(_fastTrigger)
                .SetInvertLed(_invertLed)
                .SetSerialNumberApiVisible(_serialApi)
                .SetSerialNumberButtonVisible(_serialButton)
                .SetSerialNumberUsbVisible(_serialUsb)
                .SetUseNumericKeypad(_numericKeypad)
                .SetSendTabFirst(_sendTabFirst)
                .SetAppendTabToFixed(_appendTabToFixed)
                .SetAppendTabToOtp(_appendTabToOtp)
                .SetAppendDelayToFixed(_appendDelayToFixed)
                .SetAppendDelayToOtp(_appendDelayToOtp)
                .SetAppendCarriageReturn(!_noEnter)
                .SetUse10msPacing(_use10msPacing)
                .SetUse20msPacing(_use20msPacing)
                .SetAllowUpdate(_allowUpdate)
                .Execute();

            return true;
        }

        private bool ConfirmConfig()
        {
            Output.WriteLine(_force
                ? $"Updating Configuration for Slot [{_slot}]:"
                : $"Proposed Configuration for slot [{_slot}]:");
            Output.WriteLine(new string('-', 80));
            Output.Write($"AppendDelayToFixed:    | {Val(_appendDelayToFixed)} | AppendDelayToOtp: ");
            Output.WriteLine($"{Val(_appendDelayToOtp)} | Dormant:    {Val(_dormant)}");
            Output.WriteLine(new string('-', 80));
            Output.Write($"SerialVisibleToApi:    | {Val(_serialApi)} | UseNumericKeypad: ");
            Output.WriteLine($"{Val(_numericKeypad)} | 10msPacing: {Val(_use10msPacing)}");
            Output.WriteLine(new string('-', 80));
            Output.Write($"SerialVisibleToButton: | {Val(_serialButton)} | AppendTabToFixed: ");
            Output.WriteLine($"{Val(_appendTabToFixed)} | 20msPacing: {Val(_use20msPacing)}");
            Output.WriteLine(new string('-', 80));
            Output.Write($"SerialVisibleToUsb:    | {Val(_serialUsb)} | AppendTabToOtp:   ");
            Output.WriteLine($"{Val(_appendTabToOtp)} | NoEnter:    {Val(_noEnter)}");
            Output.WriteLine(new string('-', 80));
            Output.Write($"SendTabFirst:          | {Val(_sendTabFirst)} | FastTrigger:      ");
            Output.WriteLine($"{Val(_fastTrigger)} | InvertLed:  {Val(_invertLed)}");
            Output.WriteLine(new string('-', 80) + Environment.NewLine);

            if (_force)
            {
                return true;
            }
            Output.WriteLine("Type \"Yes\" and press [Enter] to proceed.");
            Output.WriteLine("Type anything else or hit [Ctrl] + [C] to abort.");
            return Console.ReadLine()?.ToLower() == "yes";

            static string Val(bool val) => val ? "true " : "false";
        }

        #region Private Fields
        private bool _dormant;
        private bool _fastTrigger;
        private bool _invertLed;
        private bool _serialApi;
        private bool _serialButton;
        private bool _serialUsb;
        private bool _numericKeypad;
        private bool _sendTabFirst;
        private bool _appendTabToFixed;
        private bool _appendTabToOtp;
        private bool _appendDelayToFixed;
        private bool _appendDelayToOtp;
        private bool _use10msPacing;
        private bool _use20msPacing;
        private bool _allowUpdate;
        #endregion
    }
}
