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
using System.Runtime.InteropServices;
using System.Text;

namespace Yubico.PlatformInterop
{
    public static class SdkPlatformInfo
    {
        public static Encoding Encoding => OperatingSystem switch
        {
            SdkPlatform.Windows => Encoding.Unicode,
            SdkPlatform.Unknown => throw new PlatformNotSupportedException(),
            _ => Encoding.UTF8
        };

        public static int CharSize => OperatingSystem switch
        {
            SdkPlatform.Windows => 2,
            SdkPlatform.Unknown => throw new PlatformNotSupportedException(),
            _ => 1
        };

        public static int DwordSize => OperatingSystem switch
        {
            SdkPlatform.Linux => 8,
            _ => 4
        };

        public static SdkPlatform OperatingSystem
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return SdkPlatform.Windows;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return SdkPlatform.MacOS;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return SdkPlatform.Linux;
                }
                else
                {
                    return SdkPlatform.Unknown;
                }
            }
        }
    }
}
