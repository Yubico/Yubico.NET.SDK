// Copyright 2026 Yubico AB
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
using System.Threading;
using Yubico.Core.Devices.SmartCard;
using Yubico.PlatformInterop;

namespace Yubico.Core.Performance.Legacy
{
    /// <summary>
    /// Simplified snapshot of the pre-#445 (develop) DesktopSmartCardDeviceListener behavior
    /// that exhibits the busy-loop bug when SCARD_E_INVALID_HANDLE is returned repeatedly.
    ///
    /// This is NOT a full port of the 513-line develop file, but rather a minimal distillation
    /// that preserves the essential characteristic: no recovery path for SCARD_E_INVALID_HANDLE,
    /// so GetStatusChange is called in a tight loop with no sleep when the context is dead.
    ///
    /// Used exclusively for BenchmarkDotNet performance comparison to prove PR #445's fix.
    /// </summary>
    internal sealed class LegacyDesktopSmartCardDeviceListener : SmartCardDeviceListener
    {
        private readonly ISCardInterop _scard;
        private SCardContext _context;
        private Thread? _listenerThread;
        private volatile bool _isListening;

        public LegacyDesktopSmartCardDeviceListener(ISCardInterop scard)
        {
            _scard = scard;
            Status = DeviceListenerStatus.Stopped;

            uint result = _scard.EstablishContext(SCARD_SCOPE.USER, out SCardContext context);
            if (result != ErrorCode.SCARD_S_SUCCESS)
            {
                context.Dispose();
                _context = new SCardContext(IntPtr.Zero);
                Status = DeviceListenerStatus.Error;
                return;
            }

            _context = context;
            StartListening();
        }

        private void StartListening()
        {
            _listenerThread = new Thread(BusyLoopOnInvalidHandle)
            {
                IsBackground = true
            };
            _isListening = true;
            Status = DeviceListenerStatus.Started;
            _listenerThread.Start();
        }

        /// <summary>
        /// Simplified representation of the pre-#445 busy-loop bug: when GetStatusChange
        /// returns SCARD_E_INVALID_HANDLE, the listener does NOT re-establish context or
        /// sleep — it immediately calls GetStatusChange again, resulting in a tight loop.
        /// </summary>
        private void BusyLoopOnInvalidHandle()
        {
            // Probe call to determine UsePnpWorkaround (mimics the real code's first call)
            var probeStates = SCARD_READER_STATE.CreateFromReaderNames(new[] { "\\\\?\\Pnp\\Notifications" });
            _ = _scard.GetStatusChange(_context, 0, probeStates, probeStates.Length);

            var readerStates = SCARD_READER_STATE.CreateFromReaderNames(new[] { "\\\\?\\Pnp\\Notifications" });

            while (_isListening)
            {
                // This is the essence of the pre-#445 bug:
                // - Call GetStatusChange with 100ms timeout
                // - If it returns SCARD_E_INVALID_HANDLE, the switch/if logic in the old code
                //   does NOT recognize it as recoverable, so control returns to the top of
                //   the while loop immediately
                // - No Thread.Sleep, no context re-establishment → busy spin
                uint result = _scard.GetStatusChange(_context, 100, readerStates, readerStates.Length);

                // Pre-#445 logic only handled SCARD_E_TIMEOUT and a few other codes.
                // SCARD_E_INVALID_HANDLE was NOT in the recoverable list, so the loop
                // continues spinning. This simplified version makes that explicit:
                if (result == ErrorCode.SCARD_E_TIMEOUT)
                {
                    // Normal case: timeout, continue polling
                    continue;
                }

                // For any other error (including SCARD_E_INVALID_HANDLE), the old code
                // did not sleep or recover — it just kept looping. That's the bug.
                // Here we explicitly do nothing, which causes the tight loop.
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isListening = false;
                _ = _scard.Cancel(_context);
                _listenerThread?.Join(TimeSpan.FromSeconds(2));
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
