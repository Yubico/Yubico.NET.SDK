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
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Yubico.Core.Logging;
using Yubico.YubiKey.Fido2;

namespace Yubico.YubiKey.TestApp.Plugins
{
    internal class GregPlugin : PluginBase
    {
        public override string Name => "Greg";
        public override string Description => "A place for Greg's test code";

        public GregPlugin(IOutput output) : base(output) { }

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

            var yubiKey = YubiKeyDevice.FindAll().First();

            Thread.Sleep(3000);

            Console.WriteLine($"YubiKey Version: {yubiKey.FirmwareVersion}");

            using (var fido2 = new Fido2Session(yubiKey))
            {
                fido2.KeyCollector = data =>
                {
                    Console.WriteLine("Touch now.");
                    return true;
                };

                var info = fido2.GetAuthenticatorInfo();

                foreach (var option in info.Options!)
                {
                    Console.WriteLine($"{option.Key} = {option.Value}");
                }

                var pin = Encoding.UTF8.GetBytes("123456");
                _ = fido2.TrySetPin(pin);

                bool success = fido2.TryVerifyPin(pin, null, null, out _, out _);

                Console.WriteLine($"Verify PIN: {success}");

                byte[] clientDataHash = {
                    0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                    0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
                };

                var mcParams = new MakeCredentialParameters(
                    new RelyingParty("test-rp-id") { Name = "My RP" },
                    new UserEntity(new byte[] { 0x11, 0x22, 0x33, 0x44 }) { Name = "SomeUserName", DisplayName = "User"})
                    {
                        ClientDataHash = clientDataHash
                    };

                var mcData = fido2.MakeCredential(mcParams);

                Console.WriteLine($"Successfully made credential: {mcData.Format}");
            }

            return true;
        }
    }
}
