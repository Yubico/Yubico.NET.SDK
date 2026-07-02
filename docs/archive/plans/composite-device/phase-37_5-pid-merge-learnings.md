# Phase 37.5 Learnings: Composite Merge By USB Product ID (Rust Model)

Handoff record for Phase 37.5. This phase replaced the Phase 37 serial-only merge with the Rust
PID-from-reader-name model, fixing the two Phase 37 limitations (exclusive CCID holder; serial-less / SKY keys).

## Phase Summary

- Branch: `yubikit-composite-device-new`.
- Supersedes the Phase 37 merge mechanism; Phase 37's public criteria remain satisfied.
- Phase ISA: `docs/plans/composite-device/phase-37_5-pid-merge-ISA.md` (Cato-gated).

## Model (as built)

- CCID PID is parsed from the PC/SC reader-name capability string (`ReaderNamePidParser`, case-insensitive, OTP/FIDO/U2F/CCID, NEO variant); HID PID comes from the real descriptor `ProductId`. Both are merge keys only when they are a known Yubico PID (`IsKnownPid`); `ProductId == 0`/unknown ⇒ null PID (never merged).
- `CompositeDeviceMerger` (pure): per-PID count = max across transports. PID-count == 1 ⇒ merge by PID with **zero opens**; PID-count > 1 ⇒ disambiguate by serial (conservative no-collapse if serial-less); NFC / null-PID ⇒ standalone; `forceSerialMerge` (set when a USB CCID name fails to parse) ⇒ merge all USB by serial so the unparsed CCID rejoins its HID siblings.
- `FindYubiKeys` builds descriptors with PID from cheap enumeration, computes PID counts, opens + reads serial only for PID-count > 1 (or the force-serial scan), then merges. Discovery is serialized (`_scanLock`); identity reads cached (successes only).
- Device metadata (`DeviceInfo`/`FirmwareVersion`) is read best-effort via `CompositeMetadataReader`: a single bounded pass over CCID → OTP → FIDO with a hard per-read timeout and NO retries, run concurrently across keys, cached by a collision-free length-prefixed member-id key with eviction. It never affects the merge.
- Composite `DeviceId`: `ykphysical:pid:{pid:X4}` (PID-count == 1) or `ykphysical:{serial}` (serial-disambiguated). Shared `ProtocolDeviceInfo` builds the right protocol over an open connection and reads `DeviceInfo` (used by both the serial read and the metadata read).

## Key Decisions

- Merge by PID, not serial, in the common case — the cheapest reliable signal that needs no open. Serial only disambiguates multiple same-model keys.
- PID is a merge key only when known-Yubico; HID `ProductId` must be `> 0` and known (guards platforms that default ProductId to 0).
- Metadata read is eager but strictly bounded/non-correctness-affecting (chosen over a zero-read design because metadata availability is valuable and grouping is decoupled from the read).
- No process killing: unlike the Rust `kill_pcsc_blockers` (which `pkill`s scdaemon/yubikey-agent), an SDK library call must not kill a user's processes. Deferred as a possible CLI-only opt-in.

## Review Evidence

- Interim Cato (ISA, pre-edit, broad merge-evidence change): GPT-5.4 — round 1 **BLOCKED** (1 BLOCKER + 2 HIGH + 2 MEDIUM), round 2 **CONCERNS**, round 3 **PASS**. Drove: known-PID gate + `ProductId > 0` (no PID-0 over-merge), case-insensitive parse + unparsed-USB-CCID force-serial fallback, bounded no-retry metadata path, and honest recording of the PID-count==1 transient-two-same-model-keys residual (self-corrects next rescan). Outputs: `/tmp/opencode/cato-review-phase37_5-isa-output{,2,3}.md`.
- Interim /DevTeam Reviewer (implementation): GPT-5.4 — **CHANGES REQUIRED** (2 MEDIUM + 2 LOW), then **PASS WITH NOTES**. Fixes: metadata order CCID→OTP→FIDO; always-evict metadata even with no composites; force-serial warning; collision-free length-prefixed metadata cache key with stored member ids (no delimiter split); plus a doc-order nit. Affirmed correct: parser table, partition (no double-count/drop), open-independent merge, protocol disposal. Outputs: `/tmp/opencode/devteam-review-phase37_5-output{,2b}.md`.
- GPT-5.5 cross-vendor reviews (DevTeam + Cato) **QUEUED** for when quota returns.

## Verification Evidence

- Branch `## yubikit-composite-device-new`; Rust ref `## experiment/rust`.
- Full solution build: succeeded, 0 warnings, 0 errors.
- Core unit suite: 486 total, 0 failed (484 passed, 2 skipped) — new `ReaderNamePidParserTests`, rewritten `CompositeDeviceMergerTests` (PID merge / SKY / serial-less-multi-merges-by-PID / two-same-PID / NFC / null-PID / unknown-PID / force-serial-rejoin), and `FindYubiKeysPidMergeTests` (single key merges even when **every** ConnectAsync throws — the exclusive-CCID-holder fix at unit level; ProductId==0 not merged).
- **Hardware (serial 103, FW 5.8.0 beta, OTP+FIDO+CCID), the proof of the fix:**
  - With GnuPG `scdaemon` running/holding the CCID: `FindAllAsync(All)` returns ONE device with `AvailableConnections == SmartCard | HidFido | HidOtp` (CCID merged by PID despite the exclusive hold) — the exact Phase 37 failure case, now passing.
  - With the CCID free (`gpgconf --kill scdaemon`): full smoke 4/4 — merge, per-connection filters return the same physical device, and typed SmartCard/FIDO/OTP connects succeed.
  - Local allow-list edit (serial 103) reverted before commit.
- Changed-file format clean; `git diff --check` clean; docs-qa 54 validated; Core has no dependency on Management.

## What Did Not Work / Hazards

- **Do not run `gpg --card-status` to "force" scdaemon to hold the CCID.** On the 5.8 beta key it triggered a reset and knocked the key off the USB bus (gone from `lsusb`), requiring a physical reseat. scdaemon holds the CCID naturally at rest — no action is needed to reproduce the held state. The safe way to FREE the CCID for connect-tests is `gpgconf --kill scdaemon` (does not disturb the device).
- The metadata read still opens a connection at discovery (best-effort). It is bounded (hard timeout, no retries, concurrent) and never blocks the merge, but it is not zero-cost; the merge result is unaffected by its success.

## Residual Limitations (carried forward)

- **PID-count == 1 transient window:** if two same-model keys are hot-plugged near-simultaneously and a snapshot shows split interface visibility, they could momentarily mis-group; this self-corrects on the next rescan (PID-count becomes > 1 → serial path). Documented, not claimed away. Master ISC-19 is satisfied in steady state and by the serial path.
- **Opening a held CCID still fails:** the fix is to discovery/merge (CCID is included by PID). Actually opening `ConnectAsync<ISmartCardConnection>()` on a CCID held exclusively by another process still legitimately fails — that is correct behavior, not a regression. Applet sessions that prefer SmartCard (Management, YubiOtp) will fail to open a held CCID; Phase 38 owns transport preference/fallback policy.

## Deferred Candidates

- Phase 38: applet smart-default/override transport policy — should consider falling back off a held CCID to a HID transport where the applet supports it.
- CLI/tooling-only opt-in to kill `scdaemon`/`yubikey-agent` (Rust `kill_pcsc_blockers`), never inside a library call.
- USB parent/bus/port topology correlation as an alternative to reader-name PID (more platform-interop work; PC/SC cannot expose the parent on macOS/Win7).
- HID `ProductId` reliability on macOS/Windows is untested here (Linux-only hardware); the `ProductId > 0` + known-PID guard degrades safely (null PID → standalone) if a platform does not populate it.
- Queued: GPT-5.5 DevTeam + Cato reviews of Phase 37.5.

## Next Phase Inputs (Phase 38)

- Discovery now merges by PID; a single physical key is one `IYubiKey` even when its CCID is externally held. `IYubiKey.ResolvePreferredConnection` is the transport-selection mechanism. Phase 38 should formalize smart defaults/overrides AND consider transport fallback when the preferred transport (often SmartCard) is held by another process.

## Compact Summary

- Goal: merge a physical key's interfaces by USB PID (Rust model); fix exclusive-CCID-holder + serial-less/SKY.
- Branch: `yubikit-composite-device-new`.
- Status: implemented, verified (build 0/0, Core 486 pass; hardware proves CCID merges by PID even while scdaemon holds it, and full connect smoke passes with CCID free). Cato PASS, DevTeam PASS WITH NOTES; GPT-5.5 queued.
