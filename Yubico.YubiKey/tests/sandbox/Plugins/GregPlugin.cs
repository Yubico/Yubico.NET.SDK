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
using Serilog;
using Yubico.Core.Logging;
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.YubiHsmAuth;
using Yubico.YubiKey.YubiHsmAuth.Commands;

namespace Yubico.YubiKey.TestApp.Plugins
{
    internal class GregPlugin : PluginBase
    {
        public override string Name => "Greg";
        public override string Description => "A place for Greg's test code";

        public GregPlugin(IOutput output) : base(output) { }

        public override bool Execute()
        {
            using Serilog.Core.Logger? log = new LoggerConfiguration()
                .Enrich.With(new ThreadIdEnricher())
                .WriteTo.Console(
                    outputTemplate: "[{Level}] ({ThreadId})  {Message}{NewLine}{Exception}")
                .CreateLogger();

            Core.Logging.Log.LoggerFactory = LoggerFactory.Create(
                builder => builder
                    .AddSerilog(log)
                    .AddFilter(level => level >= LogLevel.Information));

            IYubiKeyDevice? yubiKey = YubiKeyDevice.FindAll().First();

            Console.WriteLine($"YubiKey Version: {yubiKey.FirmwareVersion}");

            using (var hsmAuth = new YubiHsmAuthSession(yubiKey))
            {
                string label = "mycred";
                byte[] password = new byte[16];
                Encoding.ASCII.GetBytes("abc123").CopyTo(password, 0);
                byte[] hostChallenge = { 0, 1, 2, 3, 4, 5, 6, 7 };
                byte[] hsmDeviceChallenge = { 8, 9, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15 };

                var command = new GetAes128SessionKeysCommand(label, password, hostChallenge, hsmDeviceChallenge);

                Console.WriteLine("Calling calculate...");
                GetAes128SessionKeysResponse? response = hsmAuth.Connection.SendCommand(command);
                Console.WriteLine($"Calculate returned with {response.Status}");
            }

            return true;
        }
    }
}
