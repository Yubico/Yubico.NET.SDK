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

using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Native.Windows.Cfgmgr32;
using CfgmgrNativeMethods = Yubico.YubiKit.Core.Native.Windows.Cfgmgr32.NativeMethods;
using ScardNativeMethods = Yubico.YubiKit.Core.Native.Desktop.SCard.NativeMethods;
using WinSCardNativeMethods = Yubico.YubiKit.Core.Native.Windows.WinSCard.NativeMethods;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     The real Windows topology native operations: <c>SCardGetReaderDeviceInstanceIdW</c> for CCID readers
///     and <see cref="CmDevice" /> (Cfgmgr32) for Container-ID lookups.
/// </summary>
/// <remarks>
///     Constructed only on Windows (see <see cref="DeviceTopologyResolver.Create" />). Every method is
///     best-effort: a native failure returns <c>false</c> rather than throwing, because topology evidence is
///     optional and its absence must degrade cleanly rather than abort a scan.
/// </remarks>
internal sealed class WindowsTopologyNativeOps : IWindowsTopologyNativeOps
{
    public bool TryGetReaderDeviceInstanceId(string readerName, out string? deviceInstanceId)
    {
        deviceInstanceId = null;

        var establishResult = ScardNativeMethods.SCardEstablishContext(SCARD_SCOPE.USER, out var context);
        if (establishResult != ErrorCode.SCARD_S_SUCCESS)
            return false;

        using (context)
        {
            // Two-call pattern: length probe, then the buffered read.
            var lengthInChars = 0;
            var probeResult = WinSCardNativeMethods.SCardGetReaderDeviceInstanceId(
                context,
                readerName,
                null,
                ref lengthInChars);

            if (probeResult != ErrorCode.SCARD_S_SUCCESS || lengthInChars <= 0)
                return false;

            var buffer = new char[lengthInChars];
            var readResult = WinSCardNativeMethods.SCardGetReaderDeviceInstanceId(
                context,
                readerName,
                buffer,
                ref lengthInChars);

            if (readResult != ErrorCode.SCARD_S_SUCCESS)
                return false;

            // The API returns a null-terminated string; trim the terminator and any slack.
            var terminator = Array.IndexOf(buffer, '\0');
            var length = terminator >= 0 ? terminator : Math.Min(lengthInChars, buffer.Length);
            if (length <= 0)
                return false;

            deviceInstanceId = new string(buffer, 0, length);
            return true;
        }
    }

    public bool TryGetContainerIdByInstanceId(string deviceInstanceId, out Guid containerId) =>
        TryReadContainerId(() => new CmDevice(GetDeviceInstance(deviceInstanceId)), out containerId);

    public bool TryGetContainerIdByDevicePath(string devicePath, out Guid containerId) =>
        TryReadContainerId(() => new CmDevice(devicePath), out containerId);

    private static int GetDeviceInstance(string deviceInstanceId)
    {
        var errorCode = CfgmgrNativeMethods.CM_Locate_DevNode(
            out var deviceInstance,
            deviceInstanceId,
            CfgmgrNativeMethods.CM_LOCATE_DEVNODE.NORMAL);

        // CR_NO_SUCH_DEVNODE is expected for a stale instance id mid-hotplug: treat as unknown topology.
        return errorCode == CfgmgrNativeMethods.CmErrorCode.CR_SUCCESS
            ? deviceInstance
            : throw new KeyNotFoundException($"CM_Locate_DevNode failed with {errorCode}.");
    }

    private static bool TryReadContainerId(Func<CmDevice> deviceFactory, out Guid containerId)
    {
        containerId = Guid.Empty;
        try
        {
            containerId = deviceFactory().ContainerId;
            return containerId != Guid.Empty;
        }
        catch (Exception e) when (e is KeyNotFoundException or PlatformApiException or InvalidOperationException)
        {
            // Missing ContainerId property, stale devnode, or a malformed path: unknown topology.
            return false;
        }
    }
}