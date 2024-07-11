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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.TestApp.Plugins
{
    internal class EnumeratePlugin : PluginBase
    {
        public override string Name => "Enumeration";

        public override string Description =>
            "This plugin displays different types of YubiKeys, both in a scripted and interactive way.";

        public EnumeratePlugin(IOutput output) : base(output)
        {
            Parameters["command"].Description = "[transport] The type of YubiKey transport to enumerate. "
                                                + "Current valid transports are All, HidKeyboard, HidFido, UsbSmartCard, NfcSmartCard, "
                                                + "and AllSmartCard. If this is not specified, interactive is assumed.";
            Parameters["interactive"] = new Parameter
            {
                Name = "Interactive",
                Shortcut = "i",
                Description = "This mode prompts the user to select the YubiKey transport to "
                              + "enumerate. If Command is also specified, it is executed first.",
                Type = typeof(bool),
                Required = false
            };
        }

        public override bool Execute()
        {
            bool result = Command.ToLower() switch
            {
                "all" => OutputDevices(Transport.All),
                "hidkeyboard" => OutputDevices(Transport.HidKeyboard),
                "hidfido" => OutputDevices(Transport.HidFido),
                "usbsmartcard" => OutputDevices(Transport.UsbSmartCard),
                "nfcsmartcard" => OutputDevices(Transport.NfcSmartCard),
                "allsmartcard" => OutputDevices(Transport.SmartCard),
                "" => Interactive(),
                _ => throw GetArgumentException(Command)
            };

            // If interactive was specified on the command line and it hasn't
            // already been done (as a result of no command being specified),
            // then run interactive mode.
            return _interactive && Command != ""
                ? Interactive()
                : result;
        }

        private static ArgumentException GetArgumentException(string command)
        {
            return new ArgumentException(string.Join(Eol, new[]
            {
                $"[{command}] is not valid. Valid commands are:",
                "  All", "  HidKeyboard", "  HidFido", "  UsbSmartCard",
                "  NfcSmartCard", "  AllSmartCard"
            }));
        }

        private bool OutputDevices(Transport transport)
        {
            IList<IYubiKeyDevice> keys = IntegrationTestDeviceEnumeration.GetTestDevices(transport);
            if (keys.Count == 0)
            {
                Output.WriteLine($"No keys found of type [{transport}]");
                return false;
            }

            for (int i = 0; i < keys.Count; ++i)
            {
                Output.WriteLine($"{Eol}YubiKey # {i + 1}{Eol + keys[i]}");
                Output.WriteLine(new string('-', ConsoleWidth - 1));
            }

            Output.WriteLine(new string('=', ConsoleWidth - 1));
            Output.Write(Eol + Eol);

            return true;
        }

        public bool Interactive()
        {
            //
            // Enumerate all YubiKeys
            //
            char inputChar;
            do
            {
                Output.WriteLine($"YubiKey Enumeration Options");
                Output.WriteLine($"1. All keys");
                Output.WriteLine($"2. HID Keyboard");
                Output.WriteLine($"3. HID FIDO");
                Output.WriteLine($"4. USB SmartCard");
                Output.WriteLine($"5. NFC SmartCard");
                Output.WriteLine($"6. All SmartCard");
                Output.WriteLine($"");
                Output.Write($"Select an option, or any other key to exit: ");
                inputChar = Console.ReadKey().KeyChar;
                Output.WriteLine();

                Transport transport = inputChar switch
                {
                    '1' => Transport.All,
                    '2' => Transport.HidKeyboard,
                    '3' => Transport.HidFido,
                    '4' => Transport.UsbSmartCard,
                    '5' => Transport.NfcSmartCard,
                    '6' => Transport.SmartCard,
                    _ => Transport.None,
                };

                if (transport == Transport.None)
                {
                    Output.WriteLine(Eol + "Exiting...");
                    return true;
                }

                Output.WriteLine(Eol + "Getting keys...");
                _ = OutputDevices(transport);
            } while (inputChar >= '1' && inputChar <= '6');

            return true;
        }

        public override void HandleParameters()
        {
            base.HandleParameters();
            string? interactive = (string?)Parameters["interactive"].Value;
            _interactive = interactive != null
                           && StaticConverters.ParseBool(interactive);
        }

        private bool _interactive;
    }
}
