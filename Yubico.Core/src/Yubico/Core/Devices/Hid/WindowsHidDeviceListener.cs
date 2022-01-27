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
using Yubico.PlatformInterop;

using static Yubico.PlatformInterop.NativeMethods;

namespace Yubico.Core.Devices.Hid
{
    internal class WindowsHidDeviceListener : HidDeviceListener
    {
        private IntPtr _notificationContext;

        public WindowsHidDeviceListener()
        {
            StartListening();
        }

        ~WindowsHidDeviceListener()
        {
            StopListening();
        }

        private void StartListening()
        {
            Guid interfaceClass = CmClassGuid.HidClass;

            byte[] zeroBytes = new byte[NativeMethods.CmNotifyFilterSize];
            byte[] guidBytes = interfaceClass.ToByteArray();

            IntPtr pFilter = Marshal.AllocHGlobal(NativeMethods.CmNotifyFilterSize);

            try
            {
                // Set all the bytes to zero.
                Marshal.Copy(zeroBytes, 0, pFilter, zeroBytes.Length);
                Marshal.WriteInt32(pFilter, NativeMethods.OffsetCbSize, NativeMethods.CmNotifyFilterSize);
                Marshal.WriteInt32(pFilter, NativeMethods.OffsetFlags, 0);
                Marshal.WriteInt32(pFilter, NativeMethods.OffsetFilterType, (int)CM_NOTIFY_FILTER_TYPE.DEVINTERFACE);
                Marshal.WriteInt32(pFilter, NativeMethods.OffsetReserved, 0);
                for (int index = 0; index < guidBytes.Length; index++)
                {
                    Marshal.WriteByte(pFilter, NativeMethods.OffsetGuidData1 + index, guidBytes[index]);
                }

                CmErrorCode errorCode = CM_Register_Notification(pFilter, IntPtr.Zero, OnEventReceived, out _notificationContext);
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
                ThrowIfFailed(errorCode);
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

        private int OnEventReceived(IntPtr hNotify, IntPtr context, CM_NOTIFY_ACTION action, IntPtr eventDataPtr, int eventDataSize)
        {
            CM_NOTIFY_EVENT_DATA eventData = Marshal.PtrToStructure<CM_NOTIFY_EVENT_DATA>(eventDataPtr);
            Debug.Assert(eventData.ClassGuid == CmClassGuid.HidClass);
            Debug.Assert(eventData.FilterType == CM_NOTIFY_FILTER_TYPE.DEVINTERFACE);

            int stringOffset = 24; // Magic number from C land
            int stringSize = eventDataSize - stringOffset;
            byte[] buffer = new byte[eventDataSize];

            Marshal.Copy(eventDataPtr + stringOffset, buffer, 0, stringSize);

            string instancePath = System.Text.Encoding.Unicode.GetString(buffer);
            var device = new WindowsHidDevice(new CmDevice(instancePath));

            if (action == CM_NOTIFY_ACTION.DEVICEINTERFACEARRIVAL)
            {
                OnArrived(device);
            }
            else if (action == CM_NOTIFY_ACTION.DEVICEINTERFACEREMOVAL)
            {
                OnRemoved(device);
            }

            return 0;
        }
    }
}
