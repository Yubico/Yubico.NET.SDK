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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Native.MacOS.IOKitFramework;
using IOKitNativeMethods = Yubico.YubiKit.Core.Native.MacOS.IOKitFramework.NativeMethods;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

/// <summary>
///     macOS implementation of the keyboard feature report connection.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class MacOSHidFeatureReportConnection : IHidConnection
{
    private readonly long _entryId;
    private readonly IIOKitDeviceLifetime _lifetime;
    private nint _deviceHandle;
    private bool _disposed;

    public MacOSHidFeatureReportConnection(long entryId)
        : this(entryId, IOKitDeviceLifetime.Instance)
    {
    }

    /// <summary>
    ///     Test seam. Lets the constructor-failure and disposal paths be exercised without macOS hardware.
    /// </summary>
    internal MacOSHidFeatureReportConnection(long entryId, IIOKitDeviceLifetime lifetime)
    {
        _entryId = entryId;
        _lifetime = lifetime;

        // A constructor that acquires a native resource and then throws leaves nothing for the caller to
        // dispose: the object never finishes construction, so no using, finally, or factory catch can reach
        // it. Release here or the IOHIDDeviceRef leaks for the process lifetime.
        try
        {
            SetupConnection();

            InputReportSize = _lifetime.GetIntProperty(_deviceHandle, IOKitHidConstants.MaxInputReportSize);
            OutputReportSize = _lifetime.GetIntProperty(_deviceHandle, IOKitHidConstants.MaxOutputReportSize);
        }
        catch
        {
            Dispose(false);
            GC.SuppressFinalize(this);
            throw;
        }
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
        _deviceHandle = _lifetime.CreateDevice(_entryId);
        _lifetime.OpenDevice(_deviceHandle);
    }

    private void Dispose(bool disposing)
    {
        // Set first: this runs from the failing-constructor path as well as from Dispose and the finalizer,
        // and every CoreFoundation object below must be released exactly once. Over-releasing corrupts a
        // retain count just as surely as never releasing leaks.
        if (_disposed) return;
        _disposed = true;

        if (_deviceHandle != IntPtr.Zero)
        {
            _lifetime.CloseDevice(_deviceHandle);

            // IOHIDDeviceCreate returns a retained CoreFoundation object. Closing it is not releasing it.
            _lifetime.ReleaseCFObject(_deviceHandle);
            _deviceHandle = IntPtr.Zero;
        }
    }

    ~MacOSHidFeatureReportConnection()
    {
        Dispose(false);
    }
}