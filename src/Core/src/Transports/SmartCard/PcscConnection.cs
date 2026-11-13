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

/// <summary>Exposes a USB or NFC PC/SC connection backed by a dedicated native-lifetime owner.</summary>
/// <remarks>
///     The owner confines native open, transmit, transaction and checked-release calls to one worker. One ordinary
///     raw operation is admitted at a time; overlap is rejected rather than queued. This is not a logical-exchange
///     or session guard: callers remain responsible for sequencing complete exchanges and avoiding concurrent session use.
///     Cancellation before dispatch submits no native call. Once dispatched, a call keeps its borrowed storage and
///     admission until native completion and may succeed after cancellation. Disposal drains the same owner;
///     unproven native release retains the rooted owner and reports failure instead of claiming safe reuse.
/// </remarks>
internal class PcscConnection : ISmartCardConnection
{
    private const int MaxExtendedApduResponseSize = 65536 + 2;
    private readonly IPcscDevice _smartCardDevice;
    private readonly ILogger<PcscConnection> _logger;
    private readonly SmartCardSharing _sharing;
    private readonly PcscConnectionNativeOwner _nativeOwner;

    /// <summary>Creates an unopened connection and roots its native-lifetime owner before acquisition.</summary>
    /// <param name="smartCardDevice">The reader identity and transport metadata; the connection does not dispose it.</param>
    /// <param name="connectionApi">The synchronous native seam, or <see langword="null" /> to use native PC/SC forwarding.</param>
    /// <param name="sharing">The opening policy; strict exclusive access is the default.</param>
    /// <param name="released">The observer invoked when no native resource remains unproven, including worker-start failure.</param>
    /// <param name="unrecovered">The observer used to quarantine retained ownership when native release is unproven.</param>
    /// <param name="startWorker">
    ///     An optional test seam that must start the supplied worker or throw before starting it.
    /// </param>
    /// <param name="workerLoopStarting">An optional test hook invoked on the worker before its loop starts.</param>
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

    /// <summary>Gets whether the successfully opened native handle uses exclusive sharing.</summary>
    /// <inheritdoc />
    /// <remarks>This is the negotiated handle mode, not the requested fallback policy, and is false before open succeeds.</remarks>
    public bool IsExclusive => _nativeOwner.IsExclusive;

    /// <summary>Requests shutdown without waiting and observes cleanup failure without throwing from finalization.</summary>
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

    /// <summary>Gets the SmartCard connection type without native I/O.</summary>
    public ConnectionType Type => ConnectionType.SmartCard;

    /// <summary>Gets NFC for an NFC reader and USB for USB, unknown or other reader kinds.</summary>
    public Transport Transport => _smartCardDevice.Kind switch
    {
        PscsConnectionKind.Nfc => Transport.Nfc,
        PscsConnectionKind.Usb => Transport.Usb,
        PscsConnectionKind.Unknown or PscsConnectionKind.Any => Transport.Usb,
        _ => Transport.Usb
    };

    /// <summary>Reports extended-APDU support only for a reader classified as USB, without probing the card.</summary>
    /// <returns><see langword="true" /> for USB; otherwise <see langword="false" />.</returns>
    public bool SupportsExtendedApdu() => _smartCardDevice.Kind == PscsConnectionKind.Usb;

    /// <summary>Awaits native context establishment and card connection on the owner's worker.</summary>
    /// <remarks>
    ///     Only the explicit fallback policy retries exclusive contention using shared access. Cancellation before
    ///     dispatch skips acquisition; after dispatch it does not interrupt native open. Failed initialization awaits
    ///     shutdown of partial resources without a timeout before reporting the original failure or unproven cleanup.
    /// </remarks>
    /// <param name="cancellationToken">The token checked before admission and again before native dispatch.</param>
    /// <exception cref="ConnectionInUseException">Strict exclusive opening encounters a sharing violation.</exception>
    /// <exception cref="UnrecoveredConnectionException">Initialization fails and partial native release remains unproven.</exception>
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

    /// <summary>Synchronously waits for a native transaction whose scope ends with LEAVE_CARD.</summary>
    /// <inheritdoc />
    /// <remarks>
    ///     Acquisition and synchronous scope disposal block on the owner worker without a timeout. The returned
    ///     scope also supports asynchronous disposal. A transaction does not reserve the ordinary-operation slot
    ///     for its holder, so callers must still exclude competing logical exchanges.
    /// </remarks>
    public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
        BeginTransaction(SCARD_DISPOSITION.LEAVE_CARD, cancellationToken);

    /// <summary>Synchronously waits for a native transaction with a caller-selected scope-end disposition.</summary>
    /// <param name="endDisposition">The card action requested by scope disposal; shutdown requests LEAVE_CARD if no end was requested.</param>
    /// <param name="cancellationToken">The token checked before admission and dispatch, not during native acquisition.</param>
    /// <returns>A scope implementing both synchronous and asynchronous disposal; end failures are logged.</returns>
    /// <exception cref="InvalidOperationException">Called on the native worker or while operation/transaction admission is unavailable.</exception>
    internal IDisposable BeginTransaction(
        SCARD_DISPOSITION endDisposition,
        CancellationToken cancellationToken = default) =>
        _nativeOwner.BeginTransaction(
            endDisposition,
            cancellationToken,
            result => _logger.LogDebug("SCardEndTransaction returned {Error}", result));

    /// <summary>Awaits a native transaction whose scope ends with LEAVE_CARD.</summary>
    /// <inheritdoc />
    /// <remarks>
    ///     A second begin or overlapping ordinary operation is rejected, not queued. Native acquisition may succeed
    ///     after cancellation; the returned scope must still be disposed. An active transaction allows sequential
    ///     ordinary operations and does not identify or exclude other logical callers.
    /// </remarks>
    public Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        _nativeOwner.BeginTransactionAsync(
            SCARD_DISPOSITION.LEAVE_CARD,
            cancellationToken,
            result => _logger.LogDebug("SCardEndTransaction returned {Error}", result));

    /// <summary>Awaits one native exchange and returns an independent copy of its raw response.</summary>
    /// <inheritdoc />
    /// <remarks>
    ///     Command memory is borrowed, not copied or cleared; keep it valid and unchanged until the returned task
    ///     completes, including after cancellation. The pooled native destination is cleared and returned in all
    ///     cases. Successful response storage belongs to the caller, who must clear sensitive data after use.
    ///     No APDU formatting, chaining or logical-exchange exclusion is supplied here.
    /// </remarks>
    /// <param name="command">The caller-owned, already formatted command bytes.</param>
    /// <param name="cancellationToken">The token checked before admission and dispatch; it does not abort a dispatched call.</param>
    /// <returns>Caller-owned raw response storage after the native borrow has ended.</returns>
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

    /// <summary>Stops admission and synchronously waits for the owner's shared shutdown outcome.</summary>
    /// <remarks>
    ///     There is no drain timeout: an admitted native call and transaction end must finish before checked
    ///     disconnection with UNPOWER_CARD and context release. Release failure faults disposal and retains uncertain
    ///     resources and ownership; a disposal exception is not release proof. Prefer <see cref="DisposeAsync" />.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Called on the native worker, where waiting would deadlock.</exception>
    public void Dispose()
    {
        if (_nativeOwner.IsWorkerThread)
            throw new InvalidOperationException("Synchronous disposal cannot wait on the native worker.");

        GC.SuppressFinalize(this);
        var shutdown = _nativeOwner.RequestShutdown();
        shutdown.GetAwaiter().GetResult();
    }

    /// <summary>Stops admission and asynchronously awaits the owner's shared shutdown outcome.</summary>
    /// <remarks>
    ///     Repeated disposal observes the same outcome. This wait has no cancellation token or timeout and does not
    ///     detach an admitted native call. Checked release failures are reported while uncertain resources and
    ///     ownership remain retained; shutdown completion alone must not be mistaken for successful release.
    /// </remarks>
    /// <returns>The shared shutdown completion, faulted if cleanup or its observers fail.</returns>
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