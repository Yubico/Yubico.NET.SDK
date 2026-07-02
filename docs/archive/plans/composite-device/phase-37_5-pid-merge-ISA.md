# Phase 37.5 ISA: Composite Merge By USB Product ID (Rust Model)

This phase replaces the Phase 37 serial-only composite merge with the Rust reference's PID-from-reader-name model, so that in the common single-key case discovery groups the CCID, FIDO HID, and OTP HID interfaces of one physical YubiKey into one `IYubiKey` **without opening any interface**. It removes the two known Phase 37 limitations: a process holding the CCID exclusively (e.g. GnuPG `scdaemon`) no longer drops the CCID from the merged device, and serial-less keys (SKY series / serial-disabled) merge correctly.

Read this together with:

- `docs/plans/composite-device/ISA.md`
- `docs/plans/composite-device/phase-37-composite-discovery-learnings.md` (Known Limitations 1 and 2 — the motivation)
- `src/Core/CLAUDE.md`
- `../yubikey-manager` on branch `experiment/rust` — `crates/yubikit/src/platform/device.rs` (`pid_from_reader_name`, `pid_from_interfaces`, `merge_devices`, `open_single_usb`, `is_sky_pid`) and `crates/yubikit/src/platform/pcsc.rs` (`is_reader_usb`, `open`).

## Problem

Phase 37 merges per-interface devices by **application serial number**, which it reads by opening a connection to every USB interface during discovery. This has two confirmed limitations (Phase 37 learnings):

1. If another process holds the CCID exclusively (`SCARD_E_SHARING_VIOLATION`; observed with GnuPG `scdaemon`), the discovery serial read over CCID fails, so the CCID interface gets a null serial and is left out of the merge — the key shows up as a composite of the HID interfaces plus a separate CCID device.
2. Serial-less keys (SKY series, or serial-API-visibility disabled) cannot be grouped at all; a serial-less multi-interface key appears as several device rows.

Both stem from using serial as the only merge key and from opening every interface to read it.

## Vision

Discovery correlates the interfaces of one physical USB YubiKey by **USB Product ID**, exactly like the Rust reference: the CCID interface's PID is parsed from its PC/SC reader name (`"Yubico YubiKey OTP+FIDO+CCID 00 00"` → `0x0407`), and the HID interfaces' PID comes from the real HID descriptor. When exactly one physical key of a given PID is present, its interfaces merge with no connection opened. Serial is consulted only to disambiguate the rare case of multiple same-model keys present at once. NFC stays separate. Read-only device metadata is still populated, best-effort, over a single preferred transport with CCID→OTP→FIDO fallback, so it is available even when the CCID is locked.

## Out of Scope

- No change to the public `IYubiKey` shape, `YubiKeyManager.FindAllAsync` signatures, `CompositeYubiKey` connect-routing semantics, the repository event contract, or the Phase 37 merge-safety gating of Management/YubiOtp. This phase changes only the **merge evidence and discovery mechanism** plus the composite `DeviceId` scheme.
- No `pkill scdaemon` / `kill_pcsc_blockers` behavior. An SDK library call must not kill a user's `scdaemon`/`yubikey-agent`. (Recorded as a possible future opt-in for CLI/tooling contexts only; not in this phase.)
- No USB parent/bus/port topology correlation (an alternative to reader-name PID; more platform-interop work; deferred).
- No applet smart-default/override work (Phase 38) and no final-verification work (Phase 39).
- No mechanical Rust port: Rust is the behavior reference; the .NET shape keeps the `CompositeYubiKey`/member-device model.

## Principles

- Correlate interfaces by PID, the cheapest reliable signal available without opening the device. CCID PID from the reader name (case-insensitive parse); HID PID from the descriptor, but only when it is a plausible value (`ProductId > 0` and a known Yubico PID) — never group on a defaulted/zero PID.
- Grouping must never depend on opening an interface. A locked or unreadable interface of a single key must still be grouped.
- A PID is only used to merge when it maps to a known Yubico USB Product ID. An unknown/zero/unparseable PID is not a merge key.
- Only escalate to opening + serial reads when PID alone is ambiguous: more than one physical key of the same PID present (`pidCount > 1`), OR a USB CCID reader whose name does not parse to a known PID (naming drift must degrade to the Phase 37 serial behavior, never to silent fragmentation). Then disambiguate by serial; if serial is unavailable, do not collapse (conservative).
- NFC is never merged with USB. Unknown / unparseable NFC readers are standalone.
- Device metadata is best-effort and strictly decoupled from correctness: a single bounded pass over a preferred transport (CCID → OTP → FIDO) with a hard per-read timeout and NO retries; null on failure; reads run concurrently across keys so added latency is bounded by ~one timeout. It can add a bounded delay but cannot stall discovery and cannot change the merge result.
- The `pidCount == 1` zero-open merge assumes near-atomic USB enumeration of one physical key. A transient split-visibility of two same-model keys during simultaneous hot-plug can momentarily mis-group; this self-corrects on the next rescan (when both keys fully enumerate, `pidCount` becomes > 1 and the serial path takes over). This residual is documented, not claimed away.
- Preserve Phase 37 behavior where it was correct: pure testable merge function, serialized discovery, identity cache with eviction, repository physical-identity keying with remove+add on change.

## Constraints

- Execute on branch `yubikit-composite-device-new`.
- This phase relaxes the merge-evidence rule (PID instead of serial in the common case), which is a broad behavioral change: a **Cato review of this ISA is required before source edits**. While GPT-5.5 is rate-limited, run the interim opposite-family Cato review via `scripts/interim-cross-vendor-review.sh` (GPT-5.4, high reasoning) and queue the GPT-5.5/Cato review.
- Implementation uses the **/DevTeam** workflow (Engineer implements; Reviewer reviews; fix loop). The cross-vendor reviewer is interim GPT-5.4 with the GPT-5.5 DevTeam review queued. Record review output before commit.
- Use `dotnet toolchain.cs`; never raw `dotnet build`/`dotnet test`. Integration runs use `--project` and `--smoke`/category filters.
- Hardware smoke uses the connected composite key, serial 103 (OTP+FIDO+CCID). Add it to the Core integration allow-list locally only; revert before commit.
- Commit only intended files; never `git add .`/`-A`/`commit -a`.
- Do not introduce a `Core` reference to `Management`.

## Goal

Introduce a reader-name → PID parser (porting the Rust table), correlate interfaces by PID so a single physical key merges with zero opens, fall back to serial only for ambiguous PID-count>1 groups (conservative no-collapse when serial-less), keep NFC/unknown standalone, populate composite `DeviceInfo` best-effort over a preferred transport with CCID→OTP→FIDO fallback, give the composite a stable PID-or-serial `DeviceId`, cover it all with unit tests over fakes (including an "opens nothing for a single key" test and a "CCID read fails but still merges" test), verify the solution, prove the fix on hardware with `scdaemon` still holding the CCID, run interim Cato (ISA) and /DevTeam (impl) reviews with GPT-5.5 queued, write a learning note, and commit Phase 37.5 only.

## Criteria

### Governance

- [ ] ISC-1: Branch check shows `## yubikit-composite-device-new` before source edits, review delegation, build/test, or commit.
- [ ] ISC-2: `../yubikey-manager` reference branch is confirmed as `experiment/rust` before citing Rust behavior.
- [ ] ISC-3: This ISA records the PID-merge model, the serial-fallback rule, NFC/unknown handling, the DeviceInfo best-effort decision, and the DeviceId scheme before source edits.
- [ ] ISC-4: Interim Cato review of this ISA runs before source edits and returns pass or all concerns resolved; the GPT-5.5 Cato review is queued.

### PID Parser

- [ ] ISC-5: A reader-name → PID parser maps a USB YubiKey PC/SC reader name to its USB Product ID by **case-insensitive** capability substrings (`OTP`, `FIDO`/`U2F`, `CCID`), with the NEO variant (`NEO` in name) and standard tables matching the Rust reference: standard `0x0401`(OTP) `0x0402`(FIDO) `0x0403`(OTP+FIDO) `0x0404`(CCID) `0x0405`(OTP+CCID) `0x0406`(FIDO+CCID) `0x0407`(OTP+FIDO+CCID); NEO `0x0110`–`0x0116`. A non-USB-YubiKey reader name (no `"yubico yubikey"` signal, case-insensitive) or an uncomputable combination returns null.
- [ ] ISC-6: A `IsSky(pid)` predicate identifies the Security Key PID (`0x0120`). (SKY is FIDO-HID-only and has no CCID reader; its PID comes from the HID descriptor.)
- [ ] ISC-7: HID interface PID is taken from `HidDescriptorInfo.ProductId` only when it is a plausible merge key: `ProductId > 0` AND a known Yubico PID (ISC-7.1). A defaulted/zero or unknown HID `ProductId` (which can occur on platforms where enumeration does not populate it) yields a null PID, so unrelated HID interfaces never collapse into a shared PID `0`. Only the CCID PID is derived from the reader name.
- [ ] ISC-7.1: A single source-of-truth set of known Yubico USB PIDs (the values the parser can produce: `0x0110`–`0x0116`, `0x0120`, `0x0401`–`0x0407`) gates mergeability for both CCID-parsed and HID-descriptor PIDs. A PID outside this set is treated as null (not a merge key).

### Merge Model

- [ ] ISC-8: `DeviceInterfaceDescriptor` carries the interface PID (`ushort?`).
- [ ] ISC-9: `CompositeDeviceMerger` is a deterministic, side-effect-free function. For a known PID present on exactly one physical key (`pidCount == 1`), all descriptors sharing that PID merge into one `CompositeYubiKey` with no serial required; a single-interface PID passes through unwrapped. This relies on near-atomic single-key enumeration; the documented transient two-same-model-keys window (Principles) is a recorded residual that self-corrects on the next rescan, not a claim that no transient mis-group can ever occur.
- [ ] ISC-10: For a known PID present on more than one physical key (`pidCount > 1`), descriptors are disambiguated by serial: interfaces with the same non-null serial merge; null/unreadable serial does not collapse (conservative). This path opens interfaces (serial read).
- [ ] ISC-11: NFC interfaces and interfaces with null PID stand alone, EXCEPT the unparsed-USB-CCID fallback: if ANY USB CCID reader name fails to parse to a known PID (`Kind == Usb`, `pid == null`) — the symptom of reader-name format drift — the scan degrades to full Phase 37 serial-based merge for ALL Yubico USB interfaces in that scan (CCID and HID), with a logged warning. This is required so the unparsed CCID rejoins its HID siblings by serial rather than the CCID staying fragmented while the HID interfaces merge by PID. PID-merge resumes automatically on scans where all CCID names parse. NFC and other null-PID interfaces remain standalone.
- [ ] ISC-12: Merge correctness never depends on a successful connection/serial read for the `pidCount == 1` case — an interface (e.g. a CCID held exclusively by scdaemon) whose connection cannot be opened still merges by PID (this is the exclusive-CCID-holder fix).

### Discovery Orchestration

- [ ] ISC-13: `FindYubiKeys` builds descriptors with PID from cheap enumeration (no opens), computes per-PID counts (max across transports, mirroring Rust), and opens + reads serial only for interfaces whose PID count > 1 (or for all USB interfaces in a scan that triggers the ISC-11 unparsed-CCID fallback). The Phase 37 ">1 USB interface" identity-read gate is removed in favor of PID-count.
- [ ] ISC-14: For each merged physical key, read-only `DeviceInfo` is read best-effort via a SEPARATE metadata path: a single bounded pass over a preferred transport (CCID → OTP → FIDO), with a hard per-read timeout and NO retries (distinct from the retry-heavy `DiscoveryIdentityReader` used for serial disambiguation). It is **bounded and non-correctness-affecting**: the merge never depends on it (null on failure/timeout), and metadata reads across keys run concurrently so total added discovery latency is bounded by roughly one timeout, not the sum. (It is eager during discovery, so it can add a bounded delay; it cannot stall discovery and cannot change the merge result.) It is cached keyed by the interface pre-key/path with eviction-on-absence, NOT by the composite `DeviceId`; repeated monitor rescans do not reopen already-known interfaces. Discovery remains serialized (`_scanLock`).
- [ ] ISC-15: The `CompositeYubiKey` `DeviceId` is stable only within the current ambiguity class: `ykphysical:{serial}` when a serial is known (`pidCount > 1` disambiguated case), else `ykphysical:pid:{pid:X4}` for a `pidCount == 1` merge. It intentionally flips from the pid form to the serial form when a second same-model key appears; that surfaces as a repository remove(pid-id)+add(serial-id×2) sequence (ISC-19). Single-interface passthrough devices keep their per-interface `DeviceId`.

### Tests And Verification

- [ ] ISC-16: Unit tests cover the PID parser: every standard capability combination → expected PID, NEO variants, `U2F` alias, `CCID`-less names, non-YubiKey/unparseable → null, and `IsSky`.
- [ ] ISC-17: Unit tests over fake inventories cover: (a) full key (CCID+OTP+FIDO, same PID, one physical key) merges into one composite with unioned connections and no serial; (b) SKY single FIDO-only passes through as one device; (c) a serial-less multi-interface single key merges by PID; (d) two same-PID keys with distinct serials → two composites; (e) two same-PID serial-less keys → no-collapse; (f) NFC and null-PID interfaces stand alone.
- [ ] ISC-18: Unit tests prove: (a) merge correctness is open-independent — a single full key (CCID+OTP+FIDO, one PID) still merges into one composite with all connections even when EVERY `ConnectAsync` throws (only the best-effort metadata read opens anything, and its failure just leaves `DeviceInfo` null); this is the exclusive-CCID-holder fix at the unit level; (b) a HID interface with `ProductId == 0` (or an unknown PID) is treated as null-PID and is NOT merged on a shared zero PID; (c) at the merger level, the force-serial path rejoins an unparsed CCID with its HID siblings by serial (covered by the merger tests).
- [ ] ISC-19: Repository event semantics from Phase 37 still hold (one event per physical device; remove+add on capability change), including the `DeviceId`-scheme transition when a second same-model key appears: a focused test asserts the pid-id composite is removed and two serial-id composites are added (the self-correction of the documented transient). Different PIDs never collide on the pid-id form.
- [ ] ISC-20: Focused Core build and tests pass; full solution build passes; Core has no dependency on `Yubico.YubiKit.Management`.
- [ ] ISC-21: Active documentation validates (`docs-qa`); changed-file formatting verifies clean; `git diff --check` is clean.
- [ ] ISC-22: Hardware smoke against serial 103 proves the fix: with `scdaemon` **still holding** the CCID, `FindAllAsync(ConnectionType.All)` returns exactly one device whose `AvailableConnections` includes `SmartCard | HidFido | HidOtp` (CCID merged via PID despite the lock); and with the CCID free, the same one-device result plus typed SmartCard/FIDO/OTP connects succeed. No UP/UV/touch. The local allow-list edit is reverted before commit.
- [ ] ISC-23: /DevTeam review (interim GPT-5.4) returns pass or all findings fixed; GPT-5.5 review queued; review output recorded.
- [ ] ISC-24: Phase 37.5 learning note records the model, source changes, review status, verification (incl. the scdaemon-held hardware evidence), residual limitations, and deferred items.

### Anti-Criteria

- [ ] ISC-25: Anti: the MERGE of a single physical key depends on opening any interface (it must merge by PID alone; only the best-effort, non-correctness-affecting metadata read may open a connection).
- [ ] ISC-26: Anti: two same-model keys are collapsed without serial evidence, or any USB↔NFC merge occurs.
- [ ] ISC-27: Anti: the SDK kills `scdaemon`/`yubikey-agent` or any external process.
- [ ] ISC-28: Anti: a Core reference to Management is introduced, or the local allow-list edit (serial 103) is committed.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | branch | Active branch | `## yubikit-composite-device-new` | `git status --short --branch` |
| ISC-2 | branch | Sibling repo branch | `## experiment/rust` | `git -C ../yubikey-manager status --short --branch` |
| ISC-3 | design | Decisions recorded | present before edits | Read |
| ISC-4 | review | Interim Cato ISA review | pass/resolved | `scripts/interim-cross-vendor-review.sh` |
| ISC-5 to ISC-7.1 | unit | PID parser table (case-insensitive) + HID-PID guard (>0, known) + known-PID set | parser tests pass | `dotnet toolchain.cs -- test --project Core --filter ...` |
| ISC-8 to ISC-12 | unit | Pure merge: PID-1 merge, PID->1 serial, NFC/null standalone, locked-interface still merges | merger tests pass | unit tests |
| ISC-13 to ISC-15 | source/unit | Orchestration: PID counts, conditional serial read, best-effort DeviceInfo, DeviceId scheme | tests + read | unit tests |
| ISC-16 to ISC-19 | unit | Full fake-inventory + orchestration + repository coverage | focused tests pass | unit tests |
| ISC-20 | build | Core + full build, no Core->Mgmt | exit 0 / clean | `dotnet toolchain.cs build` + grep |
| ISC-21 | docs/format | docs-qa, changed-file format, whitespace | exit 0 / clean | `dotnet toolchain.cs -- docs-qa`; `dotnet format --verify-no-changes`; `git diff --check` |
| ISC-22 | integration | scdaemon-held + free hardware smoke (serial 103) | one merged device incl. SmartCard; allow-list reverted | `dotnet toolchain.cs -- test --project Core --integration --filter ...` |
| ISC-23 | review | /DevTeam review | pass/fixed | review output |
| ISC-24 | file | Learning note | present | Read |
| ISC-25 to ISC-28 | scope/dep | Anti-guards | no open-dependence, no weak merge, no process kill, no Core->Mgmt, allow-list reverted | tests / grep / git diff |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Phase 37.5 ISA + interim Cato | Write this ISA; record decisions; run interim Cato before edits. | ISC-1, ISC-2, ISC-3, ISC-4 | Phase 37 | false |
| PID parser | `ReaderNamePidParser` (reader-name → PID table, NEO, U2F alias, `IsSky`); HID uses descriptor PID. | ISC-5, ISC-6, ISC-7, ISC-16 | interim Cato pass | false |
| Merge model | Add `Pid` to descriptor; rewrite `CompositeDeviceMerger` (PID-count==1 merge, >1 serial fallback, NFC/null standalone, locked-interface still merges). | ISC-8, ISC-9, ISC-10, ISC-11, ISC-12, ISC-17 | PID parser | false |
| Orchestration | Rewrite `FindYubiKeys` (PID descriptors, per-PID counts, conditional serial read, best-effort DeviceInfo with fallback, cache/serialize); composite `DeviceId` scheme. | ISC-13, ISC-14, ISC-15, ISC-18, ISC-19 | merge model | false |
| Verify, smoke, review, learn, commit | Build, docs/format/dep, hardware smoke (scdaemon-held + free), /DevTeam review, learnings, commit. | ISC-20, ISC-21, ISC-22, ISC-23, ISC-24, ISC-25..28 | implementation complete | false |

## Decisions

- 2026-06-10: **Merge by USB PID, not serial, in the common case.** CCID PID from the PC/SC reader-name capability string (Rust `pid_from_reader_name`/`pid_from_interfaces`); HID PID from `HidDescriptorInfo.ProductId`. PID-count==1 ⇒ merge with zero opens. This is the core change and the fix for both Phase 37 limitations.
- 2026-06-10: **Serial is the disambiguator only for PID-count>1** (multiple same-model keys present). Then group by serial; serial-less ⇒ conservative no-collapse (Rust Strategy 2 + fallback). The Phase 37 `DiscoveryIdentityReader`, identity cache, serialization, and retry are retained for this branch.
- 2026-06-10: **DeviceInfo is read eagerly but best-effort over a single preferred transport** (CCID → OTP → FIDO fallback), cached, and never blocks the merge. Because grouping is PID-based, this read is pure metadata: it succeeds over OTP/FIDO even when the CCID is locked, and a total failure leaves `DeviceInfo` null without affecting the merge. (Chosen over a zero-read design because metadata availability is valuable and correctness is already decoupled from the read.)
- 2026-06-10: **No process killing.** Unlike the Rust `kill_pcsc_blockers`, the SDK will not `pkill scdaemon`/`yubikey-agent`; that is inappropriate inside a library call. Possible CLI-only opt-in is deferred.
- 2026-06-10: **Composite `DeviceId` scheme**: `ykphysical:{serial}` when serial known (PID-count>1 case), `ykphysical:pid:{pid:X4}` for a PID-count==1 merge. A scheme transition (a second same-model key appears) surfaces as repository remove+add.
- 2026-06-10: **Guards added after interim Cato review (round 1, BLOCKED):**
  - PID is a merge key only when it is a known Yubico USB PID (ISC-7.1); HID `ProductId` must be `> 0` and known (defends against platforms that default `ProductId` to 0 → false PID-0 merges).
  - Reader-name parsing is case-insensitive; a USB CCID reader that does not parse to a known PID falls back to the serial-read path (Phase 37 behavior) with a logged warning, so naming drift never causes silent fragmentation.
  - The `DeviceInfo` metadata read is a separate bounded single-pass-with-timeout path (no retries), strictly decoupled from the merge, so a locked CCID cannot stall discovery.
  - The `pidCount == 1` zero-open merge's transient-two-same-model-keys window is recorded as a residual limitation that self-corrects on the next rescan (then `pidCount > 1` → serial path); master ISC-19 is satisfied in steady state and by the serial path, not claimed for the transient.
- 2026-06-10: This phase **supersedes the Phase 37 serial-based merge mechanism**. Phase 37's public criteria (ISC-16..ISC-20, ISC-27 of the master ISA) remain satisfied; only the internal evidence/mechanism changes. Numbered Phase 37.5 to keep Phase 38/39 intact.

## Changelog

- conjectured (Phase 37): serial is the only cross-transport identity available in .NET, so merge by opening every interface and reading serial.
  refuted by: hardware showed an exclusive CCID holder (`scdaemon`) blocks the CCID serial read (CCID dropped from the merge), and serial-less keys (SKY) cannot be grouped at all; the Rust reference correlates by PID-from-reader-name without opening the device.
  learned: derive the CCID PID from the reader name and the HID PID from the descriptor, merge by PID for the single-key case, and use serial only to disambiguate multiple same-model keys.
  criterion now: ISC-5 to ISC-13 govern the PID model and the serial fallback.
- conjectured: PID-from-reader-name with `pidCount == 1` is sufficient and safe on its own, HID `ProductId` is always valid, and the eager DeviceInfo read is trivially non-blocking.
  refuted by: interim Cato (GPT-5.4, round 1, BLOCKED) — a transient split-visibility of two same-model keys can make `max`-count == 1 and wrongly merge them; HID `ProductId` defaults to 0 on some platforms (and Windows HID enumeration is absent), so zero/unknown PIDs could over-merge; a USB CCID name that fails to parse would silently fragment a normal key; and reusing the retry-heavy serial read for metadata could stall discovery.
  learned: gate merging on known Yubico PIDs only (HID `ProductId > 0` and in the set), parse case-insensitively with a serial-read fallback for unparsed USB CCID, use a separate bounded no-retry metadata read, and record the `pidCount == 1` transient as a self-correcting residual rather than over-claiming master ISC-19.
  criterion now: ISC-7, ISC-7.1, ISC-11, ISC-14, ISC-18, ISC-19 capture the guards.

## Verification

Populated in the Phase 37.5 learning note before commit.
