﻿// Copyright 2021 Yubico AB
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
using System.Collections.Immutable;
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
    internal class DesktopSmartCardDeviceListener : SmartCardDeviceListener, IDisposable
    {
        private readonly ILogger _log = Logging.Log.GetLogger<DesktopSmartCardDeviceListener>();

        // The resource manager context.
        private SCardContext _context;

        // The active smart card readers.
        private ImmutableArray<SCARD_READER_STATE> _readerStates;

        private Thread? _listenerThread;

        private readonly object _syncLock = new object();
        private volatile bool _isListening;
        private readonly CancellationTokenSource _cancellationSource = new CancellationTokenSource();


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
                _readerStates = ImmutableArray.Create<SCARD_READER_STATE>();

                Status = DeviceListenerStatus.Error;
                _log.LogWarning("SmartCardDeviceListener dormant as SDK was unable to establish a context to the PCSC service.");
                return;
            }

            _context = context;

            StartListening();
        }

        /// <summary>
        /// Starts listening for all actions within a certain manager context.
        /// </summary>
        private void StartListening()
        {
            lock (_syncLock)
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
                _listenerThread.Start();
                Status = DeviceListenerStatus.Started;
            }
        }

        // This method is the delegate sent to the new Thread.
        // Once the new Thread is started, this method will execute. As long as
        // the _isListening field is true, it will keep checking for updates.
        // Once _isListening is false, it will quit the loop and return, which
        // will terminate the thread.
        private void ListenForReaderChanges()
        {
            try
            {
                _log.LogInformation("Smart card listener thread started. ThreadID is {ThreadID}.", Environment.CurrentManagedThreadId);

                bool usePnpWorkaround = UsePnpWorkaround();
                while (!_cancellationSource.Token.IsCancellationRequested)
                {
                    bool shouldContinue = CheckForUpdates(-1, usePnpWorkaround);
                    if (!shouldContinue)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _log.LogInformation("Smart card listener thread was cancelled.");
            }

            catch (Exception ex)
            {
                _log.LogError(ex, "An exception occurred in the smart card listener thread.");
            }

        }

        #region IDisposable Support

        private bool _disposedValue; // To detect redundant calls

        /// <summary>
        /// Disposes the objects.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue)
            {
                return;
            }

            if (disposing)
            {
                lock (_syncLock)
                {
                    _cancellationSource.Cancel();
                    uint _ = SCardCancel(_context);
                    StopListening();

                    _context.Dispose();
                    _cancellationSource.Dispose();
                }
            }
            _disposedValue = true;
        }

        ~DesktopSmartCardDeviceListener()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        /// <summary>
        /// Calls Dispose(true).
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// Stops listening for all actions within a certain manager context.
        /// </summary>
        private void StopListening()
        {
            lock (_syncLock)
            {
                if (!_isListening || _listenerThread is null)
                {
                    return;
                }

                _isListening = false;
                _cancellationSource.Cancel();
                ClearEventHandlers();
                Status = DeviceListenerStatus.Stopped;

                if (!_listenerThread.Join(TimeSpan.FromSeconds(5)))
                {
                    _log.LogWarning("Failed to stop listener thread within 5 seconds");
                }
            }
        }

        private bool CheckForUpdates(int timeout, bool usePnpWorkaround)
        {
            var arrivedDevices = new List<ISmartCardDevice>();
            var removedDevices = new List<ISmartCardDevice>();
            bool sendEvents = timeout != 0;

            SCARD_READER_STATE[] newStates = _readerStates.ToArray();


            uint getStatusChangeResult = SCardGetStatusChange(_context, timeout, newStates, newStates.Length);
            if (getStatusChangeResult == ErrorCode.SCARD_E_CANCELLED)
            {
                _log.LogInformation("GetStatusChange indicated SCARD_E_CANCELLED.");
                return false;
            }

            if (UpdateContextIfNonCritical(getStatusChangeResult))
            {
                _log.LogInformation("GetStatusChange indicated non-critical status {Status:X}.", getStatusChangeResult);
                return true;
            }

            _log.SCardApiCall(nameof(SCardGetStatusChange), getStatusChangeResult);
            _log.LogInformation("Reader states:\n{States}", newStates);

            while (ReaderListChangeDetected(ref newStates, usePnpWorkaround))
            {
                SCARD_READER_STATE[] eventStateList = GetReaderStateList();
                SCARD_READER_STATE[] addedReaderStates = eventStateList.Except(newStates, new ReaderStateComparer()).ToArray();
                SCARD_READER_STATE[] removedReaderStates = newStates.Except(eventStateList, new ReaderStateComparer()).ToArray();

                // Don't get status changes if there are no updates in state list.
                if (!addedReaderStates.Any() && !removedReaderStates.Any())
                {
                    break;
                }

                List<SCARD_READER_STATE> readerStateList = newStates.ToList();
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
                if (addedReaderStates.Any())
                {
                    _log.LogInformation("Additional smart card readers were found. Calling GetStatusChange for more information.");
                    getStatusChangeResult = SCardGetStatusChange(_context, 0, updatedStates, updatedStates.Length);

                    if (getStatusChangeResult == ErrorCode.SCARD_E_CANCELLED)
                    {
                        _log.LogInformation("GetStatusChange indicated SCARD_E_CANCELLED.");
                        return false;
                    }

                    if (UpdateContextIfNonCritical(getStatusChangeResult))
                    {
                        _log.LogInformation("GetStatusChange indicated non-critical status {Status:X}.", getStatusChangeResult);
                        return true;
                    }

                    _log.SCardApiCall(nameof(SCardGetStatusChange), getStatusChangeResult);
                    _log.LogInformation("Reader states:\n{States}", newStates);
                }

                newStates = updatedStates;
            }

            if (RelevantChangesDetected(newStates))
            {
                getStatusChangeResult = SCardGetStatusChange(_context, 0, newStates, newStates.Length);
                if (getStatusChangeResult == ErrorCode.SCARD_E_CANCELLED)
                {
                    _log.LogInformation("GetStatusChange indicated SCARD_E_CANCELLED.");
                    return false;
                }

                if (UpdateContextIfNonCritical(getStatusChangeResult))
                {
                    _log.LogInformation("GetStatusChange indicated non-critical status {Status:X}.", getStatusChangeResult);
                    return true;
                }

                _log.SCardApiCall(nameof(SCardGetStatusChange), getStatusChangeResult);
                _log.LogInformation("Reader states:\n{States}", newStates);
            }

            if (sendEvents)
            {
                DetectRelevantChanges(_readerStates, newStates, arrivedDevices, removedDevices);
            }

            UpdateCurrentlyKnownState(ref newStates);

            _readerStates = ImmutableArray.Create(newStates);

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
            SCARD_READER_STATE[] testState = SCARD_READER_STATE.CreateFromReaderNames(new[] { "\\\\?\\Pnp\\Notifications" });
            _ = SCardGetStatusChange(_context, 0, testState, testState.Length);
            bool usePnpWorkaround = testState[0].EventState.HasFlag(SCARD_STATE.UNKNOWN);
            return usePnpWorkaround;
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
        private static void DetectRelevantChanges(ImmutableArray<SCARD_READER_STATE> originalStates, SCARD_READER_STATE[] newStates, List<ISmartCardDevice> arrivedDevices, List<ISmartCardDevice> removedDevices)
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
            lock (_syncLock)
            {
                uint result = SCardEstablishContext(SCARD_SCOPE.USER, out SCardContext context);
                _log.SCardApiCall(nameof(SCardEstablishContext), result);

                _context = context;
                _readerStates = ImmutableArray.Create(GetReaderStateList());
            }
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
