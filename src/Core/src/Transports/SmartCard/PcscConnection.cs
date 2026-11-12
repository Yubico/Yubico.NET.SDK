// Copyright 2025 Yubico AB
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
using System.Buffers;
using System.Runtime.ExceptionServices;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Desktop.SCard;

namespace Yubico.YubiKit.Core.Transports.SmartCard;

internal class PcscConnection : ISmartCardConnection
{
    private const int MaxExtendedApduResponseSize = 65536 + 2;
    private readonly IPcscDevice _smartCardDevice;
    private readonly ILogger<PcscConnection> _logger;
    private readonly SmartCardSharing _sharing;
    private readonly PcscConnectionNativeOwner _nativeOwner;

    internal PcscConnection(
        IPcscDevice smartCardDevice,
        ISCardConnectionApi? connectionApi = null,
        SmartCardSharing sharing = SmartCardSharing.Exclusive,
        Action? released = null,
        Action<Exception>? unrecovered = null,
        Action<Thread>? startWorker = null,
        Action? workerLoopStarting = null)
    {
        _smartCardDevice = smartCardDevice;
        _sharing = sharing;
        _logger = YubiKitLogging.CreateLogger<PcscConnection>();
        _nativeOwner = new PcscConnectionNativeOwner(
            smartCardDevice.ReaderName,
            connectionApi ?? new NativeSCardConnectionApi(),
            sharing,
            released,
            unrecovered,
            startWorker,
            workerLoopStarting);
    }

    /// <inheritdoc />
    public bool IsExclusive => _nativeOwner.IsExclusive;

    ~PcscConnection()
    {
        try
        {
            _ = ObserveFinalizerShutdownAsync(_nativeOwner.RequestShutdown());
        }
        catch
        {
            // A finalizer can request cleanup but must never terminate the process if scheduling cleanup fails.
        }
    }

    public ConnectionType Type => ConnectionType.SmartCard;

    public Transport Transport => _smartCardDevice.Kind switch
    {
        PscsConnectionKind.Nfc => Transport.Nfc,
        PscsConnectionKind.Usb => Transport.Usb,
        PscsConnectionKind.Unknown or PscsConnectionKind.Any => Transport.Usb,
        _ => Transport.Usb
    };

    public bool SupportsExtendedApdu() => _smartCardDevice.Kind == PscsConnectionKind.Usb;

    public async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Initializing smart card connection to reader {ReaderName}", _smartCardDevice.ReaderName);
            await _nativeOwner.OpenAsync(cancellationToken).ConfigureAwait(false);
            if (_sharing == SmartCardSharing.ExclusiveWithSharedFallback && !IsExclusive)
            {
                _logger.LogWarning(
                    "Failed to get exclusive access to reader {ReaderName}; using a shared connection. Card-wide security state is reachable from other smart card clients.",
                    _smartCardDevice.ReaderName);
            }

            _logger.LogDebug("Smart card connection initialized to reader {ReaderName}", _smartCardDevice.ReaderName);
        }
        catch (Exception initializationFailure)
        {
            try
            {
                await _nativeOwner.RequestShutdown().ConfigureAwait(false);
            }
            catch (Exception cleanupFailure)
            {
                if (!_nativeOwner.IsNativeReleaseProven)
                {
                    throw new UnrecoveredConnectionException(
                        $"The smart-card connection to reader '{_smartCardDevice.ReaderName}' failed to initialize " +
                        "and its partial native resources were not proven released.",
                        new AggregateException(initializationFailure, cleanupFailure));
                }
            }

            ExceptionDispatchInfo.Capture(initializationFailure).Throw();
            return;
        }
    }

    public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
        BeginTransaction(SCARD_DISPOSITION.LEAVE_CARD, cancellationToken);

    internal IDisposable BeginTransaction(
        SCARD_DISPOSITION endDisposition,
        CancellationToken cancellationToken = default) =>
        _nativeOwner.BeginTransaction(
            endDisposition,
            cancellationToken,
            result => _logger.LogDebug("SCardEndTransaction returned {Error}", result));

    public Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        _nativeOwner.BeginTransactionAsync(
            SCARD_DISPOSITION.LEAVE_CARD,
            cancellationToken,
            result => _logger.LogDebug("SCardEndTransaction returned {Error}", result));

    public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ReadOnlyMemory<byte> command,
        CancellationToken cancellationToken)
    {
        var outputBuffer = ArrayPool<byte>.Shared.Rent(MaxExtendedApduResponseSize);
        try
        {
            var responseLength = await _nativeOwner.RunAsync(
                () => _nativeOwner.Transmit(command.Span, outputBuffer),
                cancellationToken).ConfigureAwait(false);

            var response = new byte[responseLength];
            outputBuffer.AsSpan(0, responseLength).CopyTo(response);
            return response;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(outputBuffer, clearArray: true);
        }
    }

    public void Dispose()
    {
        if (_nativeOwner.IsWorkerThread)
            throw new InvalidOperationException("Synchronous disposal cannot wait on the native worker.");

        GC.SuppressFinalize(this);
        var shutdown = _nativeOwner.RequestShutdown();
        shutdown.GetAwaiter().GetResult();
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return new ValueTask(_nativeOwner.RequestShutdown());
    }

    private static async Task ObserveFinalizerShutdownAsync(Task shutdown)
    {
        try
        {
            await shutdown.ConfigureAwait(false);
        }
        catch
        {
            // Explicit disposal reports cleanup failures. A finalizer has no caller to receive them.
        }
    }
}