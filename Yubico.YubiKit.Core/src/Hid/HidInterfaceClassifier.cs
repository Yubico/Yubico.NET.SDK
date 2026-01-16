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

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// Classifies HID devices based on their descriptor information.
/// Contains the authoritative mapping from HID UsagePage+Usage to YubiKey interface types.
/// </summary>
/// <remarks>
/// This is the single source of truth for determining YubiKey interface types from raw HID descriptors.
/// All UsagePage and Usage validation logic is centralized here.
/// </remarks>
public static class HidInterfaceClassifier
{
    // HID Usage Tables constants (USB-IF specification)
    private const ushort UsagePageGenericDesktop = 0x0001;
    private const ushort UsagePageFido = 0xF1D0;
    private const ushort UsageKeyboard = 0x0006;
    private const ushort UsageU2fDevice = 0x0001;

    /// <summary>
    /// Determines the YubiKey interface type from HID descriptor information.
    /// </summary>
    /// <param name="descriptorInfo">Raw HID descriptor data.</param>
    /// <returns>The classified interface type, or <see cref="YubiKeyHidInterfaceType.Unknown"/> if not recognized.</returns>
    public static YubiKeyHidInterfaceType Classify(HidDescriptorInfo descriptorInfo)
    {
        ArgumentNullException.ThrowIfNull(descriptorInfo);
        
        return (descriptorInfo.UsagePage, descriptorInfo.Usage) switch
        {
            (UsagePageFido, UsageU2fDevice) => YubiKeyHidInterfaceType.Fido,
            (UsagePageGenericDesktop, UsageKeyboard) => YubiKeyHidInterfaceType.Otp,
            _ => YubiKeyHidInterfaceType.Unknown
        };
    }
    
    /// <summary>
    /// Checks if the HID descriptor represents a supported YubiKey interface.
    /// </summary>
    /// <param name="descriptorInfo">Raw HID descriptor data.</param>
    /// <returns><c>true</c> if the interface is supported; otherwise, <c>false</c>.</returns>
    public static bool IsSupported(HidDescriptorInfo descriptorInfo)
    {
        ArgumentNullException.ThrowIfNull(descriptorInfo);
        return Classify(descriptorInfo) != YubiKeyHidInterfaceType.Unknown;
    }
    
    /// <summary>
    /// Gets the expected report communication method for an interface type.
    /// </summary>
    /// <param name="interfaceType">The YubiKey HID interface type.</param>
    /// <returns>The HID report type to use for communication.</returns>
    /// <exception cref="ArgumentException">Thrown when the interface type is not supported.</exception>
    public static HidReportType GetReportType(YubiKeyHidInterfaceType interfaceType) =>
        interfaceType switch
        {
            YubiKeyHidInterfaceType.Fido => HidReportType.InputOutput,
            YubiKeyHidInterfaceType.Otp => HidReportType.Feature,
            _ => throw new ArgumentException($"Unsupported interface type: {interfaceType}", nameof(interfaceType))
        };
}
