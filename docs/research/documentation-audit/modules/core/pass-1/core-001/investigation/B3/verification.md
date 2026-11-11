# B3 verification and coordinator plan

## Executed record-only checks

`python -B docs/research/documentation-audit/modules/core/pass-1/core-001/investigation/B3/record_dispositions.py`
serialized the completed semantic-read decisions and independently checked:

- 41 complete primary files, 7,636 lines, 1,139 unique primary IDs and the five actual shared IDs;
- 1,144 dispositions: 1,087 read-clean, 57 read-candidate, zero unread and zero primary exclusions;
- original raw-file/range SHA-256 equality, pinned Git blob identity and explicit LF-normalized source equality;
- five original native source files matching the published release-tree Git blobs;
- 37 static IOReturn enum mismatches against the identified original Apple header;
- cached stable 1.18.1 package and both Mac dylib asset hashes, without loading either binary.

`verify_records.py` checks the final serialized records, source/capture hashes, mapped path existence,
record links and absence of tracked product changes. Its `verification-receipt.json` is the actual
fresh outcome. Hashes validate identities; the file-evidence notes and report document the semantic
reading, rather than relabeling hashed files as read. B3 ran no SDK/compiler/runner/native/device code.
Independent review, consumer-example compilation and Mac qualification remain pending.

## Bounded future checks, all unexecuted

Coordinator assigns each run separately, serializes it in the chosen checkout, uses current build/test
skills and records actual nonzero execution counts, warnings, failures and exact selectors. New scratch,
TMPDIR/TMP/TEMP and logs belong under
`/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/B3/`, with original failed outputs retained.
Reuse the current validation foundation for the complete example; avoid a new generic host.

| Check | Smallest useful oracle / stop condition | Limit |
| --- | --- | --- |
| C001 constants | Pure managed/static constant table independently transcribed from original header, including both reserved gaps and Invalid; no native load or DTO initialization needed | Confirms definitions; permission failure cause needs its own host evidence |
| C002 enumeration | Future narrow injectable manager/CFSet seam: null Create, null CopyDevices, empty valid set and ordinary set; count/values must receive only valid sets and each acquired reference releases once on all exits | Source-supported unsafe argument path; do not intentionally crash this host to test CFSet(0) |
| C003 property contract | Missing property, wrong CF type, conversion false and valid SInt32 using an authorized narrow seam | Assert exception/value plus key cleanup; changing conversion policy needs separate approval |
| C004/C009 ABI | Inspect actual LibraryImport-generated stubs and corresponding original C typedefs on supported architecture; verify CFNumberType carrier versus SInt32 pointed storage and callback calling convention | Linux compilation alone is not Apple ABI/runtime qualification |
| C005 startup | Inject pre-registration failure, partial registration and Thread.Start failure separately; count manager/root ownership and distinguish known-unregistered cleanup from ambiguous retention; throwing start logger control | Tests require a deliberately scoped seam. Keep post-registration root until real quiescence proof exists |
| C006 context | Bounded nonsecret AsyncLocal sentinel across FIDO/OTP/listener Start, with caller value removed afterward and explicit owner teardown | Consolidate policy with B1-C010/B2; no real secret content in capture |
| Current owner regressions | Exact Mac lifetime classes from the packet: FIDO read cancellation/route, OTP route/recovery, IO/feature facade/compatibility, listener lifetime and string bytes | Fake-native managed ordering; run existing subprocess-isolated quarantine tests in their real test project with child logs retained |
| C007 input shape | Existing metadata65/72/SET72/GET64 oracle plus short feature GET0/3/8 and oversized9 cases | Describes current expert semantics, not generic raw descriptor behavior |
| Source/package provenance | Producer binds stable 1.18.1 package SHA-256, native asset hashes, source commit, native build configuration and deployed export set | Static release source availability does not prove a specific binary was compiled from it |
| Example shape | Compile the complete typed method in investigation-B3.md with actual Core output/reference/analyzer identities | Compile-only, no device/session method execution |

Several older held-native fake tests release barriers only after intermediate assertions. Before a
stress/fault lane, explicitly review their failure-path gate release and actual drain. The useful
oracle is native borrow exit/close/claim release, rather than a watchdog completing. Isolate deliberate
permanent retained-claim probes, and release every recoverable held gate in finally before disposing
its events. No tests/assertions were edited here.

## Mac machine protocol, pending separate assignment

No Mac device/machine work is available to this packet. If the coordinator later assigns one, bind
exact source commit/diff, Core binary, stable package/asset hashes, loaded native path, OS/architecture,
runtime, authorization state, serial/firmware and chosen mode. Do not mix the historical `.8` comparison
default with the current stable profile. Record cold before/after, current profiling, synthetic native,
typed/direct info, listener drain and attended touch/removal as separate lanes.

For a selected read-only Mac lifetime lane, record admitted open, call invocation return, actual report
completion, managed read cancellation/terminal wake, native cancellation request, native ack/delivery
drain, checked destroy/close, original claim release and reopen separately. Hold output only in an
authorized instrumented seam; a token or outer timeout is insufficient. Real callback drain should
observe callback exit before manager close/unschedule/reference release. Preserve returned nonzero
per-device manager close diagnostics instead of relabeling them successful device close.

Fresh-host TCC/Input Monitoring needs human permission coordination. User-deferred PIV physical work,
touch/hotplug and cancellation ceremonies remain untouched. Existing retained final Mac results and
the original failed/inconclusive probes remain historical; no repeat experiment is requested merely
to replace that provenance. Windows access/elevation/concurrency and user-ready deltas stay with B4.

## Non-boilerplate clean nominees

Coordinator applies the campaign's stratified 10%/minimum-three rule after excluding later edited
overlaps. These are concrete nominees, not a completed sample or an alternative denominator:

| Exact original ID | Scope and reason |
| --- | --- |
| `core-001:unit:9e53ed11e8002160` | CoreFoundationString.Create overload: exact null-terminated UTF-8 bytes, copying native constructor, retained returned object and null failure |
| `core-001:unit:251588c89cce54e6` | Native HidInputDestroy import: int result and owner pointer; sample together with original owner.h result/release contract |
| `core-001:unit:c1c79d3db2b973cf` | Actual public read wrapper finally disposes cancellation registration; sample together with ReceiveAsync markers and Shutdown drain, not in isolation |
| `core-001:unit:db15c64a3234cabc` | Listener CloseManager: nonzero return is diagnostic while throw is unproven; recheck exact Apple manager implementation and enclosing cleanup |
| `core-001:unit:ec04abe9fc7a7a51` | Expert feature GET transfers same original eight-byte array and pads short successful input; no physical report-size inference |
| `core-001:unit:60dff27859a26101` | OTP actual fake-wire cancellation/recovery/CRC correlation and exactly 22 sends; inspect full helper/gate scope |
| `core-001:unit:fbc26130ef118583` | CurrentProfile sample completeness/censor accounting preserves the 2+10+1 plan versus partial results; source oracle, not executed profiling |

Associated unchanged paths and original-citation dependencies remain mandatory review dispositions even
when they are omitted from the final edit set. No primary source unit was excluded or added to the
packet; secondary parent IDs in secondary-handoffs.json add zero primary units.
