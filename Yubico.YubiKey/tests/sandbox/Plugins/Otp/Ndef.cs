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
using System.Threading;
using Yubico.YubiKey.Otp;

namespace Yubico.YubiKey.TestApp.Plugins.Otp
{
    internal class Ndef : OtpPluginBase
    {
        public override string Name => "NDEF";

        public override string Description => "Configure or read a slot to be used over NDEF (NFC).";

        protected override ParameterUse ParametersUsed =>
            ParameterUse.Slot
            | ParameterUse.Text
            | ParameterUse.Force
            | ParameterUse.Uri
            | ParameterUse.Utf16
            | ParameterUse.Read;

        public Ndef(IOutput output) : base(output)
        {
            Parameters["slot"].Required = false;
        }

        public override void HandleParameters()
        {
            // We'll let the base class handle populating all of the fields.
            base.HandleParameters();

            // It's better, however, for the individual plugins to decide how to
            // validate options.
            var exceptions = new List<Exception>();

            if (_read)
            {
                if (_uri != null || !string.IsNullOrEmpty(_text))
                {
                    exceptions.Add(new InvalidOperationException(
                        "You cannot read and program NDEF tag in the same operation."));
                }
                if(_slot != Slot.None)
                {
                    exceptions.Add(new InvalidOperationException(
                        "Setting the slot is not relevant when reading an NDEF tag."));
                }
            }

            // To allow for notifying the user to touch the YubiKey to the NFC
            // reader, we'll first see if it's already there, then do some polling.
            Exception? noKeysException = null;
            try
            {
                _yubiKey = GetYubiKey(_serialNumber, Transport.NfcSmartCard);
            }
            catch (InvalidOperationException ex)
            {
                // Only continue if the error is that there are no keys found.
                if (!ex.Data.Contains("NoYubiKeys")) { throw; }
                noKeysException = ex;
            }

            if (_yubiKey is null && exceptions.Count == 0)
            {
                Output.WriteLine(
                    _serialNumber.HasValue
                        ? $"Touch YubiKey with serial number { _serialNumber.Value } to NFC reader."
                        : "Touch YubiKey to NFC reader.",
                    OutputLevel.Quiet);
                var timer = Stopwatch.StartNew();

                while (_yubiKey is null && timer.Elapsed.TotalSeconds < 10)
                {
                    Thread.Sleep(500);
                    try
                    {
                        _yubiKey = GetYubiKey(_serialNumber, Transport.NfcSmartCard);
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Only continue if the error is that there are no keys found.
                        if (!ex.Data.Contains("NoYubiKeys")) { throw; }
                        // Otherwise, we'll wait and see.
                    }
                }
                if (_yubiKey is null)
                {
                    exceptions.Add(noKeysException ?? new InvalidOperationException(
                        "No YubiKeys were found."));
                }
            }

            if (exceptions.Count > 0)
            {
                throw exceptions.Count == 1
                    ? exceptions[0]
                    : new AggregateException(
                        $"{ exceptions.Count } errors encountered.",
                        exceptions);
            }

            // I'm going to let the other parameters ride so the API can throw the errors.
        }

        public override bool Execute()
        {
            using var otp = new OtpSession(_yubiKey!);

            if (_read)
            {
                NdefDataReader reader = otp.ReadNdefTag();

                Uri uri() => reader.ToUri();
                NdefText text() => reader.ToText();

                string raw() =>
                    reader.Type == NdefDataType.Uri
                    ? uri().ToString()
                    : text().ToString();

                string labeled() =>
                    reader.Type == NdefDataType.Uri
                    ? $"URI Read: { raw() }"
                    : $"Text Read: { raw() }";

                string detailed() =>
                    reader.Type == NdefDataType.Uri
                    ? uriDetails(uri())
                    : textDetails(raw(), text().Language.Name, text().Encoding);

                string output = Output.OutputLevel switch
                {
                    OutputLevel.Quiet => raw(),
                    OutputLevel.Normal => labeled(),
                    OutputLevel.Verbose => detailed(),
                    _ => string.Empty
                };
                if (Output.OutputLevel >= OutputLevel.Quiet)
                {
                    Output.WriteLine(output, OutputLevel.Quiet);
                }
            }
            else
            {
                if (!Verify(otp))
                {
                    Output.WriteLine("Aborted.", OutputLevel.Error);
                    return false;
                }

                otp.ConfigureNdef(_slot)
                    .AsText(_text)
                    .AsUri(_uri)
                    .WithLanguage(_lcid)
                    .UseUtf16Encoding(_encoding == NdefTextEncoding.Utf16)
                    .Execute();
                if (Output.OutputLevel >= OutputLevel.Normal)
                {
                    Output.WriteLine("NDEF tag set.");
                }
                if (Output.OutputLevel >= OutputLevel.Verbose)
                {
                    Output.WriteLine(
                        _uri != null
                        ? uriDetails(_uri)
                        : textDetails(_text, _lcid, _encoding));
                }
            }
            return true;

            string uriDetails(Uri uri) =>
                string.Join(Eol, new[]
                {
                    $"URL: { uri }",
                    $"Scheme: { uri.Scheme }",
                    $"Host: { uri.Host }",
                    $"Port: { uri.Port }",
                    $"Path: { uri.AbsolutePath }"
                });
            string textDetails(string text, string lcid, NdefTextEncoding encoding) =>
                $"Text: { text + Eol }LCID: { lcid + Eol }Encoding: { encoding }";
        }
    }
}
