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
///     Linux implementation of the FIDO IO report connection using hidraw.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxHidIOReportConnection : IHidConnection
{
    private const int FidoReportSize = 64;

    private readonly string _devNode;
    private readonly LinuxFileSafeHandle _handle;
    private bool _disposed;

    public LinuxHidIOReportConnection(string devNode)
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

        // Get the report sizes from the HID descriptor
        (InputReportSize, OutputReportSize) = GetReportSizes();
    }

    public int InputReportSize { get; }
    public int OutputReportSize { get; }

    public byte[] GetReport()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var buffer = new byte[InputReportSize];
        int bytesRead = LibcNativeMethods.read(_handle, buffer, buffer.Length);

        if (bytesRead < 0)
        {
            throw new PlatformApiException(
                nameof(LibcNativeMethods.read),
                Marshal.GetLastWin32Error(),
                $"Failed to read from HID device: {_devNode}. {LibcHelpers.GetErrnoString()}");
        }

        // Return only the bytes actually read
        if (bytesRead < buffer.Length)
        {
            var result = new byte[bytesRead];
            Array.Copy(buffer, result, bytesRead);
            return result;
        }

        return buffer;
    }

    public void SetReport(byte[] report)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(report);

        // On Linux hidraw, we need to prepend the report ID (0x00) before writing
        // See: https://www.kernel.org/doc/Documentation/hid/hidraw.txt
        var reportWithId = new byte[report.Length + 1];
        reportWithId[0] = 0x00; // Report ID
        report.AsSpan().CopyTo(reportWithId.AsSpan(1));

        int bytesWritten = LibcNativeMethods.write(_handle, reportWithId, reportWithId.Length);

        if (bytesWritten < 0)
        {
            throw new PlatformApiException(
                nameof(LibcNativeMethods.write),
                Marshal.GetLastWin32Error(),
                $"Failed to write to HID device: {_devNode}. {LibcHelpers.GetErrnoString()}");
        }

        if (bytesWritten != reportWithId.Length)
        {
            throw new PlatformApiException(
                nameof(LibcNativeMethods.write),
                0,
                $"Incomplete write to HID device: {_devNode}. Expected {reportWithId.Length} bytes, wrote {bytesWritten}.");
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

    private (int InputSize, int OutputSize) GetReportSizes()
    {
        // Try to get report sizes from HID descriptor
        var descSizePtr = Marshal.AllocHGlobal(LibcNativeMethods.DescriptorSizeSize);
        try
        {
            int result = LibcNativeMethods.ioctl(_handle, LibcNativeMethods.HIDIOCGRDESCSIZE, descSizePtr);
            if (result < 0)
            {
                // Fall back to FIDO standard size
                return (FidoReportSize, FidoReportSize);
            }

            int descSize = Marshal.ReadInt32(descSizePtr);
            if (descSize <= 0 || descSize > LibcNativeMethods.DescriptorSize - LibcNativeMethods.OffsetDescValue)
            {
                return (FidoReportSize, FidoReportSize);
            }

            var descPtr = Marshal.AllocHGlobal(LibcNativeMethods.DescriptorSize);
            try
            {
                Marshal.WriteInt32(descPtr, descSize);
                result = LibcNativeMethods.ioctl(_handle, LibcNativeMethods.HIDIOCGRDESC, descPtr);
                if (result < 0)
                {
                    return (FidoReportSize, FidoReportSize);
                }

                var descriptor = new byte[descSize];
                Marshal.Copy(descPtr + LibcNativeMethods.OffsetDescValue, descriptor, 0, descSize);

                return ParseReportSizes(descriptor);
            }
            finally
            {
                Marshal.FreeHGlobal(descPtr);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(descSizePtr);
        }
    }

    private static (int InputSize, int OutputSize) ParseReportSizes(ReadOnlySpan<byte> descriptor)
    {
        // Parse HID descriptor for report sizes
        // Looking for Input and Output main items with their associated Report Size and Report Count

        int inputSize = FidoReportSize;
        int outputSize = FidoReportSize;
        int currentReportSize = 0;
        int currentReportCount = 0;

        int i = 0;
        while (i < descriptor.Length)
        {
            byte prefix = descriptor[i];
            int size = prefix & 0x03;
            if (size == 3)
            {
                size = 4;
            }

            int type = (prefix >> 2) & 0x03;
            int tag = (prefix >> 4) & 0x0F;

            i++;

            if (i + size > descriptor.Length)
            {
                break;
            }

            uint value = 0;
            for (int j = 0; j < size; j++)
            {
                value |= (uint)descriptor[i + j] << (8 * j);
            }

            // Global items (type = 1)
            if (type == 1)
            {
                switch (tag)
                {
                    case 7: // Report Size
                        currentReportSize = (int)value;
                        break;
                    case 9: // Report Count
                        currentReportCount = (int)value;
                        break;
                }
            }
            // Main items (type = 0)
            else if (type == 0)
            {
                int reportBytes = (currentReportSize * currentReportCount + 7) / 8;
                switch (tag)
                {
                    case 8: // Input
                        if (reportBytes > 0)
                        {
                            inputSize = reportBytes;
                        }
                        break;
                    case 9: // Output
                        if (reportBytes > 0)
                        {
                            outputSize = reportBytes;
                        }
                        break;
                }
            }

            i += size;
        }

        return (inputSize, outputSize);
    }

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

    ~LinuxHidIOReportConnection()
    {
        Dispose(false);
    }
}
