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

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Yubico.YubiKey.Sample.SharedCode
{
    public static class ListYubiKeys
    {
        // List all the YubiKeys. If there ar none to be found, report that.
        // Always return true, even if there are no YubiKeys. After all, this
        // method is just determining which YubiKeys are available, and finding
        // none available is a successful completion of its task.
        public static bool RunListYubiKeys(Transport transport)
        {
            var yubiKeyEnumerable = YubiKeyDevice.FindByTransport(transport);

            ReportResult(yubiKeyEnumerable);

            return true;
        }

        private static void ReportResult(IEnumerable<IYubiKeyDevice> yubiKeyEnumerable)
        {
            IEnumerable<IYubiKeyDevice> yubiKeyDevices = yubiKeyEnumerable as IYubiKeyDevice[] ?? yubiKeyEnumerable.ToArray();

            // Are there any?
            string outputList = "No YubiKeys found";
            if (yubiKeyDevices.Any())
            {
                outputList = "\n   YubiKeys:";
                foreach (var current in yubiKeyDevices)
                {
                    int serial = 0;
                    if (!(current.SerialNumber is null))
                    {
                        serial = (int)current.SerialNumber;
                    }
                    string serialNumber = serial.ToString(CultureInfo.InvariantCulture);

                    string versionNumber = current.FirmwareVersion.ToString();
                    outputList += "\n   " + serialNumber + " : " + versionNumber;
                }
                outputList += "\n";
            }

            SampleMenu.WriteMessage(MessageType.Special, 0, outputList);
        }
    }
}
