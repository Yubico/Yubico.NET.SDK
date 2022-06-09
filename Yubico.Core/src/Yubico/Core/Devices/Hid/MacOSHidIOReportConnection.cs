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
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Yubico.Core.Buffers;
using Yubico.Core.Logging;
using Yubico.PlatformInterop;
using static Yubico.PlatformInterop.NativeMethods;

namespace Yubico.Core.Devices.Hid
{
    /// <summary>
    /// macOS implementation of the FIDO IO report connection.
    /// </summary>
    public sealed class MacOSHidIOReportConnection : IHidConnection
    {
        private readonly long _entryId;
        private IntPtr _deviceHandle;
        private bool _isDisposed;
        private readonly IntPtr _loopId;
        private readonly Logger _log = Log.GetLogger();

        /// <summary>
        /// The correct size, in bytes, for the data buffer to be transmitted to the device.
        /// </summary>
        public int InputReportSize { get; }

        /// <summary>
        /// The correct size, in bytes, for the data buffer to be received from the device.
        /// </summary>
        public int OutputReportSize { get; }

        /// <summary>
        /// Constructs an instance of the MacOSHidIOReportConnection class.
        /// </summary>
        /// <param name="entryId">
        /// The IOKit registry entry identifier representing the device we're trying to connect to.
        /// </param>
        public MacOSHidIOReportConnection(long entryId)
        {
            _log.LogInformation("Creating a new IO report connection for device [{EntryId}]", entryId);

            _entryId = entryId;

            byte[] cstr = Encoding.UTF8.GetBytes($"fido2-loopid-{entryId}");
            _loopId = CFStringCreateWithCString(IntPtr.Zero, cstr, 0);

            SetupConnection();

            InputReportSize = IOKitHelpers.GetIntPropertyValue(_deviceHandle, IOKitHidConstants.MaxInputReportSize);
            OutputReportSize = IOKitHelpers.GetIntPropertyValue(_deviceHandle, IOKitHidConstants.MaxOutputReportSize);

            _log.LogInformation(
                "InputReportSize = {InputReportSize}; OutputReportSize = {OutputReportSize}",
                InputReportSize,
                OutputReportSize);
        }

        private void SetupConnection()
        {
            int deviceEntry = 0;
            try
            {
                IntPtr matchingDictionary = IORegistryEntryIDMatching((ulong)_entryId);
                deviceEntry = IOServiceGetMatchingService(0, matchingDictionary);

                if (deviceEntry == 0)
                {
                    _log.LogError("Failed to find a matching device entry in the IO registry.");
                    throw new PlatformApiException(ExceptionMessages.IOKitRegistryEntryNotFound);
                }

                _deviceHandle = IOHIDDeviceCreate(IntPtr.Zero, deviceEntry);

                if (_deviceHandle == IntPtr.Zero)
                {
                    _log.LogError("Failed to open the device handle.");
                    throw new PlatformApiException(ExceptionMessages.IOKitCannotOpenDevice);
                }

                int result = IOHIDDeviceOpen(_deviceHandle, 0);
                _log.IOKitApiCall(nameof(IOHIDDeviceOpen), (kern_return_t)result);

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
                    _log.LogDebug("Releasing deviceEntry object.");
                    _ = IOObjectRelease(deviceEntry);
                }
            }
        }

        /// <summary>
        /// Reads a report from the FIDO interface.
        /// </summary>
        /// <returns>
        /// A buffer that contains the data received from the device.
        /// </returns>
        /// <exception cref="PlatformApiException">
        /// Thrown when the underlying IOKit framework reports an error. See the exception message for details.
        /// </exception>
        public byte[] GetReport()
        {
            const int readBufferSize = 64;

            byte[] readBuffer = new byte[readBufferSize];
            GCHandle readHandle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);

            try
            {
                // Why are we using callbacks instead of directly reading the report? This is because it is stated in
                // Apple documentation here https://developer.apple.com/documentation/iokit/1588659-iohiddevicegetreport
                // that this async methods should be used for "input reports", which is the type of report frame that
                // FIDO uses.
                IntPtr reportCallback = Marshal.GetFunctionPointerForDelegate<IOHIDReportCallback>(ReportCallback);
                IOHIDDeviceRegisterInputReportCallback(
                    _deviceHandle,
                    readBuffer,
                    readBuffer.Length,
                    reportCallback,
                    GCHandle.ToIntPtr(readHandle));

                IntPtr callback = Marshal.GetFunctionPointerForDelegate<IOHIDCallback>(RemovalCallback);
                IOHIDDeviceRegisterRemovalCallback(_deviceHandle, callback, _deviceHandle);

                IntPtr runLoop = CFRunLoopGetCurrent();

                IOHIDDeviceScheduleWithRunLoop(_deviceHandle, runLoop, _loopId);

                // The YubiKey has a reclaim timeout of 3 seconds. This can cause the SDK some trouble if we just
                // switched out of a different USB interface (like Keyboard or CCID). We previously used a fairly
                // tight timeout of 4 seconds, but that seemed to not always work. 6 seconds (double the timeout)
                // seems like a more reasonable timeout for the operating system.
                int runLoopResult = CFRunLoopRunInMode(_loopId, 6, true);
                if (runLoopResult != kCFRunLoopRunHandledSource)
                {
                    throw new PlatformApiException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.WrongIOKitRunLoopMode,
                            runLoopResult));
                }

                IOHIDDeviceUnscheduleFromRunLoop(_deviceHandle, runLoop, _loopId);

                _log.SensitiveLogInformation(
                    "GetReport returned buffer: {Report}",
                    Hex.BytesToHex(readBuffer));

                // Return a copy of the report
                return readBuffer.ToArray();
            }
            finally
            {
                IOHIDDeviceRegisterInputReportCallback(
                    _deviceHandle,
                    readBuffer,
                    readBuffer.Length,
                    IntPtr.Zero,
                    IntPtr.Zero);

                IOHIDDeviceRegisterRemovalCallback(_deviceHandle, IntPtr.Zero, IntPtr.Zero);

                if (readHandle.IsAllocated)
                {
                    readHandle.Free();
                }
            }
        }

        /// <summary>
        /// The callback that is invoked when an input report is read.
        /// </summary>
        /// <param name="context">
        /// Callback context, in this case the buffer in which we wish to deposit the report.
        /// </param>
        /// <param name="result">
        /// Result of the GetReport operation.
        /// </param>
        /// <param name="sender">
        /// Ignore.
        /// </param>
        /// <param name="type">
        /// The type of the report that was read. Should always be Input Report type.
        /// </param>
        /// <param name="reportId">
        /// Report ID of the HID packet. Should always be non-zero.
        /// </param>
        /// <param name="report">
        /// The report buffer as delivered by the IOKit service. We need to copy this buffer.
        /// </param>
        /// <param name="reportLength">
        /// The length of the report buffer delivered to us by the IOKit service.
        /// </param>
        private static void ReportCallback(
            IntPtr context,
            int result,
            IntPtr sender,
            int type,
            int reportId,
            byte[] report,
            long reportLength)
        {
            Logger log = Log.GetLogger();

            log.LogInformation("MacOSHidIOReportConnection.ReportCallback has been called.");

            if (result != 0 || type != IOKitHidConstants.kIOHidReportTypeOutput || reportId != 0 || reportLength < 0)
            {
                // Something went wrong. We don't currently signal, just continue.
                log.LogWarning(
                    "ReportCallback did not receive some or all of the expected output.\n" +
                    "result = [{Result}], type = [{Type}], reportId = [{ReportId}], reportLength = [{ReportLength}]",
                    result,
                    type,
                    reportId,
                    reportLength);

                return;
            }

            byte[] buffer = (byte[])GCHandle.FromIntPtr(context).Target;
            long length = Math.Min(buffer.Length, reportLength);
            log.LogInformation(
                "Buffer length determined to be {Length} bytes. (buffer.Length was {BufferLength}, and reportLength was {ReportLength}",
                length,
                buffer.Length,
                reportLength);

            Array.Copy(report, buffer, length);
        }

        /// <summary>
        /// Called when the device has been removed and the connection class is still alive.
        /// </summary>
        /// <param name="context">
        /// The run loop which we need to stop.
        /// </param>
        /// <param name="result">
        /// Ignore.
        /// </param>
        /// <param name="sender">
        /// Ignore.
        /// </param>
        private static void RemovalCallback(IntPtr context, int result, IntPtr sender) =>
            CFRunLoopStop(context);

        /// <summary>
        /// Sends a buffer to the device.
        /// </summary>
        /// <param name="report">
        /// The buffer to send.
        /// </param>
        /// <exception cref="PlatformApiException">
        /// Thrown when the underlying IOKit framework reports an error. See the exception message for details.
        /// </exception>
        public void SetReport(byte[] report)
        {
            if (report is null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            _log.SensitiveLogInformation(
                "Calling SetReport with data: {Report}",
                Hex.BytesToHex(report));

            int result = IOHIDDeviceSetReport(
                _deviceHandle,
                IOKitHidConstants.kIOHidReportTypeOutput,
                0,
                report,
                report.Length);

            _log.IOKitApiCall(nameof(IOHIDDeviceSetReport), (kern_return_t)result);

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

        ~MacOSHidIOReportConnection()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
