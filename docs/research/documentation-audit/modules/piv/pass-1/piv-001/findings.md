# PIV triage and implementation return ledger

Astra,2026-11-05; baseline727fe43afc430c5d8364d868a6436de50626e476. Investigator reconciled123 paths,
23 public types,370 API entries and42 interface operations. All11 documentation candidate groups
are accepted for fresh editing and independent original-citation review. Their exact observations,
paths and reopening conditions remain in investigation.md and evidence/source-analysis.md.

## Accepted documentation direction

- **C001:** normal PIV reset scope, retained F9 material, partial retry consumption and post-reset
  metadata-refresh failures; Bio MPE/preflight remains an implementation/capability question.
- **C002:** actual UTF-8 byte validation per operation, standards/profile distinction, precise retry
  fallback/count uncertainty, credential resetting effects and actionable recovery.
- **C003:** algorithm-specific input/output formats, host preparation/verification, raw agreement and
  mutable-owned output lifetime. Public-key extractor examples cannot imply access to device private keys.
- **C004:** separate keys/certificates and enrollment binding, fixed SDK provisioning models versus
  general standard shapes, metadata raw encodings/unknown identifiers, parser limits and typed/raw routes.
- **C005:** usable PIN-only setup/reopen/recovery/disable tasks, already-active key requirements, partial
  writes and blocked PUK, related recovery-object coherence and caller recovery responsibilities.
- **C006:** management versus PIN versus SCP state, card-policy lifetimes, actual callback locations,
  parsed completion versus local unpadding, cooperative cancellation/drain and qualified fresh recovery.
- **C007:** current source gates, vendor release/profile distinctions, ROCA interval, FIPS scope,
  old-family limits and catalog identity versus actual PIV operation support.
- **C008:** present mechanisms in internal/test/tool commentary; preserve genuine unfinished work.
- **C009:** task-complete PivTool guidance and compilable current examples, meaningful scopes/callbacks,
  command/result limitations and the actual RSA/Ed25519 framing problem. Runtime help remains unchanged.
- **C010:** assertions actually established, fixture eligibility, cleanup and outliving tasks; distinguish
  generated keys/output shape from crypto/key-binding evidence.
- **C011:** concrete reference inheritance, model fields and scoped current migration/shared examples,
  preserving historical v1 passages and existing bounded qualification.

Write direct, calm, precise and approachable prose. Prefer affirmative task/actor/behavior first,
retain useful rationale and local conditions, and pair consequential limitations with supported
actions. The next release is breaking; this sweep still changes documentation rather than runtime.

## Deferred engineering and Core questions

Preserve the investigator's stable **PIVIMP-001 through010** identities and exact path/reopening map:
reset Bio preflight; PIN-only transition/coherence decisions; firmware/profile qualification; typed
object generality; PivTool algorithm-specific signing; tool workflow/ownership/cancellation defects;
crypto-response/padding/decompression handling; test oracles/fixture/drain/cleanup; AdminData timestamp
interoperability; raw object-ID request range. These10 groups contain overlapping observations, not
an assertion of10 newly proven runtime bugs. Original sources and explicit missing evidence govern.

**PIVCORE-001 through003** retain physical PIN state/handle isolation, cancellation/workflow serialization,
and shared owned-response/borrowed-connection lifetime for the final Core cycle. No Core boundary audit
is opened here. Source analysis already identifies numerous earlier findings fixed by745 or preceding
commits; preserve their rebaseline rather than re-listing them as current defects.

AdminData timestamp interoperability is source-confirmed against pinned Python: the C# trimmed
little-endian representation differs from fixed four-byte big-endian. A bounded independent host
encoding comparison may validate the documentation; implementation/migration decisions stay Pass2.

## Execution checkpoint

Full source/effect matrix covers89 integration methods across24 files. None explicitly replaces F9
private material or installs irreversible policies. After complete serial103/125 read-only baselines
and successful independent103 cleanup control, coordinator selected the full unattended lane directly:
86 trait-selected methods, expected83 single-key executions and3 multi-key skips under103-only allowlist,
subject to actual runner evidence. This includes slow cases and avoids a redundant smoke rerun. Raw
results, setup/failure scope and independent cleanup follow; no passing outcome is presumed.

Fresh editor owns PIV module documentation/comments/descriptive metadata and PIV-only sections of
migration guide/map/gaps, comparison, user-interaction, raw-access-tiers and applet-public-API guidance
as needed. Root/package/highlights/AOT claims found clean stay in coverage, with precise new edits
requiring scope reconciliation. Preserve DOC-002 and original user files. Record every changed
path/hash, candidate mapping and complete example scope in edit-report.md.

## Independent review adjudication

PIV-REV-001–003 require six-path documentation corrections. Coordinator accepts the concrete findings:
the F9 retrieval helper populates AttestationCertificate while its example inspects the null alternate
property; partial credential rotation can leave one or both credentials at factory values; and the
SHA256/differential Ed25519 tests need their actual limited oracles described. Runtime helpers and
assertions stay unchanged. F9 result-shape coverage extends existing PIVIMP-006 rather than creating
another engineering group. Fresh editor applies corrections; the original reviewer rechecks exact hashes.

Internal/test sampling expanded by ten additional unchanged regions after the residual oracle misses,
bringing the provisional independent sample to61. Original citation review accepted revision/status/
actor/profile distinctions and the approved style, with no optional stylistic rewrite requests.

## Executed supplemental evidence

P01–P04 passed RFC-bound X25519 with an independent same-key vendor control, repeated PIN-policy
consumption, key/certificate separation and preserved-issuer attestation/key binding. P05/P06 confirmed
**PIVIMP-005** in the actual PivTool helper: direct SDK controls passed while the RSA helper output
failed original-message verification, and Ed25519 output verified againstSHA256(message) rather than
the original message. The two intended-contract assertions remain failed; source/tool fixes awaitPass2.
Existing public qualifications already describe these limitations and are undergoing a bounded late-
evidence impact check. No implementation group was added solely because a second oracle confirmed it.

Canonical coverage is86/89 methods passed. Three physical-lane methods and supplemental cancellation/
recovery-touch remain deferred by the user. Both dedicated keys' final captured public states match
their initial baselines, with existingF9 preserved and factory authentication independently verified.
Original test allowlist bytes are restored. Final compiler/repository/evidence/publication gates remain
separate from these scoped device results.
