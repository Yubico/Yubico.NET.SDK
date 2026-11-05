# PIV hardware results

## Current checkpoint

Primary serial103, USB-A YubiKey5 NFC Enhanced PIN, Linux USB SmartCard, metadata5.8.0.beta.0.
Direct PIV GET VERSION returned000001; CLI5.8.0 and Management metadata remain distinct provenance.
Full25-slot baseline and existingF9 certificate/SPKI are recorded in baseline-103.json.
Secondary125 has a separate read-only baseline at5.8.0.alpha.2, with its ownF9 identity.

**Canonical unattended batch: PASS,83 methods executed,3 multi-key methods skipped, zero warnings.**
Elapsed483.8 seconds. Exact command/result/full text retained under validation/integration-unattended-103.
The103-only allowlist excludes125 from mutation. All selected single-key slow methods executed.
The original89-method denominator includes three RequiresUserPresence methods outside this filter.

Independent vendor-v2 cleanup afterward passed reset, full fresh-state equality, factory PIN and
AES192 management-key authentication, and another fresh-state equality. All24 normal slots/objects
returned to baseline and existingF9 key metadata/certificate/policies remained unchanged. Reset and
authentication proof are distinct outcomes. Initial v1 cleanup failed before mutation on a transport-
chaining comparison assumption; v2 retains and corrects that evidence failure.

## Physical-action deferral

User selected **“Defer physical tests”** when asked to coordinate the PIV touch ceremony. Therefore
the existing read-only presence-prerequisite method, touch-signing method and hotplug method remain
not run in this audit's physical-action lane. The prepared supplemental cancellation/recovery case
also requires a later touch, so it remains deferred. No touch/cache/hotplug/native-cancellation claim
is inferred from current unit, unattended hardware or preparation evidence.

## Supplemental and two-key work

Six unattended supplemental cases are prepared: RFC7748 X25519 agreement, repeated PIN-policy
consumption, key/certificate independence, preserved-issuer attestation binding, actual PivTool RSA
signing and actual PivTool Ed25519 signing. They use independent cryptographic oracles and retain
desired-contract failures. The seventh coordinated-touch cancellation case is deferred by the user.
All six unattended probes executed with one case each and zero skips. Compiler captures qualified
actual sources, references, analyzers and the twelve-project closure; loaded/runtime DLL identities
matched their compilation contexts. Every probe received successful independent before/after cleanup.

| Probe | Actual outcome and scope |
| --- | --- |
| P01 X25519 | **PASS.** Imported RFC7748 Alice public value matched; SDK agreement with Bob matched the exact RFC secret. Independent installed Python agreed on the same key before reset/import/cleanup. |
| P02 PIN policy | **PASS.** First Always signature host-verified; second unverified attempt returned InvalidOperationException and no signature. Reverification restored same-key signing; two Once signatures verified after one verification. The6982 attribution comes from SDK mapping, not independently captured raw status. |
| P03 key/certificate separation | **PASS.** Moved key metadata/SPKI and signature matched the generated key. Deliberately unrelated certificates in both source/destination retained exact DER through key move and deletion. |
| P04 attestation | **PASS.** Certificate signature verified under preservedF9 RSA issuer; generated/metadata/attested SPKIs matched; issuer certificate bytes stayed identical. Manufacturer-root trust was not evaluated. |
| P05 actual PivTool RSA helper | **FAIL, intended contract preserved.** Direct SDK signing of host-prepared EMSA-PKCS1-SHA256 verified. Actual helper reported success, but its output failed SHA256/PKCS1 verification of the original message. Caller bytes stayed unchanged. |
| P06 actual PivTool Ed25519 helper | **FAIL, intended contract preserved.** OpenSSL verified the direct SDK original-message control. Actual helper reported success; its output failed original-message verification and passed verification over SHA256(message). Caller bytes stayed unchanged. |
| P07 coordinated cancellation/recovery | **NOT RUN — user deferred physical tests.** Preparation and compilation establish no terminal timing, cancellation or recovery-touch outcome. |

P05/P06 confirm two algorithm manifestations of existing **PIVIMP-005**, not new independent bug
groups. Both failed assertions remain failures despite successful controls and cleanup. Public OpenSSL
fixtures live outside RUN with exact identities; no private fixture/key, management key or PIN bytes
are logged. Earlier two successful-build/failed-capture invocations stopped before any probe ran and
are not counted among the six executed supplemental methods.

All three existing multi-key methods subsequently **passed**, zero skips/warnings,117.6seconds.
Coordinator read the complete322-line class/helper effects, verified125's cleanup control and a fresh
103 control, then selected exactly103+125. The cases exercised discovery while one key was busy,
concurrent signing loops and RSA4096 cross-key liveness in both directions. Their output-size/timing
assertions retain their narrower scope; they do not supply independent cross-key signature binding.

Both devices then passed independent reset, factory PIN/AES192 authentication and complete fresh
baseline equality, including their distinct F9 identities. Fixture scope returned to103 afterward.
The canonical total is now **86 distinct methods passed out of89**, with the three physical-lane
methods deferred by the user. The earlier three single-key fixture skips remain preserved alongside
this later dedicated two-key execution. Both keys are development5.8 devices; these results establish
no older-release, NFC, Bio, FIPS, other-OS or Native AOT runtime qualification.

## Final restoration

After the last supplemental cleanup, coordinator opened fresh independent vendor sessions on103 and125.
`final-reconciliation.json` verifies every captured stable field equals each device's own baseline,
excluding only observation time and capture label. Both retain default PIN/PUK3/3, AES192/default/Never
management metadata, empty24normalkey/certificate slots, captured public-object absence and preserved
distinctF9 identities/policies. Factory PIN/management authentication was proved by the preceding
independent cleanup receipts. Protected PRINTED content was deliberately excluded from baseline and
readback; these records make no separate observed byte-absence claim for that unread object.

The original tracked test allowlist was restored **byte-for-byte**, with no remaining source diff.
All device/probe processes completed before final readback; no hardware job remains active. Existing
F9 private keys were never replaced. Public-state equality plus successful issuer-signature proof
has its recorded scope; broader platform/product/native-cancellation qualification remains deferred.
