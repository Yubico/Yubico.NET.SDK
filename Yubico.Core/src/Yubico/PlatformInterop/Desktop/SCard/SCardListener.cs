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

using static Yubico.PlatformInterop.NativeMethods;

namespace Yubico.PlatformInterop
{

#if false

    internal class SCardCardEventArgs : EventArgs
    {
        public SCardReader Reader { get; private set; }
        public IList<byte> CardAtr { get; private set; }

        public SCardCardEventArgs(string readerName, IList<byte> cardAtr)
        {
            Reader = new SCardReader(readerName);
            CardAtr = cardAtr;
        }
    }

    internal class SCardListener : IDisposable
    {
        private readonly IntPtr _context;
        private SCARD_READERSTATE[] _readerStates;
        private readonly Thread _listenerThread;

        public event EventHandler<SCardCardEventArgs>? CardArrival;
        public event EventHandler<SCardCardEventArgs>? CardRemoval;

        public SCardListener()
        {
            ErrorCode errorCode = SCardEstablishContext(SCARD_SCOPE.USER, out _context);
            ThrowIfFailed(errorCode);

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

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                _ = SCardReleaseContext(_context); // TODO: Error handling

                disposedValue = true;
            }
        }

        ~SCardListener()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    #endregion

        public void StopListening()
        {
            ThrowIfFailed(SCardCancel(_context));
            _listenerThread.Join();
        }

        private bool ListenForReaderChanges(int timeout)
        {
            var arrivalEvents = new List<SCardCardEventArgs>();
            var removalEvents = new List<SCardCardEventArgs>();
            bool sendEvents = timeout != 0;

            var newStates = (SCARD_READERSTATE[])_readerStates.Clone();

            ErrorCode code = SCardGetStatusChange(
                _context,
                timeout,
                newStates,
                newStates.Length
                );

            if (code == ErrorCode.SCARD_E_CANCELLED)
            {
                return false;
            }

            ThrowIfFailed(code);

            while (ReaderListChangeDetected(newStates))
            {
                SCARD_READERSTATE[] eventStateList = GetReaderStateList();

                IEnumerable<SCARD_READERSTATE> addedReaderStates = eventStateList.Except(newStates, new ReaderStateComparer());
                IEnumerable<SCARD_READERSTATE> removedReaderStates = newStates.Except(eventStateList, new ReaderStateComparer());

                var readerStateList = newStates.ToList();
                readerStateList.AddRange(addedReaderStates);

                newStates = readerStateList.Except(removedReaderStates, new ReaderStateComparer()).ToArray();

                // Special handle readers with cards in them
                if (sendEvents)
                {
                    foreach (SCARD_READERSTATE removedReader in removedReaderStates.Where(r => r.dwCurrentState.HasFlag(SCARD_STATE.PRESENT)))
                    {
                        removalEvents.Add(new SCardCardEventArgs(removedReader.szReader, removedReader.Atr));
                    }
                }

                // Only call get status change if a new reader was added. If nothing was added,
                // we would otherwise hang / timeout here because all changes (in SCard's mind)
                // have been dealt with.
                if (addedReaderStates.Any())
                {
                    code = SCardGetStatusChange(
                        _context,
                        0,
                        newStates,
                        newStates.Length
                        );

                    if (code == ErrorCode.SCARD_E_CANCELLED)
                    {
                        return false;
                    }

                    ThrowIfFailed(code);
                }
            }

            if (sendEvents)
            {
                DetectRelevantChanges(newStates, arrivalEvents, removalEvents);
            }

            UpdateCurrentlyKnownState(newStates);

            FireEvents(arrivalEvents, removalEvents);

            return true;
        }

        private static bool ReaderListChangeDetected(SCARD_READERSTATE[] newStates)
        {
            if (newStates[0].EventState.HasFlag(SCARD_STATE.CHANGED))
            {
                newStates[0].dwCurrentState = newStates[0].dwEventState & ~SCARD_STATE.CHANGED;
                newStates[0].dwEventState = 0;

                return true;
            }

            return false;
        }

        private void DetectRelevantChanges(SCARD_READERSTATE[] newStates, List<SCardCardEventArgs> arrivalEvents, List<SCardCardEventArgs> removalEvents)
        {
            for (int i = 0; i < newStates.Length; i++)
            {
                SCARD_STATE diffState = newStates[i].CurrentState ^ newStates[i].EventState;

                if (diffState.HasFlag(SCARD_STATE.PRESENT) && newStates[i].CurrentState.HasFlag(SCARD_STATE.PRESENT))
                {
                    removalEvents.Add(new SCardCardEventArgs(newStates[i].szReader, _readerStates[i].Atr)); // Use of _readerStates here so that we can get the old ATR
                }
                else if (diffState.HasFlag(SCARD_STATE.PRESENT) && newStates[i].EventState.HasFlag(SCARD_STATE.PRESENT))
                {
                    arrivalEvents.Add(new SCardCardEventArgs(newStates[i].szReader, newStates[i].Atr));
                }
            }
        }

        private void FireEvents(List<SCardCardEventArgs> arrivalEvents, List<SCardCardEventArgs> removalEvents)
        {
            if (CardArrival != null)
            {
                foreach (SCardCardEventArgs arrival in arrivalEvents)
                {
                    CardArrival.Invoke(this, arrival);
                }
            }

            if (CardRemoval != null)
            {
                foreach (SCardCardEventArgs removal in removalEvents)
                {
                    CardRemoval.Invoke(this, removal);
                }
            }
        }

        private void UpdateCurrentlyKnownState(SCARD_READERSTATE[] newStates)
        {
            _readerStates = newStates;

            for (int i = 0; i < _readerStates.Length; i++)
            {
                if (_readerStates[i].dwEventState.HasFlag(SCARD_STATE.CHANGED))
                {
                    _readerStates[i].dwCurrentState = _readerStates[i].dwEventState & ~SCARD_STATE.CHANGED;
                    _readerStates[i].dwEventState = 0;
                }
            }
        }

        private static SCARD_READERSTATE[] GetReaderStateList() =>
            SCardReader
                .GetList()
                .Select(r => new SCARD_READERSTATE()
                {
                    szReader = r.Name,
                    dwCurrentState = SCARD_STATE.UNAWARE
                })
                .Prepend(new SCARD_READERSTATE()
                {
                    szReader = "\\\\?PnP?\\Notification"
                })
                .ToArray();

        private class ReaderStateComparer : IEqualityComparer<SCARD_READERSTATE>
        {
            public bool Equals(SCARD_READERSTATE x, SCARD_READERSTATE y) => x.szReader == y.szReader;

            public int GetHashCode(SCARD_READERSTATE obj) =>
                obj.szReader.GetHashCode();
        }

        private static void ThrowIfFailed(ErrorCode errorCode)
        {
            if (errorCode != ErrorCode.SCARD_S_SUCCESS)
            {
                throw new PlatformApiException();
            }
        }
    }
#endif
}
