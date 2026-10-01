// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Xunit.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Core.IntegrationTests.Transports.SmartCard;

public class PcscLifetimeIntegrationTests(ITestOutputHelper output) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await YubiKeyManager.ShutdownAsync();

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task PcscLifetime_AsyncTransactionReadAndReopen_ReadStableSerial()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var candidates = AuthorizedDevices.GetByConnectionType(ConnectionType.SmartCard)
            .Where(device => device.IsUsbTransport && device.SerialNumber is not null)
            .OrderByDescending(device => device.SerialNumber).ToList();
        Assert.NotEmpty(candidates);
        var selected = candidates[0];
        output.WriteLine($"Selected authorized USB smart-card device: serial={selected.SerialNumber}");

        for (var cycle = 0; cycle < 2; cycle++)
        {
            await using var connection = await selected.Device.ConnectAsync<ISmartCardConnection>(timeout.Token);
            IDisposable scope = await connection.BeginTransactionAsync(timeout.Token);
            try
            {
                var info = await ProtocolDeviceInfo.ReadAsync(connection, timeout.Token);
                Assert.Equal(selected.SerialNumber, info.SerialNumber);
            }
            finally
            {
                await ((IAsyncDisposable)scope).DisposeAsync();
            }
        }
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task PcscLifetime_SequentialTransactionsAndConnectionReopens_ReadStableDeviceInfo()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var cancellationToken = timeout.Token;
        var candidates = AuthorizedDevices
            .GetByConnectionType(ConnectionType.SmartCard)
            .Where(device => device.IsUsbTransport && device.SerialNumber is not null)
            .OrderByDescending(device => device.SerialNumber)
            .ToList();

        Assert.NotEmpty(candidates);
        var selected = candidates[0];
        var device = selected.Device;
        var expectedSerialNumber = selected.SerialNumber!.Value;
        var expectedFirmwareVersion = selected.FirmwareVersion;
        output.WriteLine(
            $"Selected authorized USB smart-card device: serial={expectedSerialNumber}, " +
            $"firmware={expectedFirmwareVersion}, formFactor={selected.FormFactor}");

        for (var connectionCycle = 0; connectionCycle < 3; connectionCycle++)
        {
            await using (var connection = await device
                             .ConnectAsync<ISmartCardConnection>(cancellationToken))
            {
                for (var transactionCycle = 0; transactionCycle < 2; transactionCycle++)
                {
                    using (connection.BeginTransaction(cancellationToken))
                    {
                        var deviceInfo = await ProtocolDeviceInfo.ReadAsync(connection, cancellationToken);
                        Assert.Equal(expectedSerialNumber, deviceInfo.SerialNumber);
                        Assert.Equal(expectedFirmwareVersion, deviceInfo.FirmwareVersion);
                    }
                }
            }
        }
    }
}
