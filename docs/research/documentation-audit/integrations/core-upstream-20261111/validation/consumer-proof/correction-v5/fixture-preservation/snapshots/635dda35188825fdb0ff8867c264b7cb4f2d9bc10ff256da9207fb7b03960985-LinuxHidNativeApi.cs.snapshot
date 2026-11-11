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
using Yubico.YubiKit.Core.Native.Linux.Libc;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

// glibc libc.so.6 adapter. Each wrapper pins the borrowed span and retains the handle
// only for the synchronous syscall; none of these methods promises asynchronous I/O.
internal sealed class LinuxHidNativeApi : ILinuxHidNativeApi
{
    public LinuxFileSafeHandle Open(string path, NativeMethods.OpenFlags flags) => NativeMethods.Open(path, flags);

    public LinuxIoResult Read(LinuxFileSafeHandle handle, Span<byte> buffer)
    {
        nint count = NativeMethods.Read(handle, buffer);
        int error = Marshal.GetLastPInvokeError();
        return new LinuxIoResult(count, error);
    }

    public LinuxIoResult Write(LinuxFileSafeHandle handle, ReadOnlySpan<byte> buffer)
    {
        nint count = NativeMethods.Write(handle, buffer);
        int error = Marshal.GetLastPInvokeError();
        return new LinuxIoResult(count, error);
    }

    public LinuxIoResult Ioctl(LinuxFileSafeHandle handle, nuint request, Span<byte> buffer)
    {
        int count = NativeMethods.Ioctl(handle, request, buffer);
        int error = Marshal.GetLastPInvokeError();
        return new LinuxIoResult(count, error);
    }
}