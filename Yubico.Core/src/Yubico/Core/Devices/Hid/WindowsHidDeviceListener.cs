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
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Yubico.PlatformInterop;

using static Yubico.PlatformInterop.NativeMethods;

namespace Yubico.Core.Devices.Hid
{
    internal class WindowsHidDeviceListener : HidDeviceListener
    {
        private IntPtr _notificationContext;
        private GCHandle? _marshalableThisPtr;
        private CM_NOTIFY_CALLBACK? _callbackDelegate;

        private readonly ILogger _log = Logging.Log.GetLogger<WindowsHidDeviceListener>();

        public WindowsHidDeviceListener()
        {
            _log.LogInformation("Creating WindowsHidDeviceListener.");
            StartListening();
        }

        ~WindowsHidDeviceListener()
        {
            StopListening();
        }

        private void StartListening()
        {
            Guid interfaceClass = CmInterfaceGuid.Hid;

            byte[] zeroBytes = new byte[CmNotifyFilterSize];
            byte[] guidBytes = interfaceClass.ToByteArray();

            IntPtr pFilter = Marshal.AllocHGlobal(CmNotifyFilterSize);

            try
            {
                // Set all the bytes to zero.
                Marshal.Copy(zeroBytes, 0, pFilter, zeroBytes.Length);
                Marshal.WriteInt32(pFilter, OffsetCbSize, CmNotifyFilterSize);
                Marshal.WriteInt32(pFilter, OffsetFlags, 0);
                Marshal.WriteInt32(pFilter, OffsetFilterType, (int)CM_NOTIFY_FILTER_TYPE.DEVINTERFACE);
                Marshal.WriteInt32(pFilter, OffsetReserved, 0);
                for (int index = 0; index < guidBytes.Length; index++)
                {
                    Marshal.WriteByte(pFilter, OffsetGuidData1 + index, guidBytes[index]);
                }

                _marshalableThisPtr = GCHandle.Alloc(this);
                _callbackDelegate = OnEventReceived;
                CmErrorCode errorCode = CM_Register_Notification(pFilter, GCHandle.ToIntPtr(_marshalableThisPtr.Value), _callbackDelegate, out _notificationContext);
                _log.LogInformation("Registered callback with ConfigMgr32.");
                ThrowIfFailed(errorCode);
            }
            finally
            {
                Marshal.FreeHGlobal(pFilter);
            }
        }

        private void StopListening()
        {
            if (_notificationContext != IntPtr.Zero)
            {
                CmErrorCode errorCode = CM_Unregister_Notification(_notificationContext);
                _log.LogInformation("Unregistered callback with ConfigMgr32.");
                ThrowIfFailed(errorCode);
            }

            if (_marshalableThisPtr.HasValue)
            {
                _marshalableThisPtr.Value.Free();
                _marshalableThisPtr = null;
            }
        }

        private static void ThrowIfFailed(CmErrorCode errorCode)
        {
            if (errorCode != CmErrorCode.CR_SUCCESS)
            {
                throw new PlatformApiException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.CmError));
            }
        }

        private static int OnEventReceived(IntPtr hNotify, IntPtr context, CM_NOTIFY_ACTION action, IntPtr eventDataPtr, int eventDataSize)
        {
            GCHandle thisPtr = GCHandle.FromIntPtr(context);
            var thisObj = thisPtr.Target as WindowsHidDeviceListener;
            thisObj?._log.LogInformation("ConfigMgr callback received.");

            CM_NOTIFY_EVENT_DATA eventData = Marshal.PtrToStructure<CM_NOTIFY_EVENT_DATA>(eventDataPtr);
            Debug.Assert(eventData.ClassGuid == CmInterfaceGuid.Hid);
            Debug.Assert(eventData.FilterType == CM_NOTIFY_FILTER_TYPE.DEVINTERFACE);

            int stringOffset = 24; // Magic number from C land
            int stringSize = eventDataSize - stringOffset;
            byte[] buffer = new byte[eventDataSize];

            Marshal.Copy(eventDataPtr + stringOffset, buffer, 0, stringSize);

            if (action == CM_NOTIFY_ACTION.DEVICEINTERFACEARRIVAL)
            {
                string instancePath = System.Text.Encoding.Unicode.GetString(buffer);
                var cmDevice = new CmDevice(instancePath);
                var device = new WindowsHidDevice(cmDevice);
                thisObj?.OnArrived(device);
            }
            else if (action == CM_NOTIFY_ACTION.DEVICEINTERFACEREMOVAL)
            {
                thisObj?.OnRemoved(null);
            }

            return 0;
        }
    }
}
