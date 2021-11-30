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
using System.Runtime.InteropServices;

using static Yubico.PlatformInterop.NativeMethods;

namespace Yubico.PlatformInterop
{

    public class CmDeviceEventArgs : EventArgs
    {
        public string DeviceInterfacePath { get; private set; }

        public CmDeviceEventArgs(string deviceInterfacePath)
        {
            DeviceInterfacePath = deviceInterfacePath;
        }
        public CmDevice GetDevice() => new CmDevice(DeviceInterfacePath);
    }

    public class CmDeviceListener : IDisposable
    {
        private readonly IntPtr _notificationContext;

        public Guid InterfaceClass { get; private set; }

        public event EventHandler<CmDeviceEventArgs>? DeviceArrived;
        public event EventHandler<CmDeviceEventArgs>? DeviceRemoved;

        public CmDeviceListener(Guid classGuid)
        {
            InterfaceClass = classGuid;

            byte[] zeroBytes = new byte[NativeMethods.CmNotifyFilterSize];
            byte[] guidBytes = classGuid.ToByteArray();

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
                if (errorCode != CmErrorCode.CR_SUCCESS)
                {
                    throw new PlatformApiException(
                        "CONFIG_RET",
                        (int)errorCode,
                        $"Failed to register for notifications on the device interface class {classGuid}."
                        );
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pFilter);
            }
        }

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // No managed state to free
                }

                StopListening();

                disposedValue = true;
            }
        }

        ~CmDeviceListener()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private int OnEventReceived(IntPtr hNotify, IntPtr Context, CM_NOTIFY_ACTION Action, IntPtr EventData, int EventDataSize)
        {
            EventHandler<CmDeviceEventArgs>? handler = Action switch
            {
                CM_NOTIFY_ACTION.DEVICEINTERFACEARRIVAL => DeviceArrived,
                CM_NOTIFY_ACTION.DEVICEINTERFACEREMOVAL => DeviceRemoved,
                _ => throw new InvalidOperationException($"Received an unexpected device event of type {Action}"),
            };

            if (handler is null)
            {
                return 0;
            }

            CM_NOTIFY_EVENT_DATA eventData = Marshal.PtrToStructure<CM_NOTIFY_EVENT_DATA>(EventData);
            Debug.Assert(eventData.ClassGuid == InterfaceClass);
            Debug.Assert(eventData.FilterType == CM_NOTIFY_FILTER_TYPE.DEVINTERFACE);

            int stringOffset = 24; // Magic number from C land
            int stringSize = EventDataSize - stringOffset;
            byte[] buffer = new byte[EventDataSize];

            Marshal.Copy(EventData + stringOffset, buffer, 0, stringSize);

            var eventArgs = new CmDeviceEventArgs(System.Text.Encoding.Unicode.GetString(buffer));

            handler.Invoke(this, eventArgs);

            return 0;
        }

        private void StopListening()
        {
            if (_notificationContext != IntPtr.Zero)
            {
                CmErrorCode errorCode = CM_Unregister_Notification(_notificationContext);
                if (errorCode != CmErrorCode.CR_SUCCESS)
                {
                    throw new PlatformApiException(
                        "CONFIG_RET",
                        (int)errorCode,
                        $"Unexpected error occured when attempting to unregister for notifications. InterfaceClass = {InterfaceClass}."
                        );
                }
            }
        }
    }
}
