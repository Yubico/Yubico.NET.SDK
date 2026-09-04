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

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>One live enumerated PC/SC interface candidate that can open its raw smart-card connection.</summary>
internal sealed class PcscConnectionSlot : IYubiKeyConnectionSlot, IDiscoveryConnectionProvider
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<PcscConnectionSlot>();

    private readonly IPcscDevice _pcscDevice;
    private readonly ISmartCardConnectionFactory _smartCardConnectionFactory;

    internal PcscConnectionSlot(
        IPcscDevice pcscDevice,
        ISmartCardConnectionFactory smartCardConnectionFactory)
    {
        _pcscDevice = pcscDevice;
        _smartCardConnectionFactory = smartCardConnectionFactory;
        InterfaceId = $"pcsc:{pcscDevice.ReaderName}";
    }

    public string InterfaceId { get; }

    public ConnectionType ConnectionType => ConnectionType.SmartCard;

    public async Task<IConnection> OpenRawConnectionAsync(
        ConnectionType connection,
        CancellationToken cancellationToken = default)
    {
        if (connection != ConnectionType)
        {
            throw new NotSupportedException(
                $"Connection type {connection} is not supported by this device connection slot.");
        }

        var result = await _smartCardConnectionFactory
            .CreateAsync(_pcscDevice, cancellationToken)
            .ConfigureAwait(false);
        Logger.LogInformation("Connected to YubiKey in reader {ReaderName}", _pcscDevice.ReaderName);
        return result;
    }

    Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
        ConnectionType connection,
        CancellationToken cancellationToken) =>
        OpenRawConnectionAsync(connection, cancellationToken);
}