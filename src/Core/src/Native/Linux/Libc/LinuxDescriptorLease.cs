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

using System.Runtime.InteropServices;

namespace Yubico.YubiKit.Core.Native.Linux.Libc;

// Every raw int extracted for a native call remains borrowed from its SafeHandle.
// A future reactor must retain a SafeHandle reference for its entire registration,
// not just poll(). This stack-only lease is for synchronous native call scopes.
internal readonly ref struct LinuxDescriptorLease : IDisposable
{
    private readonly SafeHandle _handle;

    public LinuxDescriptorLease(SafeHandle handle)
    {
        bool acquired = false;
        handle.DangerousAddRef(ref acquired);
        _handle = handle;
        Descriptor = handle.DangerousGetHandle().ToInt32();
    }

    public int Descriptor { get; }

    // libudev owns its monitor fd; retain that SafeHandle without treating its native
    // pointer as an int descriptor. The source owner supplies the associated fd.
    public LinuxDescriptorLease(SafeHandle owner, int descriptor)
    {
        bool acquired = false;
        owner.DangerousAddRef(ref acquired);
        _handle = owner;
        Descriptor = descriptor;
    }

    public void Dispose()
    {
        // The last borrower can execute close here. Do not overwrite the syscall's errno
        // with that separate release operation before the caller captures it.
        int error = Marshal.GetLastPInvokeError();
        _handle.DangerousRelease();
        Marshal.SetLastPInvokeError(error);
    }
}