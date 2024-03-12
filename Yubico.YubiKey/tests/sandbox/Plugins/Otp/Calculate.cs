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
using System.Linq;
using Yubico.Core.Buffers;
using Yubico.YubiKey.Otp;
using Yubico.YubiKey.Otp.Operations;

namespace Yubico.YubiKey.TestApp.Plugins.Otp
{
    internal class Calculate : OtpPluginBase
    {
        public override string Name => "Calculate";

        public override string Description => "Perform a challenge-response operation.";

        protected override ParameterUse ParametersUsed =>
            ParameterUse.Totp
            | ParameterUse.Digits
            | ParameterUse.Slot
            | ParameterUse.Challenge
            | ParameterUse.YubiOtp;

        public Calculate(IOutput output) : base(output) { }

        public override bool Execute()
        {
            string result = string.Empty;
            using var otp = new OtpSession(_yubiKey!);
            if (!(_slot == Slot.ShortPress ? otp.IsShortPressConfigured : otp.IsLongPressConfigured))
            {
                Output.WriteLine(
                    $"Slot[{_slot}] is not programmed and can't be used for a challenge-response transaction.",
                    OutputLevel.Error);
                Output.WriteLine("Aborted.", OutputLevel.Error);
                return false;
            }
            try
            {
                // Important to use Console.WriteLine instead of Output.WriteLine
                // here. It would be useless if output were being written to a file.
                CalculateChallengeResponse op =
                    otp.CalculateChallengeResponse(_slot)
                    .UseTouchNotifier(() => Console.WriteLine("Touch the key."))
                    .UseYubiOtp(_yubiOtp);
                op =
                    _generateTotp
                    ? op.UseTotp()
                    : op.UseChallenge(_challenge);

                result =
                    _digits.HasValue
                    ? op.GetCode(_digits.Value)
                    : _yubiOtp
                        ? ModHex.EncodeBytes(op.GetDataBytes().Span)
                        : Hex.BytesToHex(op.GetDataBytes().Span).ToLower();
            }
            catch (Exception ex)
            {
                Output.WriteLine(
                    $"Error attempting to calculate challenge response: {ex.Message}.",
                    OutputLevel.Error);
                return false;
            }

            if (Output.OutputLevel > OutputLevel.Normal)
            {
                Output.WriteLine(
                    _digits.HasValue
                    ? $"OTP Code is [{result}]"
                    : $"Response is [{result}]");
            }
            else
            {
                Output.WriteLine(result, OutputLevel.Quiet);
            }

            return true;
        }

        public override void HandleParameters()
        {
            // We'll let the base class handle populating all of the fields.
            base.HandleParameters();

            // It's better, however, for the individual plugins to decide how to
            // validate options.
            var exceptions = new List<Exception>();

            if (_slot != Slot.ShortPress && _slot != Slot.LongPress)
            {
                exceptions.Add(
                    new ArgumentException("Invalid (or no) slot specified. " +
                    "You must specify slot 1 (ShortPress) or 2 (LongPress)."));
            }
            if (_generateTotp && _challenge.Length != 0)
            {
                exceptions.Add(new ArgumentException("Can't use both TOTP and a challenge."));
            }
            if (_digits.HasValue && _yubiOtp)
            {
                exceptions.Add(new ArgumentException("You can't specify digits with Yubico OTP."));
            }
            if (_digits.HasValue && _digits != 6 && _digits != 8)
            {
                exceptions.Add(new ArgumentException("The response must be either six (6) or eight (8) digits."));
            }
            if (!_challenge.Any() && !_generateTotp)
            {
                exceptions.Add(new ArgumentException("You much choose either TOTP or provide a challenge."));
            }
            if (_yubiOtp && _challenge.Length < CalculateChallengeResponse.YubicoOtpChallengeSize)
            {
                Array.Resize(ref _challenge, CalculateChallengeResponse.YubicoOtpChallengeSize);
            }

            try
            {
                _yubiKey = GetYubiKey(_serialNumber);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            if (exceptions.Count > 1)
            {
                throw new AggregateException($"{exceptions.Count} errors encountered.",
                    exceptions);
            }
            else if (exceptions.Count == 1)
            {
                throw exceptions[0];
            }
        }
    }
}
