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
using Yubico.Core.Buffers;
using Yubico.YubiKey.Otp;
using Yubico.YubiKey.Otp.Operations;
using Yubico.YubiKey.TestApp.Converters;

namespace Yubico.YubiKey.TestApp.Plugins.Otp
{
    internal class Hotp : OtpPluginBase
    {
        public Hotp(IOutput output) : base(output)
        {
            // We're reusing ParameterUse.Generate, so we'll update the description.
            Parameters["generate"].Description = "Generate a random key. Conflicts with key.";
            // We're reusing ParameterUse.Key, so we'll update it.
            var keyParam = Parameters["key"];
            keyParam.Description = "Key. This is to be provided as a base-32 encoded string.";
            keyParam.Type = typeof(Base32Bytes);
        }

        public override string Name => "HOTP";

        public override string Description => "Program an HMAC-SHA1 OATH-HOTP credential.";

        protected override ParameterUse ParametersUsed =>
            ParameterUse.Slot
            | ParameterUse.Key
            | ParameterUse.Digits
            | ParameterUse.IMF
            | ParameterUse.NoEnter
            | ParameterUse.Generate
            | ParameterUse.Force;

        public override void HandleParameters()
        {
            // We'll let the base class handle populating all of the fields.
            base.HandleParameters();

            // It's better, however, for the individual plugins to decide how to
            // validate options.
            var exceptions = new List<Exception>();

            if (!_generate)
            {
                if (_key.Length == 0)
                {
                    exceptions.Add(
                        new InvalidOperationException("You must either specify a key " +
                                                      "or specify that the key should be generated."));
                }
                else if (_key.Length < ConfigureHotp.HmacKeySize)
                {
                    Array.Resize(ref _key, ConfigureHotp.HmacKeySize);
                }
            }
            else if (_key.Length != 0)
            {
                exceptions.Add(new InvalidOperationException(
                    "You must either specify a key or specify that the key should be generated, but not both."));
            }
            else
            {
                _key = new byte[ConfigureHotp.HmacKeySize];
            }

            // If nothing was chosen, it's six.
            _digits ??= 6;
            if (_digits != 6 && _digits != 8)
            {
                exceptions.Add(
                    new InvalidOperationException("OATH-HOTP passwords must be six or eight " +
                                                  "digits. If you don't specify, 6 is default."));
            }

            try
            {
                _yubiKey = GetYubiKey(_serialNumber);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            if (exceptions.Count > 0)
            {
                throw exceptions.Count == 1
                    ? exceptions[index: 0]
                    : new AggregateException(
                        $"{exceptions.Count} errors encountered.",
                        exceptions);
            }
        }

        public override bool Execute()
        {
            using var otp = new OtpSession(_yubiKey!);

            if (!Verify(otp))
            {
                Output.WriteLine("Aborted.", OutputLevel.Error);
                return false;
            }

            try
            {
                var op = GetOperation(otp);
                op.Execute();
                OutputResult(op);
            }
            catch (Exception ex)
            {
                throw new PluginFailureException(
                    $"Error executing OtpSession.ConfigureHotp: {ex.Message}.",
                    ex);
            }

            return true;
        }

        private ConfigureHotp GetOperation(OtpSession otp)
        {
            var op = otp.ConfigureHotp(_slot)
                .UseCurrentAccessCode((SlotAccessCode)_currentAccessCode)
                .SetNewAccessCode((SlotAccessCode)_newAccessCode)
                .AppendCarriageReturn(!_noEnter)
                .UseInitialMovingFactor(_imf);
            op = _digits == 8 ? op.Use8Digits() : op;
            return _generate
                ? op.GenerateKey(_key)
                : op.UseKey(_key);
        }

        private void OutputResult(ConfigureHotp op)
        {
            Output.WriteLine("OATH-HOTP configured.");

            if (Output.OutputLevel > OutputLevel.Normal || _generate)
            {
                Span<char> encoded = stackalloc char[Base32.GetEncodedSize(_key.Length)];
                try
                {
                    Base32.EncodeBytes(_key, encoded);

                    Output.Write((_generate ? "Generated " : string.Empty) + "Key (base-32): ");
                    Output.WriteSensitive(encoded, OutputLevel.Quiet);
                    Output.WriteLine(string.Empty, OutputLevel.Quiet);
                    Output.WriteLine($"OTP Length: {_digits} digits", OutputLevel.Verbose);
                    Output.WriteLine($"Initial Moving Factor: {_imf}", OutputLevel.Verbose);
                }
                finally
                {
                    encoded.Clear();
                }
            }
        }
    }
}
