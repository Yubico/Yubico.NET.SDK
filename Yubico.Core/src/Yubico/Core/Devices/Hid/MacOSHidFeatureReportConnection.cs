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
using Yubico.PlatformInterop;
using static Yubico.PlatformInterop.NativeMethods;

namespace Yubico.Core.Devices.Hid
{
    /// <summary>
    /// macOS implementation of the keyboard feature report connection.
    /// </summary>
    internal sealed class MacOSHidFeatureReportConnection : IHidConnection
    {
        private readonly long _entryId;
        private IntPtr _deviceHandle;

        private bool _isDisposed;

        /// <summary>
        /// The correct size, in bytes, for the data buffer to be transmitted to the keyboard.
        /// </summary>
        public int InputReportSize { get; }

        /// <summary>
        /// The correct size, in bytes, for the data buffer to be received from the keyboard.
        /// </summary>
        public int OutputReportSize { get; }

        /// <summary>
        /// Constructs an instance of the MacOSHidFeatureReportConnection class.
        /// </summary>
        /// <param name="entryId">
        /// The IOKit registry entry identifier representing the device we're trying to connect to.
        /// </param>
        public MacOSHidFeatureReportConnection(long entryId)
        {
            _entryId = entryId;
            SetupConnection();

            InputReportSize = IOKitHelpers.GetIntPropertyValue(_deviceHandle, IOKitHidConstants.MaxInputReportSize);
            OutputReportSize = IOKitHelpers.GetIntPropertyValue(_deviceHandle, IOKitHidConstants.MaxOutputReportSize);
        }

        private void SetupConnection()
        {
            int deviceEntry = 0;
            try
            {
                IntPtr matchingDictionary = IORegistryEntryIDMatching(_entryId);
                deviceEntry = IOServiceGetMatchingService(0, matchingDictionary);

                if (deviceEntry == 0)
                {
                    throw new PlatformApiException(ExceptionMessages.IOKitRegistryEntryNotFound);
                }

                _deviceHandle = IOHIDDeviceCreate(IntPtr.Zero, deviceEntry);

                if (_deviceHandle == IntPtr.Zero)
                {
                    throw new PlatformApiException(ExceptionMessages.IOKitCannotOpenDevice);
                }

                int result = IOHIDDeviceOpen(_deviceHandle, 0);

                if (result != 0)
                {
                    throw new PlatformApiException(
                        nameof(IOHIDDeviceOpen),
                        result,
                        ExceptionMessages.IOKitCannotOpenDevice);
                }
            }
            finally
            {
                if (deviceEntry != 0)
                {
                    _ = IOObjectRelease(deviceEntry);
                }
            }
        }

        /// <summary>
        /// Reads a report from the keyboard interface.
        /// </summary>
        /// <returns>
        /// A buffer that contains the data received from the keyboard.
        /// </returns>
        /// <exception cref="PlatformApiException">
        /// Thrown when the underlying IOKit framework reports an error. See the exception message for details.
        /// </exception>
        public byte[] GetReport()
        {
            const int featureReportSize = 8;

            byte[] buffer = new byte[featureReportSize];
            long bufferSize = buffer.Length;

            int result = IOHIDDeviceGetReport(
                _deviceHandle,
                IOKitHidConstants.kIOHidReportTypeFeature,
                0,
               buffer,
               ref bufferSize);

            if (result != 0)
            {
                throw new PlatformApiException(
                    nameof(IOHIDDeviceGetReport),
                    result,
                    ExceptionMessages.IOKitOperationFailed);
            }

            return buffer;
        }

        /// <summary>
        /// Sends a buffer to the keyboard device.
        /// </summary>
        /// <param name="report">
        /// The buffer to send.
        /// </param>
        /// <exception cref="PlatformApiException">
        /// Thrown when the underlying IOKit framework reports an error. See the exception message for details.
        /// </exception>
        public void SetReport(byte[] report)
        {
            int result = IOHIDDeviceSetReport(
                _deviceHandle,
                IOKitHidConstants.kIOHidReportTypeFeature,
                0,
                report,
                report.Length);

            if (result != 0)
            {
                throw new PlatformApiException(
                    nameof(IOHIDDeviceSetReport),
                    result,
                    ExceptionMessages.IOKitOperationFailed);
            }
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed state here
            }

            if (_deviceHandle != IntPtr.Zero)
            {
                _ = IOHIDDeviceClose(_deviceHandle, 0);
                _deviceHandle = IntPtr.Zero;
            }

            _isDisposed = true;
        }

        ~MacOSHidFeatureReportConnection()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        /// <summary>
        /// Disposes this object and all of the underlying platform handles for this connection.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
