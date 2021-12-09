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

namespace Yubico.PlatformInterop
{
    /// <summary>
    /// A listener class for smart card related events.
    /// </summary>
    public class SCardListener : IDisposable
    {
        // The resource manager context.
        private SCardContext _context;

        // The active smart card readers.
        private SCardReaderStates _readerStates;
        private readonly Thread _listenerThread;

        /// <summary>
        /// Event for card arrival.
        /// </summary>
        public event EventHandler<SCardEventArgs>? CardArrival;

        /// <summary>
        /// Event for card removal.
        /// </summary>
        public event EventHandler<SCardEventArgs>? CardRemoval;

        /// <summary>
        /// Constructs a <see cref="SCardListener"/>.
        /// </summary>
        public SCardListener()
        {
            uint result = PlatformLibrary.Instance.SCard.EstablishContext(SCARD_SCOPE.USER, out SCardContext context);

            _context = context;
            _readerStates = GetReaderStateList();

            _listenerThread = new Thread(() =>
            {
                while (ListenForReaderChanges(-1))
                {
                    ;
                }
            }
            );

            _listenerThread.Start();
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
                    _context.Dispose();
                    _readerStates.Dispose();
                }
                _disposedValue = true;
            }
        }

        ~SCardListener()
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
        /// Stops listening for all actions within a certain resource context.
        /// manager context.
        /// </summary>
        public void StopListening()
        {
            uint result = PlatformLibrary.Instance.SCard.Cancel(_context);
            ThrowIfFailed(result);
            _listenerThread.Join();
        }

        private bool ListenForReaderChanges(int timeout)
        {
            var arrivalEvents = new List<SCardEventArgs>();
            var removalEvents = new List<SCardEventArgs>();
            bool sendEvents = timeout != 0;

            SCardReaderStates? newStates = null;
            try
            {
                newStates = (SCardReaderStates)_readerStates.Clone();

                uint getStatusChangeResult = PlatformLibrary.Instance.SCard.GetStatusChange(_context, timeout, newStates);

                if (getStatusChangeResult == ErrorCode.SCARD_E_CANCELLED)
                {
                    return false;
                }

                if (UpdateContextIfNonCritical(getStatusChangeResult))
                {
                    return true;
                }

                while (ReaderListChangeDetected(newStates))
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
                                removalEvents.Add(new SCardEventArgs(removedReader.ReaderName, removedReader.Atr));
                            }
                        }

                        // Only call get status change if a new reader was added. If nothing was added,
                        // we would otherwise hang / timeout here because all changes (in SCard's mind)
                        // have been dealt with.
                        if (addedReaderStates.Any())
                        {
                            getStatusChangeResult = PlatformLibrary.Instance.SCard.GetStatusChange(_context, 0, updatedStates);

                            if (getStatusChangeResult == ErrorCode.SCARD_E_CANCELLED)
                            {
                                return false;
                            }

                            if (UpdateContextIfNonCritical(getStatusChangeResult))
                            {
                                return true;
                            }

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
                        return false;
                    }

                    if (UpdateContextIfNonCritical(getStatusChangeResult))
                    {
                        return true;
                    }

                }

                if (sendEvents)
                {
                    DetectRelevantChanges(_readerStates, newStates, arrivalEvents, removalEvents);
                }

                UpdateCurrentlyKnownState(newStates);

                (_readerStates, newStates) = (newStates, _readerStates);

                FireEvents(arrivalEvents, removalEvents);
            }
            finally
            {
                newStates?.Dispose();
            }

            return true;
        }

        /// <summary>
        /// Checks if reader state list contains any changes.
        /// </summary>
        /// <param name="newStates">Reader states to check.</param>
        /// <returns>True if changes are detected.</returns>
        private static bool ReaderListChangeDetected(SCardReaderStates newStates)
        {
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
        /// Determines whether the relevant changes in state list detected and creates arrival and removal events.
        /// </summary>
        /// <param name="originalStates">Original states used to get Atr values for removal events.</param>
        /// <param name="newStates">State list to check for changes.</param>
        /// <param name="arrivalEvents">List where new arrival events will be added.</param>
        /// <param name="removalEvents">List where new removal events will be added.</param>
        private static void DetectRelevantChanges(SCardReaderStates originalStates, SCardReaderStates newStates, List<SCardEventArgs> arrivalEvents, List<SCardEventArgs> removalEvents)
        {
            foreach (SCardReaderStates.Entry entry in newStates)
            {
                SCARD_STATE diffState = entry.CurrentState ^ entry.EventState;

                if (diffState.HasFlag(SCARD_STATE.PRESENT) && entry.CurrentState.HasFlag(SCARD_STATE.PRESENT))
                {
                    IEnumerable<SCardReaderStates.Entry> states = originalStates.Where((e) => e.ReaderName == entry.ReaderName);
                    removalEvents.Add(new SCardEventArgs(entry.ReaderName, states.FirstOrDefault()?.Atr ?? entry.Atr));
                }
                else if (diffState.HasFlag(SCARD_STATE.PRESENT) && entry.EventState.HasFlag(SCARD_STATE.PRESENT))
                {
                    arrivalEvents.Add(new SCardEventArgs(entry.ReaderName, entry.Atr));
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
                }
            }
        }

        /// <summary>
        /// Updates the current context.
        /// </summary>
        private void UpdateCurrentContext()
        {
            uint _ = PlatformLibrary.Instance.SCard.EstablishContext(SCARD_SCOPE.USER, out SCardContext context);

            _context = context;
            _readerStates = GetReaderStateList();
        }

        /// <summary>
        /// Gets readers within the current context. 
        /// </summary>
        /// <returns><see cref="SCardReaderStates"/></returns>
        private SCardReaderStates GetReaderStateList()
        {
            uint _ = PlatformLibrary.Instance.SCard.ListReaders(_context, null, out string[] readerNames);

            var readerStates = new SCardReaderStates(readerNames.Prepend("\\\\?PnP?\\Notification").ToArray());

            return readerStates;
        }

        /// <summary>
        /// Fires all created events.
        /// </summary>
        /// <param name="arrivalEvents">List of arrival events to fire.</param>
        /// <param name="removalEvents">List of removal events to fire.</param>
        private void FireEvents(List<SCardEventArgs> arrivalEvents, List<SCardEventArgs> removalEvents)
        {
            if (CardArrival != null)
            {
                foreach (SCardEventArgs arrival in arrivalEvents)
                {
                    CardArrival.Invoke(this, arrival);
                }
            }

            if (CardRemoval != null)
            {
                foreach (SCardEventArgs removal in removalEvents)
                {
                    CardRemoval.Invoke(this, removal);
                }
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

        private static void ThrowIfFailed(uint errorCode)
        {
            if (errorCode != ErrorCode.SCARD_S_SUCCESS)
            {
                throw new PlatformApiException();
            }
        }

        private class ReaderStateComparer : IEqualityComparer<SCardReaderStates.Entry>
        {
            public bool Equals(SCardReaderStates.Entry x, SCardReaderStates.Entry y) => x.ReaderName == y.ReaderName;

            public int GetHashCode(SCardReaderStates.Entry obj) => obj.ReaderName.GetHashCode();
        }
    }
}
