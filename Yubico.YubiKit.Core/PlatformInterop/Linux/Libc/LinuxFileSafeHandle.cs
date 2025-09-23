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

using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace Yubico.YubiKit.Core.PlatformInterop.Linux.Libc;

// This class represents the libc file descriptor, the return from a call to
// open.
internal class LinuxFileSafeHandle : SafeHandle
{
    private const long InvalidHandle = 0x00000000FFFFFFFF;

    public LinuxFileSafeHandle()
        : base(new IntPtr(0x00000000FFFFFFFF), true)
    {
    }

    public override bool IsInvalid => handle.ToInt64() == InvalidHandle;

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    protected override bool ReleaseHandle() => NativeMethods.close(handle) == 0;
}