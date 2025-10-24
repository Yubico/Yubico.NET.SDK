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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using Yubico.Core.Buffers;
using static Yubico.PlatformInterop.NativeMethods;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Yubico.PlatformInterop
{
    public class CmDevice
    {
        public string InstanceId { get; private set; }
        public int Instance { get; private set; }
        public string Class { get; private set; } = null!;
        public Guid ClassGuid { get; private set; }
        public string Path { get; private set; } = null!;
        public Guid ContainerId { get; private set; }
        public string? InterfacePath { get; private set; }

        public short HidUsageId { get; private set; }
        public short HidUsagePage { get; private set; }

        [Obsolete("Use the factory methods FromDevicePath or FromDeviceInstance instead. This will be removed in a future release.", false)]
        public CmDevice(string devicePath)
        {
            InterfacePath = devicePath;
            InstanceId = (string?)CmPropertyAccessHelper.TryGetProperty(CM_Get_Device_Interface_Property, devicePath, DEVPKEY_Device_InstanceId)!;
            Instance = LocateDevNode(InstanceId);
            
            InitializeProperties();
        }

        [Obsolete("Use the factory methods FromDevicePath or FromDeviceInstance instead. This will be removed in a future release.", false)]
        public CmDevice(int deviceInstance)
        {
            Instance = deviceInstance;
            InstanceId = GetProperty<string>(CmDeviceProperty.InstanceId);

            InitializeProperties();
        }

        public static CmDevice? FromDevicePath(string devicePath)
        {
            ILogger logger = Core.Logging.Log.GetLogger<CmDevice>();

            try
            {
                return new CmDevice(devicePath);
            }
            catch (KeyNotFoundException ex)
            {
                // Device disappeared between notification and property query
                // Expected during rapid plug/unplug cycles
                logger.LogDebug(
                    ex,
                    "Device property unavailable - device removed during enumeration.");

                return null;
            }

            catch (PlatformApiException ex)
            {
                // CmDevice constructor or property access failed
                // Could be transient (device gone) or persistent (CM API broken)
                logger.LogWarning(
                    ex,
                    "Configuration Manager API error during device enumeration.");

                return null;
            }
            catch (Exception ex)
            {
                // Truly unexpected exceptions - could be SDK bugs
                logger.LogError(
                    ex,
                    "Unexpected exception in ConfigMgr callback.");

                return null;
            }
        }

        public static CmDevice? FromDeviceInstance(int deviceInstance)
        {
            ILogger logger = Core.Logging.Log.GetLogger<CmDevice>();

            try
            {
                return new CmDevice(deviceInstance);
            }
            catch (KeyNotFoundException ex)
            {
                // Device disappeared between notification and property query
                // Expected during rapid plug/unplug cycles
                logger.LogDebug(
                    ex,
                    "Device property unavailable - device removed during enumeration.");

                return null;
            }

            catch (PlatformApiException ex)
            {
                // CmDevice constructor or property access failed
                // Could be transient (device gone) or persistent (CM API broken)
                logger.LogWarning(
                    ex,
                    "Configuration Manager API error during device enumeration.");

                return null;
            }
            catch (Exception ex)
            {
                // Truly unexpected exceptions - could be SDK bugs
                logger.LogError(
                    ex,
                    "Unexpected exception in ConfigMgr callback.");

                return null;
            }
        }

        public static IList<CmDevice> GetList(Guid classGuid)
        {
            IList<string> devicePaths = GetDevicePaths(classGuid, null);
            IEnumerable<CmDevice?> devices = devicePaths.Select(FromDevicePath);
            
            return [.. devices .Where(d => d != null).Select(d => d!)];
        }

        public bool TryGetProperty<T>(CmDeviceProperty property, out T? value) where T : class
        {
            DEVPROPKEY propKey = property switch
            {
                CmDeviceProperty.DeviceDescription => DEVPKEY_Device_DeviceDesc,
                CmDeviceProperty.HardwareIds => DEVPKEY_Device_HardwareIds,
                CmDeviceProperty.CompatibleIds => DEVPKEY_Device_CompatibleIds,
                CmDeviceProperty.Service => DEVPKEY_Device_Service,
                CmDeviceProperty.Class => DEVPKEY_Device_Class,
                CmDeviceProperty.ClassGuid => DEVPKEY_Device_ClassGuid,
                CmDeviceProperty.Driver => DEVPKEY_Device_Driver,
                CmDeviceProperty.Manufacturer => DEVPKEY_Device_Manufacturer,
                CmDeviceProperty.FriendlyName => DEVPKEY_Device_FriendlyName,
                CmDeviceProperty.LocationInfo => DEVPKEY_Device_LocationPaths,
                CmDeviceProperty.PdoName => DEVPKEY_Device_PDOName,
                CmDeviceProperty.LocationPaths => DEVPKEY_Device_LocationPaths,
                CmDeviceProperty.InstanceId => DEVPKEY_Device_InstanceId,
                CmDeviceProperty.DevNodeStatus => DEVPKEY_Device_DevNodeStatus,
                CmDeviceProperty.ProblemCode => DEVPKEY_Device_ProblemCode,
                CmDeviceProperty.Parent => DEVPKEY_Device_Parent,
                CmDeviceProperty.Children => DEVPKEY_Device_Children,
                CmDeviceProperty.Siblings => DEVPKEY_Device_Siblings,
                CmDeviceProperty.Model => DEVPKEY_Device_Model,
                CmDeviceProperty.ContainerId => DEVPKEY_Device_ContainerId,
                _ => throw new ArgumentException(Core.ExceptionMessages.CmPropertyNotSupported, nameof(property)),
            };

            value = (T?)CmPropertyAccessHelper.TryGetProperty(CM_Get_DevNode_Property, Instance, propKey);
            return value != null;
        }

        public T GetProperty<T>(CmDeviceProperty property)
        {
            if (TryGetProperty(property, out object? value))
            {
                return (T)value!;
            }

            throw new KeyNotFoundException(property.ToString());
        }

        public SafeFileHandle OpenDevice()
        {
            SafeFileHandle handle = CreateFile(
                Path,
                DESIRED_ACCESS.NONE,
                FILE_SHARE.READWRITE,
                IntPtr.Zero,
                CREATION_DISPOSITION.OPEN_EXISTING,
                FILE_FLAG.NORMAL,
                IntPtr.Zero
                );

            if (handle.IsInvalid)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            return handle;
        }

        public CmDevice? Parent()
        {
            CmErrorCode errorCode = CM_Get_Parent(out int parentInstance, Instance);
            if (errorCode == CmErrorCode.CR_SUCCESS)
            {
                return FromDeviceInstance(parentInstance);
            }

            if (errorCode != CmErrorCode.CR_NO_SUCH_DEVNODE)
            {
                throw new PlatformApiException(
                    "CONFIG_RET",
                    (int)errorCode,
                    $"Failed to retrieve parent device for device {Path}."
                    );
            }

            return null;
        }

        public IList<CmDevice> Children()
        {
            var children = new List<CmDevice>();

            CmErrorCode errorCode = CM_Get_Child(out int childInstance, Instance);
            while (errorCode == CmErrorCode.CR_SUCCESS)
            {
                CmDevice? cmDevice = FromDeviceInstance(childInstance);
                if (cmDevice is null)
                {
                    continue;
                }
                
                children.Add(cmDevice);

                errorCode = CM_Get_Sibling(out childInstance, childInstance);
            }

            if (errorCode != CmErrorCode.CR_NO_SUCH_DEVNODE)
            {
                throw new PlatformApiException(
                    "CONFIG_RET",
                    (int)errorCode,
                    $"Failed to retrieve child devices for device {Path}."
                    );
            }

            return children;
        }

        public CmDevice FindFirstChild(Guid classGuid) => Children().Where(d => d.ClassGuid == classGuid).First();

        [SuppressMessage("Performance", "CA1846:Prefer \'AsSpan\' over \'Substring\'")]
        private static short GetHexShort(string s, int offset, int length)
        {
            #pragma warning disable CA1846 // Prefer 'AsSpan' over 'Substring'

            // The overload required by this preference is not available
            // for our .NETStandard 2.0 targets.
            ushort temp = ushort.Parse(
                s.Substring(offset, length), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            #pragma warning restore CA1846 // Prefer 'AsSpan' over 'Substring'
            return unchecked((short)temp);
        }
        
        private void InitializeProperties()
        {
            // Requires Instance to be set first, otherwise will throw.
            Class = GetProperty<string>(CmDeviceProperty.Class);
            ClassGuid = GetProperty<Guid>(CmDeviceProperty.ClassGuid);
            ContainerId = GetProperty<Guid>(CmDeviceProperty.ContainerId);
            Path = "\\\\?\\GLOBALROOT" + GetProperty<string>(CmDeviceProperty.PdoName);

            if (ClassGuid == CmClassGuid.HidClass || ClassGuid == CmClassGuid.Keyboard)
            {
                ResolveHidUsages();
            }
        }

        private void ResolveHidUsages()
        {
            // see: https://docs.microsoft.com/en-us/windows-hardware/drivers/hid/hidclass-hardware-ids-for-top-level-collections

            string[] hardwareIds = GetProperty<string[]>(CmDeviceProperty.HardwareIds);
            string? hidUsageHardwareId = hardwareIds
                .FirstOrDefault(hi => hi.StartsWith("HID_DEVICE_UP", StringComparison.OrdinalIgnoreCase));

            // HID_DEVICE_UP:XXXX_U:XXXX
            // 0123456789012345678901234

            if (hidUsageHardwareId is null || hidUsageHardwareId.Length < 25)
            {
                return;
            }

            HidUsagePage = GetHexShort(hidUsageHardwareId, 14, 4);
            HidUsageId = GetHexShort(hidUsageHardwareId, 21, 4);
        }

        private static IList<string> GetDevicePaths(Guid classGuid, string? deviceInstanceId)
        {
            CmErrorCode errorCode = CM_Get_Device_Interface_List_Size(
                out int bufferLength,
                classGuid,
                deviceInstanceId,
                CM_GET_DEVICE_LIST.PRESENT
                );

            if (errorCode != CmErrorCode.CR_SUCCESS)
            {
                throw new PlatformApiException(
                    "CONFIG_RET",
                    (int)errorCode,
                    $"Failed to get the size needed for the device interface list for the device class {classGuid}."
                    );
            }

            // Multiple by two as size is in (wide) character count, but we're passing a byte array.
            byte[] buffer = new byte[bufferLength * 2];
            errorCode = CM_Get_Device_Interface_List(
                classGuid,
                deviceInstanceId,
                buffer,
                bufferLength * 2,
                CM_GET_DEVICE_LIST.PRESENT
                );

            if (errorCode != CmErrorCode.CR_SUCCESS)
            {
                throw new PlatformApiException(
                    "CONFIG_RET",
                    (int)errorCode,
                    $"Failed to retrieve the device interface list for the device class {classGuid}."
                    );
            }

            return MultiString.GetStrings(buffer, System.Text.Encoding.Unicode);
        }

        private static int LocateDevNode(string instanceId)
        {
            const CM_LOCATE_DEVNODE flags = CM_LOCATE_DEVNODE.NORMAL;
            
            CmErrorCode errorCode = CM_Locate_DevNode(out int devNodeHandle, instanceId, flags);
            if (errorCode != CmErrorCode.CR_SUCCESS)
            {
                throw new PlatformApiException(
                    "CONFIG_RET",
                    (int)errorCode,
                    $"Unable to locate the device node for the device interface {instanceId} using the flags {flags}."
                    );
            }

            return devNodeHandle;
        }
    }
}
