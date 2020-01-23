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

namespace Yubico.YubiKey.TestApp.Plugins.Otp
{
    internal class ChallengeResponse : OtpPluginBase
    {
        public override string Name => "ChalResp";

        public override string Description => "Program a challenge-response credential.";

        protected override ParameterUse ParametersUsed =>
            ParameterUse.Button
            | ParameterUse.Generate
            | ParameterUse.Force
            | ParameterUse.Slot
            | ParameterUse.Key
            | ParameterUse.TotpKey
            | ParameterUse.Totp
            | ParameterUse.YubiOtp
            | ParameterUse.ShortHMAC;

        public ChallengeResponse(IOutput output) : base(output)
        {
            // We're reusing ParameterUse.Generate, so we'll update the description.
            Parameters["generate"].Description = "Generate a random key. Conflicts with key, " +
                "TOTP, and generate.";
            // We're reusing ParameterUse.Totp...
            Parameters["totp"].Description = "Output key as base32 (generally used in TOTP applications).";
        }

        public override void HandleParameters()
        {
            // We'll let the base class handle populating all of the fields.
            base.HandleParameters();

            // It's better, however, for the individual plugins to decide how to
            // validate options.
            var exceptions = new List<Exception>();

            try
            {
                _yubiKey = GetYubiKey(_serialNumber);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            if (_useShortChallenge && _yubiOtp)
            {
                exceptions.Add(new InvalidOperationException(
                    "Specifying full HMAC challenges is not compatible with speciying Yubico OTP."));
            }

            if (_totpkey.Length > 0)
            {
                if (_key.Length > 0)
                {
                    exceptions.Add(new InvalidOperationException(
                        "You can't specify both key and TOTP-Key."));
                }
                else
                {
                    _key = _totpkey;
                }
            }

            int expectedKeyLength =
                _yubiOtp
                ? ConfigureChallengeResponse.YubiOtpKeySize
                : ConfigureChallengeResponse.HmacSha1KeySize;
            if (!_generate)
            {
                if (_key.Length == 0)
                {
                    exceptions.Add(new InvalidOperationException(
                        "You must either specify a key or specify that the key should be generated."));
                }
                else
                {
                    if (_key.Length < expectedKeyLength)
                    {
                        Array.Resize(ref _key, expectedKeyLength);
                    }
                }
            }
            else if (_key.Length != 0)
            {
                exceptions.Add(new InvalidOperationException(
                    "You must either specify a key or specify that the key should be generated, but not both."));
            }
            else
            {
                _key = new byte[expectedKeyLength];
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
            using var otp = new OtpSession(_yubiKey!);

            ConfigureChallengeResponse op = otp.ConfigureChallengeResponse(_slot)
                .UseCurrentAccessCode((SlotAccessCode)_currentAccessCode)
                .SetNewAccessCode((SlotAccessCode)_newAccessCode)
                .UseSmallChallenge(_useShortChallenge)
                .UseButton(_button);

            if (!Verify(otp))
            {
                Output.WriteLine("Aborted.", OutputLevel.Error);
                return false;
            }
            op = _yubiOtp
                ? op.UseYubiOtp()
                : op.UseHmacSha1();
            op = _generate
                ? op.GenerateKey(_key)
                : op.UseKey(_key);
            op = _useShortChallenge
                ? op.UseSmallChallenge()
                : op;

            op.Execute();

            OutputResult();

            return true;
        }

        private void OutputResult()
        {
            Output.WriteLine($"Challenge-response ({ (_yubiOtp ? "Yubico OTP" : "HMAC-SHA1") }) configured.");

            if (_generate || _generateTotp || Output.OutputLevel > OutputLevel.Normal)
            {
                int encodedKeySize =
                    _generateTotp
                    ? Base32.GetEncodedSize(_key.Length)
                    : _key.Length * 2;

                Span<char> encodedKey = stackalloc char[encodedKeySize];
                try
                {
                    ITextEncoding encoding =
                        _generateTotp
                        ? (ITextEncoding)new Base32()
                        : _yubiOtp
                            ? new ModHex()
                            : new Base16();
                    encoding.Encode(_key, encodedKey);
                    Output.Write((_generate ? "Generated " : string.Empty) + $"Key ({ encoding.GetType() }): ");
                    Output.WriteSensitive(encodedKey, OutputLevel.Quiet);
                    Output.WriteLine(string.Empty, OutputLevel.Quiet);
                }
                finally
                {
                    encodedKey.Clear();
                }
                Output.WriteLine("Mode: " + (_yubiOtp ? "Yubico OTP" : "HMAC-SHA1"), OutputLevel.Verbose);
                Output.WriteLine($"Button press: { (_button ? string.Empty : "Not ") } required");
            }

            //// if outputlevel is none or error, then no output here.
            //if (output.outputlevel > outputlevel.error)
            //{
            //    string output = string.empty;
            //    // the key will be output (if at all) in modhex, base-16, or base-32.
            //    string key = _generatetotp
            //        ? base32.encodebytes(op.getkey().span)
            //        : _yubiotp
            //            ? modhex.encodebytes(op.getkey().span)
            //            : hex.bytestohex(op.getkey().span);
            //    // if it's generated, or set to verbose, output the key.
            //    if (_generate || output.outputlevel > outputlevel.normal)
            //    {
            //        output = output.outputlevel >= outputlevel.normal
            //            ? $"key: [{ key }]."
            //            : key;
            //    }
            //    if (!string.isnullorwhitespace(output))
            //    {
            //        output.writeline(output, outputlevel.quiet);
            //    }

            //}
        }
    }
}
