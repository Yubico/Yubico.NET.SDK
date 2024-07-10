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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Yubico.Core.Buffers;
using Yubico.YubiKey.Otp;
using Yubico.YubiKey.Otp.Operations;

namespace Yubico.YubiKey.TestApp.Plugins.Otp
{
    internal class YubiOtp : OtpPluginBase
    {
        public YubiOtp(IOutput output) : base(output)
        {
            // We're reusing ParameterUse.Generate, so we'll update the description.
            Parameters["generate"].Description = "Generate a random key. Conflicts with key.";
        }

        public override string Name => "YubiOTP";

        public override string Description => "Mimics the 'ykman yubiotp' command";

        protected override ParameterUse ParametersUsed =>
            ParameterUse.Slot
            | ParameterUse.PublicId
            | ParameterUse.PrivateId
            | ParameterUse.Key
            | ParameterUse.NoEnter
            | ParameterUse.SerialAsPublicId
            | ParameterUse.GeneratePrivateId
            | ParameterUse.Generate
            | ParameterUse.Upload
            | ParameterUse.Force;

        public override void HandleParameters()
        {
            // We'll let the base class handle populating all of the fields.
            base.HandleParameters();

            // It's better, however, for the individual plugins to decide how to
            // validate options.
            var exceptions = new List<Exception>();

            if (_serialAsPublicId)
            {
                // If we're using the serial number, then we shouldn't have a buffer
                // for the public ID yet.
                if (_publicId.Length != 0)
                {
                    exceptions.Add(
                        new InvalidOperationException("You must either specify a public ID or choose to " +
                                                      "use the YubiKey serial number as the public ID, but not both."));
                }
                // Otherwise, we'll create the buffer to receive it.
                else
                {
                    _publicId = new byte[6];
                }
            }
            else
            {
                if (_publicId.Length == 0)
                {
                    exceptions.Add(
                        new InvalidOperationException("You must either specify a public ID between" +
                                                      "one and 16 bytes or specify that the device serial number should be used " +
                                                      "as the public ID."));
                }
                else if (_upload && _publicId[0] != 0xff)
                {
                    exceptions.Add(
                        new InvalidOperationException("YubiCloud requires the public ID to begin " +
                                                      "with \"vv\" (0xff), so that condition must be true to upload."));
                }
            }

            if (_generatePrivateId)
            {
                // If we're generating the private ID, we shouldn't have a buffer
                // for it yet.
                if (_privateId.Length != 0)
                {
                    exceptions.Add(
                        new InvalidOperationException("You must either specify a private ID or choose " +
                                                      "to have a private ID generated, but not both."));
                }
                else
                {
                    _privateId = new byte[ConfigureYubicoOtp.PrivateIdentifierSize];
                }
            }
            else if (_privateId.Length == 0)
            {
                exceptions.Add(
                    new InvalidOperationException("Must must specify either a private ID or choose " +
                                                  "to have one generated."));
            }

            if (_generate)
            {
                if (_key.Length != 0)
                {
                    exceptions.Add(
                        new InvalidOperationException("You must either specify a key or choose to have a " +
                                                      "key randomly generated, but not both."));
                }
                else
                {
                    _key = new byte[ConfigureYubicoOtp.KeySize];
                }
            }
            else if (_key.Length == 0)
            {
                exceptions.Add(
                    new InvalidOperationException("You must either specify a key or choose to have a " +
                                                  "key randomly generated."));
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

            if (_key.Length < ConfigureYubicoOtp.KeySize)
            {
                _key = SafeArrayResize(_key);
            }

            if (_privateId.Length < ConfigureYubicoOtp.PrivateIdentifierSize)
            {
                _privateId = SafeArrayResize(_privateId);
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

                // Declaring these outside the _upload block so that
                // we can decide whether or not to use the result later.
                Uri? yubiOtp = null;
                if (_upload)
                {
                    yubiOtp = UploadToYubiCloud();
                }

                op.Execute();

                OutputResult();

                if (_upload)
                {
                    OpenUrl(yubiOtp!.ToString());
                }
            }
            catch (Exception ex)
            {
                throw new PluginFailureException(
                    $"Error executing OtpSession.ConfigureYubicoOtp: {ex.Message}.",
                    ex);
            }

            return true;
        }

        private ConfigureYubicoOtp GetOperation(OtpSession otp)
        {
            var op = otp.ConfigureYubicoOtp(_slot)
                .UseCurrentAccessCode((SlotAccessCode)_currentAccessCode)
                .SetNewAccessCode((SlotAccessCode)_newAccessCode)
                .AppendCarriageReturn(!_noEnter);
            op = _serialAsPublicId
                ? op.UseSerialNumberAsPublicId(_publicId)
                : op.UsePublicId(_publicId);
            op = _generatePrivateId
                ? op.GeneratePrivateId(_privateId)
                : op.UsePrivateId(_privateId);
            return _generate
                ? op.GenerateKey(_key)
                : op.UseKey(_key);
        }

        private void OutputResult()
        {
            Output.WriteLine("YubiOtp configured.");

            // Using a local function to eliminate duplication.
            // If the property is specified by the user and output level is < verbose:
            //     output nothing.
            // Else, if the output level is >= normal:
            //     output Generated (if it is generated) Full Name: Value
            // Else
            //     output NameWithoutSpaces[ValueInBrackets]
            bool PropOut(string name, Span<char> value, bool generated, bool leadWithSeparator)
            {
                try
                {
                    // If it's quiet output, then it will all be on one line.
                    // Otherwise, each property we print gets its own line.
                    var sep =
                        leadWithSeparator
                            ? Output.OutputLevel >= OutputLevel.Normal ? Eol : " "
                            : string.Empty;
                    if (Output.OutputLevel > OutputLevel.Normal
                        || (Output.OutputLevel == OutputLevel.Normal && generated))
                    {
                        Output.Write(
                            $"{sep}{(generated ? "Generated " : string.Empty)}{name}: ",
                            OutputLevel.Quiet);
                        Output.WriteSensitive(value, OutputLevel.Quiet);
                        return true;
                    }
                    else if (generated)
                    {
                        Output.Write($"{sep}{name.Replace(" ", newValue: null)}[", OutputLevel.Quiet);
                        Output.WriteSensitive(value, OutputLevel.Quiet);
                        Output.Write("]", OutputLevel.Quiet);
                        return true;
                    }

                    return leadWithSeparator;
                }
                finally
                {
                    for (var i = 0; i < value.Length; ++i)
                    {
                        value[i] = 'X';
                    }
                }
            }

            Span<char> encoded = new char[_publicId.Length * 2];
            ModHex.EncodeBytes(_publicId, encoded);
            var hasOutput = PropOut("Public ID", encoded, _serialAsPublicId, leadWithSeparator: false);
            encoded = new char[_privateId.Length * 2];
            Base16.EncodeBytes(_privateId, encoded);
            hasOutput = PropOut("Private ID", encoded, _generatePrivateId, hasOutput);
            encoded = new char[_key.Length * 2];
            Base16.EncodeBytes(_key, encoded);
            hasOutput = PropOut("Key", encoded, _generate, hasOutput);
            if (hasOutput)
            {
                Output.WriteLine(string.Empty, OutputLevel.Quiet);
            }
        }

        private Uri UploadToYubiCloud()
        {
            // Here are all the things the Yubico OTP service wants.
            var json = JsonSerializer.Serialize(new
            {
                aes_key = Base16.EncodeBytes(_key),
                serial = (_serialNumber ?? 0).ToString(),
                public_id = ModHex.EncodeBytes(_publicId),
                private_id = Base16.EncodeBytes(_privateId)
            });

            HttpResponseMessage response;
            try
            {
                response = SendRequest(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Sending credential to YubiCloud failed with error [{ex.Message}]",
                    ex);
            }

            var yubiOtp = (YubiOtpResponse)JsonSerializer.Deserialize(
                response.Content.ReadAsStringAsync().Result,
                typeof(YubiOtpResponse))!;

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errors = yubiOtp.Errors ?? Array.Empty<string>();
                    if (errors.Length == 0)
                    {
                        throw new InvalidOperationException(
                            "Upload to Yubico OTP server failed with BAD_REQUEST (no details from server).");
                    }

                    if (errors.Length == 1)
                    {
                        throw new InvalidOperationException(
                            $"Upload to Yubico OTP server failed with BAD_REQUEST ({GetYubiOtpErrors(errors).First()}).");
                    }

                    IEnumerable<Exception> exceptions = GetYubiOtpErrors(errors)
                        .Select(e => new InvalidOperationException(
                            $"Upload to Yubico OTP server failed with BAD_REQUEST ({e})"));
                    throw new AggregateException(
                        "Errors encountered uploading to Yubico OTP server. See inner exceptions for details",
                        exceptions);
                }
            }

            return yubiOtp?.FinishUrl ?? throw new InvalidOperationException(
                "The Yubico OTP server returned an invalid response.");
        }

        private static HttpResponseMessage SendRequest(string json)
        {
            var content = new StringContent(json,
                Encoding.UTF8,
                "application/json");
            using var client = new HttpClient
            {
                BaseAddress = new Uri("https://upload.yubico.com/")
            };
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("YubiKeyTestApp", "1.0"));
            return client.PostAsync("/prepare", content).Result;
        }

        private static IEnumerable<string> GetYubiOtpErrors(IEnumerable<string> errorCodes)
        {
            return errorCodes.Select(e => e switch
                {
                    "PRIVATE_ID_INVALID_LENGTH" => "Private ID must be 12 characters long.",
                    "PRIVATE_ID_NOT_HEX" => "Private ID must consist only of hex characters (0-9A-F).",
                    "PRIVATE_ID_UNDEFINED" => "Private ID is required.",
                    "PUBLIC_ID_INVALID_LENGTH" => "Public ID must be 12 characters long.",
                    "PUBLIC_ID_NOT_MODHEX" => "Public ID must consist only of modhex characters (cbdefghijklnrtuv).",
                    "PUBLIC_ID_NOT_VV" => "Public ID must begin with \"vv\".",
                    "PUBLIC_ID_OCCUPIED" => "Public ID is already in use.",
                    "PUBLIC_ID_UNDEFINED" => "Public ID is required.",
                    "SECRET_KEY_INVALID_LENGTH" => "Key must be 32 character long.",
                    "SECRET_KEY_NOT_HEX" => "Key must consist only of hex characters (0-9A-F).",
                    "SECRET_KEY_UNDEFINED" => "Key is required.",
                    "SERIAL_NOT_INT" => "Serial number must be an integer.",
                    "SERIAL_TOO_LONG" => "Serial number is too long.",
                    _ => "Undefined error from server."
                }
            );
        }

        private static void OpenUrl(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                _ = Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _ = Process.Start("open", url);
            }
            else
            {
                throw new InvalidOperationException("Default browser failed to launch.");
            }
        }

        private class YubiOtpResponse
        {
            [JsonPropertyName("finish_url")] public Uri? FinishUrl { get; set; }

            [JsonPropertyName("request_id")] public Guid? RequestId { get; set; }

            [JsonPropertyName("errors")] public string[]? Errors { get; set; }
        }
    }
}
