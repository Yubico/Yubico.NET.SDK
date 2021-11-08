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
    /// This class lists the readers and listens for changes in readers  states.
    /// </summary>
    public class SCardListener : IDisposable
    {
        // The resource manager context.
        private readonly SCardContext _context;

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
            ThrowIfFailed(result);

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

                ThrowIfFailed(getStatusChangeResult);

                while (ReaderListChangeDetected(newStates))
                {
                    using SCardReaderStates eventStateList = GetReaderStateList();
                    IEnumerable<SCardReaderStates.Entry> addedReaderStates = eventStateList.Except(newStates, new ReaderStateComparer());
                    IEnumerable<SCardReaderStates.Entry> removedReaderStates = newStates.Except(eventStateList, new ReaderStateComparer());

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

                            ThrowIfFailed(getStatusChangeResult);
                        }

                        (newStates, updatedStates) = (updatedStates, newStates);
                    }
                    finally
                    {
                        updatedStates?.Dispose();
                    }
                }

                if (sendEvents)
                {
                    DetectRelevantChanges(newStates, arrivalEvents, removalEvents);
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

        private static void DetectRelevantChanges(SCardReaderStates newStates, List<SCardEventArgs> arrivalEvents, List<SCardEventArgs> removalEvents)
        {
            for (int i = 0; i < newStates.Count; i++)
            {
                SCARD_STATE diffState = newStates[i].CurrentState ^ newStates[i].EventState;

                if (diffState.HasFlag(SCARD_STATE.PRESENT) && newStates[i].CurrentState.HasFlag(SCARD_STATE.PRESENT))
                {
                    removalEvents.Add(new SCardEventArgs(newStates[i].ReaderName, newStates[i].Atr));
                }
                else if (diffState.HasFlag(SCARD_STATE.PRESENT) && newStates[i].EventState.HasFlag(SCARD_STATE.PRESENT))
                {
                    arrivalEvents.Add(new SCardEventArgs(newStates[i].ReaderName, newStates[i].Atr));
                }
            }
        }

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

        private static void UpdateCurrentlyKnownState(SCardReaderStates newStates)
        {
            for (int i = 0; i < newStates.Count; i++)
            {
                if (newStates[i].EventState.HasFlag(SCARD_STATE.CHANGED))
                {
                    newStates[i].CurrentState = newStates[i].EventState & ~SCARD_STATE.CHANGED;
                    newStates[i].EventState = 0;
                }
            }
        }

        private SCardReaderStates GetReaderStateList()
        {
            uint result = PlatformLibrary.Instance.SCard.ListReaders(_context, null, out string[] readerNames);
            ThrowIfFailed(result);

            var readerStates = new SCardReaderStates(readerNames.Prepend("\\\\?PnP?\\Notification").ToArray());

            return readerStates;
        }

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

        private class ReaderStateComparer : IEqualityComparer<SCardReaderStates.Entry>
        {
            public bool Equals(SCardReaderStates.Entry x, SCardReaderStates.Entry y) => x.ReaderName == y.ReaderName;

            public int GetHashCode(SCardReaderStates.Entry obj) => obj.ReaderName.GetHashCode();
        }

        private static void ThrowIfFailed(uint errorCode)
        {
            if (errorCode != ErrorCode.SCARD_S_SUCCESS)
            {
                throw new PlatformApiException();
            }
        }
    }
}
