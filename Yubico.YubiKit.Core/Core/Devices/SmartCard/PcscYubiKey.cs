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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Connections;
using Yubico.YubiKit.Core.PlatformInterop.Desktop.SCard;

namespace Yubico.YubiKit.Core.Core.Devices.SmartCard;

internal class PcscYubiKey : IYubiKey
{
    private readonly ILogger<PcscYubiKey> _logger;
    private ISmartCardDevice _pcscDevice;
    private readonly IConnectionFactory _connectionFactory;
    private ISmartCardConnection? _connection;

    internal PcscYubiKey(
        ILogger<PcscYubiKey> logger,
        PcscDevice pcscDevice,
        IConnectionFactory connectionFactory)
    {
        _logger = logger;
        _pcscDevice = pcscDevice;
        _connectionFactory = connectionFactory;
    }

    // Move this to service later
    public static async Task<IReadOnlyList<PcscYubiKey>> GetAllAsync()
    {
        return await Task.Run(GetAll);
    }

    private static IReadOnlyList<PcscYubiKey> GetAll()
    {
        uint result = NativeMethods.SCardEstablishContext(SCARD_SCOPE.USER, out SCardContext context);
        if (result != ErrorCode.SCARD_S_SUCCESS)
        {
            throw new InvalidOperationException("Can't establish context with PC/SC service.");
        }

        result = NativeMethods.SCardListReaders(context, null, out string[] readerNames);
        if (result != ErrorCode.SCARD_S_SUCCESS || readerNames.Length == 0)
        {
            return [];
        }

        var readerStates = SCARD_READER_STATE.CreateMany(readerNames);
        result = NativeMethods.SCardGetStatusChange(
            context,
            0,
            readerStates,
            readerStates.Length);

        if (result != ErrorCode.SCARD_S_SUCCESS)
        {
            throw new InvalidOperationException($"SCardGetStatusChange failed: 0x{result:X8}");
        }

        var yubikeys = new List<PcscYubiKey>();
        foreach (var reader in readerStates)
        {
            if ((reader.GetEventState() & SCARD_STATE.PRESENT) == 0)
            {
                continue; // No card in reader
            }

            if (!ProductAtrs.AllYubiKeys.Contains(reader.GetAtr()))
            {
                continue; // Not a YubiKey
            }

            var pcscDevice = new PcscDevice { ReaderName = reader.GetReaderName(), Atr = reader.GetAtr(), Kind = SmartCardConnectionKind.Usb };
            var yubikey = new PcscYubiKey(
                NullLogger<PcscYubiKey>.Instance,
                pcscDevice,
                new ConnectionFactory(NullLoggerFactory.Instance)
            );
            
            yubikeys.Add(yubikey);
        }

        return yubikeys;
    }

    public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : IYubiKeyConnection
    {
        TConnection connection = _connectionFactory.Create<TConnection>(_pcscDevice);
        _connection = connection as ISmartCardConnection;
        return Task.FromResult(connection);
    }
}