# Independent reviewer A — narrow R1/R2 follow-up

2026-11-03; same independent reviewer as `review-a.md`; final current-file correction review after editor C completed.

## Verdict: PASS

**R1 and R2 are resolved. No remaining required correction in the assigned follow-up scope.** This closes the two blockers in `review-a.md`; it is not a new whole-module review or campaign-gate result.

Read the four assigned current files/affected sections, `edit-c.md`'s A-correction map and verification boundaries, the triage review amendment, and the leading errata in investigation-a/edit-a. Compared the revised attribution with the primary specification evidence independently retrieved during the original review. No broader re-review, tests, builds, formatter, hardware operation, repository edit, or delegation. This follow-up report is the only file authored in this turn.

## R1 — version attribution and empty-ID limitation: PASS

| Current file / lines | Verified disposition |
| --- | --- |
| `src/WebAuthn/src/Client/Authentication/AuthenticationResponse.cs:27–33` | Explicitly attributes singleton omission permission to CTAP2.0 §5.2, and says CTAP2.1/2.3 §6.2.2 require the descriptor. Retains the actual empty-ID behavior as a client limitation, not a usable empty identifier. Original-single-entry application context and mandatory RP response/account verification remain local. Explicitly rejects taking the first entry of a multi-entry list. |
| `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs:49–54,216–217` | Public remarks make the same exact version distinction and preserve the missing-recovery limitation. The final construction still uses `ctapResponse.Credential?.Id ?? ReadOnlyMemory<byte>.Empty`, consistent with the disclosure. The original-single-entry qualification and prohibition on first-ID guessing remain. The construction comment now cites CTAP2.0 §5.2 rather than implying a later-version exemption. |
| `src/WebAuthn/src/Client/CredentialFilter.cs:130–138` | Replaces the inherited incorrect generic §6.2.2 attribution with CTAP2.0 §5.2 versus CTAP2.1/2.3 §6.2.2. The singleton branch returns its sole chunk descriptor after a successful probe; the multi-entry branch matches the returned ID. This is not advice to recover a missing final assertion ID by guessing the first original allowed credential. |

The revised claims agree with the exact primary evidence recorded in the original review:

- [CTAP2.0 PS, 30 Jan 2019, §5.2](https://fidoalliance.org/specs/fido-v2.0-ps-20190130/fido-client-to-authenticator-protocol-v2.0-ps-20190130.html#authenticatorGetAssertion): response `credential (0x01)` is **Optional** and “May be omitted if the allowList has exactly one Credential.” The actor is the authenticator, and the condition concerns the transmitted singleton allow list.
- [CTAP2.1 PS with 21 Jun 2022 errata, §6.2.2](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#sctn-getAssert-authnr-alg) and [CTAP2.3 PS, 26 Feb 2026, §6.2.2](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#sctn-getAssert-authnr-alg): response descriptor is **Required**, without that exemption.

The correction does not conceal or claim to fix the SDK's actual empty-ID result path. It also preserves the important distinction between the final narrowed request and the application's original multi-entry request.

## R2 — dummy-request test summary: PASS

`src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientCeremonyFrameTests.cs:380–402` now describes the dummy final request's normal required-UP policy and the empty result for `NoCredentials`. It explicitly says the fake checks the request/result, **not a gesture or a no-disclosure guarantee**.

That matches the complete affected test: lines 389–391 arrange fake `NoCredentials`; lines 396–401 check the empty match list, absent explicit options, and dummy descriptor/type. The former unconditional “cannot learn credential absent without a gesture” assurance is gone. Authentication implementation comments at `WebAuthnClient.Authentication.cs:158–161,177–178` remain appropriately bounded and do not claim equal timing/prompts.

## Evidence errata and handoff: repaired

- `investigation-a.md:3–6` explicitly supersedes C2/Q1's incorrect inherited version attribution, names CTAP2.0 PS 2019-01-30 §5.2 and the later required-field rule, preserves the source finding, and labels the original body historical. **This repairs the inherited attribution without rewriting investigation history.**
- `edit-a.md:3–5` likewise explicitly supersedes the wrong A-Q1 citation and distinguishes the later required descriptor. **Its attribution erratum is correct.** Its statement that R1/R2 await correction/re-review records the pre-follow-up status; this PASS report now supplies the subsequent resolution and should be used with `edit-c.md`, not interpreted as leaving those corrections open.
- `triage.md:37–49` accepts the version correction, the narrow CredentialFilter citation correction, and the test-assurance weakening, and assigns precisely these four files to C. Its distinction between the real empty-ID limitation and version-specific permitted omission is sound.
- `edit-c.md:42–45` accurately describes the four corrections observed in current source. Its lexical/XML checks are not relied upon as semantic proof here.

## Remaining corrections and limits

**Required:** none for R1/R2. **Optional:** none needed to close this follow-up.

No deferred engineering item is closed: identifier recovery remains future implementation work. No broader C prose, B changes, runtime behavior, compiler cref resolution, or campaign build/test/documentation gate is accepted by this narrow PASS. The coordinator retains those gates.
