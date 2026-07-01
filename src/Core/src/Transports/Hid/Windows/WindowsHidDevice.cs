// Copyright 2026 Yubico AB
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

using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Native.Windows.Cfgmgr32;
using Cfgmgr32NativeMethods = Yubico.YubiKit.Core.Native.Windows.Cfgmgr32.NativeMethods;

namespace Yubico.YubiKit.Core.Transports.Hid.Windows;

/// <summary>
/// Windows implementation of a Human Interface Device (HID).
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsHidDevice : IHidDevice
{
    private static readonly ILogger<WindowsHidDevice> Logger = YubiKitLogging.CreateLogger<WindowsHidDevice>();

    private readonly string _devicePath;

    private WindowsHidDevice(HidDescriptorInfo descriptorInfo)
    {
        DescriptorInfo = descriptorInfo;
        InterfaceType = HidInterfaceClassifier.Classify(descriptorInfo);
        _devicePath = descriptorInfo.DevicePath;
    }

    public string ReaderName => _devicePath;

    public HidDescriptorInfo DescriptorInfo { get; }

    public HidInterfaceType InterfaceType { get; }

    public static IReadOnlyList<IHidDevice> GetList()
    {
        var devices = new List<IHidDevice>();

        foreach (var interfacePath in CmDevice.GetDevicePaths(CmInterfaceGuid.Hid, null))
        {
            if (string.IsNullOrEmpty(interfacePath))
                continue;

            try
            {
                var descriptorInfo = GetDescriptorInfo(interfacePath);

                if (descriptorInfo.VendorId == HidConstants.YubicoVendorId &&
                    HidInterfaceClassifier.IsSupported(descriptorInfo))
                {
                    devices.Add(new WindowsHidDevice(descriptorInfo));
                }
            }
            catch (PlatformApiException ex)
            {
                Logger.LogDebug(ex, "Skipping HID interface {InterfacePath}: ConfigMgr property read failed", interfacePath);
                // Ignore inaccessible or transient HID interfaces; one bad device must not abort discovery.
            }
            catch (NotSupportedException ex)
            {
                Logger.LogDebug(ex, "Skipping HID interface {InterfacePath}: unsupported or malformed ConfigMgr HID properties", interfacePath);
                // Ignore HID interfaces with unsupported/malformed ConfigMgr properties.
            }
        }

        return devices;
    }

    private static HidDescriptorInfo GetDescriptorInfo(string interfacePath) =>
        new()
        {
            UsagePage = GetInterfaceProperty<ushort>(interfacePath, Cfgmgr32NativeMethods.DEVPKEY_DeviceInterface_HID_UsagePage),
            Usage = GetInterfaceProperty<ushort>(interfacePath, Cfgmgr32NativeMethods.DEVPKEY_DeviceInterface_HID_UsageId),
            DevicePath = interfacePath,
            VendorId = unchecked((short)GetInterfaceProperty<ushort>(interfacePath, Cfgmgr32NativeMethods.DEVPKEY_DeviceInterface_HID_VendorId)),
            ProductId = unchecked((short)GetInterfaceProperty<ushort>(interfacePath, Cfgmgr32NativeMethods.DEVPKEY_DeviceInterface_HID_ProductId))
        };

    private static T GetInterfaceProperty<T>(string interfacePath, Cfgmgr32NativeMethods.DEVPROPKEY propertyKey)
    {
        var value = CmPropertyAccessHelper.TryGetProperty(
            Cfgmgr32NativeMethods.CM_Get_Device_Interface_Property,
            interfacePath,
            propertyKey);

        return value is T typed
            ? typed
            : throw new NotSupportedException($"Missing or invalid HID interface property {propertyKey}.");
    }

    public IHidConnection ConnectToFeatureReports() =>
        new WindowsHidFeatureReportConnection(_devicePath);

    public IHidConnection ConnectToIOReports() =>
        new WindowsHidIOReportConnection(_devicePath);
}