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
using System.Globalization;
using Yubico.YubiKit.Core.PlatformInterop.Desktop.SCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.SmartCard;

public interface IConnection : IDisposable
{
}

public interface ISmartCardConnection : IConnection
{
    Transport Transport { get; }
    // IDisposable BeginTransaction(out bool cardWasReset);

    Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ReadOnlyMemory<byte> command,
        CancellationToken cancellationToken = default);

    bool SupportsExtendedApdu();
    // byte[] getAtr();
}

internal class UsbSmartCardConnection(IPcscDevice smartCardDevice, ILogger<UsbSmartCardConnection>? logger = null)
    : ISmartCardConnection
{
    private readonly ILogger<UsbSmartCardConnection> _logger = logger ?? NullLogger<UsbSmartCardConnection>.Instance;
    private SCardCardHandle? _cardHandle;
    private SCardContext? _context;
    private bool _disposed;
    private SCARD_PROTOCOL? _protocol;
    private bool _transactionActive;

    #region ISmartCardConnection Members

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_transactionActive && _cardHandle is not null && !_cardHandle.IsInvalid)
        {
            try
            {
                var result = NativeMethods.SCardEndTransaction(_cardHandle, SCARD_DISPOSITION.LEAVE_CARD);
                if (result != ErrorCode.SCARD_S_SUCCESS)
                    _logger.LogDebug("SCardEndTransaction returned {ErrorCode} during dispose", result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to end transaction during dispose");
            }
            _transactionActive = false;
        }

        try
        {
            _cardHandle?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dispose card handle for reader {ReaderName}", smartCardDevice.ReaderName);
        }

        try
        {
            _context?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dispose SCard context for reader {ReaderName}", smartCardDevice.ReaderName);
        }

        _cardHandle = null;
        _context = null;
    }

    public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ReadOnlyMemory<byte> command,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(_protocol);
        ArgumentNullException.ThrowIfNull(_context);
        ArgumentNullException.ThrowIfNull(_cardHandle);

        var outputBuffer = new byte[512]; // TODO. How will we know expected outputBuffer size?
        var bytesReceived = 0;
        var buffer = outputBuffer;

        var result = await Task.Run(() => NativeMethods.SCardTransmit(
            _cardHandle,
            new SCARD_IO_REQUEST(_protocol.Value),
            command.Span,
            nint.Zero,
            buffer,
            out bytesReceived
        ), cancellationToken).ConfigureAwait(false);

        if (result != ErrorCode.SCARD_S_SUCCESS)
            throw new SCardException("ExceptionMessages.SCardTransmitFailure, result");

        Array.Resize(ref outputBuffer, bytesReceived);

        return (ReadOnlyMemory<byte>)outputBuffer;
    }

    public Transport Transport => Transport.Usb; // TODO determine transport, currently only supports USB
    public bool SupportsExtendedApdu() => true; // TODO determine who supports extended APDUs https://yubico.atlassian.net/browse/YESDK-1499

    #endregion

    public async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Initializing smart card connection to reader {ReaderName}", smartCardDevice.ReaderName);

        try
        {
            await Task.Run(() =>
            {
                (_context, _cardHandle, _protocol) = GetConnection(smartCardDevice.ReaderName);
            }, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Smart card connection initialized to reader {ReaderName}", smartCardDevice.ReaderName);
        }
        catch
        {
            _cardHandle?.Dispose();
            _context?.Dispose();
            _cardHandle = null;
            _context = null;
            throw;
        }
    }

    private static (SCardContext Context, SCardCardHandle CardHandle, SCARD_PROTOCOL Protocol)
        GetConnection(string readerName)
    {
        var result = NativeMethods.SCardEstablishContext(SCARD_SCOPE.USER, out var context);
        if (result != ErrorCode.SCARD_S_SUCCESS)
            throw new SCardException(
                "ExceptionMessages.SCardCantEstablish",
                result);

        try
        {
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
                        "ExceptionMessages.SCardCardCantConnect {0}",
                        readerName),
                    result);

            return (context, cardHandle, activeProtocol);
        }
        catch
        {
            context.Dispose();
            throw;
        }
    }


}