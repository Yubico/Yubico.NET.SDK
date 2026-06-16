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
using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;

namespace Yubico.YubiKit.Core.Transports.SmartCard;

/// <summary>
/// Monitors for SmartCard reader arrival and removal using PC/SC SCardGetStatusChange.
/// </summary>
/// <remarks>
/// Uses a dedicated background thread with 1000ms timeout for responsive cancellation.
/// On Windows, also watches for PnP notifications via the special <c>\\?\PnP?\Notification</c> reader.
/// <para>
/// The listener does not auto-start. Call <see cref="Start"/> after setting up <see cref="DeviceEvent"/>
/// callback. The listener establishes baseline during <see cref="Start"/> to avoid duplicate events.
/// </para>
/// </remarks>
public sealed class DesktopSmartCardDeviceListener : ISmartCardDeviceListener
{
    private const string PnpNotificationReaderName = @"\\?PnP?\Notification";
    private static readonly TimeSpan CheckForChangesWaitTime = TimeSpan.FromMilliseconds(1000);
    private static readonly TimeSpan MaxDisposalWaitTime = TimeSpan.FromSeconds(8);

    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<DesktopSmartCardDeviceListener>();

    private readonly ISCardApi _sCardApi;
    private readonly Action<TimeSpan> _sleep;
    private readonly Lock _syncLock = new();
    private SCardContext? _context;
    private Thread? _listenerThread;
    private HashSet<string>? _knownReaders;
    private volatile bool _shouldStop;
    private bool _disposed;

    /// <inheritdoc />
    public Action? DeviceEvent { get; set; }

    /// <inheritdoc />
    public DeviceListenerStatus Status { get; private set; } = DeviceListenerStatus.Stopped;

    /// <summary>
    /// Creates a new instance. The listener does not start automatically - call <see cref="Start"/>
    /// after setting up the <see cref="DeviceEvent"/> callback.
    /// </summary>
    public DesktopSmartCardDeviceListener() : this(NativeSCardApi.Instance, Thread.Sleep)
    {
        // Lazy start - do nothing in constructor
    }

    internal DesktopSmartCardDeviceListener(ISCardApi sCardApi, Action<TimeSpan> sleep)
    {
        ArgumentNullException.ThrowIfNull(sCardApi);
        ArgumentNullException.ThrowIfNull(sleep);

        _sCardApi = sCardApi;
        _sleep = sleep;
    }

    /// <inheritdoc />
    public void Start()
    {
        lock (_syncLock)
        {
            if (Status == DeviceListenerStatus.Started)
            {
                return;
            }

            try
            {
                var result = _sCardApi.SCardEstablishContext(SCARD_SCOPE.USER, out var context);
                if (result != ErrorCode.SCARD_S_SUCCESS)
                {
                    Logger.LogWarning("Failed to establish SCard context: 0x{Result:X8}", result);
                    Status = DeviceListenerStatus.Error;
                    return;
                }

                _context = context;
                _shouldStop = false;

                // Establish baseline of currently connected readers BEFORE starting thread
                EstablishBaseline();

                _listenerThread = new Thread(ListenerThreadProc)
                {
                    Name = "SmartCardDeviceListener",
                    IsBackground = true
                };
                _listenerThread.Start();

                Status = DeviceListenerStatus.Started;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to start SmartCard listener");
                Status = DeviceListenerStatus.Error;
            }
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        lock (_syncLock)
        {
            if (Status == DeviceListenerStatus.Stopped)
            {
                return;
            }
        }

        StopListening();

        lock (_syncLock)
        {
            _knownReaders = null;
            Status = DeviceListenerStatus.Stopped;
        }
    }

    private void EstablishBaseline()
    {
        if (_context is null || _context.IsInvalid)
        {
            _knownReaders = [];
            return;
        }

        var result = _sCardApi.SCardListReaders(_context, null, out var currentReaders);

        if (result == ErrorCode.SCARD_E_NO_READERS_AVAILABLE)
        {
            _knownReaders = [];
            return;
        }

        if (result != ErrorCode.SCARD_S_SUCCESS)
        {
            Logger.LogWarning("SCardListReaders failed during baseline: 0x{Result:X8}", result);
            _knownReaders = [];
            return;
        }

        _knownReaders = [.. currentReaders];
    }

    private void StopListening()
    {
        _shouldStop = true;

        SCardContext? context;
        Thread? listenerThread;
        lock (_syncLock)
        {
            context = _context;
            listenerThread = _listenerThread;
            _listenerThread = null;
        }

        // Signal the SCard context to cancel any blocking calls
        if (context is { IsInvalid: false })
        {
            _ = _sCardApi.SCardCancel(context);
        }

        // Wait for the listener thread to exit
        if (listenerThread is not null && listenerThread.IsAlive)
        {
            if (!listenerThread.Join(MaxDisposalWaitTime))
            {
                Logger.LogWarning("SmartCard listener thread did not exit within timeout");
            }
        }
    }

    private void ListenerThreadProc()
    {
        try
        {
            while (!_shouldStop)
            {
                if (_context is null || _context.IsInvalid)
                {
                    break;
                }

                // Get current list of readers
                var result = _sCardApi.SCardListReaders(_context, null, out var currentReaders);

                if (result == ErrorCode.SCARD_E_NO_READERS_AVAILABLE)
                {
                    // No readers available - wait with PnP notification on Windows
                    currentReaders = [];
                    _knownReaders ??= [];
                    WaitForPnpChange();
                    continue;
                }

                if (result != ErrorCode.SCARD_S_SUCCESS)
                {
                    if (!HandleSCardFailure(result, "SCardListReaders"))
                    {
                        break;
                    }

                    continue;
                }

                var currentReaderSet = new HashSet<string>(currentReaders);

                // Baseline was established in Start(), so we always have _knownReaders
                _knownReaders ??= currentReaderSet;

                // Detect removed readers
                foreach (var reader in _knownReaders.Except(currentReaderSet))
                {
                    OnDeviceEvent();
                }

                // Detect new readers
                foreach (var reader in currentReaderSet.Except(_knownReaders))
                {
                    OnDeviceEvent();
                }

                _knownReaders = currentReaderSet;

                // Wait for status change with timeout
                WaitForStatusChange(currentReaders);
            }
        }
        catch (Exception ex)
        {
            if (!_shouldStop)
            {
                Logger.LogError(ex, "SmartCard listener thread encountered an error");
                Status = DeviceListenerStatus.Error;
            }
        }
    }

    private void WaitForPnpChange()
    {
        if (_context is null || _context.IsInvalid || _shouldStop)
        {
            return;
        }

        // Use PnP notification reader on Windows to wait for reader arrival
        var pnpState = SCARD_READER_STATE.Create(PnpNotificationReaderName);
        var states = new[] { pnpState };

        var result = _sCardApi.SCardGetStatusChange(
            _context,
            (int)CheckForChangesWaitTime.TotalMilliseconds,
            states,
            states.Length);

        if (result != ErrorCode.SCARD_S_SUCCESS && !_shouldStop)
        {
            _ = HandleSCardFailure(result, "SCardGetStatusChange");
        }

        // Free the allocated string
        if (pnpState.ReaderName != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(pnpState.ReaderName);
        }
    }

    private void WaitForStatusChange(string[] readers)
    {
        if (_context is null || _context.IsInvalid || _shouldStop || readers.Length == 0)
        {
            _sleep(CheckForChangesWaitTime);
            return;
        }

        // Create reader states for monitoring
        var readerStates = SCARD_READER_STATE.CreateMany(readers);

        // Also add PnP notification reader to detect new reader arrivals
        var allStates = new SCARD_READER_STATE[readerStates.Length + 1];
        Array.Copy(readerStates, allStates, readerStates.Length);
        allStates[^1] = SCARD_READER_STATE.Create(PnpNotificationReaderName);

        try
        {
            var result = _sCardApi.SCardGetStatusChange(
                _context,
                (int)CheckForChangesWaitTime.TotalMilliseconds,
                allStates,
                allStates.Length);

            if (result != ErrorCode.SCARD_S_SUCCESS && !_shouldStop)
            {
                _ = HandleSCardFailure(result, "SCardGetStatusChange");
            }
        }
        finally
        {
            // Free all allocated strings
            foreach (var state in allStates)
            {
                if (state.ReaderName != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(state.ReaderName);
                }
            }
        }
    }

    private bool HandleSCardFailure(uint result, string operation)
    {
        if (_shouldStop || result == ErrorCode.SCARD_E_CANCELLED)
        {
            return false;
        }

        Logger.LogWarning("{Operation} failed: 0x{Result:X8}", operation, result);
        _sleep(CheckForChangesWaitTime);

        if (_shouldStop)
        {
            return false;
        }

        if (IsRecoverableContextError(result))
        {
            return TryReestablishContext(operation, result);
        }

        return true;
    }

    private bool TryReestablishContext(string operation, uint result)
    {
        SCardContext? oldContext;
        lock (_syncLock)
        {
            oldContext = _context;
            _context = null;
        }

        try
        {
            oldContext?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.LogDebug(
                ex,
                "Failed to dispose stale SCard context after {Operation} returned 0x{Result:X8}",
                operation,
                result);
        }

        var establishResult = _sCardApi.SCardEstablishContext(SCARD_SCOPE.USER, out var newContext);
        if (establishResult != ErrorCode.SCARD_S_SUCCESS)
        {
            Logger.LogWarning(
                "Failed to re-establish SCard context after {Operation} returned 0x{Result:X8}: 0x{EstablishResult:X8}",
                operation,
                result,
                establishResult);
            Status = DeviceListenerStatus.Error;
            return false;
        }

        lock (_syncLock)
        {
            if (_shouldStop)
            {
                newContext.Dispose();
                return false;
            }

            _context = newContext;
            Status = DeviceListenerStatus.Started;
        }

        EstablishBaseline();
        return true;
    }

    private static bool IsRecoverableContextError(uint result) =>
        result is ErrorCode.SCARD_E_INVALID_HANDLE
            or ErrorCode.SCARD_E_SYSTEM_CANCELLED
            or ErrorCode.ERROR_BROKEN_PIPE
            or ErrorCode.SCARD_E_SERVICE_STOPPED
            or ErrorCode.SCARD_E_NO_SERVICE;

    private void OnDeviceEvent()
    {
        try
        {
            DeviceEvent?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Exception in DeviceEvent callback");
        }
    }

    private void ClearEventHandlers()
    {
        DeviceEvent = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopListening();

        lock (_syncLock)
        {
            _context?.Dispose();
            _context = null;
        }

        ClearEventHandlers();
        Status = DeviceListenerStatus.Stopped;
    }

    /// <summary>
    /// Destructor to ensure resources are cleaned up.
    /// </summary>
    ~DesktopSmartCardDeviceListener()
    {
        _shouldStop = true;
        _context?.Dispose();
    }
}