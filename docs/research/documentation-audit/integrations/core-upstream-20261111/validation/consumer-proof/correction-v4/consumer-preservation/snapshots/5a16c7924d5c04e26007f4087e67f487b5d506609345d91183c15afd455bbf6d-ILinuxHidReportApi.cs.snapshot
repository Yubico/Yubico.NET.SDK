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

/// <summary>Synchronous, borrowed-payload access to unnumbered Linux HID reports.</summary>
/// <remarks>
/// No method retains a span or owns the caller's buffers. Callers clear sensitive payloads;
/// the adapter clears its temporary native framing in finally. Calls may block in native code:
/// owners put open, metadata, output, feature I/O and close on their dedicated worker, while
/// only nonblocking input belongs on the read reactor. Typed connections enforce FIDO64/OTP8;
/// direct report access preserves supported caller lengths. No synthetic report-ID byte escapes.
/// </remarks>
internal interface ILinuxHidReportApi
{
    /// <summary>Opens a caller-owned descriptor with close-on-exec, or throws on native failure.</summary>
    /// <remarks>The owner must drain all borrowers and inspect <see cref="CloseOnce"/> before releasing its physical claim.</remarks>
    LinuxFileSafeHandle Open(string path, NativeMethods.OpenFlags flags);
    /// <summary>Gets bounded input/output payload-size metadata, not actual transfer counts.</summary>
    /// <remarks>Unavailable descriptor ioctls fall back to 64/64. Invalid sizes and numbered reports fail; parsing is YubiKey-specific.</remarks>
    (int InputSize, int OutputSize) GetReportSizes(LinuxFileSafeHandle handle);
    /// <summary>Attempts one read into borrowed capacity, without padding short input or retrying.</summary>
    /// <remarks>
    /// The caller must open nonblocking for reactor use. Only the Data prefix [..ActualBytes]
    /// is valid; the remainder is unspecified and caller-owned. Capacity is at most 16384 bytes.
    /// </remarks>
    LinuxReadResult TryRead(LinuxFileSafeHandle handle, Span<byte> payload);
    /// <summary>Writes one payload (0..16383 bytes), adding native report ID zero and requiring the complete framed count.</summary>
    /// <remarks>Does not require descriptor output length. Failure does not imply the device saw no mutation; no suffix retry is made.</remarks>
    void Write(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload);
    /// <summary>Gets one eight-byte OTP payload, copying only after an exact nine-byte native transfer with report ID zero.</summary>
    /// <remarks>Failure leaves the caller destination unchanged; owned native framing is cleared on every exit.</remarks>
    void GetFeature(LinuxFileSafeHandle handle, Span<byte> payload);
    /// <summary>Sets one caller-length payload (0..16382 bytes) using zero-ID framing and a length-encoded feature ioctl.</summary>
    /// <remarks>Requires the complete payload-plus-one native count. Does not retry or impose typed OTP length.</remarks>
    void SetFeature(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload);
    /// <summary>Requests exactly-once descriptor disposal and observes cached native release evidence.</summary>
    /// <remarks>
    /// Returning is not release proof while borrowers or release remain pending. Only Released
    /// permits claim release; Linux EINTR/EIO preserve diagnostics but prove release, unlike EBADF
    /// or unknown outcomes. Never retry native close or substitute a timeout for release evidence.
    /// </remarks>
    LinuxReleaseOutcome CloseOnce(LinuxFileSafeHandle handle);
}

/// <summary>Outcome of one native input attempt, independent of descriptor-size metadata.</summary>
internal enum LinuxReadDisposition
{
    /// <summary>A positive actual payload count is available.</summary>
    Data,
    /// <summary>Nonblocking read returned EAGAIN; no payload was delivered.</summary>
    WouldBlock,
    /// <summary>Read returned EINTR; the owner decides whether a bounded retry is allowed.</summary>
    Interrupted,
    /// <summary>Read returned zero; the FIDO owner treats this as terminal, not an empty report.</summary>
    EndOfStream,
    /// <summary>Read failed with another captured native error.</summary>
    Error
}
/// <summary>Payload-level result of one input attempt; contains no owned report storage.</summary>
/// <param name="Disposition">Classification of the native read outcome.</param>
/// <param name="ActualBytes">Valid payload prefix length for Data; zero for every other disposition.</param>
/// <param name="Error">Immediately captured errno; meaningful for failed reads, not evidence of failure after success.</param>
internal readonly record struct LinuxReadResult(LinuxReadDisposition Disposition, int ActualBytes, int Error);

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