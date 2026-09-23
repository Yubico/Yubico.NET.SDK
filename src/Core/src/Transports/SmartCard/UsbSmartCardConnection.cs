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

internal class UsbSmartCardConnection : ISmartCardConnection
{
    private const int MaxExtendedApduResponseSize = 65536 + 2;
    private readonly IPcscDevice _smartCardDevice;
    private readonly ILogger<UsbSmartCardConnection> _logger;
    private readonly PcscConnectionNativeState _nativeState;

    internal UsbSmartCardConnection(
        IPcscDevice smartCardDevice,
        ISCardConnectionApi? api = null,
        Action? released = null,
        Action<Exception>? unrecovered = null,
        Action<Thread>? startWorker = null,
        Action? workerLoopStarting = null)
    {
        _smartCardDevice = smartCardDevice;
        _logger = YubiKitLogging.CreateLogger<UsbSmartCardConnection>();
        _nativeState = new PcscConnectionNativeState(
            smartCardDevice.ReaderName,
            api ?? new NativeSCardConnectionApi(),
            released,
            unrecovered,
            startWorker,
            workerLoopStarting);
    }

    ~UsbSmartCardConnection()
    {
        try
        {
            _ = ObserveFinalizerShutdownAsync(_nativeState.RequestShutdown());
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
            await _nativeState.OpenAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Smart card connection initialized to reader {ReaderName}", _smartCardDevice.ReaderName);
        }
        catch (Exception initializationFailure)
        {
            try
            {
                await _nativeState.RequestShutdown().ConfigureAwait(false);
            }
            catch (Exception cleanupFailure)
            {
                if (!_nativeState.IsNativeReleaseProven)
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
        _nativeState.BeginTransaction(
            endDisposition,
            cancellationToken,
            result => _logger.LogDebug("SCardEndTransaction returned {Error}", result));

    public Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        _nativeState.BeginTransactionAsync(
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
            var responseLength = await _nativeState.RunAsync(
                () => _nativeState.Transmit(command.Span, outputBuffer),
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
        if (_nativeState.IsWorkerThread)
            throw new InvalidOperationException("Synchronous disposal cannot wait on the native worker.");

        GC.SuppressFinalize(this);
        var shutdown = _nativeState.RequestShutdown();
        shutdown.GetAwaiter().GetResult();
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return new ValueTask(_nativeState.RequestShutdown());
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
