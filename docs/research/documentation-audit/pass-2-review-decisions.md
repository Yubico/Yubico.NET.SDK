# Pass 2 review: voice-batch decisions

Record of Dennis's review of `pass-2-applet-master.md`, card by card over voice and the vault. Review closed
2026-11-20 (AG-25). The per-card outcome of every card is in `pass-2-review-ledger.md`.

- Worktree: `~/Code/y/worktrees/yubikit-docs-audit-review`, branch `yubikit-docs-audit-review`, based on `origin/yubikit-docs-audit` at `52d34ca65`
- Master: `docs/research/documentation-audit/pass-2-applet-master.md`
- Sibling pins checked live on 2026-11-14:
  - Python yubikey-manager: as pinned in the master
  - python-fido2: `~/Code/y/python-fido2` at `5bc9d3a`
  - Android: `~/Code/y/yubikit-android` at `f4626856`
  - Rust: `~/Code/y/yubikey-manager-rust-auto` at `90940e9b`

## Agreements

| # | Date | Agreement | Status |
| --- | --- | --- | --- |
| AG-1 | 2026-11-14 | Triage policy in `pass-2-review-policy.md` (gates G1 to G5, auto-approve A1 to A6, tracks T1 and T2, guardrails E1 to E3) | Approved |
| AG-2 | 2026-11-14 | YubiKit v2 is alpha with no direct consumers; breaking changes are allowed. Gate G4 suspended until first public release | Approved |
| AG-3 | 2026-11-14 | Auto-approvals are not read out over voice. They go to `pass-2-review-classification.md` with rule, evidence pin and pseudocode; Dennis skims and vetoes | Approved |
| AG-4 | 2026-11-14 | Voice interviews cover only gated cards, highest severity first | Approved |
| AG-5 | 2026-11-14 | Delivery: one commit per card, one PR per applet, cross-cutting decisions in their own PR first. Details below | Approved |
| AG-6 | 2026-11-14 | Base branch for every fix PR is `yubikit-docs-audit`. Ignore `main`, `develop` and `yubikit` for this work; always pass `--base yubikit-docs-audit` | Approved |
| AG-7 | 2026-11-14 | After each applet round, request a GitHub Copilot review via `gh` and run a review-fix loop until major findings are resolved. Details below | Approved |
| AG-8 | 2026-11-14 | Classification is delegated with one dedicated agent per applet; no agent classifies more than one applet. Each writes its own file; Sia merges and cross-checks. Details below | Approved |
| AG-9 | 2026-11-15 | D0: a card that hits no gate and fits no A-rule goes to Dennis for interview, at the lowest priority within its severity. Never auto-approved on Sia's lean; Dennis wants to know the nature of every such card. 35 cards | Approved |
| AG-10 | 2026-11-15 | F1 fast-confirm: a High card whose fix is mechanical and on which the siblings agree is settled by a one-line yes from Dennis instead of a full interview. Per-card confirmation still required. Candidates below | Approved; all five cards approved 2026-11-15 |
| AG-11 | 2026-11-17 | X11: previewSign and ARKG are in scope behind `[Experimental]` (Sia's option C). Safety fixes CTAP-IMP-029 (a) and CTAP-IMP-040 (a) approved; the four autos stand. CTAP-IMP-038 and -041 are not parked: Dennis decided them in the D0 pass (AG-12). No new user documentation for these surfaces; the attribute plus the existing XML disclaimers carry the warning | Approved |
| AG-12 | 2026-11-17 | D0 calls: Dennis answered 34 of 35 cards in the vault copy. Table below | Approved, 34 of 35; PIVIMP-008 blank |
| AG-13 | 2026-11-17 | X10 test-state rule: every applet test starts from a known, retrievable state, resetting or restoring before the test if needed. Tests are not expected to clean up after themselves; self-cleanup is an explicit, justified exception. Test-only secret wiping is low value because test fixtures never hold real high-value secrets | Approved (general rule, from CTAP-IMP-020, -021, OTP-IMP-011, HSM-IMP-006, CTAP-IMP-042) |
| AG-14 | 2026-11-17 | X8 firmware rule (answers Q2). Prefer `IYubiKey.CachedDeviceInfo.FirmwareVersion`; when it is null, use the applet's own version read. If that read returns 0.0.0 or any alpha/beta version (`FirmwareVersion.IsAlphaOrBeta`, Major == 0), the key is assumed to be alpha/beta and **every firmware gate admits**. "If we cannot read a production firmware version, assume alpha or beta." Details below | Approved; one follow-up (Q6) on reads that fail outright |
| AG-15 | 2026-11-17 | Q1, Q3, Q4, Q5 answered: PIVIMP-008 A; MGMT-IMP-012 (b) keep internal and match Python's bytes, with a no-regression guard for later firmware; CTAP-IMP-042 remove the export; HSM-IMP-006 vectors from Python `hashlib.pbkdf2_hmac` | Approved. D0 now 35 of 35 decided |
| AG-16 | 2026-11-18 | Review-queue calls from the vault (`Inbox/YubiKit pass-2 review queue.md`, last edited 2026-11-18 09:10): 9 High cards answered, 8 decided and 1 deferred. Table below | Recorded 2026-11-18 20:05 |
| AG-17 | 2026-11-18 | H1 proposal: let High cards auto-approve when sibling precedent shows the correct behaviour (G1 no longer gates on its own). Draft in `pass-2-review-policy.md`; impact count below | Proposed by Dennis; scope question open (Q7) |
| AG-18 | 2026-11-18 | M1 and M2 proposal for Medium cards: M1 doctrine strictness (settles X7), M2 spec-mandated. Draft in `pass-2-review-policy.md`; nature of the 36 Medium cards and impact count below | Approved 2026-11-18 20:25 as AG-19 |
| AG-19 | 2026-11-18 | Q8 answered yes on all three: M1 and M2 in force (X7 decided as the repo doctrine), MGMT-XREF-002 reclassified A2, rule-covered facets split off the staying cards. Dennis also asked for the 10 Low cards to be checked against the same rules. Every new auto goes to the top of the skim for veto. Results below | Approved 2026-11-18 20:25; applied 20:40 |
| AG-20 | 2026-11-20 | Dennis's vault calls (queue edited 2026-11-20 10:53): 26 of 41 decided, WA-IMP-015 auto vetoed to B, HSM-IMP-008 deferral withdrawn (now a question), 16 open with answers in `Inbox/YubiKit pass-2 answers 2026-11-20.md`. X2 "continue after reset" rule proposed (conflicts with PGP-IMP-005 and MGMT-IMP-010 end-session) | Logged 2026-11-20 11:05; X2 and 16 cards awaiting Dennis |
| AG-21 | 2026-11-20 | Dennis's answers in `Inbox/YubiKit pass-2 answers 2026-11-20.md` (edited 12:32): 12 of 16 open cards decided; Q6 answered; X4, X5, X6, X9, X13 settled; new rules S1 (sample apps: readability first, no secret clearing, demo-only warnings), D1 (every sibling divergence labelled in the PR) and C1 (one operation at a time per session, simple Core guard). X2 reset left blank. Follow-ups in `Inbox/YubiKit pass-2 answers 2026-11-20 round 2.md` | Logged 2026-11-20 12:45; X2, 4 cards and Q7, Q9, Q10, Q11 awaiting Dennis |
| AG-22 | 2026-11-20 | Dennis's round 2 answers (edited 13:15): X2 "continue everywhere" (flips PGP-IMP-005 and MGMT-IMP-010; SD reset re-authenticates with the restored default SCP03 keys); CORE-HSM-X001 B, SD-IMP-005 continue with default keys, MGMT-IMP-001 a; Q7 closed unadopted; Q9, Q10 approved on lean; Q11 this pass, plus a question on reusing KeyReplacement elsewhere. WA-IMP-014 answered with a question; Sia's sibling claim corrected. X1, X3 (SDK half) and X12 framed in `Inbox/YubiKit pass-2 answers 2026-11-20 round 3.md` | Logged 2026-11-20 13:25; WA-IMP-014, Q12, X1, X3, X12 and the veto skim awaiting Dennis |
| AG-23 | 2026-11-20 | Dennis's round 3 answers (edited 13:54): Q12 (a) always continue on default SCP03 after SD reset; KeyReplacement SD only, overwrite documented elsewhere; X1 A (failed login never ends the session, clears that credential's success only, SCP handshake failure ends it); X12 B (`CredentialPassword` with `FromUtf8` padded and `FromRaw` exactly 16, seven `IHsmAuthSession` signatures). WA-IMP-014 answered with a question (siblings only send largeBlobKey when largeBlob is requested); X3 answered "combine A and B case by case, house style; show call sites". Examples in `Inbox/YubiKit pass-2 answers 2026-11-20 round 4.md` | Logged 2026-11-20 14:05; WA-IMP-014 letter, X3 shape and the veto skim awaiting Dennis |
| AG-24 | 2026-11-20 | Dennis's round 4 answers (edited 14:54): WA-IMP-014 A (refuse before sending, NotSupported); X3 single secrets P1 `Memory<byte>`, including OATH `CalculateAsync` and YubiOTP `CalculateHmacSha1Async` (plus OpenPGP `DecryptAsync`; PIV unchanged). Composite shape (P3 disposable result objects) not accepted yet; Dennis asked for .NET ecosystem idioms. Survey and new lean R1 in `Inbox/YubiKit pass-2 answers 2026-11-20 round 5.md` | Logged 2026-11-20 15:05; X3 composite shape (R1) and the veto skim awaiting Dennis; all gated cards decided |
| AG-25 | 2026-11-20 | Dennis's round 5 answer (edited 21:24) and chat 21:27: R1 approved (X3 composite results are plain records with writable caller-owned `Memory<byte>` fields; `IDisposable` only for secret holders `CredentialPassword` and `SessionKeys`; no `Clear()`). Veto skim: "I approve all of the skim-through auto-approved cards", so no vetoes on the 53 original autos, the 16 AG-19 autos or the 14 split facets. The earlier WA-IMP-015 veto (AG-20) stands. Review closed; per-card ledger `pass-2-review-ledger.md` | Approved; review complete |
| [AG-26](#ag-26-ctap-imp-027-ownership) | 2026-11-20 | Dennis explicitly approved applying X3/R1 to CTAP-IMP-027, superseding the earlier A5 disposable-response proposal. | Ownership approved; exact raw-envelope API open |
| [AG-27](#ag-27-r1-clear-scope) | 2026-11-20 | Dennis clarified R1: no new public `Clear()` convenience methods on applet result types. Existing public `ApduResponse.Clear()` remains supported; the SDK must still clear sensitive buffers it owns. | Approved |
| [AG-28](#ag-28-delivery-commit-scope) | 2026-11-20 | Dennis approved reconciliation #3: one commit per unsplit card or per approved, independently scoped facet of a split card. | Interpretation only; no implementation authorized |
| [AG-29](#ag-29-c1-caller-serialization) | 2026-11-20 | Dennis withdraws SDK-wide fail-fast operation admission (C1/X4). Callers serialize operations per session; the existing logical-exchange guard is unchanged. | Approved; documentation only |

### AG-8 classification run

- Started 2026-11-14 16:22. Nine parallel `general` subagents, read-only on source and sibling repos; each may write only its
  own file under `pass-2-review-classification/`.
- Per-applet outputs: `oath.md`, `webauthn.md`, `management.md`, `ctap2.md`, `security-domain.md`, `yubiotp.md`,
  `yubihsm-auth.md`, `openpgp.md`, `piv.md`.
- Sia's merge step (after all nine finish): build `pass-2-review-classification.md` with the combined interview queue
  sorted by severity, the merged T2 cross-cutting list (deduplicated across applets), the T1 hardware list, totals per
  rule, and a coverage check against the master's ~159 cards. Spot-check a sample of evidence pins.
- Session IDs, for follow-ups: OATH `ses_e2559627dffe6Dx5xA0BnG2zqe`, WebAuthn `ses_e25592d03ffewXorg6PX8zFlxz`,
  Management `ses_e2558fc35ffek5slZD4bmxXWXz`, CTAP2 `ses_e2558c744ffeFSGTc76czunxKS`, Security Domain
  `ses_e2558937affe1chLWSHU1MbG26`, YubiOTP `ses_e255861deffeOPPcnwuSOM3hfK`, YubiHSM Auth
  `ses_e25582eebffe010nO56E32cXPj`, OpenPGP `ses_e2557faccffet6QrzEBP1hlq0O`, PIV `ses_e2557c63dffeC51sWNOss7RzTn`.
- Completed 2026-11-14. Merged into `pass-2-review-classification.md`: 178 cards, 53 auto, 108 interview (25 High),
  17 T1, 13 cross-cutting themes (X1–X13). Batch-1 table above is superseded by the merged file.

### AG-10 F1 fast-confirm candidates

| # | Card | Fix | Clean? | Dennis |
| --- | --- | --- | --- | --- |
| 1 | PGP-IMP-001 | Use the user PIN identity in both verify modes | Yes: Python, Android, Rust and spec agree | Approved 2026-11-15 |
| 2 | SD-IMP-002 | Reject SCP03 DELETE with key version 0 before sending | Yes: all three siblings reject | Approved 2026-11-15 |
| 3 | WA-IMP-012 | Fail with not-supported before creating when required credProtect is unsupported | Yes: python-fido2, Android, Rust and spec agree | Approved 2026-11-15 |
| 4 | CTAP-IMP-009 | Validate decrypted PIN token length, zero rejected data, then throw | No: Android does not check | Approved 2026-11-15 (Android gap accepted; follow python-fido2 and Rust) |
| 5 | PIVIMP-005 | Sample tool signs the message, not the hash | Open choice: RSA padding, Sia leans PKCS#1 v1.5 only (Rust) vs also PSS (Python) | Approved 2026-11-15 with Sia's lean: PKCS#1 v1.5 only, no PSS (Rust precedent) |

Dennis, 2026-11-15 02:10: "Approve all five F1 cards." Card 5 was presented with the lean "PKCS#1 v1.5 only", so
that is the recorded choice; PSS stays out unless Dennis reopens it. These five leave the High interview queue and go
to the engineer with their applet PRs (cross-cutting PRs still land first, AG-5).

### D0 cards in the vault

All 35 D0 cards, with a plain-language summary, Sia's lean and the agent detail, were copied to the Obsidian vault on
2026-11-15 for Dennis to read on mobile: `Inbox/YubiKit pass-2 D0 cards.md`. The per-applet files stay the source of
truth; Dennis's calls come back through chat and are recorded here.

### AG-12 D0 calls (from `Inbox/YubiKit pass-2 D0 cards.md`, read 2026-11-17)

"Lean" means Dennis accepted Sia's lean as written on the card. Sia's notes record what was verified in source
before acknowledging.

| Card | Dennis's call | Recorded outcome | Sia's note |
| --- | --- | --- | --- |
| WA-WHOLE-CEREMONY | A | Lean: caller's job, documented, until X4 | |
| CTAP-IMP-002 | Use device info firmware | Use `IYubiKey.CachedDeviceInfo.FirmwareVersion` for the USB SmartCard gate instead of the optimistic 0.x sentinel | Fallback when null is open (Q2) |
| CTAP-IMP-003 | Enforce size limit | Lean (a) | |
| CTAP-IMP-004 | Lean, follow SDK patterns | Lean (a): single-use builder, wipe after send or failure, reuse throws (OATH pattern) | |
| CTAP-IMP-015 | Follow python-fido2 | Adopt python-fido2 names, including 0x0C "database full", with a code comment explaining the divergence from the spec's "unused" | Overrides Sia's lean (a) on 0x0C |
| CTAP-IMP-020 | Normalize by default | `normalizePin` defaults to true; tests opt out explicitly; document it | Overrides Sia's lean (a); see AG-13 |
| CTAP-IMP-021 | Start from known state | AG-13 rule: restore before the test, not teardown after; exceptions explicit | Overrides Sia's lean (a) |
| CTAP-IMP-026 | Lean | (a) | |
| CTAP-IMP-028 | Lean | (a) | |
| CTAP-IMP-032 | Lean | (a) | |
| CTAP-IMP-034 | Lean | (a), confirm strict reader behaviour first | |
| CTAP-IMP-037 | Follow Python and Rust; no write-more-than-read | Lean (a): 64-bit original size, symmetric 1 MiB cap | |
| CTAP-IMP-038 | Most forward-compatible; Rust | (a): keep Try contract, return null with a distinct diagnostic (Rust warns and omits, `sign.rs:421–437`) | Same as lean |
| CTAP-IMP-041 | Keep if internal; if public it must be correct | (a): strict DER plus documented P-256/SHA-256 raw-message limit | Verified: `PreviewSignDerivedKey` and `VerifySignature` are **public** (`PreviewSignDerivedKey.cs:48, 121`), so Dennis's "must be correct" branch applies |
| CTAP-IMP-042 | Low value; open to a same-or-better suggestion | Remove the env-var export (AG-15, Q4 yes) | Sia's suggestion: remove the export; the test already decrypts the blob in-process (`Ctap2LargeBlobAuditReproTests.cs:~90–106`), which is the guarantee |
| CTAP-IMP-046 | Lean | (a) | |
| CTAP-DEC-002 | Lean, follow siblings | (a) | |
| CORE-CTAP-X020 | Lean | (a) | |
| MGMT-IMP-004 | Lean | (a) | |
| MGMT-IMP-007 | Lean | (a) | |
| MGMT-IMP-012 | Follow Python | (b) keep the primitive internal and dormant; align P1 to 0x11 and the payload to Python's little-endian `<BBH` (AG-15, Q3) | Overrides the card's lean (a) retire. No-regression guard below |
| MGMT-XREF-001 | Lean | (a) | |
| OTP-IMP-003 | Lean | B | |
| OTP-IMP-006 | Python and Rust floors, admit alpha/beta, use device info firmware | Floors 2.2 for HOTP, static and update (HMAC 2.2 unchanged); alpha/beta admitted; firmware from `CachedDeviceInfo` | Overrides Sia's lean C. Consequence accepted under AG-2: legacy 2.1 HOTP and 2.0 static use is dropped. Fallback when null is Q2 |
| OTP-IMP-011 | Reset before each test | AG-13 rule | Overrides Sia's lean (A/B) |
| HSM-IMP-006 | Skip cleanup; fix circular crypto tests | B: independent PBKDF2 vectors computed outside .NET with Python `hashlib.pbkdf2_hmac`, command recorded next to each vector; no cleanup work (AG-15, Q5) | Overrides Sia's lean A |
| HSM-IMP-007 | Lean | A | |
| CORE-HSM-X003 | Lean | B, decide properly under X4 | |
| CORE-HSM-X004 | Lean, optionally add firmware | A, plus firmware from `CachedDeviceInfo` when present | |
| PGP-IMP-016 | Lean | A | |
| PGP-CORE-001 | Lean | A | |
| PGP-CORE-003 | Use device info firmware | Prefer `CachedDeviceInfo.FirmwareVersion`; GET VERSION remains the fallback | Overrides Sia's lean A partly; how the fallback behaves is Q2 |
| PIVIMP-004 | Lean | A | |
| PIVIMP-006d | Lean | A | |
| PIVIMP-008 | A (2026-11-17, chat) | A: fix the failure-path drain now; defer release expectations and fixture floors (AG-15, Q1) | |

**Device info fact (verified 2026-11-17):** there is no `IYubiKey.DeviceInfo`. The property is
`IYubiKey.CachedDeviceInfo` (`src/Core/src/Abstractions/IYubiKey.cs:153`). It is nullable, does no I/O, can be stale,
and defaults to null for custom `IYubiKey` implementations. Applet sessions are created from a connection
(`Ctap2Session.CreateAsync(IConnection)`, `OpenPgpSession.CreateAsync(ISmartCardConnection)` and so on), so only the
`IYubiKey` extension entry points can pass it in. A live read needs `GetDeviceInfoAsync` from the Management module,
which the applets don't reference. The four device-info calls above therefore need one shared fallback rule (X8, Q2).

### Outstanding questions for Dennis (2026-11-17)

Q1 to Q5 answered 2026-11-17 21:28 (AG-14, AG-15). Kept for the record:

- **Q1 PIVIMP-008:** answered A.
- **Q2 X8 fallback:** answered; see AG-14.
- **Q3 MGMT-IMP-012:** answered (b), keep internal and match Python's bytes.
- **Q4 CTAP-IMP-042:** answered yes, remove the export.
- **Q5 HSM-IMP-006:** answered, Python `hashlib.pbkdf2_hmac`.
- **Q6 (new, open) failed version reads:** AG-14 covers a read that *returns* 0.x. Does it also cover a read that
  *fails*? The case that matters is PGP-CORE-003: OpenPGP cards older than 1.0.2 answer GET VERSION with SW 6985,
  and Python and Rust map that to 1.0.0, the oldest profile, not the newest. Sia's lean: AG-14 applies only to reads
  that return 0.x; a failed read keeps each card's narrow rule (OpenPGP: 6985 maps to 1.0.0, other statuses
  propagate). Treating a pre-1.0.2 card as alpha would open every modern gate on the oldest hardware.

### AG-16 review-queue calls (from the vault, read 2026-11-18 20:05)

Dennis's *Your call* lines in `Inbox/YubiKit pass-2 review queue.md`. No Medium, Low or qualification card has a call
yet. The auto-approved skim has no vetoes written (its top *Veto line* is blank), so the AG-3 veto pass is still open.

| Card | Severity | Dennis's call | Recorded outcome |
| --- | --- | --- | --- |
| MGMT-IMP-010 | High | Approved | (b) Keep device-authoritative dispatch (5.6 and SmartCard); document scope (FIDO, PIV, shared PIN; F9 retained); end the session after confirmed success; report uncertain outcomes without claiming completion; revisit family gating when a verified source exists. Settles the Management part of X2 |
| CTAP-IMP-012 | High | Approved | (a) Standard bio subcommands 1 to 7, modality-prefixed authenticated transcript, timeout in parameter key 3; preview routing separate |
| CTAP-IMP-048 | High | Approved | (a) Retain the selected device object through reinsertion; without a serial, verify with a GetInfo fingerprint and document that it is not unique identity |
| HSM-IMP-002 | High | Approved | A. Move S1 to S4 into one failure-safe clearing scope (precedent `CalculateSessionKeys.cs:55–105`) |
| HSM-IMP-005 | High | B, approved | B. Example tool safe by default: hidden secret prompts, no key output by default, diagnostics behind a flag. `Cli.Commands` unchanged |
| HSM-IMP-008 | High | Defer | Deferred. No device capture authorized now; strict 48-byte parsing stays as it is today (no code change). Not truncation (option C) |
| PGP-IMP-002 | High | Approved A | A. Low-level F9 write, documented caller procedure, fresh-session readback; no new public helper |
| PGP-IMP-003 | High | Approved | A. Drop the KDF cache; reread F9 per PIN or Reset-Code operation; propagate F9 errors; clear encoded copies in finally |
| PGP-IMP-005 | High | Approved, plus a docs follow-up | A. Terminal session after reset; check TERMINATE and ACTIVATE; stop on unrelated blocking failure; report completion only after fresh-session readback. **Extra deliverable:** record possible future improvements for keeping the session alive after reset (option B, in-session reinitialization) in the internal docs-audit documentation |

Still open in the High queue (10): WA-IMP-011, WA-IMP-013, PGP-IMP-009, PGP-IMP-011, PGP-IMP-015, PIVIMP-001,
PIVIMP-002, PIVIMP-006a, PIVIMP-006b, PIVCORE-002. Cross-cutting effect: MGMT-IMP-010, CTAP-IMP-048 and PGP-IMP-005
decide three of X2's cards; PIVIMP-001 is the last X2 High card.

### AG-17 H1 proposal: High cards with correct sibling precedent

Dennis, 2026-11-18: allow auto-approve "even for high-severity cards, as long as they have sibling precedents which
showcase the correct behavior."

Count, checked against every High card's gates and evidence (25 High in the merged classification):

| Group | Cards | Count |
| --- | --- | --- |
| Already settled by F1 (AG-10), all would have qualified | PGP-IMP-001, SD-IMP-002, WA-IMP-012, CTAP-IMP-009, PIVIMP-005 | 5 |
| Decided today (AG-16), would have qualified | CTAP-IMP-012, CTAP-IMP-048, PGP-IMP-002, PGP-IMP-003 (sibling consensus); HSM-IMP-002 (internal precedent plus Rust zeroize) | 5 |
| **Still open, qualify only if split siblings count (stricter wins)** | PIVIMP-001 (Python and Android swallow only "unsupported"; Rust swallows all, outlier), PGP-IMP-011 (Python and Rust [4.2.0, 4.3.5) vs Android [4.2.6, 4.3.5); conservative range wins) | 2 |
| Still open, partial: only the separable low facets qualify | PGP-IMP-015 (status display: Python and Rust; certificate disposal). The attest PIN question has Python-only precedent and stays | 0 whole, 1 partial |
| Still open, stay gated | WA-IMP-011 (G2, G3), WA-IMP-013 (no usable sibling), PGP-IMP-009 (G3), PIVIMP-002 (G3, G5), PIVIMP-006a (product choice on printing), PIVIMP-006b (Sia's lean diverges from siblings, G5), PIVCORE-002 (Core-owned design) | 7 |
| Decided today, would still have been gated | MGMT-IMP-010 (G3), HSM-IMP-005 (product intent), HSM-IMP-008 (siblings truncate, forbidden), PGP-IMP-005 (G3 on the blocking loop; terminal-session design) | 4 |
| Decided under X11 (AG-11), outside H1 | CTAP-IMP-029 (no sibling ARKG precedent beyond python-fido2's draft) | 1 |

Total 25. Bottom line: 10 High cards fit the rule, and all 10 are already decided. On the 10 still open, a strict
reading (A2 or A3 consensus only) moves **0**; extending A4 stricter-wins to High moves **2**, plus PGP-IMP-015's
low facets. No Medium or Low card changes: G1 is the only gate H1 relaxes.

- **Q7 (open):** does H1 include split siblings (A4, stricter fail-closed behaviour wins) for High, or only full
  consensus (A2, A3)? Sia's lean: include A4, with every High auto listed at the top of the skim for veto.

### AG-18 Medium cards: nature and M1/M2 proposal (2026-11-18 20:17)

Dennis asked what the 36 Medium cards are and whether a safe auto-approve policy exists for them. No new calls in
the vault since AG-16 (review queue last edited 2026-11-18 09:10; skim has no vetoes). Read from
`Inbox/YubiKit pass-2 review queue.md`, Medium section.

**Why they are gated (primary gate):**

| Gate | Cards | Count |
| --- | --- | --- |
| G3 shared bug: every sibling has the defect, so precedent can't decide | WA-IMP-014, MGMT-IMP-003, CTAP-IMP-001, -008, -010, -019, -030, -043, SD-IMP-001, -004, -006, SD-DEP-005, OTP-IMP-005, HSM-IMP-001, PGP-IMP-018, PGP-IMP-019, PIVIMP-007a, -007b | 18 |
| G5 Sia names a better option than the siblings (mostly product/API shape) | WA-IMP-002, MGMT-IMP-015, MGMT-XREF-002, CTAP-IMP-047, SD-IMP-005, OTP-IMP-002, CORE-OTP-X004, OTP-IMP-007, HSM-IMP-003, CORE-HSM-X001, PGP-IMP-010, PGP-CORE-004, PIVIMP-009 | 13 |
| G2 spec conflict: siblings contradict the spec, lean follows the spec | WA-IMP-010, -015, -016, CTAP-IMP-006, -036 | 5 |

Themes: about a third are "reject malformed device data or out-of-range input instead of defaulting silently" (X7);
the rest are lifecycle/reset (X2), error mapping (X6), sample-tool secrets (X3, X9) and small API-shape choices.

**Proposed rules (draft in the policy file):**

- **M1 Doctrine strictness (decides X7).** Medium or Low card, gated only by G3 and/or a G5 whose better option *is*
  the written repo doctrine (`CLAUDE.md:156–158`: reject malformed and security-critical data, keep unknown but
  well-formed values in open carriers). The fix only adds a rejection before a side effect or before data is
  published, or preserves unknown values; well-formed traffic behaves the same. Excluded: hardware-unverified firmware
  boundaries, a value Dennis must set, ties to open X1–X6, X9, X12, X13.
- **M2 Spec-mandated.** Medium or Low card where the lean follows governing spec text quoted on the card and the
  fix is local. The quote is re-verified against the spec before the card enters the skim; if it can't be verified,
  the card stays gated. The spec outranks an open X tie.

**Impact on the 36 Medium cards:**

| Outcome | Cards | Count |
| --- | --- | --- |
| M1 auto | CTAP-IMP-001, -008, -019, -043, SD-IMP-004, HSM-IMP-003, PIVIMP-007a, MGMT-IMP-003, PGP-IMP-018 | 9 |
| M2 auto | WA-IMP-010, -015, -016, CTAP-IMP-006, -036 | 5 |
| Reclassify as A2 (lean matches all three siblings; looks misfiled under G5) | MGMT-XREF-002 | 1 |
| Stays with Dennis: product or API shape | WA-IMP-002, MGMT-IMP-015, CTAP-IMP-047, SD-IMP-005, OTP-IMP-002, CORE-OTP-X004, OTP-IMP-007, CORE-HSM-X001, PGP-IMP-010, PGP-CORE-004, PIVIMP-009 | 11 |
| Stays with Dennis: behaviour choice, open X tie, hardware or a number to set | WA-IMP-014, CTAP-IMP-010, -030, SD-IMP-001, -006, SD-DEP-005, OTP-IMP-005, HSM-IMP-001, PGP-IMP-019, PIVIMP-007b | 10 |

Total 36: 15 auto, 21 stay. Separable facets that already fit an A-rule and could be split off the staying cards
(shrinks the question, card stays): SD-IMP-006 (int replaceKvn, exact 10-byte KCV), OTP-IMP-005 (reject short
status, common to every option), PIVIMP-007b (reject unknown CertInfo, Net iD zlib), PGP-IMP-010 (challenge, UIF
5.2.1 and NEO PIN gates), MGMT-IMP-015 (owned cleanup, A5), OTP-IMP-007 (4.3.2–4.3.5 check, A2). M1 and M2 were
not run against the 10 Low cards yet.

- **Q8 (answered 2026-11-18 20:25, AG-19):** yes to M1 (settles X7), M2 and the splits. Dennis: "Yes, I approve of
  these classifications and policies ... For Q8, yes, follow your lean there."

### AG-19 Q8 applied: M1, M2, splits and the Low check (2026-11-18 20:40)

Dennis approved M1, M2, the MGMT-XREF-002 reclassification and the splits, and asked for the 10 Low cards to be run
through the same rules. Q7 (H1 scope for split siblings) was not part of this answer and stays open.

**M2 spec re-verification** (required before a card enters the skim). Checked 2026-11-18 against the live W3C
WebAuthn Level 3 TR (`w3.org/TR/webauthn-3/`) and CTAP 2.1 PS errata 2022-06-21 (`fidoalliance.org`):

| Card | Quote checked | Result |
| --- | --- | --- |
| WA-IMP-015 | §10.1.5: "If allowCredentials does not contain exactly one element: Return a DOMException whose name is NotSupportedError" | Verified. M2 auto |
| WA-IMP-016 | §10.1.5 note: "If the read is not successful, largeBlob will be present ... but the blob member will not be present" | Verified. M2 auto |
| WA-IMP-010 | §5.1.4.2 (issuing a credential request): "If allowCredentialDescriptorList has exactly one value, set savedCredentialIds[authenticator] to allowCredentialDescriptorList[0].id"; §5.1.4.1 `credentialIdResult` uses the saved ID | Verified, **lean corrected**: the spec saves the single entry of the list *filtered for that authenticator*, not of the caller's original list. M2 auto with the spec's rule; flagged at the top of the skim |
| CTAP-IMP-036 | §6.10.2: hash mismatch means "the configuration is corrupt and the platform MUST discard it and act as if the initial serialized large-blob array was received" | Verified. M2 for act-as-initial; fragment typing and the iteration cap are M1 |
| WA-IMP-005 (Low) | §5.8.1.1 CCDToString: UTF-8 for U+0020, U+0021, U+0023–U+005B, U+005D–U+10FFFF; `\"`, `\\`; otherwise `\u` plus four lowercase hex digits | Verified. M2 auto |
| CTAP-IMP-006 | §6.5.1: platforms MUST enforce minimum 4 Unicode characters, UTF-8 at most 63 bytes, NFC, no trailing 0x00, "on PINs used with this specification" | Verified for the **new** PIN. The lean's "current PIN: encodability only" departs from the text, so that facet stays with Dennis. Card split |
| CTAP-IMP-007 (Low) | §6.5.6 getPublicKey: kty 2, alg -25, crv 1, 32-byte x and y | Verified for the required labels. "Tolerate extra labels" is a doctrine relaxation, not spec text; that facet stays. Card split |

**Whole cards now auto (16):**

| Card | Severity | Rule |
| --- | --- | --- |
| CTAP-IMP-001, CTAP-IMP-008, CTAP-IMP-019, CTAP-IMP-043, SD-IMP-004, HSM-IMP-003, PIVIMP-007a, MGMT-IMP-003, PGP-IMP-018 | Medium | M1 |
| WA-IMP-010, WA-IMP-015, WA-IMP-016 | Medium | M2 |
| CTAP-IMP-036 | Medium | M2 + M1 |
| MGMT-XREF-002 | Medium | A2 (reclassified; Python, Android and Rust all mask the base form factor) |
| WA-IMP-005 | Low | M2 |
| OTP-IMP-004 | Low | A2 (reclassified; the lean, stay silent, is what all three siblings do; Python's SmartCard `send_and_receive` has no prompt or keepalive, `yubikit/yubiotp.py` @4ca60f70 checked) |

**Split cards (9): facet now auto, card stays with a smaller question:**

| Card | Severity | Now auto | Remaining question |
| --- | --- | --- | --- |
| SD-IMP-006 | Medium | replaceKvn passed as `int` to `ApduCommand` (A4: Python and Rust reject negatives); exact 10-byte SCP03 KCV bound to the KVN (A2) | Restrict GENERATE to 0x11, 0x13, 0x15 before sending? |
| OTP-IMP-005 | Medium | Reject a short status; publish state only after validation (M1) | Sequence rule: keep the 5.0.0–<5.5.0 bypass now (B), narrow after hardware (A)? |
| PIVIMP-007b | Medium | Reject unknown nonzero CertInfo modes; add the Net iD zlib variant (A2) | Decompressed-size ceiling (placeholder 64 KiB) |
| PGP-IMP-010 | Medium | Challenge flag and maximum length; cached and ATT UIF gated at 5.2.1; NEO PIN-attempt gate (A2) | Mark synthesized pre-5.2.0 RSA entries (B) or not (A)? |
| MGMT-IMP-015 | Medium | Owned cleanup of the parsed lock code under `try/finally` (A5, OathTool pattern) | Clearable input instead of Spectre string prompts? |
| OTP-IMP-007 | Medium | 4.3.2 ≤ version < 4.3.6 check in `UpdateConfigurationAsync` (A2) | Keep the two-code shape with CLI retain-by-default (A), or add an intent-bearing update API (B)? |
| CTAP-IMP-006 | Medium | New-PIN rules of §6.5.1 (M2, verified) | Current PIN: encodability only (lean) or the full §6.5.1 checks? |
| CTAP-IMP-007 | Low | Require kty, alg, crv and 32-byte coordinates on the peer key (M2, verified) | Tolerate unknown extra labels (lean) or reject them (spec-literal)? |
| OTP-IMP-008 | Low | Reject undefined `NdefType` values before building the payload (M1: "request encoders ... must model and validate what they send", `CLAUDE.md:158`) | Keep null-as-disable and longest prefix (A) or align with the siblings (B)? |

**Low cards checked (10):** 2 leave (WA-IMP-005 M2, OTP-IMP-004 A2), 2 split (CTAP-IMP-007, OTP-IMP-008), 6 stay
whole: OATH-DEC-003 (lean departs from the Key URI text, so M2 can't apply; URI input isn't device data, so M1
can't either), WA-IMP-003 (§5.1.3 leaves the range to the client: a number to set, X5), MGMT-IMP-001 (firmware
preflight is a behaviour choice and waits on MGMT-IMP-002), MGMT-IMP-014 (sample CLI scope, X9), CTAP-IMP-016
(read-before-toggle has no precedent; siblings split on explicit false), OTP-IMP-010 (OtpTool scope, X9).

**Net effect on the review queue:** 57 open gated cards become **41**: 10 High, 22 Medium, 8 Low, 1 qualification.
The Medium count is 22, not the 21 forecast at 20:17, because M2 verification kept CTAP-IMP-006's current-PIN facet
with Dennis. Veto window: 16 whole cards and 14 facets are listed at the top of the skim.

### AG-20 Dennis's vault calls (2026-11-20 11:05)

Source: `Inbox/YubiKit pass-2 review queue.md`, edited by Dennis 2026-11-20 10:53 (backup before Sia's edit:
`$TMPDIR/opencode/vault-backup-20261120/`). Skim: no veto lines filled (53 + 16 + 14 still unvetoed).

**Decided (26):**

| Card | Severity | Call | Note |
| --- | --- | --- | --- |
| WA-IMP-013 | High | B | Mechanical clears now; owner API waits on X3 |
| PGP-IMP-009 | High | Defer | No change this pass |
| PGP-IMP-011 | High | A | Conservative ROCA [4.2.0, 4.3.5); settles X13 |
| PIVIMP-001 | High | A | |
| PIVIMP-006a | High | A | |
| PGP-IMP-015 | High | A | Via existing `OpenPgpCommand.GetPin` (ConsoleCredentialReader); `KeysAttestCommand` never calls it today |
| WA-IMP-002 | Medium | a | |
| CTAP-IMP-006 | Medium | lean | Current PIN encodability only; deliberate departure from the §6.5.1 "PINs used" reading |
| CTAP-IMP-030 | Medium | Defer | |
| CTAP-IMP-047 | Medium | a | |
| SD-IMP-001 | Medium | A | |
| SD-IMP-006 | Medium | A | Dennis asked about a cleaner replaceKvn API; Sia: post-pass API follow-up, Q in vault |
| SD-DEP-005 | Medium | A | |
| OTP-IMP-002 | Medium | A | |
| CORE-OTP-X004 | Medium | A | |
| OTP-IMP-005 | Medium | **A (against lean)** | Narrow now to the Python/Rust sequence rule; 5.4.3 boundary stays on T1 |
| OTP-IMP-007 | Medium | A | |
| HSM-IMP-001 | Medium | A | |
| PGP-IMP-019 | Medium | A | |
| PIVIMP-009 | Medium | A by rule | Dennis: stay V1-codec-compatible. V1 `Piv/Objects/AdminData.cs` (develop = origin/main) writes LE trimmed and reads `ReadInt64LittleEndian`, identical to V2 |
| OATH-DEC-003, WA-IMP-003, MGMT-IMP-014, CTAP-IMP-007, OTP-IMP-010 | Low | lean | |
| CORE-SD-X007 | Qualification | A | |

**Changes to earlier records:**
- WA-IMP-015 (auto, M2): Dennis wrote "Side with siblings" on the queue card. Recorded as a veto: option B, accept
  multi-entry writes; deliberate divergence from L3 §10.1.5 "exactly one element", to be documented.
- HSM-IMP-008: the AG-16 "Defer" was the 2026-11-18 text; the call now asks what Python slices on. Deferral withdrawn.
  Verified: Python `SessionKeys.parse` slices [:16]/[16:32]/[32:48] with no length check; Rust requires >= 48 and keeps
  the first 48. New Sia lean: Rust shape (>= 48, first 48, zero the whole buffer, log length only). A misparse yields
  wrong session keys that the HSM rejects at authentication, so it fails loudly.

**X2 proposal ("continue"), needs Dennis.** All siblings keep the session after reset in every applet. Android OATH
reselects, refreshes salt and challenge, and re-runs `initScp(scpKeyParams)`. Proposal: after a confirmed reset,
reselect, clear all cached and authenticated state, re-read, re-handshake SCP with the retained parameters, and end
the session with a clear error only if that fails. Security Domain reset continues unauthenticated (SD-IMP-005 B).
Would settle SD-IMP-005 B and CORE-HSM-X001 B, flip PGP-IMP-005 (approved terminal) and the end-session part of
MGMT-IMP-010, and change OATH now. Alternative: continue for SD and HSM only, converge later.

**Open (16), answers in `Inbox/YubiKit pass-2 answers 2026-11-20.md`:** High WA-IMP-011 (blank), PIVIMP-002,
PIVIMP-006b (pseudocode supplied), PIVCORE-002, HSM-IMP-008. Medium WA-IMP-014, MGMT-IMP-015 (Sia's cost claim
withdrawn: `ConsoleCredentialReader.ForHexKey(16)` already exists), CTAP-IMP-010, SD-IMP-005, CORE-HSM-X001,
PGP-IMP-010, PGP-CORE-004, PIVIMP-007b (ceiling value only). Low MGMT-IMP-001, CTAP-IMP-016, OTP-IMP-008 (new
lean: reject null as V1 does, explicit disable, case-insensitive prefixes as V1).

**Proposed:** X5, X6 and X13 marked settled by the calls above unless Dennis objects. X9 rule: every example tool
reads secrets through Core `ConsoleCredentialReader`. Q6 and Q7 still open.

### AG-21 Dennis's answers (2026-11-20 12:45)

Source: `Inbox/YubiKit pass-2 answers 2026-11-20.md`, edited by Dennis 2026-11-20 12:32 (backup:
`$TMPDIR/opencode/vault-backup-20261120-1235/`). The queue and the skim were not edited (skim last modified
2026-11-18 20:30, no veto lines filled). The X2 answer line is blank.

**Decided (12 of the 16 open cards):**

| Card | Severity | Call | Note |
| --- | --- | --- | --- |
| WA-IMP-011 | High | A | Enforce verified PRF (L3 §10.1.4 MUST). Unit and integration tests where applicable, with L3 references. PR labels the sibling divergence (rule D1) |
| PIVIMP-002 | High | A, Python shape | Raw calls stay raw; opt-in coherent helpers modelled on ykman `pivman_set_mgm_key` / `pivman_change_pin` / `pivman_set_pin_attempts` (@4ca60f70 `ykman/piv.py:314-420`). Derived mode follows Python: management-key rotation clears the salt, PIN change re-derives with a fresh salt. Rust `crates/ykman/src/piv.rs:96-139` @90940e9b has no derived-mode handling, so Python is the only precedent there. Tests: Q10 |
| PIVIMP-006b | High | C + S1 | Keep the sample's result shape; no clearing. Add the demo-only warning (rule S1) |
| PIVCORE-002 | High | A, all applets (C1) | Document one operation at a time per session; the session refuses a second concurrent operation instead of interleaving. Core-level, all applet sessions. No lease |
| HSM-IMP-008 | High | Rust shape | >= 48 bytes, first 48 as keys, zero the whole buffer, log length only. Hardware capture optional |
| MGMT-IMP-015 | Medium | a | `ConsoleCredentialReader` with `CredentialReaderOptions.ForHexKey(16)` |
| CTAP-IMP-010 | Medium | c | Keep the legacy fallback and document it. Clarification for the record: the choice is driven by GetInfo option `pinUvAuthToken` (`ClientPin.cs:409-416`), not by the PIN/UV auth protocol version (1 or 2) |
| PGP-IMP-010 | Medium | A | Built-in algorithm list on old keys, no inferred flag |
| PGP-CORE-004 | Medium | A | Verified: the advisory UIF read builds a `UserPresenceContext` (Basis PolicyRequires / PolicyMayRequire) for Core `IUserPresencePrompt` (`OpenPgpSession.cs:327-378`), the same primitive PIV, OATH, OTP, HSM Auth and CTAP2 use. Engineer adds a test: failed UIF read plus throwing logger still notifies with PolicyMayRequire and signs |
| PIVIMP-007b | Medium | 64 KiB | Documented SDK safety limit |
| CTAP-IMP-016 | Low | a | `SetAlwaysUvAsync(bool)` with read-before-toggle, plus optional-length `SetMinPinLength` |
| OTP-IMP-008 | Low | B variant | Reject null as V1 does, add explicit `DisableNdefAsync`, case-insensitive URI prefixes as V1 |

Confirmed: PIVIMP-009 A (V1 codec). SD-IMP-006: Dennis prefers the named-intent API; pseudocode in the round 2
vault file, timing open (Q11).

**New rules:**
- **S1 sample apps.** Example tools (`src/*/examples/**`) are illustrations, not reference implementations.
  Readability comes first. No added secret clearing, owned-buffer carriers or defensive copies. Each sample carries a
  "demo only, not for production" warning. Existing helpers that already exist (`ConsoleCredentialReader`, `GetPin`)
  are still used because they cost no readability. Does not apply to `src/Cli.Commands` (production CLI) or SDK code.
  Follow-through on earlier approved sample-hygiene cards is Q9.
- **D1 divergence labelling.** Every PR that diverges from the sibling SDKs lists each divergence explicitly, with the
  spec or policy reason.
- **C1 session admission.** From PIVCORE-002: simple, Core-level "one operation at a time" guard for every applet
  session; no workflow lease.

**Cross-applet:** X4 settled by C1. X5, X6, X13 settled (no objection). X9 settled: secrets are read through
`ConsoleCredentialReader` (MGMT-IMP-015 answer) and sample output follows S1. X3 split: the sample half is settled by
S1; the SDK half (owned results, WA-IMP-013 owner API) stays open. Still open: X1, X2, X3 (SDK half), X12.

**Q6 answered:** a version read that fails outright is also treated as alpha/beta (every gate admits), with
`CachedDeviceInfo` preferred first. Consequence accepted: OpenPGP applets older than 1.0.2 with no device info pass
modern gates and fail at the card instead of in the SDK.

**Still open (4 cards):** WA-IMP-014 (full picture plus CTAP 2.3 check in round 2), MGMT-IMP-001 (what it guards),
SD-IMP-005 and CORE-HSM-X001 (both wait on X2). Correction to the 11:05 framing of WA-IMP-014: a non-discoverable
credential is not stored on the key, so nothing is left on the key; the cost is a registered credential the RP can
never attach a blob to.

**Open questions:** X2 (blank); Q7 (Dennis asked whether it applies: no open card is affected, proposal close it);
Q9 (S1 follow-through on HSM-IMP-002, PIVIMP-006a cleanup scopes, HSM-IMP-005); Q10 (PIVIMP-002 hardware tests);
Q11 (SD-IMP-006 named-intent API in this pass or after).

### AG-22 Dennis's round 2 answers (2026-11-20 13:25)

Source: `Inbox/YubiKit pass-2 answers 2026-11-20 round 2.md`, edited by Dennis 2026-11-20 13:15 (backup:
`$TMPDIR/opencode/vault-backup-20261120-1320/`). The skim is still unedited (last modified 2026-11-18 20:30).

**X2 reset, decided: continue everywhere now (option 1).** Dennis: "we're going to continue the session everywhere,
which flips my earlier calls". Rule: after a confirmed reset the session stays open; reselect, clear all cached and
authenticated host state, re-read what the session needs, and re-run SCP with the same key parameters when the reset
did not touch the Security Domain. If the re-handshake fails, the session ends with a clear error.
- **Flipped:** PGP-IMP-005 (was end session; the "document how to keep the session" follow-up becomes the fix) and
  MGMT-IMP-010 (keeps everything except "end the session"). OATH changes now.
- **CORE-HSM-X001:** B (continue, and refresh SCP state through the shared helper).
- **SD-IMP-005:** continue, re-authenticating with the restored default SCP03 keys (`StaticKeys.GetDefaultKeys()`,
  `src/Core/src/Protocols/SmartCard/Scp/StaticKeys.cs:104`). Dennis: "The static default keys should be available and
  should work to authenticate." Facts: Python's `SecurityDomainSession.reset` docstring says reset restores the default
  SCP03 static keys and generates a new SCP11b key (`yubikey-manager/packages/yubikit/yubikit/securitydomain.py`
  @745c5219). .NET's own docs say the SDK does not verify factory contents after reset
  (`ISecurityDomainSession.cs:331–340`); a successful default-key handshake now provides that check. Open detail Q12:
  sessions that were not on SCP03 before reset.

**Cards decided:** CORE-HSM-X001 B, SD-IMP-005 (above), MGMT-IMP-001 (a) guard now with the 5.7 boundary.

**Questions closed:** Q7 closed without adopting the "stricter option wins" extension to H1. Q9 approved on Sia's lean:
HSM-IMP-002 dropped; PIVIMP-006a keeps only the label; HSM-IMP-005 keeps hidden prompts through the existing reader,
drops the diagnostics flag, prints keys with the demo-only label; existing sample clearing code stays. Q10 approved:
three new PIN-only hardware tests (derived-mode PIN change, protected-mode rotation, derived-mode rotation clears the
salt). Q11: KeyReplacement in this pass, required parameter, four overloads.

**Dennis's follow-up on Q11 (answered in round 3):** reuse KeyReplacement in other applets? Sia's answer: no. GP PUT
KEY carries the replaced KVN on the wire; PIV import, OpenPGP key import, OATH PUT and HSM Auth PUT have no add-versus-
replace field. OATH already documents "adds or replaces ... without checking whether that ID already exists"
(`src/Oath/src/IOathSession.cs:89–94`). A KeyReplacement parameter there would be a host-side read-then-write: extra
round trip, not atomic, and a parameter that changes no bytes. Proposal: document overwrite behaviour on each import
method instead.

**WA-IMP-014, correction:** the round 2 claim that all three siblings "create a credential that can never hold a blob"
was wrong. python-fido2 (`fido2/ctap2/extensions.py:408–430` @5bc9d3a), Android (`LargeBlobExtension.java:72–92`
@f4626856) and Rust (`crates/yubikit/src/webauthn/extensions/large_blob.rs:142–185` @745c5219) all send
`largeBlobKey: true` whenever the authenticator supports it, whatever `rk` is. CTAP 2.3 §12.3 (`audit-specs/ctap23.txt:10019`): "If the options field of the authenticatorMakeCredential request does not map rk to true, return
CTAP2_ERR_INVALID_OPTION." So on a conforming key the siblings' registration fails at the key with a low-level error,
for `required` and `preferred` alike. .NET is the only one that withholds the request when rk is false
(`LargeBlobProcessor.cs:59–76`, `requestKey: supported && requireResidentKey`) and returns success with
`supported: false`. Whether YubiKeys return INVALID_OPTION here is not hardware-verified. Lean A unchanged and now
closer to the siblings' outcome (a failure) with a clear error before transmission. Awaiting Dennis.

**Framed for Dennis (round 3):** X1 (session state after failed authentication), X3 SDK half (who owns secret
outputs), X12 (exact-length credential keys). Facts in the round 3 vault file.

**Still open:** WA-IMP-014, Q12, X1, X3 (SDK half), X12, the veto skim.

### AG-23 Dennis's round 3 answers (2026-11-20 14:05)

Source: `Inbox/YubiKit pass-2 answers 2026-11-20 round 3.md`, edited by Dennis 2026-11-20 13:54 (backup:
`$TMPDIR/opencode/vault-backup-20261120-1400/`). The skim is still unedited (last modified 2026-11-18 20:30).

**Decided:**
- **Q12: (a).** After an SD reset the session always continues on the default SCP03 keys, whatever it used before
  (SCP11a/b/c, SCP03 or none). The default keys are public; the channel enables re-provisioning, not peer proof.
- **KeyReplacement: Security Domain only** ("I approve of this"). PIV import, OpenPGP key import, OATH PUT and HSM
  Auth PUT get explicit "overwrites" docs; no code change there.
- **X1: A** ("approved"). Rule: a failed login never ends the session; it clears the earlier success for that
  credential only (PIV PIN versus management key are independent); the caller can retry, bounded only by the key's
  counter; a failed SCP handshake ends the session (consistent with X2). Docs plus one test per applet; no behaviour
  change expected (PIV `PivSession.cs:302–313`, `:818–820`; OATH-DEC-001). PIV and OpenPGP sibling paths only
  partly read.
- **X12: B** ("I like that!"). New `CredentialPassword` type mirroring Rust `CredentialPassword::from_password` /
  `::new` (`hsmauth.rs:159–187`) and Python's str/bytes split (`hsmauth.py:90–104`): `FromUtf8` (at most 16 bytes,
  zero-padded) and `FromRaw` (exactly 16, otherwise throws). Seven `credentialPassword` parameters in
  `src/YubiHsm/src/IHsmAuthSession.cs` change (lines 71, 104, 164, 246, 276, 300, 324). It owns a secret, so per
  `CLAUDE.md` it is a sealed `IDisposable` class that zeroes in `Dispose`. Matches both siblings; D1 label not needed
  for the split itself.

**WA-IMP-014, Dennis's question:** "do the sibling SDKs always append a large blob extension, even though it may not
have been requested?" Answer, verified: no. python-fido2 (`fido2/ctap2/extensions.py` `LargeBlobExtension.make_credential`
@5bc9d3a), Android (`LargeBlobExtension.java` `makeCredential` @f4626856) and Rust (`large_blob.rs` `make_credential`)
only send `largeBlobKey: true` when the request carries the `largeBlob` input; Rust also skips it when the key lacks
`largeBlobKey`. .NET matches. The gap is only an explicit `largeBlob` request without `rk`. No letter logged yet;
lean A re-asked in round 4.

**X3, Dennis's direction:** a combination of A and B where it makes sense, in house style, case by case; response
objects sometimes, direct buffers sometimes; possibly a clear and/or dispose method. Asked for example call sites per
applet. Round 4 proposes rule **H**: P1 `Memory<byte>` for a single secret (PIV unchanged; OpenPGP `DecryptAsync`,
OATH `CalculateAsync` and YubiOTP `CalculateHmacSha1Async` move from `ReadOnlyMemory<byte>`); P3 sealed disposable
result object for composite results (`SessionKeys` unchanged; `PrfEvaluationResults` and `LargeBlobAuthenticationOutput`
move); P2 `IMemoryOwner<byte>` stays internal to Core and the credential reader; `Dispose` only, no `Clear()`.
Alternatives H+R (one disposable `AuthenticationResponse`) and B (P2 for single secrets). House-style basis:
`CLAUDE.md` memory and security sections, `docs/SDK-HOUSE-STYLE.md:243`, `Core/src/Utilities/DisposableArrayPoolBuffer.cs`.
New finding: OATH `CalculateAsync` (`IOathSession.cs:173`) and YubiOTP `CalculateHmacSha1Async`
(`IYubiOtpSession.cs:308`) return `ReadOnlyMemory<byte>` while OATH's docs tell callers to clear via
`MemoryMarshal.AsMemory`; OpenPGP `DecryptAsync` docs say "Caller-owned ... clear it after use" on a read-only type.
CTAP2 `ClientPin` tokens are `byte[]` and stay.

**Still open:** WA-IMP-014 letter, X3 shape (H, H+R or B, and whether OATH and YubiOTP are included), the veto skim.

### AG-24 Dennis's round 4 answers (2026-11-20 15:05)

Source: `Inbox/YubiKit pass-2 answers 2026-11-20 round 4.md`, edited by Dennis 2026-11-20 14:54 (backup:
`$TMPDIR/opencode/vault-backup-20261120-1500/`). The skim is still unedited (last modified 2026-11-18 20:30).

**Decided:**
- **WA-IMP-014: A** ("approved"). With `largeBlob: required` and no discoverable credential requested, refuse
  before any CTAP traffic with a clear NotSupported error. Close to what the siblings end up doing (the key refuses
  `largeBlobKey` without `rk` per CTAP 2.3); D1 label: ".NET refuses client-side instead of at the authenticator".
  **This was the last gated card; all cards are now decided.**
- **X3, single secrets: P1** ("I approve of P1 for single secrets, including OATH and YubiOTP"). A single secret is
  returned as a fresh, caller-owned `Memory<byte>`; the caller zeroes it. PIV `DecryptAsync` / `CalculateSecretAsync`
  unchanged; OpenPGP `DecryptAsync`, OATH `CalculateAsync` (`IOathSession.cs:173`) and YubiOTP
  `CalculateHmacSha1Async` (`IYubiOtpSession.cs:308`) move from `ReadOnlyMemory<byte>`, and the
  `MemoryMarshal.AsMemory` advice in the OATH docs goes away.

**X3, composite results: not accepted yet.** Dennis: not convinced by the disposable types; worried about developer
UX and complexity, and notes that dispose needs remembering just like zeroing. Asked for .NET ecosystem guidance.

Survey (verified sources):
- **BCL crypto:** outputs are plain `byte[]` (`RSA.Decrypt`, `ECDiffieHellman.DeriveKeyMaterial`) or a
  caller-supplied `Span<byte>` destination (`RSA.TryDecrypt`, `HKDF.DeriveKey`, `Rfc2898DeriveBytes.Pbkdf2`).
  Disposable types (`AesGcm`, `HMAC`, `RSA`, `ECDsa`) are key *holders*, not results. `CryptographicOperations.ZeroMemory`
  is the provided clearing tool.
- **`SecureString`:** the BCL's disposable secret wrapper, now discouraged for new code (dotnet/platform-compat
  DE0001): it only shortens the plaintext window and is unencrypted outside .NET Framework.
- **Azure Key Vault** `CryptographyClient.DecryptAsync` returns `DecryptResult` with `byte[] Plaintext`; not
  disposable (`sdk/keyvault/Azure.Security.KeyVault.Keys/src/Cryptography/DecryptResult.cs`).
- **Fido2NetLib** (the main .NET WebAuthn server library): `AuthenticationExtensionsPRFValues` has
  `byte[] First` and `byte[]? Second`; not disposable (`Src/Fido2.Models/Objects/AuthenticationExtensionsPRFValues.cs`).
- **Disposable precedents exist only for secret holders:** NSec `SharedSecret : IDisposable` (libsodium-backed,
  consumed by key derivation, `Export` returns `byte[]`); ASP.NET Core DataProtection `Secret : IDisposable, ISecret`
  ("Wipes the secret from memory"), a long-lived key holder.
- **Pattern:** in the ecosystem, *results* are plain caller-owned bytes; *holders* of a secret that is used again
  later are disposable.

Repo-specific hazard: `PrfEvaluationResults` and `LargeBlobAuthenticationOutput` are `sealed record class`
(`src/WebAuthn/src/Extensions/Outputs/PrfOutput.cs:29`, `LargeBlobOutput.cs:51`). Making a record disposable
interacts badly with `with` copies and value equality: a copy shares the same array, so disposing one clears the
other, and equality changes after dispose.

New lean **R1** (replaces H for composites):
- Results, single or composite, use P1. Secret fields inside result records become writable caller-owned
  `Memory<byte>` (`PrfEvaluationResults.First`/`Second`, `LargeBlobAuthenticationOutput.Blob`). Records stay records
  and are not disposable. Docs on each field: "caller-owned; zero with `CryptographicOperations.ZeroMemory`".
- `IDisposable` only for secret *holders* that are passed back into the SDK or kept across calls:
  `CredentialPassword` (AG-23, input) and HSM Auth `SessionKeys` (existing; handed to the HSM session, like NSec
  `SharedSecret`). No new disposable result types.
- No `Clear()` methods. P2 stays internal.
- Trade-off accepted: no compile-time or analyzer help for forgetting to zero (CA2000 is off by default and would
  only help the disposable shape anyway). Managed zeroing is best-effort defence in depth (GC compaction can leave
  copies), so the cheaper UX wins.

**Still open:** R1 (yes or no), the veto skim.

### AG-25 review closed (2026-11-20 21:30)

Source: `Inbox/YubiKit pass-2 answers 2026-11-20 round 5.md`, answer line under R1 "Approved" (edited 21:24), and
Dennis in chat at 21:27: "I approve all of the skim-through auto-approved cards". The skim file itself was not
edited (last modified 2026-11-18 20:30); the chat answer is the veto pass.

- **X3 settled (R1).** Single and composite secret results use P1: a fresh, caller-owned, writable `Memory<byte>`.
  `PrfEvaluationResults.First`/`Second` and `LargeBlobAuthenticationOutput.Blob` become `Memory<byte>`; the records
  stay records and are not disposable. `IDisposable` only for secret holders: `CredentialPassword` (X12) and HSM
  Auth `SessionKeys`. No `Clear()` methods; `IMemoryOwner<byte>` stays internal to Core. Each secret field documents
  "caller-owned; zero with `CryptographicOperations.ZeroMemory`".
- **Veto pass (AG-3) done: no vetoes.** All 53 original autos, the 16 AG-19 whole-card autos and the 14 split facets
  go to the engineer as written. WA-IMP-015 keeps the AG-20 veto (option B).
- **All cross-applet decisions settled:** X1 (AG-23), X2 (AG-22), X3 (AG-21 sample half, AG-24 and AG-25 SDK half),
  X4 (AG-21, C1), X5, X6 (AG-20/21), X7 (AG-19, M1), X8 (AG-14, Q6), X9 (AG-21), X10 (AG-13), X11 (AG-11),
  X12 (AG-23), X13 (AG-20/21).
- **Ledger.** `pass-2-review-ledger.md` lists all 178 cards (plus the folded CORE-HSM-X005) with the final call and
  where it was recorded. Where a later agreement changed an earlier call (X2 flips, Q9, AG-14 firmware rule, the
  WA-IMP-015 veto), the ledger states the final call. The per-applet classification files keep the original
  analysis and their disposition columns are superseded by the ledger.

### AG-26 CTAP-IMP-027 ownership

Dennis explicitly approved the R1 ownership model for `MakeCredentialResponse` and `GetAssertionResponse` in chat.
Both stay non-disposable. Sensitive result buffers must be caller-owned writable `Memory<byte>` so callers can zero
them. The SDK retains cleanup of sensitive buffers it owns on decode failure and must eliminate inaccessible
sensitive copies. This supersedes the [earlier disposable-response proposal](pass-2-review-classification/ctap2.md#ctap-imp-027-credential-and-extension-result-owners).
The exact raw-envelope API remains an open engineering detail; no `RawData` property was approved.

### AG-27 R1 Clear scope

In R1 (AG-25), “no `Clear()`” prohibits adding public `Clear()` convenience methods to applet result types. It does not
remove or deprecate the existing public `ApduResponse.Clear()` or change the SDK’s obligation to clear sensitive
buffers it owns.

### AG-28 Delivery commit scope

- Keep one commit per unsplit card. For a split card, use one commit per approved, independently scoped facet.
- Each card or facet commit carries the canonical `Audit-Card` ID and its actual applicable `Triage-Rule`. Identify
  the facet in the subject or body; do not add a trailer. Each commit must build and pass the affected module's
  unit tests so it remains a meaningful unit to bisect or revert.
- Implement shared cross-applet changes once in the foundational cross-cutting PRs, not again in each applet PR.
  For a genuinely indivisible change spanning multiple cards, confirm attribution case by case; do not invent a
  trailer convention.

### AG-29 C1 caller serialization

Dennis's later approval of option 1 supersedes AG-21's fail-fast C1 decision. Do not add the SDK-wide
`EnterOperation` guard. Callers use a session one operation at a time and await the complete operation before
starting another on that session or disposing it. Concurrent use is unsupported. The existing logical protocol
exchange guard is unchanged; it does not imply thread safety, an atomic workflow, device-wide exclusion, or
draining the rest of an applet workflow during disposal.

**PIVCORE-002** is documentation only: the [general Core session contract](../../architecture/connection-ownership-and-contention.md#protocol-exchange-overlap)
and PIV-specific guidance in the PIV PR. This disposition does not presume or authorize new bug fixes. Fix a
verified concurrency or state defect only within existing narrow approval; escalate new findings for a scope
decision rather than creating new cards under C1.

### AG-14 firmware rule detail

- Dennis, 2026-11-17: "If the applet's version read comes up as 000, or if it equals the static firmware version
  (alpha or beta), then that is the assumed firmware version. If we cannot read a production firmware version, we're
  going to assume that it is alpha or beta, and then we're going to admit all the firmware version gates."
- Order: `CachedDeviceInfo.FirmwareVersion` when present; else the applet's own version read; a 0.x result from
  either means alpha/beta and admits all gates.
- This matches what Core already does: `FirmwareVersion.IsAlphaOrBeta => Major == 0`
  (`src/Core/src/Devices/FirmwareVersion.cs:63`), `Feature.IsSupportedByFirmware` admits it
  (`src/Core/src/Devices/Feature.cs:57`), and CTAP2 defaults a missing GetInfo version to `new FirmwareVersion()`
  (`src/Ctap2/src/Ctap2Session.cs:161`), documented in `src/Ctap2/CLAUDE.md:44–46`. The rule makes it policy.
- Effect on cards: **CTAP-IMP-002** becomes option (a) for the primary source with the optimistic (b) behaviour as
  fallback; Sia's earlier "fail closed when neither source knows" is withdrawn. **OTP-IMP-006**, **PGP-CORE-003**
  and **CORE-HSM-X004** use the same order. PGP-CORE-003's failed-read case waits on Q6.
- Accepted cost (AG-2 alpha): on a production key whose version read returns a 0.x sentinel, an unsupported command
  reaches the device and fails with a device status instead of a clean "not supported" error.

### AG-15 MGMT-IMP-012 no-regression guard

- Python `set_mode` (`yubikit/management.py:695–751` @4ca60f70) sends the legacy SET_MODE only below 5.0.0; on 5.0.0
  and later it translates the mode into a DeviceConfig write. Payload `struct.pack("<BBH", code, chalresp_timeout,
  auto_eject_timeout or 0)`.
- .NET today: `src/Management/src/Backend/SmartCardBackend.cs:58–68` sends INS 0x16 with P1 = 0; no public caller.
- Engineer guardrails: change P1 to 0x11 and the payload to little-endian; keep the primitive internal with no new
  public API; any future caller must route firmware 5.0.0 and later (and alpha/beta, which .NET treats as newest
  under AG-14) to DeviceConfig, never to legacy SET_MODE. Python sends 0.x keys down the legacy path because its
  tuple compare treats 0.x as old; .NET deliberately differs per AG-14. Emitted-bytes unit test for the APDU;
  no device run, since NEO and YK4 are not in the qualification record.

### X11 (decided 2026-11-17, AG-11)

- **X11:** are experimental draft extensions (previewSign, ARKG) in scope for this pass? Explained 2026-11-15.
  - Scope correction: X11 touches 8 cards, not 3. Interview: CTAP-IMP-029 (High, G1), CTAP-IMP-040 (Medium, G3),
    CTAP-IMP-038 and CTAP-IMP-041 (Medium, D0). Auto: CTAP-IMP-039 (A2), WA-IMP-019 (A2), WA-IMP-020 and 021 (A1).
    Core ARKG math (CORE C018/C019) is Core-owned and outside the applet cards.
  - Facts: .NET labels the surface experimental only in XML docs (`PreviewSignCbor.cs:25–27`,
    `ArkgP256SignArgs.cs:20, 40`); no `[Experimental]` attribute is used anywhere in `src/` (net10.0 supports it).
    python-fido2 marks previewSign and ARKG "DRAFT ... NOT part of the stable API" (`fido2/ctap2/extensions.py:704–705`,
    `fido2/arkg.py:48–49` @5bc9d3a). Android and Rust ship previewSign but no ARKG. ARKG draft-10's stated expiry was
    2026-08-31 (master line 3777); python-fido2 cites draft-08 in one file and draft-10 in another.
  - Sia's lean, approved 2026-11-17 as AG-11 (038 and 041 decided rather than parked): "safety floor in, design out". Do the four autos, CTAP-IMP-029 option (a)
    (validate curve, coordinate length and on-curve points before ARKG derivation) and CTAP-IMP-040 option (a)
    (validate COSE_Sign_Args as one map, reject empty TBS and key handle). Park CTAP-IMP-038 and -041 in an
    experimental backlog until a previewSign/ARKG profile is pinned. Add `[Experimental("YKEXP001")]` to the public
    previewSign and ARKG surface so consumers must opt in (new proposal, G5-style: better than siblings' doc-only warning).

### AG-5 delivery proposal

The original rule is retained below; [AG-28](#ag-28-delivery-commit-scope) clarifies the split-card exception.

- One commit per card. Conventional subject, for example `fix(oath): require 16-byte access key`, with trailers
  `Audit-Card: OATH-DEC-002` and `Triage-Rule: A4`. Each commit builds and passes the module's unit tests, so any
  single card can be reverted or bisected.
- One PR per applet, as separate stacks (unrelated work, per `CLAUDE.md`), always created with an explicit `--base`.
  About 9 applet PRs plus a few cross-cutting PRs, instead of ~159.
- Cross-cutting (T2) decisions land first, each in its own PR, because they change several applets at once.
- Docs-only and test-only cards (A6) ride in the same applet PR as separate commits.
- Engineer works one applet at a time through the DevTeam loop with a cross-vendor reviewer and the Pre-Commit
  Checklist. PRs open as drafts; nothing merges without Dennis.
- Base-branch fact: `yubikit-docs-audit` is 42 commits ahead of `origin/yubikit` and 0 behind (checked
  2026-11-14), including code, not only docs.

### AG-7 Copilot review-fix loop

- Trigger: an applet PR is pushed with all its card commits building and passing tests.
- Request: `gh pr edit <pr> --add-reviewer @copilot` (supported in local `gh` 2.104.0, checked 2026-11-14).
  Re-request the same way after each fix push.
- Triage each Copilot comment: major (correctness, security, wire behaviour, public API, test gap on changed code)
  must be fixed or explicitly rebutted with evidence; minor and style comments are fixed if cheap, otherwise replied
  to with a reason.
- Fixes for review comments go in as their own commits with an `Audit-Card` trailer when they belong to a card,
  otherwise `Review-Fix: copilot`.
- Exit: no unresolved major comments. Cap at 3 rounds; if majors remain after round 3, escalate to Dennis.
- Copilot review is in addition to the internal cross-vendor reviewer, not a replacement.

### Batch 1 re-run through the policy (proposal, awaiting veto)

| Card | Rule | Result |
| --- | --- | --- |
| OATH-DEC-001 | A2 | Auto: keep locked, all three siblings agree |
| OATH-DEC-002 | A4 | Auto: strict 16 bytes, Android and Rust strict |
| OATH-DEC-003 | A6 | Auto: keep and document; counter widening deferred |
| WA-IMP-001 | A3 | Auto: python-fido2 and Android both forward enterprise attestation |
| WA-IMP-002 | G5 | Interview: Sia's call keeps per-extension omission, which diverges from python-fido2 failing the whole ceremony |
| WA-IMP-003 | A4 | Auto: stricter range check; success-can-win kept |

## Batch 1 (sent 2026-11-14)

| Card | Severity | Sia's call | Dennis's decision |
| --- | --- | --- | --- |
| OATH-DEC-001 | Medium, conditional | Keep locked state, document fresh-session recovery, add test | _pending_ |
| OATH-DEC-002 | Low | Fix: exactly 16 bytes in Validate and SetKey | _pending_ |
| OATH-DEC-003 | Low | Keep permissive parsing, document it; keep Int32 counter, defer widening | _pending_ |
| WA-IMP-001 | Medium | Fix Enterprise forwarding like python-fido2; document Indirect as passthrough | _pending_ |
| WA-IMP-002 | Medium | Fix: GetInfo and token errors through the ceremony mapper; keep and document per-extension omission | _pending_ |
| WA-IMP-003 | Low | Keep success-can-win; validate timeout range before starting the timer | _pending_ |

### Evidence

**OATH-DEC-001** (state after a device HMAC mismatch)
- .NET: `src/Oath/src/OathSession.cs:745-750` throws BadResponseException before `IsLocked = false`; challenge kept.
- Python: `yubikit/oath.py:333-351` raises before clearing the challenge.
- Android: `oath/.../OathSession.java:264-298` `unlock` returns false and keeps the challenge.
- Rust: `crates/yubikit/src/oath.rs:699-719` returns `InvalidData` and keeps the challenge.
- No sibling marks the session terminal. A mismatch at this point means the device accepted our HMAC but sent back a wrong one, so the device itself can't be trusted. Retrying with the same stale challenge can't succeed.

**OATH-DEC-002** (raw access-key length)
- .NET: `OathSession.cs:702-704` and `:786-787` reject only empty keys; SetKey sends the full raw key.
- Android: `OathSession.java:328-331` `setAccessKey` requires exactly 16 bytes.
- Rust: `oath.rs:122-133` `OathAccessKey::new` requires exactly 16 bytes; Validate and SetKey only take that type.
- Python: permissive; it's the outlier.

**OATH-DEC-003** (URI parsing and counter width)
- .NET: `src/Oath/src/CredentialData.cs:179-183` (query issuer wins), `:217-222` (missing counter means 0, Int32).
- Python: `oath.py:118-146` query issuer wins, counter defaults to 0; `:407-408` packs it as an unsigned 32-bit value.
- Rust CLI: `crates/ykman-cli/src/oath.rs:926-1000` query issuer overrides the label issuer, counter `u32` defaults to 0, digits default to 6.
- Android: `oath/.../CredentialData.java:97,124-130,297-316` has the **label** issuer win over the query issuer, counter is a signed int defaulting to 0, and digits must be 6 to 8.

**WA-IMP-001** (Enterprise and Indirect attestation)
- .NET: `src/WebAuthn/src/Client/WebAuthnClient.Registration.cs:205-207` has no enterprise field; `IWebAuthnBackend.cs:121` `BackendMakeCredentialRequest` has no carrier either. No `EnterpriseAttestation` exists anywhere in the non-test source.
- python-fido2: `fido2/client/__init__.py:759-769` sends `2` (platform-facilitated) when GetInfo has `ep` and the RP is in the configured enterprise RP list, `1` (vendor-facilitated) when no list is configured, and otherwise nothing.
- Android: `fido/.../client/Ctap2Client.java:194,224-231` reads `ep` and forwards an `enterpriseAttestation` integer.
- Indirect: neither sibling anonymizes; both pass it through.

**WA-IMP-002** (ceremony errors and extension isolation)
- .NET: `src/WebAuthn/src/Client/WebAuthnClient.Ceremony.cs:131-136` runs GetInfo and token acquisition outside the error mapper; per-extension CBOR and InvalidOperation failures are dropped.
- python-fido2: `__init__.py:1130-1139` wraps the whole backend call, GetInfo included, and maps CtapError to ClientError. At `:905-913`, an extension output ValueError fails the whole ceremony as CONFIGURATION_UNSUPPORTED, even though the credential already exists.

**WA-IMP-003** (timeout input and the success race)
- .NET: `src/WebAuthn/src/Client/CeremonyDeadline.cs:37-41` uses a cooperative linked token. A null timeout starts no timer, and the 300-second default isn't applied.
- python-fido2: `__init__.py:1119-1141` turns `options.timeout` in milliseconds into a Timer that sets the abort event. It doesn't validate the range, it doesn't check the result after a success, and the timer starts before client-data collection.
- Android: no client-level timeout; the caller cancels through CommandState.

## Next steps

After [AG-29](#ag-29-c1-caller-serialization), the withdrawn guard is not a shared prerequisite. Start with OATH;
future fix PRs target `yubikit-pass2-base`, one PR per applet.

Status 2026-11-20 21:30: AG-25 logged. Review complete: every card decided, every cross-applet decision settled,
veto pass done with no new vetoes. Record committed on `yubikit-docs-audit`. Next: delegate per AG-5, cross-cutting
PRs first (X1 to X13 and rules S1, D1, C1, R1), then OATH, then the remaining applets in master order, each against
`--base yubikit-docs-audit`, then the AG-7 Copilot loop. No engineering started.

Status 2026-11-20 15:05: AG-24 logged. WA-IMP-014 A; all gated cards decided. X3 single secrets P1 (OpenPGP, OATH,
YubiOTP move; PIV unchanged). Open: X3 composite shape (new lean R1: plain records with writable `Memory<byte>`
fields, disposable only for secret holders), the veto skim. After those the review is complete and the engineer
starts with the cross-applet PRs. No engineering started.

Status 2026-11-20 14:05: AG-23 logged. Q12, KeyReplacement scope, X1 and X12 decided. Open: WA-IMP-014 (letter
still needed), X3 SDK half (pattern choice, examples in round 4), the veto skim. After those the review is complete
and the engineer starts with the cross-applet PRs. No engineering started.

Status 2026-11-20 13:25: AG-22 logged. X2 decided (continue everywhere; PGP-IMP-005 and MGMT-IMP-010 flipped).
CORE-HSM-X001, SD-IMP-005, MGMT-IMP-001 decided; Q7, Q9, Q10, Q11 closed. Open: WA-IMP-014 (corrected framing), Q12
(post-reset SCP for non-SCP03 Security Domain sessions), X1, X3 (SDK half), X12, the veto skim. No engineering started.

Status 2026-11-20 12:45: AG-21 logged. 12 of 16 cards decided; 4 open (WA-IMP-014, MGMT-IMP-001, SD-IMP-005,
CORE-HSM-X001). Q6 closed. X4, X5, X6, X9, X13 settled; X1, X2, X3 (SDK half), X12 open. Q7, Q9, Q10, Q11 and the
veto skim open. No engineering started.

Status 2026-11-20 11:05: AG-20 logged. 26 of 41 decided; 16 open (5 High incl. reopened HSM-IMP-008, 8 Medium,
3 Low) plus the X2 reset rule, X9, Q6, Q7 and the veto skim. No engineering started.

Status 2026-11-18 20:40: AG-19 applied. X7 decided (M1). 16 more cards auto and 14 facets split off 9 staying cards,
all listed at the top of the skim for veto. Open gated cards: 41 (10 High, 22 Medium, 8 Low, 1 qualification), list
at the top of `Inbox/YubiKit pass-2 review queue.md`. Still open: Q6, Q7, the veto skim (now 53 + 16 cards + 14
facets), X1–X6, X9, X12, X13. No engineering started.

Status 2026-11-18 20:17: unchanged except AG-18 (M1/M2 proposal, Q8 open). No new vault calls. If Q8 is approved,
the Medium queue drops from 36 to 21 and X7 closes.

Status 2026-11-18 20:05: still not ready for the full engineer run; no engineering started. Decided and ready: 5 F1
cards, 8 X11 cards, 35 D0 cards, 8 High review-queue cards (AG-16; HSM-IMP-008 deferred). Blocking the AG-5 order:
Dennis's veto skim of the 53 autos (AG-3); open cross-cutting themes X1–X7, X9, X12, X13; 57 gated interview cards
(10 High, 36 Medium, 10 Low, 1 qualification); Q6; Q7 (H1 scope).

Status 2026-11-17 21:30 (superseded): not ready for the full engineer run; no engineering started (Dennis: "don't start any
engineering efforts yet"). Decided and ready: 5 F1 cards, 8 X11 cards, 35 D0 cards. Blocking the AG-5 order:
Dennis's veto skim of the 53 autos (AG-3); open cross-cutting themes X1–X7, X9, X12, X13 (X8, X10, X11 settled);
66 gated interview cards, 19 of them High; Q6.

Vault copies for mobile review (generated 2026-11-17 from the per-applet files by
`pass-2-review-build-vault.py` in this folder; per-applet files stay the source of truth):
- `Inbox/YubiKit pass-2 review queue.md`: the 66 gated cards, High first, each with question, lean and folded detail.
- `Inbox/YubiKit pass-2 auto-approved skim.md`: the 53 autos with rule, one-line reason and folded pseudocode.
- `Inbox/YubiKit pass-2 D0 cards.md`: done, all 35 decided.

1. Classify all ~159 cards into `pass-2-review-classification.md`: rule or gate, evidence pin, pseudocode for
   every auto-approve, and an interview queue sorted by severity. Replaces the card-by-card batches.
2. Extract the T2 cross-cutting decisions and the T1 hardware-qualification list into the same file.
3. Next voice session: T2 decisions first, then gated cards, High first.
4. After Dennis's veto pass, delegate applet by applet to the engineer: one commit per card, one draft PR per
   applet against `yubikit-docs-audit`, then the AG-7 Copilot loop.
5. Suggested applet order (to confirm at the T2 session): cross-cutting PRs, then OATH (smallest, already half
   triaged), then the rest in master order.
