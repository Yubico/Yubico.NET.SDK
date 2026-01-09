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

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// Wraps a synchronous HID feature report connection for OTP/Keyboard communication.
/// Provides async interface for 8-byte OTP feature reports.
/// </summary>
internal class OtpConnection(IHidConnectionSync syncConnection) : IOtpConnection
{
    private bool _disposed;

    public int FeatureReportSize => 8;
    public ConnectionType Type => ConnectionType.Hid;

    public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (report.Length != FeatureReportSize)
            throw new ArgumentException(
                $"OTP feature report must be exactly {FeatureReportSize} bytes, got {report.Length}",
                nameof(report));

        syncConnection.SetReport(report.ToArray());
        return Task.CompletedTask;
    }

    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var report = syncConnection.GetReport();
        if (report.Length != FeatureReportSize)
            throw new InvalidOperationException(
                $"Expected {FeatureReportSize}-byte report, got {report.Length} bytes");

        return Task.FromResult<ReadOnlyMemory<byte>>(report);
    }

    public void Dispose()
    {
        if (_disposed) return;

        syncConnection.Dispose();
        _disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
