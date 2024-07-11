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
using System.Security.Cryptography;
using Yubico.Core.Devices.Hid;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Otp;
using Yubico.YubiKey.Otp.Commands;

namespace Yubico.YubiKey.TestApp.Plugins
{
    internal class JamiePlugin : PluginBase
    {
        public JamiePlugin(IOutput output) : base(output)
        {
            Parameters["command"].Required = true;
        }

        public override string Name => "Jamie";

        public override string Description => "A place for Jamie's test code";

        public override bool Execute()
        {
            return Command.ToLower() switch
            {
                "setstaticpassword" => SetStaticPassword(),
                "swapslots" => SwapSlots(),
                "printrandomstring" => PrintRandomBytes(),
                _ => throw new ArgumentException($"Invalid command [{Command}] specified")
            };
        }

        private bool PrintRandomBytes()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = Enumerable
                .Range(start: 0, count: 40)
                .Select(x => rng.GetByte(fromInclusive: 0, toExclusive: 0x100))
                .ToArray();
            Output.WriteLine(BitConverter.ToString(bytes));
            return true;
        }

        private bool SwapSlots()
        {
            var result = false;
            var keys = YubiKeyDevice.FindByTransport();

            if (keys.Any())
            {
                var key = keys.First();
                var connection = key.Connect(YubiKeyApplication.Otp);

                // Try getting status.
                var statusCmd = new ReadStatusCommand();
                var statusx = connection.SendCommand(statusCmd);
                var data = statusx.GetData();
                Output.WriteLine($"Data is {data}");

                var cmd = new SwapSlotsCommand();
                var response = connection.SendCommand(cmd);
                var status = response.GetData();
                Output.WriteLine(status.ToString()!);
                result = response.Status == ResponseStatus.Success;
            }

            return result;
        }

        private bool SetStaticPassword()
        {
            var result = false;
            var keys = YubiKeyDevice.FindByTransport();

            if (keys.Any())
            {
                var key = keys.First();
                var otpSession = new OtpSession(key);
                try
                {
                    var password = new char[12];
                    otpSession.ConfigureStaticPassword(Slot.ShortPress)
                        .WithKeyboard(KeyboardLayout.en_US)
                        //.SetPassword("JackiKennedy")
                        .GeneratePassword(password)
                        .Execute();
                    result = true;
                }
                catch (Exception ex)
                {
                    throw new PluginFailureException(
                        $"Error [{ex.Message}] executing command [{Command}]",
                        ex);
                }
            }

            return result;
        }
    }
}
