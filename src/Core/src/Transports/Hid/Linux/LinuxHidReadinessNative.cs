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

// Small syscall seam: fault tests can withhold poll without replacing registration mechanics.
internal interface ILinuxHidReadinessNative
{
    // Must return a private owned non-semaphore eventfd, never a borrowed device fd.
    // Only this local notification resource may be retired by the poller after poll exits.
    LinuxFileSafeHandle CreateWake();
    LinuxIoResult Poll(NativeMethods.PollFd[] descriptors);
    LinuxIoResult WriteWake(LinuxFileSafeHandle wake);
    LinuxIoResult ReadWake(LinuxFileSafeHandle wake);
}

internal sealed class LinuxHidReadinessNative : ILinuxHidReadinessNative
{
    public LinuxFileSafeHandle CreateWake()
    {
        var wake = NativeMethods.EventFd(0, NativeMethods.EFD_NONBLOCK | NativeMethods.EFD_CLOEXEC);
        if (!wake.IsInvalid)
            return wake;
        int error = Marshal.GetLastPInvokeError();
        wake.Dispose();
        throw new IOException($"eventfd failed (errno {error}).");
    }

    public LinuxIoResult Poll(NativeMethods.PollFd[] descriptors)
    {
        int count = NativeMethods.Poll(descriptors, descriptors.Length, -1);
        return new LinuxIoResult(count, Marshal.GetLastPInvokeError());
    }

    public LinuxIoResult WriteWake(LinuxFileSafeHandle wake)
    {
        Span<byte> value = stackalloc byte[8];
        _ = BitConverter.TryWriteBytes(value, 1UL);
        nint count = NativeMethods.Write(wake, value);
        return new LinuxIoResult(count, Marshal.GetLastPInvokeError());
    }

    public LinuxIoResult ReadWake(LinuxFileSafeHandle wake)
    {
        Span<byte> value = stackalloc byte[8];
        nint count = NativeMethods.Read(wake, value);
        return new LinuxIoResult(count, Marshal.GetLastPInvokeError());
    }
}