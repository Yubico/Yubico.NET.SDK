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
using Microsoft.Extensions.Logging;
using Yubico.Core.Logging;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.TestApp.Plugins
{
    internal class GregPlugin : PluginBase
    {
        public override string Name => "Greg";
        public override string Description => "A place for Greg's test code";

        public GregPlugin(IOutput output) : base(output) { }
        public static ILogger Logger => Log.GetLogger<GregPlugin>();

        public override bool Execute()
        {
            IYubiKeyDevice? yubiKey = YubiKeyDevice.FindByTransport(Transport.All).First();

            // IYubiKeyDevice? yubiKey = YubiKeyDevice.FindByTransport(Transport.HidFido).First();

            Console.Error.WriteLine($"YubiKey Version: {yubiKey.FirmwareVersion}");
            Console.Error.WriteLine("NFC Before Value: " + yubiKey.IsNfcRestricted);

            using (var session = new PivSession(yubiKey))
            {
                Logger.LogDebug("Disconnect Yubikey");
                Console.ReadLine();
            }
            //Dispose
            Console.ReadLine();
            return true;
        }

    }


}
