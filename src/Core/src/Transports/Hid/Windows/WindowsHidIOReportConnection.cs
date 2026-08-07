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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Windows.HidD;

namespace Yubico.YubiKit.Core.Transports.Hid.Windows;

internal sealed class WindowsHidIOReportConnection : IHidConnection
{
    private readonly IHidDDevice _hidDDevice;
    private bool _disposed;

    internal WindowsHidIOReportConnection(string path)
        : this(new HidDDevice(path))
    {
    }

    /// <remarks>
    ///     Takes an already-constructed device so the open can be made failure-safe: if the open throws, this
    ///     constructor never completes, so nothing else can dispose the device and its native handle would leak.
    ///     The seam also lets the failure path be unit-tested without Windows hardware.
    /// </remarks>
    internal WindowsHidIOReportConnection(IHidDDevice hidDDevice)
    {
        _hidDDevice = hidDDevice;
        try
        {
            _hidDDevice.OpenIOConnection();

            // HidD report lengths include the report ID byte; IHidConnection sizes are payload-only.
            InputReportSize = _hidDDevice.InputReportByteLength - 1;
            OutputReportSize = _hidDDevice.OutputReportByteLength - 1;
        }
        catch
        {
            _hidDDevice.Dispose();
            throw;
        }
    }

    public int InputReportSize { get; }
    public int OutputReportSize { get; }
    public ConnectionType Type => ConnectionType.Hid;

    public byte[] GetReport()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _hidDDevice.GetInputReport();
    }

    public void SetReport(byte[] report)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(report);
        _hidDDevice.SetOutputReport(report);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _hidDDevice.Dispose();
        _disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}