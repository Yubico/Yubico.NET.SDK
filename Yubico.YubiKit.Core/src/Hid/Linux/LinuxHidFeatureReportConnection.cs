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

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.PlatformInterop;
using Yubico.YubiKit.Core.PlatformInterop.Linux.Libc;
using LibcNativeMethods = Yubico.YubiKit.Core.PlatformInterop.Linux.Libc.NativeMethods;

namespace Yubico.YubiKit.Core.Hid.Linux;

/// <summary>
///     Linux implementation of the keyboard feature report connection using hidraw ioctl.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxHidFeatureReportConnection : IHidConnectionSync
{
    private const int FeatureReportSize = 8;

    private readonly string _devNode;
    private readonly LinuxFileSafeHandle _handle;
    private bool _disposed;

    public LinuxHidFeatureReportConnection(string devNode)
    {
        _devNode = devNode;

        _handle = LibcNativeMethods.open(devNode, LibcNativeMethods.OpenFlags.O_RDWR);
        if (_handle.IsInvalid)
        {
            throw new PlatformApiException(
                nameof(LibcNativeMethods.open),
                Marshal.GetLastWin32Error(),
                $"Failed to open HID device: {devNode}. {LibcHelpers.GetErrnoString()}");
        }

        // Feature reports typically use a fixed size for YubiKey OTP
        InputReportSize = FeatureReportSize;
        OutputReportSize = FeatureReportSize;
    }

    public int InputReportSize { get; }
    public int OutputReportSize { get; }

    public byte[] GetReport()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Allocate buffer: first byte is the report ID, followed by data
        var buffer = new byte[FeatureReportSize + 1];
        buffer[0] = 0; // Report ID 0

        var bufferPtr = Marshal.AllocHGlobal(buffer.Length);
        try
        {
            Marshal.Copy(buffer, 0, bufferPtr, buffer.Length);

            // Build the ioctl request with buffer size
            long request = LibcNativeMethods.HIDIOCGFEATURE | ((long)buffer.Length << 16);
            int result = LibcNativeMethods.ioctl(_handle, request, bufferPtr);

            if (result < 0)
            {
                throw new PlatformApiException(
                    "ioctl(HIDIOCGFEATURE)",
                    Marshal.GetLastWin32Error(),
                    $"Failed to get feature report from HID device: {_devNode}. {LibcHelpers.GetErrnoString()}");
            }

            // Copy the result back, excluding the report ID byte
            var output = new byte[FeatureReportSize];
            Marshal.Copy(bufferPtr + 1, output, 0, FeatureReportSize);

            return output;
        }
        finally
        {
            Marshal.FreeHGlobal(bufferPtr);
        }
    }

    public void SetReport(byte[] report)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(report);

        // Allocate buffer: first byte is the report ID, followed by data
        var buffer = new byte[report.Length + 1];
        buffer[0] = 0; // Report ID 0
        Array.Copy(report, 0, buffer, 1, report.Length);

        var bufferPtr = Marshal.AllocHGlobal(buffer.Length);
        try
        {
            Marshal.Copy(buffer, 0, bufferPtr, buffer.Length);

            // Build the ioctl request with buffer size
            long request = LibcNativeMethods.HIDIOCSFEATURE | ((long)buffer.Length << 16);
            int result = LibcNativeMethods.ioctl(_handle, request, bufferPtr);

            if (result < 0)
            {
                throw new PlatformApiException(
                    "ioctl(HIDIOCSFEATURE)",
                    Marshal.GetLastWin32Error(),
                    $"Failed to set feature report on HID device: {_devNode}. {LibcHelpers.GetErrnoString()}");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(bufferPtr);
        }
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

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _handle.Dispose();
        }

        _disposed = true;
    }

    ~LinuxHidFeatureReportConnection()
    {
        Dispose(false);
    }
}
