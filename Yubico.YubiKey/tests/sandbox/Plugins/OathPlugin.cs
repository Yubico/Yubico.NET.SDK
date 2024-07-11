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
using System.Threading;
using Microsoft.Extensions.Logging;
using Yubico.Core.Logging;
using Yubico.YubiKey.Oath;
using Yubico.YubiKey.Oath.Commands;

namespace Yubico.YubiKey.TestApp.Plugins
{
    // Use Authenticator Test (https://rootprojects.org/authenticator/) to test OTP values.
    internal class OathPlugin : PluginBase
    {
        public override string Name => "OATH";
        public override string Description => "OATH credential calculation";

        public OathPlugin(IOutput output) : base(output) { }

        public override bool Execute()
        {
            Log.LoggerFactory = LoggerFactory.Create(
                builder => builder.AddSimpleConsole(
                        options =>
                        {
                            options.IncludeScopes = true;
                            options.SingleLine = true;
                            options.TimestampFormat = "hh:mm:ss";
                        })
                    .AddFilter(level => level >= LogLevel.Information));

            IEnumerable<IYubiKeyDevice> keys = YubiKeyDevice.FindAll();
            IYubiKeyDevice? yubiKey = keys.First();


            using var oathSession = new OathSession(yubiKey);

            // Copy URI string from Authenticator Test console and pass here.
            var uri = new Uri("otpauth://totp/ACME%20Co:john@example.com?secret=23A3DQA6AB6CAQDKWQOHN4HGHBWASHX6&issuer=ACME%20Co&algorithm=SHA1&digits=6&period=30");
            var credential = Credential.ParseUri(uri);

            oathSession.AddCredential(credential);
            Code otp = oathSession.CalculateCredential(credential);

            // Verify OTP value the the value in Authenticator Test.
            Console.WriteLine($"OTP value: {otp.Value}");

            return true;
        }
    }
}
