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
using Yubico.Core.Devices.Hid;

namespace Yubico.YubiKey.TestApp.Plugins
{
    internal class HidDeviceListenerPlugin : PluginBase
    {
        public HidDeviceListenerPlugin(IOutput output) : base(output)
        {
        }

        public override string Name => "HidDeviceListener";
        public override string Description => "A test plugin that demonstrates HID events";

        public override bool Execute()
        {
            Console.WriteLine("Create listener");
            var listener = HidDeviceListener.Create();

            listener.Arrived += (sender, args) => Console.WriteLine("Device arrived!");
            listener.Removed += (sender, args) => Console.WriteLine("Device removed!");
            Console.WriteLine("Subscribed");

            _ = Console.ReadLine();
            Console.WriteLine("Done");
            return true;
        }
    }
}
