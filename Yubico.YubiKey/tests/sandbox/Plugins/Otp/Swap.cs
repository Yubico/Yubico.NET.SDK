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
    internal class Swap : OtpPluginBase
    {
        public override string Name => "Swap";

        public override string Description => "Swaps the two slot configurations.";

        protected override ParameterUse ParametersUsed => ParameterUse.Force;

        public Swap(IOutput output) : base(output) { }

        public override void HandleParameters()
        {
            base.HandleParameters();
            _yubiKey = GetYubiKey(_serialNumber);
        }

        public override bool Execute()
        {
            using var otpSession = new OtpSession(_yubiKey!);
            bool isConfigured = otpSession.IsShortPressConfigured || otpSession.IsLongPressConfigured;

            if (!isConfigured)
            {
                throw new InvalidOperationException("None of the slots are configured.");
            }

            if (!_force)
            {
                Output.WriteLine("Do you really want to swap slots?");
                Output.WriteLine("Type \"Yes\" and press [Enter] to swap");
                if (Console.ReadLine()?.ToLower() != "yes")
                {
                    Output.WriteLine("Aborted.", OutputLevel.Error);
                    return false;
                }
            }

            try
            {
                otpSession.SwapSlots();
            }
            catch (Exception ex)
            {
                throw new PluginFailureException($"Error executing OtpSession.SwapSlots: { ex.Message }.", ex);
            }

            return true;
        }
    }
}
