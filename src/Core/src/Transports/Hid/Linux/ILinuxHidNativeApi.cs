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

using Yubico.YubiKit.Core.Native.Linux.Libc;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>Borrowed synchronous syscall seam beneath report framing, also used by exact ABI/framing tests.</summary>
/// <remarks>No method retains buffers. Native counts include any report-ID framing supplied in the buffer.</remarks>
internal interface ILinuxHidNativeApi
{
    /// <summary>Acquires a descriptor with the requested flags; returns an invalid handle on failure with errno preserved for immediate capture.</summary>
    LinuxFileSafeHandle Open(string path, NativeMethods.OpenFlags flags);
    /// <summary>Performs one read into borrowed storage and captures count/errno before another native call can overwrite them.</summary>
    LinuxIoResult Read(LinuxFileSafeHandle handle, Span<byte> buffer);
    /// <summary>Performs one write from borrowed native-framed storage and captures count/errno without retry.</summary>
    LinuxIoResult Write(LinuxFileSafeHandle handle, ReadOnlySpan<byte> buffer);
    /// <summary>Performs one ioctl with borrowed request storage; Count is request-specific status or transfer count.</summary>
    LinuxIoResult Ioctl(LinuxFileSafeHandle handle, nuint request, Span<byte> buffer);
}

/// <summary>Native return value and errno captured together, with no deferred thread-local error lookup.</summary>
/// <param name="Count">Native return: byte count for read/write, ready-descriptor count for poll, request-specific result for ioctl; -1 on failure.</param>
/// <param name="Error">Captured errno, meaningful only when the native return indicates failure; it may be stale on success.</param>
internal readonly record struct LinuxIoResult(nint Count, int Error);