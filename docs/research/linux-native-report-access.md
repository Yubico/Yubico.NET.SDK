# Linux native report access — L1

Scope: native ABI, SafeHandle release evidence, and a synchronous borrowed-report seam.
Linux production HID remains synchronous. No reactor, FIDO/OTP lifetime worker, claim-transfer
protocol, native-AOT acceptance row, or universal Linux verification is introduced here.
The accepted [L0 baseline](linux-async-boundaries-baseline.md) remains separate.

## Native ABI and descriptor ownership

All entry points use `LibraryImport` and `DefaultDllImportSearchPaths`. The library is glibc
`libc.so.6`; this is not a musl compatibility claim. Native declarations have these shapes:

| Entry | Native shape |
|---|---|
| `open` | UTF-8 path, int flags → int |
| `eventfd` | uint initial value, int flags → int |
| `close` | int fd → int |
| `read` / `write` | int fd, byte pointer, nuint size_t → nint ssize_t |
| `poll` | pollfd array, nuint nfds_t, int timeout → int |
| `ioctl` | int fd, nuint glibc request, void pointer → int |
| `fcntl` | int fd, int command, int flags → int |

`open` and `eventfd` allocate their SafeHandle before acquiring the descriptor, capture native
int/errno, initialize without another allocation, and preserve the error for existing callers.
This avoids the default SafeHandle marshaller's **IntPtr** unmanaged representation: a native
int -1 must not be interpreted as zero-extended 0x00000000FFFFFFFF on x64. fd 0 is valid.
File opens include `O_CLOEXEC`; production eventfd creation supplies `EFD_CLOEXEC`.
The old byte-array read/write and int-count poll entry points are thin managed wrappers,
not duplicate imports. The pointer/span syscall wrappers retain handles through each call,
including errno preservation if the last borrower executes a deferred close. The existing
listener also retains its shutdown descriptor and libudev monitor owner through poll.

The only native close invocation is in `LinuxFileSafeHandle.ReleaseHandle`. The eventfd
SafeHandle shares it. `CloseOnce` calls Dispose and reads a published, exactly-once outcome:

```csharp
LinuxReleaseOutcome(bool Executed, bool Released, int NativeResult, int Error)
```

* Outstanding references / in-progress release: default outcome, no release proof. After all
  references drain, call again to observe the cached result. `Dispose` returning is not proof.
* Native 0: released. Native -1/EINTR or -1/EIO on Linux: released with the diagnostic preserved.
* EBADF: ownership failure, **not** release proof. Other unexpected errors are conservative
  unknown-release outcomes. Nothing retries native close, including repeated Dispose/CloseOnce.
* The finalizer path cannot throw. An unexpected managed release exception publishes no proof.
* Future worker owners must drain native/reactor references, inspect the outcome, and retain
  ownership on no proof. L1 does not retrofit that protocol into existing synchronous owners.

## Report contract

`ILinuxHidReportApi` exposes `Open(path, flags)`, `GetReportSizes(handle)`,
`TryRead(handle, Span<byte>)`, `Write(handle, ReadOnlySpan<byte>)`,
`GetFeature(handle, Span<byte>)`, `SetFeature(handle, ReadOnlySpan<byte>)`, and `CloseOnce(handle)`.
Open returns a SafeHandle or throws; GetReportSizes returns payload input/output lengths.
The seam is mechanical: direct IO uses supported descriptor sizes, and direct SET calls retain
the caller's payload length. Future typed FIDO/OTP lifetime owners enforce exactly 64/8 bytes;
those protocol constraints must not narrow the existing direct synchronous facades.
TryRead returns disposition, actual payload bytes, and captured errno. Data, would-block,
interrupted, end-of-stream and terminal error are distinct; a short read is not padded into
a complete report. Existing synchronous connections retain their short-read behavior.

| Operation | Borrowed payload | Native framing / success count |
|---|---|---|
| Direct IO input | descriptor-sized capacity (up to 4096 here), actual count returned | unnumbered input, no synthetic byte |
| Direct IO SET | caller-length payload (up to 16383 bytes) | `[0] + payload`; success = payload length + 1, up to 16384 |
| OTP GET feature | exactly 8-byte destination | 9 bytes: `[0] + payload`; GET request `0xC0094807`; success = 9 |
| Direct feature SET | caller-length payload (up to 16382 bytes) | `[0] + payload`; SET request `0xC0004806 \| ((length + 1) << 16)`; success = length + 1 |

For the established typed payloads these still produce FIDO64/native65 and OTP8/native9,
including SET `0xC0094806`. The IO limit comes from Linux `HID_MAX_BUFFER_SIZE = 16384`;
the feature SET framing must also fit the generic ioctl's 14-bit size field (16383 maximum).
`TryRead` accepts bounded caller-provided capacity up to 16384 and reports actual bytes.
This does not promise that arbitrary payloads describe a supported device protocol.

Spans are borrowed only during each synchronous native call. No method retains a pointer,
span, or caller buffer. Native framing up to 512 bytes uses stack storage; larger framing uses
a bounded pool rental, sliced to the actual native length. Both are cleared with
`CryptographicOperations.ZeroMemory` in `finally`, including exceptions, before any pool return.
Input is copied to the caller only after a complete feature
GET; short/oversized counts and native errors do not become payload. Writes and feature
transfers are one attempt, never suffix retries. No report data is logged. Returned connection
arrays belong to the caller; discarded/failed sensitive connection staging is cleared.

`ILinuxHidNativeApi` is a smaller syscall-level testing seam beneath the production report
adapter. `LinuxHidReportApiTests` fake this layer to verify actual bytes/counts/request codes;
`LinuxReportConnectionTests` fake the report seam under the production connection adapters.
Both groups run without libc or hardware on any host.

## Descriptor sizing limits

The size ioctl is bounded to 1..4095 before requesting the descriptor (the v6.12 hidraw ioctl
rejects lengths above HID_MAX_DESCRIPTOR_SIZE - 1), whose native struct is 4 + 4096 bytes.
A pool rental is temporary metadata, not report/secret staging. The existing parser's
last Input/Output item and default 64-byte behavior are retained for known descriptors, including
the pinned parsing vectors. Multiplication is widened and oversized report items are rejected.
Numbered report descriptors are outside this adapter's scope and fail explicitly. This is not
a generic HID parser: global PUSH/POP and collection aggregation are not implemented. Existing
truncation/default behavior is retained rather than claiming generic malformed-HID detection.
Bounded unnumbered non64 input/output sizes pass through to direct IO metadata/read capacity;
the parser's report-byte bound is 4096. Direct SET still uses caller length rather than requiring
it to match output metadata. OTP feature GET/metadata retains its established fixed 8 bytes.
Unavailable descriptor ioctls retain the old 64/64 FIDO fallback; invalid reported sizes fail.
The descriptor ioctl returns status, not a byte count; parsing is bounded by the requested struct
size, not a fabricated transfer count.

## Source evidence (read 2026-11-11)

* [.NET runtime v10.0.0 SafeHandleMarshaller](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/Marshalling/SafeHandleMarshaller.cs):
  `ManagedToUnmanagedOut.FromUnmanaged(IntPtr)` and `ManagedToUnmanagedIn.ToUnmanaged()`
  establish the pointer-width unmanaged representation. SafeHandle references remain essential.
* Linux v6.12 [hidraw.c](https://github.com/torvalds/linux/blob/v6.12/drivers/hid/hidraw.c):
  `hidraw_read` returns actual input length; `hidraw_send_report` expects the ID at byte 0;
  `hidraw_get_report` passes the requested ID, copies up to the returned count, and returns it.
  [include/linux/hid.h](https://github.com/torvalds/linux/blob/v6.12/include/linux/hid.h)
  defines `HID_MAX_BUFFER_SIZE = 16384`, bounding framed output; feature request size must
  additionally fit the 14-bit generic ioctl size field.
* Linux v6.12 [usbhid/hid-core.c](https://github.com/torvalds/linux/blob/v6.12/drivers/hid/usbhid/hid-core.c):
  `usbhid_get_raw_report` sets `buf[0]`, advances the buffer for ID zero, and increments a
  positive return to **include synthetic zero**. `usbhid_set_raw_report` and
  `usbhid_output_report` similarly include that byte in successful counts. Thus generic hidraw
  prose about unnumbered reports must not be substituted for the USB implementation's GET contract.
* Yubico [ykman/hid/linux.py](https://github.com/Yubico/yubikey-manager/blob/main/ykman/hid/linux.py),
  main as fetched: `USB_GET_REPORT = 0xC0094807`, SET `0xC0094806`, receive allocates 1+8
  and strips `[1:]`; send prepends zero. This corroborates framing, not short-count acceptance.
* man7 [open(2)](https://man7.org/linux/man-pages/man2/open.2.html),
  [eventfd(2)](https://man7.org/linux/man-pages/man2/eventfd.2.html), and
  [close(2)](https://man7.org/linux/man-pages/man2/close.2.html): int returns, lowest available
  descriptor (including zero), errno failure contracts, close-on-exec flags, Linux early release
  and the danger of retrying close. EINTR treatment here is Linux-specific, not portable POSIX.

## Evidence boundaries

No-hardware tests exercise actual glibc failed open, invalid eventfd flags, file and eventfd
read/write, fcntl, ioctl failure, and poll. The fd-0 test reuses `PcscIsolatedProbe` to close only
the explicitly selected child's stdin, then proves valid zero descriptors, actual-number reuse,
and no double-close of a successor. Injected tests cover exact close errors, deferred references,
concurrent withheld release (using the accepted L0 `WithheldNativeProbe`), and preserved errno.
The synthetic worker in that last test belongs to the test; it is not an L1 production async claim.

The full Core inventory loses only two duplicate read/write imports: 204 sites / 132 native
imports. Only the libc family's exact entry identities change; classifications remain
`documented` + `outstanding`. L6 still owns platform/device/native-AOT/public acceptance.
