# Wave2 lifecycle/time handoff — ready for independent review

## Isolation and scope

- Worktree/session: `/tmp/opencode/yubikit-oath-fix-lifecycle`.
- Branch: `yubikit-oath-fix-lifecycle`; unchanged HEAD `c90dced180de7975895771b32fcc91b9df752591`.
- No commits, pushes, delegation, audit-ledger edits, parent edits, Core changes, shared-harness changes, PUT/rename/input-policy changes, or Security Domain changes.
- All changes remain unstaged; three test files are new/untracked.

## Exact source changes

1. `src/Oath/src/Code.cs`: small pure `GetTotpWindow` helper validates nonnegative Unix seconds and a positive period, computes `from = timestamp - timestamp % period`, checks `from <= long.MaxValue - period` before addition, and returns the inclusive/exclusive window. Formatting uses it; HOTP still has `ValidFrom=timestamp`, `ValidTo=long.MaxValue`.
2. `src/Oath/src/OathSession.cs`: single-code calculations reject negative timestamps and invalid known windows before CALCULATE. Aggregate requests validate their default 30-second window before transmission. Existing custom-period followups pass through the same individual validation, inside wave1 raw-response cleanup. Epoch zero, >2038 values, and timestamps beyond the DateTimeOffset year-9999 range remain supported where Int64 windows fit.
3. `OathSession.ResetAsync`: RESET transmission remains outside the failure-invalidation catch, so pre-admission cancellation/overlap refusal cannot dispose an incumbent admitted operation. After successful RESET, clear readiness and use Core's `InitializeSessionAsync` completion boundary for refresh. Refresh failure/cancellation invokes Core's existing `DisposeAfterInitializationFailure`, which invalidates/detaches the session and preserves the primary exception even if disposal also fails. Effective firmware override is preserved. Borrowed connections remain open; owned connection disposal stays Core-owned.
4. `OathSession.ValidateAsync`: failed device-HMAC verification now throws `BadResponseException` rather than `WrongPassword`. Raw-response/TLV/HMAC cleanup remains intact. Device APDU rejections keep the existing compatibility category and raw/inner status; comments no longer assert all rejections prove a wrong password.
5. `src/Oath/src/OathException.cs`: neutral device-rejection message and accurate XML for existing `WrongPassword` enum member. No enum member, signature, or API-baseline changes.
6. Narrow contracts/docs: `src/Oath/src/IOathSession.cs`, `src/Oath/README.md`, `src/Oath/CLAUDE.md` describe bounds, partial-success reset invalidation, and the still-unimplemented automatic SCP re-establishment.

## Tests

- New `OathTimeTests.cs`: negative/min/max/default-window overflow; exact challenge bytes and windows at 0/29/30/59/60, RFC future timestamp 20,000,000,000, beyond year 9999, last supported default window; known custom-period overflow; last representable period-1 window; HOTP empty challenge/metadata and pre-counter-advance rejection; aggregate custom-period overflow clears the actual session-owned raw response and sends no individual followup.
- New `OathResetTests.cs`: successful identity refresh/effective-version preservation/exact RESET APDU; success then cancellation/malformed SELECT; terminal invalidation/no further wire use/salt clearing; borrowed connection remains open and can bind a replacement session; cancellation before admission and overlap refusal leave the healthy session/admitted LIST intact; owned teardown failure preserves both malformed-refresh and cancellation primary outcomes.
- `OathResponseTests.cs`: exact mismatch exception type plus existing raw-buffer/key-ownership assertions; neutral rejection message while preserving raw and inner `6984`/`6A80` status. Test command copies and key buffers are cleared in finally.
- New `OathLifecycleTimeTests.cs`: shared allowlist/hardware harness and OATH helpers; real epoch/boundary/RFC vectors, individual/aggregate equality, reset identity refresh and subsequent LIST. Separate SCP03 reset/subsequent-protected-LIST test attempts default establishment first; only INITIALIZE UPDATE (`CLA=80`, `INS=50`) `6A88` skips as an explicit credential/reference availability limitation, before RESET. OATH cleanup uses a separate 15-second budget and preserves the primary error.

## TDD and verification evidence

Archived outside toolchain pruning at `/tmp/opencode/oath-lifecycle-wave2-logs/`:

- Initial meaningful RED: `/tmp/opencode/oath-lifecycle-red-initial/`, run `20261103-000001-528-test`. Timestamp acceptance/wrapped metadata and usable stale reset state fail. One additional failure was a fixture expecting a four-byte RESET rather than Core's five-byte APDU including Le; corrected before GREEN.
- Mismatch RED: `20261103-000113-834-test` (expected BadResponseException, actual OathException).
- Neutral-message RED: `20261103-000131-914-test` (old message asserts incorrect password).
- First targeted GREEN: `20261103-000039-811-test`, 24 tests, 0 warnings.
- Final OATH unit GREEN: `20261103-000657-737-test`, **344 executed, 0 failed/skipped, 0 warnings**.
- Scoped build GREEN: `20261103-000602-886-build`, all three OATH shipping/unit/integration projects, **0 warnings**.
- Complexity: `20261103-000602-897-complexity`, **0 new/worse methods needing action**; existing ValidateAsync debt unchanged. This source-only complexity run overlapped the build and printed the toolchain active-run advisory; both completed successfully.
- Final focused integration GREEN: `20261103-000806-223-test`, **5 executed, 0 failed, 4 skipped, 0 warnings**. Five executions are three real-hardware tests plus two existing fake cleanup tests. Skips are three absent pre-5.6.3 rows and one unavailable default SCP reference, not firmware/SCP recovery evidence.
- `git diff --check`: clean.
- Scoped `dotnet format --verify-no-changes`: exit 0, zero changed files. Diagnostics at `/tmp/opencode/oath-lifecycle-format.log`; workspace loader reports duplicate existing PublicAPI additional files (not compiler/analyzer build warnings).

## Hardware evidence and limits

- Read-only inventory: `ykman list --serials` returned only `125`; `ykman list` identified `YubiKey 5 NFC - Enhanced PIN (5.8.0.alpha.2) [OTP+FIDO+CCID] Serial: 125`.
- Shared harness found/authorized exactly one device, serial125 (normalized firmware5.8.0).
- New time/reset test passed on serial125: epoch/step boundaries, RFC6238 SHA1 future vector, single/aggregate equality, new identity after reset, and subsequent LIST.
- Existing wave1 exact SHA1/SHA256/SHA512 full-HMAC/lifecycle test and SELECT/wrong-key/retry test passed again on serial125. No new claim about arbitrary status meanings or HOTP failure-time counter consumption.
- Default SCP03 establishment failed **before RESET**: `SCP03 INITIALIZE UPDATE failed: Referenced data not found (SW=0x6A88)`. Original unskipped failure is preserved at `/tmp/opencode/oath-lifecycle-scp-default-unavailable/` and run `20261103-000337-059-test`. Successful SCP reset/re-establishment/protected operation was **not exercised**.
- No serial9681620 run: it was unplugged. No absent firmware-group skip counts as evidence. No discovery-guard bypass, touch, UV, power-cycle, or Security Domain key/reset operation.

## Source/design decisions

- Python sibling verified clean at `4ca60f706af930459138d8dc0f0f953480e1c7a4`: `yubikit/oath.py:243–262` challenge/window/HOTP semantics, `316–324` RESET/reselect/SCP re-establishment, `333–349` failed device verification as BadResponseError. Python unbounded integer windows and truthiness-based timestamp defaults were not copied into C#; approved nonnegative/Int64 policy and explicit nullable defaulting apply.
- RFC6238 sections4.1/4.2 (floor, epoch/default step, >32-bit support), AppendixB (future vector): https://www.rfc-editor.org/rfc/rfc6238.txt . For nonnegative timestamps integer division matches floor.
- YKOATH SELECT, RESET, CALCULATE, VALIDATE, CALCULATE ALL sections: https://developers.yubico.com/OATH/YKOATH_Protocol.html . RESET is `00 04 DE AD`; challenges are TLV74 containing big-endian eight-byte steps, individual truncated P2=1. The published VALIDATE status table does not prove a universal wrong-password classification.
- Core `ApplicationSession.InitializeSessionAsync` and `DisposeAfterInitializationFailure` reused, not duplicated. SecurityDomain reset demonstrates readiness clearing and full protocol recreation, but its lifetime-retained parameters are not a safe precedent for silently retaining OATH creation-only keys.
- Context7 `/dotnet/docs` checked numeric overflow behavior; explicit pre-addition bound avoids silent unchecked wrap and avoids exception-driven control flow.

## Outstanding decisions — do not close these as fully solved

1. **SCP reset recovery is still incomplete and blocked by an API/lifetime decision plus unavailable default reference.** `SessionCreationOptions.ScpKeyParameters` documents an initialization-only borrow. SCP03 owns disposable StaticKeys; SCP11 can reference a disposable/opaque EC private key and certificates. Retaining those objects after creation would silently require callers not to dispose them; exporting/cloning opaque keys is not an acceptable workaround. Oath does not retain them here. The old SCP wrapper can therefore remain locally authenticated after successful SELECT without a new handshake; docs require disposal/reopening after SCP reset. Failures during refresh now fail closed, but this is not full SCP recovery certification.
2. **Concrete preferred API proposal for review:** add an OATH reset overload taking fresh borrowed `ScpKeyParameters` for the duration of reset/reinitialization, rather than retaining creation secrets. Existing no-parameter reset should refuse SCP reset *before destructive transmission* when no re-establishment material is supplied. Snapshot only nonsensitive protocol configuration needed to recreate the protocol. After successful RESET, dispose the old SCP wrapper/derived state, clear readiness/authentication, use existing Prepare/Configure/InitializeSession paths to SELECT/configure/handshake and rebind the backend; failure remains terminal with the original error. Caller owns/keeps reset parameters alive only until that operation returns. Approval and API-baseline/test work required; **not implemented**.
3. **DEC001 broader policy remains open:** decide whether failed device-HMAC verification must terminally invalidate the session and whether to replace/rename the compatibility rejection enum. Current bounded change distinguishes provenance without inventing post-failure session policy or removing raw status. The previous locked-state behavior remains.
4. DEC002 raw-key length, DEC003 URI policy, DEC006 detailed HOTP consumption/marker semantics, and DEC007 earliest hardware rationale are unchanged. Existing period/name policy and firmware gates are preserved.

Stopped for independent review; no commits made.
