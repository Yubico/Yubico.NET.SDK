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

namespace Yubico.YubiKey.TestApp.Plugins
{
    internal class EventManagerPlugin : PluginBase
    {
        public override string Name => "EventManager";
        public override string Description => "A place for YubiKeyEventManager test code";

        public EventManagerPlugin(IOutput output) : base(output) { }

        public override bool Execute()
        {
            YubiKeyDevice.DeviceArrivedEvent += (s,e) =>
            {
                Console.WriteLine("YubiKey arrived:");
                Console.WriteLine(e.Device.FirmwareVersion);
                Console.WriteLine(e.Device.SerialNumber);
            };

            YubiKeyDevice.DeviceRemovedEvent += (s, e) =>
            {
                Console.WriteLine("YubiKey removed:");
                Console.WriteLine(e.Device.FirmwareVersion);
                Console.WriteLine(e.Device.SerialNumber);
            };

            _ = Console.ReadLine();

            return true;
        }
    }
}
