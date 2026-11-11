# Original-source register

Original bytes, access timestamps, URLs and SHA-256 identities are in `manifest.json`. Retrieval used
HTTP/static file reads only. The first pass and one focused follow-up obtained the consequential Linux
close/libc and Ubuntu loader-package passages. No source was compiled or loaded. Full cached files are
originals; only the ranges below received semantic source review. The date of the retrievals is
November 11, 2026 UTC; final record assembly continued on November 12.

| Source, exact status/revision | Passage and actor | Strength and applicability |
| --- | --- | --- |
| Linux kernel release tag v6.12, `fs/open.c:1516–1576`, `fs/file.c:618–658,777–793` | close removes the fd-table entry before flush and synchronous fput; missing fd returns EBADF; restart errors translate to EINTR after removal | Original implementation, not POSIX normative text. Supports release of that Linux descriptor number despite EINTR/EIO from an actual returned close. SafeHandle must first drain borrowers. Device command success, data persistence and other OS close semantics are separate. |
| glibc release tag glibc-2.40, Linux `close.c:18–31` | exported close maps to `SYSCALL_CANCEL(close, fd)` | Original Linux libc implementation; no retry wrapper in this function. Exact deployed 2.44 build source was not reacquired; the host's recorded version/bytes and reviewed upstream versions stay distinct. |
| Linux man-pages 6.19, `close(2)`, CAVEATS/ERRORS/RETURN VALUE, page date 2026-02-08 | Linux early release, EINTR/EIO diagnostics, reused-fd retry hazard; EBADF invalid | API/manual guidance, Linux-specific. The page explicitly contrasts unspecified POSIX.1-2008 EINTR state and other implementations. B2 makes no HP-UX, Windows or portable POSIX release assertion. |
| Linux v6.12 `fs/eventfd.c:82–115,302–310` | private eventfd release wakes poll, drops local context and frees ID/storage; fops has no driver flush | Original implementation supports classifying this notification resource separately from hidraw close. Locks, memory reclamation and scheduling still preclude a hard elapsed-time bound. |
| Linux man-pages 6.19 `eventfd(2)`, DESCRIPTION | eight-byte host-endian counter, non-semaphore read drains, nonblocking EAGAIN for empty/full counter, CLOEXEC | Kernel API contract; supports bounded syscall-attempt logic, not bounded wall time. `poll(2)` describes fd:int/events:short/revents:short, indefinite timeout and readiness/error bits. Poller uses poll, not epoll. |
| Linux man-pages 6.19 `open(2)`, DESCRIPTION/O_CLOEXEC/O_NONBLOCK | lowest unused nonnegative fd, close-on-exec acquisition; readiness flag bounds vary by file/device | API contract supports fd zero and acquisition flags. Nonblocking is not universal native-call latency or USB output completion proof. |
| Linux v6.12 `drivers/hid/hidraw.c:46–107,110–258,397–443` and `include/linux/hid.h` HID_MAX_BUFFER_SIZE | actual read prefix; zero-ID framed write/feature request; count bounds; descriptor ioctl status/size | Original kernel implementation. Descriptor main-item parsing choices remain SDK policy, not generic HID conformance. Zero caller payload can reach native rejection because hidraw requires at least two framed bytes; SDK's accepted bounded length is not device support. |
| Linux v6.12 `drivers/hid/usbhid/hid-core.c:870–968` | USB GET preserves zero report-ID at offset0 and includes it in positive count; SET/output strip zero on wire and include it in successful count | Original USB driver implementation supports GET8/native9 and FIDO64/native65. Generic hidraw prose alone would miss this driver-specific return detail. Other bus drivers remain outside the selected USB context. |
| systemd release tag v257 `libudev-device.c:388–415`; `libudev-monitor.c:136–162,178–192` | parent is borrowed from child; monitor unref drops reference/closes owned socket at zero and returns NULL; monitor get_fd is int | Original library implementation/API comments. Current caller scopes keep child/monitor alive during copied field access/poll. No physical-key identity or checked hidraw claim release follows from a udev pointer. |
| systemd 262 `udev(7)`, matching and OWNER/GROUP/MODE/TAG sections (current HTML saved) | administrator rules assign device-node permissions and tags | Versioned page content from moving latest URL, hash retained. Configuration semantics, not an SDK recommendation to grant all users access. Session ACL and service group deployment require actual distribution policy verification. |
| Canonical Ubuntu noble amd64 file lists, libudev1 and libudev-dev, fetched 2026-11-11 | runtime package contains `.so.1`; development package contains `.so` | Original distribution package contents, moving package page retained by hash. Establishes named package layout; no minimal-host runtime execution or all-Ubuntu claim. |
| .NET runtime release tag v10.0.0 `SafeHandle.cs:137–185,195–269`; `SafeHandleMarshaller.cs:15–49,147–182` | atomic addref versus close, last borrower invokes ReleaseHandle, disposed managed state precedes callback, default native representation is IntPtr | Original implementation supports explicit libc int wrapper and checked outcome publication after actual callback. SafeHandle itself supplies no successful native release proof. |
| .NET v10.0.0 `Task.cs:2792–2849`; `Thread.cs:238–275` | WaitAsync returns a wait task; Thread.Start captures ambient ExecutionContext unless suppressed | Original implementation/API XML. Linux owner starts suppress context. Reactor starts from owner worker on production path; direct internal custom registration retains its caller's responsibility. No claim every SDK background thread suppresses context. |
| .NET docs `native-library-loading.md`, moving main snapshot retained | Linux shared-library name variants and optional custom resolver | Official guidance; does not add a numeric soname automatically. Supports C004's conditional deployment gap; exact native loader binary behavior needs its later assigned platform lane. |
| YubiKey Manager pin `4ca60f706af930459138d8dc0f0f953480e1c7a4`, `ykman/hid/linux.py` complete 130 lines | OTP GET/SET request codes, prepend/strip zero, Python file ownership and descriptor enumeration | Implementation comparator only. Python does not inspect exact GET count here and logs payload at TRAFFIC; matching requests are neither cleanup nor secrecy/conformance proof for C#. Same shared C pin reused; no duplicate repository clone or Python import. |
| Same Python pin `doc/Device_Permissions.adoc`, complete | separates PC/SC/keyboard/FIDO access; points to rules | Vendor application guidance. Linked personalization master rule snapshot is moving, retained by hash and access time; it assigns ID_SECURITY_TOKEN and leaves actual distribution access policy to its consumer. |

## Context7 orientation

Resolved `.NET` to `/dotnet/docs`; queried SafeHandle addref/release and LibraryImport representation,
Task.WaitAsync wait-only cancellation, Thread/ExecutionContext flow, and Linux native-library name
variations. A focused string-conversion query also returned the official charset table: Unix Ansi
marshalling uses UTF-8; native char pointers still require the owning library's encoding contract.
Results linked official native-interop/disposal/cancellation/context guidance. Consequential
claims above were then checked against saved original runtime source. The legacy Framework MDA and
impersonation paragraphs returned by Context7 were not promoted into .NET 10 platform guarantees.
Resolving libudev returned only third-party go-udev bindings; original systemd implementation was used
instead of substituting those bindings as authority.

## Bounded gaps

- The exact deployed Linux 7.2.15/glibc 2.44 source-to-binary relationship remains the historical capture's
  version/byte evidence, separate from reviewed upstream v6.12/2.40 mechanics. No physical close with
  EINTR/EIO was induced. Fake-return tests establish SDK classification only.
- The original POSIX standard was not needed to assert a portable rule: this report deliberately scopes
  release semantics to Linux. An independent reviewer should reacquire the applicable deployed native
  sources before broadening that support envelope.
- No fresh distribution installation, libudev load, ACL evaluation, physical serial selection, USB
  report exchange, compiler or test execution occurred. Missing lanes remain in the proposal matrix.
- Python OATH floor/history questions stay with C/B6. The retained origin `1b87964ba258e3ce39fd2959888dfe7cf81a6843`
  establishes source-policy introduction, while its hardware rationale remains unresolved.
