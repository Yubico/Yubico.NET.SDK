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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Yubico.PlatformInterop;

using static Yubico.PlatformInterop.NativeMethods;

namespace Yubico.Core.Devices.SmartCard
{
    /// <summary>
    /// A listener class for smart card related events.
    /// </summary>
    internal class DesktopSmartCardDeviceListener : SmartCardDeviceListener
    {
        private static readonly string[] readerNames = new[] { "\\\\?\\Pnp\\Notifications" };
        private readonly ILogger _log = Logging.Log.GetLogger<DesktopSmartCardDeviceListener>();

        // The resource manager context.
        private SCardContext _context;

        // The active smart card readers.
        private SCARD_READER_STATE[] _readerStates;

        private Thread? _listenerThread;
        private bool _isListening;
        private bool _isDisposed;
        private readonly object _startStopLock = new object();
        private readonly object _disposeLock = new object();
        private static readonly TimeSpan MaxDisposalWaitTime = TimeSpan.FromSeconds(8);
        private static readonly TimeSpan CheckForChangesWaitTime = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Constructs a <see cref="SmartCardDeviceListener"/>.
        /// </summary>
        public DesktopSmartCardDeviceListener()
        {
            _log.LogInformation("Creating DesktopSmartCardDeviceListener.");
            Status = DeviceListenerStatus.Stopped;

            uint result = SCardEstablishContext(SCARD_SCOPE.USER, out SCardContext context);
            _log.SCardApiCall(nameof(SCardEstablishContext), result);

            // If we failed to establish context to the smart card subsystem, something substantially wrong
            // has occured. We should not continue, and the device listener should remain dormant.
            if (result != ErrorCode.SCARD_S_SUCCESS)
            {
                context.Dispose(); // Needed to satisfy analyzer (even though it should be null already)
                _context = new SCardContext(IntPtr.Zero);
                _readerStates = Array.Empty<SCARD_READER_STATE>();
                Status = DeviceListenerStatus.Error;
                _log.LogWarning("SmartCardDeviceListener dormant as SDK was unable to establish a context to the PCSC service.");
                return;
            }

            _context = context;
            _readerStates = GetReaderStateList();

            StartListening();
        }

        /// <summary>
        /// Starts listening for all actions within a certain manager context.
        /// </summary>
        private void StartListening()
        {
            lock (_startStopLock)
            {
                if (_isListening)
                {
                    return;
                }

                _listenerThread = new Thread(ListenForReaderChanges)
                {
                    IsBackground = true
                };

                _isListening = true;
                Status = DeviceListenerStatus.Started;
                _listenerThread.Start();
            }
        }

        // This method is the delegate sent to the new Thread.
        // Once the new Thread is started, this method will execute. As long as
        // the _isListening field is true, it will keep checking for updates.
        // Once _isListening is false, it will quit the loop and return, which
        // will terminate the thread.
        private void ListenForReaderChanges()
        {
            _log.LogInformation("Smart card listener thread started. ThreadID is {ThreadID}.", Environment.CurrentManagedThreadId);

            bool usePnpWorkaround = UsePnpWorkaround();
            while (_isListening)
            {
                try
                {
                    bool result = CheckForUpdates(usePnpWorkaround);
                    if (!result)
                    {
                        break;
                    }    
                }
                catch (Exception e)
                {
                    _log.LogError(e, "Exception occurred while listening for smart card reader changes.");
                    Status = DeviceListenerStatus.Error;
                }
            }
        }

        #region IDisposable Support

        /// <summary>
        /// Disposes the objects.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            lock (_disposeLock)
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;

                try
                {
                    if (disposing)
                    {
                        // Cancel any blocking SCardGetStatusChange calls
                        _ = SCardCancel(_context);

                        // Stop the listener thread BEFORE disposing the context
                        // This ensures the thread can exit gracefully while context is still valid
                        StopListening();

                        // Now it's safe to dispose the context
                        _context.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    // CRITICAL: Never throw from Dispose, especially when called from finalizer
                    if (disposing)
                    {
                        _log.LogWarning(ex, "Exception during DesktopSmartCardDeviceListener disposal");
                    }
                    // If !disposing (finalizer path), silently ignore to prevent GC thread crash
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        #endregion

        /// <summary>
        /// Stops listening for all actions within a certain manager context.
        /// </summary>
        private void StopListening()
        {
            // Use local variable to prevent race condition if multiple threads call StopListening()
            Thread? threadToJoin = _listenerThread;
            if (threadToJoin is null)
            {
                return;
            }

            _isListening = false;
            Status = DeviceListenerStatus.Stopped;

            // Wait for thread to exit with timeout to prevent indefinite blocking
            bool exited = threadToJoin.Join(MaxDisposalWaitTime);
            if (!exited)
            {
                _log.LogWarning("Smart card listener thread did not exit within timeout. Context may have been disposed prematurely.");
            }

            _listenerThread = null;
        }

        private bool CheckForUpdates(bool usePnpWorkaround)
        {
            var arrivedDevices = new List<ISmartCardDevice>();
            var removedDevices = new List<ISmartCardDevice>();
            bool sendEvents = CheckForChangesWaitTime != TimeSpan.Zero;
            var newStates = (SCARD_READER_STATE[])_readerStates.Clone();

            uint getStatusChangeResult = SCardGetStatusChange(_context, (int)CheckForChangesWaitTime.TotalMilliseconds, newStates, newStates.Length);
            if (!HandleSCardGetStatusChangeResult(getStatusChangeResult, newStates))
            {
                return false;
            }

            while (ReaderListChangeDetected(ref newStates, usePnpWorkaround))
            {
                SCARD_READER_STATE[] eventStateList = GetReaderStateList();
                SCARD_READER_STATE[] addedReaderStates = eventStateList.Except(newStates, new ReaderStateComparer()).ToArray();
                SCARD_READER_STATE[] removedReaderStates = newStates.Except(eventStateList, new ReaderStateComparer()).ToArray();

                // Don't get status changes if there are no updates in state list.
                if (addedReaderStates.Length == 0 && removedReaderStates.Length == 0)
                {
                    break;
                }

                var readerStateList = newStates.ToList();
                readerStateList.AddRange(addedReaderStates);

                SCARD_READER_STATE[] updatedStates = readerStateList.Except(removedReaderStates, new ReaderStateComparer()).ToArray();

                // Special handle readers with cards in them
                if (sendEvents)
                {
                    removedDevices.AddRange(
                        removedReaderStates
                            .Where(r => r.CurrentState.HasFlag(SCARD_STATE.PRESENT))
                            .Select(r => SmartCardDevice.Create(r.ReaderName, r.Atr)));
                }

                // Only call get status change if a new reader was added. If nothing was added,
                // we would otherwise hang / timeout here because all changes (in SCard's mind)
                // have been dealt with.
                if (addedReaderStates.Length != 0)
                {
                    _log.LogInformation("Additional smart card readers were found. Calling GetStatusChange for more information.");
                    getStatusChangeResult = SCardGetStatusChange(_context, 0, updatedStates, updatedStates.Length);

                    if (!HandleSCardGetStatusChangeResult(getStatusChangeResult, updatedStates))
                    {
                        return false;
                    }
                }

                newStates = updatedStates;
            }

            if (RelevantChangesDetected(newStates))
            {
                getStatusChangeResult = SCardGetStatusChange(_context, 0, newStates, newStates.Length);
                if (!HandleSCardGetStatusChangeResult(getStatusChangeResult, newStates))
                {
                    return false;
                }
            }

            if (sendEvents)
            {
                DetectRelevantChanges(_readerStates, newStates, arrivedDevices, removedDevices);
            }

            UpdateCurrentlyKnownState(ref newStates);

            _readerStates = newStates;

            FireEvents(arrivedDevices, removedDevices);

            return true;
        }

        // So apparently not all platforms implement the virtual pnp reader semantics the same. They will still wait on
        // GetStatusChange, returning when a new reader is added, they just don't mark the CHANGED flag all the time.
        // Weird. Anyways, it seems like a reasonable workaround is to detect the type of system (by seeing if it returns
        // STATE_UNKNOWN when asked about the virtual reader). If it does, any time GetStatusChange returns, we should
        // make an additional call to ListReaders and compare the count to the current reader state count (subtracting
        // the virtual reader). If they are different, then we should treat that the same as if the virtual reader informed
        // us of the change.
        private bool UsePnpWorkaround()
        {
            try
            {
                SCARD_READER_STATE[] testState = SCARD_READER_STATE.CreateFromReaderNames(readerNames);
                _ = SCardGetStatusChange(_context, 0, testState, testState.Length);
                bool usePnpWorkaround = testState[0].EventState.HasFlag(SCARD_STATE.UNKNOWN);
                return usePnpWorkaround;    
            }
            catch (Exception e)
            {
                _log.LogDebug(e, "Exception occurred while determining if PnP workaround is needed. Assuming it is not.");
                return false;
            }
        }

        /// <summary>
        /// Checks if reader state list contains any changes.
        /// </summary>
        /// <param name="newStates">Reader states to check.</param>
        /// <param name="usePnpWorkaround">Use ListReaders instead of relying on the \\?\Pnp\Notifications device.</param>
        /// <returns>True if changes are detected.</returns>
        private bool ReaderListChangeDetected(ref SCARD_READER_STATE[] newStates, bool usePnpWorkaround)
        {
            if (usePnpWorkaround)
            {
                uint result = SCardListReaders(_context, null, out string[] readerNames);
                if (result != ErrorCode.SCARD_E_NO_READERS_AVAILABLE)
                {
                    _log.SCardApiCall(nameof(SCardListReaders), result);
                }

                return readerNames.Length != newStates.Length - 1;
            }

            if (newStates[0].EventState.HasFlag(SCARD_STATE.CHANGED))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the relevant changes in state list detected.
        /// </summary>
        /// <param name="newStates">States to check.</param>
        /// <returns>True if changes detected in states.</returns>
        private static bool RelevantChangesDetected(SCARD_READER_STATE[] newStates)
        {
            for (int i = 0; i < newStates.Length; i++)
            {
                SCARD_STATE diffState = newStates[i].CurrentState ^ newStates[i].EventState;

                if (diffState.HasFlag(SCARD_STATE.PRESENT) && newStates[i].CurrentState.HasFlag(SCARD_STATE.PRESENT))
                {
                    return true;
                }
                if (diffState.HasFlag(SCARD_STATE.PRESENT) && newStates[i].EventState.HasFlag(SCARD_STATE.PRESENT))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the relevant changes in state list detected and creates arrived and removed devices.
        /// </summary>
        /// <param name="originalStates">Original states used to get Atr values for removal events.</param>
        /// <param name="newStates">State list to check for changes.</param>
        /// <param name="arrivedDevices">List where new arrived devices will be added.</param>
        /// <param name="removedDevices">List where new removed devices will be added.</param>
        private static void DetectRelevantChanges(SCARD_READER_STATE[] originalStates, SCARD_READER_STATE[] newStates, List<ISmartCardDevice> arrivedDevices, List<ISmartCardDevice> removedDevices)
        {
            foreach (SCARD_READER_STATE entry in newStates)
            {
                SCARD_STATE diffState = entry.CurrentState ^ entry.EventState;

                if (diffState.HasFlag(SCARD_STATE.PRESENT) && entry.CurrentState.HasFlag(SCARD_STATE.PRESENT))
                {
                    IEnumerable<SCARD_READER_STATE> states = originalStates.Where(e => e.ReaderName == entry.ReaderName);
                    ISmartCardDevice smartCardDevice =
                        SmartCardDevice.Create(entry.ReaderName, states.FirstOrDefault().Atr);
                    removedDevices.Add(smartCardDevice);
                }
                else if (diffState.HasFlag(SCARD_STATE.PRESENT) && entry.EventState.HasFlag(SCARD_STATE.PRESENT))
                {
                    ISmartCardDevice smartCardDevice =
                        SmartCardDevice.Create(entry.ReaderName, entry.Atr);
                    arrivedDevices.Add(smartCardDevice);
                }
            }
        }

        /// <summary>
        /// Updates the current status and event status if reader's event status has changed.
        /// </summary>
        /// <param name="states">State list to check for changes.</param>
        private static void UpdateCurrentlyKnownState(ref SCARD_READER_STATE[] states)
        {
            for (int i = 0; i < states.Length; i++)
            {
                states[i].AcknowledgeChanges();
            }
        }

        /// <summary>
        /// Updates the current context.
        /// </summary>
        private void UpdateCurrentContext()
        {
            uint result = SCardEstablishContext(SCARD_SCOPE.USER, out SCardContext context);
            _log.SCardApiCall(nameof(SCardEstablishContext), result);

            _context = context;
            _readerStates = GetReaderStateList();
        }

        /// <summary>
        /// Gets readers within the current context.
        /// </summary>
        /// <returns><see cref="SCARD_READER_STATE"/></returns>
        private SCARD_READER_STATE[] GetReaderStateList()
        {
            uint result = SCardListReaders(_context, null, out string[] readerNames);
            if (result != ErrorCode.SCARD_E_NO_READERS_AVAILABLE)
            {
                _log.SCardApiCall(nameof(SCardListReaders), result);
            }

            // We use this workaround as .NET 4.7 doesn't really support all of .NET Standard 2.0
            var allReaders = new List<string>(readerNames.Length + 1)
            {
                "\\\\?PnP?\\Notification"
            };

            allReaders.AddRange(readerNames);

            return SCARD_READER_STATE.CreateFromReaderNames(allReaders);
        }

        /// <summary>
        /// Fires all created events.
        /// </summary>
        /// <param name="arrivedDevices">List of arrival devices.</param>
        /// <param name="removedDevices">List of removal devices.</param>
        private void FireEvents(List<ISmartCardDevice> arrivedDevices, List<ISmartCardDevice> removedDevices)
        {
            foreach (ISmartCardDevice arrivedDevice in arrivedDevices)
            {
                OnArrived(arrivedDevice);
            }

            foreach (ISmartCardDevice removedDevice in removedDevices)
            {
                OnRemoved(removedDevice);
            }
        }

        /// <summary>
        /// Handles common SCardGetStatusChange result codes including cancellation, timeouts, and non-critical errors.
        /// Logs appropriately based on the result code.
        /// </summary>
        /// <param name="result">The result code from SCardGetStatusChange</param>
        /// <param name="states">The reader states for logging purposes</param>
        /// <returns>False if cancelled (caller should return false), true otherwise (caller should continue)</returns>
        private bool HandleSCardGetStatusChangeResult(uint result, SCARD_READER_STATE[] states)
        {
            if (result == ErrorCode.SCARD_E_CANCELLED)
            {
                _log.LogInformation("GetStatusChange indicated SCARD_E_CANCELLED.");
                return false;
            }

            // Timeout is expected behavior in polling - don't log as it occurs every 100ms
            if (result == ErrorCode.SCARD_E_TIMEOUT)
            {
                return true;
            }

            // Non-critical errors that need context update
            if (UpdateContextIfNonCritical(result))
            {
                _log.LogInformation("GetStatusChange indicated non-critical status {Status:X}.", result);
                return true;
            }

            // Log actual errors and reader states for debugging
            _log.SCardApiCall(nameof(SCardGetStatusChange), result);
            _log.LogInformation("Reader states:\n{States}", states);

            return true;
        }

        /// <summary>
        /// Checks if context need to be updated.
        /// </summary>
        /// <param name="errorCode"></param>
        /// <returns>true if context updated</returns>
        private bool UpdateContextIfNonCritical(uint errorCode)
        {
            switch (errorCode)
            {
                case ErrorCode.SCARD_E_SERVICE_STOPPED:
                case ErrorCode.SCARD_E_NO_READERS_AVAILABLE:
                case ErrorCode.SCARD_E_NO_SERVICE:
                    UpdateCurrentContext();
                    return true;
                default:
                    return false;
            }
        }

        private class ReaderStateComparer : IEqualityComparer<SCARD_READER_STATE>
        {
            public bool Equals(SCARD_READER_STATE x, SCARD_READER_STATE y) => x.ReaderName == y.ReaderName;

            [SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "Method needs to compile for both netstandard 2.0 and 2.1")]
            public int GetHashCode(SCARD_READER_STATE obj) => obj.ReaderName.GetHashCode();
        }
    }
}
