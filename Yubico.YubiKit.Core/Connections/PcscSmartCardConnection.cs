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
using Yubico.YubiKit.Core.Devices.SmartCard;
using Yubico.YubiKit.Core.PlatformInterop.Desktop.SCard;

namespace Yubico.YubiKit.Core.Connections;

internal class PcscSmartCardConnection : ISmartCardConnection
{
    private readonly ILogger<PcscSmartCardConnection> _logger;
    private readonly IPcscDevice _smartCardDevice;
    private SCardCardHandle? _cardHandle;
    private SCardContext? _context;
    private bool _disposed;
    private SCARD_PROTOCOL? _protocol;

    internal PcscSmartCardConnection(
        ILogger<PcscSmartCardConnection> logger,
        IPcscDevice smartCardDevice)
    {
        _logger = logger;
        _smartCardDevice = smartCardDevice;
    }

    #region ISmartCardConnection Members

    public void Dispose()
    {
        if (_disposed) return;

        _cardHandle?.Dispose();
        _context?.Dispose();
        _cardHandle = null!;
        _context = null!;
        _disposed = true;
    }

    public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ReadOnlyMemory<byte> command,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(_protocol);
        ArgumentNullException.ThrowIfNull(_context);
        ArgumentNullException.ThrowIfNull(_cardHandle);

        var outputBuffer = new byte[512]; // Todo, wont work for large APDUs
        var bytesReceived = 0;
        var buffer = outputBuffer;

        var result = await Task.Run(() => NativeMethods.SCardTransmit(
            _cardHandle,
            new SCARD_IO_REQUEST(_protocol.Value),
            command.Span,
            IntPtr.Zero,
            buffer,
            out bytesReceived
        ), cancellationToken);

        if (result != ErrorCode.SCARD_S_SUCCESS)
            throw new SCardException("ExceptionMessages.SCardTransmitFailure, result");

        Array.Resize(ref outputBuffer, bytesReceived);

        return (ReadOnlyMemory<byte>)outputBuffer;
    }

    #endregion


    public ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        var task = Task.Run(() =>
        {
            (_context, _cardHandle, _protocol) = GetConnection(_smartCardDevice.ReaderName);
        }, cancellationToken);

        return new ValueTask(task);
    }

    private static (SCardContext Context, SCardCardHandle CardHandle, SCARD_PROTOCOL Protocol)
    GetConnection(string readerName)
    {
        var result = NativeMethods.SCardEstablishContext(SCARD_SCOPE.USER, out var context);
        if (result != ErrorCode.SCARD_S_SUCCESS)
            throw new SCardException(
                "ExceptionMessages.SCardCantEstablish",
                result);

        var shareMode = SCARD_SHARE.SHARED;
        if (AppContext.TryGetSwitch(CoreCompatSwitches.OpenSmartCardHandlesExclusively, out var isEnabled) &&
            isEnabled)
            shareMode = SCARD_SHARE.EXCLUSIVE;

        result = NativeMethods.SCardConnect(
            context,
            readerName,
            shareMode,
            SCARD_PROTOCOL.Tx,
            out var cardHandle,
            out var activeProtocol);

        if (result != ErrorCode.SCARD_S_SUCCESS)
            throw new SCardException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "ExceptionMessages.SCardCardCantConnect",
                    readerName),
                result);

        return (context, cardHandle, activeProtocol);
    }
}