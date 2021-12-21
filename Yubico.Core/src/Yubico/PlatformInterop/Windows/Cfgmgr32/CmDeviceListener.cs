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
using Yubico.Core;
using static Yubico.PlatformInterop.NativeMethods;

namespace Yubico.PlatformInterop
{
    /// <summary>
    /// A listener class for Windows HID related events.
    /// </summary>
    internal class CmDeviceListener : IDisposable
    {
        private readonly IntPtr _notificationContext;

        /// <summary>
        /// A global unique identifier for HID device.
        /// </summary>
        public Guid InterfaceClass { get; }

        private readonly Action<CmDeviceEventArgs> _arrivalAction;
        private readonly Action<CmDeviceEventArgs> _removalAction;

        /// <summary>
        /// Constructs a <see cref="CmDeviceListener"/>.
        /// </summary>
        /// <param name="classGuid">A global unique identifier for Windows HID device.</param>
        /// <param name="arrivalAction">An action for Windows HID device device arrival.</param>
        /// <param name="removalAction">An action for Windows HID device device removal.</param>
        public CmDeviceListener(Guid classGuid, Action<CmDeviceEventArgs> arrivalAction, Action<CmDeviceEventArgs> removalAction)
        {
            InterfaceClass = classGuid;
            _arrivalAction = arrivalAction;
            _removalAction = removalAction;

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
                ThrowIfFailed(errorCode);
            }
            finally
            {
                Marshal.FreeHGlobal(pFilter);
            }
        }

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        /// <summary>
        /// Disposes the objects.
        /// </summary>
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
        /// <summary>
        /// Calls Dispose(true).
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private int OnEventReceived(IntPtr hNotify, IntPtr Context, CM_NOTIFY_ACTION Action, IntPtr EventData, int EventDataSize)
        {
            CM_NOTIFY_EVENT_DATA eventData = Marshal.PtrToStructure<CM_NOTIFY_EVENT_DATA>(EventData);
            Debug.Assert(eventData.ClassGuid == InterfaceClass);
            Debug.Assert(eventData.FilterType == CM_NOTIFY_FILTER_TYPE.DEVINTERFACE);

            int stringOffset = 24; // Magic number from C land
            int stringSize = EventDataSize - stringOffset;
            byte[] buffer = new byte[EventDataSize];

            Marshal.Copy(EventData + stringOffset, buffer, 0, stringSize);

            var eventArgs = new CmDeviceEventArgs(System.Text.Encoding.Unicode.GetString(buffer));

            if (Action == CM_NOTIFY_ACTION.DEVICEINTERFACEARRIVAL)
            {
                OnCmDeviceArrived(eventArgs);
            }

            if (Action == CM_NOTIFY_ACTION.DEVICEINTERFACEREMOVAL)
            {
                OnCmDeviceRemoved(eventArgs);
            }

            return 0;
        }

        /// <summary>
        /// Raises event on Windows HID device arrival.
        /// </summary>
        private void OnCmDeviceArrived(CmDeviceEventArgs e) => _arrivalAction(e);

        /// <summary>
        /// Raises event on Windows HID device removal.
        /// </summary>
        private void OnCmDeviceRemoved(CmDeviceEventArgs e) => _removalAction(e);

        /// <summary>
        /// Stops listening for all actions within a certain context.
        /// </summary>
        public void StopListening()
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
    }
}
