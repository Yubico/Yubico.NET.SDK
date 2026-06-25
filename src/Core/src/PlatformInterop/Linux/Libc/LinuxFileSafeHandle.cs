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

using System.Runtime.InteropServices;

namespace Yubico.YubiKit.Core.PlatformInterop.Linux.Libc;

// This class represents the libc file descriptor, the return from a call to
// open.
internal class LinuxFileSafeHandle : SafeHandle
{
    public LinuxFileSafeHandle()
        : base(new IntPtr(-1), true)
    {
    }

    // On Linux, open() returns -1 on failure.
    // We also treat 0 as invalid since fd 0 is stdin.
    public override bool IsInvalid => handle == new IntPtr(-1) || handle == IntPtr.Zero;

    protected override bool ReleaseHandle() => NativeMethods.close(handle) == 0;
}