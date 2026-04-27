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

namespace Yubico.PlatformInterop
{
    /// <summary>
    /// Abstraction over the WinSCard / PCSC smart card API surface used by the device listener.
    /// </summary>
    /// <remarks>
    /// Exists primarily to enable injection of test doubles so that error-handling paths in
    /// <c>DesktopSmartCardDeviceListener</c> can be exercised without requiring real smart card
    /// hardware or a Windows terminal-services environment.
    /// </remarks>
    internal interface ISCardInterop
    {
        /// <summary>Wraps SCardEstablishContext.</summary>
        uint EstablishContext(SCARD_SCOPE scope, out SCardContext context);

        /// <summary>Wraps SCardGetStatusChange.</summary>
        uint GetStatusChange(SCardContext context, int timeout, SCARD_READER_STATE[] readerStates, int readerStatesCount);

        /// <summary>Wraps the high-level SCardListReaders overload that handles the two-call Windows pattern.</summary>
        uint ListReaders(SCardContext context, string[]? groups, out string[] readerNames);

        /// <summary>Wraps SCardCancel.</summary>
        uint Cancel(SCardContext context);
    }
}
