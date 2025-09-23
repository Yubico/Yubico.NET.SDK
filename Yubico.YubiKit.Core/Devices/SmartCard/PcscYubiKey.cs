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

namespace Yubico.YubiKit.Core.Devices.SmartCard;

internal class PcscYubiKey : IYubiKey
{
    private readonly IConnectionFactory<ISmartCardConnection> _connectionFactory;
    private readonly ILogger<PcscYubiKey> _logger;
    private readonly ISmartCardDevice _pcscDevice;
    private ISmartCardConnection? _connection;

    private PcscYubiKey(
        ILogger<PcscYubiKey> logger,
        ISmartCardDevice pcscDevice,
        IConnectionFactory<ISmartCardConnection> connectionFactory)
    {
        _logger = logger;
        _pcscDevice = pcscDevice;
        _connectionFactory = connectionFactory;
    }

    #region IYubiKey Members

    public async Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : IYubiKeyConnection
    {
        if (typeof(TConnection) != typeof(ISmartCardConnection))
            throw new NotSupportedException(
                $"Connection type {typeof(TConnection).Name} is not supported by this YubiKey device.");

        _connection = await _connectionFactory.CreateAsync(_pcscDevice);
        return (TConnection)_connection;
    }

    #endregion

    // Move this to service later
    public static async Task<IReadOnlyList<PcscYubiKey>> GetAllAsync() => await Task.Run(GetAll);

    private static IReadOnlyList<PcscYubiKey> GetAll()
    {
        var result = NativeMethods.SCardEstablishContext(SCARD_SCOPE.USER, out var context);
        if (result != ErrorCode.SCARD_S_SUCCESS)
            throw new InvalidOperationException("Can't establish context with PC/SC service.");

        result = NativeMethods.SCardListReaders(context, null, out var readerNames);
        if (result != ErrorCode.SCARD_S_SUCCESS || readerNames.Length == 0) return [];

        var readerStates = SCARD_READER_STATE.CreateMany(readerNames);
        result = NativeMethods.SCardGetStatusChange(
            context,
            0,
            readerStates,
            readerStates.Length);

        if (result != ErrorCode.SCARD_S_SUCCESS)
            throw new InvalidOperationException($"SCardGetStatusChange failed: 0x{result:X8}");

        try
        {
            return (from reader in readerStates
                where (reader.GetEventState() & SCARD_STATE.PRESENT) != 0
                where ProductAtrs.AllYubiKeys.Contains(reader.GetAtr())
                select new PcscDevice
                {
                    ReaderName = reader.GetReaderName(), Atr = reader.GetAtr(), Kind = SmartCardConnectionKind.Usb
                }
                into pcscDevice
                select new PcscYubiKey(NullLogger<PcscYubiKey>.Instance, pcscDevice,
                    new ConnectionFactory<ISmartCardConnection>(NullLoggerFactory.Instance))).ToList();
        }
        finally
        {
            context.Dispose();
        }
    }
}