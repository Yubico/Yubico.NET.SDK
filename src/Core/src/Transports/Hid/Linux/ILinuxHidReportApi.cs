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

// Synchronous, borrowed payload boundary. No method retains a span. Callers own and clear
// their buffers; the adapter owns and clears any temporary native framing in finally.
// Direct IO reads use descriptor-sized caller capacity, and Write/SetFeature preserve
// caller payload length within native bounds. Future typed owners enforce FIDO64/OTP8.
// Native zero report-id framing never escapes this seam. GetFeature remains OTP8
// payload / 9 native bytes; SET length is encoded in the ioctl's 14-bit size field.
// Only TryRead's Data prefix [..ActualBytes] is valid payload; all other dispositions
// have ActualBytes=0. The rest of the caller-owned input buffer is unspecified.
internal interface ILinuxHidReportApi
{
    LinuxFileSafeHandle Open(string path, NativeMethods.OpenFlags flags);
    (int InputSize, int OutputSize) GetReportSizes(LinuxFileSafeHandle handle);
    LinuxReadResult TryRead(LinuxFileSafeHandle handle, Span<byte> payload);
    void Write(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload);
    void GetFeature(LinuxFileSafeHandle handle, Span<byte> payload);
    void SetFeature(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload);
    LinuxReleaseOutcome CloseOnce(LinuxFileSafeHandle handle);
}

internal enum LinuxReadDisposition { Data, WouldBlock, Interrupted, EndOfStream, Error }
internal readonly record struct LinuxReadResult(LinuxReadDisposition Disposition, int ActualBytes, int Error);

// Lower-level synchronous syscall seam for exact ABI/framing tests of the production adapter.
// Counts include native report IDs. Error is captured immediately, never reconstructed later.
internal interface ILinuxHidNativeApi
{
    LinuxFileSafeHandle Open(string path, NativeMethods.OpenFlags flags);
    LinuxIoResult Read(LinuxFileSafeHandle handle, Span<byte> buffer);
    LinuxIoResult Write(LinuxFileSafeHandle handle, ReadOnlySpan<byte> buffer);
    LinuxIoResult Ioctl(LinuxFileSafeHandle handle, nuint request, Span<byte> buffer);
}

internal readonly record struct LinuxIoResult(nint Count, int Error);