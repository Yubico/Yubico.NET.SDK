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
using System.Threading;
using Yubico.PlatformInterop;

namespace Yubico.Core.Performance.Mocks
{
    /// <summary>
    /// Test mock implementing <see cref="ISCardInterop"/> that simulates persistent
    /// <c>SCARD_E_INVALID_HANDLE</c> failures. Used for performance benchmarking of
    /// recovery-path behavior.
    /// </summary>
    internal sealed class AlwaysInvalidHandleScardInterop : ISCardInterop
    {
        private int _establishContextCallCount;
        private int _getStatusChangeCallCount;
        private int _postProbeInvocations;

        /// <summary>
        /// Total number of GetStatusChange calls after the initial probe.
        /// Thread-safe for reading from the benchmark thread after observation window.
        /// </summary>
        public int Invocations => Volatile.Read(ref _postProbeInvocations);

        public uint EstablishContext(SCARD_SCOPE scope, out SCardContext context)
        {
            int callNum = Interlocked.Increment(ref _establishContextCallCount);
            // Return a distinct non-zero handle on success, matching real WinSCard behavior.
            context = new SCardContext(new IntPtr(callNum));
            return ErrorCode.SCARD_S_SUCCESS;
        }

        public uint GetStatusChange(SCardContext context, int timeout, SCARD_READER_STATE[] readerStates, int readerStatesCount)
        {
            int callNum = Interlocked.Increment(ref _getStatusChangeCallCount);

            // Call #1 is the UsePnpWorkaround probe (timeout=0).
            // Return SCARD_E_TIMEOUT so UsePnpWorkaround returns false cleanly.
            if (callNum == 1)
            {
                return ErrorCode.SCARD_E_TIMEOUT;
            }

            // All subsequent calls return SCARD_E_INVALID_HANDLE and increment the counter.
            Interlocked.Increment(ref _postProbeInvocations);
            return ErrorCode.SCARD_E_INVALID_HANDLE;
        }

        public uint ListReaders(SCardContext context, string[]? groups, out string[] readerNames)
        {
            readerNames = Array.Empty<string>();
            return ErrorCode.SCARD_E_NO_READERS_AVAILABLE;
        }

        public uint Cancel(SCardContext context)
        {
            return ErrorCode.SCARD_S_SUCCESS;
        }
    }
}
