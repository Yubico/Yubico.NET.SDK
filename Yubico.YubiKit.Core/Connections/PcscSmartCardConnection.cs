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
using System.Globalization;
using Yubico.YubiKit.Core.Core;
using Yubico.YubiKit.Core.Core.Devices.SmartCard;
using Yubico.YubiKit.Core.Core.Iso7816;
using Yubico.YubiKit.Core.PlatformInterop.Desktop.SCard;

namespace Yubico.YubiKit.Core.Connections;

internal class PcscSmartCardConnection : ISmartCardConnection
{
    private readonly ILogger<PcscSmartCardConnection> _logger;
    private readonly ISmartCardDevice _smartCardDevice;
    private SCardContext? _context;
    private SCardCardHandle? _cardHandle;
    private SCARD_PROTOCOL? _protocol;
    private bool _disposed;
    
    public async Task<PcscSmartCardConnection> CreateAsync(
        ILogger<PcscSmartCardConnection> logger,
        ISmartCardDevice smartCardDevice,
        CancellationToken cancellationToken = default)
    {
        var connection = new PcscSmartCardConnection(logger, smartCardDevice);
        await connection.InitializeAsync();
        
        return connection;
    }

    internal PcscSmartCardConnection(
        ILogger<PcscSmartCardConnection> logger,
        ISmartCardDevice smartCardDevice)
    {
        _logger = logger;
        _smartCardDevice = smartCardDevice;
    }

    public Task<ResponseApdu> TransmitAndReceiveAsync(
        CommandApdu command,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private ValueTask InitializeAsync()
    {
        Task task = Task.Run(() =>
        {
            (_context, _cardHandle, _protocol) = GetConnection(_smartCardDevice.ReaderName);
        }, CancellationToken.None);
        
        return new ValueTask(task);
    }
    
    private static (SCardContext Context, SCardCardHandle CardHandle, SCARD_PROTOCOL Protocol) GetConnection(string readerName)
    {
        uint result = NativeMethods.SCardEstablishContext(SCARD_SCOPE.USER, out SCardContext? context);
        if (result != ErrorCode.SCARD_S_SUCCESS)
        {
            throw new SCardException(
                "ExceptionMessages.SCardCantEstablish",
                result);
        }

        var shareMode = SCARD_SHARE.SHARED;
        if (AppContext.TryGetSwitch(CoreCompatSwitches.OpenSmartCardHandlesExclusively, out bool isEnabled) &&
            isEnabled)
        {
            shareMode = SCARD_SHARE.EXCLUSIVE;
        }

        result = NativeMethods.SCardConnect(
            context,
            readerName,
            shareMode,
            SCARD_PROTOCOL.Tx,
            out SCardCardHandle? cardHandle,
            out SCARD_PROTOCOL activeProtocol);

        if (result != ErrorCode.SCARD_S_SUCCESS)
        {
            throw new SCardException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "ExceptionMessages.SCardCardCantConnect",
                    readerName),
                result);
        }

        return (context, cardHandle, activeProtocol);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cardHandle?.Dispose();
        _context?.Dispose();
        
        _cardHandle = null!;
        _context = null!;
        
        _disposed = true;
    }
}