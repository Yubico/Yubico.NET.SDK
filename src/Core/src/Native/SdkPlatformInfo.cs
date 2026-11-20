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

using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace Yubico.YubiKit.Core.Native;

/// <summary>Exposes runtime platform detection and platform-selected helper values.</summary>
/// <remarks>Each native API still needs its own encoding and ABI contract.</remarks>
public static class SdkPlatformInfo
{
    /// <summary>Gets UTF-16 on Windows and UTF-8 on the other recognized platforms.</summary>
    public static Encoding Encoding => OperatingSystem switch
    {
        SdkPlatform.Windows => Encoding.Unicode,
        SdkPlatform.Unknown => throw new PlatformNotSupportedException(),
        _ => Encoding.UTF8
    };

    /// <summary>Gets the helper encoding's code-unit width in bytes: two on Windows, otherwise one.</summary>
    public static int CharSize => OperatingSystem switch
    {
        SdkPlatform.Windows => 2,
        SdkPlatform.Unknown => throw new PlatformNotSupportedException(),
        _ => 1
    };

    /// <summary>Gets the existing size selection: eight bytes on Linux, otherwise four.</summary>
    /// <remarks>
    ///     This is not a universal native DWORD rule. Windows DWORD is always 32 bits; Unix API typedefs
    ///     depend on the selected header and target ABI. Inspect each imported structure separately.
    /// </remarks>
    public static int DwordSize => OperatingSystem switch
    {
        SdkPlatform.Linux => 8,
        _ => 4
    };

    /// <summary>Gets the detected Windows, macOS, or Linux platform, or Unknown.</summary>
    public static SdkPlatform OperatingSystem
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return SdkPlatform.Windows;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return SdkPlatform.MacOS;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return SdkPlatform.Linux;

            return SdkPlatform.Unknown;
        }
    }

    /// <summary>Checks Windows Administrator-role membership; returns false on other platforms.</summary>
    /// <remarks>This check does not establish access to a particular HID collection or PC/SC reader.</remarks>
    public static bool IsElevated
    {
        get
        {
            if (OperatingSystem == SdkPlatform.Windows)
            {
                return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole
                    .Administrator);
            }

            return false;
        }
    }
}