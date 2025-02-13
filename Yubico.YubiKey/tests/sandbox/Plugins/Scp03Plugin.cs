// Copyright 2025 Yubico AB
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
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.Scp;
using Yubico.YubiKey.Scp03;

namespace Yubico.YubiKey.TestApp.Plugins
{
    internal class Scp03Plugin : PluginBase
    {
        public override string Name => "Scp03";

        public override string Description => "A test for SCP03-related stuff";

        public Scp03Plugin(IOutput output) : base(output)
        {
            Parameters["command"].Required = true;
        }

        public override bool Execute()
        {
            return Command.ToLower() switch
            {
                "e2e" => BasicE2ETest(),
                _ => throw new ArgumentException($"Invalid command [{Command}] specified")
            };
        }

        private bool BasicE2ETest()
        {
            IEnumerable<IYubiKeyDevice> keys = YubiKeyDevice.FindByTransport(Transport.UsbSmartCard);
            IYubiKeyDevice device = keys.Single()!;

            using var piv = new PivSession(device, Scp03KeyParameters.DefaultKey);
            bool result = piv.TryVerifyPin(new ReadOnlyMemory<byte>(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 }), out _);
            Output.WriteLine($"pin 123456: {result}");

            PivMetadata metadata = piv.GetMetadata(PivSlot.Pin)!;
            Output.WriteLine($"retries: {metadata.RetryCount}");

            return true;
        }
    }
}
