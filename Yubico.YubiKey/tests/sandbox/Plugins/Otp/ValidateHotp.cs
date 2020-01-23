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
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using Yubico.Core.Buffers;
using Yubico.YubiKey.Otp.Operations;

namespace Yubico.YubiKey.TestApp.Plugins.Otp
{
    internal class ValidateHotp : OtpPluginBase
    {
        public override string Name => "ValidateHotp";

        public override string Description => "Validates a HOTP challenge. Important notice: " +
            "This is a test tool not meant for secure operations!";

        protected override ParameterUse ParametersUsed =>
            ParameterUse.Key
            | ParameterUse.TotpKey
            | ParameterUse.Digits
            | ParameterUse.Password
            | ParameterUse.IMF;

        public ValidateHotp(IOutput output) : base(output)
        {
            // This plugin doesn't actually talk to a YubiKey, so no need for serial-number.
            _ = Parameters.Remove("serial-number");

            Parameters["password"].Description = "This is the passcode to test. It should be numeric " +
                "digits between 0 and 9. Other characters will cause an error. If you do not specify " +
                "a passcode, then the passcode generated from the key and the initial moving factor " +
                "will be printed. This passcode has to have the same number of digits specified by the " +
                "digits parameter. If you don't specify, the default is six digits.";

            Parameters["initialmovingfactor"].Description = "Initial moving factor. This is the counter " +
                "to use for the calculation. Because this plug-in is stateless, this counter will not be " +
                "used beyond just the one calculation. Unlike programming the YubiKey, there is no " +
                "constraint on the value being evenly dividable by 16. It still must be between 0 and " +
                "1048560 (0xffff0).";
        }

        public override void HandleParameters()
        {
            // We'll let the base class handle populating all of the fields.
            base.HandleParameters();

            // It's better, however, for the individual plugins to decide how to
            // validate options.
            var exceptions = new List<Exception>();

            if (_totpkey.Length > 0)
            {
                if (_key.Length > 0)
                {
                    exceptions.Add(
                        new InvalidOperationException("You cannot specify both totpkey and key."));
                }
                else
                {
                    _key = _totpkey;
                }
            }

            if (_key.Length > ConfigureChallengeResponse.HmacSha1KeySize)
            {
                exceptions.Add(
                    new InvalidOperationException("The supplied key is too long. It must be 20 bytes or less."));
            }

            // If digits wasn't specified, default to six.
            _digits ??= 6;

            if (!string.IsNullOrWhiteSpace(_password))
            {
                // It's a little bit of a hack that we're reusing _password.
                // Rather than making the parameter stuff take care of it,
                // we'll just make sure it's BCD.
                try
                {
                    _ = Bcd.DecodeText(_password);
                }
                catch (Exception ex)
                {
                    exceptions.Add(new InvalidOperationException(
                        $"Error decoding passcode digits: { ex.Message }",
                        ex));
                }
                if (_password.Length != _digits)
                {
                    exceptions.Add(new InvalidOperationException(
                        $"Passcode supplied ({ _password }) is not the correct length. " +
                        $"Expected { _digits } digits."));
                }
            }

            if (_imf < 0 || _imf > 0xffff0)
            {
                throw new InvalidOperationException(
                    $"Invalid IMF ({ _imf }). IMF must be between 0 and 1048560 (0xffff0).");
            }
            else
            {
                // The challenge is the counter in network byte order.
                _challenge = new byte[sizeof(long)];
                BinaryPrimitives.WriteInt64BigEndian(_challenge, (long)_imf);
            }



            if (exceptions.Count > 0)
            {
                throw exceptions.Count == 1
                    ? exceptions[0]
                    : new AggregateException(
                        $"{ exceptions.Count } errors encountered.",
                        exceptions);
            }

        }

        public override bool Execute()
        {
            using var hmac = new HMACSHA1(_key);
            byte[] hash = hmac.ComputeHash(_challenge);

            byte offset = (byte)(hash[^1] & 0x0f);
            // The ykman code reads this as a uint, but masks the top bit. I'll just do the same
            // thing, but treat it as an int since that's CLS-compliant.
            int dataInt = (int)BinaryPrimitives.ReadUInt32BigEndian(hash[offset..]) & 0x7fffffff;

            string code = (dataInt % (uint)Math.Pow(10, _digits!.Value))
                .ToString(CultureInfo.InvariantCulture).PadLeft(_digits!.Value, '0');
            if (!string.IsNullOrWhiteSpace(_password))
            {
                bool pass = code == _password;
                Output.Write(pass ? "Verified" : "Invalid", OutputLevel.Quiet);
                Output.Write(" code: " + code);
                Output.WriteLine(string.Empty, OutputLevel.Quiet);
                return pass;
            }
            Output.Write("Code: ");
            Output.WriteLine(code, OutputLevel.Quiet);

            return true;
        }
    }
}
