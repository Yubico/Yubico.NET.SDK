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

using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.ConstrainedExecution;
using Yubico.Core.Devices.SmartCard;
using Yubico.Core.Logging;

namespace Yubico.PlatformInterop
{
    /// <summary>
    /// A safe-handle wrapper for the SCard card handle.
    /// </summary>
    internal class SCardCardHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private readonly ILogger _log = Log.GetLogger();

        public SCARD_DISPOSITION ReleaseDisposition { get; set; }

        public SCardCardHandle(IntPtr handle) :
            base(true)
        {
            SetHandle(handle);
            ReleaseDisposition = SCARD_DISPOSITION.RESET_CARD;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            _log.LogInformation(
                "Disconnecting from Smart Card. Handle = [{Handle:X}], Release Disposition = [{ReleaseDisposition}]",
                handle.ToInt64(),
                ReleaseDisposition);

            uint result = PlatformLibrary.Instance.SCard.Disconnect(handle, ReleaseDisposition);
            _log.SCardApiCall(nameof(SCard.Disconnect), result);

            return result == ErrorCode.SCARD_S_SUCCESS;
        }
    }
}
