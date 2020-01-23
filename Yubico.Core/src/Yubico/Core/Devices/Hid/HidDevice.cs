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
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.Hid
{
    public abstract class HidDevice : IHidDevice
    {
        public string Path { get; private set; }
        public short VendorId { get; protected set; }
        public short ProductId { get; protected set; }
        public short Usage { get; protected set; }
        public HidUsagePage UsagePage { get; protected set; }

        public static IEnumerable<HidDevice> GetHidDevices() => SdkPlatformInfo.OperatingSystem switch
        {
            SdkPlatform.Windows => WindowsHidDevice.GetList(),
            SdkPlatform.MacOS => MacOSHidDevice.GetList(),
            _ => throw new PlatformNotSupportedException()
        };

        protected HidDevice(string path)
        {
            Path = path;
        }

        public abstract IHidConnection ConnectToFeatureReports();

        public abstract IHidConnection ConnectToIOReports();
    }
}
