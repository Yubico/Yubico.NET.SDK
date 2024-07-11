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
using System.Security.Cryptography;
using Yubico.Core.Devices.Hid;
using Yubico.YubiKey.Otp;
using Yubico.YubiKey.Otp.Commands;
using Yubico.YubiKey.TestApp.Converters;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.TestApp.Plugins.Otp
{
    internal abstract class OtpPluginBase : PluginBase
    {
        public OtpPluginBase(IOutput output) : base(output)
        {
            // First, add any converters that are not in the base class.
            Converters[typeof(KeyboardLayout)] = (s) => StaticConverters.ParseEnum<KeyboardLayout>(s.Replace('-', '_'));
            Converters[typeof(Slot)] = (s) => StaticConverters.ParseEnum<Slot>(s);

            // Next, we'll build the parameter collection by enumerating
            // the child class' ParametersUsed bitfield.
            try
            {
                Parameters =
                    ((ParameterUse[])Enum.GetValues(typeof(ParameterUse)))
                    .Where(p => (ParametersUsed & p) != ParameterUse.None)
                    .Select(p => p switch
                    {
                        ParameterUse.Slot => new KeyValuePair<string, Parameter>(
                            "slot",
                            new Parameter
                            {
                                Name = "Slot",
                                Shortcut = "s",
                                Description = "The slot to configure (1 or 2), or (ShortPress or LongPress)",
                                Type = typeof(Slot),
                                Required = true
                            }),
                        ParameterUse.Force => new KeyValuePair<string, Parameter>(
                            "force",
                            new Parameter
                            {
                                Name = "Force",
                                Shortcut = "f",
                                Description = "Confirm the action without prompting.",
                                Type = typeof(bool)
                            }),
                        ParameterUse.Generate => new KeyValuePair<string, Parameter>(
                            "generate",
                            new Parameter
                            {
                                Name = "Generate",
                                Shortcut = "g",
                                Description = "Generate a random password.",
                                Type = typeof(bool)
                            }),
                        ParameterUse.Password => new KeyValuePair<string, Parameter>(
                            "password",
                            new Parameter
                            {
                                Name = "Password",
                                Shortcut = "p",
                                Description = "The static password to set. " +
                                              "Note: This option is mutually exclusive to 'generate'.",
                                Type = typeof(string)
                            }),
                        ParameterUse.Length => new KeyValuePair<string, Parameter>(
                            "length",
                            new Parameter
                            {
                                Name = "Length",
                                Shortcut = "l",
                                Description = "Length of generated password. [default: 38]",
                                Type = typeof(int)
                            }),
                        ParameterUse.Keyboard => new KeyValuePair<string, Parameter>(
                            "keyboard",
                            new Parameter
                            {
                                Name = "Keyboard",
                                Shortcut = "kb",
                                Description = "Keyboard layout to use for the static password. " +
                                              $@"Choices are {string.Join(',', Enum.GetNames(typeof(KeyboardLayout)))} [default: ModHex]",
                                Type = typeof(KeyboardLayout)
                            }),
                        ParameterUse.CurrentAccessCode => new KeyValuePair<string, Parameter>(
                            "current-access-code",
                            new Parameter
                            {
                                Name = "Current-Access-Code",
                                Shortcut = "ca",
                                Description = "Current access code protecting this OTP slot. This is " +
                                              "specified as a Base16 (hex) string. The access code is six bytes. " +
                                              "If you specify less than six bytes, it will be padded with zeros. " +
                                              "If you specify more than six bytes, an error will be shown. Note that " +
                                              "if you specify a current access code, but not a new access code, the " +
                                              "protection will be removed from the slot",
                                Type = typeof(Base16Bytes)
                            }),
                        ParameterUse.NewAccessCode => new KeyValuePair<string, Parameter>(
                            "new-access-code",
                            new Parameter
                            {
                                Name = "New-Access-Code",
                                Shortcut = "na",
                                Description = "New access-code to protect this slot. If there is currently " +
                                              "an access code, it must also be specified. Default is six zero bytes, " +
                                              "which equates to no access-code.",
                                Type = typeof(Base16Bytes)
                            }),
                        ParameterUse.NoEnter => new KeyValuePair<string, Parameter>(
                            "no-enter",
                            new Parameter
                            {
                                Name = "No-Enter",
                                Shortcut = "ne",
                                Description = "Don't send an Enter keystroke after outputting the password.",
                                Type = typeof(bool)
                            }),
                        ParameterUse.PublicId => new KeyValuePair<string, Parameter>(
                            "publicid",
                            new Parameter
                            {
                                Name = "PublicId",
                                Shortcut = "pui",
                                Description = "Public identifier prefix. Enter as a ModHex string.",
                                Type = typeof(ModHexBytes)
                            }),
                        ParameterUse.PrivateId => new KeyValuePair<string, Parameter>(
                            "privateid",
                            new Parameter
                            {
                                Name = "PrivateId",
                                Shortcut = "pri",
                                Description = "Private identifier. This is specified as a Base16 (hex) " +
                                              "string. If you specify less than six bytes, it will be padded at " +
                                              "the end with zeros.",
                                Type = typeof(Base16Bytes)
                            }),
                        ParameterUse.Key => new KeyValuePair<string, Parameter>(
                            "key",
                            new Parameter
                            {
                                Name = "Key",
                                Shortcut = "k",
                                Description = "Decryption key. This is specified as a Base16 (hex) " +
                                              "string. The length depends on the algorithm type used. For HMAC, " +
                                              "it's twenty bytes. For Yubico OTP, it's sixteen bytes. If you " +
                                              "specify a key that is shorter than what is required, it will be " +
                                              "padded at the end with zeros. If you specify too long of a key, " +
                                              "an error will be shown.",
                                Type = typeof(Base16Bytes)
                            }),
                        ParameterUse.TotpKey => new KeyValuePair<string, Parameter>(
                            "totp-key",
                            new Parameter
                            {
                                Name = "TOTP-Key",
                                Shortcut = "tk",
                                Description = "Key, encoded in Base32. The result of setting this " +
                                              "parameter is the same as setting the non TOTP key parameter. " +
                                              "The only difference is that this parameter accepts Base32 " +
                                              "instead of Base16 (hex). If you specify both this parameter " +
                                              "and \"key\", an error will occur. By itself, this does not put " +
                                              "anything in TOTP mode. It simply specifies a Base32 key.",
                                Type = typeof(Base32Bytes)
                            }),
                        ParameterUse.SerialAsPublicId => new KeyValuePair<string, Parameter>(
                            "serialaspublicid",
                            new Parameter
                            {
                                Name = "SerialAsPublicId",
                                Shortcut = "sp",
                                Description = "Use YubiKey serial number as public ID. Conflicts with publicid.",
                                Type = typeof(bool)
                            }),
                        ParameterUse.GeneratePrivateId => new KeyValuePair<string, Parameter>(
                            "generateprivateid",
                            new Parameter
                            {
                                Name = "GeneratePrivateId",
                                Shortcut = "gp",
                                Description = "Generate a random private id. Conflicts with privateid.",
                                Type = typeof(bool)
                            }),
                        ParameterUse.Upload => new KeyValuePair<string, Parameter>(
                            "upload",
                            new Parameter
                            {
                                Name = "Upload",
                                Shortcut = "u",
                                Description = "Upload credential to YubiCloud (opens in browser). " +
                                              "Conflicts with force.",
                                Type = typeof(bool)
                            }),
                        ParameterUse.Totp => new KeyValuePair<string, Parameter>(
                            "totp",
                            new Parameter
                            {
                                Name = "TOTP",
                                Shortcut = "t",
                                Description = "Generate a TOTP code, use the current time if " +
                                              "challenge is omitted.",
                                Type = typeof(bool)
                            }),
                        ParameterUse.Digits => new KeyValuePair<string, Parameter>(
                            "digits",
                            new Parameter
                            {
                                Name = "Digits",
                                Shortcut = "d",
                                Description = "[6|8] Number of digits in generated OTP code (default: 6).",
                                Type = typeof(int)
                            }),
                        ParameterUse.Challenge => new KeyValuePair<string, Parameter>(
                            "challenge",
                            new Parameter
                            {
                                Name = "Challenge",
                                Shortcut = "c",
                                Description = "A hex string to calculate the response for.",
                                Type = typeof(Base16Bytes)
                            }),
                        ParameterUse.Button => new KeyValuePair<string, Parameter>(
                            "button",
                            new Parameter
                            {
                                Name = "Button",
                                Shortcut = "b",
                                Description = "Require the user to touch the YubiKey button to " +
                                              "perform a challenge-response calculation.",
                                Type = typeof(bool)
                            }),
                        ParameterUse.IMF => new KeyValuePair<string, Parameter>(
                            "initialmovingfactor",
                            new Parameter
                            {
                                Name = "InitialMovingFactor",
                                Shortcut = "imf",
                                Description = "Initial moving factor. This is the initial counter " +
                                              "value for the YubiKey. This should be a value between 0 and " +
                                              "1048560, evenly dividable by 16.",
                                Type = typeof(int)
                            }),
                        ParameterUse.Prefix => new KeyValuePair<string, Parameter>(
                            "prefix",
                            new Parameter
                            {
                                Name = "Prefix",
                                Shortcut = "pf",
                                Description = "Added before the NDEF payload. Typically a URI.",
                                Type = typeof(string)
                            }),
                        ParameterUse.YubiOtp => new KeyValuePair<string, Parameter>(
                            "yubiotp",
                            new Parameter
                            {
                                Name = "YubiOtp",
                                Shortcut = "y",
                                Description = "Use the Yubico OTP Algorithm for challenge response.",
                                Type = typeof(bool)
                            }),
                        ParameterUse.ShortHMAC => new KeyValuePair<string, Parameter>(
                            "shortchallenge",
                            new Parameter
                            {
                                Name = "ShortChallenge",
                                Shortcut = "sc",
                                Description = "Use HMAC challenges that are shorter than 64 bytes.",
                                Type = typeof(bool)
                            }),
                        ParameterUse.Text => new KeyValuePair<string, Parameter>(
                            "text",
                            new Parameter
                            {
                                Name = "Text",
                                Shortcut = "txt",
                                Description = "Text string to program the NDEF device to emit. This " +
                                              "parameter is not compatible with NDEF-URI.",
                                Type = typeof(string)
                            }),
                        ParameterUse.Uri => new KeyValuePair<string, Parameter>(
                            "uri",
                            new Parameter
                            {
                                Name = "URI",
                                Description = "URI to program the NDEF device to emit. This parameter" +
                                              "is not compatible with NDEF-Text or NDEF-UTF16.",
                                Type = typeof(Uri)
                            }),
                        ParameterUse.Utf16 => new KeyValuePair<string, Parameter>(
                            "utf16",
                            new Parameter
                            {
                                Name = "UTF16",
                                Shortcut = "u16",
                                Description = "Program text for NDEF tag encoded as UTF-16. Note that " +
                                              "this is not a common setting to use, and is only recommended if you're " +
                                              "certain that your application calls for it.",
                                Type = typeof(bool)
                            }),
                        ParameterUse.Read => new KeyValuePair<string, Parameter>(
                            "read",
                            new Parameter
                            {
                                Name = "Read",
                                Shortcut = "r",
                                Description = "Read the data stored in the NDEF device. If this " +
                                              "parameter is specified, no other NDEF-specific ones can be. You can " +
                                              "either program an NDEF device or read from it, but not both. Also, " +
                                              "you cannot specify a slot when you're reading. Slot is only used to " +
                                              "program the NDEF configuration.",
                                Type = typeof(bool)
                            }),
                        ParameterUse.LanguageId => new KeyValuePair<string, Parameter>(
                            "languageid",
                            new Parameter
                            {
                                Name = "LanguageID",
                                Shortcut = "lcid",
                                Description = "Language Code Identifier (LCID) for the string to be " +
                                              "used to program the NDEF device. This setting is not compatible with " +
                                              "the URI parameter, since URIs are language neutral.",
                                Type = typeof(string)
                            }),
                        _ => throw new InvalidOperationException(
                            $"Invalid value [{p}] in {GetType().Name}.ParametersUsed collection.")
                    })
                    // For now, I'll assume that this parameter will be available to all OTP plugins.
                    .Append(new KeyValuePair<string, Parameter>(
                        "serial-number",
                        new Parameter
                        {
                            Name = "Serial-Number",
                            Shortcut = "sn",
                            Description = "Serial number of the YubiKey to program. You can omit this if " +
                                          "there is only one plugged-in. However, if you have multiple, then you " +
                                          "must either specify one or remove others.",
                            Type = typeof(int)
                        }))
                    .ToDictionary(p => p.Key, p => p.Value);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException(
                    "Error building parameters object. Likely problem is incompatible "
                    + $"parameters specified in class [{GetType().Name}].",
                    ex);
            }
        }

        public override Dictionary<string, Parameter> Parameters { get; }

        /// <summary>
        /// Specify this in your class to determine which parameters you need.
        /// </summary>
        /// <remarks>
        /// The constructor will enumerate all of the parameters you specified
        /// and populate the <see cref="Parameters"/> collection.
        /// </remarks>
        protected abstract ParameterUse ParametersUsed { get; }

        public override void HandleParameters()
        {
            // Not calling base.HandleParameters() here because we overrode the Parameters object.

            // Setting up the protected fields that plug-ins inheriting from this
            // base class will use.
            _serialNumber = (int?)GetParameter("serial-number");
            _slot = (Slot?)GetParameter("slot") ?? Slot.None;
            _force = (bool?)GetParameter("force") ?? false;
            _generate = (bool?)GetParameter("generate") ?? false;
            _password = (string?)GetParameter("password") ?? string.Empty;
            _passwordLength = (int?)GetParameter("length") ?? 0;
            _keyboard = (KeyboardLayout?)GetParameter("keyboard") ?? KeyboardLayout.ModHex;
            _currentAccessCode = ((BytesBase?)GetParameter("current-access-code"))?.Value ?? Array.Empty<byte>();
            _newAccessCode = ((BytesBase?)GetParameter("new-access-code"))?.Value ?? Array.Empty<byte>();
            _noEnter = (bool?)GetParameter("no-enter") ?? false;
            _publicId = ((BytesBase?)GetParameter("publicid"))?.Value ?? Array.Empty<byte>();
            _privateId = ((BytesBase?)GetParameter("privateid"))?.Value ?? Array.Empty<byte>();
            _key = ((BytesBase?)GetParameter("key"))?.Value ?? Array.Empty<byte>();
            _totpkey = ((BytesBase?)GetParameter("totp-key"))?.Value ?? Array.Empty<byte>();
            _serialAsPublicId = (bool?)GetParameter("serialaspublicid") ?? false;
            _generatePrivateId = (bool?)GetParameter("generateprivateid") ?? false;
            _generateTotp = (bool?)GetParameter("totp") ?? false;
            _upload = (bool?)GetParameter("upload") ?? false;
            _challenge = ((BytesBase?)GetParameter("challenge"))?.Value ?? Array.Empty<byte>();
            _prefix = (string?)GetParameter("prefix") ?? string.Empty;
            _yubiOtp = (bool?)GetParameter("yubiotp") ?? false;
            _digits = (int?)GetParameter("digits") ?? (_generateTotp && !_yubiOtp ? 6 : (int?)null);
            _useShortChallenge = (bool?)GetParameter("shortchallenge") ?? false;
            _button = (bool?)GetParameter("button") ?? false;
            _text = (string?)GetParameter("text") ?? string.Empty;
            _uri = (Uri?)GetParameter("uri");
            _encoding = (bool?)GetParameter("utf16") ?? false ? NdefTextEncoding.Utf16 : NdefTextEncoding.Utf8;
            _read = (bool?)GetParameter("read") ?? false;
            _lcid = (string?)GetParameter("languageid") ?? string.Empty;
            _imf = (int?)GetParameter("initialmovingfactor") ?? 0;
            _text = (string?)GetParameter("text") ?? string.Empty;
            _uri = (Uri?)GetParameter("uri");
            _encoding = (bool?)GetParameter("utf16") ?? false ? NdefTextEncoding.Utf16 : NdefTextEncoding.Utf8;
            _read = (bool?)GetParameter("read") ?? false;
            _lcid = (string?)GetParameter("languageid") ?? string.Empty;

            object? GetParameter(string name)
            {
                if (Parameters.TryGetValue(name, out Parameter? param))
                {
                    return param.Value;
                }

                return null;
            }
        }

        internal static byte[] SafeArrayResize(byte[] array)
        {
            byte[] replacement = new byte[array.Length];
            array.CopyTo(replacement, 0);
            CryptographicOperations.ZeroMemory(array);
            return replacement;
        }

        internal static IYubiKeyDevice GetYubiKey(int? serialNumber, Transport transport = Transport.HidKeyboard)
        {
            IEnumerable<IYubiKeyDevice> keys = YubiKeyDevice.FindByTransport(transport);
            IYubiKeyDevice key = keys.FirstOrDefault() ?? throw new InvalidOperationException();
            if (serialNumber.HasValue)
            {
                key = keys
                    .Where(k => k.SerialNumber == serialNumber)
                    .FirstOrDefault() ?? throw new InvalidOperationException();
                if (key is null)
                {
                    string message = "YubiKey with serial number {0} was not found.{1}";
                    string subMessage = string.Empty;
                    if (keys.Any())
                    {
                        if (keys.Skip(1).Any())
                        {
                            string keystr = string.Join(", ", keys.Select(k => k.SerialNumber));
                            subMessage = $" Keys found: [{keystr}].";
                        }
                        else
                        {
                            subMessage = $" Key found: [{keys.First().SerialNumber}].";
                        }
                    }

                    string exText = string.Format(message, serialNumber, subMessage);
                    throw new InvalidOperationException(exText);
                }
            }
            else if (keys.Skip(1).Any())
            {
                throw new InvalidOperationException(
                    "More than one YubiKey was found. You must remove extra keys " +
                    "or specify one by serial number.");
            }
            else if (key is null)
            {
                var ex = new InvalidOperationException("No YubiKeys were found on your system.");
                ex.Data["NoYubiKeys"] = true;
                throw ex;
            }

            return key!;
        }

        protected bool Verify(OtpSession otp, string? message = null)
        {
            bool verify =
                _slot == Slot.ShortPress
                    ? otp.IsShortPressConfigured
                    : otp.IsLongPressConfigured;
            if (verify && !_force)
            {
                // For now, we're going to assume that "quiet" means that the
                // user doesn't want to be prompted.
                if (Output.OutputLevel >= OutputLevel.Normal)
                {
                    message ??= $"Slot[{_slot}] is already programmed. {Eol}" +
                                "Type \"Yes\" and press [Enter] to overwrite";
                    // This is an exception to the "Always use Output" rule.
                    // Outputing a prompt to type "yes" to a file would be worse
                    // than useless.
                    Console.WriteLine(message);
                    if (Console.ReadLine()?.ToLower() != "yes")
                    {
                        Output.WriteLine("Aborted.", OutputLevel.Error);
                        return false;
                    }
                }
                else
                {
                    Output.WriteLine(
                        $"Slot[{_slot}] is already programmed." + Eol +
                        "Either select the [-force] option, or don't select [-quiet] to be prompted.",
                        OutputLevel.Error);
                }
            }

            return true;
        }

        #region Common Protected Fields

        protected Slot _slot;
        protected bool _generate;
        protected int _passwordLength;
        protected IYubiKeyDevice? _yubiKey;
        protected int? _serialNumber;
        protected bool _force;
        protected byte[] _currentAccessCode = new byte[SlotConfigureBase.AccessCodeLength];
        protected byte[] _newAccessCode = new byte[SlotConfigureBase.AccessCodeLength];
        protected bool _noEnter;
        protected KeyboardLayout _keyboard;
        protected string _password = string.Empty;
        protected byte[] _publicId = Array.Empty<byte>();
        protected byte[] _privateId = Array.Empty<byte>();
        protected byte[] _key = Array.Empty<byte>();
        protected byte[] _totpkey = Array.Empty<byte>();
        protected byte[] _challenge = Array.Empty<byte>();
        protected bool _serialAsPublicId;
        protected bool _generatePrivateId;
        protected bool _generateTotp;
        protected bool _upload;
        protected bool _useShortChallenge;
        protected int? _digits;
        protected string _prefix = string.Empty;
        protected bool _yubiOtp;
        protected bool _button;
        protected int _imf;
        protected string _text = string.Empty;
        protected Uri? _uri;
        protected NdefTextEncoding _encoding;
        protected bool _read;
        protected string _lcid = string.Empty;

        #endregion

        [Flags]
        protected enum ParameterUse
        {
            None = 0,
            Slot = 0b1 << 0,
            Force = 0b1 << 1,
            NoEnter = 0b1 << 2,
            CurrentAccessCode = 0b1 << 3,
            NewAccessCode = 0b1 << 4,
            Generate = 0b1 << 5,
            Password = 0b1 << 6,
            Length = 0b1 << 7,
            SerialNumber = 0b1 << 8,
            Keyboard = 0b1 << 9,
            PublicId = 0b1 << 10,
            PrivateId = 0b1 << 11,
            Key = 0b1 << 12,
            SerialAsPublicId = 0b1 << 13,
            GeneratePrivateId = 0b1 << 14,
            Totp = 0b1 << 15,
            Digits = 0b1 << 16,
            Challenge = 0b1 << 17,
            Button = 0b1 << 18,
            IMF = 0b1 << 19,
            Prefix = 0b1 << 20,
            YubiOtp = 0b1 << 21,
            Upload = 0b1 << 22,
            ShortHMAC = 0b1 << 23,
            TotpKey = 0b1 << 24,
            Text = 0b1 << 25,
            Uri = 0b1 << 26,
            Utf16 = 0b1 << 27,
            Read = 0b1 << 28,
            LanguageId = 0b1 << 29
        }
    }
}
