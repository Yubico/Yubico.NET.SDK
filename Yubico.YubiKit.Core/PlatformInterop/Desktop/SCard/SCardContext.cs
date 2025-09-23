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

using Microsoft.Win32.SafeHandles;
using System.Runtime.ConstrainedExecution;

namespace Yubico.YubiKit.Core.PlatformInterop.Desktop.SCard;

/// <summary>
///     A safe-handle wrapper for the SCard context handle.
/// </summary>
internal class SCardContext : SafeHandleZeroOrMinusOneIsInvalid
{
    public SCardContext() :
        base(true)
    {
    }

    public SCardContext(IntPtr handle) :
        base(true) =>
        SetHandle(handle);

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    protected override bool ReleaseHandle() => NativeMethods.SCardReleaseContext(handle) == ErrorCode.SCARD_S_SUCCESS;
}