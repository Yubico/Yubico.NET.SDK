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

/// <summary>Synchronous poll/eventfd seam allowing readiness fault injection without replacing registration mechanics.</summary>
/// <remarks>These are native calls, not async or hard wall-clock-bounded operations. Results capture errno immediately.</remarks>
internal interface ILinuxHidReadinessNative
{
    /// <summary>Creates a private owned nonblocking, close-on-exec, non-semaphore eventfd or throws.</summary>
    /// <remarks>Never returns a borrowed device fd. Only this local notification resource may be retired on the poller after poll exits.</remarks>
    LinuxFileSafeHandle CreateWake();
    /// <summary>Waits indefinitely for readiness and updates the borrowed descriptors' revents; returns ready count or native failure.</summary>
    /// <remarks>The reactor retains device references across the snapshot. No descriptor array is retained after return.</remarks>
    LinuxIoResult Poll(NativeMethods.PollFd[] descriptors);
    /// <summary>Attempts one eight-byte increment of the borrowed wake eventfd; caller interprets EAGAIN/EINTR and count.</summary>
    LinuxIoResult WriteWake(LinuxFileSafeHandle wake);
    /// <summary>Attempts one eight-byte non-semaphore read to drain the wake counter; caller validates count and errno.</summary>
    LinuxIoResult ReadWake(LinuxFileSafeHandle wake);
}

/// <summary>Production libc poll/eventfd adapter used by <see cref="LinuxHidReadReactor"/>.</summary>
internal sealed class LinuxHidReadinessNative : ILinuxHidReadinessNative
{
    /// <inheritdoc />
    public LinuxFileSafeHandle CreateWake()
    {
        var wake = NativeMethods.EventFd(0, NativeMethods.EFD_NONBLOCK | NativeMethods.EFD_CLOEXEC);
        if (!wake.IsInvalid)
            return wake;
        int error = Marshal.GetLastPInvokeError();
        wake.Dispose();
        throw new IOException($"eventfd failed (errno {error}).");
    }

    /// <inheritdoc />
    public LinuxIoResult Poll(NativeMethods.PollFd[] descriptors)
    {
        int count = NativeMethods.Poll(descriptors, descriptors.Length, -1);
        return new LinuxIoResult(count, Marshal.GetLastPInvokeError());
    }

    /// <inheritdoc />
    public LinuxIoResult WriteWake(LinuxFileSafeHandle wake)
    {
        Span<byte> value = stackalloc byte[8];
        _ = BitConverter.TryWriteBytes(value, 1UL);
        nint count = NativeMethods.Write(wake, value);
        return new LinuxIoResult(count, Marshal.GetLastPInvokeError());
    }

    /// <inheritdoc />
    public LinuxIoResult ReadWake(LinuxFileSafeHandle wake)
    {
        Span<byte> value = stackalloc byte[8];
        nint count = NativeMethods.Read(wake, value);
        return new LinuxIoResult(count, Marshal.GetLastPInvokeError());
    }
}