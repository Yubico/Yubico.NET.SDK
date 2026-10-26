# v2 audit: verification snapshot and remaining dispositions

Snapshot: 2026-10-22. This is a review handoff, not a release sign-off or a claim that the pull requests have merged. The final close-out change is documentation-only; it does not alter protocol code, tests, thresholds or workflow permissions.

## Delivered for review

| PR | Content | Base at this snapshot |
| --- | --- | --- |
| [#698](https://github.com/Yubico/Yubico.NET.SDK/pull/698) | Main audit fixes, including tokenless exclude-list pre-flight (`94e95e57`) | `yubikit` |
| [#707](https://github.com/Yubico/Yubico.NET.SDK/pull/707) | OATH applet-identity checks in all four `Credential`-taking operations | `audit/v2-fixes` |
| [#708](https://github.com/Yubico/Yubico.NET.SDK/pull/708) | OpenPGP `PutKeyAsync(KeyRef, IPrivateKey)`; derive attributes/templates internally | `audit/v2-fixes` |
| [#709](https://github.com/Yubico/Yubico.NET.SDK/pull/709) | Unsigned OpenPGP S2K count; hash at least one complete salt and PIN, following RFC 9580/GnuPG | `audit/v2-fixes` |
| [#710](https://github.com/Yubico/Yubico.NET.SDK/pull/710) | CI lockfiles, pinned tools and reduced write-token exposure | `yubikit` |
| [#711](https://github.com/Yubico/Yubico.NET.SDK/pull/711) | Refuse missing R-MAC on empty SCP success/warning responses; retain initialization/error exceptions | `audit/v2-fixes` |

The four children are independent siblings, not a dependency chain. They have not yet incorporated the latest base fixes. Review, retargeting, ancestry synchronization and fresh checks remain separate landing steps. Missing build CI on the audit base is not passing CI. The CTAP rename (#692) and session-initialization refactoring (#712) are outside this landing sequence.

## Verification evidence

The code snapshot at `94e95e57` built warning-free. Its whole-solution unit run executed **3443 tests in 13 projects, with 0 failures and 9 known skips**. The skip total includes repros parked on the main PR whose fixes are in the separate child PRs; do not carry those deferrals forward as completed tests after integration.

Hardware measurements used the dedicated YubiKey 5 NFC Enhanced PIN, firmware **5.8.0.beta.0**, serial **103**, on Linux. These results do not establish cross-platform hardware behavior or compatibility with every firmware.

| Check | Evidence and qualification |
| --- | --- |
| Fido2 suite | 82 executed, 0 failed, 3 skipped in a complete attended run |
| WebAuthn suite | All 18 cases have passing results across coordinated split runs, not one uninterrupted green suite |
| Tokenless exclude-list regression | Five new unit cases failed before the fix; existing 17-credential hardware stress case then passed without resetting the key |
| PIV touch-required signature | Signature and exactly one request/completion callback pair verified; session reused successfully; temporary Retired20 key deleted |
| OTP HMAC touch | Expected response and exactly one device-wait/completion callback pair verified; temporary slot 2 configuration deleted |
| CLI static password | CLI-programmed 38-character ModHex fixture produced exactly those characters plus Enter in a saved editor capture; slot 2 deleted and empty state checked afterward |
| Core device events | Initial discovery, standalone removal and three operator-driven remove/reinsert cycles passed; repeated test proves event liveness, not identity correlation |
| PIV removal and recovery | Mid-session removal, disposal and non-stranded lease assertions passed; separate fresh PIV serial read succeeded after reinsertion |

All close-out targeted toolchain runs reported **0 warnings**. Removal watchdogs did not fire. Temporary PIV/OTP material was cleaned up and verified. The initial static-password attempt produced no saved output; a coordinated retry succeeded without reprogramming or SDK changes. No root cause is claimed for that initial attempt.

The earlier WebAuthn run stalled during PC/SC activity. Isolated checks passed after reinsertion, but the stall's cause remains **unconfirmed**; it is not claimed fixed by the exclude-list change. SmartCard cancellation remains cooperative, not a guarantee that native I/O is interruptible.

Reproduce automated checks through the repository toolchain, for example:

```sh
dotnet toolchain.cs -- build
dotnet toolchain.cs -- test
dotnet toolchain.cs -- complexity --complexity-args "--base $(git merge-base HEAD origin/yubikit)"
```

Hardware checks require explicit filters and coordination; the relevant tests are [PIV presence](../../src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/UserPresenceProbeTests.cs), [OTP presence](../../src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/UserPresenceProbeTests.cs), [Core repeated hot-plug](../../src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Devices/DeviceEventHotPlugTests.cs) and [PIV removal](../../src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivHotplugContentionTests.cs). Do not overwrite occupied slots or reset the key to reproduce these close-out checks. The keyboard-output check must target a scratch editor, not a shell: the configuration appends Enter.

## Approved complexity dispositions

The whole-PR comparison against `d7404385` measured 230 methods across 90 files: **15 new/worse flags** and 2 existing-debt improvements. The report's successful exit is informational; it does not erase the flags. The maintainer approved the following method-specific justifications, also recorded as `Complexity-Justification:` commit trailers. No thresholds or classifier behavior are changed.

| Method | Approved reason to retain the explicit flow |
| --- | --- |
| `DeviceInfoReader.ReadAsync` | Page-byte exhaustion guard prevents the paging loop from wrapping. |
| `Tlv.ParseData` | Explicit reserved-length, truncation and range checks keep BER parsing bounded while accepting valid leading-zero length octets. |
| `MakeCredentialResponse.DecodeInternal` | Explicit absent-versus-empty conditional preserves a missing `largeBlobKey` as null. |
| `GetAssertionResponse.Decode` | Same absent-versus-empty correction; no new parsing state machine. |
| `CredentialData.ParseUri` | TOTP-only positive-period check prevents an invalid divisor without changing HOTP behavior. |
| `OathSession.CalculateAllAsync` | Malformed device IDs are skipped through the shared parser so one bad entry does not hide valid credentials. |
| `FidoSessionRequestEncoding.BuildMakeCredentialRequest` | Explicit illegal-`up:false` guard precedes encoding; duplicate-parameter validation remains delegated. |
| `WebAuthnClient.ValidateRegistrationOptions` | Flat boundary checks reject duplicate algorithms, invalid deadlines and inconsistent cross-origin metadata. |
| `ScpState.Scp11InitAsync` | Count and ordered-tag checks precede indexing and use of handshake key/receipt TLVs. |
| `ExtensionBuilder.Encode` | Explicit mutually exclusive extension shapes and ceremony-specific variants keep wire encoding inspectable. |
| `CredentialFilter.SelectAsync` | Shared pre-flight retains bounded batching, size reduction, no-credential continuation, tokenless/authenticated probes and authentication-parameter cleanup. |
| `RegistrationExtensionSession.Create` | Short, flat processor-selection list; independent input/support guards dominate the score rather than nested orchestration. |
| `LargeBlobEntry.TryDecode` | One map parser keeps typed required keys, opaque unknown pairs, invalid-entry skipping and reader positioning together. |
| `PrivateKeyConsistency.ThrowIfInconsistent` (RSA) | Explicit required-component and RSA/CRT relation checks; compound missing-component guards contribute to the score. |
| `WebAuthnClient.MakeCredentialCoreAsync` | Cyclomatic complexity fell from 16 to 11 while cognitive rose from 9 to 10; scoped attempt functions retain ownership, cached probing and UV escalation. |

## Residuals and ownership

Owners below identify the responsible role; individual assignees still require maintainer confirmation. These are not promises that issues have been filed or work completed.

| Item | Disposition | Responsible role / next action |
| --- | --- | --- |
| #4 / YESDK-1612: unsigned getAssertion outputs | Deferred: measured firmware does not emit the relevant output; shared sibling gap | SDK maintainer: retain measurement and revisit when supported firmware provides a producer |
| #17 / YESDK-1625: runtime EC scalar copy | XML residual remark delivered; dotnet/runtime issue draft prepared, not filed | Maintainer: choose upstream filer and approve submission |
| #19 / YESDK-1627 and N4 | LOW disposition for #19; implemented in #710, not merged | CI maintainers: review #710 and dependency updates |
| Docs patch symlink/mode validation | Follow-up outside #710 MVP | CI maintainers: assign and track; do not describe path checks as mode validation |
| Dependency submission | NativeShims GitHub Packages restore returns HTTP 401/NU1301 on #698/#711 and unrelated #712; maintainer accepts this as separately tracked/nonblocking | CI maintainers: diagnose package-feed authentication separately; no credentials changed here |
| 26o: OATH retry seed | Closed as not a defect; passing counterexample retained | Maintainer: reconcile Jira disposition |
| PRF UV override | Deliberate sibling parity, not full WebAuthn L3 conformance; measured PRF outputs differ with UV | Maintainer/sibling teams: hand off evidence; applications needing stable output must request required UV |
| PC/SC stall and finalizer flake | Unconfirmed stall cause; previously observed finalizer watchdog flake remains separate | Core maintainers: track and isolate recurrence; passing hot-plug checks do not close this investigation |
| SCP11c allowlist counter, TLV error mapping and other non-audit observations | Separate backlog, not silently fixed | Module maintainers: triage and assign |
| Original audit #15, #18, #21 and “items to consider” | Outside verified scope; #20 is covered with 25e | Audit/release maintainer: accept the scope boundary or schedule separate verification |
| Jira and sibling-team handoff | Reconciliation/reporting pending; nothing reported upstream by this close-out | Maintainer: confirm assignees, link PRs, record deferrals and send the divergence ledger |

Tokenless exclude-list filtering converges with Python, Android and Swift. OpenPGP KDF semantics and the missing-R-MAC check retain the deliberately isolated divergences documented in #709 and #711. The [annotated N2 probe](https://gist.github.com/DennisDyallo/252e563a9b0418a05acbe3df0ac0627c) demonstrates adversarial response tampering; it is not evidence that normal YubiKey firmware emits an unauthenticated success.

## Remaining landing gates

Complete final candidate build/unit/whole-PR checks and substantive review before merging. Preserve the base branch through child retargeting despite automatic branch deletion. Recommended order is #698, #710, then #707/#708/#709/#711, independently synchronized and checked against then-current `yubikit`. Verify the integrated trunk afterward. This snapshot authorizes neither automatic merging nor branch/worktree cleanup.
