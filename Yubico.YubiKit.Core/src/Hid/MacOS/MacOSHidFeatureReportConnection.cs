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

using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.PlatformInterop;
using Yubico.YubiKit.Core.PlatformInterop.MacOS.IOKitFramework;
using IOKitNativeMethods = Yubico.YubiKit.Core.PlatformInterop.MacOS.IOKitFramework.NativeMethods;

namespace Yubico.YubiKit.Core.Hid.MacOS;

/// <summary>
///     macOS implementation of the keyboard feature report connection.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class MacOSHidFeatureReportConnection : IHidConnection
{
    private readonly long _entryId;
    private nint _deviceHandle;
    private bool _disposed;

    public MacOSHidFeatureReportConnection(long entryId)
    {
        _entryId = entryId;
        SetupConnection();

        InputReportSize = IOKitHelpers.GetIntPropertyValue(_deviceHandle, IOKitHidConstants.MaxInputReportSize);
        OutputReportSize = IOKitHelpers.GetIntPropertyValue(_deviceHandle, IOKitHidConstants.MaxOutputReportSize);
    }

    public int InputReportSize { get; }
    public int OutputReportSize { get; }

    public byte[] GetReport()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        const int featureReportSize = 8;

        var buffer = new byte[featureReportSize];
        long bufferSize = buffer.Length;

        var result = IOKitNativeMethods.IOHIDDeviceGetReport(
            _deviceHandle,
            IOKitHidConstants.kIOHidReportTypeFeature,
            0,
            buffer,
            ref bufferSize);

        if (result != 0)
            throw new PlatformApiException(
                nameof(IOKitNativeMethods.IOHIDDeviceGetReport),
                result,
                "Failed to get HID report.");

        return buffer;
    }

    public void SetReport(byte[] report)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var result = IOKitNativeMethods.IOHIDDeviceSetReport(
            _deviceHandle,
            IOKitHidConstants.kIOHidReportTypeFeature,
            0,
            report,
            report.Length);

        if (result != 0)
            throw new PlatformApiException(
                nameof(IOKitNativeMethods.IOHIDDeviceSetReport),
                result,
                "Failed to set HID report.");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync() =>
        _disposed
            ? ValueTask.CompletedTask
            : new ValueTask(Task.Run(Dispose));

    public ConnectionType Type { get; } = ConnectionType.Hid;

    private void SetupConnection()
    {
        var deviceEntry = 0;
        try
        {
            var matchingDictionary = IOKitNativeMethods.IORegistryEntryIDMatching((ulong)_entryId);
            deviceEntry = IOKitNativeMethods.IOServiceGetMatchingService(0, matchingDictionary);

            if (deviceEntry == 0)
                throw new PlatformApiException("Failed to find matching device entry in IO registry.");

            _deviceHandle = IOKitNativeMethods.IOHIDDeviceCreate(IntPtr.Zero, deviceEntry);

            if (_deviceHandle == IntPtr.Zero) throw new PlatformApiException("Failed to create HID device handle.");

            var result = IOKitNativeMethods.IOHIDDeviceOpen(_deviceHandle, 0);

            if (result != 0)
                throw new PlatformApiException(
                    nameof(IOKitNativeMethods.IOHIDDeviceOpen),
                    result,
                    "Failed to open HID device.");
        }
        finally
        {
            if (deviceEntry != 0) _ = IOKitNativeMethods.IOObjectRelease(deviceEntry);
        }
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (_deviceHandle != IntPtr.Zero)
        {
            _ = IOKitNativeMethods.IOHIDDeviceClose(_deviceHandle, 0);
            _deviceHandle = IntPtr.Zero;
        }

        _disposed = true;
    }

    ~MacOSHidFeatureReportConnection()
    {
        Dispose(false);
    }
}