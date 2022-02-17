// Copyright 2021 Yubico AB
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
using System.Linq;
using System.Threading;
using Yubico.Core.Logging;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.SmartCard
{
    /// <summary>
    /// A listener class for smart card related events.
    /// </summary>
    internal class DesktopSmartCardDeviceListener : SmartCardDeviceListener, IDisposable
    {
        private readonly Logger _log = Log.GetLogger();

        // The resource manager context.
        private SCardContext _context;

        // The active smart card readers.
        private SCardReaderStates _readerStates;

        private bool _isListening;
        private Thread? _listenerThread;

        /// <summary>
        /// Constructs a <see cref="SmartCardDeviceListener"/>.
        /// </summary>
        public DesktopSmartCardDeviceListener()
        {
            _log.LogInformation("Creating DesktopSmartCardDeviceListener.");

            uint result = PlatformLibrary.Instance.SCard.EstablishContext(SCARD_SCOPE.USER, out SCardContext context);
            _log.SCardApiCall(nameof(SCard.EstablishContext), result);

            _context = context;
            _readerStates = GetReaderStateList();

            StartListening();
        }

        /// <summary>
        /// Starts listening for all actions within a certain manager context.
        /// </summary>
        private void StartListening()
        {
            if (!_isListening)
            {
                _listenerThread = new Thread(ListenForReaderChanges)
                {
                    IsBackground = true
                };
                _isListening = true;
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
            _log.LogInformation("Smart card listener thread started. ThreadID is {ThreadID}.", Thread.CurrentThread.ManagedThreadId);

            bool usePnpWorkaround = UsePnpWorkaround();

            while (_isListening && CheckForUpdates(-1, usePnpWorkaround))
            {
                
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
            if (!_disposedValue)
            {
                if (disposing)
                {
                    uint _ = PlatformLibrary.Instance.SCard.Cancel(_context);
                    _context.Dispose();
                    _readerStates.Dispose();
                    StopListening();
                }
                _disposedValue = true;
            }
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
            if (!(_listenerThread is null))
            {
                ClearEventHandlers();
                _isListening = false;
                _listenerThread.Join();
            }
        }

        private bool CheckForUpdates(int timeout, bool usePnpWorkaround)
        {
            var arrivedDevices = new List<ISmartCardDevice>();
            var removedDevices = new List<ISmartCardDevice>();
            bool sendEvents = timeout != 0;

            SCardReaderStates? newStates = null;
            try
            {
                newStates = (SCardReaderStates)_readerStates.Clone();

                uint getStatusChangeResult = PlatformLibrary.Instance.SCard.GetStatusChange(_context, timeout, newStates);

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

                _log.SCardApiCall(nameof(SCard.GetStatusChange), getStatusChangeResult);
                _log.LogInformation("Reader states:\n{States}", newStates);

                while (ReaderListChangeDetected(newStates, usePnpWorkaround))
                {
                    using SCardReaderStates eventStateList = GetReaderStateList();
                    IEnumerable<SCardReaderStates.Entry> addedReaderStates = eventStateList.Except(newStates, new ReaderStateComparer());
                    IEnumerable<SCardReaderStates.Entry> removedReaderStates = newStates.Except(eventStateList, new ReaderStateComparer());

                    // Don't get status changes if there are no updates in state list.
                    if (!addedReaderStates.Any() && !removedReaderStates.Any())
                    {
                        break;
                    }

                    var readerStateList = newStates.ToList();
                    readerStateList.AddRange(addedReaderStates);

                    SCardReaderStates? updatedStates = null;
                    try
                    {
                        updatedStates = CreateReaderStates(readerStateList.Except(removedReaderStates, new ReaderStateComparer()).ToArray());

                        // Special handle readers with cards in them
                        if (sendEvents)
                        {
                            foreach (SCardReaderStates.Entry removedReader in removedReaderStates.Where(r =>
                                r.CurrentState.HasFlag(SCARD_STATE.PRESENT)))
                            {
                                ISmartCardDevice smartCardDevice =
                                    SmartCardDevice.Create(removedReader.ReaderName, removedReader.Atr);
                                removedDevices.Add(smartCardDevice);
                            }
                        }

                        // Only call get status change if a new reader was added. If nothing was added,
                        // we would otherwise hang / timeout here because all changes (in SCard's mind)
                        // have been dealt with.
                        if (addedReaderStates.Any())
                        {
                            _log.LogInformation("Additional smart card readers were found. Calling GetStatusChange for more information.");
                            getStatusChangeResult = PlatformLibrary.Instance.SCard.GetStatusChange(_context, 0, updatedStates);

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

                            _log.SCardApiCall(nameof(SCard.GetStatusChange), getStatusChangeResult);
                            _log.LogInformation("Reader states:\n{States}", newStates);
                        }

                        // Swap states to allow correct dispose of unmanaged objects.
                        (newStates, updatedStates) = (updatedStates, newStates);
                    }
                    finally
                    {
                        updatedStates?.Dispose();
                    }
                }

                if (RelevantChangesDetected(newStates))
                {
                    getStatusChangeResult = PlatformLibrary.Instance.SCard.GetStatusChange(_context, 0, newStates);
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

                    _log.SCardApiCall(nameof(SCard.GetStatusChange), getStatusChangeResult);
                    _log.LogInformation("Reader states:\n{States}", newStates);
                }

                if (sendEvents)
                {
                    DetectRelevantChanges(_readerStates, newStates, arrivedDevices, removedDevices);
                }

                UpdateCurrentlyKnownState(newStates);

                (_readerStates, newStates) = (newStates, _readerStates);

                FireEvents(arrivedDevices, removedDevices);
            }
            finally
            {
                newStates?.Dispose();
            }

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
            using var testState = new SCardReaderStates(new string[] { "\\\\?\\Pnp\\Notifications" });
            _ = PlatformLibrary.Instance.SCard.GetStatusChange(_context, 0, testState);
            bool usePnpWorkaround = testState[0].EventState.HasFlag(SCARD_STATE.UNKNOWN);
            return usePnpWorkaround;
        }

        /// <summary>
        /// Checks if reader state list contains any changes.
        /// </summary>
        /// <param name="newStates">Reader states to check.</param>
        /// <param name="usePnpWorkaround">Use ListReaders instead of relying on the \\?\Pnp\Notifications device.</param>
        /// <returns>True if changes are detected.</returns>
        private bool ReaderListChangeDetected(SCardReaderStates newStates, bool usePnpWorkaround)
        {
            if (usePnpWorkaround)
            {
                uint result = PlatformLibrary.Instance.SCard.ListReaders(_context, null, out string[] readerNames);
                _log.SCardApiCall(nameof(SCard.ListReaders), result);

                return readerNames.Length != newStates.Count - 1;
            }

            if (newStates[0].EventState.HasFlag(SCARD_STATE.CHANGED))
            {
                newStates[0].CurrentState = newStates[0].EventState & ~SCARD_STATE.CHANGED;
                newStates[0].EventState = 0;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the relevant changes in state list detected.
        /// </summary>
        /// <param name="newStates">States to check.</param>
        /// <returns>True if changes detected in states.</returns>
        private static bool RelevantChangesDetected(SCardReaderStates newStates)
        {
            for (int i = 0; i < newStates.Count; i++)
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
        private static void DetectRelevantChanges(SCardReaderStates originalStates, SCardReaderStates newStates, List<ISmartCardDevice> arrivedDevices, List<ISmartCardDevice> removedDevices)
        {
            foreach (SCardReaderStates.Entry entry in newStates)
            {
                SCARD_STATE diffState = entry.CurrentState ^ entry.EventState;

                if (diffState.HasFlag(SCARD_STATE.PRESENT) && entry.CurrentState.HasFlag(SCARD_STATE.PRESENT))
                {
                    IEnumerable<SCardReaderStates.Entry> states = originalStates.Where((e) => e.ReaderName == entry.ReaderName);
                    ISmartCardDevice smartCardDevice =
                        SmartCardDevice.Create(entry.ReaderName, states.FirstOrDefault()?.Atr ?? entry.Atr);
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
        /// Creates new <see cref="SCardReaderStates"/> from array of SCardReaderStates.Entry objects.
        /// </summary>
        /// <param name="newStateList">Array of SCardReaderStates.Entry objects</param>
        /// <returns>New <see cref="SCardReaderStates"/> object.</returns>
        private static SCardReaderStates CreateReaderStates(SCardReaderStates.Entry[] newStateList)
        {
            var newStates = new SCardReaderStates(newStateList.Length);

            for (int i = 0; i < newStateList.Length; i++)
            {
                SCardReaderStates.Entry state = newStateList[i];
                newStates[i].ReaderName = state.ReaderName;
                newStates[i].CurrentState = state.CurrentState;
                newStates[i].CurrentSequence = state.CurrentSequence;
                newStates[i].EventState = state.EventState;
                newStates[i].EventSequence = state.EventSequence;
            }

            return newStates;
        }

        /// <summary>
        /// Updates the current status and event status if reader's event status has changed.
        /// </summary>
        /// <param name="newStates">State list to check for changes.</param>
        private static void UpdateCurrentlyKnownState(SCardReaderStates newStates)
        {
            foreach (SCardReaderStates.Entry entry in newStates)
            {
                if (entry.EventState.HasFlag(SCARD_STATE.CHANGED))
                {
                    entry.CurrentState = entry.EventState & ~SCARD_STATE.CHANGED;
                    entry.EventState = 0;
                    entry.CurrentSequence = entry.EventSequence;
                    entry.EventSequence = 0;
                }
            }
        }

        /// <summary>
        /// Updates the current context.
        /// </summary>
        private void UpdateCurrentContext()
        {
            uint result = PlatformLibrary.Instance.SCard.EstablishContext(SCARD_SCOPE.USER, out SCardContext context);
            _log.SCardApiCall(nameof(SCard.EstablishContext), result);

            _context = context;
            _readerStates = GetReaderStateList();
        }

        /// <summary>
        /// Gets readers within the current context. 
        /// </summary>
        /// <returns><see cref="SCardReaderStates"/></returns>
        private SCardReaderStates GetReaderStateList()
        {
            uint result = PlatformLibrary.Instance.SCard.ListReaders(_context, null, out string[] readerNames);
            _log.SCardApiCall(nameof(SCard.ListReaders), result);

            var readerStates = new SCardReaderStates(readerNames.Prepend("\\\\?PnP?\\Notification").ToArray());

            return readerStates;
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

        private class ReaderStateComparer : IEqualityComparer<SCardReaderStates.Entry>
        {
            public bool Equals(SCardReaderStates.Entry x, SCardReaderStates.Entry y) => x.ReaderName == y.ReaderName;

            public int GetHashCode(SCardReaderStates.Entry obj) => obj.ReaderName.GetHashCode();
        }
    }
}
