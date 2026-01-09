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

internal class HidConnection(IHidConnectionSync syncConnection) : IHidConnection
{
    private bool _disposed;

    public int InputReportSize => syncConnection.InputReportSize;
    public int OutputReportSize => syncConnection.OutputReportSize;

    public ConnectionType Type => ConnectionType.Hid;

    public Task SetReportAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        syncConnection.SetReport(report.ToArray());
        return Task.CompletedTask;
    }

    public Task<ReadOnlyMemory<byte>> GetReportAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var report = syncConnection.GetReport();
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