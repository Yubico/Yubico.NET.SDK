# Pass 2 review: merged classification

Merged by Sia on 2026-11-14 from nine per-applet classifications (AG-8), each produced by a dedicated agent that
classified one applet only. Source: `pass-2-applet-master.md` at `52d34ca65`. Policy: `pass-2-review-policy.md`.
Per-card detail (evidence pins, pseudocode, options, Sia's lean) lives in the per-applet files under
`pass-2-review-classification/`; this file is the index and the voice agenda.

## Totals

| Applet | Cards | Auto | Interview (gated) | Interview (no rule, D0) | T1 | High |
| --- | --- | --- | --- | --- | --- | --- |
| OATH | 8 | 3 | 1 | 0 | 4 | 0 |
| WebAuthn | 23 | 11 | 10 | 1 | 1 | 3 |
| Management | 21 | 8 | 6 | 4 | 3 | 1 |
| CTAP2 | 50 | 15 | 16 | 17 | 2 | 4 |
| Security Domain | 14 | 5 | 7 | 0 | 2 | 1 |
| YubiOTP | 14 | 3 | 7 | 3 | 1 | 0 |
| YubiHSM Auth | 13 (+1 folded) | 2 | 6 | 4 | 1 | 3 |
| OpenPGP | 18 | 3 | 11 | 3 | 1 | 7 |
| PIV | 17 | 3 | 9 | 3 | 2 | 6 |
| **Total** | **178** | **53 (30%)** | **73** | **35** | **17** | **25** |

The card count is 178, not ~159, because classifiers indexed unnumbered qualification and lifetime cards and some
referrals separately. My earlier 60–70% auto estimate was wrong: auto is 30%. Two reasons: the master's sibling
precedent often splits or shares the bug (G3 fired 30+ times), and 35 cards fit no A-rule (proposed D0).

## Recommended voice agenda

1. **Policy tweaks (two yes/no questions)**
   - D0: no-gate, no-rule cards go to interview at lowest priority within severity (approved 2026-11-15, AG-9).
   - **F1 fast-confirm (approved 2026-11-15, AG-10; all five cards approved 2026-11-15, PIVIMP-005 with PKCS#1 v1.5 only):** a High card whose fix is mechanical and on which all siblings agree gets a
     one-line confirmation instead of a full interview. Candidates: PGP-IMP-001, SD-IMP-002, CTAP-IMP-009,
     PIVIMP-005 (framing part only), WA-IMP-012. Saves roughly five interviews.
2. **T2 cross-cutting decisions** (below). Each settles several cards across applets at once.
3. **Remaining High cards**, by applet.
4. Medium and Low gated cards, then D0 cards, per applet in delivery order.

## T2 cross-cutting decisions (deduplicated)

| # | Theme | Cards it settles or shapes |
| --- | --- | --- |
| X1 | Session state after failed authentication | OATH-DEC-001, CORE-HSM-X001, PIVCORE-002, PGP PIN verification state |
| X2 | Reset eligibility and lifecycle (what is reselected, what host state survives) | MGMT-IMP-010, PIVIMP-001, PGP-IMP-005, SD-IMP-005, SD-IMP-001, CORE-HSM-X001, CTAP-IMP-048 |
| X3 | Secret buffer ownership and zeroing (owned results, temporaries, input prompts) | WA-IMP-013, PIVIMP-006a, PIVIMP-006b, HSM-IMP-002, MGMT-IMP-004, MGMT-IMP-015, OTP-IMP-003, SD-IMP-008, CTAP-IMP-004/005/021/024/035, CORE-CTAP-X020 |
| X4 | Whole-operation lifetime and admission (lease vs caller serialization, disposal) | WA whole-ceremony card, PIVCORE-002, PIVIMP-008, CORE-HSM-X003, PGP whole-workflow lease, CTAP-IMP-018/027 |
| X5 | Cancellation and timeout semantics | WA-IMP-003, PIVIMP-006d, CTAP-IMP-047, MGMT-IMP-016, OTP-IMP-004 |
| X6 | Error mapping and diagnostics (status provenance, inner-message copying, optional-output failure) | WA-IMP-002, WA-IMP-016, SD-DEP-005, HSM-IMP-001, PGP-IMP-019, CORE-CTAP-X020 |
| X7 | Strict vs lenient response parsing and forward-compatible carriers | PIVIMP-007a, HSM-IMP-003, HSM-IMP-008, CTAP-IMP-001/034, SD-IMP-004, OTP-IMP-005, MGMT-IMP-003, PGP-IMP-018 |
| X8 | Firmware gating and version-fallback provenance | OATH-DEC-007, CTAP-IMP-002, OTP-IMP-006, PGP-CORE-003, CORE-HSM-X004, MGMT-IMP-008 |
| X9 | Sample and CLI handling of secrets and presence prompts | HSM-IMP-005, PIVIMP-006a, PGP-CORE-004, PGP-IMP-015, OTP-IMP-010, MGMT-IMP-014 |
| X10 | Destructive and device-dependent test policy | MGMT-IMP-007, HSM-IMP-006, PGP-IMP-016, OTP-IMP-011, CTAP-IMP-020/021 |
| X11 | Experimental draft extensions in scope for this pass? (previewSign, ARKG) | Interview: CTAP-IMP-029 (High), CTAP-IMP-040, CTAP-IMP-038 (D0), CTAP-IMP-041 (D0). Auto: CTAP-IMP-039, WA-IMP-019, 020, 021 |
| X12 | Exact-length credential keys | OATH-DEC-002 (A4, auto), new YubiHSM Auth observation: .NET pads short credential passwords, Python and Rust require 16 bytes |
| X13 | ROCA vulnerable-firmware range | PGP-IMP-011 (High), PIV equivalent |

X11 is the cheapest lever. Correction 2026-11-15: it touches four interview cards (one High, three Medium) and four autos, not three cards. Sia's lean is "safety floor in, design out"; see the decisions file.

Status 2026-11-17: **X10 settled** (AG-13: tests start from a known state; self-cleanup is an explicit exception).
**X11 settled** (AG-11: in scope behind `[Experimental]`). **X8 settled** (AG-14: cached device info, then the
applet's own read; 0.x means alpha/beta and admits every gate; Q6 open on reads that fail outright). D0: 35 of 35
decided (AG-12, AG-15). Remaining: 66 gated cards (19 High), vault copy `Inbox/YubiKit pass-2 review queue.md`;
53 autos for veto, vault copy `Inbox/YubiKit pass-2 auto-approved skim.md`.

Status 2026-11-18 20:40: **X7 settled** (AG-19, M1: the repo forward-compatibility doctrine, `CLAUDE.md:156–158`).
M1 and M2 in force; 16 more cards auto and 14 facets split off 9 cards (decisions file, AG-19). The per-applet files
below are not rewritten; the decisions file's AG-16 to AG-19 tables override their dispositions. Remaining: 41 gated
cards (10 High, 22 Medium, 8 Low, 1 qualification). Do not regenerate the vault files with
`pass-2-review-build-vault.py`: it would overwrite Dennis's calls in the review queue and the 2026-11-18 additions.

Status 2026-11-20 12:45: **X4 settled** (AG-21, rule C1: one operation at a time per session, simple Core guard).
**X5, X6, X13 settled** (AG-20/21). **X9 settled** (secrets via `ConsoleCredentialReader`; sample output under rule
S1). **X3 split:** sample half settled by S1, SDK half open. **Q6 closed** (failed reads also admit as alpha/beta).
Open: X1, X2, X3 (SDK half), X12. Remaining gated cards: 4 (WA-IMP-014, MGMT-IMP-001, SD-IMP-005, CORE-HSM-X001).

Status 2026-11-20 13:25: **X2 settled** (AG-22: continue after reset in every applet; SD reset re-authenticates with
the restored default SCP03 keys; PGP-IMP-005 and MGMT-IMP-010 flipped). Open: X1, X3 (SDK half), X12, framed in
`Inbox/YubiKit pass-2 answers 2026-11-20 round 3.md`. Remaining gated cards: 1 (WA-IMP-014).

Status 2026-11-20 14:05: **X1 settled** (AG-23, A: failed login never ends the session; clears only that
credential's success; SCP handshake failure ends it). **X12 settled** (AG-23, B: `CredentialPassword` with
`FromUtf8` and `FromRaw`). Q12 closed (a). Open: X3 SDK half (pattern choice; call-site examples in
`Inbox/YubiKit pass-2 answers 2026-11-20 round 4.md`). Remaining gated cards: 1 (WA-IMP-014, letter not yet given).

Status 2026-11-20 15:05: **WA-IMP-014 decided** (AG-24, A); no gated cards remain. **X3 SDK half partly settled**:
single secrets are P1 `Memory<byte>` (OpenPGP, OATH, YubiOTP move). Composite shape open: lean R1 (plain records
with writable fields; `IDisposable` only for secret holders) in `Inbox/YubiKit pass-2 answers 2026-11-20 round 5.md`.

Status 2026-11-20 21:30: **X3 settled** (AG-25, R1). All 13 cross-applet decisions are settled and the veto pass is
done (no vetoes). Final per-card calls: `pass-2-review-ledger.md`, which supersedes the queues below.

## Interview queue: High (25)

| Applet | Cards |
| --- | --- |
| WebAuthn | WA-IMP-011 (G1, G2, G3: PRF must be the UV-verified one), WA-IMP-012 (unsupported credProtect silently omitted), WA-IMP-013 (residual PRF/blob plaintext) |
| Management | MGMT-IMP-010 (reset eligibility, G3) |
| CTAP2 | CTAP-IMP-009 (length check after decrypt), CTAP-IMP-012 (bio enrollment), CTAP-IMP-029 (ARKG, experimental), CTAP-IMP-048 (reset not bound to confirmed key across reinsertion) |
| Security Domain | SD-IMP-002 (empty SCP03 DELETE filter; all siblings reject) |
| YubiHSM Auth | HSM-IMP-002 (owned key arrays survive failures), HSM-IMP-005 (sample prints long-lived keys), HSM-IMP-008 (strict 48-byte parse rejects a recorded 56-byte response) |
| OpenPGP | PGP-IMP-001, 002, 003, 005 (G3), 009 (G3), 011 (ROCA), 015 (split: status display and certificate disposal separable) |
| PIV | PIVIMP-001, 002 (G3), 005, 006a, 006b, PIVCORE-002 |

Medium, Low and D0 interview cards: see each per-applet file's "Interview queue" section.

## T1 hardware qualification (17)

OATH-DEC-004, OATH-DEC-006, OATH-DEC-007, OATH secure-reset; WebAuthn ceremony/platform; MGMT-IMP-002, MGMT-IMP-011,
Management qualification (includes NFC-restriction unlock: Rust docs say touch, .NET and Python say USB power);
CTAP-DEC-003, CTAP-IMP-022; SD-DEP-001, SD device/producer; OTP-IMP-012; QUAL-HSM; OpenPGP physical/chronology;
PIVIMP-003, PIV-QUAL. Device matrices are in each per-applet T1 table.

## Changes to batch 1 and to the master

- **WA-IMP-003:** batch-1 "reject out-of-range timeout" withdrawn. WebAuthn L3 §5.1.3/§5.1.4 say clamp. Now interview (G2).
- **WA-IMP-001:** A3 upgraded to A2; Rust also forwards enterprise attestation. The master's "no enterprise field" is
  wrong: Ctap2 already has `MakeCredentialOptions.EnterpriseAttestation`.
- **OATH-DEC-003:** A6 withdrawn; Key URI text makes the HOTP counter required while all siblings default to 0 (G2).
- **OATH-DEC-001:** stays A2. Batch-1 said a retry "can't succeed"; the classifier said a retry is a fresh exchange.
  Spot-check: `OathSession.cs:715` generates a fresh *host* challenge, but the stored *device* challenge from SELECT is
  reused. Whether the device accepts a second VALIDATE on the same SELECT is a hardware question (added to OATH T1).
- **OATH-DEC-007:** master pin off by one; `FeatureScp03` is at `OathSession.cs:37`.
- **YubiOTP:** Android's static-password floor is 2.0, not 2.2; Rust CLI is not a clean precedent for OTP-IMP-007;
  `PublicAPI.Unshipped.txt:177` is stale for `ConfigFlagMasks.UpdateMask`.
- **PIV:** Android aliases object IDs like .NET, Python and Rust don't (PIVIMP-010 is A4). Python clears the derived
  salt on management-key rotation, which the master's lean diverges from (PIVIMP-002).
- **WebAuthn:** WA-IMP-005, 010, 015 and 016 lose their master "no-brainer/clean" status due to spec or sibling gates.

## Sia's spot-checks

| Claim | Result |
| --- | --- |
| Rust shares the YubiOTP short-status leniency (`yubiotp.rs` ~1410–1417) | Confirmed: a missing status byte returns `Ok` |
| Python clears derived salt on management-key change (`ykman/piv.py:345–348`) | Confirmed |
| OATH retry is a fresh exchange (`OathSession.cs:715`) | Partly: host challenge fresh, device challenge reused (see above) |
| Android `ObjectId` masks to 24 bits | Not confirmed by a quick grep; the engineer must re-pin before implementing PIVIMP-010 (E1) |

## Known limits

- yubikey-manager was not read by the CTAP2 classifier; its citations there are the master's, unverified.
- OpenPGP 3.4.1 PDF could not be fetched; §4.3.2 and §7.9 cites unverified. CTAP checks used the 2.1 errata; CTAP-IMP-030
  needs CTAP 2.2. No normative YubiHSM Auth spec was located, so G2 could not be evaluated there.
- PIV's review map has no severity column; severities came from card bodies.
- One classifier wrote a scratch spec copy to `/tmp` and deleted it; no other writes outside assigned files.
