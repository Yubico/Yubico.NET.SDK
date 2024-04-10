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
using Yubico.YubiKey.Otp.Operations;

namespace Yubico.YubiKey.TestApp.Plugins.Otp
{
    internal class Delete : OtpPluginBase
    {
        public override string Name => "Delete";

        public override string Description => "Delete the configuration from an OTP slot.";

        protected override ParameterUse ParametersUsed =>
            ParameterUse.Slot
            | ParameterUse.Force
            | ParameterUse.CurrentAccessCode;

        public Delete(IOutput output) : base(output) { }

        public override void HandleParameters()
        {
            base.HandleParameters();

            _yubiKey = GetYubiKey(_serialNumber);
        }

        public override bool Execute()
        {
            using var otp = new OtpSession(_yubiKey!);

            if (!Verify(otp, $"Type \"Yes\" to delete slot[{_slot}] configuration."))
            {
                Output.WriteLine("Aborted.", OutputLevel.Error);
                return false;
            }
            try
            {
                otp.DeleteSlotConfiguration(_slot)
                    .UseCurrentAccessCode((SlotAccessCode)_currentAccessCode)
                    .Execute();
                Output.WriteLine($"Configuration in OTP slot [{_slot}] deleted.");
            }
            catch (Exception ex)
            {
                throw new PluginFailureException(
                    $"Error executing OtpSession.DeleteSlotConfiguration: {ex.Message}.",
                    ex);
            }

            return true;
        }
    }
}
