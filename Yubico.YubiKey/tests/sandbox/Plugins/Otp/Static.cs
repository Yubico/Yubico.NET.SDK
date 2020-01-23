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
using Yubico.YubiKey.Otp;
using Yubico.YubiKey.Otp.Commands;
using Yubico.YubiKey.Otp.Operations;

namespace Yubico.YubiKey.TestApp.Plugins.Otp
{
    internal class Static : OtpPluginBase
    {
        public override string Name => "Static";

        public override string Description => "Configures a static password in a YubiKey OTP slot.";

        protected override ParameterUse ParametersUsed =>
            ParameterUse.Password
            | ParameterUse.Generate
            | ParameterUse.Length
            | ParameterUse.Keyboard
            | ParameterUse.CurrentAccessCode
            | ParameterUse.NewAccessCode
            | ParameterUse.Slot
            | ParameterUse.Force
            | ParameterUse.NoEnter;

        public Static(IOutput output) : base(output) { }

        public override void HandleParameters()
        {
            // We'll let the base class handle populating all of the fields.
            base.HandleParameters();

            // It's better, however, for the individual plugins to decide how to
            // validate options.
            var exceptions = new List<Exception>();

            if (_generate && _password.Length > 0)
            {
                exceptions.Add(
                    new ArgumentException("You can only generate a password " +
                    "or specify one, but not both."));
            }
            if (_password.Length > 0 && _passwordLength > 0)
            {
                exceptions.Add(
                    new ArgumentException("You can't specify a password length " +
                    "if you are specifying a password."));
            }
            if (_password.Length == 0 && !_generate)
            {
                exceptions.Add(
                    new ArgumentException("You must select to generate a password " +
                    "or specify a password to use."));
            }
            if (!_generate && _passwordLength > 0)
            {
                exceptions.Add(
                    new ArgumentException("It is not relevant to select a password " +
                    "length if you have not selected to generate a password."));
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
                throw new AggregateException($"{ exceptions.Count } errors encountered.",
                    exceptions);
            }
            else if (exceptions.Count == 1)
            {
                throw exceptions[0];
            }

            // I was going to check the access codes here, but I think it's a
            // better idea to just let them fly so the real production code can
            // tell us if something is wrong.
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
                char[] password = Array.Empty<char>();
                if (_generate)
                {
                    int len =
                        _passwordLength > 0
                        ? _passwordLength
                        : SlotConfigureBase.MaxPasswordLength;
                    password = new char[len];
                }
                else
                {
                    password = _password.ToCharArray();
                }
                try
                {
                    ConfigureStaticPassword op = otp.ConfigureStaticPassword(_slot)
                        .WithKeyboard(_keyboard)
                        .UseCurrentAccessCode((SlotAccessCode)_currentAccessCode)
                        .SetNewAccessCode((SlotAccessCode)_newAccessCode)
                        .AppendCarriageReturn(!_noEnter);
                    if (_generate)
                    {
                        op = op.GeneratePassword(password);
                    }
                    else
                    {
                        op = op.SetPassword(password);
                    }
                    op.Execute();

                    // If OutputLevel is None or Error, then no output here.
                    if (Output.OutputLevel > OutputLevel.Error)
                    {
                        string output = string.Empty;
                        // If it's generated, or set to verbose, output the password.
                        if (_generate || Output.OutputLevel > OutputLevel.Normal)
                        {
                            if (Output.OutputLevel >= OutputLevel.Normal)
                            {
                                Output.Write("Password set: [");
                                Output.WriteSensitive(password);
                                Output.Write("].");
                            }
                            else
                            {
                                Output.WriteSensitive(password);
                                Output.WriteLine();
                            }
                        }
                        else if (Output.OutputLevel > OutputLevel.Quiet)
                        {
                            // If it's not quiet, just output that it's done.
                            Output.WriteLine("Password set.");
                        }
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            Output.WriteLine(output, OutputLevel.Quiet);
                        }
                    }
                }
                finally
                {
                    for (int i = 0; i < password.Length; ++i)
                    {
                        password[i] = (char)0xfe;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new PluginFailureException(
                    $"Error executing OtpSession.SetStaticPassword: { ex.Message }.",
                    ex);
            }

            return true;
        }
    }
}
