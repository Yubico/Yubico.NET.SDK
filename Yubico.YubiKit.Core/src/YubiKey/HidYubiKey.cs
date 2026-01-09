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

using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Hid;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.YubiKey;

internal class HidYubiKey(
    IHidDevice hidDevice,
    ILogger<HidYubiKey> logger)
    : IYubiKey
{
    #region IYubiKey Members

    public string DeviceId { get; } = 
        $"hid:{hidDevice.VendorId:X4}:{hidDevice.ProductId:X4}:{hidDevice.Usage:X4}";

    public async Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : class, IConnection
    {
        if (typeof(TConnection) != typeof(IAsyncHidConnection))
            throw new NotSupportedException(
                $"Connection type {typeof(TConnection).Name} is not supported by this YubiKey device.");

        var connection = await CreateConnection(cancellationToken).ConfigureAwait(false);
        return connection as TConnection ??
               throw new InvalidOperationException("Connection is not of the expected type.");
    }

    #endregion

    private async Task<IAsyncHidConnection> CreateConnection(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Make async
        
        logger.LogInformation(
            "Connecting to HID YubiKey VID={VendorId:X4} PID={ProductId:X4} Usage={Usage:X4}",
            hidDevice.VendorId,
            hidDevice.ProductId,
            hidDevice.Usage);

        IHidConnection syncConnection = hidDevice.UsagePage switch
        {
            HidUsagePage.Fido => hidDevice.ConnectToIOReports(),
            HidUsagePage.Keyboard => hidDevice.ConnectToFeatureReports(),
            _ => throw new NotSupportedException($"HID usage page {hidDevice.UsagePage} is not supported.")
        };

        return new AsyncHidConnectionWrapper(syncConnection);
    }

    public static HidYubiKey Create(IHidDevice hidDevice, ILogger<HidYubiKey>? logger) => 
        new(hidDevice, logger ?? NullLogger<HidYubiKey>.Instance);
}

internal class AsyncHidConnectionWrapper(IHidConnection syncConnection) : IAsyncHidConnection
{
    private bool _disposed;

    public int InputReportSize => syncConnection.InputReportSize;
    public int OutputReportSize => syncConnection.OutputReportSize;

    public Task SetReportAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        syncConnection.SetReport(report.ToArray());
        return Task.CompletedTask;
    }

    public Task<ReadOnlyMemory<byte>> GetReportAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        byte[] report = syncConnection.GetReport();
        return Task.FromResult<ReadOnlyMemory<byte>>(report);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        syncConnection.Dispose();
        _disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
