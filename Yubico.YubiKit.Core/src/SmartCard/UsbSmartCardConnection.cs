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
using System.Buffers;
using System.Globalization;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.PlatformInterop.Desktop.SCard;

namespace Yubico.YubiKit.Core.SmartCard;

/// <summary>
///     A smart card connection running over USB (via PC/SC).
/// </summary>
/// <remarks>
///     <para>
///         This class manages the lifecycle of PC/SC resources (<see cref="SCardContext" /> and
///         <see cref="SCardCardHandle" />).
///         It implements robust disposal patterns to ensure resources are released even if initialization fails.
///     </para>
///     <para>
///         By default, closing this connection leaves the card in its current state (
///         <see cref="SCARD_DISPOSITION.LEAVE_CARD" />)
///         to prevent sharing violations with other applications or subsequent tests.
///     </para>
/// </remarks>
internal class UsbSmartCardConnection(IPcscDevice smartCardDevice, ILogger<UsbSmartCardConnection>? logger = null)
    : ISmartCardConnection
{
    private readonly ILogger<UsbSmartCardConnection> _logger = logger ?? NullLogger<UsbSmartCardConnection>.Instance;
    private SCardCardHandle? _cardHandle;
    private SCardContext? _context;
    private bool _disposed;
    private SCARD_PROTOCOL? _protocol;
    private bool _transactionActive;

    public ConnectionType Type => ConnectionType.SmartCard;

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

    /// <summary>
    ///     Starts a PC/SC transaction with a specific disposition when it ends.
    /// </summary>
    /// <param name="endDisposition">The action to take on the card when the transaction ends.</param>
    /// <param name="cancellationToken">Token to cancel the transaction start.</param>
    /// <returns>A disposable scope that ends the transaction when disposed.</returns>
    public IDisposable BeginTransaction(SCARD_DISPOSITION endDisposition, CancellationToken cancellationToken = default)
        => BeginTransactionInternal(endDisposition, cancellationToken);

    private IDisposable BeginTransactionInternal(SCARD_DISPOSITION endDisposition, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(_cardHandle);

        if (_transactionActive)
            throw new InvalidOperationException("A card transaction is already active on this connection.");

        var scope = new TransactionScope(this, endDisposition);

        // BeginTransaction can block behind another process's transaction. Run it on a worker
        // to allow best-effort cancellation.
        var beginTask = Task.Run(() => NativeMethods.SCardBeginTransaction(_cardHandle!), ct);

        try
        {
            var ec = beginTask.GetAwaiter().GetResult();
            scope.MarkBeganOrThrow(ec);
        }
        catch (OperationCanceledException)
        {
            // Not holding a transaction; ensure scope doesn't try to end it.
            scope.Dispose();
            throw;
        }

        _transactionActive = true;
        return scope;
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

    #region Nested type: TransactionScope

    private sealed class TransactionScope(UsbSmartCardConnection owner, SCARD_DISPOSITION endDisposition)
        : IDisposable
    {
        private bool _began;
        private bool _ended;

        #region IDisposable Members

        public void Dispose()
        {
            if (_ended) return;
            _ended = true;

            // Only clear the flag if this specific scope is the active one
            if (owner._transactionActive)
                owner._transactionActive = false;

            if (_began && !owner._disposed && owner._cardHandle is not null && !owner._cardHandle.IsInvalid)
                try
                {
                    var result = NativeMethods.SCardEndTransaction(owner._cardHandle, endDisposition);
                    if (result != ErrorCode.SCARD_S_SUCCESS)
                        owner._logger.LogDebug("SCardEndTransaction returned {Error}", result);
                }
                catch (Exception ex)
                {
                    owner._logger.LogWarning(ex, "Failed to end transaction during scope dispose");
                }
        }

        #endregion

        public void MarkBeganOrThrow(uint ec)
        {
            if (ec != ErrorCode.SCARD_S_SUCCESS)
                throw new SCardException("ExceptionMessages.SCardBeginTransactionFailure", ec);
            _began = true;
        }
    }

    #endregion

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
            _logger.LogWarning(ex, "Failed to dispose SCard context for reader {ReaderName}",
                smartCardDevice.ReaderName);
        }

        _cardHandle = null;
        _context = null;
    }

    /// <summary>
    ///     Asynchronously disposes the connection, releasing all PC/SC resources.
    /// </summary>
    /// <remarks>
    ///     Offloads the synchronous disposal to a worker thread to avoid blocking the caller.
    /// </remarks>
    public ValueTask DisposeAsync() =>
        _disposed
            ? ValueTask.CompletedTask
            : new ValueTask(Task.Run(Dispose));

    public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ReadOnlyMemory<byte> command,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(_protocol);
        ArgumentNullException.ThrowIfNull(_context);
        ArgumentNullException.ThrowIfNull(_cardHandle);

        // Use a larger buffer for responses, especially for extended APDUs
        const int bufferSize = 65536;
        var outputBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        var bytesReceived = 0;

        try
        {
            var result = await Task.Run(() => NativeMethods.SCardTransmit(
                _cardHandle,
                new SCARD_IO_REQUEST(_protocol.Value),
                command.Span,
                nint.Zero,
                outputBuffer,
                out bytesReceived
            ), cancellationToken).ConfigureAwait(false);

            if (result != ErrorCode.SCARD_S_SUCCESS)
                throw new SCardException("ExceptionMessages.SCardTransmitFailure", result);

            var response = new byte[bytesReceived];
            outputBuffer.AsSpan(0, bytesReceived).CopyTo(response);

            return response;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(outputBuffer, true);
        }
    }

    public Transport Transport => Transport.Usb; // TODO determine transport, currently only supports USB

    public bool SupportsExtendedApdu() =>
        true; // TODO determine who supports extended APDUs https://yubico.atlassian.net/browse/YESDK-1499

    public IDisposable BeginTransaction(CancellationToken cancellationToken = default)
        => BeginTransactionInternal(SCARD_DISPOSITION.LEAVE_CARD, cancellationToken);

    #endregion
}