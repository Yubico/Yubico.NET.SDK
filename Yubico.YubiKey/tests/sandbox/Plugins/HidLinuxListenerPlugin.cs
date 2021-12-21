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
    internal class HidLinuxListenerPlugin : PluginBase
    {
        public override string Name => "HidLinuxListener";
        public override string Description => "A place for HidLinuxListener test code";

        public HidLinuxListenerPlugin(IOutput output) : base(output) { }

        public override bool Execute()
        {
            LinuxHidDevice.UdevDeviceArrival += (s, e) =>
            {
                Console.WriteLine("HID device arrived:");
                Console.WriteLine(e.Device.Path);
            
            };

            LinuxHidDevice.UdevDeviceRemoval += (s, e) =>
            {
                Console.WriteLine("HID device removed:");
                Console.WriteLine(e.Device.Path);
            };

            _ = Console.ReadLine();

            return true;
        }
    }
}
