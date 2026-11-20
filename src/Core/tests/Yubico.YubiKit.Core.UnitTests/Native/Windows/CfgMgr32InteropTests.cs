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

using System.Reflection;
using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Native.Windows.Cfgmgr32;
using Yubico.YubiKit.Core.Transports.Hid.Windows;

namespace Yubico.YubiKit.Core.UnitTests.Native.Windows;

public class CfgMgr32InteropTests
{
    [Fact]
    public void CmNotifyFilter_Size_MatchesNativeLayout()
    {
        Assert.Equal(416, Marshal.SizeOf<NativeMethods.CM_NOTIFY_FILTER>());
        Assert.Equal(NativeMethods.CmNotifyFilterSize, Marshal.SizeOf<NativeMethods.CM_NOTIFY_FILTER>());
    }

    [Fact]
    public void WindowsHidDeviceListener_Start_RegistersNotification()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip("Windows-only");
        }

        using var listener = new WindowsHidDeviceListener();

        listener.Start();

        try
        {
            Assert.Equal(DeviceListenerStatus.Started, listener.Status);
        }
        finally
        {
            listener.Stop();
        }

        Assert.Equal(DeviceListenerStatus.Stopped, listener.Status);
    }

    // CM_Get_Device_Interface_List(_Size)W take the class GUID as LPGUID. Passing it by value happens to
    // work on x64 (16-byte structs go by hidden pointer) but shifts every argument on Arm64, where the
    // call then fails with CR_INVALID_DEVNODE. Only an Arm64 run can turn this test red.
    [Fact]
    public void GetDevicePaths_HidInterfaceClass_ReturnsOnlyHidInterfacePaths()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip("Windows-only");
        }

        var paths = CmDevice.GetDevicePaths(CmInterfaceGuid.Hid, null);

        var hidClass = CmInterfaceGuid.Hid.ToString("B");
        Assert.All(paths, path => Assert.Contains(hidClass, path, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("CM_Get_Device_Interface_PropertyW")]
    [InlineData("CM_Get_DevNode_PropertyW")]
    public void PropertyImports_TakeBufferSizeAsAnUnsigned32BitReference(string entryPoint)
    {
        var method = Assert.Single(typeof(NativeMethods).GetMethods(BindingFlags.Static | BindingFlags.NonPublic),
            method => method.GetCustomAttribute<LibraryImportAttribute>()?.EntryPoint == entryPoint);

        Assert.Equal(typeof(uint).MakeByRefType(), method.GetParameters()[4].ParameterType);
    }

    [Fact]
    public void ReadProperties_DiskInterfaceAndDevNodeReturnTheSameInstanceId()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip("Windows-only");
        }

        // GUID_DEVINTERFACE_DISK gives this test a real interface without requiring a YubiKey.
        var diskInterfaceClass = new Guid("53F56307-B6BF-11D0-94F2-00A0C91EFB8B");
        var paths = CmDevice.GetDevicePaths(diskInterfaceClass, null);
        Assert.NotEmpty(paths);

        var key = NativeMethods.DEVPKEY_Device_InstanceId;
        var interfaceId = Assert.IsType<string>(CmPropertyAccessHelper.TryGetProperty(
            NativeMethods.CM_Get_Device_Interface_Property, paths[0], key));
        Assert.NotEmpty(interfaceId.TrimEnd('\0'));

        var result = NativeMethods.CM_Locate_DevNode(out var instance, interfaceId, NativeMethods.CM_LOCATE_DEVNODE.NORMAL);
        Assert.Equal(NativeMethods.CmErrorCode.CR_SUCCESS, result);

        var devNodeId = Assert.IsType<string>(CmPropertyAccessHelper.TryGetProperty(
            NativeMethods.CM_Get_DevNode_Property, instance, key));
        Assert.Equal(interfaceId, devNodeId, StringComparer.OrdinalIgnoreCase);
        Assert.NotEqual(Guid.Empty, Assert.IsType<Guid>(CmPropertyAccessHelper.TryGetProperty(
            NativeMethods.CM_Get_DevNode_Property, instance, NativeMethods.DEVPKEY_Device_ClassGuid)));
    }
}