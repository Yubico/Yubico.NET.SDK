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
}