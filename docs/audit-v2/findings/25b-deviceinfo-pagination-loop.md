# 25b Device-info pagination never terminates (YESDK-1633)

| | |
| --- | --- |
| Audit severity | MED (item 25b of the MED row 25) |
| Our verdict | Confirmed |
| Our severity | LOW-MED |
| Root cause | SDK and Python share it (the page loop has no bound). The wrap-around at 256 is SDK-specific. |
| Fix group | A (no-brainer) |
| Evidence | unit test (fake device), static (code, Python read) |
| Since the audit | Loop unchanged: `DeviceInfoReader.cs` is identical at `df1ec06d`. The caller in `ProtocolDeviceInfo.cs` moved from line 463 to line 559 (that file changed for other reasons). `ManagementSession.cs` is unchanged. |

Abbreviations used in this file: API is the application programming interface. APDU is the application protocol data unit. FIDO and OTP are the HID (human interface device) transports of the FIDO and YubiOTP applets. P1 is the first parameter byte of an APDU. SDK is the software development kit. TLV is tag-length-value. ykman is the Yubico command-line tool, written in Python.

## What the audit says

The audit says that the device-info page counter wraps, so the reader can continue indefinitely.

> "Device-info pagination wraps and permits indefinite continuation"

## What is right

- The page index is a `byte` ([DeviceInfoReader.cs:61](../../../src/Core/src/Devices/DeviceInfoReader.cs#L61)). `++page` ([DeviceInfoReader.cs:101](../../../src/Core/src/Devices/DeviceInfoReader.cs#L101)) wraps from 255 to 0.
- The device controls how long the loop runs. The "more data" count from each page sets `remainingPages` ([DeviceInfoReader.cs:88](../../../src/Core/src/Devices/DeviceInfoReader.cs#L88)). Nothing caps the number of pages.
- The page index goes into the single P1 byte of the APDU (`P1 = page`, [DeviceInfoReader.cs:148](../../../src/Core/src/Devices/DeviceInfoReader.cs#L148)). So only 256 distinct page numbers exist.
- The repro shows the wrap. The fake device answers "more data" on every page. The reader asks for pages 0 to 255, then page 0 again. Repro: [CoreNumericBoundaryAuditReproTests.YESDK1633_DeviceInfoAlwaysMoreData_StopsWithBadResponseWithinPageSpace](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs#L36-L54). Result today: failed as expected. The message reads "got InvalidOperationException after 600 page requests (first repeated page index: request #256 asked for page 0 again)". The `InvalidOperationException` comes from the fake device's own guard, which stops the test after 600 requests. It shows that the SDK reader did not stop by itself.

## What is wrong or imprecise

- Nothing in the reader itself stops the loop except cancellation. The repro needs its own 600-request guard and a 10-second token, so it cannot hang.
- The audit does not mention Python. Python has the same missing bound on two of its transports, so this is not only an SDK defect. See the Python section.
- The SDK reads only the last byte of the "more data" value (`Span[^1]`, [DeviceInfoReader.cs:88](../../../src/Core/src/Devices/DeviceInfoReader.cs#L88)). Python reads the whole value as one big-endian integer (`management.py` line 659). The two agree for one-byte values. They differ for longer values. This is not part of the bug, but the fix should keep the same count rule as Python.

## Why it matters

- **Who can trigger it:** a faulty or malicious device, or a transport that answers with a bad device-info page.
- **Precondition:** the device keeps saying "more pages follow". We have no evidence that real devices do this. The hardware record does not cover device-info paging, so we did not measure the page count on the test keys.
- **Effect on direct reads.** `ManagementSession.GetDeviceInfoAsync` ([ManagementSession.cs:140](../../../src/Management/src/ManagementSession.cs#L140)) and the version probe ([ManagementSession.cs:200](../../../src/Management/src/ManagementSession.cs#L200)) call `DeviceInfoReader` directly, on the caller's token. Direct management reads can continue until the caller cancels.
- **Effect on discovery.** Discovery bounds its wait. A three-second total budget applies to one best-effort metadata read ([FindYubiKeys.cs:82-85](../../../src/Core/src/Devices/FindYubiKeys.cs#L82-L85)), and the bounded reader enforces it ([CompositeMetadataReader.cs:76](../../../src/Core/src/Devices/CompositeMetadataReader.cs#L76), [ReadBoundedAsync](../../../src/Core/src/Devices/ProtocolDeviceInfo.cs#L158-L179)). When the budget runs out, discovery stops waiting. The timeout does not cancel the underlying read, which is the pagination task. The read runs with `CancellationToken.None` ([ProtocolDeviceInfo.cs:408](../../../src/Core/src/Devices/ProtocolDeviceInfo.cs#L408) at `fdfcd6fd`; [line 430](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/src/Core/src/Devices/ProtocolDeviceInfo.cs#L430) at `df1ec06d`) and holds a discovery worker slot ([ProtocolDeviceInfo.cs:402](../../../src/Core/src/Devices/ProtocolDeviceInfo.cs#L402)) until the pagination loop ends. When no slot is free, metadata reads are skipped with `WorkerAdmissionSaturated` ([ProtocolDeviceInfo.cs:328-331](../../../src/Core/src/Devices/ProtocolDeviceInfo.cs#L328-L331)). A faulty device that never ends pagination can therefore hold a slot until the device or the transport gives up. We did not test how long that takes.

## Specification

No public specification covers management device-info paging. The contract is the protocol's one-byte P1 field (see above). The Core guidance on loops is in [src/Core/CLAUDE.md, lines 72-74](../../../src/Core/CLAUDE.md#L72-L74). It says: "Background listeners and native/resource-manager retry loops must block, back off, exit, or throttle on every failure path." That rule is written for listeners and native retry loops, not for this read loop. The same principle applies.

## Canonical Python reference

**Python has the same issue on the OTP and FIDO transports** (candidate for the divergence ledger). On the SmartCard transport, Python stops with an exception at page 256, because the page number no longer fits in the P1 byte.

The loop in [yubikit/management.py, lines 647-663](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/management.py#L647-L663) at `4ca60f7`:

```python
page = 0
while more_data:
    more_data -= 1
    encoded = self.backend.read_config(page)
    ...
    page += 1
```

- The page counter is a Python integer. It never wraps. Nothing bounds it.
- SmartCard: `read_config` passes `page` as P1 ([management.py#L557-L558](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/management.py#L557-L558)). The APDU formatter packs P1 with `struct.pack(">BBBB", ...)` ([yubikit/core/smartcard/__init__.py#L158](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/core/smartcard/__init__.py#L158)). Page 256 raises `struct.error: ubyte format requires 0 <= number <= 255`. We ran this. The loop ends with an exception.
- OTP and FIDO: `read_config` sends `int2bytes(page)` ([management.py#L498-L500](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/management.py#L498-L500), [management.py#L591-L592](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/management.py#L591-L592)). Page 256 becomes a two-byte payload. Nothing stops the loop, so it continues for as long as the device answers "more data".
- Deliberate? The "more data" semantics changed on 2026-05-04 (commit `13f8a21c`, "Interpret TAG_MORE_DATA as number of following pages"). The message describes a future firmware meaning. It says nothing about a bound. We found no evidence that the missing bound is deliberate.

## Sibling SDKs (context only)

Not checked for this item.

## Reproduction

- Unit: [CoreNumericBoundaryAuditReproTests.YESDK1633_DeviceInfoAlwaysMoreData_StopsWithBadResponseWithinPageSpace](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs#L36-L54). Asserts `BadResponseException`, at most 256 requests, and no repeated page. Result today: failed as expected (see above).
- Hardware: not run. The fake device reproduces the loop. The hardware record has no device-info paging test ([hardware results](../evidence/hardware-results.md)).

## Proposed fix

- **Recommendation.** In `DeviceInfoReader.ReadAsync` ([DeviceInfoReader.cs:54-105](../../../src/Core/src/Devices/DeviceInfoReader.cs#L54-L105)), before the page counter wraps, throw `BadResponseException` if more pages are announced after page 255. A check of this form works:

  ```csharp
  if (remainingPages > 0 && page == byte.MaxValue)
      throw new BadResponseException("Device info exceeds 256 pages");
  ```

  Put it before `++page` ([DeviceInfoReader.cs:101](../../../src/Core/src/Devices/DeviceInfoReader.cs#L101)).
- **Options considered.** (a) 256 pages, the protocol maximum (recommended). (b) A smaller cap such as 16. It fails sooner, but it assumes what future firmware will do.
- **API impact.** None.
- **Proving test.** The existing repro passes after the change.
- **Depends on / interacts with.** Item 25e: the TLV parser throws `ArgumentException` for malformed data, and `DeviceInfoReader` may need to map that to `BadResponseException`. Item 21 (YESDK-1629) also runs through the same TLV decoder.
- **Open questions for the maintainer.** Use 256 or a smaller cap? (We recommend 256.)

## Check it yourself

- `dotnet toolchain.cs -- test --project Core.UnitTests --filter "FullyQualifiedName~YESDK1633_DeviceInfo"`, from the worktree root.
- Read `src/Core/src/Devices/DeviceInfoReader.cs` lines 54-105.
- Read `yubikit/management.py` lines 647-663 at `4ca60f7`.
- Read `yubikit/core/smartcard/__init__.py` line 158 at `4ca60f7`, and try `struct.pack(">BBBB", 0, 0x1D, 256, 0)`.
