// Copyright 2022 Yubico AB
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
using Yubico.YubiKey.YubiHsmAuth;

namespace Yubico.YubiKey.TestApp.Plugins
{
    internal class DavidPlugin : PluginBase
    {
        public override string Name => "David";

        public override string Description => "A place for David's test code";

        public DavidPlugin(IOutput output) : base(output)
        {
            Parameters["command"].Required = true;
        }

        public override bool Execute()
        {
            return Command.ToLower() switch
            {
                "connectyha" => ConnectYubiHsmAuth(),
                _ => throw new ArgumentException($"Invalid command [{ Command }] specified")
            };
        }

        private bool ConnectYubiHsmAuth()
        {
            bool result = default;
            IEnumerable<IYubiKeyDevice> keys = YubiKeyDevice.FindByTransport(Transport.All);

            if (keys.Any())
            {
                foreach (IYubiKeyDevice device in keys)
                {
                    Output.WriteLine($"Using YubiKey v{device.FirmwareVersion} S/N {device.SerialNumber}...");

                    bool yubiHsmAuthCapable = device.HasFeature(YubiKeyFeature.YubiHsmAuthApplication);
                    bool yubiHsmAuthEnabled = device.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.YubiHsmAuth);

                    Output.WriteLine($"YubiHSM Auth app, has feature: {yubiHsmAuthCapable}");
                    Output.WriteLine($"YubiHSM Auth app, is enabled: {yubiHsmAuthEnabled}");

                    result = yubiHsmAuthEnabled ? device.TryConnect(YubiKeyApplication.YubiHsmAuth, out _) : false;

                    if (result)
                    {
                        Output.WriteLine($"Successfully connected to YubiHSM Auth");
                    }
                    else
                    {
                        Output.WriteLine($"Failed to connect to YubiHSM Auth");
                    }

                    Output.WriteLine();
                }
            }

            return result;
        }
    }
}
