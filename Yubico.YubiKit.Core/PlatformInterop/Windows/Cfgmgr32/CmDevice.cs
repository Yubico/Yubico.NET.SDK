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

using Microsoft.Win32.SafeHandles;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Yubico.YubiKit.Core.Buffers;

namespace Yubico.YubiKit.Core.PlatformInterop.Windows.Cfgmgr32;

public class CmDevice
{
    public CmDevice(string devicePath)
    {
        InterfacePath = devicePath;
        InstanceId = (string?)CmPropertyAccessHelper.TryGetProperty(NativeMethods.CM_Get_Device_Interface_Property,
            devicePath, NativeMethods.DEVPKEY_Device_InstanceId)!;
        Instance = LocateDevNode();
        Class = GetProperty<string>(CmDeviceProperty.Class);
        ClassGuid = GetProperty<Guid>(CmDeviceProperty.ClassGuid);
        ContainerId = GetProperty<Guid>(CmDeviceProperty.ContainerId);
        Path = "\\\\?\\GLOBALROOT" + GetProperty<string>(CmDeviceProperty.PdoName);

        if (ClassGuid == CmClassGuid.HidClass || ClassGuid == CmClassGuid.Keyboard) ResolveHidUsages();
    }

    public CmDevice(int deviceInstance)
    {
        InterfacePath = null;
        Instance = deviceInstance;
        InstanceId = GetProperty<string>(CmDeviceProperty.InstanceId);
        Class = GetProperty<string>(CmDeviceProperty.Class);
        ClassGuid = GetProperty<Guid>(CmDeviceProperty.ClassGuid);
        ContainerId = GetProperty<Guid>(CmDeviceProperty.ContainerId);
        Path = "\\\\?\\GLOBALROOT" + GetProperty<string>(CmDeviceProperty.PdoName);

        if (ClassGuid == CmClassGuid.HidClass || ClassGuid == CmClassGuid.Keyboard) ResolveHidUsages();
    }

    public string InstanceId { get; }
    public int Instance { get; }
    public string Class { get; private set; }
    public Guid ClassGuid { get; }
    public string Path { get; }
    public Guid ContainerId { get; private set; }
    public string? InterfacePath { get; private set; }

    public short HidUsageId { get; private set; }
    public short HidUsagePage { get; private set; }

    public static IList<CmDevice> GetList(Guid classGuid) =>
        GetDevicePaths(classGuid, null).Select(path => new CmDevice(path)).ToList();

    public bool TryGetProperty<T>(CmDeviceProperty property, out T? value) where T : class
    {
        var propKey = property switch
        {
            CmDeviceProperty.DeviceDescription => NativeMethods.DEVPKEY_Device_DeviceDesc,
            CmDeviceProperty.HardwareIds => NativeMethods.DEVPKEY_Device_HardwareIds,
            CmDeviceProperty.CompatibleIds => NativeMethods.DEVPKEY_Device_CompatibleIds,
            CmDeviceProperty.Service => NativeMethods.DEVPKEY_Device_Service,
            CmDeviceProperty.Class => NativeMethods.DEVPKEY_Device_Class,
            CmDeviceProperty.ClassGuid => NativeMethods.DEVPKEY_Device_ClassGuid,
            CmDeviceProperty.Driver => NativeMethods.DEVPKEY_Device_Driver,
            CmDeviceProperty.Manufacturer => NativeMethods.DEVPKEY_Device_Manufacturer,
            CmDeviceProperty.FriendlyName => NativeMethods.DEVPKEY_Device_FriendlyName,
            CmDeviceProperty.LocationInfo => NativeMethods.DEVPKEY_Device_LocationPaths,
            CmDeviceProperty.PdoName => NativeMethods.DEVPKEY_Device_PDOName,
            CmDeviceProperty.LocationPaths => NativeMethods.DEVPKEY_Device_LocationPaths,
            CmDeviceProperty.InstanceId => NativeMethods.DEVPKEY_Device_InstanceId,
            CmDeviceProperty.DevNodeStatus => NativeMethods.DEVPKEY_Device_DevNodeStatus,
            CmDeviceProperty.ProblemCode => NativeMethods.DEVPKEY_Device_ProblemCode,
            CmDeviceProperty.Parent => NativeMethods.DEVPKEY_Device_Parent,
            CmDeviceProperty.Children => NativeMethods.DEVPKEY_Device_Children,
            CmDeviceProperty.Siblings => NativeMethods.DEVPKEY_Device_Siblings,
            CmDeviceProperty.Model => NativeMethods.DEVPKEY_Device_Model,
            CmDeviceProperty.ContainerId => NativeMethods.DEVPKEY_Device_ContainerId,
            _ => throw new ArgumentException("ExceptionMessages.CmPropertyNotSupported, nameof(property")
        };

        value = (T?)CmPropertyAccessHelper.TryGetProperty(NativeMethods.CM_Get_DevNode_Property, Instance, propKey);
        return value != null;
    }

    public T GetProperty<T>(CmDeviceProperty property)
    {
        if (TryGetProperty(property, out object? value)) return (T)value!;

        throw new KeyNotFoundException(property.ToString());
    }

    public SafeFileHandle OpenDevice()
    {
        var handle = Kernel32.NativeMethods.CreateFile(
            Path,
            Kernel32.NativeMethods.DESIRED_ACCESS.NONE,
            Kernel32.NativeMethods.FILE_SHARE.READWRITE,
            IntPtr.Zero,
            Kernel32.NativeMethods.CREATION_DISPOSITION.OPEN_EXISTING,
            Kernel32.NativeMethods.FILE_FLAG.NORMAL,
            IntPtr.Zero
        );

        if (handle.IsInvalid) Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

        return handle;
    }

    public CmDevice? Parent()
    {
        var errorCode = NativeMethods.CM_Get_Parent(out var parentInstance, Instance);
        if (errorCode == NativeMethods.CmErrorCode.CR_SUCCESS) return new CmDevice(parentInstance);

        if (errorCode != NativeMethods.CmErrorCode.CR_SUCCESS &&
            errorCode != NativeMethods.CmErrorCode.CR_NO_SUCH_DEVNODE)
            throw new PlatformApiException(
                "CONFIG_RET",
                (int)errorCode,
                $"Failed to retrieve parent device for device {Path}."
            );

        return null;
    }

    public IList<CmDevice> Children()
    {
        List<CmDevice> children = new();
        NativeMethods.CmErrorCode errorCode;

        errorCode = NativeMethods.CM_Get_Child(out var childInstance, Instance);

        while (errorCode == NativeMethods.CmErrorCode.CR_SUCCESS)
        {
            children.Add(new CmDevice(childInstance));

            errorCode = NativeMethods.CM_Get_Sibling(out childInstance, childInstance);
        }

        if (errorCode != NativeMethods.CmErrorCode.CR_SUCCESS &&
            errorCode != NativeMethods.CmErrorCode.CR_NO_SUCH_DEVNODE)
            throw new PlatformApiException(
                "CONFIG_RET",
                (int)errorCode,
                $"Failed to retrieve child devices for device {Path}."
            );

        return children;
    }

    public CmDevice FindFirstChild(Guid classGuid) => Children().Where(d => d.ClassGuid == classGuid).First();

    [SuppressMessage("Performance", "CA1846:Prefer \'AsSpan\' over \'Substring\'")]
    private static short GetHexShort(string s, int offset, int length)
    {
#pragma warning disable CA1846 // Prefer 'AsSpan' over 'Substring'
        // The overload required by this preference is not available
        // for our .NETStandard 2.0 targets.
        var temp = ushort.Parse(s.Substring(offset, length), NumberStyles.HexNumber,
            CultureInfo.InvariantCulture);
#pragma warning restore CA1846 // Prefer 'AsSpan' over 'Substring'
        return unchecked((short)temp);
    }

    private void ResolveHidUsages()
    {
        // see: https://docs.microsoft.com/en-us/windows-hardware/drivers/hid/hidclass-hardware-ids-for-top-level-collections

        var hardwareIds = GetProperty<string[]>(CmDeviceProperty.HardwareIds);

        var hidUsageHardwareId = hardwareIds
            .Where(hi => hi.StartsWith("HID_DEVICE_UP", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        // HID_DEVICE_UP:XXXX_U:XXXX
        // 0123456789012345678901234

        if (!(hidUsageHardwareId is null) && hidUsageHardwareId.Length >= 25)
        {
            HidUsagePage = GetHexShort(hidUsageHardwareId, 14, 4);
            HidUsageId = GetHexShort(hidUsageHardwareId, 21, 4);
        }
    }

    private static IList<string> GetDevicePaths(Guid classGuid, string? deviceInstanceId)
    {
        var errorCode = NativeMethods.CM_Get_Device_Interface_List_Size(
            out var bufferLength,
            classGuid,
            deviceInstanceId,
            NativeMethods.CM_GET_DEVICE_LIST.PRESENT
        );

        if (errorCode != NativeMethods.CmErrorCode.CR_SUCCESS)
            throw new PlatformApiException(
                "CONFIG_RET",
                (int)errorCode,
                $"Failed to get the size needed for the device interface list for the device class {classGuid}."
            );

        // Multiple by two as size is in (wide) character count, but we're passing a byte array.
        var buffer = new byte[bufferLength * 2];
        errorCode = NativeMethods.CM_Get_Device_Interface_List(
            classGuid,
            deviceInstanceId,
            buffer,
            bufferLength * 2,
            NativeMethods.CM_GET_DEVICE_LIST.PRESENT
        );

        if (errorCode != NativeMethods.CmErrorCode.CR_SUCCESS)
            throw new PlatformApiException(
                "CONFIG_RET",
                (int)errorCode,
                $"Failed to retrieve the device interface list for the device class {classGuid}."
            );

        return MultiString.GetStrings(buffer, Encoding.Unicode);
    }

    private int LocateDevNode()
    {
        var flags = NativeMethods.CM_LOCATE_DEVNODE.NORMAL;
        var errorCode =
            NativeMethods.CM_Locate_DevNode(out var devNodeHandle, InstanceId, flags);
        if (errorCode != NativeMethods.CmErrorCode.CR_SUCCESS)
            throw new PlatformApiException(
                "CONFIG_RET",
                (int)errorCode,
                $"Unable to locate the device node for the device interface {InstanceId} using the flags {flags}."
            );

        return devNodeHandle;
    }
}