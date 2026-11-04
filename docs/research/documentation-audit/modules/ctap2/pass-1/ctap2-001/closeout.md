# CTAP2 cycle ctap2-001 closeout

## Current outcome

Pass-1 content, independent source/boundary reviews and verification are complete against
`5b09a9b6942ded3913b5c9340c3e126617e61a82` on `yubikit-docs-audit`. Archival retention and
[final record/evidence follow-up](review-closeout-followup.md) passed with no remaining required issues.
The [CTAP2 handoff](../../../../ctap2.md) records the source commit and verified remote checkpoint after
publication; that entry is outside the run snapshot manifest.
Pass 2 remains deferred until the first sweep across the remaining selected modules is complete.

## Coverage and content

- All **183 CTAP2 module paths** dispositioned:73 library C#,77 test C#,20 FidoTool C#, five Markdown
  and eight API/project/runner support files. No whole-Core or whole-WebAuthn audit is implied.
- **85 public type families /948 API snapshot entries**, including generated projections and four
  historical removal entries. Meaningful source contracts were reviewed; generated boilerplate was not
  padded with invented prose.
- F's133 external reference paths distinguish actual full/narrow reads, collateral discovery and
  historical material. G adds a dedicated, bounded CTAP/WebAuthn claim map and fresh final review.
- **165 authored content paths reviewed**, plus five regenerated architecture outputs (two SVG/PNG pairs
  and manifest). The **152 edited C# paths** contain documentation changes plus explicitly recorded
  formatter-only namespace-import/whitespace cleanup. SDK behavior/APIs, test assertions, executable
  sample strings and device state were not changed.
- **103 overlapping initial candidate groups** reconciled across A–G; late COORD-001 is also retained.
  Numbered IMP-001–048 contain **47 deferred engineering/verification/decision groups** and one rejected
  ordering mechanism (IMP-017). These are not48 confirmed bugs or blanket implementation authorization.
  Existing WA engineering IDs remain separate and open; Core input-reader dependencies remain bounded.

## Documentation improvements

- Complete session/GetInfo/transport/raw/notification/cancellation/reset contracts with exact support,
  configured-state, permission, firmware/sentinel, timing and ownership distinctions.
- PIN/UV algorithms, padding, protocol selection versus command fallback, scope/permissions, Unicode
  obligations, retry meanings, protocol disposal and returned-buffer responsibilities.
- Credential/request/response/COSE/attestation contracts, real parsing versus verification, opaque user
  handles versus identifying fields, exact descriptor-omission revision and unknown/borrowed data limits.
- Administrative and extension guidance covers eager/stateful sequences, counts/estimates, actual
  biometric wire/schema limitations, configuration outcomes, storage routes/chunks and scoped cleanup.
- previewSign/ARKG and encrypted metadata have explicit publication/profile/algorithm/ownership/trust
  boundaries; experimental availability is not production or whole-draft conformance.
- Guides preserve real protocol tasks and distinguish them from RP registration/sign-in. FidoTool's
  subset, fixed protocol2, verify-PIN scope, cancellation and reset-target limitations are explicit.
- Test explanations follow actual assertions/setup/cleanup rather than names, fake data, conditional
  returns, sensor metadata or claimed certification. Real unfinished work remains in the ledger.

## Independent review and rejected inferences

Sol6.1 performed investigations, fresh editing and independent reviews; Astra coordinated triage,
consequential checks, sampling, integration and validation. All required source/boundary corrections
were re-reviewed. Exact pre-format review hashes, final hashes and bounded transforms are retained.

- Canonical CBOR writer auto-sorting rejected D-08's inferred update-user wire defect; source insertion
  commentary was corrected without an encoding change.
- Original legacy0x05 forcePINChange PIN_INVALID differs from permission0x09 PIN_POLICY_VIOLATION.
  D-14's alleged wrong-status assertion was rejected; real test-state/cleanup concerns remain.
- ARKG-10 explicitly supplies seed placeholder-65700; partial agreement was separated from actual
  kty/wrapping/dkalg/profile differences. BE/BS uses an independently verified pinned L3 Recommendation.
- COORD-001 found absent encrypted GetInfo fields becoming present empty nullable memory; documentation
  now distinguishes that from wire presence and support. Code remains deferred.
- Astra completed20 bounded checks: **19 eligible clean units/132 (14.4%)**, exceeding minimum14,
  plus one supplemental behavior check not counted. No material clean-unit miss found.

## CTAP/WebAuthn result

The dedicated investigation and fresh integrated review assess29 claim rows and G-001–011. Corrected
CTAP User semantics satisfy **WA-XREF-001 as documentation only**. Existing WA-IMP-010 recovery and all
other WA engineering items remain open. Legitimate client orchestration/support-policy differences are
preserved, and DEC-002 native selected-user privacy/serializer scope is not silently decided.

Final boundary review also corrected three narrow WebAuthn dependencies: requested permission union
versus issued-token guarantee; makeCredential unsigned outer key0x06 versus getAssertion0x08; and inner
previewSign policy flags versus tolerated outer key4. No WebAuthn runtime or wider audit completion claimed.

## Verification

See [validation.md](validation.md), [snippet-validation.md](snippet-validation.md) and exact retained logs:

- Clean solution build and **3,998 unit tests**, including **492 CTAP2**, five existing skips, zero failures
  and **zero compiler/analyzer warnings**. Docs/package, architecture and complexity gates passed;
  four complexity violations remain unchanged existing debt.
- Scoped formatting passed. All declaration/body tokens and preprocessor directives preserved, with
  **31 explicitly listed unused namespace-import removals in30 files** plus import ordering/whitespace
  separately proven. Strict pre-format check also passed. All20 API snapshot files unchanged.
- **33 example groups** compiled across six real SDK contexts, all zero warnings. Every original selected
  body retained;95 blocks inventoried,39 selected,56 explicitly excluded. Extractor and collector failures
  remain preserved rather than hidden by stubs, assertion removal or warning suppression.
- Pure framework probes established writer sorting/trailing-map behavior on Linux/.NET10.0.14 and Guid
  conventions from pinned sources. Big-endian behavior is inferred, not platform-executed.
- L2/L3b architecture images regenerated with pinned Mermaid11.16.0 and inspected. Other source-identical
  image pairs retain verified cached hashes; freshness/evidence checks passed.
- No live CTAP/WebAuthn ceremony, PIN/UV/bio/storage/reset operation, integration test or cross-platform
  execution occurred. These results establish neither hardware qualification nor full conformance.

## Durable handoff and next target

Use [triage.md](triage.md) plus [triage-addendum.md](triage-addendum.md) for stable IDs, observed/desired
distinctions and exact documentation re-review triggers. Coverage, matrices, source excerpts, reviews,
corrections, failed attempts, compiled inputs, actual compiler/analyzer provenance and final patches/hashes
remain in this run. Re-baseline selected findings before the later implementation sweep.

The approved next target is **Security Domain**, followed by **YubiOTP**. PIV and Core remain for last,
with Core's detailed scope separately planned; other remaining applets retain their scheduling status.
Do not begin CTAP or WebAuthn Pass2 on this closeout. The release is breaking; backward API compatibility
is not a goal. Four original untracked user files remain outside this work.

## Lessons carried forward

- Check emitted bytes/framework behavior before calling source insertion order a wire defect.
- Verify exact subcommand/revision and actor, not just a plausible status or section name.
- Distinguish borrowed slices, materialized arrays, nullable conversions and actual cleanup owners.
- A separate dependency-boundary reviewer catches stale claims that individual module reviews miss.
- Compile full meaningful examples in their real dependency/analyzer contexts; resource using statements
  must remain inside their lifetime scopes, and genuine package-generated sources need precise provenance.
- Keep validation project names out of ordinary module/test discovery and retain textual evidence rather
  than duplicate generated runtimes. Preserve original failures and exact exclusions/path mappings.
