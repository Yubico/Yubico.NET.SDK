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

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.PlatformInterop.Desktop.SCard;

namespace Yubico.YubiKit.Core.SmartCard;

/// <summary>
/// Monitors for SmartCard reader arrival and removal using PC/SC SCardGetStatusChange.
/// </summary>
/// <remarks>
/// Uses a dedicated background thread with 1000ms timeout for responsive cancellation.
/// On Windows, also watches for PnP notifications via the special <c>\\?\PnP?\Notification</c> reader.
/// </remarks>
public sealed class DesktopSmartCardDeviceListener : ISmartCardDeviceListener
{
    private const string PnpNotificationReaderName = @"\\?PnP?\Notification";
    private static readonly TimeSpan CheckForChangesWaitTime = TimeSpan.FromMilliseconds(1000);
    private static readonly TimeSpan MaxDisposalWaitTime = TimeSpan.FromSeconds(8);

    private static readonly ILogger Logger = NullLoggerFactory.Instance.CreateLogger<DesktopSmartCardDeviceListener>();

    private readonly object _syncLock = new();
    private SCardContext? _context;
    private Thread? _listenerThread;
    private volatile bool _shouldStop;
    private bool _disposed;

    private event EventHandler<SmartCardDeviceEventArgs>? _arrived;
    private event EventHandler<SmartCardDeviceEventArgs>? _removed;

    /// <inheritdoc />
    public event EventHandler<SmartCardDeviceEventArgs>? Arrived
    {
        add => _arrived += value;
        remove => _arrived -= value;
    }

    /// <inheritdoc />
    public event EventHandler<SmartCardDeviceEventArgs>? Removed
    {
        add => _removed += value;
        remove => _removed -= value;
    }

    /// <inheritdoc />
    public DeviceListenerStatus Status { get; private set; } = DeviceListenerStatus.Stopped;

    /// <summary>
    /// Creates a new instance and starts listening for SmartCard device events.
    /// </summary>
    public DesktopSmartCardDeviceListener()
    {
        StartListening();
    }

    private void StartListening()
    {
        try
        {
            var result = NativeMethods.SCardEstablishContext(SCARD_SCOPE.USER, out var context);
            if (result != ErrorCode.SCARD_S_SUCCESS)
            {
                Logger.LogWarning("Failed to establish SCard context: 0x{Result:X8}", result);
                Status = DeviceListenerStatus.Error;
                return;
            }

            _context = context;

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

    private void StopListening()
    {
        _shouldStop = true;

        // Signal the SCard context to cancel any blocking calls
        if (_context is { IsInvalid: false })
        {
            _ = NativeMethods.SCardCancel(_context);
        }

        // Wait for the listener thread to exit
        if (_listenerThread is not null && _listenerThread.IsAlive)
        {
            if (!_listenerThread.Join(MaxDisposalWaitTime))
            {
                Logger.LogWarning("SmartCard listener thread did not exit within timeout");
            }
        }

        _listenerThread = null;
    }

    private void ListenerThreadProc()
    {
        HashSet<string> knownReaders = [];

        try
        {
            while (!_shouldStop)
            {
                if (_context is null || _context.IsInvalid)
                {
                    break;
                }

                // Get current list of readers
                var result = NativeMethods.SCardListReaders(_context, null, out var currentReaders);

                if (result == ErrorCode.SCARD_E_NO_READERS_AVAILABLE)
                {
                    // No readers available - wait with PnP notification on Windows
                    currentReaders = [];
                    WaitForPnpChange();
                    continue;
                }

                if (result != ErrorCode.SCARD_S_SUCCESS)
                {
                    if (result == ErrorCode.SCARD_E_CANCELLED ||
                        result == ErrorCode.SCARD_E_SERVICE_STOPPED ||
                        result == ErrorCode.SCARD_E_NO_SERVICE)
                    {
                        break;
                    }

                    Logger.LogWarning("SCardListReaders failed: 0x{Result:X8}", result);
                    Thread.Sleep(CheckForChangesWaitTime);
                    continue;
                }

                var currentReaderSet = new HashSet<string>(currentReaders);

                // Detect removed readers
                foreach (var reader in knownReaders.Except(currentReaderSet))
                {
                    OnRemoved(reader);
                }

                // Detect new readers
                foreach (var reader in currentReaderSet.Except(knownReaders))
                {
                    OnArrived(reader);
                }

                knownReaders = currentReaderSet;

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

        _ = NativeMethods.SCardGetStatusChange(
            _context,
            (int)CheckForChangesWaitTime.TotalMilliseconds,
            states,
            states.Length);

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
            Thread.Sleep(CheckForChangesWaitTime);
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
            _ = NativeMethods.SCardGetStatusChange(
                _context,
                (int)CheckForChangesWaitTime.TotalMilliseconds,
                allStates,
                allStates.Length);
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

    private void OnArrived(string readerName)
    {
        var args = new SmartCardDeviceEventArgs(readerName);
        InvokeEventSafely(_arrived, args, "Arrived");
    }

    private void OnRemoved(string readerName)
    {
        var args = new SmartCardDeviceEventArgs(readerName);
        InvokeEventSafely(_removed, args, "Removed");
    }

    private void InvokeEventSafely(EventHandler<SmartCardDeviceEventArgs>? handler, SmartCardDeviceEventArgs args, string eventName)
    {
        if (handler is null)
        {
            return;
        }

        foreach (var invoker in handler.GetInvocationList())
        {
            try
            {
                ((EventHandler<SmartCardDeviceEventArgs>)invoker).Invoke(this, args);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Exception in {EventName} event handler", eventName);
            }
        }
    }

    private void ClearEventHandlers()
    {
        _arrived = null;
        _removed = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
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
